# Investigation: Splatted NOTE Comments and Abstain Guards

## Summary

The compiler marks `DO NOTE` comments as "splatted" (unparseable nonsense statements). Splatted statements receive an abstain guard that starts disabled (`false`). This was initially suspected as a critical bug causing programs to silently skip executable code.

**Finding: This is NOT a bug.** The abstain guard only wraps the splatted statement's own body and its debugger cache reload. The guard closes BEFORE the next statement begins. Each statement gets its own independent cache reload in its own epilog. Furthermore, statement `Emit()` code uses `frame.ExecutionContext` directly, not the cached locals — the cached locals exist only for the debugger watch panel and have no effect on execution.

## Root Cause

### 1. NOTE Comment Parsing

When the parser encounters a statement it cannot parse, it falls through to a catch block (`confuse.cs` line 289-290) that creates a `NonsenseStatement` with `Splatted = true`. Some `DO NOTE` comments trigger this path — particularly those containing characters or patterns that the parser attempts to interpret before recognizing the NOTE keyword.

### 2. Abstain Guard Generation

During code generation, splatted statements receive an abstain slot in the `abstainMap` array (`futile.cs` lines 542-556). The initial value for splatted statements is `false` (disabled):

```csharp
ctx.EmitRaw((s.bEnabled && !s.Splatted) ? "true" : "false");
```

### 3. Fall-Through Skip Logic

In the goto-based execution model, when a statement's abstain guard evaluates to `false`, execution skips to the next labeled statement (`futile.cs` lines 932-938):

```csharp
// When a statement is abstained, skip everything until the next
// labeled statement to prevent fall-through in the goto model.
if (s.AbstainSlot >= 0 && s as Statement.ComeFromStatement == null)
{
    c.EmitRaw("_abstain_skip_" + s.StatementNumber + ": ;\n");
}
```

This means: if a splatted NOTE appears between two labels, ALL statements between the NOTE and the next label are wrapped in the `if(abstainMap[slot])` guard. Since the slot starts `false`, all those statements are **permanently skipped**.

### 4. Generated Code Example

Input:
```intercal
    DO (1500) NEXT
    DO NOTE SUBTRACT LOW - USE .3 FROM MASK AS-IS
    DO :1 <- :10 ~ #65535
    DO :2 <- .1
    DO (1500) NEXT
```

Generated C#:
```csharp
// (1500) NEXT executes normally

// NOTE — splatted, gets abstain guard
if(abstainMap[0])    // abstainMap[0] = false at init!
{
    Lib.Fail("26 * DO NOTE SUBTRACT LOW ...");

    // ALL subsequent code until next label is INSIDE this guard:
    colon_1 = frame.ExecutionContext.GetVarValue(":1") ?? 0;
    colon_10 = frame.ExecutionContext.GetVarValue(":10") ?? 0;
    // ... variable setup for the next (1500) call ...
}
// The (1500) call runs with STALE variable values
```

The `:1` and `:2` assignments are inside the disabled guard. The subsequent `(1500)` call executes with whatever stale values `:1` and `:2` had from the previous call, causing incorrect behavior (hangs, wrong results, or crashes).

## Why It's Not a Bug

The code generation structure for each statement is:

1. **Prolog** (`EmitStatementProlog`): Opens `if(abstainMap[slot]) {` guard
2. **Body** (`s.Emit()`): The actual statement code
3. **Epilog** (`EmitStatementEpilog`): Debug cache reload, then closes `}` guard, then skip label

The guard wraps only the statement body and its own cache reload. The next statement starts fresh with its own prolog, guard, body, and epilog. Skipping a splatted NOTE skips only the NOTE itself (a `Lib.Fail()` call that would error if reached anyway) and its cache reload (debugger-only, not used by execution).

Statement code uses `frame.ExecutionContext[":1"]` directly for reads and writes, NOT the cached locals (`colon_1`, `dot_1`). The cached locals exist solely for the VS Code debugger's watch panel.

## Remaining Known Issues

The following remain true and ARE problems:

1. **Labels in NOTE comments**: `DO NOTE USES (1520) MINGLE` causes the compiler to parse `(1520)` as a label definition, shadowing syslib routines. This is a tokenizer bug, not an ABSTAIN bug. Avoid parenthesized numbers in NOTEs.

2. **All NOTEs are splatted**: Every `DO NOTE` comment is marked as a `NonsenseStatement` with `Splatted = true`. This is wasteful (generates unnecessary abstain slots and guards) but harmless to execution.

3. **(1000) overflow causes RESUME #0 hang**: When `(1000)` (16-bit wrapping add) receives inputs whose sum exceeds 65535, the carry propagation reaches bit 15. The overflow check computes `.5 = 0` and `RESUME .5` becomes `RESUME #0`, which falls through to `(1010)` (16-bit subtract), causing an infinite loop. This is NOT an ABSTAIN bug — ABSTAIN correctly suppresses the overflow error, but the code path after suppression produces an invalid RESUME value. See `doc/abstain_internals.md` for details and workarounds.

## Key Files

- `cringe/confuse.cs` lines 285-293: NonsenseStatement creation
- `cringe/futile.cs` lines 519-567: EmitAbstainMap
- `cringe/futile.cs` lines 820-822, 878-881: Abstain guard open/close
- `cringe/futile.cs` lines 852-855: Debug cache reload (inside guard, debugger-only)
- `cringe/futile.cs` lines 932-938: Skip label placement
