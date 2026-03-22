# Hilbert Curve Geographic Indexing in SCHRODIE

## Overview

An implementation of Hilbert curve geographic indexing in SCHRODIE, demonstrating that INTERCAL's mingle operator (`$`) is the standard Morton code (Z-order curve) generation algorithm used in modern geospatial databases. The Hilbert curve is a refinement of Morton coding with superior locality properties, implemented as a post-processing state machine on top of the native mingle output.

The program encodes latitude/longitude coordinate pairs as 64-bit Hilbert curve indices, sorts a dataset of cities by their Hilbert index, and performs a geographic range query demonstrating that spatially proximate points have contiguous Hilbert indices.

## Design Document

`doc/hilbert schrodie.pdf` — full specification including coordinate encoding, state machine definition, and implementation notes.

## Current Status

### Completed

- **Morton-to-Hilbert state machine** (`morton2hilbert.schrodie`): 32-iteration loop converting a 64-bit Morton code to a 64-bit Hilbert index. Verified against C# reference implementation for London, Paris, and Madrid — all match exactly. Uses branchless loop body: extract top 2 bits, table lookup, shift and accumulate.

- **Hilbert lookup tables** (`hilbert_table.i`): 4-state × 4-input state machine tables (`;10` = Hilbert output, `;11` = next state). 16 entries each. Verified with round-trip testing in C# reference.

- **Left-shift-by-2** (`lshift2_64.schrodie`): 64-bit left shift at label (9400). Uses 16-bit quarter decomposition with carry propagation — the 128-bit ephemeral mingle approach CANNOT do left-shift because select always right-justifies, preventing zero insertion at the bottom.

- **C# reference implementation** (`Program.cs`): Complete pipeline — coordinate encoding, Morton codes, Hilbert conversion with round-trip verification, bubble sort, Haversine distance calculation, and geographic range query with bounding box. All 10 cities pass round-trip verification.

### In Progress

- **64-bit bubble sort** (`bubble_sort64.i`): Adapted from Jim Howell's 16-bit bubble sort reference implementation. Sort structure (loops, swap, increment/decrement) reused directly. 64-bit comparison via branchless carry-out of complement-and-add needs debugging — currently gives incorrect comparison results for values that fit in the low 32 bits.

### Not Started

- City data file (pre-computed encoded coordinates)
- Main program integrating all phases
- Range query implementation
- Paper/writeup

## Architecture

### Morton Code Generation

One INTERCAL statement:

```intercal
DO ::morton <- :lat $ :lon
```

The mingle operator interleaves 32-bit latitude and longitude into a 64-bit Morton code. This is the same bit-interleaving operation used by geospatial databases (Google S2, PostGIS).

### Hilbert State Machine

The Morton code is converted to a Hilbert index by processing 2 bits at a time through a 4-state finite automaton:

1. Extract top 2 bits of Morton code (quadrant 0-3)
2. Look up `hilbert_output[state][quadrant]` and `next_state[state][quadrant]`
3. Shift Hilbert accumulator left by 2, OR in the output bits
4. Shift Morton code left by 2
5. Update state
6. Repeat 32 times

The state table (verified via round-trip in C# reference):

```
hilbert_out: 0,1,3,2,  0,3,1,2,  2,3,1,0,  2,1,3,0
next_state:  1,0,3,0,  0,2,1,1,  3,2,1,2,  2,3,0,3
```

Index = state × 4 + quadrant + 1 (1-based INTERCAL arrays).

### Bubble Sort

The sort routine is adapted from Jim Howell's INTERCAL bubble sort (`bubble.i`, available at https://www.ofb.net/~jlm/pit/bubble.i). The original sorts 16-bit arrays using mingle-based comparison. The 64-bit adaptation retains the identical loop structure, increment/decrement routines, and control flow; only the array type (`,1` → `;;1`), swap temporaries, and comparison logic change.

## Key Findings

### 128-bit Left-Shift Is Impossible

The design document suggested using the 128-bit ephemeral mingle for left-shift: `(::x $ ####0) ~ (####mask_high $ ####mask_low)`. This does NOT work. The select operator always packs from lowest position first (right-justifies). The lowest positions in any mingle arrangement contain data bits, not zeros. There is no way to insert zero padding at the bottom of the result.

Left-shift must use the 16-bit quarter decomposition (two applications of the shift-by-1 mingle trick `'.x$#0'~'#32767$#1'` with carry propagation between quarters).

This asymmetry is fundamental: right-shift removes low bits (select naturally handles this), while left-shift adds low bits (requires explicit zero insertion, which select cannot do).

### Design Document State Table Has Errors

The state machine table in `hilbert schrodie.pdf` differs from the verified C# reference implementation. Several rows were marked "NOTE: verify" in the original. The corrected table is in `hilbert_table.i`.

## Files

| File | Purpose |
|------|---------|
| `morton2hilbert.schrodie` | State machine: Morton → Hilbert |
| `hilbert_table.i` | Lookup tables for state machine |
| `lshift2_64.schrodie` | 64-bit left-shift by 2 |
| `bubble_sort64.i` | 64-bit bubble sort (WIP) |
| `bubble_sort.i` | Original 16-bit sort reference |
| `Program.cs` | C# reference implementation |
| `test_m2h.schrodie` | Test harness for state machine |
| `test_m2h_end.schrodie` | Output after state machine |

## Compilation

```
schrodie.exe hilbert_table.i test_m2h.schrodie morton2hilbert.schrodie test_m2h_end.schrodie lshift2_64.schrodie my_add64.schrodie -b -r:syslib64.dll -noplease
```

Data tables first, main/test program second, subroutines last. Uses `my_add64.schrodie` from the knight's tour (`samples/warnsdorff/`).

## Acknowledgments

- Jim Howell — 16-bit bubble sort reference implementation (`bubble.i`, https://www.ofb.net/~jlm/pit/bubble.i)
- Google S2 Geometry Library — production Hilbert curve geographic indexing reference
- Lam, W. C. and Shapiro, J. M. (1994) — Hilbert curve state machine algorithm
