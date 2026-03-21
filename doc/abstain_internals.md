# ABSTAIN/REINSTATE Internals

## How It Works

The compiler generates a `bool[] abstainMap` as a **local variable** inside the `Eval` method of each compiled assembly. Each abstainable statement gets a slot index. The array is initialized at method entry with compile-time defaults (statements that start abstained get `false`, others get `true`).

- `ABSTAIN FROM (label)` emits: `abstainMap[slot] = false;`
- `REINSTATE (label)` emits: `abstainMap[slot] = true;`
- Each guarded statement emits: `if(abstainMap[slot]) { ... }`

## Scope

ABSTAIN/REINSTATE are **local to the compilation unit**. They bind to a target at compile time. The `abstainMap` lives inside the compiled method, so:

- Within a single `.schrodie` file compiled to an assembly, ABSTAIN/REINSTATE work correctly across all labels in that file.
- Cross-assembly ABSTAIN (e.g., user code trying to ABSTAIN a syslib label) is not supported. The compiler resolves labels at compile time and can only target statements in the local program.

## Syslib Behavior

When user code calls a syslib entry point like `DO (1500) NEXT`, the runtime calls `syslib64.DO_1500(call)` which enters the syslib's `Eval` method. A **fresh** `abstainMap` is created for each `Eval` call, initialized from compile-time defaults.

This means syslib routines that use ABSTAIN internally (like (1500) which ABSTAINs FROM (1502)/(1506)/(1999)) work correctly: each call starts clean, ABSTAINs what it needs, does the work, REINSTATEs at exit, and returns.

## NEXT Stack

The `_nextStack` is a **field** (not local to `Eval`), so it persists across calls to the same syslib instance. However, syslib routines are designed to clean up their own NEXT stack entries (via RESUME #N matching their internal NEXT depth). Testing confirms that sequential syslib calls do not leak stack entries.

## Gerund ABSTAIN

No syslib routines use gerund-based ABSTAIN (`ABSTAIN FROM CALCULATING`). All ABSTAINs target specific labels. Gerund ABSTAIN would disable ALL statements of that type across the entire program, which would be destructive. It's syntactically valid but not practically useful.

## Previously Suspected Bug: (1500) Overflow

It was previously suspected that `(1500)` (32-bit add) failed to suppress overflow errors across compiled assembly boundaries. Investigation shows the `abstainMap` mechanism works correctly within the syslib — each call creates a fresh map and the ABSTAIN FROM (1999) at (1500)'s entry correctly prevents overflow errors.

The actual cause of overflow errors when calling `(1500)` with values that exceed 32 bits needs further investigation. It may be related to the internal carry propagation logic rather than the ABSTAIN mechanism itself.

## Key Files

- `cringe/confuse.cs` lines 902-993: AbstainStatement and ReinstateStatement emit code
- `cringe/futile.cs` lines 519-567: EmitAbstainMap generates the bool array
- `cringe/futile.cs` lines 820-822: Abstain guard generation (`if(abstainMap[slot])`)
- `cringe/futile.cs` line 565: `_nextStack` declared as field
