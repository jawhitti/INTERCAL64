
--------------------------- MODULE TrampolineSearch ----------------------------
(*
   Exhaustive search for ALL working conditional exit patterns in a
   COME FROM loop.

   The model allows arbitrary sequences of NEXT pushes, RESUME pops,
   and FORGETs within the "trampoline" phase of a loop iteration.
   It does NOT hardcode the beer.i double-NEXT pattern — it discovers
   what works.

   The inverted invariant NoCorrectReturn asserts that correct return
   is impossible. TLC counterexamples are working patterns.

   We expect TLC to find the beer.i pattern (and variations) as the
   ONLY counterexamples.
*)
EXTENDS Sequences, Naturals, FiniteSets

CONSTANTS MaxDepth, MaxIterations, MaxTrampSteps

ASSUME MaxDepth \in Nat /\ MaxIterations \in Nat /\ MaxTrampSteps \in Nat

VARIABLES
  stack,           \* the NEXT stack (sequence of {"R", "T1", "T2", "T3"})
  phase,           \* execution phase
  iterations,      \* completed iterations
  done,            \* exit condition
  trampSteps,      \* steps taken in current trampoline phase
  returned,        \* TRUE when we've executed the final return
  pushed,          \* TRUE when at least one trampoline entry pushed this phase
  resumed,         \* TRUE when a RESUME has fired this phase
  pushCount,       \* number of pushes this iteration
  fixedPushCount   \* push count from first iteration (0 = not yet set)

vars == <<stack, phase, iterations, done, trampSteps, returned, pushed, resumed, pushCount, fixedPushCount>>

(* Stack entries *)
(* R = caller's return address *)
(* T1, T2, T3 = trampoline entries (generic, not tied to beer.i) *)
Entries == {"R", "T1", "T2", "T3"}

Phases == {"body", "tramp", "continue", "exit_path", "exiting", "done"}

Init ==
  /\ stack = <<"R">>
  /\ phase = "body"
  /\ iterations = 0
  /\ done = FALSE
  /\ trampSteps = 0
  /\ returned = FALSE
  /\ pushed = FALSE
  /\ resumed = FALSE
  /\ pushCount = 0
  /\ fixedPushCount = 0

(* === LOOP BODY === *)
(* COME FROM fires, enter body. Syslib calls are stack-neutral so omitted. *)

BodyToTramp ==
  /\ phase = "body"
  /\ phase' = "tramp"
  /\ trampSteps' = 0
  /\ pushed' = FALSE
  /\ resumed' = FALSE
  /\ pushCount' = 0
  /\ UNCHANGED <<stack, iterations, done, returned, fixedPushCount>>

(* === TRAMPOLINE PHASE === *)
(* The model allows ANY combination of pushes, pops, and forgets. *)
(* This is the key difference from COMEFROMCorrect.tla which only *)
(* offered beer.i-shaped actions. *)

(* Done/not-done is decided at RESUME time, not before. *)
(* The same pushes happen regardless. The RESUME depth *)
(* (.5 = 1 or 2) is the only thing that varies. *)
(* SetDone/SetNotDone are no longer separate actions. *)
(* Instead, Resume1Done and Resume2NotDone each set done. *)

(* Push any trampoline entry *)
(* Push — only allowed if we haven't exceeded the fixed count *)
(* (or if this is the first iteration and count isn't set yet) *)
PushT1 ==
  /\ phase = "tramp"
  /\ trampSteps < MaxTrampSteps
  /\ Len(stack) < MaxDepth
  /\ (fixedPushCount = 0 \/ pushCount < fixedPushCount)
  /\ stack' = <<"T1">> \o stack
  /\ trampSteps' = trampSteps + 1
  /\ pushed' = TRUE
  /\ pushCount' = pushCount + 1
  /\ UNCHANGED <<phase, iterations, done, returned, resumed, fixedPushCount>>

PushT2 ==
  /\ phase = "tramp"
  /\ trampSteps < MaxTrampSteps
  /\ Len(stack) < MaxDepth
  /\ (fixedPushCount = 0 \/ pushCount < fixedPushCount)
  /\ stack' = <<"T2">> \o stack
  /\ trampSteps' = trampSteps + 1
  /\ pushed' = TRUE
  /\ pushCount' = pushCount + 1
  /\ UNCHANGED <<phase, iterations, done, returned, resumed, fixedPushCount>>

PushT3 ==
  /\ phase = "tramp"
  /\ trampSteps < MaxTrampSteps
  /\ Len(stack) < MaxDepth
  /\ (fixedPushCount = 0 \/ pushCount < fixedPushCount)
  /\ stack' = <<"T3">> \o stack
  /\ trampSteps' = trampSteps + 1
  /\ pushed' = TRUE
  /\ pushCount' = pushCount + 1
  /\ UNCHANGED <<phase, iterations, done, returned, resumed, fixedPushCount>>

(* RESUME 1: pop 1 entry — this is the "done" path (.5=1) *)
(* In real INTERCAL: RESUME .5 where .5=1, computed at runtime *)
Resume1Done ==
  /\ phase = "tramp"
  /\ trampSteps < MaxTrampSteps
  /\ pushed = TRUE
  /\ pushCount = fixedPushCount \/ fixedPushCount = 0  \* must match fixed count
  /\ Len(stack) >= 1
  /\ stack' = Tail(stack)
  /\ trampSteps' = trampSteps + 1
  /\ resumed' = TRUE
  /\ done' = TRUE
  /\ phase' = "exit_path"
  /\ fixedPushCount' = pushCount   \* lock in the count
  /\ UNCHANGED <<iterations, returned, pushed, pushCount>>

(* RESUME 2: pop 2 entries — this is the "not done" path (.5=2) *)
Resume2NotDone ==
  /\ phase = "tramp"
  /\ trampSteps < MaxTrampSteps
  /\ pushed = TRUE
  /\ pushCount = fixedPushCount \/ fixedPushCount = 0  \* must match fixed count
  /\ Len(stack) >= 2
  /\ stack' = Tail(Tail(stack))
  /\ trampSteps' = trampSteps + 1
  /\ resumed' = TRUE
  /\ done' = FALSE
  /\ phase' = "continue"
  /\ fixedPushCount' = pushCount   \* lock in the count
  /\ UNCHANGED <<iterations, returned, pushed, pushCount>>

(* FORGET 1: discard 1 entry *)
Forget1 ==
  /\ phase = "tramp"
  /\ trampSteps < MaxTrampSteps
  /\ Len(stack) >= 1
  /\ stack' = Tail(stack)
  /\ trampSteps' = trampSteps + 1
  /\ UNCHANGED <<phase, iterations, done, returned, pushed, resumed, pushCount, fixedPushCount>>

(* === TRAMPOLINE EXIT DECISIONS === *)

(* Continue loop: COME FROM fires, back to body *)
(* Only allowed when not done AND at least one push+resume happened *)
(* Continue loop: only reachable via RESUME 2 (the "not done" path) *)
ContinueLoop ==
  /\ phase = "continue"
  /\ iterations < MaxIterations
  /\ phase' = "body"
  /\ iterations' = iterations + 1
  /\ trampSteps' = 0
  /\ pushed' = FALSE
  /\ resumed' = FALSE
  /\ pushCount' = 0
  /\ UNCHANGED <<stack, done, returned, fixedPushCount>>

(* Exit loop: only reachable via RESUME 1 (the "done" path) *)
(* In real INTERCAL, RESUME .5 where .5=1 selects this path *)
ExitLoop ==
  /\ phase = "exit_path"
  /\ phase' = "exiting"
  /\ UNCHANGED <<stack, iterations, done, trampSteps, returned, pushed, resumed, pushCount, fixedPushCount>>

(* === EXITING PHASE === *)
(* Can do additional forgets/resumes to clean up before final return *)

ExitForget1 ==
  /\ phase = "exiting"
  /\ Len(stack) >= 1
  /\ stack' = Tail(stack)
  /\ UNCHANGED <<phase, iterations, done, trampSteps, returned, pushed, resumed, pushCount, fixedPushCount>>

(* Final RESUME #1: pop R and return *)
FinalResume ==
  /\ phase = "exiting"
  /\ Len(stack) >= 1
  /\ Head(stack) = "R"
  /\ stack' = Tail(stack)
  /\ phase' = "done"
  /\ returned' = TRUE
  /\ UNCHANGED <<iterations, done, trampSteps, pushed, resumed, pushCount, fixedPushCount>>

Next ==
  \/ BodyToTramp
  \/ PushT1 \/ PushT2 \/ PushT3
  \/ Resume1Done \/ Resume2NotDone \/ Forget1
  \/ ContinueLoop \/ ExitLoop
  \/ ExitForget1 \/ FinalResume

Spec == Init /\ [][Next]_vars

(* === INVERTED INVARIANT === *)
(* Assert that correct return after 2+ iterations is IMPOSSIBLE. *)
(* TLC counterexamples = working patterns. *)

NoCorrectReturn ==
  ~(phase = "done" /\ returned = TRUE /\ iterations >= 2)

=============================================================================
