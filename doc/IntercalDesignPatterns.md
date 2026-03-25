# INTERCAL Design Patterns

### Jason Whittington and Claude (Anthropic), 2026

A catalog of idiomatic patterns discovered during the development of INTERCAL-64. These patterns are the building blocks of nontrivial INTERCAL programs. Most were discovered empirically through the implementation of algorithms that INTERCAL was never designed to express.

---

## 1. Double-NEXT Trampoline

**Problem:** INTERCAL has no `if/else`. How do you conditionally execute one of two code paths?

**Solution:** Push two return addresses onto the NEXT stack, then RESUME to one based on a computed value. The unused address is cleaned up with FORGET.

```intercal
    DO (110) NEXT            NOTE push "true" return address
    DO (120) NEXT            NOTE push "false" return address
    DO RESUME .3             NOTE .3 = 1 → true, .3 = 2 → false

(110) DO FORGET #1           NOTE true path: discard false address
      DO NOTE ... true branch ...
      DO RESUME #1

(120) DO FORGET #1           NOTE false path: discard true address
      DO NOTE ... false branch ...
      DO RESUME #1
```

**Discovered in:** beer.i (99 Bottles of Beer). Became the standard conditional branching pattern for all subsequent programs.

**Caution:** Never use `.5 ~ #1` to compute the RESUME depth — it can produce RESUME #0 which is illegal.

---

## 2. COME FROM Loop

**Problem:** NEXT/RESUME loops corrupt the NEXT stack after 79 iterations. How do you iterate indefinitely?

**Solution:** COME FROM creates a back-edge that doesn't touch the NEXT stack. ABSTAIN the COME FROM to exit.

```intercal
(10) DO COME FROM (20)      NOTE back-edge
     DO NOTE ... loop body ...
     DO NOTE exit: DO ABSTAIN FROM (10)
(20) DO NOTE end of body — COME FROM fires here
```

**Key insight:** ABSTAINing the COME FROM *target* (20) does NOT prevent COME FROM from firing. You must ABSTAIN the COME FROM *statement itself* (10).

**Proven necessary by:** "COME FROM Considered Helpful" (TLA+ formal proof).

---

## 3. Mingle-Unary (Binary Logic)

**Problem:** The unary operators (&, V, ?) operate on adjacent bit pairs within a single value. How do you perform binary AND/OR/XOR on two separate values?

**Solution:** Mingle the two values to interleave their bits into adjacent pairs, then apply the unary operator. Select the result bits back out.

```intercal
DO :1 <- .1 $ .2          NOTE mingle: bits in adjacent pairs
DO .3 <- '&:1' ~ mask     NOTE unary AND on pairs, select result
```

**Key insight:** Mingle is the binary operator constructor. Unary operators are the evaluators. This is why the unary operators exist — they are designed to be applied to mingled values.

---

## 4. Select-as-Shift

**Problem:** INTERCAL has no shift operator. How do you right-shift a value?

**Solution:** Select with a contiguous high-bit mask. The selected bits are automatically right-justified.

```intercal
DO .2 <- .1 ~ #65280      NOTE select top 8 bits, right-justified
DO NOTE #65280 = 0xFF00 — this IS a right-shift by 8
```

**Discovered during:** Knight's Tour (Warnsdorff). Used for bitboard move evaluation.

---

## 5. ABSTAIN-as-Conditional

**Problem:** INTERCAL has no `if` statement. How do you conditionally execute a statement?

**Solution:** ABSTAIN the statement to disable it. REINSTATE to re-enable. The statement exists but does nothing when abstained.

```intercal
    DO ABSTAIN FROM (50)     NOTE disable the print
    DO NOTE ... check condition ...
    DO NOTE if true: DO REINSTATE (50)
(50) DO READ OUT .1          NOTE only fires if reinstated
```

**Combines with COME FROM loops** to implement conditional output per iteration — the equivalent of `for(...) { if(...) print(...); }`.

---

## 6. Single-NEXT + ABSTAIN/REINSTATE

**Problem:** The double-NEXT trampoline uses two NEXT calls per branch. Can we do it with one?

**Solution:** Push one return address, use a computed RESUME to take it or not, then ABSTAIN the RESUME statement so it can't fire on the next loop iteration. REINSTATE it at the top of each iteration.

```intercal
(10) DO COME FROM (20)
     DO REINSTATE (15)       NOTE re-enable for this iteration
     DO NOTE ... compute .1 = 1 or 2 ...
(15) DO (18) NEXT
     DO ABSTAIN FROM (15)
(18) DO RESUME .1
(20) DO NOTE end of loop
```

**Discovered during:** Knight's Tour (Warnsdorff). Cleaner than double-NEXT for COME FROM loops because it uses one fewer NEXT call per iteration.

---

## 7. Trampoline Placement

**Problem:** Trampoline subroutine bodies are reachable via NEXT, but sequential execution must not fall through to them.

**Solution:** Place trampoline bodies after GIVE UP. The compiler includes the code (it's reachable via NEXT) but sequential execution never reaches it.

```intercal
    DO NOTE ... main program ...
    PLEASE GIVE UP

(100) DO NOTE ... trampoline body ...
      DO RESUME #1

(200) DO NOTE ... another trampoline ...
      DO RESUME #1
```

---

## 8. Dispatch Subroutine

**Problem:** INTERCAL has no indirect variable access. How do you index into data by a computed value?

**Solution:** A series of labeled blocks, each accessing a specific variable. NEXT to the label computed from the index.

```intercal
DO NOTE dispatch: read element .10 from position .1
DO (1000) NEXT             NOTE .1 = 1..5 selects which block

(1001) DO .10 <- ,1 SUB #1
       DO RESUME #1
(1002) DO .10 <- ,1 SUB #2
       DO RESUME #1
...
```

**Used in:** Stable Marriage (Gale-Shapley) for preference array access.

---

## 9. STASH/RETRIEVE as Local Variables

**Problem:** INTERCAL variables are global. How do you preserve state across subroutine calls?

**Solution:** STASH the variables you need to preserve before calling, RETRIEVE them after.

```intercal
    DO STASH .1 + .2
    DO (100) NEXT            NOTE subroutine may clobber .1, .2
    DO RETRIEVE .1 + .2
```

**Caution:** syslib routines use .1/.2 for input and .3/.4 for output. Always STASH your values before calling syslib.

---

## 10. Linearized Array

**Problem:** INTERCAL has no 2D array indexing by computed row/column.

**Solution:** Flatten 2D data into a 1D array with computed index: `index = row * width + column`.

```intercal
DO NOTE 5x5 preference matrix in ,1 (25 elements)
DO NOTE element [row][col] = ,1 SUB (row * 5 + col)
DO .1 <- .10               NOTE row
DO .2 <- #5
DO (1040) NEXT              NOTE :3 = row * 5
DO .1 <- :3 ~ #65535
DO .2 <- .11               NOTE col
DO (1000) NEXT              NOTE .3 = row * 5 + col
DO .12 <- ,1 SUB .3
```

**Used in:** Stable Marriage (Gale-Shapley) for 5x5 preference tables.

---

## 11. Zero Test via Subtraction

**Problem:** INTERCAL has no comparison operators. How do you test if two values are equal?

**Solution:** Subtract them using the syslib. If the result is zero, they're equal. Use the result as a RESUME depth or dispatch index.

```intercal
DO .1 <- .10
DO .2 <- .11
DO (1010) NEXT              NOTE .3 = .10 - .11
DO NOTE .3 = 0 if equal, nonzero if different
```

---

## 12. Morton Code via Mingle

**Problem:** You need to compute a Morton code (Z-order curve index) from two coordinates.

**Solution:** Mingle IS Morton coding. The mingle operator interleaves bits, which is exactly the Morton code algorithm.

```intercal
DO :1 <- .1 $ .2            NOTE that's the whole algorithm
```

**Discovered during:** Hilbert Curve Geographic Indexing. INTERCAL's mingle operator is identical to the Morton code algorithm used in production geospatial databases.

---

## 13. State Machine via Table Lookup

**Problem:** You need a state machine with computed transitions.

**Solution:** Precompute transition tables in arrays. Each iteration: look up the transition, extract the new state, continue.

```intercal
DO NOTE state in .1, input in .2
DO .3 <- .1                 NOTE compute table index
DO .2 <- #4
DO (1040) NEXT              NOTE :3 = state * 4
DO .3 <- :3 ~ #65535
DO .2 <- .5                 NOTE + input
DO (1000) NEXT
DO .1 <- ,100 SUB .3        NOTE new state from table
```

**Used in:** Hilbert Curve (Morton-to-Hilbert state machine conversion).

---

## 14. FORGET-as-GOTO

**Problem:** You need a one-way transfer of control without accumulating NEXT stack entries.

**Solution:** NEXT to the target, then immediately FORGET #1. The return address is dropped and execution continues forward from the target.

```intercal
    DO (100) NEXT
    DO NOTE this line is never reached

(100) DO FORGET #1
      DO NOTE execution continues here, no return possible
```

---

## 15. COME FROM + ABSTAIN Conditional Output

**Problem:** Inside a loop, you want to conditionally output a value — but INTERCAL has no `if`.

**Solution:** ABSTAIN the READ OUT statement. On each iteration, check the condition and REINSTATE if it should fire. The COME FROM loop handles iteration; ABSTAIN/REINSTATE handles the conditional.

```intercal
(10) DO COME FROM (30)
     DO ABSTAIN FROM (25)    NOTE disable output by default
     DO NOTE ... check condition ...
     DO NOTE if true: DO REINSTATE (25)
(25) DO READ OUT .1          NOTE only fires if reinstated
(30) DO NOTE loop continues
```

**This is the general-purpose INTERCAL conditional loop pattern.** It handles what would be `for(...) { if(...) print(...); }` in a conventional language.

**Discovered during:** Hilbert Curve Geographic Indexing. Previously believed to be impossible in INTERCAL.

---

## Notes

These patterns compose. The Knight's Tour uses patterns 2, 4, 5, 6, 7, 9, and 15 simultaneously. The Hilbert Curve uses 2, 3, 5, 12, 13, 14, and 15. The Stable Marriage problem uses 1, 2, 5, 8, 9, and 10.

No prior catalog of INTERCAL design patterns is known to exist. The authors welcome additions from the community.
