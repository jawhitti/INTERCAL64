	DO .10 <- #1
	PLEASE COME FROM (23)
	DO .11 <- !10$#1'~'#32767$#1'
	DO .12 <- #1
	PLEASE COME FROM (16)
	DO .13 <- !12$#1'~'#32767$#1'
	DO .1 <- .11
	DO .2 <- .13
	DO (2030) NEXT
	DO (11) NEXT
(15)	DO (13) NEXT
(13)	DO .3 <- "?!4~.4'$#2"~#3
	DO (14) NEXT
	PLEASE FORGET #1
	DO .1 <- .12
	DO (1020) NEXT
(16)	DO .12 <- .1
(12)	DO .3 <- '?.2$.3'~'#0$#65535'
	DO .3 <- '?"'&"!2~.3'~'"?'?.3~.3'$#32768"~"#0$#65535"'"$
                 ".3~.3"'~#1"$#2'~#3
(14)	PLEASE RESUME .3
(11)	DO (12) NEXT
	DO FORGET #1
	PLEASE READ OUT .11
	DO COME FROM (15)
	DO .1 <- .10
	DO (1020) NEXT
	DO .10 <- .1
(23)	DO (21) NEXT
(22)	PLEASE RESUME "?!10~#32768'$#2"~#3
(21)	DO (22) NEXT
	DO FORGET #1
	PLEASE GIVE UP
