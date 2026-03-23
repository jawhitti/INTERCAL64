
--------------------------- MODULE ComeFromTrampoline ---------------------------
(*
   Model of a COME FROM loop with N beer.i double-NEXT trampolines per iteration.

   Question: Does the stack depth return to baseline (1 entry = R) after each
   iteration, for all combinations of trampoline paths across arbitrary iterations?

   Each trampoline has two paths:
     .5=1 (inside): RESUME 1 pops R_inner, enters wrapper. FORGET 1 pops R_outer. Net: 0.
     .5=2 (outside): RESUME 2 pops R_inner + R_outer. Net: 0.

   Between trampolines, syslib calls may occur (stack-neutral: push sentinel,
   internal work, RESUME 2 pops internal + sentinel).

   The model explores all 2^N path combinations per iteration across multiple
   iterations to check whether the stack invariant holds.
*)
EXTENDS Sequences, Naturals, FiniteSets

CONSTANTS
  NumTrampolines,    \* number of trampolines per iteration (e.g., 3 for Stable Marriage)
  MaxIterations,     \* bound on iterations to explore
  MaxStackDepth      \* stack limit (80 in INTERCAL)

ASSUME NumTrampolines \in Nat /\ MaxIterations \in Nat /\ MaxStackDepth \in Nat

VARIABLES
  stack,             \* the shared NEXT stack (sequence of labels)
  phase,             \* current execution phase
  iteration,         \* current iteration count
  trampIdx,          \* which trampoline we're evaluating (1..NumTrampolines)
  tramphase,         \* phase within a trampoline evaluation
  done               \* algorithm terminated

vars == <<stack, phase, iteration, trampIdx, tramphase, done>>

(* Stack entries *)
Entries == {"R", "Outer", "Inner", "Sentinel", "SyslibInt", "Skip"}

(* Phases *)
Phases == {"init", "loop_top", "tramp_eval", "tramp_inside", "between_tramps",
           "syslib_call", "loop_bottom", "exit_check", "exiting", "terminated"}

(* Trampoline sub-phases *)
TrampPhases == {"push_outer", "at_outer", "push_inner", "at_inner",
                "resume", "inside_forget", "inside_done", "outside_done"}

Init ==
  /\ stack = <<"R">>         \* caller pushed R
  /\ phase = "init"
  /\ iteration = 0
  /\ trampIdx = 0
  /\ tramphase = "push_outer"
  /\ done = FALSE

(* Enter the COME FROM loop *)
EnterLoop ==
  /\ phase = "init"
  /\ phase' = "loop_top"
  /\ UNCHANGED <<stack, iteration, trampIdx, tramphase, done>>

(* Start of iteration: COME FROM has fired, begin first trampoline *)
LoopTop ==
  /\ phase = "loop_top"
  /\ iteration < MaxIterations
  /\ iteration' = iteration + 1
  /\ trampIdx' = 1
  /\ tramphase' = "push_outer"
  /\ phase' = "tramp_eval"
  /\ UNCHANGED <<stack, done>>

(* === TRAMPOLINE EVALUATION === *)

(* Step 1: Push R_outer (DO (outer) NEXT) *)
TrampPushOuter ==
  /\ phase = "tramp_eval"
  /\ tramphase = "push_outer"
  /\ Len(stack) < MaxStackDepth
  /\ stack' = <<"Outer">> \o stack
  /\ tramphase' = "at_outer"
  /\ UNCHANGED <<phase, iteration, trampIdx, done>>

(* Step 2: At (outer), push R_inner (DO (inner) NEXT) *)
TrampPushInner ==
  /\ phase = "tramp_eval"
  /\ tramphase = "at_outer"
  /\ Len(stack) < MaxStackDepth
  /\ stack' = <<"Inner">> \o stack
  /\ tramphase' = "resume"
  /\ UNCHANGED <<phase, iteration, trampIdx, done>>

(* Step 3a: RESUME .5 = 1 (inside path) — pop R_inner only *)
TrampResume1 ==
  /\ phase = "tramp_eval"
  /\ tramphase = "resume"
  /\ Len(stack) >= 1
  /\ Head(stack) = "Inner"
  /\ stack' = Tail(stack)            \* pop R_inner
  /\ tramphase' = "inside_forget"
  /\ UNCHANGED <<phase, iteration, trampIdx, done>>

(* Step 3b: RESUME .5 = 2 (outside path) — pop R_inner + R_outer *)
TrampResume2 ==
  /\ phase = "tramp_eval"
  /\ tramphase = "resume"
  /\ Len(stack) >= 2
  /\ Head(stack) = "Inner"
  /\ Head(Tail(stack)) = "Outer"
  /\ stack' = Tail(Tail(stack))      \* pop R_inner and R_outer
  /\ tramphase' = "outside_done"
  /\ UNCHANGED <<phase, iteration, trampIdx, done>>

(* Step 4 (inside path): FORGET #1 to clean up R_outer *)
TrampInsideForget ==
  /\ phase = "tramp_eval"
  /\ tramphase = "inside_forget"
  /\ Len(stack) >= 1
  /\ Head(stack) = "Outer"
  /\ stack' = Tail(stack)            \* pop R_outer
  /\ tramphase' = "inside_done"
  /\ UNCHANGED <<phase, iteration, trampIdx, done>>

(* Removed: TrampInsideForgetWrong and TrampInsideForgetEmpty *)
(* We model only correct execution — FORGET always finds R_outer on top *)
(* The question is whether correct execution preserves the stack invariant *)

(* Trampoline done (either path): advance to next trampoline or syslib *)
TrampDone ==
  /\ phase = "tramp_eval"
  /\ tramphase \in {"inside_done", "outside_done"}
  /\ phase' = "between_tramps"
  /\ UNCHANGED <<stack, iteration, trampIdx, tramphase, done>>

(* === BETWEEN TRAMPOLINES === *)

(* Optional syslib call: push sentinel, then pop sentinel + internal *)
(* Modeled as net-zero: push sentinel, push internal, RESUME 2 pops both *)
SyslibCallPush ==
  /\ phase = "between_tramps"
  /\ Len(stack) < MaxStackDepth - 1
  /\ stack' = <<"SyslibInt">> \o (<<"Sentinel">> \o stack)
  /\ phase' = "syslib_call"
  /\ UNCHANGED <<iteration, trampIdx, tramphase, done>>

SyslibCallResume ==
  /\ phase = "syslib_call"
  /\ Len(stack) >= 2
  /\ Head(stack) = "SyslibInt"
  /\ Head(Tail(stack)) = "Sentinel"
  /\ stack' = Tail(Tail(stack))
  /\ phase' = "between_tramps"
  /\ UNCHANGED <<iteration, trampIdx, tramphase, done>>

(* Advance to next trampoline *)
NextTrampoline ==
  /\ phase = "between_tramps"
  /\ trampIdx < NumTrampolines
  /\ trampIdx' = trampIdx + 1
  /\ tramphase' = "push_outer"
  /\ phase' = "tramp_eval"
  /\ UNCHANGED <<stack, iteration, done>>

(* All trampolines done: go to loop bottom *)
AllTrampsDone ==
  /\ phase = "between_tramps"
  /\ trampIdx = NumTrampolines
  /\ phase' = "loop_bottom"
  /\ UNCHANGED <<stack, iteration, trampIdx, tramphase, done>>

(* === LOOP BOTTOM === *)

(* At loop bottom: push skip entry, FORGET at COME FROM target, loop *)
LoopBottomSkip ==
  /\ phase = "loop_bottom"
  /\ Len(stack) < MaxStackDepth
  /\ stack' = <<"Skip">> \o stack      \* DO (target) NEXT
  /\ phase' = "loop_top"               \* COME FROM fires after FORGET
  /\ UNCHANGED <<iteration, trampIdx, tramphase, done>>

(* FORGET the skip entry at the COME FROM target *)
(* Note: in practice this is (target) DO FORGET #1; COME FROM fires *)
(* Modeled as: push Skip, goto target, FORGET pops Skip, COME FROM loops *)
LoopBottomForgetAndLoop ==
  /\ phase = "loop_bottom"
  /\ phase' = "loop_top"              \* COME FROM fires
  /\ UNCHANGED <<stack, iteration, trampIdx, tramphase, done>>

(* Exit the loop *)
ExitLoop ==
  /\ phase = "loop_bottom"
  /\ phase' = "exit_check"
  /\ UNCHANGED <<stack, iteration, trampIdx, tramphase, done>>

(* === EXIT === *)

(* Try to return: RESUME #1 should pop R *)
TryReturn ==
  /\ phase = "exit_check"
  /\ Len(stack) >= 1
  /\ phase' = "terminated"
  /\ done' = TRUE
  /\ stack' = Tail(stack)
  /\ UNCHANGED <<iteration, trampIdx, tramphase>>

Next ==
  \/ EnterLoop
  \/ LoopTop
  \/ TrampPushOuter
  \/ TrampPushInner
  \/ TrampResume1
  \/ TrampResume2
  \/ TrampInsideForget
  \/ TrampDone
  \/ SyslibCallPush
  \/ SyslibCallResume
  \/ NextTrampoline
  \/ AllTrampsDone
  \/ LoopBottomForgetAndLoop
  \/ ExitLoop
  \/ TryReturn

Spec == Init /\ [][Next]_vars

(* === INVARIANTS === *)

(* The stack should never exceed the INTERCAL limit *)
StackBounded == Len(stack) <= MaxStackDepth

(* After each complete iteration (at loop_top), the stack should have
   exactly 1 entry (R). If this fails, trampolines are leaking. *)
StackInvariantAtLoopTop ==
  (phase = "loop_top" /\ iteration > 0) => Len(stack) = 1

(* When we try to return, R should be on top *)
ReturnFindsR ==
  phase = "exit_check" => (Len(stack) >= 1 /\ Head(stack) = "R")

(* The stack should never have R consumed before we try to return *)
RPreserved ==
  phase # "terminated" =>
    \E i \in 1..Len(stack) : stack[i] = "R"

=============================================================================
