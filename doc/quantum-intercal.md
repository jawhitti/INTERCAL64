# QUANTUM INTERCAL

## A Conditions-Appropriate Extension to the Compiler Language With No Pronounceable Acronym

### Jason Whittington, 2026

### With Computational Assistance That Wishes to Remain Anonymous

---

## 1. INTRODUCTION

It has been over fifty years since the publication of the original INTERCAL reference manual (Woods and Lyon, 1973). During this time, computing has advanced considerably. Processors now operate on 64-bit integers as a matter of course, and quantum computing has progressed from theoretical curiosity to the subject of intensely funded research programs that have yet to produce anything useful. INTERCAL has kept pace with neither development.

This is a shame, because INTERCAL has elements that are already quantum-aligned. Woods and Lyon had vision that was not appreciated in 1973. INTERCAL is built around a mingle operator that puts two values into a sort of superposition and a select operator that collapses the superposition into a usable value. Superposition and measurement are fundamental properties of quantum mechanics. INTERCAL has been a quantum programming language for fifty years. No one noticed.

This document describes a series of extensions to INTERCAL that build on those strengths. The resulting language brings INTERCAL into the modern era by introducing 64-bit variable types, quantum superposition primitives, and a control flow mechanism based on whether or not a cat is alive.

The reader is advised that familiarity with the original INTERCAL reference manual is assumed throughout. Familiarity with quantum mechanics is not required, as it would not help.

## 2. OVERVIEW: NONDETERMINISTIC CONTROL FLOW

The central contribution of this work is a mechanism for nondeterministic program control flow (Whittington, 2019). Traditional programming languages offer deterministic branching (if/else), pseudo-random branching (rand), and INTERCAL's own probabilistic branching (%50). All of these are, at their core, predictable. The programmer or the runtime decides which path to take.

We propose something different: let the universe decide.

The mechanism requires three primitives:

1. **A source of quantum randomness.** We introduce the _cat box_ (`[]`), a variable that holds a value in quantum superposition. The value exists and does not exist simultaneously. Neither the programmer nor the runtime knows the value until it is observed.

2. **A way to correlate outcomes.** We introduce the _MASH_ statement, which entangles two or more cat boxes. Once mashed, the cats share a fate: when any box in the group is observed, exactly one cat survives. All others die. The survivor is chosen by the universe uniformly at random.

3. **A way to measure the result and act on it.** We introduce the _schrodie_ (`⟨N|ψ⟩`), a statement guard that observes a cat box. If the cat is alive, the statement executes. If the cat is dead, the statement is skipped. The schrodie is a quantum ABSTAIN.

These three primitives compose. The following program creates two possible control paths and lets the universe choose which one executes:

```
DO []1 <- .1 <- #1
DO []2 <- .2 <- #2
DO MASH []1 WITH []2
DO ⟨1|ψ⟩ READ OUT .1
DO ⟨2|ψ⟩ READ OUT .2
PLEASE GIVE UP
```

This program outputs either `1` or `2`, never both, never neither. The choice is made at the moment the first schrodie is evaluated. At that instant the entangled superposition collapses, one cat lives, one cat dies, and the program's control flow is determined. The programmer has no influence over the outcome. This is by design.

The mechanism scales. The quantum roulette program (Section 6.2) creates 38 entangled cat boxes and uses 38 schrodies to implement a roulette wheel. One pocket fires per spin. Thirty-seven cats die. The house always wins.

## 3. CHARACTER SET EXTENSIONS

The following characters have been added to the INTERCAL character set, or have been reassigned from their previous roles of doing nothing.

| Character | Name | Usage |
|-----------|------|-------|
| `[` | correct horse battery staple | Variable prefix (left half of cat box) |
| `]` | incorrect horse battery staple | Variable prefix (right half of cat box) |
| `[]` | cat box | Quantum variable prefix |
| `\|` | maypole | Unary spin operator (mirror+invert) |
| `-` | monkey bar | Unary spin operator (invert) |
| `⟨` | spread | Schrodie opening (U+27E8) |
| `⟩` | pinch | Schrodie closing (U+27E9) |
| `ψ` | rake | Quantum state indicator (U+03C8) |
| `<` | left lung | Wimpmode schrodie opening |
| `>` | right lung | Wimpmode schrodie closing |
| `?` | what | Wimpmode quantum state indicator (also XOR, context-dependent) |

### 3.1 Labels

Many applications have fallen victim to label clash due to many authors using labels with values < 5000. Labels have been extended to 64 bits so that programs exceeding 119 lines are assured of having enough labels. The reader is probably aware that 64-bit values take more space. Users are encouraged to avoid labels like `(10) DO <whatever>` and are encouraged to prefer `(744073709551615) DO <whatever>`. Hopefully this will lead to fewer integration problems in the future. Please note that the authors of this paper did test the label 744073709551615 but did not test all labels available. Care has been taken to only use labels with values > INT_MAX for all new library code included in this release.

## 4. VARIABLES AND CONSTANTS

### 4.1 Variable Types

All classic INTERCAL variable types are supported along with two new ones:

| Prefix | Name | Width | Description |
|--------|------|-------|-------------|
| `.` | quantum dot | 16-bit | A 16-bit unsigned integer |
| `:` | double dot | 32-bit | A 32-bit unsigned integer |
| `::` | double cateye | 64-bit | A 64-bit unsigned integer |
| `,` | tail | 16-bit | A 16-bit array |
| `;` | hybrid | 32-bit | A 32-bit array |
| `;;` | double hybrid | 64-bit | A 64-bit array |
| `[]` | cat box | quantum | A quantum superposition |

The quantum dot (`.`) replaces spot (`.`) as the indicator for variable declarations. Single-dot (`.`) and double-dotted (`:`) variables replace their classic INTERCAL equivalents and the new type double cateye extends the existing quantum dot (`.`, 16-bit) and double dot (`:`, 32-bit) series to 64-bit precision. It is so named because it consists of two double dots, and two twos is four.

The cat box (`[]`) holds a value in quantum superposition. Once a value is put in the box it can be entangled (mashed) with other cat boxes. Values in the cat box exist in a state of superposition. Observation occurs upon assignment to a scalar variable, upon use in a READ OUT statement, or upon evaluation of a schrodie. At that point the superposition collapses and the cat is either alive (the value is preserved) or dead (the value becomes DEDKITTY, a 64-bit sentinel). Which outcome occurs is determined by the universe.

#### Reserved value

The integer value 4919413258115634265 is reserved for the library. This value is used as a sentinel in certain cases related to quantum collapse. It is the ASCII encoding of the string "DEDKITTY" interpreted as a big-endian 64-bit integer, and it means what it sounds like. The value 0 is available for general use, as a survey of the existing literature found it to be quite popular. No mentions of 4919413258115634265 have been found in the literature so it is assumed to be a reasonable sentinel value. Users are encouraged to avoid this value in their own programs.

### 4.2 Constants

Constants are formed with the mesh (`#`) prefix:

| Prefix | Name | Width |
|--------|------|-------|
| `#` | mesh | 16-bit |
| `##` | insecure fence | 32-bit |
| `####` | secure fence | 64-bit |

The absence of a triple mesh (`###`) is intentional. Three meshes would imply 48-bit precision, which is not a thing.

## 5. OPERATORS

### 5.1 Existing Operators

The five original INTERCAL operators are retained:

| Operator | Name | Type | Description |
|----------|------|------|-------------|
| `$` | big money | Binary | Interleaves bits of two operands |
| `~` | select | Binary | Extracts bits using a mask |
| `&` | ampersand | Unary | AND of adjacent bit pairs |
| `V` | V | Unary | OR of adjacent bit pairs |
| `?` | what | Unary | XOR of adjacent bit pairs |

### 5.2 New Unary Operators

Two new unary operators are introduced to support quantum spin operations:

| Operator | Name | Type | Description |
|----------|------|------|-------------|
| `\|` | maypole | Unary | Horizontal spin. The value "swings around the maypole 180 degrees". Bits are mirrored and flipped front-to-back so bits that were showing their front side (1) now show their back side (0) and vice-versa. |
| `-` | monkey bar | Unary | Bits flip 180 off the monkey bar. Each bit is inverted in place (ones' complement) but is now upside-down. |

Both operators are _involutions_: applying either twice yields the original value. This is the defining property of a spin-1/2 operator.

### 5.3 Array Involutions

The maypole and monkey bar operators may also be applied to entire arrays using the syntax `DO ,2 <- |,1` or `DO ,2 <- -,1`. Chained operations such as `DO ,2 <- -|,1` are supported.

For one-dimensional arrays:
- `|` (maypole): reverses element order and bit-inverts each value
- `-` (monkey bar): bit-inverts each value in place

For two-dimensional arrays, the operations correspond to flipping a sheet of paper:
- `|` (maypole): reverses columns (horizontal flip) and bit-inverts
- `-` (monkey bar): reverses rows (vertical flip) and bit-inverts

For three-dimensional arrays, the operations correspond to spinning a Rubik's cube 180 degrees:
- `|` (maypole): reverses the last axis (horizontal spin) and bit-inverts
- `-` (monkey bar): reverses the first axis (vertical spin) and bit-inverts

The programmer who wishes to rotate a Rubik's cube 90 degrees, or to spin a four-dimensional array, will receive:

    E4D1 ROTATING A HYPERCUBE IS LEFT AS AN EXERCISE FOR THE READER

### 5.4 The Identity Discovery

The composition of the maypole and monkey bar operators, applied twice in sequence as `|-|-`, produces the identity function. That is, for any value `x`:

    '|-|-x' = x

This is significant because the mesh character `#`, when viewed as two vertical strokes and two horizontal strokes, is itself composed of `|-|-`. It is clear that the original INTERCAL designers chose `#` for the constant prefix for exactly this reason: `#` represents two identity operators applied to a value, which exactly defines a constant.

Note: We have no evidence for this claim.

### 5.5 Mingle and Select at Higher Widths

The mingle operator (`$`) produces a result twice the width of its operands:

| Operands | Result |
|----------|--------|
| 16-bit `$` 16-bit | 32-bit |
| 32-bit `$` 32-bit | 64-bit |
| 64-bit `$` 64-bit | 128-bit (ephemeral) |

The 128-bit result of mingling two 64-bit values is ephemeral. It cannot be stored in any variable. It exists only long enough to be consumed by a select operator, which reduces it back to at most 64 bits. The 128-bit value is a fleeting quantum of information, briefly real and then gone. Much like a good idea in a committee meeting.

The select operator (`~`) correspondingly operates at all widths, including selecting from a 128-bit value using a 128-bit mask (which is again ephemeral and cannot be stored in a variable).

## 6. STATEMENTS

### 6.1 Existing Statements

All original INTERCAL statements are retained without modification. The programmer may continue to ABSTAIN FROM CALCULATING, COME FROM unexpected places, and GIVE UP at any time.

### 6.2 MASH

The MASH statement entangles two or more quantum cat boxes. Once entangled, the cats share a correlated fate: when the superposition of any box in the group is collapsed, exactly one cat in the entire entangled group survives. All others become DEDKITTY.

Syntax:

    DO MASH []1 WITH []2
    PLEASE MASH []1 WITH []2 WITH []3 WITH []4

N-way entanglement is supported. The entanglement operation is idempotent; mashing already-acquainted cats produces no additional effect.

The choice of the word MASH was guided by the following considerations: (1) it is an English word, (2) it describes what happens to the cats, and (3) it was not already taken. The gerund MASHING follows standard INTERCAL gerund formation. ABSTAIN FROM MASHING prevents the formation of new entanglements, which may be desirable in programs where the cats have had enough.

### 6.3 Cat Box Operations

The cat box is fundamental to quantum programs as the source of randomness.

**Creation:**

    DO []1 <- .1

Creates a cat box containing the value stored in `.1` in quantum superposition. The cat is alive and holds the passed value. It is probably angry to be in a box. The cat box can hold any scalar value.

**Chained initialization:**

Testing has indicated that the following construct appears often in quantum INTERCAL code:
```
DO .1 <- #2
DO []1 <- .1
```
Multiline constructs degrade code readability so chaining operators are available as a shortcut:
```
DO []1 <- .1 <- #2
DO []1 <- #2
```

**Collapse:**

Cat boxes collapse upon observation, which most commonly happens when they are assigned to a scalar value. For example:
```
DO []1 <- #19
DO .2 <- []1
PLEASE DO NOTE .2 CONTAINS EITHER 19 OR DEDKITTY
```

The superposition is destroyed in line 2 by the `<-` operator. If the cat is alive, `.2` receives the box's value. If the cat is dead, `.2` receives DEDKITTY. The box retains whichever state was observed.

### 6.4 The Schrodie

The schrodie is the mechanism by which quantum superposition affects control flow (see Section 2). It is placed between the statement prefix (DO, PLEASE DO, etc.) and the statement body:

    DO ⟨5|ψ⟩ READ OUT .1

The schrodie `⟨5|ψ⟩` observes cat box 5 at the time of statement execution. If the cat box is entangled with other cats the superposition collapses. If the cat is alive, the schrodie opens and the statement executes. If the cat is dead, the schrodie stays closed. The statement is skipped, as if it had been abstained from by the universe itself.

The schrodie is named after Erwin Schrodinger, whose famous thought experiment involving a cat in a box is the direct inspiration for the cat box variable type. That the notation also resembles Dirac's bra-ket notation (Dirac, 1939) is a happy coincidence. The full form consists of five characters:

| Position | Character | Name |
|----------|-----------|------|
| 1 | `⟨` | spread (U+27E8) |
| 2 | digits | box identifier |
| 3 | `\|` | maypole |
| 4 | `ψ` | rake (U+03C8) |
| 5 | `⟩` | pinch (U+27E9) |

The inclusion of ψ (rake) is mandatory (as is good dental hygiene). It serves no computational purpose but provides the statement with an air of scientific legitimacy. Future work may extend the schrodie with other fancy glyphs in its place.

The alert reader has likely already sussed out that the schrodie is a kind of quantum ABSTAIN. It is the spiritual successor to %50 but considerably more powerful. %50 will likely be removed in future versions.

#### 6.4.1 Wimpmode Alternative

For programmers whose keyboards lack the mathematical angle brackets and Greek letters required by the full schrodie notation, a wimpmode alternative is provided:

    DO <5|?> READ OUT .1

The wimpmode schrodie substitutes the left lung (`<`), right lung (`>`), and what (`?`) for their Unicode counterparts. It is functionally identical to the full schrodie except in two respects:

1. The compiler emits the warning: `W001 USING WIMPMODE QUANTUM NOTATION CAUSES OBSERVABLE DECOHERENCE (YOUR CODE WILL BE SLOWER)`

2. Using wimpmode routes calls through a translator thunk. This thunk carries a performance penalty so programmers are encouraged to update and use the full syntax.

## 7. EXAMPLE PROGRAMS

### 7.1 Quantum Shared Secret (eve.i)

The following program demonstrates quantum key distribution with eavesdropper detection. Alice creates two entangled cat boxes. Eve intercepts one and collapses it, destroying the entanglement. Eve creates a forgery. Alice and Bob compare results and detect the tampering approximately 50% of the time.

```
DO .1 <- #42
DO []1 <- .1
PLEASE DO []2 <- .1
DO MASH []1 WITH []2
DO ::1 <- []2
DO READ OUT ::1
PLEASE DO []2 <- .1
DO ::2 <- []1
PLEASE DO ::3 <- []2
DO READ OUT ::2
PLEASE DO READ OUT ::3
DO GIVE UP
```

### 7.2 Quantum Roulette (roulette4.i)

The following program implements a 38-pocket quantum roulette wheel. Thirty-eight cat boxes are created with values 0 through 37, entangled via a single MASH statement, and observed through schrodies. Exactly one number is output per execution.

```
DO NOTE QUANTUM ROULETTE WITH CHAINED INIT
DO []1<-.1<-#0 DO []2<-.2<-#1 PLEASE DO []3<-.3<-#2 DO []4<-.4<-#3
DO []5<-.5<-#4 DO []6<-.6<-#5 PLEASE DO []7<-.7<-#6 DO []8<-.8<-#7
DO []9<-.9<-#8 DO []10<-.10<-#9 PLEASE DO []11<-.11<-#10 DO []12<-.12<-#11
DO []13<-.13<-#12 DO []14<-.14<-#13 PLEASE DO []15<-.15<-#14 DO []16<-.16<-#15
DO []17<-.17<-#16 DO []18<-.18<-#17 PLEASE DO []19<-.19<-#18 DO []20<-.20<-#19
DO []21<-.21<-#20 DO []22<-.22<-#21 PLEASE DO []23<-.23<-#22 DO []24<-.24<-#23
DO []25<-.25<-#24 DO []26<-.26<-#25 PLEASE DO []27<-.27<-#26 DO []28<-.28<-#27
DO []29<-.29<-#28 DO []30<-.30<-#29 PLEASE DO []31<-.31<-#30 DO []32<-.32<-#31
DO []33<-.33<-#32 DO []34<-.34<-#33 PLEASE DO []35<-.35<-#34 DO []36<-.36<-#35
DO []37<-.37<-#36 DO []38<-.38<-#37
DO MASH []1 WITH []2 WITH []3 WITH []4 WITH []5 WITH []6 WITH []7 WITH []8 WITH
        []9 WITH []10 WITH []11 WITH []12 WITH []13 WITH []14 WITH []15 WITH
        []16 WITH []17 WITH []18 WITH []19 WITH []20 WITH []21 WITH []22 WITH
        []23 WITH []24 WITH []25 WITH []26 WITH []27 WITH []28 WITH []29 WITH
        []30 WITH []31 WITH []32 WITH []33 WITH []34 WITH []35 WITH []36 WITH
        []37 WITH []38
PLEASE DO NOTE SPIN THE WHEEL
DO ⟨1|ψ⟩ READ OUT .1 DO ⟨2|ψ⟩ READ OUT .2 PLEASE DO ⟨3|ψ⟩ READ OUT .3
DO ⟨4|ψ⟩ READ OUT .4 DO ⟨5|ψ⟩ READ OUT .5 DO ⟨6|ψ⟩ READ OUT .6
PLEASE DO ⟨7|ψ⟩ READ OUT .7 DO ⟨8|ψ⟩ READ OUT .8 DO ⟨9|ψ⟩ READ OUT .9
DO ⟨10|ψ⟩ READ OUT .10 PLEASE DO ⟨11|ψ⟩ READ OUT .11 DO ⟨12|ψ⟩ READ OUT .12
DO ⟨13|ψ⟩ READ OUT .13 DO ⟨14|ψ⟩ READ OUT .14 PLEASE DO ⟨15|ψ⟩ READ OUT .15
DO ⟨16|ψ⟩ READ OUT .16 DO ⟨17|ψ⟩ READ OUT .17 DO ⟨18|ψ⟩ READ OUT .18
PLEASE DO ⟨19|ψ⟩ READ OUT .19 DO ⟨20|ψ⟩ READ OUT .20 DO ⟨21|ψ⟩ READ OUT .21
DO ⟨22|ψ⟩ READ OUT .22 PLEASE DO ⟨23|ψ⟩ READ OUT .23 DO ⟨24|ψ⟩ READ OUT .24
DO ⟨25|ψ⟩ READ OUT .25 DO ⟨26|ψ⟩ READ OUT .26 PLEASE DO ⟨27|ψ⟩ READ OUT .27
DO ⟨28|ψ⟩ READ OUT .28 DO ⟨29|ψ⟩ READ OUT .29 DO ⟨30|ψ⟩ READ OUT .30
PLEASE DO ⟨31|ψ⟩ READ OUT .31 DO ⟨32|ψ⟩ READ OUT .32 DO ⟨33|ψ⟩ READ OUT .33
DO ⟨34|ψ⟩ READ OUT .34 PLEASE DO ⟨35|ψ⟩ READ OUT .35 DO ⟨36|ψ⟩ READ OUT .36
DO ⟨37|ψ⟩ READ OUT .37 DO ⟨38|ψ⟩ READ OUT .38
PLEASE GIVE UP
```

The first schrodie to be evaluated triggers the collapse of all 38 entangled boxes. Thirty-seven cats die. One survives. Its value is printed. The house always wins.

### 7.3 Pauly Shore's Algorithm (shores_algorithm.i)

An algorithm for integer factorization, substantially improved. Factors 15 into 5 and 3 using quantum computational advantage. Complexity: O(BUUUDDY). Memory: O(1). Correctness: optimistic. Full source available in the distribution.

## 8. ERROR MESSAGES

The following error messages have been added:

| Code | Message | Cause |
|------|---------|-------|
| E2007 | THE CAT IS DEAD | A cat box was observed and the cat did not survive |
| E2010 | THE CAT IS BOTH DEAD AND A DIFFERENT SIZE | Type mismatch in quantum superposition |
| E4D1 | ROTATING A HYPERCUBE IS LEFT AS AN EXERCISE FOR THE READER | Array involution on rank > 3 |
| W001 | USING WIMPMODE QUANTUM NOTATION CAUSES OBSERVABLE DECOHERENCE | Wimpmode schrodie detected |

The error code E2007 was chosen because 2007 is the year of the first experimental demonstration of quantum entanglement at a distance exceeding 100 kilometers (Ursin et al., 2007). It is also a prime number, which is not relevant but is the sort of thing one mentions in academic papers.

## 9. SYSTEM LIBRARY

The system library (`syslib64.i`) provides arithmetic routines at 16-bit, 32-bit, and 64-bit widths. All routines are implemented in pure INTERCAL.

Routines may be called by their numeric label or by their ASCII name label. ASCII name labels are computed by interpreting the routine name as an 8-character big-endian 64-bit integer. For example, the label for ADD16 is the integer whose bytes are `A`, `D`, `D`, `1`, `6`, `\0`, `\0`, `\0` = 4702958889031696384. The programmer who finds this inconvenient is reminded that convenience has never been a design goal.

### 9.1 16-Bit Arithmetic

Operands in `.1` and `.2`. Result in `.3`. Overflow indicator in `.4`.

| Label | Name | Description |
|-------|------|-------------|
| (1000) | ADD16 | .3 = .1 + .2 (no overflow check) |
| (1009) | | .3 = .1 + .2 (with overflow check) |
| (1010) | MINUS16 | .3 = .1 - .2 |
| (1020) | | .3 = .1 * .2 (low 16 bits) |
| (1030) | DIVIDE16 | .3 = .1 / .2, .4 = .1 mod .2 |
| (1040) | TIMES16 | :3 = .1 * .2 (full 32-bit result) |
| (1050) | MODULO16 | .3 = .1 mod .2 |

### 9.2 32-Bit Arithmetic

Operands in `:1` and `:2`. Result in `:3`. Overflow indicator in `:4`.

| Label | Name | Description |
|-------|------|-------------|
| (1500) | ADD32 | :3 = :1 + :2 (no overflow check) |
| (1509) | | :3 = :1 + :2 (with overflow check) |
| (1510) | MINUS32 | :3 = :1 - :2 |
| (1520) | | :1 = .1 $ .2 (mingle 16-bit to 32-bit) |
| (1530) | | :1 = .1 / .2 (16-bit divide, 32-bit result) |
| (1540) | TIMES32 | :3 = :1 * :2 (low 32 bits), :4 = high 32 bits |

### 9.3 64-Bit Arithmetic

Operands in `::1` and `::2`. Result in `::3`.

| Label | Name | Description |
|-------|------|-------------|
| 4702958910472978432 | ADD64 | ::3 = ::1 + ::2 |
| 5569068542595576832 | MINUS64 | ::3 = ::1 - ::2 |
| 6073470532629967872 | TIMES64 | ::3 = ::1 * ::2 |

Division and modulo at 64-bit width are under development. A complete implementation exists for 32-bit:

### 9.4 Division and Modulo

| Label | Name | Operands | Result |
|-------|------|----------|--------|
| (1030) | DIVIDE16 | .1, .2 | .3 = quotient, .4 = remainder |
| (1050) | MODULO16 | .1, .2 | .3 = remainder |
| 4920558940556964658 | DIVIDE32 | :1, :2 | :3 = quotient, :4 = remainder |
| 5570746397223760690 | MODULO32 | :1, :2 | :3 = remainder |
| 5570746397223761460 | MODULO64 | ::1, ::2 | ::3 = remainder (pending DIVIDE64) |

### 9.5 Random Number Generation

| Label | Name | Result |
|-------|------|--------|
| (1900) | RANDOM16 | .1 = random 16-bit value |
| (1910) | | .2 = random value in range [0, .3) |
| 5927104639891485490 | RANDOM32 | :1 = random 32-bit value (mingles two RANDOM16) |
| 5927104639891486260 | RANDOM64 | ::1 = random 64-bit value (mingles two RANDOM32) |

### 9.6 Named Entry Points

The following table lists all named (ASCII label) entry points. These are wrappers around the numeric-label routines and may be used interchangeably.

| Name | Label | Wraps |
|------|-------|-------|
| ADD16 | 4702958889031696384 | (1000) |
| ADD32 | 4702958897554522112 | (1500) |
| ADD64 | 4702958910472978432 | new |
| MINUS16 | 5569068542595249664 | (1010) |
| MINUS32 | 5569068542595379712 | (1510) |
| MINUS64 | 5569068542595576832 | new |
| TIMES16 | 6073470532629640704 | (1040) |
| TIMES32 | 6073470532629770752 | (1540) |
| TIMES64 | 6073470532629967872 | new |
| DIVIDE16 | 4920558940556964150 | (1030) |
| DIVIDE32 | 4920558940556964658 | new |
| MODULO16 | 5570746397223760182 | (1050) |
| MODULO32 | 5570746397223760690 | DIVIDE32 |
| MODULO64 | 5570746397223761460 | DIVIDE64 (pending) |
| RANDOM16 | 5927104639891484982 | (1900) |
| RANDOM32 | 5927104639891485490 | new |
| RANDOM64 | 5927104639891486260 | new |

### 9.7 Overflow Handling

The label (1999) is the overflow handler. It is abstained from by default when calling through (1000) or (1500), and reinstated upon return. Programs that call (1009) or (1509) directly will receive overflow errors via (1999) if the result exceeds the operand width. The error message is:

    (1999) DOUBLE OR SINGLE PRECISION OVERFLOW

The programmer who encounters this error is encouraged to use wider variables.

## REFERENCES

Dirac, P. A. M. (1939). *A New Notation for Quantum Mechanics*. Mathematical Proceedings of the Cambridge Philosophical Society, 35(3), 416-418.

Shore, P. (1994). *Encino Man*. Buena Vista Pictures.

Shor, P. (1994). Algorithms for quantum computation: Discrete logarithms and factoring. *Proceedings 35th Annual Symposium on Foundations of Computer Science*, pp. 124-134.

Ursin, R. et al. (2007). Entanglement-based quantum communication over 144 km. *Nature Physics*, 3, 481-486.

Whittington, J. (2019). *A Preliminary Investigation into Whether INTERCAL Could Be Made Worse*. Unpublished manuscript, never submitted.

Woods, D. R. and Lyon, J. M. (1973). *The INTERCAL Programming Language Reference Manual*. Princeton University.
