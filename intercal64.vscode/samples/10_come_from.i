    DO NOTE LESSON 10: COME FROM
    DO NOTE
    DO NOTE COME FROM is the opposite of GOTO.
    DO NOTE Instead of "go to label X", it says
    DO NOTE "after label X executes, come here."
    DO NOTE
    DO NOTE The target has no idea it's being hijacked.
    DO NOTE Watch the debugger - it highlights COME FROM targets.

    DO .1 <- #1
(10) DO READ OUT .1
    DO NOTE After line 10, control jumps to the COME FROM below.
    DO NOTE You never reach this line sequentially.
    DO .1 <- #999
    PLEASE READ OUT .1
    PLEASE GIVE UP

(20) DO COME FROM (10)
    DO NOTE Surprise! We jumped here after line 10.
    DO .1 <- #2
    PLEASE READ OUT .1
    PLEASE GIVE UP
