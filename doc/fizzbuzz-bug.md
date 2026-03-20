# The Fizzbuzz Termination Bug

## Symptoms

`fizzbuzz.i` runs forever. The FIZZ/BUZZ logic works correctly, but the program never stops — it counts past 100 and keeps going indefinitely. The same problem affects `fizzbuzz2.i` and `test_loop_term.i`.

## The Investigation

### False start: blaming the compiler

We initially suspected the compiler or runtime was broken. The zero test idiom `"?'.1~.1'$#1"~#3` was returning the wrong value in the debugger when inspected during fizzbuzz execution. We spent considerable effort:

- Binary searching through commits (7 compiler-touching commits across a day of work)
- Testing at the earliest commit where fizzbuzz was added — still broken
- Reverting width-specific changes to the expression emitter
- Testing standalone zero tests (which worked perfectly)
- Comparing Select/Mingle/XOR behavior at different bit widths

The binary search revealed that fizzbuzz was broken even at the commit where it was first added. This was the critical insight: **it was never a regression**.

### Understanding the zero test

The standard INTERCAL zero test idiom:

```
"?'.1~.1'$#1"~#3
```

breaks down as:

1. `'.1~.1'` — Select `.1` by itself (packs set bits contiguously)
2. `...$#1` — Mingle the select result with 1
3. `?` — Unary XOR on the 32-bit mingle result
4. `"..."~#3` — Select with mask 3 (extract bits 0 and 1)

This returns **#1 when .1 is zero** and **#2 when .1 is nonzero**.

We initially misparsed the expression as applying `?` to the select result before the mingle. The correct parse has `?` applied to the mingle result:

```
" ?(  '.1~.1' $ #1  ) " ~ #3
      ^^^^^^^^
      select      mingle    XOR     outer select
```

This distinction matters because the unary XOR operates on a 32-bit mingle result, not a 16-bit select result. Unit tests confirmed the Lib functions produce correct results at every step.

### Finding the real bug

The generated C# code was verified to be correct — the expression evaluator produces the right values. The bug was in the INTERCAL source itself.

fizzbuzz.i uses this pattern for loop termination:

```intercal
DO .5 <- "?'.1~.1'$#1"~#3    (line 66: .5 = 1 or 2)
DO .5 <- .5 ~ #1              (line 67: .5 = 1 or 0)
DO (300) NEXT                  (line 68: jump to 300)
PLEASE DO GIVE UP              (line 69: reached by RESUME 1)
(300)  DO .5 <- #0             (line 70: CLOBBERS .5!)
       DO RESUME .5            (line 71: always RESUME 0)
       DO (100) NEXT           (line 72: loop back)
```

The INTERCAL NEXT/RESUME idiom works like this:

- `DO (300) NEXT` pushes a return address and jumps to label (300)
- At label (300), `RESUME .5` pops `.5` entries from the NEXT stack
- `RESUME 1` returns to after the NEXT call; `RESUME 0` falls through

The bug: **line 70 sets `.5 <- #0` before the RESUME**. This unconditionally clobbers the zero test result. No matter what `.5` was before the NEXT, it becomes 0 at label (300), and `RESUME 0` falls through to the loop-back. The `GIVE UP` on line 69 is unreachable.

Compare with the *working* divisibility check pattern earlier in the same file:

```intercal
DO .5 <- "?'.1~.1'$#1"~#3    (line 27)
DO .5 <- .5 ~ #1              (line 28: .5 = 1 or 0)
DO (210) NEXT                  (line 29: jump to 210)
DO REINSTATE (310)             (line 30: "then" code — only reached by RESUME 1)
...
DO .5 <- #0                   (line 33: reached after "then" code executes)
(210) DO RESUME .5             (line 34)
```

Here, `.5 <- #0` is BETWEEN the NEXT and the label, not AT the label. When NEXT jumps to (210), lines 30-33 are skipped. When RESUME 1 returns to line 30, the "then" code executes, then `.5 <- #0` ensures the second pass through (210) falls through. This pattern is correct.

## The Fix

Remove `DO .5 <- #0` from the termination label:

```intercal
(300)  DO RESUME .5            ← use the zero test result directly
```

With this fix, when `.5=1` (counter equals limit), `RESUME 1` returns from the NEXT and execution reaches `GIVE UP`. When `.5=0` (counter has not reached limit), `RESUME 0` falls through to the loop-back.

## Lessons

1. **The compiler and runtime were never broken.** The Lib functions (Select, Mingle, XOR) produce correct results. The code generator emits correct C#. The bug was in the sample INTERCAL source.

2. **Standalone tests can be misleading.** The zero test idiom works perfectly in isolation. It only fails in the loop context because of the `.5 <- #0` clobbering, not because of any interaction with syslib calls.

3. **INTERCAL's NEXT/RESUME pattern is subtle.** The placement of `.5 <- #0` relative to the label determines whether the pattern works as a conditional branch or an unconditional fall-through. Moving it from "between NEXT and label" to "at the label" breaks the entire control flow.

4. **Binary search was the right instinct but the wrong scope.** We were searching compiler commits when the bug was in a sample file that never changed. The breakthrough came from unit testing the Lib functions in isolation, then comparing unit test results with the actual generated code, then tracing the NEXT/RESUME flow.
