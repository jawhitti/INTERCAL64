# Component Architecture Performance: Why Smaller Modules Are Faster

## Summary

An experiment to measure the overhead of cross-assembly component calls in the SCHRODIE compiler produced a counterintuitive result: **compiling the system library as a separate assembly is faster than compiling everything into one module.** The cross-assembly call overhead is more than offset by the cost of a larger goto dispatch table in the monolithic build.

## Background

The SCHRODIE compiler uses a goto-based state machine for INTERCAL execution. Each labeled statement becomes a case in a switch statement. NEXT calls push return labels onto a stack, and RESUME pops them and dispatches via the switch. Cross-assembly calls (e.g., user code calling syslib routines) use .NET method invocation through a `ComponentCall` wrapper, which creates a new `ExecutionFrame` and enters the target assembly's `Eval` method.

The question: how much overhead does the cross-assembly call boundary add, and would eliminating it by compiling everything into one module be faster?

## Experiment

The test case is DIVIDE32 (32-bit integer division), which performs approximately 10 `(1500)` calls, 3 `(1520)` calls, and 2 other syslib calls per loop iteration, for 32 iterations — roughly 480 cross-assembly calls total per division.

**Configuration A (cross-assembly):** User program and DIVIDE32 compiled together, referencing `syslib64.dll` as an external assembly via `-r:syslib64.dll`.

**Configuration B (monolithic):** User program, DIVIDE32, and the full syslib64 source compiled together as a single module, with no external assembly reference.

Test input: 1,000,000 ÷ 7 = 142,857 remainder 1.

## Results

| Configuration | Time | Correct |
|--------------|------|---------|
| Cross-assembly (syslib64.dll) | **7.0s** | Yes |
| Monolithic (single module) | **9.7s** | Yes |

The monolithic build is **39% slower** than the cross-assembly build.

## Analysis

The SCHRODIE compiler generates a single `Eval` method containing a goto-based state machine. Every labeled statement in the program becomes:

1. A `case` in the entry dispatch switch
2. A labeled goto target
3. An abstain guard check (if applicable)
4. The statement body
5. A variable cache refresh (for debugger)

The syslib64 source contains approximately 850 lines with dozens of labeled statements (1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1009, 1010, 1020, 1021, 1022, 1023, 1030, 1031, 1032, 1033, 1039, 1040, 1050, 1051, 1500, 1501, 1502, 1503, 1504, 1505, 1506, 1509, 1510, 1520, 1525, 1530, 1535, and many 64-bit ASCII-labeled routines). In the monolithic build, ALL of these labels are added to the same switch statement and state machine as the user code and DIVIDE32.

### Why bigger is slower

1. **Switch dispatch cost.** The .NET JIT compiles switch statements as either jump tables or binary search trees. A switch with 100+ cases is less efficient per dispatch than one with 10 cases, even when the same case is hit repeatedly. The CPU's branch predictor has more targets to track.

2. **Method size.** The monolithic `Eval` method is substantially larger (the generated C# for the monolithic build is approximately 3x the size of the user-only build). Larger methods are harder for the JIT to optimize, may not inline, and have worse instruction cache behavior.

3. **Abstain map overhead.** The monolithic build has a larger `abstainMap` array (one entry per abstainable statement across all source files). Every statement that might be abstained pays the cost of an array bounds check and branch.

4. **Variable cache refresh.** After every statement, the debugger variable cache refreshes ALL variables used anywhere in the program. In the monolithic build, this includes all syslib internal variables, adding unnecessary `GetVarValue` calls.

### What the component boundary provides for free

In the cross-assembly model, each `DO_1500` call enters a fresh, small `Eval` method with:
- A compact switch (only syslib labels)
- A small abstainMap (only syslib abstainable statements)
- A tight variable cache (only syslib variables)
- Isolated `_nextStack` management

The cost of creating a `ComponentCall`, invoking a .NET method, and constructing an `ExecutionFrame` is apparently less than the cost of dispatching through a bloated monolithic state machine.

## Correctness: Shared `_nextStack` Corrupts RESUME Dispatch

Beyond performance, monolithic compilation introduces a **correctness bug** that has gone unnoticed in the INTERCAL syslib design for 50 years.

### The problem

Each compiled assembly gets its own `_nextStack` (declared in `futile.cs:565`). In cross-assembly mode, this provides natural isolation: the syslib's internal NEXT/RESUME/FORGET bookkeeping cannot interfere with the calling program's stack.

In monolithic mode, everything shares one `_nextStack`. The syslib routines rely heavily on multi-level RESUME to return from nested subroutine calls:

- `(1007) PLEASE RESUME #2` — pops 2 entries to return from `(1000)` add
- `(1500)` chain uses `RESUME #3` — pops 3 entries to return from `(1500)` 32-bit add
- `(1001) DO RESUME .5` — conditional pop based on comparison result

When these RESUME operations share a stack with the calling program, they can pop past the syslib's own entries and consume return labels that belong to the main program's loops and control flow.

### Trace of the failure

1. Main program: `DO (1000) NEXT` → pushes return label 1 onto `_nextStack`
2. Inside `(1000)`: internal calls push/pop their own labels (8, 9, 10, ...)
3. If any FORGET/RESUME accounting is off by even one entry — or if a code path through `(1001) DO RESUME .5` takes an unexpected branch — the `RESUME #2` at `(1007)` reaches past the syslib's entries
4. It pops the main program's return label 1 and dispatches to `_ret_1`, which is now some arbitrary point in the merged 203-case switch table
5. Execution lands in unrelated code, eventually hitting RETRIEVE without a matching STASH → **E436**

### Why this was invisible for 50 years

The original C-INTERCAL syslib was designed for a single-process, single-stack model where the caller's NEXT entries and the library's NEXT entries *were* supposed to share a stack. The RESUME depths in the syslib (e.g., `RESUME #2` to pop the library's internal frame plus the caller's NEXT) were carefully balanced for that model.

The SCHRODIE compiler's cross-assembly boundary accidentally fixed this: each component gets its own `_nextStack`, so the syslib's `RESUME #2` only pops entries from the syslib's own stack. The depth arithmetic that was designed for a shared stack becomes harmless — the extra pop just hits the bottom of an isolated stack and exits cleanly via `frame.Call.NextStackDepth`.

Simple programs (like `test_add.i`) may work monolithically because their shallow, sequential call patterns happen to keep the stack balanced. Complex programs with nested loops and repeated syslib calls (like Hilbert geo) accumulate stack drift until a RESUME eats a wrong label.

### Why the crash "doesn't make sense"

The 203-case RESUME dispatch switch means a corrupted pop can jump to any `_ret_N` label in the entire merged program. The crash appears in a completely unrelated part of the code — a RETRIEVE inside a syslib routine that was never called, or a variable assignment in the middle of an unrelated computation. The symptom has no apparent connection to the cause.

## Implications

1. **The component architecture is the right design.** It was originally motivated by isolation (preventing ABSTAIN state leakage between modules), but it also provides both a performance benefit and a critical correctness guarantee through isolated `_nextStack` instances.

2. **Cross-assembly compilation is a correctness requirement, not just an optimization.** The `-r:syslib64.dll` flag is not optional for programs that call syslib routines. Monolithic compilation may produce correct results for simple programs but will corrupt execution for complex ones.

3. **Future optimization should focus on reducing call count, not call overhead.** The bottleneck in DIVIDE32 is not the cost of each syslib call (which is reasonable) but the NUMBER of calls (480 per division). Implementing compound operations (e.g., a native 32-bit compare-and-subtract) would be more impactful than optimizing the call mechanism.

4. **Module splitting is a valid optimization strategy.** Programs that make heavy use of only a few syslib routines could benefit from linking against a minimal syslib containing only the needed routines, further reducing the dispatch table size.

## Reproduction

```bash
# Cross-assembly (faster)
schrodie.exe test.schrodie divide32.schrodie -b -r:syslib64.dll -noplease
time dotnet test.dll

# Monolithic (slower)
schrodie.exe test.schrodie divide32.schrodie syslib64.schrodie -b -noplease
time dotnet test.dll
```

Note: file order matters. The test program must come first (INTERCAL executes top-to-bottom). Syslib source must come last so its labeled statements are reached only via NEXT calls, never by sequential execution.
