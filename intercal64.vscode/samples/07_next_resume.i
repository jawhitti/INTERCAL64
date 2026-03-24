    DO NOTE LESSON 7: NEXT AND RESUME
    DO NOTE
    DO NOTE NEXT is INTERCAL's subroutine call.
    DO NOTE   DO (100) NEXT  jumps to label (100) and pushes
    DO NOTE   the return address onto the NEXT stack.
    DO NOTE   RESUME #1 pops the stack and returns.
    DO NOTE
    DO NOTE Watch the call stack in the debugger.

    DO .1 <- #10
    DO (100) NEXT
    DO NOTE We're back! .1 was changed by the subroutine.
    PLEASE READ OUT .1
    PLEASE GIVE UP

(100) DO .1 <- #42
      PLEASE RESUME #1
