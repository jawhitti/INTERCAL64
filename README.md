# INTERCAL-64

## A 64-Bit Extension to the Compiler Language With No Pronounceable Acronym

### Jason Whittington, 2026

### With Computational Assistance That Wishes to Remain Anonymous

---

## ABSTRACT

We present a backwards-compatible extension to INTERCAL (Woods and Lyon, 1973) that introduces 64-bit arithmetic, two new involution operators, and a complete system library implemented in pure INTERCAL. The resulting language is capable of expressing nontrivial algorithms including graph traversal, spatial indexing, and combinatorial optimization — achievements previously assumed to be beyond INTERCAL's reach. A formal proof demonstrates that INTERCAL-72's NEXT/RESUME control flow cannot express callable subroutines containing arbitrary-length loops, and that the COME FROM statement is computationally necessary to overcome this limitation. The implementation is 100% backwards compatible with all prior INTERCAL programs, a claim we are in the rare position of being able to verify against substantially all code ever written in the language. This work received no funding from any source.

## 1. INTRODUCTION

### 1.1 Background

It has been over fifty years since the publication of the original INTERCAL reference manual (Woods and Lyon, 1973). During this time, computing has advanced considerably. Processors now operate on 64-bit integers as a matter of course. INTERCAL has not kept pace.

This is a shame, because INTERCAL has elements that deserve wider study. Woods and Lyon built a language around a mingle operator that interleaves bits and a select operator that extracts them — operations that map directly to Morton codes, spatial hashing, and the kind of bit manipulation that modern processors still struggle with. INTERCAL has been a bit-parallel programming language for fifty years. No one noticed.

This document describes a series of extensions to INTERCAL that build on those strengths. The resulting language brings INTERCAL into the modern era by introducing 64-bit variable types, new operators, and a system library that makes arithmetic possible without external assistance.

The reader is advised that familiarity with the original INTERCAL reference manual is assumed throughout.

### 1.2 Prior Work

The INTERCAL language has a small but distinguished lineage of implementations, each building upon the last in ways that the original authors would almost certainly not have approved of.

The original INTERCAL-72 compiler (Woods and Lyon, 1973) was implemented at Princeton University and targeted the IBM System/370. It established the core language: mingle, select, ABSTAIN, COME FROM, and the general principle that a programming language need not be pleasant to use. The original compiler's source code was lost for decades before being recovered and published (Stross, 2015), by which time it was of primarily archaeological interest.

Raymond (1990) produced C-INTERCAL, a portable reimplementation in C that revived the language for Unix systems. C-INTERCAL introduced several extensions, including TriINTERCAL (a ternary variant), Threaded INTERCAL, and Backtracking INTERCAL. Raymond's implementation also added support for the COME FROM statement, which had been described but not implemented in the original manual. C-INTERCAL remains actively maintained and accepts keywords in Latin, a feature whose utility is left as an exercise for the classicist.

Calvelli (2001) produced CLC-INTERCAL, an ambitious Perl-based implementation that introduced literate programming support, Roman numeral literals, a networking library, and a feature called Quantum INTERCAL. CLC-INTERCAL's quantum extension treats ABSTAIN and REINSTATE as operations on qubits, placing statements into superpositions of executed and not-executed states.

Whittington (2019) produced CRINGE (Common Runtime INTERCAL Next-Generation Engine), a .NET-based implementation notable for being the first component-oriented INTERCAL compiler. CRINGE compiles INTERCAL to .NET assemblies, enabling cross-component calls via the NEXT statement and allowing INTERCAL libraries to be referenced from other INTERCAL programs — or, theoretically, from C# programs, though no evidence of anyone attempting this voluntarily has been found. CRINGE serves as the foundation for the present work.

Several other implementations exist, including J-INTERCAL (targeting the JVM), POGA-INTERCAL, OrthINTERCAL, and at least one attempt at an LLVM backend. A comprehensive survey is beyond the scope of this document and, frankly, beyond the scope of our funding, which is zero.

### 1.3 Backwards Compatibility and File Extensions

INTERCAL-64 is fully backwards compatible with all prior INTERCAL programs. No existing syntax has been modified or removed. Programs written for INTERCAL-72, C-INTERCAL, or any prior CRINGE release will compile and execute without modification.

We are in the unusual position of being able to verify this claim against substantially all INTERCAL code ever written. The total corpus of known INTERCAL programs is modest. We have tested against it. Everything works, except for the programs that did not work before, which continue not to work in exactly the same way.

The compiler accepts two file extensions:

| Extension | Usage |
|-----------|-------|
| `.i` | Classic INTERCAL source files. All existing programs use this extension. |
| `.ic64` | INTERCAL-64 source files. Recommended for new programs that use 64-bit features. |

The two extensions are functionally identical to the compiler.

## 2. CHARACTER SET EXTENSIONS

### 2.1 Labels

Many applications have fallen victim to label clash due to many authors using labels with values < 5000. Labels have been extended to 64 bits so that programs exceeding 119 lines are assured of having enough labels. The reader is probably aware that 64-bit values take more space. Users are encouraged to avoid labels like `(10) DO <whatever>` and are encouraged to prefer `(744073709551615) DO <whatever>`. Hopefully this will lead to fewer integration problems in the future. Please note that the authors of this paper did test the label 744073709551615 but did not test all labels available. Care has been taken to only use labels with values > INT_MAX for all new library code included in this release.

## 3. VARIABLES AND CONSTANTS

### 3.1 Variable Types

All classic INTERCAL variable types are supported along with new 64-bit types:

| Prefix | Name | Width | Description |
|--------|------|-------|-------------|
| `.` | quantum dot | 16-bit | A 16-bit unsigned integer |
| `:` | double dot | 32-bit | A 32-bit unsigned integer |
| `::` | double cateye | 64-bit | A 64-bit unsigned integer |
| `,` | tail | 16-bit | A 16-bit array |
| `;` | hybrid | 32-bit | A 32-bit array |
| `;;` | double hybrid | 64-bit | A 64-bit array |

The quantum dot (`.`) replaces spot (`.`) as the indicator for variable declarations. Single-dot (`.`) and double-dotted (`:`) variables replace their classic INTERCAL equivalents and the new type double cateye extends the existing quantum dot (`.`, 16-bit) and double dot (`:`, 32-bit) series to 64-bit precision. It is so named because it consists of two double dots, and two twos is four.

### 3.2 Constants

Constants are formed with the mesh (`#`) prefix:

| Prefix | Name | Width |
|--------|------|-------|
| `#` | mesh | 16-bit |
| `##` | insecure fence | 32-bit |
| `####` | secure fence | 64-bit |

The absence of a triple mesh (`###`) is intentional. Three meshes would imply 48-bit precision, which is not a thing.

## 4. OPERATORS

### 4.1 Existing Operators

The five original INTERCAL operators are retained:

| Operator | Name | Type | Description |
|----------|------|------|-------------|
| `$` | big money | Binary | Interleaves bits of two operands |
| `~` | select | Binary | Extracts bits using a mask |
| `&` | ampersand | Unary | AND of adjacent bit pairs |
| `V` | V | Unary | OR of adjacent bit pairs |
| `?` | what | Unary | XOR of adjacent bit pairs |

### 4.2 New Unary Operators (Involutions)

Two new unary operators are introduced. Both are _involutions_: applying either operator twice to any value yields the original value. This property, familiar to physicists as spin-1/2 behavior, is fundamental to their design and should not be regarded as a coincidence.

| Operator | Name | Type | Description |
|----------|------|------|-------------|
| `\|` | maypole | Unary | Reverses the bit positions of the operand and then inverts each bit. The value may be understood as "swinging around the maypole 180 degrees": the bit that was at position 0 is now at position _n_-1, and all bits that were showing their front side (1) now show their back side (0). |
| `-` | monkey bar | Unary | Inverts each bit in place (ones' complement). The value may be understood as "flipping 180 degrees on the monkey bar": each bit flips over but does not change position. |

It should be noted that the maypole performs _two_ operations (reversal and inversion) while the monkey bar performs only one (inversion). This asymmetry is deliberate. A gymnast who swings around a vertical pole executes a more complex maneuver than one who merely flips on a horizontal bar. The reader who objects to this metaphor is invited to attempt both maneuvers and report back.

#### 4.2.1 Scalar Behavior

On scalar values, the operators behave as described above. The width of the operand (16-bit, 32-bit, or 64-bit) determines the number of bit positions involved in the maypole reversal. The monkey bar operates identically at all widths.

#### 4.2.2 Array Behavior

The maypole and monkey bar operators may also be applied to entire arrays using the syntax `DO ,2 <- |,1` or `DO ,2 <- -,1`. Chained operations such as `DO ,2 <- -|,1` are supported and are evaluated left to right.

The semantics depend on the dimensionality of the array:

**One-dimensional arrays.** The maypole (`|`) reverses the element order and bit-inverts each value. The monkey bar (`-`) bit-inverts each value in place without reordering. The reader may observe that in one dimension there is only one axis to reverse, and the maypole has claimed it. The monkey bar contents itself with inversion alone.

**Two-dimensional arrays.** The operations correspond to flipping a sheet of paper. The maypole (`|`) reverses columns within each row (a horizontal flip) and bit-inverts each value. The monkey bar (`-`) reverses the order of rows (a vertical flip) and bit-inverts each value. Between them, the two operators can reach both axes of the array.

**Three-dimensional arrays.** The operations correspond to rotating a solid object 180 degrees. The maypole (`|`) reverses the last axis and bit-inverts. The monkey bar (`-`) reverses the first axis and bit-inverts. The middle axis is not reachable by either operator. This is acknowledged as a limitation. A third operator would be required to address it, and the authors did not feel that the world needed another one.

**Higher-dimensional arrays.** Not supported. The programmer will receive:

    E4D1 ROTATING A HYPERCUBE IS LEFT AS AN EXERCISE FOR THE READER

### 4.3 The Identity Property

A consequence of the involution property is that the composition `|-|-` (maypole, monkey bar, maypole, monkey bar) produces the identity function. This can be verified by observing that the maypole reverses bit positions and inverts, and the monkey bar inverts. Two inversions cancel. Two reversals cancel. Four applications therefore restore the original value:

    '|-|-x' = x

This result holds for scalars, one-dimensional arrays, two-dimensional arrays, and three-dimensional arrays. A proof by induction over all possible array dimensionalities up to three is left to the reader.

It has been further observed that the mesh character `#`, when viewed as two vertical strokes and two horizontal strokes, is itself composed of `|-|-`. The original INTERCAL designers chose `#` for the constant prefix. A constant is a value to which the identity function has been applied. The correspondence is exact.

The authors wish to state clearly that they have no evidence that this was intentional. They also wish to state that they have no evidence that it was not.

### 4.4 Mingle and Select at Higher Widths

The mingle operator (`$`) produces a result twice the width of its operands:

| Operands | Result |
|----------|--------|
| 16-bit `$` 16-bit | 32-bit |
| 32-bit `$` 32-bit | 64-bit |
| 64-bit `$` 64-bit | 128-bit (ephemeral) |

The 128-bit result of mingling two 64-bit values is ephemeral. It cannot be stored in any variable. It exists only long enough to be consumed by a select operator, which reduces it back to at most 64 bits. The 128-bit value is a fleeting quantum of information, briefly real and then gone. Much like a good idea in a committee meeting.

The select operator (`~`) correspondingly operates at all widths, including selecting from a 128-bit value using a 128-bit mask (which is again ephemeral and cannot be stored in a variable).

## 5. STATEMENTS

All original INTERCAL statements are retained without modification. The programmer may continue to ABSTAIN FROM CALCULATING, COME FROM unexpected places, and GIVE UP at any time.

## 6. SYSTEM LIBRARY

The system library (`syslib64.i`) provides arithmetic routines at 16-bit, 32-bit, and 64-bit widths. All routines are implemented in pure INTERCAL.

Routines may be called by their numeric label or by their ASCII name label. ASCII name labels are computed by interpreting the routine name as an 8-character big-endian 64-bit integer. For example, the label for ADD16 is the integer whose bytes are `A`, `D`, `D`, `1`, `6`, `\0`, `\0`, `\0` = 4702958889031696384. The programmer who finds this inconvenient is reminded that convenience has never been a design goal.

### 6.1 16-Bit Arithmetic

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

### 6.2 32-Bit Arithmetic

Operands in `:1` and `:2`. Result in `:3`. Overflow indicator in `:4`.

| Label | Name | Description |
|-------|------|-------------|
| (1500) | ADD32 | :3 = :1 + :2 (no overflow check) |
| (1509) | | :3 = :1 + :2 (with overflow check) |
| (1510) | MINUS32 | :3 = :1 - :2 |
| (1520) | | :1 = .1 $ .2 (mingle 16-bit to 32-bit) |
| (1530) | | :1 = .1 / .2 (16-bit divide, 32-bit result) |
| (1540) | TIMES32 | :3 = :1 * :2 (low 32 bits), :4 = high 32 bits |

### 6.3 64-Bit Arithmetic

Operands in `::1` and `::2`. Result in `::3`.

| Label | Name | Description |
|-------|------|-------------|
| 4702958910472978432 | ADD64 | ::3 = ::1 + ::2 |
| 5569068542595576832 | MINUS64 | ::3 = ::1 - ::2 |
| 6073470532629967872 | TIMES64 | ::3 = ::1 * ::2 |

### 6.4 Division and Modulo

| Label | Name | Operands | Result |
|-------|------|----------|--------|
| (1030) | DIVIDE16 | .1, .2 | .3 = quotient, .4 = remainder |
| (1050) | MODULO16 | .1, .2 | .3 = remainder |
| 4920558940556964658 | DIVIDE32 | :1, :2 | :3 = quotient, :4 = remainder |
| 5570746397223760690 | MODULO32 | :1, :2 | :3 = remainder |

### 6.5 Random Number Generation

| Label | Name | Result |
|-------|------|--------|
| (1900) | RANDOM16 | .1 = random 16-bit value |
| (1910) | | .2 = random value in range [0, .3) |
| 5927104639891485490 | RANDOM32 | :1 = random 32-bit value (mingles two RANDOM16) |
| 5927104639891486260 | RANDOM64 | ::1 = random 64-bit value (mingles two RANDOM32) |

### 6.6 64-Bit Bitwise Operations

Operands in `::1` and `::2`. Result in `::3`. These operate by splitting each 64-bit value into 32-bit halves, mingling the corresponding halves, applying the unary operator, selecting the result bits, and recombining via PACK32.

| Label | Name | Description |
|-------|------|-------------|
| 4705773660240084992 | AND64 | ::3 = ::1 AND ::2 |
| 5715690474052780032 | OR64 | ::3 = ::1 OR ::2 |
| 6363395191251927040 | XOR64 | ::3 = ::1 XOR ::2 |
| 5642821449895903232 | NOT64 | ::3 = NOT ::1 (XOR with all ones) |

### 6.7 Named Entry Points

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
| RANDOM16 | 5927104639891484982 | (1900) |
| RANDOM32 | 5927104639891485490 | new |
| RANDOM64 | 5927104639891486260 | new |
| AND64 | 4705773660240084992 | new |
| OR64 | 5715690474052780032 | new |
| XOR64 | 6363395191251927040 | new |
| NOT64 | 5642821449895903232 | new |

### 6.8 Overflow Handling

The label (1999) is the overflow handler. It is abstained from by default when calling through (1000) or (1500), and reinstated upon return. Programs that call (1009) or (1509) directly will receive overflow errors via (1999) if the result exceeds the operand width. The error message is:

    (1999) DOUBLE OR SINGLE PRECISION OVERFLOW

The programmer who encounters this error is encouraged to use wider variables.

## 7. COMPILER

The compiler is `churn`. It descends from Eric Raymond's `ick` via `cringe` (Whittington, 2019).

### 7.1 Building from Source

```
dotnet build schrodie.sln
```

### 7.2 Compiling Programs

```
churn hello.i
churn -t:library syslib64.i
churn -r:syslib64.dll stable_marriage.i
```

Source files are consumed in order and compiled into a single executable. Multiple files may be specified to combine source.

### 7.3 Library Compilation

Libraries are produced via `-t:library`. All labels are exposed publicly by default. Libraries must ensure every code path terminates in RESUME or GIVE UP.

### 7.4 Cross-Language Interop

INTERCAL libraries can be consumed from C# and vice versa. The `csharplib` sample demonstrates how to author a C# extension DLL callable from INTERCAL via DO...NEXT:

```csharp
using INTERCAL.Runtime;

[assembly: EntryPoint("(3000)", "CSIntercalLib", "foobar")]
public class CSIntercalLib
{
    public bool foobar(ExecutionContext ctx)
    {
        ctx[".3"] = ctx[".2"] + ctx[".1"];
        return false;
    }
}
```

### 7.5 DAP Debugger

INTERCAL-64 includes a Debug Adapter Protocol (DAP) debugger for Visual Studio Code. Features include:

- **Breakpoints** — set breakpoints on any INTERCAL statement
- **Step debugging** — Step In, Step Over, Continue
- **Variables panel** — inspect spots, two-spots, four-spots, and arrays in real time
- **Watch expressions** — evaluate arbitrary INTERCAL expressions against current program state
- **ABSTAIN tracking** — see which statements are currently abstained
- **COME FROM visualization** — the debugger marks COME FROM targets and shows where control will transfer
- **Debug console** — program I/O appears in real time

This is, to our knowledge, the first interactive debugger ever built for INTERCAL.

### 7.6 Component Architecture

INTERCAL-64 compiles to .NET assemblies. Cross-component NEXT/RESUME/FORGET is supported via a thread-pool-based execution model. When DO NEXT is encountered, a new thread is scheduled; RESUME signals the waiting thread to continue; FORGET terminates threads below the current stack position. This allows INTERCAL's unique stack semantics — where FORGET can drop entries from the middle of the call stack — to work across assembly boundaries.

The following restrictions apply across component boundaries:

- **COME FROM** — only legal to COME FROM a label local to the current component
- **ABSTAIN / REINSTATE** — only act on the local component (including gerunds)
- **IGNORE / REMEMBER** — cannot target labels in other components

## 8. KNOWN LIMITATIONS

### 8.1 Overflow Suppression Across Assembly Boundaries

The standard library routine `(1500)` (32-bit addition) suppresses overflow errors via `ABSTAIN FROM (1999)` at entry and `REINSTATE (1999)` at exit. This works correctly within a single compilation unit but may not preserve state across assembly boundaries.

**Workaround:** Split operands into 16-bit halves and add using `(1500)` with values that cannot exceed 32 bits.

### 8.2 Label Parsing in Comments

The tokenizer does not distinguish between labels in executable statements and parenthesized numbers in `DO NOTE` comments. A comment such as `DO NOTE USES: (1520) MINGLE` will create a local label that shadows the syslib routine.

**Workaround:** Avoid parenthesized numbers in comments.

## 9. SAMPLE PROGRAMS

### 9.1 Classical INTERCAL

| Program | Description | Significance |
|---------|-------------|--------------|
| `hello.i` | Hello, World | The classic INTERCAL program. Encodes ASCII as a numeric array. |
| `echo.i` | Echo input back to output | COME FROM loop with Turing tape encoding/decoding. |
| `fizzbuzz.i` | FizzBuzz | Uses ABSTAIN/REINSTATE for conditional control flow. |
| `beer.i` | 99 Bottles of Beer | Introduces the double-NEXT trampoline pattern for COME FROM loop branching. This pattern became the standard for all subsequent loop-based programs. |
| `hanoi.i` | Tower of Hanoi | Recursive solution using STASH/RETRIEVE for local state. |
| `rot13.i` | ROT13 cipher | Bit manipulation via mingle and select. |
| `collatz.i` | Collatz conjecture (3n+1) | Conditional branching and syslib arithmetic. |
| `primes.i` | Prime number sieve | Iterative division — a stress test for syslib multiply and modulo. |
| `pi.i` | Digits of pi | Extended-precision arithmetic. Proof that INTERCAL can compute transcendental numbers and probably shouldn't. |
| `pow.i` | Exponentiation | COME FROM loop with ABSTAIN-based exit. |

### 9.2 Syslib and Arithmetic Tests

| Program | Description | Significance |
|---------|-------------|--------------|
| `test_add.i` | ADD16 and ADD64 tests | Verifies addition at 16-bit and 64-bit widths including overflow. |
| `test_sub.i` | MINUS64 tests | Subtraction edge cases: identity, zero result, large values. |
| `test_mul.i` | TIMES16/32/64 tests | Multiplication at all widths including max values. |
| `test_div.i` | DIVIDE32 tests | Division by zero, zero numerator, identity. |
| `test_math.i` | Comprehensive math suite | Combined arithmetic tests across all operations. |
| `test_abstain.i` | ABSTAIN/REINSTATE tests | Verifies conditional statement suppression. |
| `32bitdivide.i` | 32-bit division subroutine | Callable shift-and-subtract division, usable from other programs. |

### 9.3 Research and Formal Proofs

| Program | Description | Significance |
|---------|-------------|--------------|
| `lemma1.i` | Lemma 1 bug reproducer | Demonstrates NEXT stack corruption: FORGET in a callable subroutine destroys the caller's return address. |
| `lemma1_comefrom.i` | Lemma 1 fix | Same algorithm using COME FROM loop — return address preserved. Confirmed on both SCHRODIE and C-INTERCAL. |
| `lemma2.i` | Lemma 2 bug reproducer | Loop with syslib calls: E421 on C-INTERCAL, infinite loop on SCHRODIE. Proves complex loops impossible with NEXT/RESUME alone. |
| `lemma2_comefrom.i` | Lemma 2 fix | Double-NEXT trampoline in a COME FROM loop. Works on both compilers. |
| `comefrom_bug.i` | Minimal COME FROM test | Isolated test case for COME FROM interaction with the NEXT stack. |
| `stable_marriage.i` | Gale-Shapley stable matching (n=5) | Most complex algorithm attempted. COME FROM loops with nested trampoline branching. Demonstrates that nontrivial algorithms require COME FROM. |

### 9.4 Knight's Tour (`warnsdorff/`)

Complete solution to the Knight's Tour problem on an 8×8 chessboard using Warnsdorff's heuristic with Arnd Roth tiebreaking. Visits all 64 squares. Believed to be the most complex algorithmic implementation in INTERCAL to date. ~570 lines across 6 source files.

| File | Description |
|------|-------------|
| `warnsdorff.schrodie` | Main solver: COME FROM loop, ABSTAIN/REINSTATE control flow, bitboard representation |
| `knight_attacks.i` | Precomputed attack masks for all 64 squares |
| `clear_mask.i` | Bit-clear masks for marking visited squares |
| `center_dist.i` | Center distance table for Arnd Roth tiebreaking |
| `lowbit.schrodie` | Lowest-set-bit extraction |
| `build.cmd` | Build script |

### 9.5 Hilbert Curve Geographic Indexing (`hilbertGeo/`)

Geographic range queries using Hilbert space-filling curves. Indexes 10 European cities by latitude/longitude, converts to Hilbert indices via Morton code intermediates, sorts, and performs range queries. Demonstrates that INTERCAL's mingle operator IS the standard Morton code algorithm used in production geospatial databases.

| File | Description |
|------|-------------|
| `hilbert_geo.schrodie` | Main program: coordinate conversion, Hilbert indexing, range query |
| `morton2hilbert.schrodie` | Morton-to-Hilbert state machine converter |
| `hilbert_table.i` | Hilbert state transition lookup tables |
| `city_data.i` | 32-bit fixed-point coordinates for 10 European cities |
| `bubble_sort.i` | 16-bit bubble sort |
| `bubble_sort64.i` | 64-bit bubble sort |
| `sort64.schrodie` | 64-bit sorting |
| `Program.cs` | C# reference implementation for verification |

## 10. ASSOCIATED PAPERS

| Paper | Description |
|-------|-------------|
| [COME FROM Considered Helpful](doc/COME_FROM_Considered_Helpful.md) | Formal proof that INTERCAL-72 cannot express callable subroutines containing arbitrary-length loops (79-iteration bound). Demonstrates COME FROM as computationally necessary. Includes TLA+ model checking. |
| [Warnsdorff Implementation Notes](doc/warnsdorff_implementation.md) | Technical writeup of the Knight's Tour solver. |
| [Hilbert Curve Geographic Indexing](doc/hilbertGeo.md) | Discovery that mingle IS Morton coding. COME FROM loops as the general-purpose conditional loop pattern. |
| [Stable Marriage](doc/stable_marriage.md) | Gale-Shapley algorithm design and the limits of INTERCAL data structures. |
| [Hardware Accelerator Proposal](doc/hardware_proposal.md) | FPGA processor design for native mingle/select. DIVIDE32 would run 7,000x faster in hardware. |

## REFERENCES

Calvelli, C. (2001). *CLC-INTERCAL*. Available at http://www.intERCAL.org.uk/

Raymond, E. S. (1990). *C-INTERCAL*. Available at http://catb.org/~esr/intercal/

Stross, C. (2015). Published for the first time: the Princeton INTERCAL compiler's source code. *esoteric.codes*.

Whittington, J. (2019). *A Preliminary Investigation into Whether INTERCAL Could Be Made Worse*. Unpublished manuscript, never submitted.

Whittington, J. and Claude (Anthropic). (2026a). Optimal graph traversal under adversarial constraints: A bitwise approach to memory-constrained environments. Manuscript in preparation.

Whittington, J. and Claude (Anthropic). (2026b). Hilbert curve geographic indexing in INTERCAL-64. Manuscript in preparation.

Woods, D. R. and Lyon, J. M. (1972). *The INTERCAL Programming Language Reference Manual*. Princeton University.

## OTHER RESOURCES

* The [C-INTERCAL Git Repository](https://github.com/calvinmetcalf/intercal) contains a wealth of code. The [pit](https://github.com/calvinmetcalf/intercal/tree/master/pit) is probably the most complete collection of INTERCAL code and docs anywhere.
* [MuppetLabs INTERCAL Pages](http://www.muppetlabs.com/~breadbox/intercal/) — essential reference material.

## ACKNOWLEDGEMENTS

This project draws inspiration from Eric Raymond's C-INTERCAL implementation (ick) and builds on CRINGE (Whittington, 2019). The formal results owe a debt to Dijkstra, who would have hated all of this.
