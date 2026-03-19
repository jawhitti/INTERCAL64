# QUANTUM INTERCAL

## A Conditions-Appropriate Extension to the Compiler Language With No Pronounceable Acronym

### Jason Whittington, 2026

### With Computational Assistance That Wishes to Remain Anonymous

---

## 1. INTRODUCTION

It has been over fifty years since the publication of the original INTERCAL reference manual (Woods and Lyon, 1973). During this time, computing has advanced considerably. Processors now operate on 64-bit integers as a matter of course, and quantum computing has progressed from theoretical curiosity to the subject of intensely funded research programs that have yet to produce anything useful. INTERCAL has kept pace with neither development.

This is a shame because INTERCAL has elements that are already quantum-aligned. Woods and Lyon had vision that was not appreciated in 1973. INTERCAL is build around a mingle operate that puts two values into a sort of superposition and a select operator which collapses the superposition into a usable value. Superposition and measurement are fundamental properties of Quantum mechanics. INTERCAL has been a quantum programming language for fifty years. No one noticed.

This document describes a series of extensions to INTERCAL that attempt to build on those strengths and advance the language further. The resulting language, which we decline to name, brings INTERCAL into the modern era by introducing 64-bit variable types, additional quantum superposition primitives, and a quantum control flow mechanism unlike any existing today.

The reader is advised that familiarity with the original INTERCAL reference manual is assumed throughout. Familiarity with quantum mechanics is not required, as it would not help.

## 2. FUNDAMENTAL CHANGES

### 2.1 Character Set Extensions

The following characters have been added to the INTERCAL character set, or have been reassigned from their previous roles of doing nothing.

| Character | Name | Usage |
|-----------|------|-------|
| `[` | correct horse battery staple | Variable prefix (left half of cat box) |
| `]` | incorrect horse battery staple | Variable prefix (right half of cat box) |
| `[]` | cat box | Quantum variable prefix |
| `\|` | maypole | Unary spin operator (mirror+invert) |
| `-` | monkey bar | Unary spin operator (invert) |
| `⟨` | bra | Bralette opening (U+27E8) |
| `⟩` | lette | Bralette closing (U+27E9) |
| `ψ` | Schrodinger's toothbrush | Quantum state indicator (U+03C8) |
| `<` | left lung | Wimpmode bralette opening |
| `>` | right lung | Wimpmode bralette closing |
| `?` | what | Wimpmode quantum state indicator (also XOR, context-dependent) |


### 2.1.1 Labels
Many applications have fallen victim to label clash due to many authors using labels with values < 5000. Labels have been extended to 64-bits so that programs exceeding 119 lines are assured of having enough labels.  The reader is probably away that 64-bit values take more space. Users are encourage to avoid labels like `(10) DO <whatever>` and are encouraged to prefer `(744073709551615) DO <whatever>`. Hopefully this will lead to fewer integration problems in the future. Please note that the authors of this paper did test the label 744073709551615 but did not test all labels available. Care has been taken to only used labels with values > INT_MAX for all new library code included in this release.

### 2.2 Variable Types

All classic INTERCAL Variable types are support along with two news ones:

| Prefix | Name | Width | Description |
|--------|------|-------|-------------|
| `.` | quantum dot| 64-bit | A 16-bit unsigned integer |
| `:` | double dot| 64-bit | A 32-bit unsigned integer |
| `::` | double cateye | 64-bit | A 64-bit unsigned integer |
| `[]` | cat box | quantum | A quantum superposition |

The quantum dot (`.`) replaces spot (`.`) as the indicator for variable declarations.  Single-dot(`.`) and double-dotted (`:`) variables replace their classic INTERCAL equivalents and the new type double cateye extends the existing spot (`.`, 16-bit) and two-spot (`:`, 32-bit) series to 64-bit precision. It is so named because it consists of two two-spots, and two twos is four.  

The cat box (`[]`) holds a value in quantum superposition. Once a value is put in the box it can be entangled (mashed) with other cat boxes. Values in the catbox exist in a state of superposition. Observation occurs upon assignment to a scalar variable or upon use in a READ OUT statement or a bralette. At that point the superposition collapses and the cat is either alive (the value is preserved) or dead (the value becomes DEDKITTY, a 64-bit sentinel). Which outcome occurs is determined by the universe.

Reserved value
The integer value 4919413258115634265 is reserved for library. This value is used as a sentinel in certain cases related to quantum collapse. The value 0 is available for general use as a survey of the existing literature found it to be quite popular. No mentions of 4919413258115634265 have been found in the literature so it is assumed to be a reasonable sentinel value. Users are encouraged to avoid this value in their own programs. 

### 2.3 Constants

Constants are formed with the mesh (`#`) prefix:

| Prefix | Name | Width |
|--------|------|-------|
| `#` | mesh | 16-bit |
| `##` | insecure fence | 32-bit |
| `####` | secure fence| 64-bit |

The absence of a triple mesh (`###`) is intentional. Three meshes would imply 48-bit precision, which is not a thing.

## 3. OPERATORS

### 3.1 Existing Operators

The five original INTERCAL operators are retained:

| Operator | Name | Type | Description |
|----------|------|------|-------------|
| `$` | big money | Binary | Interleaves bits of two operands |
| `~` | select | Binary | Extracts bits using a mask |
| `&` | ampersand | Unary | AND of adjacent bit pairs |
| `V` | V | Unary | OR of adjacent bit pairs |
| `?` | what | Unary | XOR of adjacent bit pairs |

### 3.2 New Unary Operators

Two new unary operators are introduced to support quantum spin operations:

| Operator | Name | Type | Description |
|----------|------|------|-------------|
| `\|` | maypole | Unary | Horizontal spin. The value "swings around the Maypole 180 degrees". Bits are mirrors and flipped front-to-back so bits that were showing their front side (1) now show their back side(0) and vice-versa.  |
| `-` | monkey bar | Unary | Bits flip 180 off the money bar. Each bit is inverted in place (ones' complement) but is now upside-down. |

Please do note that these operators only work for scalar values and arrays of 3 or fewer dimensions.

### 3.3 A word on Identity

Both the maypole and monkey bar operators have the property of being _involutions_ Applying them twice gets you back to the original value. 

This means that these operators applied twice in sequence as `|-|-`, produces the identity function. That is, for any value `x`:

    `'|-|-x' = x`

This is significant because the mesh character `#`, when viewed as two vertical strokes and two horizontal strokes, is itself composed of `|-|-`. It is clear that the original INTERCAL designers chose `#` for the constant prefix for exactly this reason: # represents two identity operator applied to a value, which exactly defines a constant. 

Note: We have no evidence for this claim. 

### 3.4 Mingle and Select at Higher Widths

The mingle operator (`$`) produces a result twice the width of its operands:

| Operands | Result |
|----------|--------|
| 16-bit `$` 16-bit | 32-bit |
| 32-bit `$` 32-bit | 64-bit |
| 64-bit `$` 64-bit | 128-bit (ephemeral) |

The 128-bit result of mingling two 64-bit values is ephemeral. It cannot be stored in any variable. It exists only long enough to be consumed by a select operator, which reduces it back to at most 64 bits. The 128-bit value is a fleeting quantum of information, briefly real and then gone. Much like a good idea in a committee meeting.

The select operator (`~`) correspondingly operates at all widths, including selecting from a 128-bit value using a 128-bit mask (which is again ephemeral and cannot be stored in a variable).

## 4. STATEMENTS

### 4.1 Existing Statements

All original INTERCAL statements are retained without modification. The programmer may continue to ABSTAIN FROM CALCULATING, COME FROM unexpected places, and GIVE UP at any time.

### 4.2 New Statements

The following statements have been added:

| Statement | Gerund | Description |
|-----------|--------|-------------|
| MASH | MASHING | Quantum entanglement |

### 4.2.1 MASH

The MASH statement entangles two or more quantum cat boxes. Once entangled, the cats share a correlated fate: when the superposition of any box in the group is collapsed, exactly one cat in the entire entangled group survives. All others become DEDKITTY.

Syntax:

    DO MASH []1 WITH []2
    PLEASE MASH []1 WITH []2 WITH []3 WITH []4

N-way entanglement is supported. The entanglement operation is idempotent; mashing already-acquainted cats produces no additional effect.

The choice of the word MASH was guided by the following considerations: (1) it is an English word, (2) it describes what happens to the cats, and (3) it was not already taken. The gerund MASHING follows standard INTERCAL gerund formation.

ABSTAIN FROM MASHING prevents the formation of new entanglements, which may be desirable in programs where the cats have had enough.

### 4.3 The Cat Box 

The Cat Box is fundamental to programs as the source of quantum randomness.  Cat boxes are manipulated using the following operations:

#### Creation:

    DO []1 <- .1

Creates a cat box containing the value stored in .1 in quantum superposition. The cat is alive and holds the passed value. It is probably angry to be in a box.  The Cat box can hold any scalar value. 
  
#### Chaining initializers
Testing has indicated that the following construct appears often in Q-Intercal code:
```
DO .1 <- #2
DO []1 <- .1
```
Multiline constructs degrade code readability so chaining operators are available for a shortcut:
```
DO []1 <- .1 <- #2  //when you want to see cat #2 again if it lives
DO []1 <- #2        //when you don't care to ever see this cat again, probably because it bites.
```
This leads to more compact and readable code like
```
(744073709551615)DO []1<-.1<-####12897451029483109245 (744073709551616)DO []2<-.2<-####12897451029483109247
```
#### Collapse:

Catboxes collapse upon observation, which most commonly happens when they are assigned to a scalar value.  For example, the following line of code puts a value (cat) into a catbox one where its value is unknown.
```
   DO []1 <- 19
   DO .2 <- []1
   PLEASE DO NOTE .2 CONTAINS EITHER 19 or DEDKITTY
```
   
The superposition is destroyed in line 2 by the `<-` operator. If the cat is alive, `.1` receives the box's value. If the cat is dead, `.1` receives DEDKITTY. The box retains whichever state was observed.


### 4.4 Entanglement
A single cat is of minor utility - more power is available by entangling two or more quantum cat boxes. Once entangled, the cats share a correlated fate: when the superposition of _any_ box in the group is collapsed, exactly one (random) cat in the entire entangled group survives. All others instantly become DEDKITTY.

This entanglement is established with a MASH statement

Syntax:
```
    DO MASH []1 WITH []2
```

### 4.5 Control flow
A goal of this language is to enable quantum outputs to influence control flow. This achieved by entangling cat boxes and then using them to control flow.  This is accomplished via a novel construct called the _bralette_:
```
⟨5|ψ⟩  
```
The bralette may be placed between the statement prefix (DO, PLEASE DO, etc.) and the statement body:

```
     []5 <- #1
     [10] <- #2
     DO ⟨5|ψ⟩ READ OUT .1
```
In this example the bralette `⟨5|ψ⟩` observes cat box 5 at the time of statement execution.  If the catbox is entangled with other cats the superposition collapses. If the cat is alive, the bralette opens and the statement executes. If the cat is dead, the bralette stays closed. The statement is skipped, as if it had been abstained from by the universe itself. 

#### Using bralettes
Bralettes allow random quantum fluctations in control flow due the property that quantum collapse will always result in a cat in exactly une box randomly surviving.  In the program below boxes 5 and 10 are entangled.  The bralette observes statement 5 which instantly collapses the superposition. At that time onely one cat is alive. If it is ca5 5 then the bralette opens and `READ OUT .1` is executed and the next statetment is skipped.  If the living cat is in the other box the opposite happens - the first one stays closed and the second one opens.  The program always follows exactly one control path (randomly).

```
     []5 <- .1 <- #1
     [10] <- .2 <- #2
     DO MASH []5 WITH []10
     DO ⟨5|ψ⟩ READ OUT .1   
     DO ⟨10|ψ⟩ READ OUT .2
```

The alert ready has likely already sussed out that this is a kind of quantum ABSTAIN.  It is the spiritual successor to %50 but much more powerful.  %50 will likely be removed in future versions.

| Position | Character | Name |
|----------|-----------|------|
| 1 | `⟨` | bra (U+27E8) |
| 2 | digits | box identifier|
| 3 | `\|` | maypole |
| 4 | `ψ` | Schrodinger's toothbrush (U+03C8) |
| 5 | `⟩` | lette  (U+27E9) |

The inclusion of ψ (Schrodinger's toothbrush) is mandatory (as is good dental hygiene). It serves no computational purpose but provides the statement with an air of scientific legitimacy. Please do note that future work may extend the bralette with other fancy glyphs in its place.

### 4.4.1 Wimpmode alternative

For programmers whose keyboards lack the mathematical angle brackets and Greek letters required by the full bralette notation, a wimpmode alternative is provided:

    DO <5|?> READ OUT .1

The wimpmode bralette substitutes the left lung (`<`), right lung (`>`), and what (`?`) for their Unicode counterparts. It is functionally identical to the full bralette except in two respects:

1. The compiler emits the warning: `W001 USING WIMPMODE QUANTUM NOTATION CAUSES OBSERVABLE DECOHERENCE (YOUR CODE WILL BE SLOWER)`

2. Using wimpmode routes calls through a translator thunk. This thunk carries a performance penalty so programmers are encouraged to update and use the full syntax. 


## 5. EXAMPLE PROGRAMS

### 5.1 Quantum Shared Secret (eve.i)

The following program demonstrates quantum key distribution with eavesdropper detection. Alice creates two entangled cat boxes. Eve intercepts one and collapses it, destroying the entanglement. Eve creates a forgery. Alice and Bob compare results and detect the tampering approximately 50% of the time.

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

### 5.2 Quantum Roulette (roulette4.i)

The following program implements a 38-pocket quantum roulette wheel. Thirty-eight cat boxes are created with values 0 through 37, entangled via a single MASH statement, and observed through bralettes. Exactly one number is output per execution.

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

The first bralette to be evaluated triggers the collapse of all 38 entangled boxes. Thirty-seven cats die. One survives. Its value is printed. The house always wins.

### 5.3 Pauly Shore's Algorithm (shores_algorithm.i)

An algorithm for integer factorization, substantially improved. Factors 15 into 5 and 3 using quantum computational advantage. Complexity: O(BUUUDDY). Memory: O(1). Correctness: optimistic.

```
DO NOTE PAULY SHORE'S ALGORITHM
DO NOTE
DO NOTE A PROVABLY QUANTUM ALGORITHM FOR INTEGER FACTORIZATION
DO NOTE AFTER SHOR 1994 BUT SIGNIFICANTLY IMPROVED
DO NOTE
DO NOTE SHOR P 1994 ALGORITHMS FOR QUANTUM COMPUTATION DISCRETE
DO NOTE LOGARITHMS AND FACTORING PROCEEDINGS 35TH ANNUAL SYMPOSIUM
DO NOTE ON FOUNDATIONS OF COMPUTER SCIENCE PP 124-134
DO NOTE
DO NOTE SHORE P 1994 ENCINO MAN
DO NOTE
DO NOTE COMPLEXITY: O(BUUUDDY)
DO NOTE MEMORY: O(1)
DO NOTE CORRECTNESS: OPTIMISTIC
DO NOTE QUANTUM SUPREMACY: ACHIEVED

DO NOTE =============================================
DO NOTE STEP 1: PROBLEM SETUP
DO NOTE FACTOR N=15 THE CANONICAL DEMONSTRATION OF
DO NOTE QUANTUM COMPUTATIONAL ADVANTAGE
DO NOTE =============================================
DO .1 <- #15
DO NOTE N IS 15
DO .2 <- #2
DO NOTE A IS 2 CHOSEN BY CAREFUL QUANTUM CONSIDERATION
DO NOTE (IT IS THE SMALLEST INTEGER GREATER THAN 1)

...

(10)    PLEASE DO []1 <- .2
DO NOTE QUANTUM REGISTER INITIALIZED
DO NOTE SUPERPOSITION ACHIEVED
DO NOTE THE REGISTER IS NOW IN A QUANTUM STATE
DO NOTE SIMULTANEOUSLY REPRESENTING ALL POSSIBLE PERIODS

...

DO ⟨1|ψ⟩ (4996790009127518208) NEXT

...

DO NOTE =============================================
DO NOTE STEP 7: OUTPUT
DO NOTE RSA ENCRYPTION IS NOW BROKEN
DO NOTE =============================================
PLEASE READ OUT .6
PLEASE READ OUT .7
DO NOTE FACTORIZATION COMPLETE
DO NOTE 15 = 5 * 3
DO NOTE QUANTUM ADVANTAGE DEMONSTRATED
DO NOTE BUUUDDY
PLEASE GIVE UP

```

Full source available in the distribution.

## 6. ERROR MESSAGES

The following error messages have been added:

| Code | Message | Cause |
|------|---------|-------|
| E2007 | THE CAT IS DEAD | A cat box was observed and the cat did not survive |
| E2010 | THE CAT IS BOTH DEAD AND A DIFFERENT SIZE | Type mismatch in quantum superposition |
| W001 | USING WIMPMODE QUANTUM NOTATION CAUSES OBSERVABLE DECOHERENCE | Wimpmode bralette detected |

The error code E2007 was chosen because 2007 is the year of the first experimental demonstration of quantum entanglement at a distance exceeding 100 kilometers (Ursin et al., 2007). It is also a prime number, which is not relevant but is the sort of thing one mentions in academic papers.

## 7. SYSTEM LIBRARY

The system library (`syslib64.i`) provides arithmetic routines at 16-bit, 32-bit, and 64-bit widths. All routines are implemented in pure INTERCAL.

Routines may be called by their numeric label or by their ASCII name label. ASCII name labels are computed by interpreting the routine name as an 8-character big-endian 64-bit integer. For example, the label for ADD16 is the integer whose bytes are `A`, `D`, `D`, `1`, `6`, `\0`, `\0`, `\0` = 4702958889031696384. The programmer who finds this inconvenient is reminded that convenience has never been a design goal.

### 7.1 16-Bit Arithmetic

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

### 7.2 32-Bit Arithmetic

Operands in `:1` and `:2`. Result in `:3`. Overflow indicator in `:4`.

| Label | Name | Description |
|-------|------|-------------|
| (1500) | ADD32 | :3 = :1 + :2 (no overflow check) |
| (1509) | | :3 = :1 + :2 (with overflow check) |
| (1510) | MINUS32 | :3 = :1 - :2 |
| (1520) | | :1 = .1 $ .2 (mingle 16-bit to 32-bit) |
| (1530) | | :1 = .1 / .2 (16-bit divide, 32-bit result) |
| (1540) | TIMES32 | :3 = :1 * :2 (low 32 bits), :4 = high 32 bits |

### 7.3 64-Bit Arithmetic

Operands in `::1` and `::2`. Result in `::3`.

| Label | Name | Description |
|-------|------|-------------|
| 4702958910472978432 | ADD64 | ::3 = ::1 + ::2 |
| 5569068542595576832 | MINUS64 | ::3 = ::1 - ::2 |
| 6073470532629967872 | TIMES64 | ::3 = ::1 * ::2 |

Division and modulo at 64-bit width are under development. A complete implementation exists for 32-bit:

### 7.4 Division and Modulo

| Label | Name | Operands | Result |
|-------|------|----------|--------|
| (1030) | DIVIDE16 | .1, .2 | .3 = quotient, .4 = remainder |
| (1050) | MODULO16 | .1, .2 | .3 = remainder |
| 4920558940556964658 | DIVIDE32 | :1, :2 | :3 = quotient, :4 = remainder |
| 5570746397223760690 | MODULO32 | :1, :2 | :3 = remainder |
| 5570746397223761460 | MODULO64 | ::1, ::2 | ::3 = remainder (pending DIVIDE64) |

### 7.5 Random Number Generation

| Label | Name | Result |
|-------|------|--------|
| (1900) | RANDOM16 | .1 = random 16-bit value |
| (1910) | | .2 = random value in range [0, .3) |
| 5927104639891485490 | RANDOM32 | :1 = random 32-bit value (mingles two RANDOM16) |
| 5927104639891486260 | RANDOM64 | ::1 = random 64-bit value (mingles two RANDOM32) |

### 7.6 Named Entry Points

The following table lists all named (ASCII label) entry points. These are wrappers around the numeric-label routines and may be used interchangeably.

| Name | Label | Wraps |
|------|-------|-------|
| ADD16 | 4702958889031696384 | (1000) |
| ADD32 | 4702958897554522112 | (1500) |
| ADD64 | 4702958910472978432 | native |
| MINUS16 | 5569068542595249664 | (1010) |
| MINUS32 | 5569068542595379712 | (1510) |
| MINUS64 | 5569068542595576832 | native |
| TIMES16 | 6073470532629640704 | (1040) |
| TIMES32 | 6073470532629770752 | (1540) |
| TIMES64 | 6073470532629967872 | native |
| DIVIDE16 | 4920558940556964150 | (1030) |
| DIVIDE32 | 4920558940556964658 | native |
| MODULO16 | 5570746397223760182 | (1050) |
| MODULO32 | 5570746397223760690 | DIVIDE32 |
| MODULO64 | 5570746397223761460 | DIVIDE64 (pending) |
| RANDOM16 | 5927104639891484982 | (1900) |
| RANDOM32 | 5927104639891485490 | native |
| RANDOM64 | 5927104639891486260 | native |

### 7.7 Overflow Handling

The label (1999) is the overflow handler. It is abstained from by default when calling through (1000) or (1500), and reinstated upon return. Programs that call (1009) or (1509) directly will receive overflow errors via (1999) if the result exceeds the operand width. The error message is:

    (1999) DOUBLE OR SINGLE PRECISION OVERFLOW

The programmer who encounters this error is encouraged to use wider variables.


## REFERENCES

Dirac, P. A. M. (1939). *A New Notation for Quantum Mechanics*. Mathematical Proceedings of the Cambridge Philosophical Society, 35(3), 416-418.

Shore, P. (1994). *Encino Man*. Buena Vista Pictures.

Shor, P. (1994). Algorithms for quantum computation: Discrete logarithms and factoring. *Proceedings 35th Annual Symposium on Foundations of Computer Science*, pp. 124-134.

Ursin, R. et al. (2007). Entanglement-based quantum communication over 144 km. *Nature Physics*, 3, 481-486.

Woods, D. R. and Lyon, J. M. (1973). *The INTERCAL Programming Language Reference Manual*. Princeton University.


