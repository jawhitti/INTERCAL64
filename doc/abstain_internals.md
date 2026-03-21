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

## (1000) Overflow and RESUME #0

The 16-bit add at `(1000)` suppresses overflow by ABSTAINing FROM `(1005)` and `(1999)`. This works for values whose sum fits in 16 bits. However, when the carry propagation reaches bit 15 (i.e., the sum overflows 16 bits), the overflow check computes `.5 = 0` and the syslib executes `RESUME .5` which becomes `RESUME #0`.

`RESUME #0` is either a no-op or falls through to the next sequential statement in the syslib — which is `(1010)`, the 16-bit subtract entry point. This causes execution to enter completely unrelated code, typically resulting in an infinite loop or hang.

**This means `(1000)` cannot safely add values whose sum exceeds 65535.** Despite the ABSTAIN mechanism, the overflow path produces `RESUME #0` which has undefined behavior in the syslib's control flow.

### Affected patterns

Any code that calls `(1000)` with inputs that sum to >= 65536 will hang. Common cases:

- `NOT(0) + 1 = 65535 + 1` — computing two's complement of 0
- `65535 + N` for any N > 0
- Any 16-bit add where both operands are large

### Workaround

Use `(1500)` (32-bit add) with 16-bit values widened to 32-bit. The sum of two 16-bit values never exceeds 131070, which fits in 32 bits without overflow. Extract the 16-bit result from the low half and the carry from bit 16.

## Key Files

- `cringe/confuse.cs` lines 902-993: AbstainStatement and ReinstateStatement emit code
- `cringe/futile.cs` lines 519-567: EmitAbstainMap generates the bool array
- `cringe/futile.cs` lines 820-822: Abstain guard generation (`if(abstainMap[slot])`)
- `cringe/futile.cs` line 565: `_nextStack` declared as field
