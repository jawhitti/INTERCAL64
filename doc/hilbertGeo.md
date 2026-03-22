# Hilbert Curve Geographic Indexing in SCHRODIE

## Overview

An implementation of Hilbert curve geographic indexing in SCHRODIE, demonstrating that INTERCAL's mingle operator (`$`) is the standard Morton code (Z-order curve) generation algorithm used in modern geospatial databases. The Hilbert curve is a refinement of Morton coding with superior locality properties, implemented as a post-processing state machine on top of the native mingle output.

The program encodes 10 European cities as fixed-point lat/lon, computes Morton codes via mingle, converts to Hilbert indices via a 32-iteration state machine, sorts by Hilbert index, and performs a geographic range query that dynamically identifies cities within 500km of London.

## Status: Complete

All phases working. Output matches C# reference implementation exactly.

## What Worked

### Morton code generation — one statement

`DO ::morton <- ':lon $ :lat'`. The mingle operator IS bit-interleaving. No loops, no subroutines, no arithmetic.

Note: mingle order is `:lon $ :lat` (not `:lat $ :lon`) to match the C# reference convention where x goes to even positions and y to odd.

### Hilbert state machine — branchless loop body

The 4-state × 4-input state machine converts Morton to Hilbert by processing 2 bits at a time. Completely branchless loop body: extract 2 bits, table lookup, shift, accumulate. The only branch is the 32-iteration counter check.

### Bubble sort adaptation — mechanical

Jim Howell's 16-bit bubble sort (https://www.ofb.net/~jlm/pit/bubble.i) provided the loop structure. Only changes: wider array type and 64-bit comparison via branchless carry-out.

### Range query — unrolled with ABSTAIN-based output

Each of 10 sorted cities individually compared against the Hilbert range bounds using the branchless carry-out comparison. Two flags computed per city (>= min, <= max), combined via sum check. Cities in range have their `READ OUT` statements REINSTATED; out-of-range stay ABSTAINED.

This approach works because each city has its own independent trampoline — no loop, no accumulated NEXT stack state, no interference between iterations.

## What Was Hard

### Left-shift-by-2 — 128-bit mingle doesn't work

The design PDF suggested using 128-bit ephemeral mingle for left-shift. This is impossible: select always right-justifies, so you cannot insert zero padding at the bottom. Left-shift requires 16-bit quarter decomposition with carry propagation.

### Dynamic range query in a loop — abandoned

The range query was originally a loop: for each city, check range, conditionally output. This failed because INTERCAL's NEXT/RESUME trampolines leave residual state on the stack across loop iterations. Every approach was tried:

1. Double-NEXT trampoline — residual NEXT entries accumulated
2. ABSTAIN-based conditional output — state leaked across iterations
3. Sum-of-flags with trampoline dispatch — correct flags, wrong RESUME destination

The root cause: INTERCAL cannot do conditional output inside a loop because the trampoline mechanism (the only way to branch) corrupts the NEXT stack when used repeatedly.

**Solution: unroll the loop.** 10 copies of the 55-line compare+branch block. 578 lines of generated code for what would be `if (x >= min && x <= max) print(x)` in any other language. This is what conditional logic looks like without if/else.

### Turing tape text output — encoding chain

INTERCAL's character output uses Turing tape encoding where each character depends on the previous output state (`LastOut`). Outputting multiple independent strings requires computing reset values to chain the encoding correctly. Ultimately we output city indices (numbers) instead, with a printed answer key.

## Output

```
1 2 3 4 5 6 7
```

Answer key (sorted by Hilbert curve index):
```
1=London  2=Madrid  3=Rome     4=Zurich   5=Paris
6=Brussels 7=Amsterdam 8=Berlin  9=Prague  10=Vienna
```

Cities 1-7 are within the 500km Hilbert range of London. Cities 8-10 (Berlin, Prague, Vienna) are correctly excluded. Note: Madrid and Rome are included as false positives — they are within the Hilbert range but outside the geographic bounding box. This is expected behavior for single-range Hilbert queries.

## Architecture

### Compilation

```
schrodie.exe city_data.i hilbert_table.i hilbert_geo.schrodie sort64.schrodie lshift2_64.schrodie my_add64.schrodie -b -r:syslib64.dll -noplease
```

### Program Flow

1. **Phase 1+2** (loop, 10 iterations): For each city, mingle lat/lon → Morton code, then 32-iteration Hilbert state machine → Hilbert index.
2. **Phase 3**: Bubble sort 10 cities by Hilbert index.
3. **Phase 4** (unrolled, 10 blocks): For each sorted city, compare against range bounds, REINSTATE output if in range.
4. **Output**: 10 labeled `READ OUT` statements, 7 enabled (in range), 3 abstained (out of range).

### Size

- Main program: 661 lines (578 generated for range query)
- Supporting files: ~300 lines (sort, left-shift, state machine, tables, city data)
- Total: ~960 lines of INTERCAL

## Files

| File | Lines | Purpose |
|------|-------|---------|
| `hilbert_geo.schrodie` | 661 | Main program (all phases) |
| `sort64.schrodie` | 120 | 64-bit bubble sort routines |
| `lshift2_64.schrodie` | 57 | 64-bit left-shift by 2 |
| `hilbert_table.i` | 36 | State machine lookup tables |
| `city_data.i` | 35 | Pre-computed city coordinates |
| `city_names.i` | — | Turing tape encoded names (unused) |
| `bubble_sort64.i` | — | Standalone sort with test data |
| `bubble_sort.i` | — | Jim Howell's 16-bit reference |
| `Program.cs` | 351 | C# reference implementation |

## Key Findings

1. **Mingle is Morton coding.** One INTERCAL statement does what geospatial databases implement with dedicated bit-interleaving functions.

2. **State machines are a natural fit for INTERCAL.** Table lookup + bit extraction + shift = branchless loop body. No conditionals needed in the inner loop.

3. **Conditional output in loops is impossible in INTERCAL.** The NEXT/RESUME trampoline is the only branching mechanism. It uses a shared stack that corrupts across loop iterations. The only reliable pattern is unrolled code with ABSTAIN-based output control.

4. **128-bit left-shift is impossible via mingle+select.** Select always right-justifies. This is a fundamental asymmetry: right-shift works (select drops low bits), left-shift cannot (select cannot insert zeros at the bottom).

5. **Unrolled code generation is a valid INTERCAL pattern.** When loops can't do conditional work, generate N copies of the loop body at compile time. The compiler handles hundreds of labels without issue. The resulting program is large but correct.

## Acknowledgments

- Jim Howell — 16-bit bubble sort reference implementation (https://www.ofb.net/~jlm/pit/bubble.i)
- Google S2 Geometry Library — production Hilbert curve geographic indexing reference
- Lam, W. C. and Shapiro, J. M. (1994) — Hilbert curve state machine algorithm
