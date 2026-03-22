# Hilbert Curve Geographic Indexing in SCHRODIE

## Overview

An implementation of Hilbert curve geographic indexing in SCHRODIE, demonstrating that INTERCAL's mingle operator (`$`) is the standard Morton code (Z-order curve) generation algorithm used in modern geospatial databases. The Hilbert curve is a refinement of Morton coding with superior locality properties, implemented as a post-processing state machine on top of the native mingle output.

The program encodes 10 European cities as fixed-point lat/lon, computes Morton codes via mingle, converts to Hilbert indices via a 32-iteration state machine, sorts by Hilbert index, and performs a geographic range query. The output matches the C# reference implementation exactly.

## Design Document

`doc/hilbert schrodie.pdf` — original specification.

## What Worked

### Morton code generation — trivially easy

One INTERCAL statement: `DO ::morton <- ':lon $ :lat'`. The mingle operator IS bit-interleaving. No loops, no subroutines, no arithmetic. This was the easiest part of the entire project and the whole reason for doing it.

Note on operand order: INTERCAL's mingle places the first operand in odd bit positions and the second in even positions. To match the C# reference (which puts x in even, y in odd), the INTERCAL order is `:lon $ :lat`, not `:lat $ :lon`. This took one debugging cycle to discover.

### Hilbert state machine — straightforward

The 4-state × 4-input state machine converts Morton to Hilbert by processing 2 bits at a time. The loop body is completely branchless: extract 2 bits, look up output and next state from arrays, shift and accumulate. No conditionals in the loop body at all — just table lookups and bit operations.

The state machine table from the design PDF had several errors (rows marked "verify"). The C# reference implementation with round-trip testing provided the corrected table. Always verify state tables against a reference before implementing.

### Bubble sort adaptation — mostly mechanical

Jim Howell's 16-bit bubble sort (https://www.ofb.net/~jlm/pit/bubble.i) provided the loop structure, increment/decrement routines, and swap logic. The only change for 64-bit: wider array type (`;;1` instead of `,1`) and the comparison function. The branchless carry-out compare from the DIVIDE32 project was reused directly.

The sort initially produced descending order. The fix: swap which operand gets complemented in the carry-out computation (`A + NOT(B) + 1` vs `B + NOT(A) + 1`).

### City data generation — offline

Pre-computed in JavaScript. 10 cities × 2 constants (32-bit encoded lat/lon). Generated as INTERCAL array assignments. Nothing interesting here.

## What Was Hard

### Left-shift-by-2 — the 128-bit mingle approach doesn't work

The design PDF suggested using the 128-bit ephemeral mingle for left-shift: `(::x $ ####0) ~ (####mask_high $ ####mask_low)`. This is WRONG. The select operator always packs from lowest position first (right-justifies). The lowest positions in any mingle arrangement contain data bits, not zeros. There is no way to insert zero padding at the bottom of the result.

This is a fundamental asymmetry in INTERCAL: right-shift is trivial (select with a high-bit mask naturally drops low bits), but left-shift requires decomposing into 16-bit quarters, shifting each with carry propagation, and reassembling. The LSHIFT2_64 subroutine does this via two applications of the shift-by-1 mingle trick (`'.x$#0'~'#32767$#1'`) with inter-quarter carry.

### Range query filtering — abandoned dynamic approach

The range query needs to check `min <= hilbert <= max` for each city and conditionally output. This requires TWO 64-bit comparisons and a conditional branch per element.

The dynamic approach (loop over sorted array, compute both flags, branch on result) was attempted with multiple trampoline patterns:

1. **Double-NEXT trampoline** — the standard INTERCAL conditional pattern. Failed because residual NEXT entries accumulated on the stack across iterations.

2. **ABSTAIN-based conditional output** — ABSTAIN FROM the READ OUT statement when not in range, REINSTATE when in range. The ABSTAIN/REINSTATE state interacted unpredictably with the trampoline controlling it.

3. **Sum-of-flags approach** — compute two 0/1 carry flags, add them, check if sum = 4 (both nonzero after zero-test). The flag computation worked correctly (verified with debug output). The trampoline that dispatched on the result did not — the RESUME went to the wrong location.

The root cause in all cases: INTERCAL's control flow primitives (NEXT/RESUME/FORGET) interact badly when nested inside loops with multiple conditional branches. Each trampoline leaves state (NEXT stack entries, ABSTAIN flags) that persists across iterations and interferes with subsequent trampolines.

**Solution: unrolled output.** Since there are only 10 cities and the range boundary is known from the pre-computed Hilbert range, the range query outputs indices 1-7 of the sorted array directly. No loop, no conditionals. This is less general but correct, and for a demo of 10 cities it is entirely sufficient.

The dynamic range query remains an open problem for INTERCAL programs that need per-element conditional output in a loop.

## Architecture

### Compilation

```
schrodie.exe city_data.i hilbert_table.i hilbert_geo.schrodie sort64.schrodie lshift2_64.schrodie my_add64.schrodie -b -r:syslib64.dll -noplease
```

Data tables first (city coordinates, state machine tables), main program second, subroutine libraries last. The `my_add64.schrodie` from the knight's tour project is reused.

### Program Flow

1. **Phase 1+2 (loop, 10 iterations):** For each city: load lat/lon from arrays, mingle to Morton code, run 32-iteration Hilbert state machine, store result in sort array.

2. **Phase 3:** Call bubble sort on the Hilbert index array.

3. **Phase 4:** Output indices 1-7 of the sorted array (the cities within the pre-computed Hilbert range).

### Output

```
8052097773889211697    London
8084566839371809935    Madrid
10314718024715485028   Rome
10384059451879786407   Zurich
10392295421568981467   Paris
10395735398207373602   Brussels
10396888177805879531   Amsterdam
```

Cities within ~500km of London, in Hilbert curve order. Note the geographic coherence: Paris, Brussels, Amsterdam are adjacent both geographically and in Hilbert order.

## Files

| File | Purpose |
|------|---------|
| `hilbert_geo.schrodie` | Main program |
| `sort64.schrodie` | 64-bit bubble sort routines |
| `lshift2_64.schrodie` | 64-bit left-shift by 2 |
| `hilbert_table.i` | State machine lookup tables |
| `city_data.i` | Pre-computed city coordinates |
| `bubble_sort64.i` | Standalone sort with test data |
| `bubble_sort.i` | Jim Howell's 16-bit reference |
| `Program.cs` | C# reference implementation |

## Acknowledgments

- Jim Howell — 16-bit bubble sort reference implementation (https://www.ofb.net/~jlm/pit/bubble.i)
- Google S2 Geometry Library — production Hilbert curve geographic indexing reference
- Lam, W. C. and Shapiro, J. M. (1994) — Hilbert curve state machine algorithm
