# INTERCAL-64

This is a .NET 9 INTERCAL compiler with 64-bit extensions, a DAP debugger, and a VS Code extension. The compiler is being renamed from `cringe` to `churn`. File extension is `.ic64` (also accepts `.i`).

## Build

```
dotnet build schrodie.sln
dotnet test schrodie.tests/schrodie.tests.csproj
```

Run the test samples before pushing: `build/test-samples.ps1`

## Project Structure

- `cringe/` — the compiler (C#). Parses INTERCAL source, emits C# which is compiled to .NET assemblies.
  - `confuse.cs` — statement AST classes (parser)
  - `futile.cs` — code generation (emits C#, handles COME FROM trapdoors, ABSTAIN guards, DAP hooks)
  - `spew.cs` — compilation context, abstain map, gerund registry
  - `perplex.cs` — error messages
  - `Token.cs` / `Tokenizer.cs` — lexer
- `schrodie.runtime/` — runtime library
  - `utils.cs` — ExecutionContext, variable system (spots, two-spots, four-spots, arrays, box variables)
  - `twisty.cs` — NEXT/RESUME/FORGET thread-pool implementation
  - `QRegistry.cs` — quantum cat box registry (union-find entanglement, collapse)
  - `DebugHost.cs` — DAP debug host (named pipe communication)
- `schrodie.dap/` — DAP debug adapter (VS Code ↔ runtime bridge)
  - `DebugAdapter.cs` — handles DAP protocol, breakpoints, stepping, variables, watch expressions
  - `Snark.cs` — debugger commentary ("THE COMPILER WEEPS", etc.)
- `schrodie.tests/` — xUnit tests (scanner, parser, runtime, bitwise ops, quantum)
- `syslib64/` — the system library in pure INTERCAL
- `vscode-schrodie/` — VS Code extension (syntax highlighting, launch configs, snippets)
- `samples/` — sample programs including `learn-intercal/` tutorial
- `csharplib/` — C# interop sample
- `doc/` — papers and technical notes
- `Analysis/` — TLA+ formal verification models

## Compiler Internals

### How compilation works
1. Lexer tokenizes INTERCAL source into statements
2. Parser builds AST of Statement objects (`confuse.cs`)
3. `FixupComeFroms()` links COME FROM statements to their targets via `Trapdoor` field
4. Code generator (`futile.cs`) emits C# source:
   - Each statement gets a prolog (label, abstain guard, box guard) and epilog (trapdoor check)
   - COME FROM is implemented as a `goto` emitted AFTER the target statement's epilog
   - The abstain check for COME FROM happens at the trapdoor site, not around the COME FROM itself
5. Emitted C# is compiled to a .NET assembly via `dotnet build`

### ABSTAIN internals
- `abstainMap` is a local `bool[]` array in each assembly's generated code
- Each abstainable statement gets a slot; `true` = enabled, `false` = abstained
- COME FROM statements are excluded from the normal abstain guard — their check happens at the trapdoor
- ABSTAIN/REINSTATE is local to each component — cannot cross assembly boundaries
- Gerund-based ABSTAIN (e.g., `ABSTAIN FROM CALCULATING`) uses a type map in `spew.cs`

### COME FROM trapdoor mechanism
After every statement that is a COME FROM target, the compiler emits:
1. Check if the COME FROM statement is abstained (via its abstainSlot)
2. If not abstained, check % probability
3. `goto` the COME FROM statement's label

Key: ABSTAINing the *target* does NOT prevent COME FROM from firing. You must ABSTAIN the COME FROM statement itself.

### NEXT/RESUME/FORGET
- NEXT pushes a return address onto a thread-based stack (`twisty.cs`)
- RESUME pops N entries and returns to the Nth
- FORGET drops N entries without returning
- Max stack depth: 79 entries (E123 if exceeded)
- Cross-component NEXT/RESUME/FORGET works via thread pool signaling

## INTERCAL Language Quick Reference

### Variables
| Prefix | Name | Width |
|--------|------|-------|
| `.` | spot | 16-bit |
| `:` | two-spot | 32-bit |
| `::` | four-spot (double cateye) | 64-bit |
| `,` | tail | 16-bit array |
| `;` | hybrid | 32-bit array |
| `;;` | double hybrid | 64-bit array |

### Constants
| Prefix | Name | Width |
|--------|------|-------|
| `#` | mesh | 16-bit |
| `##` | fence | 32-bit |
| `####` | stockade | 64-bit |

### Operators
| Op | Name | Type | Notes |
|----|------|------|-------|
| `$` | mingle | binary | Interleaves bits. Two 16-bit → 32-bit, two 32-bit → 64-bit, two 64-bit → 128-bit ephemeral |
| `~` | select | binary | Extracts bits where mask has 1s, right-justifies result |
| `&` | AND | unary | ANDs adjacent bit pairs. Apply to mingled value for binary AND. |
| `V` | OR | unary | ORs adjacent bit pairs. Apply to mingled value for binary OR. |
| `?` | XOR | unary | XORs adjacent bit pairs. Apply to mingled value for binary XOR. |

**Key insight**: Unary operators are designed to be used on mingled pairs. Mingle sets up the operation, unary executes it. `'&(.1$.2)' ~ mask` = binary AND of .1 and .2.

### Grouping
- `'...'` — sparks (single quotes)
- `"..."` — ears (double quotes)
- Must alternate: sparks inside ears inside sparks, etc.

## System Library (syslib64.i)

### 16-bit: operands .1, .2 → result .3
| Label | Op |
|-------|----|
| (1000) | .3 = .1 + .2 (wrapping) |
| (1009) | .3 = .1 + .2 (overflow checked) |
| (1010) | .3 = .1 - .2 |
| (1040) | :3 = .1 * .2 (full 32-bit) |
| (1030) | .3 = .1 / .2, .4 = remainder |

### 32-bit: operands :1, :2 → result :3
| Label | Op |
|-------|----|
| (1500) | :3 = :1 + :2 |
| (1510) | :3 = :1 - :2 |
| (1540) | :3 = :1 * :2 |
| DIVIDE32 | :3 = :1 / :2, :4 = remainder |

### 64-bit: operands ::1, ::2 → result ::3
| Name | Op |
|------|----|
| ADD64 | ::3 = ::1 + ::2 |
| MINUS64 | ::3 = ::1 - ::2 |
| TIMES64 | ::3 = ::1 * ::2 |

Named labels are 64-bit integers computed from the ASCII name (big-endian 8-byte encoding).

### Known syslib bugs
- ADD64 in syslib64: never writes output. Use `samples/warnsdorff/my_add64.schrodie` instead.
- MINUS64: overflow handler triggers incorrectly. Use `samples/minus32.schrodie` pattern.
- (1000) wrapping add: when sum ≥ 65536, carry propagation produces RESUME #0 → hang. Use (1500) with widened values.

## Common INTERCAL Idioms

### Mingle + Unary = Binary Logic
```
DO :1 <- .1 $ .2          NOTE mingle: bits in adjacent pairs
DO .3 <- '&:1' ~ mask     NOTE unary AND on pairs, select result
```

### Zero Test
Subtract two values via syslib. Result is 0 if equal. Use as RESUME depth or dispatch index.

### The Double-NEXT Trampoline (conditional branching)
```
    DO (110) NEXT            NOTE push "true" return
    DO (120) NEXT            NOTE push "false" return
    DO RESUME .3             NOTE .3=1 → true, .3=2 → false
(110) DO FORGET #1           NOTE true path
      ...
      DO RESUME #1
(120) DO FORGET #1           NOTE false path
      ...
      DO RESUME #1
```
Never use `.5 ~ #1` to compute the RESUME depth — it produces RESUME #0 which is illegal.

### COME FROM Loop
```
(10) DO COME FROM (20)      NOTE back-edge
     ... loop body ...
     NOTE exit: DO ABSTAIN FROM (10)
(20) DO NOTE end of body
```
COME FROM loops don't touch the NEXT stack — no 79-iteration limit.

### Select as Right-Shift
`.1 ~ #65280` selects top 8 bits of a 16-bit value, right-justified. This IS a logical right-shift by 8.

### ABSTAIN as Conditional
ABSTAIN a statement to skip it. REINSTATE to re-enable. This is INTERCAL's `if`.

## Politeness
Between 1/5 and 1/3 of statements must say PLEASE. Too few → E079 (not polite enough). Too many → E099 (sycophantic). Libraries compiled with `/noplease` bypass this check.

## Compiler Flags
Use `-` prefix, not `/` (Windows reinterprets `/` as drive paths):
```
churn hello.i                     NOTE compile to exe
churn -t:library syslib64.i       NOTE compile to dll
churn -r:syslib64.dll program.i   NOTE link against library
churn -debug+dap program.i        NOTE enable DAP debugger
```

## DAP Debugger
The debugger communicates via named pipes. The VS Code extension launches the compiler with `-debug+dap`, which emits debug hooks in the generated code. `DebugHost.cs` receives statement notifications and translates to DAP protocol.

Features: breakpoints, step in/over, variables panel (all types including quantum), watch expressions (evaluates INTERCAL expressions against current state), ABSTAIN/gerund state tracking, COME FROM visualization.

## Error Codes
| Code | Message | Meaning |
|------|---------|---------|
| E079 | PROGRAMMER IS INSUFFICIENTLY POLITE | Not enough PLEASEs |
| E099 | PROGRAMMER IS OVERLY POLITE | Too many PLEASEs (sycophantic) |
| E123 | PROGRAM HAS DISAPPEARED INTO THE BLACK LAGOON | NEXT stack overflow (>79) |
| E129 | PROGRAM HAS GOTTEN LOST ON THE WAY TO label | Unresolved label |
| E633 | PROGRAM FELL OFF THE END | No GIVE UP |
| E774 | RANDOM COMPILER BUG | Intentional — 1-in-10 chance per statement |
| E2002 | SOME ASSEMBLY REQUIRED | Referenced assembly not found |

## Writing INTERCAL Programs — Tips
1. Always end with `PLEASE GIVE UP`
2. Use COME FROM loops, not NEXT/RESUME loops, for anything over ~10 iterations
3. The double-NEXT trampoline is the standard conditional branch pattern
4. Don't FORGET at a COME FROM target — the trampoline's RESUME already cleans up
5. ABSTAIN the COME FROM statement to exit a loop, not the target
6. Avoid parenthesized numbers in DO NOTE comments — the parser treats them as labels
7. syslib routines use .1/.2 for input, .3/.4 for output — save your values before calling
8. For 64-bit work, use named labels (ADD64, TIMES64, etc.) via the 8-byte ASCII encoding
