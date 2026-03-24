    DO NOTE LESSON 17: COME FROM AS A LOOP
    DO NOTE
    DO NOTE COME FROM creates a back-edge: after the target
    DO NOTE executes, control jumps to the COME FROM.
    DO NOTE
    DO NOTE This creates an infinite loop that prints 1 forever.
    DO NOTE Hit Stop in the debugger after a few iterations.
    DO NOTE
    DO NOTE To EXIT a COME FROM loop you ABSTAIN the COME FROM
    DO NOTE itself. See beer.i for the full double-NEXT trampoline
    DO NOTE pattern. That pattern is documented in the paper
    DO NOTE "COME FROM Considered Helpful."

    DO .1 <- #1
(10) DO COME FROM (20)
    PLEASE READ OUT .1
(20) DO NOTE COME FROM pulls control back to (10).

    DO NOTE You never reach here.
    PLEASE GIVE UP
