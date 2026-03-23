# Stable Marriage in INTERCAL: Design Document

## Overview

Implement the Gale-Shapley algorithm for the Stable Marriage Problem with n=5, using COME FROM loops, the beer.i double-NEXT trampoline pattern, and syslib arithmetic. This is a demonstration that COME FROM enables non-trivial algorithms in INTERCAL — the nested loop structure required by Gale-Shapley is impossible in INTERCAL-72 without COME FROM (per Lemmas 1 and 2 of "COME FROM Considered Necessary").

## Algorithm

```
while any man is unmatched:
    for M = 1 to 5:
        if M is unmatched:
            W = M's preference list[M's proposal index]
            advance M's proposal index
            if W is free:
                match M and W
            else:
                C = W's current partner
                if W prefers M over C:
                    unmatch C
                    match M and W
                // else: M stays unmatched
    // end inner loop
// end outer loop
print results
```

## Data Structures

### Arrays

| Variable | Size | Contents |
|----------|------|----------|
| `,1` | 5 | Man 1's preference list (ranked). `,1 SUB 1` = first choice woman, etc. |
| `,2` | 5 | Man 2's preference list |
| `,3` | 5 | Man 3's preference list |
| `,4` | 5 | Man 4's preference list |
| `,5` | 5 | Man 5's preference list |
| `,6` | 5 | Woman 1's ranking of each man. `,6 SUB M` = her ranking of man M (1=best, 5=worst) |
| `,7` | 5 | Woman 2's rankings |
| `,8` | 5 | Woman 3's rankings |
| `,9` | 5 | Woman 4's rankings |
| `,10` | 5 | Woman 5's rankings |

**Why rankings for women, not preference lists?** The algorithm requires "does W prefer M over C?" frequently. With ranking arrays, this is a single comparison: `,W+5 SUB M` < `,W+5 SUB C`. With preference lists, it would require a linear search.

### Scalar Variables

| Variable | Purpose |
|----------|---------|
| `.11` | Man 1's proposal index (1-5, which woman to propose to next) |
| `.12` | Man 2's proposal index |
| `.13` | Man 3's proposal index |
| `.14` | Man 4's proposal index |
| `.15` | Man 5's proposal index |
| `.16` | Woman 1's current partner (0 = free, 1-5 = matched man) |
| `.17` | Woman 2's current partner |
| `.18` | Woman 3's current partner |
| `.19` | Woman 4's current partner |
| `.20` | Woman 5's current partner |
| `.21` | Inner loop counter (current man M, 1-5) |
| `.22` | Unmatched men count (algorithm terminates when 0) |
| `.23` | Temp: current woman W being proposed to |
| `.24` | Temp: current partner C of woman W |
| `.25` | Temp: proposal index for current man |

### Variable Access Problem

INTERCAL has no indirect variable access. We cannot write `,(.21) SUB .25` to index into man M's preference list where M is a runtime value in `.21`.

**Solutions:**

1. **Dispatch subroutine**: A subroutine `(9400)` that takes M in `.1` and returns the appropriate value via a 5-way branch. For example, "get man M's next woman" branches on M to read from `,1`, `,2`, `,3`, `,4`, or `,5`.

2. **Copy to a working array**: Less practical, lots of STASH/RETRIEVE.

3. **Unrolled dispatch via ABSTAIN/REINSTATE**: ABSTAIN all 5 options, REINSTATE only the one matching M.

Option 1 (dispatch subroutine) is the cleanest. We need dispatch subroutines for:
- **Get man M's next woman** — input: `.1`=M, output: `.3`=woman W
- **Set woman W's partner** — input: `.1`=W, `.2`=partner
- **Get woman W's partner** — input: `.1`=W, output: `.3`=partner
- **Get woman W's ranking of man M** — input: `.1`=W, `.2`=M, output: `.3`=ranking
- **Get/set man M's proposal index** — input: `.1`=M, output/input: `.3`=index

These dispatch subroutines are 5-way trampolines. Each branch is a labeled statement that can be selectively abstained/reinstated, or we use nested RESUME .5 comparisons.

Actually, the simplest dispatch: subtract and use a cascade of comparisons. Or better: use the value as an array index into a dispatch array.

**Simplest approach for dispatch:** Since n=5 is fixed, use 5 consecutive labeled statements with ABSTAIN/REINSTATE:

```intercal
DO NOTE GET MAN M'S (.1) PREFERENCE AT INDEX .2, RESULT IN .3
(9400) DO ABSTAIN FROM (9401)
       DO ABSTAIN FROM (9402)
       DO ABSTAIN FROM (9403)
       DO ABSTAIN FROM (9404)
       DO ABSTAIN FROM (9405)
       DO NOTE reinstate the one matching .1
       ... dispatch on .1 ...
(9401) DO .3 <- ,1 SUB .2
(9402) DO .3 <- ,2 SUB .2
(9403) DO .3 <- ,3 SUB .2
(9404) DO .3 <- ,4 SUB .2
(9405) DO .3 <- ,5 SUB .2
       DO RESUME #1
```

The problem: we still need to conditionally REINSTATE one of (9401)-(9405) based on the runtime value of `.1`. That requires a trampoline cascade — 5 comparisons.

**Alternative: use a single array of size 25.** Pack all 5 men's preferences into one array `,1` of size 25. Man M's preference at index I is `,1 SUB ((M-1)*5 + I)`. The index computation uses syslib multiply or repeated add.

This eliminates the dispatch problem entirely. One array, one SUB expression (after computing the linear index).

### Revised Data Layout (linearized)

| Variable | Size | Contents |
|----------|------|----------|
| `,1` | 25 | All men's preferences. `,1 SUB ((M-1)*5 + I)` = man M's I-th choice |
| `,2` | 25 | All women's rankings. `,2 SUB ((W-1)*5 + M)` = woman W's ranking of man M |
| `;1` | 5 | Man M's proposal index. `;1 SUB M` = how far down his list man M has gone |
| `;2` | 5 | Woman W's current partner. `;2 SUB W` = her current partner (0 = free) |

Using 16-bit arrays (`,` and `;`) since all values fit in 16 bits.

**Index computation:** `(M-1)*5 + I` requires multiply. We can use repeated addition: `M+M+M+M+M - 5 + I` = `5*M - 5 + I`. Or precompute offsets: man 1 starts at 1, man 2 at 6, man 3 at 11, man 4 at 16, man 5 at 21.

Subroutine to compute linear index:
```
Input: .1 = M (1-5), .2 = I (1-5)
Output: .3 = (M-1)*5 + I

Steps: .3 = .1 + .1 + .1 + .1 + .1   (5*M)
       .3 = .3 - 5 + .2               (5*M - 5 + I)
       .3 = .3 - 4 + .2 actually...
```

Simpler: `.3 = .1 * 5 - 5 + .2`. With syslib (1040) for 16-bit multiply (`.1 * .2 → .3`), or just do 4 additions:

```
.3 = .1
.3 = .3 + .1 (2M)
.3 = .3 + .1 (3M)
.3 = .3 + .3 (6M)... no that's wrong
```

Just use (1040): `.1 = M`, `.2 = #5`, `DO (1040) NEXT`, `.3 = 5*M`. Then subtract 5, add I.

Actually we don't have (1040) confirmed working. Safest: 4 additions via (1000).

```
.2 = .1     (M)
DO (1000) NEXT   → .3 = 2M
.2 = .3
.1 = .3
DO (1000) NEXT   → .3 = 4M
.1 = .3
.2 = original M  → need to save it
DO (1000) NEXT   → .3 = 5M
```

We need to STASH the original M. Or use a simpler approach:

```
input: .1 = M, .2 = I
STASH .1 + .2
.2 <- .1         (M)
DO (1000) NEXT   → .3 = 2M
.1 <- .3
DO (1000) NEXT   → .3 = 3M
.1 <- .3
DO (1000) NEXT   → .3 = 4M
.1 <- .3
DO (1000) NEXT   → .3 = 5M
.1 <- .3
.2 <- #4
DO (1010) NEXT   → .3 = 5M - 4
RETRIEVE .2       (get I back)
.1 <- .3
.2 <- retrieved .2
DO (1000) NEXT   → .3 = 5M - 4 + I = (M-1)*5 + I + 1...
```

Wait: `(M-1)*5 + I` for M=1, I=1 should give 1. `5*1 - 5 + 1 = 1`. Check: `5*1 - 4 + I`? `5 - 4 + 1 = 2`. That's off by one.

`(M-1)*5 + I = 5M - 5 + I`. For M=1, I=1: `5-5+1 = 1`. Correct.

So: compute 5M, subtract 5, add I:
```
input: .1 = M, .2 = I
STASH .2
.2 <- .1
DO (1000) NEXT   → .3 = 2M
.1 <- .3
DO (1000) NEXT   → .3 = 3M
.1 <- .3
DO (1000) NEXT   → .3 = 4M
.1 <- .3
RETRIEVE .2      → .2 = original M
DO (1000) NEXT   → .3 = 5M
.1 <- .3
.2 <- #5
DO (1010) NEXT   → .3 = 5M - 5
.1 <- .3
STASH on original I... wait we already retrieved .2

Hmm, need to save I across all the additions. Let me use .6 as temp.
```

This is getting verbose but it works. We can factor out a "compute linear index" subroutine `(9500)`.

## Subroutine Map

| Label | Name | Input | Output | Description |
|-------|------|-------|--------|-------------|
| `(8900)` | Init | — | — | Hardcode preference data into `,1` and `,2`, initialize `;1` and `;2` |
| `(9000)` | Solve | — | — | Outer COME FROM loop: while unmatched men exist |
| `(9100)` | Propose | `.21`=M | — | Man M proposes to his next woman, updates state |
| `(9200)` | Prefers | `.1`=rank_new, `.2`=rank_current | `.5`={1,2} | Compare rankings. 1=prefers new, 2=keeps current |
| `(9300)` | Print | — | — | Read out matches |
| `(9400)` | GetPref | `.1`=M, `.2`=index | `.3`=woman | Get man M's I-th preference from `,1` |
| `(9450)` | GetRank | `.1`=W, `.2`=M | `.3`=ranking | Get woman W's ranking of man M from `,2` |
| `(9500)` | LinIdx | `.1`=row, `.2`=col | `.3`=index | Compute `(row-1)*5 + col` |

## Control Flow

```
Main:
    DO (8900) NEXT          ← init
    DO (9000) NEXT          ← solve
    DO (9300) NEXT          ← print
    DO GIVE UP

(9000) Solve:
    .22 <- #5               ← 5 unmatched men
    COME FROM (9099)         ← outer loop
    .21 <- #0                ← reset inner counter
    COME FROM (9089)         ← inner loop (for M = 1 to 5)
    increment .21
    if .21 > 5: exit inner (double-NEXT trampoline)
    if man .21 is already matched: skip to inner loop bottom
    DO (9100) NEXT           ← propose
    (9089) inner loop target
    ... double-NEXT exit for inner ...
    check .22 = 0: exit outer (double-NEXT trampoline)
    (9099) outer loop target
    ... double-NEXT exit for outer ...
    RESUME #1                ← return to main
```

## Test Data

Input (men's preferences — who they propose to, in order):
```
Man 1: W3 W1 W2 W5 W4
Man 2: W1 W3 W4 W2 W5
Man 3: W1 W2 W3 W4 W5
Man 4: W2 W1 W4 W3 W5
Man 5: W3 W4 W1 W2 W5
```

Input (women's preferences — their ranked list):
```
Woman 1: M1 M3 M2 M5 M4    → rankings: M1=1, M2=3, M3=2, M4=5, M5=4
Woman 2: M2 M4 M1 M3 M5    → rankings: M1=3, M2=1, M3=4, M4=2, M5=5
Woman 3: M5 M1 M3 M2 M4    → rankings: M1=2, M2=4, M3=3, M4=5, M5=1
Woman 4: M3 M1 M5 M4 M2    → rankings: M1=2, M2=5, M3=1, M4=4, M5=3
Woman 5: M4 M2 M1 M5 M3    → rankings: M1=3, M2=2, M3=5, M4=1, M5=4
```

Stored in `,2` (linearized, women's rankings):
```
,2 SUB 1  = 1   (W1 ranks M1 as 1)
,2 SUB 2  = 3   (W1 ranks M2 as 3)
,2 SUB 3  = 2   (W1 ranks M3 as 2)
,2 SUB 4  = 5   (W1 ranks M4 as 5)
,2 SUB 5  = 4   (W1 ranks M5 as 4)
,2 SUB 6  = 3   (W2 ranks M1 as 3)
,2 SUB 7  = 1   (W2 ranks M2 as 1)
...etc
```

### Expected output (Gale-Shapley, men propose):

Running the algorithm by hand:

**Round 1:** All men propose to their first choice.
- M1 → W3, M2 → W1, M3 → W1, M4 → W2, M5 → W3
- W1 gets {M2, M3}, prefers M3 (rank 2) over M2 (rank 3). Holds M3, rejects M2.
- W2 gets {M4}. Holds M4.
- W3 gets {M1, M5}, prefers M5 (rank 1) over M1 (rank 2). Holds M5, rejects M1.

After round 1: M1 free, M2 free. Matches: M3-W1, M4-W2, M5-W3.

**Round 2:** Free men propose to next choice.
- M1 → W1 (his 2nd choice). W1 has M3 (rank 2). M1 is rank 1. Prefers M1! Dumps M3, holds M1.
- M2 → W3 (his 2nd choice). W3 has M5 (rank 1). M2 is rank 4. Keeps M5. Rejects M2.

After round 2: M2 free, M3 free. Matches: M1-W1, M4-W2, M5-W3.

**Round 3:**
- M2 → W4 (his 3rd choice). W4 is free. Holds M2.
- M3 → W2 (his 2nd choice). W2 has M4 (rank 2). M3 is rank 4. Keeps M4. Rejects M3.

After round 3: M3 free. Matches: M1-W1, M2-W4, M4-W2, M5-W3.

**Round 4:**
- M3 → W3 (his 3rd choice). W3 has M5 (rank 1). M3 is rank 3. Keeps M5. Rejects M3.

**Round 5:**
- M3 → W4 (his 4th choice). W4 has M2 (rank 5). M3 is rank 1. Prefers M3! Dumps M2, holds M3.

After round 5: M2 free. Matches: M1-W1, M3-W4, M4-W2, M5-W3.

**Round 6:**
- M2 → W2 (his 4th choice). W2 has M4 (rank 2). M2 is rank 1. Prefers M2! Dumps M4, holds M2.

After round 6: M4 free. Matches: M1-W1, M2-W2, M3-W4, M5-W3.

**Round 7:**
- M4 → W1 (his 2nd choice). W1 has M1 (rank 1). M4 is rank 5. Keeps M1. Rejects M4.

**Round 8:**
- M4 → W4 (his 3rd choice). W4 has M3 (rank 1). M4 is rank 4. Keeps M3. Rejects M4.

**Round 9:**
- M4 → W3 (his 4th choice). W3 has M5 (rank 1). M4 is rank 5. Keeps M5. Rejects M4.

**Round 10:**
- M4 → W5 (his 5th choice). W5 is free. Holds M4.

**Final stable matching:**
```
M1 — W1
M2 — W2
M3 — W4
M4 — W5
M5 — W3
```

Expected output: `I II IV V III` (woman matched to man 1, 2, 3, 4, 5 respectively).

Or if we print man-to-woman: Man 1→W1, Man 2→W2, Man 3→W4, Man 4→W5, Man 5→W3.

## Implementation Order

1. `(8900)` Init — hardcode arrays
2. `(9500)` LinIdx — compute (row-1)*5 + col
3. `(9400)` GetPref — look up man's preference
4. `(9450)` GetRank — look up woman's ranking
5. `(9200)` Prefers — compare rankings
6. `(9100)` Propose — core proposal logic
7. `(9000)` Solve — nested COME FROM loops
8. `(9300)` Print — output results
9. Main — wire it all together

## Risks

1. **Nested COME FROM loops** — never tested on C-INTERCAL. Inner loop's double-NEXT trampoline must not interfere with outer loop.
2. **Variable clobbering** — syslib uses .1-.6 internally. Must STASH/RETRIEVE around syslib calls, or only use syslib in subroutines that manage their own state.
3. **Array indexing** — `;1 SUB .21` requires `.21` to be valid. Off-by-one errors in index computation will be hard to debug.
4. **Label conflicts** — 9000-9500 range must not conflict with syslib labels (1000-1999 range).
5. **Politeness** — must maintain PLEASE ratio for C-INTERCAL compatibility.
