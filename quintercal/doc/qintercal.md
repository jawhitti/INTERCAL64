# INTERCAL-Q: Quantum Extensions for INTERCAL

## Design Document v4

-----

## Overview

INTERCAL-Q is an extension to the INTERCAL programming language adding quantum computing primitives. The quantum model is intentionally a simplified toy model that is implementable with classical computing primitives (essentially `rand()` and a graph registry) while being described in quantum mechanical terms with complete sincerity.

The core joke is that INTERCAL was quantum-native all along:

- No conditionals (replaced by quantum entanglement)
- ABSTAIN/REINSTATE as quantum control flow
- The mingle operator `¢` as a native superposition primitive
- The select operator `~` as a measurement operator
- The pole `|` and monkey bar `-` as quantum spin operators (they always were)

-----

## What INTERCAL-Q Actually Implements

INTERCAL-Q implements a specific, honest, and self-consistent slice of quantum information theory:

**What it genuinely implements:**

- Single qubit superposition
- N-particle W state entanglement
- EPR correlation
- Shared secret generation
- Man-in-the-middle detection (BB84-equivalent statistical argument)
- No-cloning theorem (enforced by type system)
- No faster-than-light communication (enforced by protocol structure)

**What it honestly does not implement:**

- GHZ states
- Complex probability amplitudes
- Quantum interference
- Teleportation (SPLIT model cannot conserve information across transfer)
- Shor’s algorithm
- Grover’s search
- Any feature providing actual quantum computational advantage

*“INTERCAL-Q is quantum computing with the dignity removed. The physics is intact. The presentation is not.”*

-----

## The Core Model

### QValues

A **qvalue** is a value in superposition of `{value, DEDKITTY}`. This maps to Schrödinger’s cat:

- The cat is either alive (you get the value) or dead (you get DEDKITTY)
- Opening the box (reading/assigning) collapses the superposition
- The cat has a color (the actual integer value, which may be zero)
- A dead cat has no color — it is specifically DEDKITTY

### The Dead Cat Sentinel

```csharp
public const long DEDKITTY = 0x4445444B49545459;
// ASCII encoding of "DEDKITTY"
```

DEDKITTY replaces 0 as the dead branch sentinel. This is necessary because:

- Zero is a valid cat color (a zero-valued alive cat is meaningful)
- The alive/dead distinction must be unambiguous
- DEDKITTY is astronomically unlikely to appear as a legitimate value

**DEDKITTY is NOT checked at box creation.** If someone puts DEDKITTY in a box and opens it they get DEDKITTY back. The cat was already dead when they put it in. The language shrugs. If they entangle a dead cat with a live cat and the dead cat wins the W state lottery, the live cat dies too. Do not entangle dead cats with live ones.

Helper methods:

```csharp
public static bool IsAlive(long value) => value != DEDKITTY;
public static bool IsDead(long value)  => value == DEDKITTY;
```

### All QValues Are 64-bit

Everything in a box is 64-bit long. 16-bit and 32-bit classical values are upcast at box creation. Only new code uses boxes. Classical INTERCAL code is unaffected. DEDKITTY requires all 64 bits and therefore can never be produced by upcasting a 16 or 32-bit value — safe by construction.

### The Invariant

**A qvalue holds either its value (any 64-bit integer including zero) or DEDKITTY. Never anything else.**

### Collapse

A qvalue collapses when:

1. It is assigned to a classical variable (`DO .n <- []m`)
1. It appears in an expression assigned to a classical variable
1. A qvalue it is entangled with collapses
1. It appears in a `DO []n NEXT` statement
1. It appears in a `DO []n ABSTAIN FROM` statement

On collapse:

- The W state determines exactly one survivor
- The survivor gets its value
- All others get DEDKITTY
- Collapsed qvalues behave as classical values thereafter

-----

## The N-Particle W State

INTERCAL-Q’s multi-particle entanglement implements the **W state**, one of the two fundamental classes of genuine multipartite quantum entanglement (the other being the GHZ state).

The N-particle W state is:

```
|W⟩ = |100...0⟩ + |010...0⟩ + |001...0⟩ + ...
```

Exactly one particle is in state |1⟩ and all others are in state |0⟩, but you don’t know which until you measure. In INTERCAL-Q terms: exactly one qvalue gets its value, all others get DEDKITTY.

**Properties of the W state that INTERCAL-Q inherits:**

- Exactly one survivor, always, regardless of N
- Robust against loss: if one qvalue is discarded without observing, the remaining ones stay entangled
- Collapse of any member collapses all members simultaneously
- The W state is experimentally verified quantum mechanics (Nobel Prize 2022 via related GHZ work)

**This was not planned.**

*“INTERCAL-Q implements the N-particle W state, one of two fundamental classes of genuine multipartite quantum entanglement. All features follow from this single primitive.”*

-----

## Syntax And Sigils

### The `[]` Sigil

Quantum variables use the `[]` sigil. The characters are officially named:

- `[` — **correct horse battery staple**
- `]` — **incorrect horse battery staple**

(Reference: XKCD #936)

```
[]1    quantum variable 1
[]2    quantum variable 2
```

### Assignment Semantics

The assignment cases are determined purely by operand types:

```
DO []1 <- #42             classical into box = create superposition {42, DEDKITTY}
DO []1 @ []2              swirl operator     = entangle two existing qvalues
DO .1  <- []1             box into classical = collapse, .1 gets value or DEDKITTY
DO []1 <- #a @ #b @ #c   W state chain      = create N-particle W state (see below)
```

### The Swirl Operator `@`

Two forms:

**Binary entanglement** — links two existing qvalues:

```
DO []1 @ []2
```

**W state chain** — creates an N-particle W state inline at assignment:

```
DO []wheel <- #1001 @ #1002 @ #1003 @ ... @ #1038
```

The chain form creates N independent qvalues in a single W state component and binds `[]wheel` as a handle to the entire component. The handle can be used to collapse the entire W state in one operation.

**Redundant entanglement is a no-op.** If the two qvalues are already in the same component (including `[]1 @ []1`), the operation silently does nothing. This is idempotent by design.

**Exception:** Attempting to entangle an already-collapsed qvalue throws:
`ICL077I: YOU CANNOT ENTANGLE A DEAD CAT`

### The Complete Assignment Table

|Syntax                       |Meaning                                                   |
|-----------------------------|----------------------------------------------------------|
|`DO []n <- #v`               |Box a classical value. Superposition of {v, DEDKITTY}.|
|`DO []n <- #a @ #b @ #c`     |W state chain. N-particle W state. []n is the handle.     |
|`DO []a @ []b`               |Entangle two existing qvalues. No-op if already entangled.|
|`DO .n <- []m`               |Collapse. .n gets value or DEDKITTY.                  |
|`DO []n NEXT`                |Collapse and jump to label. Falls through if DEDKITTY.|
|`DO []n ABSTAIN FROM (label)`|Collapse. DEDKITTY abstains, alive reinstates.        |

-----

## Quantum Control Flow

### DO []n NEXT

```
DO []wheel NEXT
```

Collapses `[]wheel`. If the result is alive (a valid label number), jumps to that label. If the result is DEDKITTY, falls through to the next statement.

This is the most direct form of quantum-driven control flow. The W state picks a destination. The others fall through silently.

**The DEDKITTY label:** DEDKITTY is just a number. If a statement exists at label DEDKITTY, every quantum collapse that produces a dead cat will jump there. This is permitted. The runtime does not prevent it. Every dead cat in your program will visit that label. Whether this is desirable is left as an exercise.

### DO []n ABSTAIN FROM (label)

```
DO []wheel ABSTAIN FROM (1001)
```

Collapses `[]wheel`. If DEDKITTY: abstains from label 1001. If alive: reinstates label 1001.

### W State Dispatch Pattern

The canonical pattern for quantum N-way dispatch using a W state chain:

```
; Create W state with one value per destination label
DO []choice <- #(label1) @ #(label2) @ ... @ #(labelN)

; Collapse — one label becomes the destination, others fall through
DO []choice NEXT

; N destination labels
(label1) ... DO (continue) NEXT
(label2) ... DO (continue) NEXT
...
(labelN) ...

(continue)
```

Exactly one label receives control. The others are never reached. The W state determines which one. The programmer has no influence over the outcome. The universe decides.

-----

## Type System

### Two Worlds

INTERCAL-Q has two completely separate type worlds:

**Classical world**: Normal INTERCAL values. Horrible arithmetic. The usual suffering.

**Quantum world**: qvalues in superposition. W state entanglement. Different suffering.

### The Hard Boundary

Classical and quantum values cannot be mixed in expressions. Mixing them would corrupt the invariant.

Compiler error: `ICL094I: YOU CANNOT ADD A REAL VALUE TO A DEAD CAT`

### Type Interactions

|Operation    |Direction             |Syntax                       |
|-------------|----------------------|-----------------------------|
|Boxing       |classical → quantum   |`DO []n <- #value`           |
|W state chain|classical → quantum   |`DO []n <- #a @ #b @ #c`     |
|Collapse     |quantum → classical   |`DO .n <- []m`               |
|Entangle     |quantum ↔ quantum     |`DO []a @ []b`               |
|Jump         |quantum → control flow|`DO []n NEXT`                |
|Guard        |quantum → abstain     |`DO []n ABSTAIN FROM (label)`|

### Select: The Bridge

Select is the only operation that legally crosses the boundary with a classical operand:

```
DO .n <- []m ~ .mask
```

The quantum value is forced to collapse. The classical mask is applied to the result. The output is classical. Select is measurement.

-----

## Binary Operators

### Only Two: Mingle and Select

INTERCAL has exactly two binary operators. In the quantum world:

**Mingle `¢`**: Builds an expression tree. Does not evaluate immediately. Valid only between qvalues from **different** entangled components. Mingling qvalues from the same component produces a result where the dead leaf contributes zero — the language does not error on this but the result is likely not what you intended.

**Select `~`**: Forces collapse. Left operand must be quantum, right operand (mask) must be classical. Produces a classical result. This is the only legal quantum/classical mixing.

```
quantum ¢ quantum (different components) → new derived qvalue, stays in superposition
quantum ¢ quantum (same component)       → valid but degraded, DEDKITTY→0 substitution
quantum ~ classical                      → forces collapse, classical result
quantum ~ quantum                        → error, mask must be classical
classical ~ quantum                      → error, nonsensical
```

### Dead Cat Propagation In Expression Trees

When a leaf collapses to DEDKITTY its contribution to expression tree evaluation is:

- In mingle: DEDKITTY is substituted with 0. Dead cat contributes no bits.
- In unary: DEDKITTY passes through as DEDKITTY. Dead cat stays dead.

No errors are thrown. The language shrugs.

### Unary Operators

AND, OR, XOR applied across all bits. Evaluated **eagerly** at construction time. Valid because:

- Unary ops are applied to the value (alive branch) at construction
- DEDKITTY is a fixed point: unary(DEDKITTY) = DEDKITTY
- The invariant is preserved through any unary transformation

-----

## Expression Trees

Quantum expressions involving only qvalues remain in superposition, building an unevaluated expression tree:

```
DO []3 <- []1 ¢ []2       quantum mingle, tree node created
DO []4 <- []3 ¢ []5       deeper tree
DO .result <- []4 ~ .mask  select forces collapse of entire tree
```

### Tree Node Types

```
QTree := QLeaf(qvalue)           bare [] variable
       | QMingle(QTree, QTree)   mingle of two quantum values
       | QUnary(op, QTree)       unary op applied to quantum value
```

Select is NOT a tree node. It always forces immediate collapse and produces a classical value.

-----

## Quantum ABSTAIN

The ABSTAIN statement itself is the observation point:

```
DO []a ABSTAIN FROM (100)
```

When execution hits this line:

1. `[]a` is observed right there, collapses immediately
1. If result is DEDKITTY: statement 100 is abstained from
1. If result is alive: statement 100 is not abstained from

### Generalized Guard Syntax

Any expression can guard any statement:

```
DO <expression> (<statement>) <verb>
```

Where expression is anything, statement is a line number, verb is NEXT or ABSTAIN or REINSTATE. If the expression evaluates to zero (or DEDKITTY for quantum expressions) the statement is skipped. If nonzero, it executes.

**Note on Quantum FizzBuzz:** Retired. FizzBuzz dispatch requires deliberately creating dead cats for non-firing cases — a classical decision, not a quantum one. The generalized guard syntax handles FizzBuzz correctly as a classical operation. The quantum layer is for genuinely uncertain decisions, not known ones.

-----

## The QRegistry

A central registry tracking W state components. Implemented as a union-find structure.

### Data Model

```csharp
class QValue {
    long Value;           // the classical value (alive branch)
    bool Collapsed;       // has this been observed
    long Result;          // classical result: Value or DEDKITTY
    QTree? Tree;          // null for leaves, expression tree for derived
    QRegistry Registry;   // the registry this belongs to

    const long DEDKITTY = 0x4445444B49545459;

    static bool IsAlive(long v) => v != DEDKITTY;
    static bool IsDead(long v)  => v == DEDKITTY;

    long Observe();           // collapses if not already collapsed
    int Select(int mask);     // forces collapse, applies mask, returns classical int
    void Swirl(QValue other); // entangles with other
    void NextTo();            // collapses, jumps to value as label or falls through
}

class QRegistry {
    // Union-find over QValue components
    // Injected random source for testability: Func<int,int>

    void Register(QValue q);
    void Entangle(QValue a, QValue b);    // no-op if already same component
    QValue CreateChain(long[] values);    // W state chain from array of values
    QValue Find(QValue q);                // with path compression
    bool AreEntangled(QValue a, QValue b);
    void Collapse(QValue trigger);
    IReadOnlySet<QValue> GetComponent(QValue q);
    void RegisterAbstainHook(QValue q, Action<long> hook);

    int ComponentCount { get; }
    int QValueCount { get; }
}
```

### Collapse Algorithm

```python
def collapse(trigger):
    component = find_component(trigger)
    leaves = [q for q in component if q.Tree is None]

    survivor = random.choice(leaves)

    for leaf in leaves:
        leaf.Result = leaf.Value if leaf == survivor else DEDKITTY
        leaf.Collapsed = True

    for derived in [q for q in component if q.Tree is not None]:
        derived.Result = derived.Tree.Evaluate()  # pure, inputs determined
        derived.Collapsed = True

    for node in component:
        fire_abstain_hooks(node, node.Result)

    remove_component(component)
```

-----

## Demos

### Quantum Key Distribution and Man-in-the-Middle Detection

**The flagship demo.** Genuinely quantum. Genuinely correct.

Alice and Bob generate a shared secret using entangled pairs. The secret doesn’t exist until boxes are opened. Eve intercepts Bob’s box, opens it, creates a fresh replacement. The replacement is not entangled with Alice. The correlation is broken. Alice and Bob detect this statistically.

Detection probability per pair: 25%
Detection probability over N pairs: 1 - 0.75^N

This is the same statistical argument as BB84 quantum key distribution. The mechanism is different. The conclusion is identical.

*“The universe does not accept forgeries.”*

See: `SharedSecretTests.cs`, `QuantumKeyDistributionTests` in `QRegistryTests.cs`

### Shared Secret Generation

Alice creates N entangled pairs, keeps one box from each, sends Bob the other. At any agreed time (not necessarily simultaneously — timing doesn’t matter) each opens their boxes. Bob flips his bits. They hold identical N-bit secrets.

Properties:

- Secret does not exist in transit
- Neither party controls the outcome
- Universe decides the bits
- 2^N possible secrets for N pairs
- Pairs are consumed — cannot be reused

See: `SharedSecretTests.cs`

### Quantum Roulette

A 38-particle W state represents an American roulette wheel. Exactly one pocket contains the ball. All others contain DEDKITTY. The ball does not exist in any pocket until the croupier triggers collapse.

```
; Load the wheel — one line
DO []wheel <- #1001 @ #1002 @ #1003 @ #1004 @ #1005 @ #1006 @ #1007 @ #1008 @ #1009 @ #1010 @ #1011 @ #1012 @ #1013 @ #1014 @ #1015 @ #1016 @ #1017 @ #1018 @ #1019 @ #1020 @ #1021 @ #1022 @ #1023 @ #1024 @ #1025 @ #1026 @ #1027 @ #1028 @ #1029 @ #1030 @ #1031 @ #1032 @ #1033 @ #1034 @ #1035 @ #1036 @ #1037 @ #1038

; Spin — one line
; 37 labels fall through, one receives control
DO []wheel NEXT

; 38 pocket labels
(1001) DO .pocket <- #0           DO (2000) NEXT   ; pocket 0
(1002) DO .pocket <- #DOUBLEZERO  DO (2000) NEXT   ; pocket 00
(1003) DO .pocket <- #1           DO (2000) NEXT   ; pocket 1
...
(1038) DO .pocket <- #36          DO (2000) NEXT   ; pocket 36

(2000) ; pay out bets using .pocket
```

The W state chain syntax makes this exactly two meaningful lines of quantum code. The rest is classical bet resolution.

**Special values:**

```csharp
public const long DOUBLEZERO = 0x3030L;  // "00" in ASCII
```

**The house edge** comes from 0 and DOUBLEZERO. The W state is perfectly fair — each pocket has exactly 1/38 probability. The house edge is baked into the bet structure not the wheel. Quantum mechanics cannot help you beat roulette. The W state does not care about your betting system.

**The DEDKITTY label:** The 37 losing pockets produce DEDKITTY which falls through by default. If an enterprising developer places a statement at label DEDKITTY it will receive control from every losing pocket on every spin. Whether this is useful is between them and the universe.

*“Quantum Roulette implements a 38-particle W state. The ball does not exist until collapse. The house edge is 5.26%. The W state does not care about your betting system. The house still wins.”*

### Quantum Fingerprinting

N independent qvalues, each a single-particle W state. Mingle them all into one expression tree across N independent components. Collapse via select. The result is a classical integer where each bit position reflects whether the corresponding value survived its independent coin flip.

Over many collapses you build a statistical fingerprint of the dataset. Two identical datasets produce the same distribution. Differing datasets diverge. Detection probability scales with dataset difference and number of trials.

Genuine quantum randomness as the entropy source. Not better than a classical probabilistic scheme. More quantum.

### Pauly Shore’s Algorithm

Integer factorization via quantum memory exhaustion. Complexity: O(buuuddy). Correctness: optimistic. Named after Pauly Shore.

The pseudocode in the design document is not valid INTERCAL-Q under current rules. This was discovered after publication. The algorithm’s other properties are unaffected.

See: `Pauly-Shores-Algorithm.docx`

### Collatz Termination

The termination condition of a Collatz sequence expressed as a quantum superposition. Whether the program halts is left to the universe, which is also where the mathematicians left it. This is not a joke. It is an accurate model of our epistemic state about an unsolved mathematical problem.

-----

## Retired Demos

### Quantum FizzBuzz

Requires deliberately creating dead cats for non-firing cases. Classical decision dressed in quantum clothing. Retired. The generalized guard syntax handles it correctly without quantum machinery.

### Quantum Bogosort (3 Elements)

Sorting requires copying values across positions. No-cloning theorem prevents this. The quantum part is just a random number generator driving a classical shuffle. Retired. Quantum Roulette is the honest version of the same idea.

### Quantum Monty Hall

Makes one valid point — host knowledge is load-bearing in the classical problem, quantum removes it, switching advantage disappears. After working out the probability the demo becomes a probability puzzle in a quantum costume. Retired. The insight is preserved as a footnote.

-----

## Design Decisions and Rationale

### Why DEDKITTY Instead of Zero

Zero is a valid cat color. A zero-valued alive cat must be distinguishable from a dead cat. DEDKITTY as ASCII sentinel achieves this cleanly, requires all 64 bits (so can never arise from upcasting), and is self-documenting.

### Why W State Chain Syntax

`DO []n <- #a @ #b @ #c` creates an N-particle W state in one assignment. The handle `[]n` refers to the entire component. This enables clean N-way dispatch — `DO []n NEXT` jumps to whichever value survived. The alternative (N separate assignments plus N entanglement calls) is verbose and obscures the intent.

### Why DO []n NEXT Falls Through On DEDKITTY

DEDKITTY is treated as zero for control flow purposes — an invalid label, a no-op jump. This makes N-way dispatch work naturally: the 37 losing pockets fall through, the one winner jumps. No explicit check needed. No error thrown. The language shrugs at dead cats jumping to nowhere.

### Why No-Op for Redundant Entanglement

`@` means “ensure these are entangled.” If they already are, the postcondition is satisfied. Idempotent operations compose better. The only error worth keeping is entangling a collapsed qvalue — that’s genuinely impossible, not redundant.

### Why 64-bit Throughout

Only new code uses boxes. Classical INTERCAL code is unaffected. 64 bits accommodates DEDKITTY unambiguously. Upcasting at box creation is the caller’s responsibility and is always implicit.

### Why The W State Not GHZ

The W state (exactly one survivor) emerged naturally from the {value, DEDKITTY} superposition model. It happens to be one of two fundamental classes of genuine multipartite quantum entanglement. The GHZ state (all same outcome) would require a different model. The W state is sufficient for all intended use cases.

### Why Assignment Is Collapse

No explicit measurement operator. Collapse is a side effect of the most ordinary operation in any programming language. This means quantum mechanics is hidden inside normal-looking code. Debug prints collapse qvalues and change program behavior. The Heisenberg uncertainty principle applies to printf debugging. This is intentional.

### Why ENTANGLE Can Be Called Anywhere

Including inside functions that received variables as arguments without the caller knowing. This is physically motivated (entanglement can be introduced by interaction), a complete violation of function encapsulation, and perfectly in keeping with INTERCAL’s general philosophy toward the programmer. All three of these things are true simultaneously.

### Why Dead Cat Propagation Is Silent

DEDKITTY flowing through expression trees produces predictable behavior: zero contribution to mingle, unchanged through unary. An error at that point would fire in the wrong place with no useful context — the death happened elsewhere entirely. Silent propagation lets the programmer discover via IsDead() at the classical boundary. The language shrugs.

-----

## The Mingle Connection

INTERCAL’s existing operators map naturally onto quantum concepts:

|INTERCAL      |Quantum analog                                          |
|--------------|--------------------------------------------------------|
|Mingle `¢`    |Superposition (two values coexist in one)               |
|Select `~`    |Measurement (extract one value with a mask)             |
|`%50`         |Classical probability (the uncool version of randomness)|
|ABSTAIN       |Quantum control flow                                    |
|REINSTATE     |Quantum control flow                                    |
|Pole `|`      |Quantum spin (vertical)                                 |
|Monkey bar `-`|Quantum spin (horizontal)                               |

The `%50` operator produces classical nondeterminism. Quantum superposition is categorically different and superior. The documentation makes clear that `%50` is pedestrian and somewhat embarrassing compared to proper quantum superposition.

-----

## Error Messages

|Code   |Message                                                                                                   |
|-------|----------------------------------------------------------------------------------------------------------|
|ICL077I|YOU CANNOT ENTANGLE A DEAD CAT                                                                            |
|ICL078I|YOU HAVE ENTANGLED A VARIABLE WITH ITSELF. THIS IS EITHER VERY DEEP OR VERY STUPID (retired — now a no-op)|
|ICL091I|YOUR OBSERVATION HAS DISTURBED THE SYSTEM. THIS IS NOT A BUG. THIS IS PHYSICS                             |
|ICL092I|THE COLLAPSED FLAG IS NOT A PHYSICAL OBSERVABLE. DO NOT BUILD FTL COMMUNICATION DEVICES WITH IT           |
|ICL093I|THESE CATS DO NOT KNOW EACH OTHER (mixing unentangled qvalues in binary expression)                       |
|ICL094I|YOU CANNOT ADD A REAL VALUE TO A DEAD CAT (mixing classical and quantum)                                  |
|ICL095I|THESE CATS ARE ALREADY ACQUAINTED (retired — now a no-op)                                                 |
|ICL100I|THE UNIVERSE HAS INSUFFICIENT MEMORY FOR YOUR QUANTUM CIRCUIT. CONSIDER A SMALLER UNIVERSE                |
|ICL101I|UNOBSERVED QVALUE ORPHANED BY REASSIGNMENT. YOUR REGISTRY IS GROWING. THIS MAY BE INTENTIONAL             |
|ICL103I|(retired — putting DEDKITTY in a box is now permitted. the language shrugs)                           |

-----

## Implementation Notes

### What This Actually Is

Underneath all the quantum terminology, INTERCAL-Q is:

- A union-find data structure (the W state registry)
- `rand()` (the collapse mechanism)
- A flag per variable (collapsed/uncollapsed)
- A 64-bit long per variable (value or DEDKITTY)
- An expression tree (deferred mingle/unary evaluation)
- A callback system (quantum ABSTAIN hooks)

### What Needs Adding To The Existing Compiler

1. `QValue` class and `QRegistry` singleton
1. `IsQuantum` property on all expression nodes
1. `EvaluateQuantum()` method on expression nodes
1. Modified assignment with four cases (box, chain, entangle, collapse)
1. `CreateChain()` in QRegistry for W state chain syntax
1. `EntangleStatement` for `@`
1. `QuantumNextStatement` for `DO []n NEXT`
1. `QuantumAbstainStatement` for `DO []n ABSTAIN FROM`
1. Guard field on base Statement class
1. Guard evaluation in dispatch loop
1. `[]` variable type in symbol table
1. 64-bit arithmetic throughout quantum layer
1. DEDKITTY fall-through in jump dispatch

### Testing

Canonical tests in order of importance:

1. `QuantumKeyDistributionTests` — verifies EPR correlation, man-in-the-middle detection, 25% detection rate, exponential scaling
1. `SharedSecretTests` — verifies secret generation protocol end to end
1. `QuantumTeleportationTests` — verifies no-cloning, no-communication (EPR only, not full teleportation)
1. `EntanglementTests` — verifies W state invariants
1. `InvariantTests` — verifies {value, DEDKITTY} invariant throughout
1. `QuantumRouletteTests` — verifies 38-particle W state, uniform distribution, DEDKITTY fall-through

-----

## The Philosophical Position

INTERCAL-Q takes the position that:

1. INTERCAL was quantum-native all along
1. The original designers were prophetic
1. The mingle operator is a superposition primitive
1. The select operator is a measurement operator
1. The absence of conditionals is a feature
1. The N-particle W state is the correct model for INTERCAL-Q entanglement
1. QKD and man-in-the-middle detection are genuine quantum results
1. Quantum Roulette is the most honest use of a W state in any programming language
1. All of the above is technically defensible

None of these claims are entirely false. That is the point.

INTERCAL and quantum mechanics were always going to find each other. INTERCAL was designed in 1972 to have no resemblance to any existing programming language. Quantum mechanics also has no resemblance to how anyone thinks computation should work. It was inevitable. 