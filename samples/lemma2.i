	DO NOTE LEMMA 2 REPRODUCER
	DO NOTE
	DO NOTE FORGET loop with syslib calls. Adds 1 each iteration.
	DO NOTE Expected output: I II III IV V
	DO NOTE Actual: E421 on C-INTERCAL, infinite loop on SCHRODIE

	DO .1 <- #0
(100)	DO FORGET #1
	DO .2 <- #1
	PLEASE DO (1000) NEXT
	DO .1 <- .3
	PLEASE DO READ OUT .1
	DO .2 <- #5
	DO (1010) NEXT
	DO .4 <- .3 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	DO .5 <- .5 ~ #1
	DO (300) NEXT
	DO GIVE UP
(300)	DO RESUME .5
	DO (100) NEXT
	PLEASE RESUME #1
