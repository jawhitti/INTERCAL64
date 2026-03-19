	PLEASE NOTE test_q_three: 3-way entanglement, exactly one survivor
	DO .1 <- #10
	DO .2 <- #20
	DO .3 <- #30
	DO []1 <- .1
	DO []2 <- .2
	DO []3 <- .3
	PLEASE ENTANGLE []1 + []2 + []3
	DO ::1 <- []1
	DO ::2 <- []2
	DO ::3 <- []3
	DO READ OUT ::1
	DO READ OUT ::2
	PLEASE DO READ OUT ::3
	PLEASE GIVE UP
