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

### Dynamic range query — solved via COME FROM

The range query checks `min <= hilbert <= max` for each city and conditionally outputs. This requires two 64-bit comparisons and a conditional branch per element inside a loop.

**What failed: NEXT/RESUME loops.** Three trampoline patterns were tried, all corrupted the NEXT stack across iterations:

1. Double-NEXT trampoline — residual entries accumulated
2. ABSTAIN-based conditional output — state leaked between iterations
3. Sum-of-flags with trampoline dispatch — correct flags, wrong RESUME destination

An unrolled version (10 copies × 55 lines = 578 lines) worked but was inelegant.

**What worked: COME FROM loop.** The breakthrough came from `beer.i` (99 Bottles of Beer), which uses `DO COME FROM (label)` for its main loop. COME FROM does not touch the NEXT stack — it's a trapdoor that fires after the target label executes.

```intercal
DO COME FROM (8999)           ← loop top: control returns here after (8999)
... loop body ...
(8999) DO FORGET #1           ← loop bottom: COME FROM fires, back to top
```

The FORGET cleans up trampoline residuals. COME FROM loops back without pushing anything. Each iteration starts with a clean NEXT stack.

**Conditional output pattern:** ABSTAIN FROM the `READ OUT` at the top of each iteration. A trampoline checks the range flags — the "in range" path REINSTATEs the READ OUT; the "not in range" path leaves it abstained. The trampoline body is placed after GIVE UP to prevent fall-through interference. Both paths converge at the READ OUT (which either fires or is skipped) then fall through to the counter.

**Exit:** Counter trampoline: "not done" falls through to (8999), COME FROM fires. "Done" falls through to GIVE UP.

**Result: 178 lines** — down from 661 unrolled. A dynamic loop with conditional output per element. The earlier conclusion that "conditional output in loops is impossible in INTERCAL" was wrong — it's impossible in NEXT/RESUME loops but works perfectly in COME FROM loops.

This technique also enabled DIVIDE32 as a callable subroutine — something that failed three times over two days using NEXT/RESUME loops. With COME FROM: one compiler fix (labeled COME FROM abstain guard) and one insight (no FORGET at the COME FROM target) solved it immediately. Four consecutive divisions, all returning correctly to the caller.

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

- Main program: 178 lines (COME FROM loop with dynamic range query)
- Supporting files: ~300 lines (sort, left-shift, state machine, tables, city data)
- Total: ~480 lines of INTERCAL

## Files

| File | Lines | Purpose |
|------|-------|---------|
| `hilbert_geo.schrodie` | 178 | Main program (all phases, COME FROM loop) |
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

3. **Conditional output in NEXT/RESUME loops is impossible.** The NEXT/RESUME trampoline uses a shared stack that corrupts across loop iterations.

4. **COME FROM loops solve the NEXT stack problem.** COME FROM does not touch the NEXT stack. Trampolines can fire inside COME FROM loops without accumulating residual state. ABSTAIN-based conditional output (ABSTAIN the READ OUT, REINSTATE if condition met) works cleanly inside COME FROM loops. This is the most important INTERCAL programming technique discovered during this project.

5. **Trampoline placement matters.** Trampoline bodies must be placed where sequential fall-through cannot reach them. Placing them after GIVE UP works — the compiler includes the code (reachable via NEXT) but sequential execution never reaches it.

6. **128-bit left-shift is impossible via mingle+select.** Select always right-justifies — cannot insert zeros at the bottom.

7. **COME FROM + ABSTAIN is the general-purpose INTERCAL conditional loop pattern.** COME FROM for the loop, trampolines for branching, ABSTAIN/REINSTATE for conditional execution. This combination handles what would be `for(...) { if(...) print(...); }` in a conventional language.

8. **COME FROM loops enable subroutines with loops.** DIVIDE32 failed three times over two days as a callable subroutine using NEXT/RESUME loops. With COME FROM it works on the first try. The key: do NOT FORGET at the COME FROM target — the trampoline's RESUME #2 already cleans up its own entries. The "done" path's FORGET #1 pops only the remaining trampoline entry, preserving the caller's return address.

9. **Labeled COME FROM required a compiler fix.** `(label) DO COME FROM (target)` generated bad C# because the abstain guard wrapped subsequent code. Fixed by excluding COME FROM from the guard at `futile.cs` lines 820 and 881. The abstain check for COME FROM already happens at the trapdoor site (line 916).

## Acknowledgments

- Jim Howell — 16-bit bubble sort reference implementation (https://www.ofb.net/~jlm/pit/bubble.i)
- Google S2 Geometry Library — production Hilbert curve geographic indexing reference
- Lam, W. C. and Shapiro, J. M. (1994) — Hilbert curve state machine algorithm
