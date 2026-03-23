	PLEASE NOTE LEMMA 1 REPRODUCER
	PLEASE NOTE
	PLEASE NOTE Callable subroutine (500) contains a FORGET loop
	PLEASE NOTE that calls syslib (1000) to add 1 each iteration.
	PLEASE NOTE The FORGET on iteration 1 destroys the caller's
	PLEASE NOTE return address R. The subroutine cannot return.
	PLEASE NOTE
	PLEASE NOTE Expected: prints 5 then returns to main
	PLEASE NOTE Actual: R destroyed, RESUME #1 goes somewhere wrong

	DO .1 <- #0
	DO (500) NEXT
	DO READ OUT .1
	PLEASE GIVE UP

	PLEASE NOTE SUBROUTINE: add 1 five times, return result in .1
(500)	DO .1 <- #0
(501)	DO FORGET #1
	DO .2 <- #1
	DO (1000) NEXT
	DO .1 <- .3
	PLEASE NOTE check if .1 = 5
	DO .2 <- #5
	DO (1010) NEXT
	DO .2 <- .3 ~ #65535
	DO .5 <- "?'.2~.2'$#1"~#3
	DO .5 <- .5 ~ #1
	DO (502) NEXT
	PLEASE RESUME #1
(502)	DO RESUME .5
	DO (501) NEXT
	PLEASE RESUME #1
