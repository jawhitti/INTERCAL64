	DO NOTE LEMMA 1 FIX: COME FROM loop inside callable subroutine
	DO NOTE
	DO NOTE Same goal as lemma1.i: subroutine adds 1 five times.
	DO NOTE COME FROM loop, R survives, RESUME #1 returns to caller.
	DO NOTE Confirmed on both SCHRODIE and C-INTERCAL.
	DO NOTE
	DO NOTE Expected output: V

	DO .1 <- #0
	DO (500) NEXT
	PLEASE DO READ OUT .1
	DO GIVE UP

	DO NOTE SUBROUTINE: add 1 five times via COME FROM loop
(500)	DO .1 <- #0
	DO COME FROM (599)
	DO .2 <- #1
	PLEASE DO (1000) NEXT
	DO .1 <- .3
	DO .2 <- #5
	DO (1010) NEXT
	DO .4 <- .3 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	DO (80) NEXT
(599)	DO .6 <- #0
(80)	PLEASE DO (81) NEXT
	DO FORGET #1
	DO RESUME #1
(81)	DO RESUME .5
