    DO NOTE LESSON 9: ABSTAIN AND REINSTATE
    DO NOTE
    DO NOTE ABSTAIN disables a statement. It still exists, but
    DO NOTE does nothing when reached. REINSTATE re-enables it.
    DO NOTE
    DO NOTE ABSTAIN by label:  DO ABSTAIN FROM (100)
    DO NOTE ABSTAIN by gerund: DO ABSTAIN FROM CALCULATING
    DO NOTE
    DO NOTE Watch the Gerund State panel in the debugger.

    DO .1 <- #1
    DO .2 <- #2

    DO ABSTAIN FROM (30)
    DO NOTE Line 30 is now disabled

(20) PLEASE READ OUT .1
(30) DO READ OUT .2
    DO NOTE Only .1 was printed. .2 was skipped.

    DO REINSTATE (30)
    DO NOTE Line 30 is back.

    DO READ OUT .1
    DO READ OUT .2
    DO NOTE Now both print.

    PLEASE GIVE UP
