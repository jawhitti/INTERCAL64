	PLEASE NOTE test_q_idempotent: collapsing twice gives same result
	DO .1 <- #77
	DO []1 <- .1
	DO ::1 <- []1
	DO ::2 <- []1
	DO READ OUT ::1
	PLEASE DO READ OUT ::2
	PLEASE GIVE UP
