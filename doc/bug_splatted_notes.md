# Bug Report: Splatted NOTE Comments Disable Subsequent Code

## Summary

The compiler sometimes marks `DO NOTE` comments as "splatted" (unparseable nonsense statements). Splatted statements receive an abstain guard that starts disabled (`false`). In the goto-based execution model, the abstain guard's skip logic spans from the splatted statement to the next labeled statement, silently disabling all executable code in between.

This is a critical code generation bug that causes programs to silently skip blocks of executable statements, leading to hangs, wrong results, or mysterious E436/E200 errors.

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

## Impact

This bug has caused multiple issues during development:

1. **DIVIDE32 hang**: The masked subtract section's NOTE comment was splatted, disabling the variable setup for the subtract's `(1500)` calls. The stale values caused an infinite carry propagation loop inside the syslib.

2. **(1500) overflow errors**: Previously attributed to ABSTAIN not working across compiled assembly boundaries. The actual cause was likely splatted NOTEs disabling the code that set up correct (non-overflowing) input values.

3. **Various E436 (STASH imbalance) errors**: Splatted NOTEs between STASH and RETRIEVE could disable the RETRIEVE, causing unbalanced stash stacks.

4. **Silent wrong results**: Any computation whose variable setup is between a splatted NOTE and the next label will use stale values without any error message.

## Diagnosis

To check if a program is affected:

1. Compile with the `-b` flag to generate `~tmp.cs`
2. Search for `if(abstainMap[` in the generated code
3. Check if the guarded block contains executable statements (variable assignments, syslib calls) beyond the NOTE itself
4. Check if the corresponding `abstainMap` entry starts `false`

## Workaround

Remove or simplify `DO NOTE` comments that appear between executable statements. Specifically avoid NOTEs that contain:
- Parenthesized numbers (already known: parsed as labels)
- Hyphens, dots, colons, or other INTERCAL operator characters
- Anything that might cause the parser to attempt interpretation before recognizing the NOTE

Safest: place NOTEs only immediately after labeled statements, or remove them entirely from hot code paths.

## Suggested Fix

Several approaches, in order of invasiveness:

1. **Quick fix**: In `EmitAbstainMap`, don't assign abstain slots to `NonsenseStatement` instances that originated from NOTE comments. NOTEs are no-ops and don't need abstain guards.

2. **Better fix**: Don't assign abstain slots to any `NonsenseStatement`. Splatted statements already emit `Lib.Fail()` which will error at runtime if reached. The abstain guard is redundant and harmful.

3. **Best fix**: Fix the parser to never splat NOTE comments. The NOTE keyword should be recognized before any expression parsing is attempted, so the rest of the line is always treated as a comment regardless of its content.

## Affected Files

- `cringe/confuse.cs` lines 285-293: NonsenseStatement creation with `Splatted = true`
- `cringe/confuse.cs` lines 1077-1083: NonsenseStatement class
- `cringe/futile.cs` lines 519-567: EmitAbstainMap — abstain slot assignment
- `cringe/futile.cs` lines 820-822: Abstain guard generation
- `cringe/futile.cs` lines 932-938: Fall-through skip logic

## Reproduction

```intercal
DO .1 <- #42
DO NOTE THIS IS A HARMLESS COMMENT - OR IS IT
DO .2 <- .1
DO READ OUT .2
PLEASE GIVE UP
```

If the NOTE is splatted, `.2 <- .1` is skipped and `.2` is uninitialized, producing E200 instead of outputting 42. Compile with `-b` and check `~tmp.cs` for `if(abstainMap[` to confirm.
