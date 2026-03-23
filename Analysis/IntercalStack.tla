
---------------------------- MODULE IntercalStack ----------------------------
EXTENDS Sequences, Naturals, FiniteSets

CONSTANTS MaxDepth, MaxIterations

ASSUME MaxDepth \in Nat /\ MaxIterations \in Nat

VARIABLES
  stack,          \* the NEXT stack
  rConsumed,      \* TRUE if R has been popped
  phase,          \* current phase of the loop
  iterations,     \* number of complete loop iterations
  bodyPushed

Phases == {"entering", "top", "body", "bottom", "returning"}

Init ==
  /\ stack = <<"R">>    \* caller has pushed R
  /\ rConsumed = FALSE
  /\ phase = "entering"
  /\ iterations = 0
  /\ bodyPushed = FALSE

\* Enter the loop: push the first loop-back NEXT entry
Enter ==
  /\ phase = "entering"
  /\ Len(stack) < MaxDepth
  /\ stack' = <<"L">> \o stack
  /\ phase' = "top"
  /\ bodyPushed' = FALSE
  /\ UNCHANGED <<rConsumed, iterations>>

\* Top of loop: FORGET fires
LoopForget ==
  /\ phase = "top"
  /\ Len(stack) >= 1
  /\ rConsumed' = (rConsumed \/ Head(stack) = "R")
  /\ stack' = Tail(stack)
  /\ phase' = "body"
  /\ iterations' = iterations + 1
  /\ bodyPushed' = FALSE

\* Body: optionally push subroutine entries (models syslib calls)
PushS ==
  /\ phase = "body"
  /\ Len(stack) < MaxDepth
  /\ stack' = <<"S">> \o stack
  /\ UNCHANGED <<rConsumed, phase, iterations>>
  /\ bodyPushed' = TRUE

\* Body: subroutine returns with RESUME #1
SubResume1 ==
  /\ phase = "body"
  /\ Len(stack) >= 1
  /\ Head(stack) = "S"
  /\ rConsumed' = (rConsumed \/ Head(stack) = "R")
  /\ stack' = Tail(stack)
  /\ UNCHANGED <<phase, iterations, bodyPushed>>

\* Body: subroutine returns with RESUME #2 (syslib pattern)
SubResume2 ==
  /\ phase = "body"
  /\ Len(stack) >= 2
  /\ Head(stack) = "S"
  /\ rConsumed' = (rConsumed
                  \/ Head(stack) = "R"
                  \/ Head(Tail(stack)) = "R")
  /\ stack' = Tail(Tail(stack))
  /\ UNCHANGED <<phase, iterations, bodyPushed>>

\* Move to bottom of loop
BodyDone ==
  /\ bodyPushed = TRUE
  /\ phase = "body"
  /\ ~\E i \in 1..Len(stack) : stack[i] = "S"
  /\ phase' = "bottom"
  /\ UNCHANGED <<stack, rConsumed, iterations, bodyPushed>>

\* Bottom of loop: push loop-back NEXT entry, go back to top
LoopNext ==
  /\ phase = "bottom"
  /\ Len(stack) < MaxDepth
  /\ iterations < MaxIterations
  /\ stack' = <<"L">> \o stack
  /\ phase' = "top"
  /\ bodyPushed = FALSE
  /\ UNCHANGED <<rConsumed, iterations>>

\* Exit the loop: instead of looping, attempt to return
ExitLoop ==
  /\ phase = "bottom"
  /\ phase' = "returning"
  /\ UNCHANGED <<stack, rConsumed, iterations, bodyPushed>>

\* Attempt to return to caller
DoReturn ==
  /\ phase = "returning"
  /\ Len(stack) >= 1
  /\ rConsumed' = (rConsumed \/ Head(stack) = "R")
  /\ stack' = Tail(stack)
  /\ UNCHANGED <<phase, iterations, bodyPushed>>

Next == Enter \/ LoopForget \/ PushS \/ SubResume1 \/ SubResume2
             \/ BodyDone \/ LoopNext \/ ExitLoop \/ DoReturn

Spec == Init /\ [][Next]_<<stack, rConsumed, phase, iterations, bodyPushed>>

\* The property: after at least 2 iterations,
\* the loop cannot return R to its caller
Safety ==
  ~(phase = "returning"
    /\ iterations >= 2
    /\ ~rConsumed
    /\ stack # <<>>
    /\ Head(stack) = "R")

=============================================================================
