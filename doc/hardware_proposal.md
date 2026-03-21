# INTERCAL Hardware Accelerator: Design Proposal

## 1. Overview

We propose the design and construction of a dedicated hardware processor implementing the INTERCAL instruction set architecture. The processor would execute INTERCAL programs natively, with mingle, select, and unary AND/OR/XOR implemented as single-cycle hardware operations. A Raspberry Pi serves as the host controller, handling program loading, I/O, and error reporting over USB or SPI.

This would be the first hardware implementation of any esoteric programming language.

## 2. Motivation

The INTERCAL instruction set, while designed as a parody in 1972, contains operations that map directly to useful bit-manipulation primitives:

| INTERCAL Operation | Hardware Equivalent | Real-World Use |
|---|---|---|
| Mingle (`$`) | Bit interleave | Morton codes, spatial hashing, GPU texture swizzling |
| Select (`~`) | Parallel bit extract + pack | x86 PEXT (BMI2), field extraction, compression |
| Unary AND (`&`) | Adjacent-pair reduction | SIMD prefix operations |
| Unary OR (`V`) | Adjacent-pair reduction | Parallel OR-reduce |
| Unary XOR (`?`) | Adjacent-pair reduction | Parity computation |

An INTERCAL processor would accidentally be a competent bit-manipulation coprocessor.

Performance analysis of the SCHRODIE software implementation shows that the bottleneck is not algorithmic complexity but call dispatch overhead — each arithmetic operation requires hundreds of cross-assembly method invocations through a .NET state machine. Native hardware execution would eliminate this overhead entirely.

## 3. Architecture

### 3.1 Execution Model

INTERCAL programs are batch-oriented: initialize inputs, execute statements sequentially, produce outputs, terminate. There are no interrupts, no syscalls, no virtual memory. This dramatically simplifies the hardware design.

Control flow uses:
- Sequential execution (top to bottom)
- `NEXT` — push return address, jump to label
- `RESUME #N` — pop N entries from return stack, jump
- `FORGET #N` — discard N entries from return stack
- `ABSTAIN` / `REINSTATE` — enable/disable specific statements

There is no conditional branch instruction. Conditional execution is achieved through the `RESUME .variable` pattern, where the variable's value (1 or 2) determines the return depth.

### 3.2 Custom ALU

The core differentiator: five operations implemented as dedicated combinational logic.

**Mingle (16-bit → 32-bit):** Interleaves two 16-bit inputs bit-by-bit. This is purely a wiring pattern — no gates required, just crossed traces. Zero propagation delay.

```
Input A:  a15 a14 a13 ... a1 a0
Input B:  b15 b14 b13 ... b1 b0
Output:   a15 b15 a14 b14 ... a1 b1 a0 b0
```

**Mingle (32-bit → 64-bit):** Same pattern at double width. For the SCHRODIE dialect's 64-bit support.

**Select (up to 64-bit):** Extract bits where mask has ones, pack right-justified. This requires a priority encoder and barrel shifter — the most complex unit, approximately 2,000-3,000 gates for 32-bit.

**Unary AND/OR/XOR (16-bit or 32-bit):** Apply the boolean operation to each adjacent pair of bits. Result is half the width of the input. Single gate per output bit — 16 AND gates for 32-bit unary AND.

**Estimated total ALU area:** Under 10,000 gates for the full 32-bit ALU. Under 25,000 gates for 64-bit SCHRODIE support.

### 3.3 Register File

INTERCAL defines variable types:
- `.N` — 16-bit (spot), N = 1 to 65535
- `:N` — 32-bit (two-spot), N = 1 to 65535
- `::N` — 64-bit (four-spot, SCHRODIE extension), N = 1 to 65535

The full address space (65535 variables × 3 types) is impractical in hardware registers. Real INTERCAL programs use at most 30-50 variables.

**Design:**
- Hardware registers for `.1`-`.16`, `:1`-`:10`, `::1`-`::5` (direct-mapped, single-cycle access)
- External SRAM for all other variables (2-3 cycle access via memory controller)
- A small variable cache (LRU, 8-16 entries) for frequently accessed higher-numbered variables

### 3.4 NEXT Stack

The NEXT stack holds return addresses for `NEXT` calls. The INTERCAL specification limits it to 80 entries (C-INTERCAL convention). `RESUME #N` pops N entries; `FORGET #N` discards N entries.

**Design:** 80-entry × 16-bit LIFO in on-chip SRAM or register file. Stack pointer register. RESUME and FORGET are pointer arithmetic — no data movement needed for FORGET (just decrement the pointer).

### 3.5 STASH Stacks

Each variable has an independent STASH stack (unlimited depth in the specification). `STASH .1 + .2 + .3` pushes current values; `RETRIEVE .1 + .2 + .3` pops them.

**Design:** STASH stacks live in external SRAM. Each variable has a stack pointer stored in a small on-chip table. STASH writes the current value to SRAM at the stack pointer and increments; RETRIEVE reads and decrements. Practical depth limit: 256 per variable (far beyond any real program's needs).

### 3.6 ABSTAIN Bitmap

ABSTAIN/REINSTATE enable or disable individual statements. The state is a single bit per statement.

**Design:** A bitmap in on-chip SRAM, indexed by statement number. For programs up to 4096 statements: 512 bytes. Each statement fetch checks the corresponding bit; if clear, the statement is skipped.

### 3.7 Array Storage

INTERCAL arrays (`,N` tail, `;N` hybrid, `;;N` double-hybrid) are dynamically sized at runtime via dimension statements. Arrays can be multi-dimensional.

**Design:** Arrays are allocated in external SRAM. A small array descriptor table (on-chip, ~64 entries) stores base address, dimensions, and element size for each active array. Array subscript computation (multi-dimensional index → linear offset) is handled by a simple multiply-accumulate unit in the memory controller.

### 3.8 Instruction Encoding

INTERCAL statements would be compiled to a compact bytecode:

| Opcode | Operand(s) | Description |
|--------|-----------|-------------|
| `ASSIGN` | dest, src_expr | Variable assignment |
| `MINGLE` | dest, a, b | Bit interleave |
| `SELECT` | dest, val, mask | Bit extract + pack |
| `UNARY_AND` | dest, src | Adjacent-pair AND |
| `UNARY_OR` | dest, src | Adjacent-pair OR |
| `UNARY_XOR` | dest, src | Adjacent-pair XOR |
| `NEXT` | label | Push return, jump |
| `RESUME` | n_or_var | Pop N, jump |
| `FORGET` | n_or_var | Discard N entries |
| `STASH` | var_list | Push variables |
| `RETRIEVE` | var_list | Pop variables |
| `ABSTAIN` | target | Disable statement |
| `REINSTATE` | target | Enable statement |
| `READ_OUT` | var | Write to output FIFO |
| `WRITE_IN` | var | Read from input FIFO |
| `GIVE_UP` | — | Signal completion |

Complex expressions would be decomposed by the compiler into sequences of primitive operations using temporary registers.

Instruction memory: on-chip ROM or SRAM, loaded from host at startup. 16-bit or 32-bit instruction words. Program size limit: 4096-8192 instructions (sufficient for any known INTERCAL program).

## 4. I/O Architecture

### 4.1 Host Interface

A Raspberry Pi (or any microcontroller with USB/SPI) serves as the host controller:

1. **Program load:** Host writes compiled bytecode to instruction memory over SPI or USB
2. **Input staging:** Host fills the input FIFO with WRITE IN data
3. **Execution:** Host asserts "go" signal; chip begins executing from instruction 0
4. **Output collection:** Chip writes READ OUT values to output FIFO; host drains it
5. **Completion:** Chip asserts "done" line on GIVE UP; host reads final state

### 4.2 FIFOs

Two small FIFOs (16-64 entries × 64 bits) for input and output:

- **Output FIFO:** READ OUT pushes a value. If full, execution stalls until the host drains an entry.
- **Input FIFO:** WRITE IN pops a value. If empty, execution stalls until the host pushes an entry.

This provides natural backpressure-based streaming. For batch programs (the common case), the host pre-fills the input FIFO and drains the output FIFO after GIVE UP.

### 4.3 Error Reporting

When the chip detects an error condition (STASH underflow, undefined variable, array bounds, etc.), it:

1. Halts execution
2. Writes an error code to a status register
3. Asserts the error signal line

The host reads the error code and prints the corresponding INTERCAL error message. Error codes map directly to the standard messages (E200 NOTHING VENTURED NOTHING GAINED, E436 THROW STICK BEFORE RETRIEVING, etc.).

## 5. Implementation Options

### 5.1 FPGA Prototype (Recommended First Step)

**Target:** Lattice iCE40 UP5K (~5,000 LUTs, $5-8)

**Additional components:**
- 256KB SPI SRAM (23LC1024, ~$1) for variables, arrays, STASH stacks
- Raspberry Pi Zero W (~$15) as host controller
- USB or SPI connection between Pi and FPGA

**Estimated BOM:** ~$25

**Development tools:** Open-source (Yosys + nextpnr + Project IceStorm). Design in Verilog or Amaranth (Python HDL).

**Timeline:** 2-4 weekends for someone experienced with FPGA development. The ALU is straightforward; the memory controller and instruction decoder are the bulk of the work.

### 5.2 ASIC Tape-Out (Bragging Rights)

**Target:** Efabless/Google open-source shuttle program (Skywater 130nm PDK)

**Cost:** ~$0 (subsidized MPW shuttles) to ~$10,000 (dedicated run)

**Area estimate:** The full design (ALU + register file + NEXT stack + memory controller + FIFOs) would fit comfortably in a fraction of the available die area on a Caravel harness chip.

**Timeline:** 3-6 months from design start to silicon, depending on shuttle schedule.

The resulting chip would be the world's first INTERCAL ASIC. It would have no practical value whatsoever. It would be magnificent.

### 5.3 Soft Core on Existing FPGA Boards

For accessibility, the design could also target common FPGA development boards (Arty A7, TinyFPGA BX, UPduino) with no custom PCB required. The Pi connection would use GPIO/SPI.

## 6. Software Toolchain

The SCHRODIE compiler currently targets .NET IL. A new backend would emit INTERCAL machine code (the bytecode defined in Section 3.8).

**Approach:**
- The compiler's frontend (tokenizer, parser, AST) is reused unchanged
- A new code generator walks the AST and emits bytecode instead of C#
- Expression trees are flattened into sequences of ALU operations using temporary registers
- Labels are resolved to instruction addresses
- The output is a binary image suitable for loading into instruction memory

**Estimated effort:** 2-3 weeks. The compiler already has a clean separation between parsing and code generation.

The assembler could also accept hand-written INTERCAL assembly for testing and bootstrapping.

## 7. Testing

### 7.1 Simulation

Before any hardware build, the full design would be simulated in Verilator or Icarus Verilog. The test suite: the existing SCHRODIE unit tests (260+ tests), compiled to bytecode and run on the simulated processor. Every test must produce identical output to the software implementation.

### 7.2 Reference Programs

Key validation programs:
- **Fizzbuzz** — exercises arithmetic, comparison, loops, I/O
- **Knight's tour** — exercises 64-bit bitboard operations, POPCOUNT, complex control flow
- **Beer** — exercises array I/O
- **Quantum roulette** — exercises cat box operations (if quantum extensions are implemented in hardware — stretch goal)

### 7.3 Performance Comparison

Benchmark: DIVIDE32 (1,000,000 ÷ 7). The software implementation takes ~7 seconds. The hardware implementation should complete in under 1 millisecond (32 iterations × ~10 clock cycles per iteration at a conservative 1 MHz clock). A speedup of approximately 7,000x.

## 8. Quantum Extension (Stretch Goal)

The SCHRODIE dialect's quantum features (cat boxes, ENTANGLE, thorn guards) could be implemented using a hardware random number generator (ring oscillator or thermal noise source) feeding a "quantum registry" state machine. Entangled groups would be tracked in a small table; collapse would select a survivor uniformly at random.

This would make it the world's first quantum INTERCAL processor, with genuine hardware randomness rather than pseudorandom software simulation. The fact that it has nothing to do with actual quantum computing is, as always, beside the point.

## 9. Conclusion

The proposed INTERCAL hardware accelerator is technically feasible, inexpensive to prototype, and completely unjustifiable by any rational cost-benefit analysis. It would demonstrate that the INTERCAL instruction set, designed as a joke, maps cleanly to silicon primitives. It would produce a physical artifact — a chip that executes INTERCAL natively — that has no equivalent in the history of computing.

We believe this makes it worth building.
