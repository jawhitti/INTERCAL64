# INTERCAL ALU Demo Board

## Concept

A breadboard demonstration that INTERCAL's operators are real hardware primitives. No microcontroller, no FPGA — just 74-series logic chips, DIP switches, LEDs, and wires.

## Components

| Part | Qty | Purpose | Cost |
|------|-----|---------|------|
| 8-bit DIP switch | 2 | Input A and B | $2 |
| 74HC08 | 1 | Quad AND — unary AND | $0.50 |
| 74HC32 | 1 | Quad OR — unary OR | $0.50 |
| 74HC86 | 1 | Quad XOR — unary XOR | $0.50 |
| LEDs | 16 | Output display (8 for mingle, 8 for unary) | $2 |
| Resistors | 16 | LED current limiting | $1 |
| Rotary switch or buttons | 1 | Operation select | $1 |
| Breadboard | 1 | | $3 |
| Hookup wire | — | Including mingle crossings | $2 |

**Total BOM: ~$12**

## Operations

### Mingle (8-bit → 16-bit)

No chips required. Mingle is a wiring pattern:

```
Input A:  a7  a6  a5  a4  a3  a2  a1  a0
Input B:  b7  b6  b5  b4  b3  b2  b1  b0
Output:   a7 b7 a6 b6 a5 b5 a4 b4 a3 b3 a2 b2 a1 b1 a0 b0
```

Run wires from the two DIP switches to 16 LEDs in the interleaved pattern. The "computation" is the copper traces themselves. Zero propagation delay. Zero power consumption. The purest possible implementation of an INTERCAL operator.

### Unary AND (8-bit → 4-bit)

One 74HC08 (quad 2-input AND gate):
- Gate 1: a7 AND a6 → output bit 3
- Gate 2: a5 AND a4 → output bit 2
- Gate 3: a3 AND a2 → output bit 1
- Gate 4: a1 AND a0 → output bit 0

### Unary OR (8-bit → 4-bit)

One 74HC32 (quad 2-input OR gate), same wiring pattern as AND.

### Unary XOR (8-bit → 4-bit)

One 74HC86 (quad 2-input XOR gate), same wiring pattern.

### Select (stretch goal)

Select with pack is the hard one. For 8-bit, a minimal implementation:
- 74HC151 (8-to-1 multiplexer) × 8 for bit selection
- Priority encoder for pack ordering
- Approximately 8-10 additional ICs

This is doable but messy on a breadboard. Could be a second-phase build or left as a "the FPGA version does this" teaser.

## Conference Demo Usage

Bring to SIGBOVIK or any esoteric computing event:

1. Set input A on left DIP switch (e.g., 10101010)
2. Set input B on right DIP switch (e.g., 11001100)
3. Mingle LEDs show the interleaved pattern immediately
4. Press buttons to route through AND/OR/XOR chips
5. Output LEDs show the unary operation result

Audience can flip switches and watch INTERCAL operations happen in real copper traces. The "mingle is just wires" point lands viscerally when you can see there are literally no logic gates between input and output.

## Presentation Talking Points

- "This is a computer that runs INTERCAL. It has no processor."
- "The mingle operation has zero gate delay. It is limited only by the speed of light in copper."
- "Every operation you see here is a single clock cycle on the FPGA version."
- "The full INTERCAL processor is $25 in parts. This demo is $12."
- "Yes, the wiring pattern IS the computation. Welcome to INTERCAL."
