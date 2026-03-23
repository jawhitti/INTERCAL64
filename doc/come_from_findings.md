# COME FROM Empirical Findings

## Summary

Testing the programs from "COME FROM Considered Necessary" on two compilers (SCHRODIE and C-INTERCAL via TIO/JDroid) revealed both language-level impossibilities and a narrow, specific pattern that resolves them. This document records every approach we tried, why most failed, and why exactly one pattern works.

## Compilers Tested

- **SCHRODIE** (our .NET compiler): cross-assembly and monolithic modes
- **C-INTERCAL** (ESR's reference implementation): via tio.run and JDroid online

C-INTERCAL is the authoritative implementation. Any program that crashes on C-INTERCAL is incorrect, regardless of whether it runs on SCHRODIE.

## Part 1: The Broken Programs (INTERCAL-72)

Both programs attempt the same task: count from I to V using syslib arithmetic. Both fail on both compilers.

### Lemma 1: Callable subroutine with FORGET loop (`samples/lemma1.i`)

A subroutine `(500)` contains a FORGET-based loop calling syslib `(1000)`. The first FORGET destroys the caller's return address R. The subroutine can never return.

```intercal
	DO .1 <- #0
	DO (500) NEXT
	DO READ OUT .1
	PLEASE GIVE UP

(500)	DO .1 <- #0
(501)	DO FORGET #1
	DO .2 <- #1
	DO (1000) NEXT
	DO .1 <- .3
	DO .2 <- #5
	DO (1010) NEXT
	DO .2 <- .3 ~ #65535
	DO .5 <- "?'.2~.2'$#1"~#3
	DO .5 <- .5 ~ #1
	DO (502) NEXT
	PLEASE RESUME #1
(502)	DO RESUME .5
	DO (501) NEXT
	PLEASE RESUME #1
```

| Compiler | Result |
|----------|--------|
| SCHRODIE | Hangs silently, no output |
| C-INTERCAL (JDroid) | **E421** — NEXT stack overflow |

### Lemma 2: FORGET loop with syslib calls (`samples/lemma2.i`)

A FORGET loop adds 1 per iteration via syslib `(1000)`, printing each value, targeting I through V. The trampoline exit's RESUME interacts with the FORGET to corrupt stack accounting.

```intercal
	DO .1 <- #0
(100)	DO FORGET #1
	DO .2 <- #1
	PLEASE DO (1000) NEXT
	DO .1 <- .3
	PLEASE DO READ OUT .1
	DO .2 <- #5
	DO (1010) NEXT
	DO .4 <- .3 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	DO .5 <- .5 ~ #1
	DO (300) NEXT
	DO GIVE UP
(300)	DO RESUME .5
	DO (100) NEXT
	PLEASE RESUME #1
```

| Compiler | Result |
|----------|--------|
| SCHRODIE | Hangs silently, no output |
| C-INTERCAL (JDroid) | **E421** — NEXT stack overflow |

**Note:** Our fizzbuzz implementation uses the same `.5 ~ #1` + `RESUME .5` pattern and also crashes on C-INTERCAL. This pattern is fundamentally broken on strict implementations.

## Part 2: Failed COME FROM Fix Attempts

We made six attempts before finding a working pattern. Each failure revealed a different constraint.

### Failed Attempt 1: Simple COME FROM + single-NEXT trampoline + `.5 ~ #1`

The most obvious approach: replace the FORGET loop with COME FROM and use a standard trampoline for the exit condition.

```intercal
DO COME FROM (699)
... syslib calls ...
DO .5 <- "?'.4~.4'$#1"~#3
DO .5 <- .5 ~ #1        ← maps {1,2} to {1,0}
DO (600) NEXT
DO GIVE UP
(600) DO RESUME .5
(699) DO FORGET #1
```

| Compiler | Result | Root Cause |
|----------|--------|------------|
| SCHRODIE | Appears to work | RESUME #0 silently treated as no-op; FORGET on empty stack silently ignored |
| C-INTERCAL | **E621** | Two errors: (1) `.5 ~ #1` produces RESUME #0 which is illegal; (2) FORGET at (699) pops from empty stack |

**Why it fails:** The `.5 ~ #1` mapping converts `{1,2}` to `{1,0}`. When the loop should continue (`.5=2`), it becomes RESUME #0. C-INTERCAL correctly rejects RESUME #0. SCHRODIE silently treats it as a no-op — a compiler bug, not correct behavior.

**Lesson:** RESUME #0 is undefined/illegal. Never use `.5 ~ #1` to remap the zero-test result.

### Failed Attempt 2: COME FROM + ABSTAIN FROM target label to exit

Instead of a trampoline, try to exit the loop by ABSTAINing the COME FROM target.

```intercal
DO COME FROM (99)
... loop body ...
DO ABSTAIN FROM (99)    ← abstains the TARGET label
(99) DO .6 <- #0
DO GIVE UP
```

| Compiler | Result | Root Cause |
|----------|--------|------------|
| C-INTERCAL | **E621** (infinite loop until stack overflow) | ABSTAIN FROM (99) abstains the *statement* at label (99), not the COME FROM that references it. COME FROM still fires. |

**Why it fails:** `DO ABSTAIN FROM (99)` marks the statement `DO .6 <- #0` as abstained (it gets skipped when reached). But the COME FROM mechanism monitors whether execution *reaches* label (99), regardless of whether the statement there is abstained. The COME FROM still fires. The loop never exits.

**Lesson:** To exit a COME FROM loop, you must ABSTAIN the COME FROM statement itself. This requires a label on the COME FROM: `(98) DO COME FROM (99)` then `DO ABSTAIN FROM (98)`. This works on C-INTERCAL (confirmed) but is not needed for the beer.i double-NEXT pattern.

### Failed Attempt 3: Beer.i double-NEXT + `.5 ~ #1`

Adopt the double-NEXT wrapper from Matt Dimeo's beer.i, but keep the `.5 ~ #1` mapping.

```intercal
DO (600) NEXT
DO (101) NEXT
(600) DO (601) NEXT
     DO GIVE UP
(601) DO RESUME .5
(101) DO FORGET #1
(699) DO FORGET #1
```

| Compiler | Result | Root Cause |
|----------|--------|------------|
| SCHRODIE | Appears to work | Same RESUME #0 leniency |
| C-INTERCAL | **E621** | Same `.5 ~ #1` → RESUME #0 problem as Attempt 1 |

**Why it fails:** The double-NEXT structure is correct, but `.5 ~ #1` still produces RESUME #0. The wrapper doesn't help if the RESUME depth is wrong.

**Lesson:** The double-NEXT wrapper is necessary but not sufficient. The RESUME value must be `{1,2}`, not `{1,0}`.

### Failed Attempt 4: Correct Lemma 2 pattern applied to Lemma 1 (subroutine)

After getting Lemma 2 working (Attempt 5 below), apply the same beer.i double-NEXT + raw `.5` pattern inside a callable subroutine.

```intercal
DO (500) NEXT
DO READ OUT .1
DO GIVE UP

(500) DO COME FROM (599)
      ... loop body ...
      DO (80) NEXT
(599) DO .6 <- #0
(80)  DO (81) NEXT
      DO RESUME #1      ← return to caller
(81)  DO RESUME .5
```

| Compiler | Result | Root Cause |
|----------|--------|------------|
| SCHRODIE | Hangs | Same as C-INTERCAL |
| C-INTERCAL | Hangs | On the done path (.5=1), RESUME 1 pops R81 and returns inside (80). RESUME #1 then pops R80 (not R_500!) because R80 is on top of the stack. Returns to after DO (80) NEXT = (599). COME FROM fires. Infinite loop. |

**Why it fails:** Inside a callable subroutine, the caller's return address R_500 sits below the double-NEXT entries on the stack. When the done path executes, RESUME 1 pops R81 (inner), leaving the stack as `[R_500, R80]`. RESUME #1 pops the *top* of the stack — R80, not R_500. Execution returns to after `DO (80) NEXT`, which is `(599)`. COME FROM fires again. The subroutine can never return.

**Lesson:** Inside a callable subroutine, the done path must explicitly discard R80 before attempting RESUME #1. This requires a FORGET #1 between the double-NEXT exit and the RESUME #1 return.

### Failed Attempt 5: Various COME FROM + syslib combinations

Multiple variations tested on C-INTERCAL:
- COME FROM loop with `(1000)` calls only → E621
- COME FROM loop with `(1010)` calls only → E621
- COME FROM loop with padding NEXT before trampoline → infinite loop (padding breaks the done path)
- Various COME FROM + ABSTAIN combinations → E621 or hangs

All failures traced to either RESUME #0 (from `.5 ~ #1`) or stack accounting errors in the trampoline structure.

## Part 3: What Actually Works

### Isolation testing

Before finding the full solution, we isolated which components work:

| Test | C-INTERCAL Result |
|------|-------------------|
| Bare COME FROM loop, no syslib, ABSTAIN exit | **Works** (prints I) |
| COME FROM loop + `(1000)` NEXT, hardcoded `.5` | **Works** (prints I II) |
| COME FROM loop + `(1000)` NEXT, computed `.5` with `~ #1` | **E621** (RESUME #0) |
| COME FROM loop + `(1000)` NEXT, computed `.5` raw | **Works** (I II III IV V) |
| Bare double-NEXT trampoline inside subroutine, no COME FROM | **Works** (prints V) |
| Double-NEXT inside subroutine + COME FROM, no FORGET | **Hangs** (R80 blocks R_500) |
| Double-NEXT inside subroutine + COME FROM + FORGET #1 | **Works** (prints V) |

### The Working Pattern: Lemma 2 (top-level COME FROM loop)

```intercal
	DO .1 <- #0
	DO COME FROM (99)
	DO .2 <- #1
	PLEASE DO (1000) NEXT
	DO .1 <- .3
	PLEASE DO READ OUT .1
	DO .2 <- #5
	DO (1010) NEXT
	DO .4 <- .3 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	DO (80) NEXT
(99)	DO .6 <- #0
(80)	PLEASE DO (81) NEXT
	DO GIVE UP
(81)	DO RESUME .5
```

**Output on C-INTERCAL:** I II III IV V
**Output on SCHRODIE:** I II III IV V

### The Working Pattern: Lemma 1 (COME FROM loop inside callable subroutine)

```intercal
	DO .1 <- #0
	DO (500) NEXT
	PLEASE DO READ OUT .1
	DO GIVE UP

(500)	DO .1 <- #0
	DO COME FROM (599)
	DO .2 <- #1
	PLEASE DO (1000) NEXT
	DO .1 <- .3
	DO .2 <- #5
	DO (1010) NEXT
	DO .4 <- .3 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	DO (80) NEXT
(599)	DO .6 <- #0
(80)	PLEASE DO (81) NEXT
	DO FORGET #1
	DO RESUME #1
(81)	DO RESUME .5
```

**Output on C-INTERCAL:** V
**Output on SCHRODIE:** Hangs (compiler bug — COME FROM + syslib inside subroutine)

## Part 4: The Pattern Explained

The working pattern has four interlocking components. Removing or modifying any one of them causes failure.

### Component 1: COME FROM for the loop back-edge

```
DO COME FROM (target)
... loop body ...
(target) DO .6 <- #0        ← no-op; COME FROM fires here
```

COME FROM does not interact with the NEXT stack. When execution reaches `(target)`, control transfers to the statement after `DO COME FROM (target)`. No entries are pushed or popped. The caller's return address R (if inside a subroutine) is undisturbed across all iterations.

This is the fundamental insight of the paper: FORGET-based loops consume NEXT entries on every iteration. COME FROM-based loops consume zero.

### Component 2: Beer.i double-NEXT wrapper for conditional branching

```
DO (outer) NEXT               ← push R_outer
... continue-loop path ...    ← reached when .5=2
(outer) DO (inner) NEXT       ← push R_inner
        DO GIVE UP             ← reached when .5=1
        [or DO FORGET #1 + DO RESUME #1 for subroutine return]
(inner) DO RESUME .5           ← .5=1: pop 1, exit. .5=2: pop 2, loop.
```

The double-NEXT provides exactly the right number of stack entries for both RESUME depths:

- **`.5=1` (done):** RESUME 1 pops R_inner. Returns inside `(outer)` to the exit path (GIVE UP or RESUME #1).
- **`.5=2` (not done):** RESUME 2 pops R_inner AND R_outer. Returns to after `DO (outer) NEXT`, which is the continue-loop path leading to the COME FROM target.

Both paths land at the correct location with the correct stack state. No stack entries leak.

This pattern was discovered by Matt Dimeo in the 99 Bottles of Beer implementation (beer.i), which uses it for all conditional branches within a COME FROM loop. It is the only conditional branching pattern we found that is correct on both SCHRODIE and C-INTERCAL.

### Component 3: Raw zero-test value {1,2} in RESUME

The INTERCAL zero-test expression `"?'.4~.4'$#1"~#3` produces:
- `1` when `.4` is zero
- `2` when `.4` is nonzero

This value must be used **directly** in `DO RESUME .5`. The mapping `DO .5 <- .5 ~ #1` that converts `{1,2}` to `{1,0}` is **fatal**: RESUME #0 is undefined behavior. SCHRODIE silently ignores RESUME #0 (a compiler bug). C-INTERCAL correctly throws E621.

The semantic alignment is natural:
- Zero (done) → `.5=1` → RESUME 1 → pop one entry → exit
- Nonzero (not done) → `.5=2` → RESUME 2 → pop two entries → continue loop

### Component 4: FORGET #1 before subroutine return (Lemma 1 only)

When the double-NEXT wrapper is inside a callable subroutine, the done path has a problem: after RESUME 1 pops R_inner and returns inside `(outer)`, the stack contains `[R_caller, R_outer]`. The intended `DO RESUME #1` would pop R_outer (the top entry), not R_caller. Execution returns to after `DO (outer) NEXT` — the continue-loop path — and the subroutine never returns.

The fix is a `DO FORGET #1` immediately before `DO RESUME #1`:

```
(outer) DO (inner) NEXT
        DO FORGET #1         ← discard R_outer
        DO RESUME #1         ← now pops R_caller, returns to caller
```

This is only needed when the loop is inside a callable subroutine (Lemma 1). Top-level loops (Lemma 2) use `DO GIVE UP` instead and don't need the FORGET.

## Part 5: Why This Was So Hard To Find

Three factors conspired to make this pattern difficult to discover:

1. **SCHRODIE's leniency masked errors.** RESUME #0 and FORGET on empty stack silently succeed on SCHRODIE, making incorrect programs appear to work. Every one of our early "successful" COME FROM fixes was actually broken — we only discovered this when testing on C-INTERCAL.

2. **The `.5 ~ #1` idiom is widespread.** Our fizzbuzz, our test programs, and many INTERCAL programs use `DO .5 <- .5 ~ #1` to convert the zero-test from `{1,2}` to `{1,0}` for use in `RESUME .5` as a "skip or don't skip" mechanism. This works on lenient compilers but is fundamentally illegal (RESUME #0). The correct pattern uses `{1,2}` directly with the double-NEXT wrapper.

3. **Stack accounting inside subroutines is non-obvious.** The FORGET #1 in the Lemma 1 fix (Component 4) is required because the double-NEXT wrapper leaves R_outer on the stack in the done path. This is invisible in top-level loops (where GIVE UP ends the program regardless) and only manifests when a subroutine needs to RESUME back to its caller. We discovered this after the program hung on both SCHRODIE and C-INTERCAL — it was the last bug fixed.

## SCHRODIE Compiler Bugs Found

### Bug 1: RESUME #0 treated as no-op

**Location:** `cringe/confuse.cs` lines 567, 769
**Code:** `while (_popped < depth && _nextStack.Count > 0) { ... }`
**Effect:** When depth=0, loop body never executes, RESUME silently does nothing
**Correct behavior:** E621 (per C-INTERCAL)
**Impact:** All programs using `.5 ~ #1` + `RESUME .5` appear to work but are incorrect

### Bug 2: FORGET on empty stack silently ignored

**Location:** `cringe/confuse.cs` line 742
**Code:** `for (int _i = 0; _i < _n && _nextStack.Count > 0; _i++) _nextStack.Pop();`
**Effect:** When stack is empty, FORGET silently does nothing
**Status:** Needs investigation — beer.i may depend on this behavior at `(999) DO FORGET #1`

### Bug 3: COME FROM + syslib inside callable subroutine hangs

**Symptom:** Lemma 1 COME FROM fix works on C-INTERCAL but hangs on SCHRODIE
**Scope:** Only affects COME FROM loops that call syslib from inside a callable subroutine
**Status:** Not yet investigated; may be related to cross-assembly COME FROM interaction

### Bug 4: Fizzbuzz is broken

Our fizzbuzz implementation (`samples/fizzbuzz.i`) uses the `.5 ~ #1` pattern, producing RESUME #0. Confirmed crash on C-INTERCAL. Needs rewrite using beer.i double-NEXT + raw `.5` pattern.

## Final Results

| Program | SCHRODIE | C-INTERCAL (TIO) |
|---------|----------|-------------------|
| Lemma 1 broken (`lemma1.i`) | Hangs | E421 |
| Lemma 2 broken (`lemma2.i`) | Hangs | E421 |
| Lemma 1 COME FROM fix (`lemma1_comefrom.i`) | Hangs (Bug 3) | **V** |
| Lemma 2 COME FROM fix (`lemma2_comefrom.i`) | **I II III IV V** | **I II III IV V** |
| beer.i | Works | Works |
| fizzbuzz.i | Works (Bug 1) | E621 |

## Files

- `samples/lemma1.i` — Lemma 1 broken reproducer
- `samples/lemma2.i` — Lemma 2 broken reproducer
- `samples/lemma1_comefrom.i` — Lemma 1 COME FROM fix (confirmed on C-INTERCAL)
- `samples/lemma2_comefrom.i` — Lemma 2 COME FROM fix (confirmed on both compilers)
- Screenshots: `C:\Users\jasonw\Pictures\Screenshots\intercal_is_broken.png` (Lemma 1 E421), `intercal_is_broken_lemma2.png` (Lemma 2 E421)
