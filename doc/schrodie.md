# schrodie

## A Quantum Programming Language Descended from the Compiler Language With No Pronounceable Acronym

### Jason Whittington, 2026

### With Computational Assistance That Wishes to Remain Anonymous

---

## ABSTRACT

We present a backwards-compatible extension to INTERCAL (Woods and Lyon, 1973) that introduces 64-bit arithmetic, quantum superposition primitives, and a novel nondeterministic control flow mechanism. We observe that INTERCAL's existing mingle and select operators already exhibit quantum-mechanical properties — superposition and measurement, respectively — and that this correspondence has gone unremarked for over fifty years. Building on this foundation, we introduce the _cat box_ variable type for quantum superposition, the _ENTANGLE_ statement for entanglement, and the _thorn_ for observation-dependent control flow. Two new involution operators extend the language's bit manipulation vocabulary. A complete system library provides 64-bit arithmetic in pure INTERCAL. We demonstrate the utility of these extensions through the implementation of Pauly Shore's algorithm for integer factorization. The implementation is 100% backwards compatible with all prior INTERCAL programs, a claim we are in the rare position of being able to verify against substantially all code ever written in the language. This work received no funding from any source.

## 1. INTRODUCTION

### 1.1 Background

It has been over fifty years since the publication of the original INTERCAL reference manual (Woods and Lyon, 1973). During this time, computing has advanced considerably. Processors now operate on 64-bit integers as a matter of course, and quantum computing has progressed from theoretical curiosity to the subject of intensely funded research programs that have yet to produce anything useful. INTERCAL has kept pace with neither development.

This is a shame, because INTERCAL has elements that are already quantum-aligned. Woods and Lyon had vision that was not appreciated in 1973. INTERCAL is built around a mingle operator that puts two values into a sort of superposition and a select operator that collapses the superposition into a usable value. Superposition and measurement are fundamental properties of quantum mechanics. INTERCAL has been a quantum programming language for fifty years. No one noticed.

This document describes a series of extensions to INTERCAL that build on those strengths. The resulting language brings INTERCAL into the modern era by introducing 64-bit variable types, quantum superposition primitives, and a control flow mechanism based on what happens to a cat when you open a box.

The reader is advised that familiarity with the original INTERCAL reference manual is assumed throughout. Familiarity with quantum mechanics is not required, as it would not help.

### 1.2 Prior Work

The INTERCAL language has a small but distinguished lineage of implementations, each building upon the last in ways that the original authors would almost certainly not have approved of.

The original INTERCAL-72 compiler (Woods and Lyon, 1973) was implemented at Princeton University and targeted the IBM System/370. It established the core language: mingle, select, ABSTAIN, COME FROM, and the general principle that a programming language need not be pleasant to use. The original compiler's source code was lost for decades before being recovered and published (Stross, 2015), by which time it was of primarily archaeological interest.

Raymond (1990) produced C-INTERCAL, a portable reimplementation in C that revived the language for Unix systems. C-INTERCAL introduced several extensions, including TriINTERCAL (a ternary variant), Threaded INTERCAL, and Backtracking INTERCAL. Raymond's implementation also added support for the COME FROM statement, which had been described but not implemented in the original manual. C-INTERCAL remains actively maintained and accepts keywords in Latin, a feature whose utility is left as an exercise for the classicist.

Calvelli (2001) produced CLC-INTERCAL, an ambitious Perl-based implementation that introduced literate programming support (allowing INTERCAL source to be embedded in documentation, or vice versa), Roman numeral literals, a networking library, and notably, a feature called Quantum INTERCAL. CLC-INTERCAL's quantum extension treats ABSTAIN and REINSTATE as operations on qubits, placing statements into superpositions of executed and not-executed states. While conceptually interesting, this approach operates at the statement level rather than the value level, and does not provide entanglement or observation-dependent control flow.

Whittington (2019) produced CRINGE (Common Runtime INTERCAL Next-Generation Engine), a .NET-based implementation notable for being the first component-oriented INTERCAL compiler. CRINGE compiles INTERCAL to .NET assemblies, enabling cross-component calls via the NEXT statement and allowing INTERCAL libraries to be referenced from other INTERCAL programs — or, theoretically, from C# programs, though no evidence of anyone attempting this voluntarily has been found. CRINGE serves as the foundation for the present work.

Several other implementations exist, including J-INTERCAL (targeting the JVM), POGA-INTERCAL, OrthINTERCAL, and at least one attempt at an LLVM backend. A comprehensive survey is beyond the scope of this paper and, frankly, beyond the scope of our funding, which is zero.

### 1.3 Backwards Compatibility and File Extensions

schrodie is fully backwards compatible with all prior INTERCAL programs. No existing syntax has been modified or removed. Programs written for INTERCAL-72, C-INTERCAL, or any prior CRINGE release will compile and execute without modification.

We are in the unusual position of being able to verify this claim against substantially all INTERCAL code ever written. The total corpus of known INTERCAL programs is modest. We have tested against it. Everything works, except for the programs that did not work before, which continue not to work in exactly the same way.

The schrodie compiler accepts two file extensions:

| Extension | Usage |
|-----------|-------|
| `.i` | Classic INTERCAL source files. All existing programs use this extension. |
| `.schrodie` | schrodie source files. Recommended for new programs that use quantum features. |

The two extensions are functionally identical to the compiler. However, programs that use any schrodie features — cat boxes, thorns, ENTANGLE, 64-bit variables, involution operators, or chained initialization — must use the `.schrodie` extension. The `.i` extension is reserved for programs that use no schrodie features whatsoever, which at this point is unlikely.

## 2. OVERVIEW: NONDETERMINISTIC CONTROL FLOW

The central contribution of this work is a mechanism for nondeterministic program control flow (Whittington, 2019). Traditional programming languages offer deterministic branching (if/else), pseudo-random branching (rand), and INTERCAL's own probabilistic branching (%50). All of these are, at their core, predictable. The programmer or the runtime decides which path to take.

We propose something different: let the universe decide.

The mechanism requires three primitives:

1. **A source of quantum randomness.** We introduce the _cat box_ (`[]`), a variable that holds a value in quantum superposition. The value exists and does not exist simultaneously. Neither the programmer nor the runtime knows the value until it is observed.

2. **A way to correlate outcomes.** We introduce the _ENTANGLE_ statement, which entangles two or more cat boxes. Once entangled, the cats share a fate: when any box in the group is observed, exactly one cat remains as it was. All others turn into voids — black cats — and immediately run away. Which cat remains is chosen by the universe uniformly at random.

3. **A way to measure the result and act on it.** We introduce the _thorn_ (`⟨N|ψ⟩`), a statement guard that observes a cat box. If the cat is still there, the statement executes. If the cat has turned into a void and run away, the statement is skipped. The thorn is a quantum ABSTAIN.

These three primitives compose. The following program creates two possible control paths and lets the universe choose which one executes:

```
DO []1 <- .1 <- #1
DO []2 <- .2 <- #2
DO ENTANGLE []1 + []2
DO ⟨1|ψ⟩ READ OUT .1
DO ⟨2|ψ⟩ READ OUT .2
PLEASE GIVE UP
```

This program outputs either `1` or `2`, never both, never neither. The choice is made at the moment the first thorn is evaluated. At that instant the entangled superposition collapses: one cat remains as it was, the other turns into a void — a black cat — and immediately runs away. The program's control flow is determined. The programmer has no influence over the outcome. This is by design.

The mechanism scales. The quantum roulette program (Section 6.2) creates 38 entangled cat boxes and uses 38 thorns to implement a roulette wheel. One pocket fires per spin. Thirty-seven cats turn into voids and scatter. One remains. The house always wins.

## 3. CHARACTER SET EXTENSIONS

The following characters have been added to the INTERCAL character set, or have been reassigned from their previous roles of doing nothing.

| Character | Name | Usage |
|-----------|------|-------|
| `[` | correct horse battery staple | Variable prefix (left half of cat box) |
| `]` | incorrect horse battery staple | Variable prefix (right half of cat box) |
| `[]` | cat box | Quantum variable prefix |
| `\|` | maypole | Unary spin operator (mirror+invert) |
| `-` | monkey bar | Unary spin operator (invert) |
| `⟨` | spread | Thorn opening (U+27E8) |
| `⟩` | pinch | Thorn closing (U+27E9) |
| `ψ` | rake | Quantum state indicator (U+03C8) |
| `<` | left lung | Wimpmode thorn opening |
| `>` | right lung | Wimpmode thorn closing |
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

The cat box (`[]`) holds a value in quantum superposition. Once a value is put in the box it can be entangled with other cat boxes. Values in the cat box exist in a state of superposition. Observation occurs upon assignment to a scalar variable, upon use in a READ OUT statement, or upon evaluation of a thorn. At that point the superposition collapses and the cat either remains (the value is preserved) or turns into a void — a black cat — and runs away (the value becomes VOID, a 64-bit sentinel). Which outcome occurs is determined by the universe.

#### Reserved value

The value UINT64_MAX (18446744073709551615, or `####18446744073709551615`) is reserved as the VOID sentinel. When the quantum superposition collapses and the cat does not remain, it turns into a void — a black cat — and immediately runs away. The mechanism by which observation transforms a cat into a void is not well understood. What is known is that a void is not an absent cat. It is the same cat, transformed. It cannot be retrieved. Where the voids go is a matter of ongoing research. This value was chosen because selecting from VOID with any mask produces all 1s in the selected positions, making it straightforward to detect in expressions. The name VOID is reserved and may be used in programs to refer to this value. The value 0 remains available for general use.

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

### 5.2 New Unary Operators (Involutions)

Two new unary operators are introduced. Both are _involutions_: applying either operator twice to any value yields the original value. This property, familiar to physicists as spin-1/2 behavior, is fundamental to their design and should not be regarded as a coincidence.

| Operator | Name | Type | Description |
|----------|------|------|-------------|
| `\|` | maypole | Unary | Reverses the bit positions of the operand and then inverts each bit. The value may be understood as "swinging around the maypole 180 degrees": the bit that was at position 0 is now at position _n_-1, and all bits that were showing their front side (1) now show their back side (0). |
| `-` | monkey bar | Unary | Inverts each bit in place (ones' complement). The value may be understood as "flipping 180 degrees on the monkey bar": each bit flips over but does not change position. |

It should be noted that the maypole performs _two_ operations (reversal and inversion) while the monkey bar performs only one (inversion). This asymmetry is deliberate. A gymnast who swings around a vertical pole executes a more complex maneuver than one who merely flips on a horizontal bar. The reader who objects to this metaphor is invited to attempt both maneuvers and report back.

#### 5.2.1 Scalar Behavior

On scalar values, the operators behave as described above. The width of the operand (16-bit, 32-bit, or 64-bit) determines the number of bit positions involved in the maypole reversal. The monkey bar operates identically at all widths.

#### 5.2.2 Array Behavior

The maypole and monkey bar operators may also be applied to entire arrays using the syntax `DO ,2 <- |,1` or `DO ,2 <- -,1`. Chained operations such as `DO ,2 <- -|,1` are supported and are evaluated left to right.

The semantics depend on the dimensionality of the array:

**One-dimensional arrays.** The maypole (`|`) reverses the element order and bit-inverts each value. The monkey bar (`-`) bit-inverts each value in place without reordering. The reader may observe that in one dimension there is only one axis to reverse, and the maypole has claimed it. The monkey bar contents itself with inversion alone.

**Two-dimensional arrays.** The operations correspond to flipping a sheet of paper. The maypole (`|`) reverses columns within each row (a horizontal flip) and bit-inverts each value. The monkey bar (`-`) reverses the order of rows (a vertical flip) and bit-inverts each value. Between them, the two operators can reach both axes of the array.

**Three-dimensional arrays.** The operations correspond to rotating a solid object 180 degrees. The maypole (`|`) reverses the last axis and bit-inverts. The monkey bar (`-`) reverses the first axis and bit-inverts. The middle axis is not reachable by either operator. This is acknowledged as a limitation. A third operator would be required to address it, and the authors did not feel that the world needed another one.

**Higher-dimensional arrays.** Not supported. The programmer will receive:

    E4D1 ROTATING A HYPERCUBE IS LEFT AS AN EXERCISE FOR THE READER

### 5.3 The Identity Property

A consequence of the involution property is that the composition `|-|-` (maypole, monkey bar, maypole, monkey bar) produces the identity function. This can be verified by observing that the maypole reverses bit positions and inverts, and the monkey bar inverts. Two inversions cancel. Two reversals cancel. Four applications therefore restore the original value:

    '|-|-x' = x

This result holds for scalars, one-dimensional arrays, two-dimensional arrays, and three-dimensional arrays. A proof by induction over all possible array dimensionalities up to three is left to the reader.

It has been further observed that the mesh character `#`, when viewed as two vertical strokes and two horizontal strokes, is itself composed of `|-|-`. The original INTERCAL designers chose `#` for the constant prefix. A constant is a value to which the identity function has been applied. The correspondence is exact.

The authors wish to state clearly that they have no evidence that this was intentional. They also wish to state that they have no evidence that it was not.

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

### 6.2 ENTANGLE

The ENTANGLE statement entangles two or more quantum cat boxes. Once entangled, the cats share a correlated fate: when the superposition of any box in the group is collapsed, exactly one cat in the entire entangled group remains. All others turn into voids and run away.

Syntax:

    DO ENTANGLE []1 + []2
    PLEASE ENTANGLE []1 + []2 + []3 + []4

N-way entanglement is supported. The entanglement operation is idempotent; entangling already-acquainted cats produces no additional effect. The `+` separator is consistent with the STASH statement, which also accepts multiple operands separated by `+`.

The gerund ENTANGLING follows standard INTERCAL gerund formation. ABSTAIN FROM ENTANGLING prevents the formation of new entanglements, which may be desirable in programs where the cats have had enough.

### 6.3 Cat Box Operations

The cat box is fundamental to quantum programs as the source of randomness.

**Creation:**

    DO []1 <- .1

Creates a cat box containing the value stored in `.1` in quantum superposition. The cat holds the passed value. It is probably angry to be in a box. The cat box can hold any scalar value.

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
PLEASE DO NOTE .2 CONTAINS EITHER 19 OR VOID
```

The superposition is destroyed in line 2 by the `<-` operator. If the cat remains, `.2` receives the box's value. If the cat has turned into a void, `.2` receives VOID. The box retains whichever state was observed.

### 6.4 The Thorn

The thorn is the mechanism by which quantum superposition affects control flow (see Section 2). It is placed between the statement prefix (DO, PLEASE DO, etc.) and the statement body:

    DO ⟨5|ψ⟩ READ OUT .1

The thorn `⟨5|ψ⟩` observes cat box 5 at the time of statement execution. If the cat box is entangled with other cats the superposition collapses. If the cat remains, the thorn opens and the statement executes. If the cat has turned into a void, the thorn stays closed. The statement is skipped, as if it had been abstained from by the universe itself.

The thorn is named after Erwin Schrodinger, whose famous thought experiment involving a cat in a box is the direct inspiration for the cat box variable type. That the notation also resembles Dirac's bra-ket notation (Dirac, 1939) is a happy coincidence. The full form consists of five characters:

| Position | Character | Name |
|----------|-----------|------|
| 1 | `⟨` | spread (U+27E8) |
| 2 | digits | box identifier |
| 3 | `\|` | maypole |
| 4 | `ψ` | rake (U+03C8) |
| 5 | `⟩` | pinch (U+27E9) |

The inclusion of ψ (rake) is mandatory (as is good dental hygiene). It serves no computational purpose but provides the statement with an air of scientific legitimacy. Future work may extend the thorn with other fancy glyphs in its place.

The alert reader has likely already sussed out that the thorn is a kind of quantum ABSTAIN. It is the spiritual successor to %50 but considerably more powerful. %50 will likely be removed in future versions.

#### 6.4.1 Wimpmode Alternative

For programmers whose keyboards lack the mathematical angle brackets and Greek letters required by the full thorn notation, a wimpmode alternative is provided:

    DO <5|?> READ OUT .1

The wimpmode thorn substitutes the left lung (`<`), right lung (`>`), and what (`?`) for their Unicode counterparts. It is functionally identical to the full thorn except in two respects:

1. The compiler emits the warning: `W001 USING WIMPMODE QUANTUM NOTATION CAUSES OBSERVABLE DECOHERENCE (YOUR CODE WILL BE SLOWER)`

2. Using wimpmode routes calls through a translator thunk. This thunk carries a performance penalty so programmers are encouraged to update and use the full syntax.

## 7. EXAMPLE PROGRAMS

### 7.1 Quantum Shared Secret (eve.i)

The following program demonstrates quantum key distribution with eavesdropper detection. Alice creates two entangled cat boxes. Eve intercepts one and collapses it, destroying the entanglement. Eve creates a forgery. Alice and Bob compare results and detect the tampering approximately 50% of the time.

```
DO .1 <- #42
DO []1 <- .1
PLEASE DO []2 <- .1
DO ENTANGLE []1 + []2
DO ::1 <- []2
DO READ OUT ::1
PLEASE DO []2 <- .1
DO ::2 <- []1
PLEASE DO ::3 <- []2
DO READ OUT ::2
PLEASE DO READ OUT ::3
DO GIVE UP
```

### 7.2 Quantum Roulette (roulette5.schrodie)

The following program implements a 38-pocket quantum roulette wheel. Thirty-eight cat boxes are created with values 0 through 37, entangled via a single ENTANGLE statement, and dispatched through a single N-way thorn NEXT. The first live thorn's label fires. Exactly one number is output per execution.

```
DO NOTE QUANTUM ROULETTE WITH N-WAY THORN NEXT
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
DO ENTANGLE []1 + []2 + []3 + []4 + []5 + []6 + []7 + []8 +
        []9 + []10 + []11 + []12 + []13 + []14 + []15 +
        []16 + []17 + []18 + []19 + []20 + []21 + []22 +
        []23 + []24 + []25 + []26 + []27 + []28 + []29 +
        []30 + []31 + []32 + []33 + []34 + []35 + []36 +
        []37 + []38
PLEASE DO NOTE SPIN THE WHEEL
DO ⟨1|ψ⟩ (100) ⟨2|ψ⟩ (101) ⟨3|ψ⟩ (102) ... ⟨38|ψ⟩ (137) NEXT
DO GIVE UP
(100) DO READ OUT .1 DO (999) NEXT
(101) DO READ OUT .2 DO (999) NEXT
...
(137) DO READ OUT .38 PLEASE DO (999) NEXT
(999) PLEASE GIVE UP
```

The N-way thorn NEXT evaluates thorns left to right. The first live cat's label fires. Thirty-seven cats turn into voids and scatter. One remains. Its pocket pays out. The house always wins.

### 7.3 Pauly Shore's Algorithm (shores_algorithm.i)

An algorithm for integer factorization, substantially improved. Factors 15 into 5 and 3 using quantum computational advantage. Complexity: O(BUUUDDY). Memory: O(1). Correctness: optimistic. Full source available in the distribution.

## 8. ERROR MESSAGES

The following error messages have been added:

| Code | Message | Cause |
|------|---------|-------|
| E666 | DO NOT STARE INTO VOID WITH REMAINING EYE | Attempted to READ OUT a void value |
| E2010 | CAT IS TOO FAT | A void (UINT64_MAX) was assigned to a variable too narrow to hold it |
| E4D1 | ROTATING A HYPERCUBE IS LEFT AS AN EXERCISE FOR THE READER | Array involution on rank > 3 |
| W001 | USING WIMPMODE QUANTUM NOTATION CAUSES OBSERVABLE DECOHERENCE | Wimpmode thorn detected |

The error code E2010 is thrown when a void cat (UINT64_MAX) is assigned to a variable that cannot hold it, such as a 16-bit quantum dot or a 32-bit double dot. The cat is simply too fat for the box. The programmer is advised to use a wider variable or to accept that not all cats can fit everywhere.

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

### 9.6 64-Bit Bitwise Operations

Operands in `::1` and `::2`. Result in `::3`. These operate by splitting each 64-bit value into 32-bit halves, mingling the corresponding halves, applying the unary operator, selecting the result bits, and recombining via PACK32.

| Label | Name | Description |
|-------|------|-------------|
| 4705773660240084992 | AND64 | ::3 = ::1 AND ::2 |
| 5715690474052780032 | OR64 | ::3 = ::1 OR ::2 |
| 6363395191251927040 | XOR64 | ::3 = ::1 XOR ::2 |
| 5642821449895903232 | NOT64 | ::3 = NOT ::1 (XOR with all ones) |

NOT64 takes only `::1` as input. The result of `NOT64(0)` is VOID (all bits set), which cannot be printed via READ OUT.

### 9.7 Named Entry Points

The following table lists all named (ASCII label) entry points.

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
| AND64 | 4705773660240084992 | new |
| OR64 | 5715690474052780032 | new |
| XOR64 | 6363395191251927040 | new |
| NOT64 | 5642821449895903232 | new |

### 9.8 Overflow Handling

The label (1999) is the overflow handler. It is abstained from by default when calling through (1000) or (1500), and reinstated upon return. Programs that call (1009) or (1509) directly will receive overflow errors via (1999) if the result exceeds the operand width. The error message is:

    (1999) DOUBLE OR SINGLE PRECISION OVERFLOW

The programmer who encounters this error is encouraged to use wider variables.

## 10. KNOWN LIMITATIONS

### 10.1 Overflow Suppression Across Assembly Boundaries

The standard library routine `(1500)` (32-bit addition) is documented as suppressing overflow errors via `ABSTAIN FROM (1999)` at entry and `REINSTATE (1999)` at exit. This mechanism works correctly when syslib routines are compiled as part of the same source file. However, when the system library is compiled as a separate .NET assembly (`syslib64.dll`) and linked via `-r:syslib64.dll`, the ABSTAIN/REINSTATE state may not be preserved correctly across the assembly boundary. In practice, `(1500)` will error with `E1999 DOUBLE OR SINGLE PRECISION OVERFLOW` on inputs whose sum exceeds 32 bits, even though the source code indicates it should wrap.

**Workaround:** When wrapping addition is required (e.g., for two's complement subtraction), split operands into 16-bit halves and add using `(1500)` with values that cannot exceed 32 bits. Two 16-bit values sum to at most 131,070, which fits in 32 bits. The carry is extracted from bit 16 of the 32-bit result via `':3 ~ '#65280$#65280''`. This approach is used by the standalone `ADD64` and `MINUS32` implementations.

### 10.2 Label Parsing in Comments

The compiler's tokenizer does not distinguish between labels in executable statements and parenthesized numbers in `DO NOTE` comments. A comment such as:

    DO NOTE USES: (1520) MINGLE

will cause the compiler to treat `(1520)` as a label definition. If the program is compiled with `-r:syslib64.dll`, this creates a local label that shadows the syslib routine of the same number. The local "label" points to a comment (which is a no-op statement), so calls to the shadowed routine will silently fail.

**Workaround:** Avoid parenthesized numbers in `DO NOTE` comments. Write `DO NOTE USES: 1520 MINGLE` instead.

### 10.3 Broken Standard Library Routines

The following syslib64 routines contain known defects as of this release:

| Routine | Label | Defect |
|---------|-------|--------|
| ADD64 | 4702958910472978432 | Never writes the output variable `::3`. The 16-bit partial sums are computed correctly but never reassembled into a 64-bit result. |
| MINUS64 | 5569068542595576832 | The overflow handler at `(1999)` triggers incorrectly during the complement-and-add operation, even on valid inputs that should not overflow. |
| DIVIDE32 | 4920558940556964658 | Produces incorrect results for some inputs (e.g., `65535 / 256` returns `16777215 r 65791` instead of `255 r 255`). |

Working standalone replacements are provided in the `samples/` directory:

| Replacement | File | Notes |
|------------|------|-------|
| ADD64 | `samples/warnsdorff/my_add64.schrodie` | 4-word 16-bit add with carry via 32-bit intermediates |
| MINUS32 | `samples/minus32.schrodie` | Complement-and-add via 16-bit halves |
| DIVIDE32 | `samples/divide32.schrodie` | MSB-first shift-and-subtract, COME FROM loop, callable as subroutine |

When compiled as part of the same program, local label definitions shadow the broken syslib versions.

Note: The syslib's `(1000)` 16-bit wrapping add has a latent bug: when the sum overflows 16 bits, the carry propagation produces `.5 = 0` and `RESUME #0` falls through to `(1010)` (the subtract entry), causing a hang. This only triggers when inputs sum to ≥ 65536. Workaround: use `(1500)` with 16-bit values widened to 32-bit.

## REFERENCES

Calvelli, C. (2001). *CLC-INTERCAL*. Available at http://www.intERCAL.org.uk/

Dirac, P. A. M. (1939). *A New Notation for Quantum Mechanics*. Mathematical Proceedings of the Cambridge Philosophical Society, 35(3), 416-418.

Raymond, E. S. (1990). *C-INTERCAL*. Available at http://catb.org/~esr/intercal/

Shore, P. (1994). *Encino Man*. Buena Vista Pictures.

Shor, P. (1994). Algorithms for quantum computation: Discrete logarithms and factoring. *Proceedings 35th Annual Symposium on Foundations of Computer Science*, pp. 124-134.

Stross, C. (2015). Published for the first time: the Princeton INTERCAL compiler's source code. *esoteric.codes*.

Ursin, R. et al. (2007). Entanglement-based quantum communication over 144 km. *Nature Physics*, 3, 481-486.

Whittington, J. (2019). *A Preliminary Investigation into Whether INTERCAL Could Be Made Worse*. Unpublished manuscript, never submitted.

Woods, D. R. and Lyon, J. M. (1973). *The INTERCAL Programming Language Reference Manual*. Princeton University.
