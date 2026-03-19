# INTERCAL in VS Code. Yes, Really.

For the first time in over fifty years, INTERCAL has a real development environment. Syntax highlighting. A step debugger. Breakpoints. A watch panel. Expression evaluation. The works.

We are not joking.

**INTERCAL** — the language designed in 1972 to have nothing in common with any other language — now has first-class support in Visual Studio Code. You can set a breakpoint on `DO .1 <- #42`, hit F5, and step through your program one statement at a time while watching variables update in real time.

No one asked for this. We built it anyway.

## What You Get

### Full INTERCAL Support
- **Syntax highlighting** for `.i` files with proper colorization of spots, two-spots, meshes, sparks, ears, and all the operators you've been afraid to use
- **Step debugging** — Step In, Step Over, Continue, breakpoints, the whole thing
- **Variables panel** — see the current value of every spot, two-spot, and four-spot variable as you step
- **Watch expressions** — type any INTERCAL expression in the watch panel and see it evaluate live. Finally understand what `'?"'.3~.3'~#1"$#1'~#3` actually produces.
- **Debug console** — program output appears in real time. WRITE IN works. You can interact with your running program.
- **64-bit arithmetic** — four-spot (`::`) variables, quad-mesh (`####`) constants, and a complete system library with ADD64, MINUS64, TIMES64, and more. All implemented in pure INTERCAL.

This is, to our knowledge, the first interactive debugger ever built for INTERCAL. In over fifty years of the language's existence, no one has been able to step through an INTERCAL program and watch it execute. Until now.

### And Then There's schrodie

As a bonus, this release includes **schrodie** — a quantum programming language that is fully backwards compatible with INTERCAL.

schrodie extends INTERCAL with quantum mechanics. Not simulated. Not metaphorical. Actual nondeterministic control flow driven by quantum superposition and entanglement.

Here's how it works:

1. **Put a cat in a box.** The `[]` variable type holds a value in quantum superposition. You don't know what's in the box until you open it.

2. **Entangle the boxes.** The `ENTANGLE` statement links two or more cat boxes. Once entangled, their fates are correlated. When you open any box in the group, exactly one cat remains. All others turn into voids — black cats — and run away.

3. **Let the universe decide your control flow.** The _thorn_ (`⟨N|ψ⟩`) is a statement guard that opens a box. If the cat is still there, the statement executes. If the cat ran away, the statement is skipped. It's a quantum ABSTAIN.

```
DO []1 <- .1 <- #1
DO []2 <- .2 <- #2
DO ENTANGLE []1 + []2
DO ⟨1|ψ⟩ READ OUT .1
DO ⟨2|ψ⟩ READ OUT .2
PLEASE GIVE UP
```

This program outputs either `1` or `2`. Never both. Never neither. The universe picks. You don't get a say.

schrodie also introduces:
- **N-way quantum branching** — `DO ⟨1|ψ⟩ (100) ⟨2|ψ⟩ (200) ⟨3|ψ⟩ (300) NEXT` dispatches to one of three labels based on which cat remains
- **Involution operators** — `|` (maypole) and `-` (monkey bar) are spin-1/2 operators. Apply either twice and you get back the original value. Apply `|-|-` and you get identity — which is why `#` is the constant prefix.
- **Array involutions** — flip arrays horizontally, vertically, or spin a Rubik's cube 180 degrees. `DO ,2 <- -|,1` rotates a 2D array. Rank > 3 returns `E4D1 ROTATING A HYPERCUBE IS LEFT AS AN EXERCISE FOR THE READER`.
- **128-bit ephemeral mingle** — mingle two 64-bit values into a 128-bit intermediate that exists only long enough to be selected back down. The value is briefly real and then gone, much like a good idea in a committee meeting.
- **AI-powered coding hints** — the debugger uses AI to analyze your code and provide helpful suggestions during debugging sessions.

### The Debugger Sees Everything

The schrodie debugger is quantum-aware:
- **Cat boxes in the Variables panel** — see `?` for uncollapsed, the value for cats that remained, and `VOID` for cats that ran away
- **Collapse by clicking** — double-click a box variable in the Variables panel and it collapses. If the box is entangled, all linked boxes collapse simultaneously. Watch 37 variables change from `?` to `VOID` in one click.
- **Thorn highlighting** — `⟨5|ψ⟩` renders in bold red so you can see where quantum decisions happen
- **`thorn` snippet** — type `thorn` + Tab to insert `⟨n|ψ⟩` with the cursor on the number

## Get It

### Quick Install (Mac)
```bash
curl -L https://github.com/jawhitti/INTERCAL/releases/download/v1.5.0/schrodie-1.5.0-osx-arm64.tar.gz | tar xz
cd osx-arm64
./install.sh
```

### Quick Install (Windows)
Download `schrodie-1.5.0-win-x64.zip` from the [releases page](https://github.com/jawhitti/INTERCAL/releases/tag/v1.5.0), extract, and run `install.ps1`.

### From Source
```bash
git clone https://github.com/jawhitti/INTERCAL.git
cd INTERCAL
dotnet build schrodie.sln
```

## Sample Programs

The distribution includes sample programs demonstrating every feature:

| Program | What It Does |
|---------|-------------|
| `hello.i` | Hello World (classic INTERCAL) |
| `hello_schrodie.schrodie` | Simplest quantum program — two entangled boxes, one output |
| `alice_bob.schrodie` | Quantum prediction game — Alice always wins because physics |
| `quantum_next.schrodie` | 3-way quantum branch via N-way thorn NEXT |
| `roulette5.schrodie` | 38-pocket quantum roulette wheel |
| `shores_algorithm.schrodie` | Pauly Shore's algorithm for integer factorization |
| `eve.schrodie` | Quantum key distribution with eavesdropper detection |
| `fizzbuzz.i` | FizzBuzz (proof that INTERCAL can do normal things) |
| `pi.i` | Digits of pi (proof that it shouldn't) |

## The Spec

The full language specification is available at [doc/schrodie.md](doc/schrodie.md). It is written in the style of an academic paper and contains everything you need to know about quantum dots, double cateyes, cat boxes, thorns, void cats, and the identity property of the mesh character.

This work received no funding from any source.

## Credits

Jason Whittington, 2026. With computational assistance that wishes to remain anonymous.
