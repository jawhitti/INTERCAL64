# Fixing the Shared NEXT Stack Bug

## The Bug

COME FROM loops inside callable subroutines hung when calling syslib routines. The program would enter the syslib, and execution would never return to the correct point in the COME FROM loop. The same programs ran correctly on C-INTERCAL (ESR's reference implementation).

This bug blocked any non-trivial use of COME FROM — including the Stable Marriage algorithm and any program pattern where a reusable subroutine needed an internal loop that called library routines.

## Discovery

The bug was discovered while writing reproducers for "COME FROM Considered Necessary." We had four programs:

- `lemma1.i` — broken INTERCAL-72 program (callable subroutine with FORGET loop). Expected to fail.
- `lemma2.i` — broken INTERCAL-72 program (FORGET loop with syslib calls). Expected to fail.
- `lemma1_comefrom.i` — COME FROM fix for Lemma 1. Expected to work.
- `lemma2_comefrom.i` — COME FROM fix for Lemma 2. Expected to work.

The broken programs correctly failed on both compilers. The Lemma 2 fix worked on both. But the Lemma 1 fix — COME FROM loop inside a callable subroutine — worked on C-INTERCAL and hung on SCHRODIE.

## Investigation

### First wrong turn: C-INTERCAL is broken too?

The Lemma 1 COME FROM fix initially crashed on C-INTERCAL with E621. This sent us down a multi-hour investigation where we:

1. Discovered our `RESUME .5` trampoline pattern used `.5 ~ #1` to map `{1,2}` to `{1,0}`
2. Realized `RESUME #0` is illegal on C-INTERCAL but silently ignored on SCHRODIE
3. Discovered that `DO ABSTAIN FROM (target)` abstains the statement, not the COME FROM that references it
4. Found that our fizzbuzz also crashes on C-INTERCAL for the same reason
5. Eventually found the beer.i double-NEXT pattern that works on both compilers

After fixing the INTERCAL-level issues (documented in `come_from_findings.md`), Lemma 1 worked on C-INTERCAL but still hung on SCHRODIE. Now we knew it was a compiler bug.

### Looking at the generated C#

The minimal reproducer:

```intercal
DO (500) NEXT
DO READ OUT .1
DO GIVE UP

(500) DO .5 <- #2
      DO COME FROM (599)
      DO .2 <- #1
      DO (1000) NEXT     ← hangs here
      DO .1 <- .3
      DO (80) NEXT
(599) DO .5 <- #1
(80)  DO (81) NEXT
      DO FORGET #1
      DO RESUME #1
(81)  DO RESUME .5
```

The generated C# for the `DO (1000) NEXT` cross-assembly call was:

```csharp
var _call = new ComponentCall(frame.ExecutionContext);
syslib64Prop.DO_1000(_call);
if (frame.ExecutionContext.Done) goto exit;
int _rd = _call.NextStackDepth;
if (_rd > 0) {
    int _retLabel = 0;
    int _popped = 0;
    while (_popped < _rd && _nextStack.Count > 0) { _retLabel = _nextStack.Pop(); _popped++; }
    if (_retLabel > 0) { switch(_retLabel) { case 1: goto _ret_1; case 2: goto _ret_2; ... } }
    goto exit;
}
```

### The root cause

Each compiled component had its own `_nextStack` — a local `Stack<int>` field. Cross-assembly calls communicated RESUME depth back via `NextStackDepth` on `ComponentCall`.

The syslib's `(1007) RESUME #2` popped 2 entries from the syslib's own `_nextStack`: one internal entry and one sentinel. Finding the sentinel (value 0), it set `frame.Call.NextStackDepth = 1` and exited.

The caller received `NextStackDepth = 1` and popped 1 entry from its own local `_nextStack`. But the caller's stack had the return label from `DO (500) NEXT` — which had nothing to do with the syslib call. The popped label dispatched execution back to main, bypassing the rest of the subroutine.

In other words: the syslib call was "returning" to the wrong location because two independent stacks couldn't communicate return labels correctly.

### Why it worked for simple programs

Programs like `test_add.i` and `fizzbuzz.i` worked because their `_nextStack` was empty (or had only unrelated entries) when the syslib returned. The `while` loop condition `_nextStack.Count > 0` prevented popping from an empty stack, `_retLabel` stayed 0, and the `goto exit` was harmless.

The bug only manifested when the caller had entries on its local stack — specifically, when a syslib call happened inside a callable subroutine (which pushed a return label via `DO (subroutine) NEXT`).

## The Fix

### Architecture change

Move the NEXT stack from a per-component local field to a shared stack on `ExecutionContext`. All components push and pop on the same stack.

### ExecutionContext (utils.cs)

Added three methods:

```csharp
private Stack<int> _sharedNextStack = new Stack<int>();

public void NextPush(int returnLabel) { _sharedNextStack.Push(returnLabel); }

public int ResumePop(int depth) {
    int retLabel = 0;
    for (int i = 0; i < depth && _sharedNextStack.Count > 0; i++)
        retLabel = _sharedNextStack.Pop();
    return retLabel;
}

public void ForgetPop(int depth) {
    for (int i = 0; i < depth && _sharedNextStack.Count > 0; i++)
        _sharedNextStack.Pop();
}
```

### ComponentCall (twisty.cs)

Added `ReturnLabel` property. When a component's RESUME pops a return label that doesn't match any local label (hits the `default` case in the dispatch switch), it sets `frame.Call.ReturnLabel` and exits. The caller dispatches based on this label.

### Code generation (confuse.cs, futile.cs)

All `_nextStack.Push(N)` → `frame.ExecutionContext.NextPush(N)`
All `_nextStack.Pop()` loops → `frame.ExecutionContext.ResumePop(depth)`
All FORGET loops → `frame.ExecutionContext.ForgetPop(depth)`

RESUME dispatch gets a `default` case that propagates foreign labels:

```csharp
int _retLabel = frame.ExecutionContext.ResumePop(depth);
if (_retLabel > 0) { switch(_retLabel) {
    case 1: goto _ret_1;  // local labels
    case 2: goto _ret_2;
    default: if (frame.Call != null) { frame.Call.ReturnLabel = _retLabel; goto exit; } break;
} }
```

Cross-assembly calls no longer push to the shared stack — the sentinel pushed by the component's dispatch switch serves as the boundary marker. The syslib's RESUME #2 pops its internal entry + the sentinel, exits cleanly, and the caller continues normally.

### What stayed the same

- The goto-based state machine is unchanged
- The syslib source code is unchanged — zero modifications to syslib64.schrodie
- Client INTERCAL programs are unchanged — zero source modifications needed
- The sentinel (value 0) pushed at component entry points is retained
- `AsyncDispatcher` and thread-based `NextingStack` are retained (unused but not removed)

## What didn't work

### Attempt 1: Propagate NextStackDepth to caller's caller

Adding `if (frame.Call != null) { frame.Call.NextStackDepth = _rd; goto exit; }` before the local pop. This just moved the problem up one level — the caller's caller would pop the wrong entry.

### Attempt 2: Push return label AND keep sentinel

Pushing the caller's return label to the shared stack before the syslib call, in addition to the sentinel pushed by the dispatch switch. This double-pushed, leaving the caller's return label as a ghost entry that was never consumed.

### Attempt 3: Remove the sentinel

Removing the sentinel `0` from the dispatch switch. This broke basic syslib calls (E436) because the syslib's internal FORGET operations depend on the sentinel to absorb pops that would otherwise eat the caller's entries.

## Testing

Zero regressions across all test programs:

| Program | Result |
|---------|--------|
| hello.i | `hello, world` |
| test_add.i | All 7 values correct |
| fizzbuzz.i | 106 lines, all correct |
| collatz.i (input: 7) | Correct Collatz sequence |
| beer.i | 99 bottles, clean termination |
| Knight's tour | All 64 squares |
| Hilbert geo | 7 cities in range |
| lemma1_comefrom.i | **V** (was: hung) |
| lemma2_comefrom.i | **I II III IV V** |

## Impact

This fix unblocks:
- COME FROM loops inside callable subroutines with syslib calls
- The Stable Marriage (Gale-Shapley) implementation
- Any program requiring nested subroutine calls within COME FROM loops
- Closer alignment with C-INTERCAL's shared-stack semantics

## Timeline

- **Night 1:** Discovered both lemmas crash on C-INTERCAL. Confirmed COME FROM fixes on C-INTERCAL but Lemma 1 hung on SCHRODIE. Discovered `.5 ~ #1` / RESUME #0 bug. Found beer.i double-NEXT pattern. Documented in `come_from_findings.md`.
- **Morning 2:** Identified root cause in generated C# — per-component `_nextStack` causing cross-assembly return label mismatch. Implemented shared stack on `ExecutionContext`. Three failed attempts before finding the correct approach (keep sentinel, don't push caller's return label for cross-assembly calls). Clean test run. Merged to `dap-debugger`.

Total: ~4 hours of investigation, ~2 hours of implementation, 3 wrong approaches before the fix.
