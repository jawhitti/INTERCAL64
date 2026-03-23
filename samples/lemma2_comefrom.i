	DO NOTE LEMMA 2 FIX: COME FROM loop (beer.i pattern)
	DO NOTE
	DO NOTE Same goal as lemma2.i: count to 5 with syslib calls.
	DO NOTE COME FROM loop, double-NEXT trampoline, raw zero-test.
	DO NOTE Confirmed on both SCHRODIE and C-INTERCAL.
	DO NOTE
	DO NOTE Expected output: I II III IV V

	DO .1 <- #0
	DO COME FROM (99)
	DO .2 <- #1
	PLEASE DO (1000) NEXT
	DO .1 <- .3
	PLEASE DO READ OUT .1
	DO .2 <- #5
	DO (1010) NEXT
	DO .4 <- .3 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	DO (80) NEXT
(99)	DO .6 <- #0
(80)	PLEASE DO (81) NEXT
	DO GIVE UP
(81)	DO RESUME .5
