	PLEASE NOTE =============================================
	PLEASE NOTE STABLE MARRIAGE (GALE-SHAPLEY) IN INTERCAL
	PLEASE NOTE n=5, men-propose
	PLEASE NOTE Uses COME FROM loops, beer.i trampolines
	PLEASE NOTE by Jason Whittington and Claude (Anthropic)
	PLEASE NOTE =============================================
	PLEASE NOTE
	PLEASE NOTE DATA:
	PLEASE NOTE ,1 size 25: men's preferences (linearized)
	PLEASE NOTE ,2 size 25: women's rankings (linearized)
	PLEASE NOTE ,3 size 5: proposal indices (starts at 1)
	PLEASE NOTE ,4 size 5: women's current partner (0=free)
	PLEASE NOTE ,5 size 5: men's current match (0=free)
	PLEASE NOTE .21 = inner loop counter (man index)
	PLEASE NOTE .22 = number of free men
	PLEASE NOTE .23 = temp: current woman W
	PLEASE NOTE .24 = temp: current partner C
	PLEASE NOTE .25 = temp: proposal index / rank
	PLEASE NOTE .26 = temp: saved col for LinIdx / rank

	DO (8900) NEXT
	DO (9000) NEXT
	DO (9300) NEXT
	PLEASE GIVE UP

	PLEASE NOTE =============================================
	PLEASE NOTE (8900) INIT
	PLEASE NOTE =============================================
(8900)	DO ,1 <- #25
	DO ,2 <- #25
	DO ,3 <- #5
	DO ,4 <- #5
	PLEASE DO ,5 <- #5

	PLEASE NOTE Man 1: W3 W1 W2 W5 W4
	DO ,1 SUB #1 <- #3
	DO ,1 SUB #2 <- #1
	DO ,1 SUB #3 <- #2
	DO ,1 SUB #4 <- #5
	PLEASE DO ,1 SUB #5 <- #4
	PLEASE NOTE Man 2: W1 W3 W4 W2 W5
	DO ,1 SUB #6 <- #1
	DO ,1 SUB #7 <- #3
	DO ,1 SUB #8 <- #4
	PLEASE DO ,1 SUB #9 <- #2
	DO ,1 SUB #10 <- #5
	PLEASE NOTE Man 3: W1 W2 W3 W4 W5
	DO ,1 SUB #11 <- #1
	DO ,1 SUB #12 <- #2
	DO ,1 SUB #13 <- #3
	PLEASE DO ,1 SUB #14 <- #4
	DO ,1 SUB #15 <- #5
	PLEASE NOTE Man 4: W2 W1 W4 W3 W5
	DO ,1 SUB #16 <- #2
	DO ,1 SUB #17 <- #1
	DO ,1 SUB #18 <- #4
	PLEASE DO ,1 SUB #19 <- #3
	DO ,1 SUB #20 <- #5
	PLEASE NOTE Man 5: W3 W4 W1 W2 W5
	DO ,1 SUB #21 <- #3
	DO ,1 SUB #22 <- #4
	PLEASE DO ,1 SUB #23 <- #1
	DO ,1 SUB #24 <- #2
	DO ,1 SUB #25 <- #5

	PLEASE NOTE Woman 1 rankings: M1=1 M2=3 M3=2 M4=5 M5=4
	DO ,2 SUB #1 <- #1
	DO ,2 SUB #2 <- #3
	DO ,2 SUB #3 <- #2
	PLEASE DO ,2 SUB #4 <- #5
	DO ,2 SUB #5 <- #4
	PLEASE NOTE Woman 2 rankings: M1=3 M2=1 M3=4 M4=2 M5=5
	DO ,2 SUB #6 <- #3
	DO ,2 SUB #7 <- #1
	PLEASE DO ,2 SUB #8 <- #4
	DO ,2 SUB #9 <- #2
	DO ,2 SUB #10 <- #5
	PLEASE NOTE Woman 3 rankings: M1=2 M2=4 M3=3 M4=5 M5=1
	DO ,2 SUB #11 <- #2
	DO ,2 SUB #12 <- #4
	PLEASE DO ,2 SUB #13 <- #3
	DO ,2 SUB #14 <- #5
	DO ,2 SUB #15 <- #1
	PLEASE NOTE Woman 4 rankings: M1=2 M2=5 M3=1 M4=4 M5=3
	DO ,2 SUB #16 <- #2
	PLEASE DO ,2 SUB #17 <- #5
	DO ,2 SUB #18 <- #1
	DO ,2 SUB #19 <- #4
	DO ,2 SUB #20 <- #3
	PLEASE NOTE Woman 5 rankings: M1=3 M2=2 M3=5 M4=1 M5=4
	DO ,2 SUB #21 <- #3
	DO ,2 SUB #22 <- #2
	PLEASE DO ,2 SUB #23 <- #5
	DO ,2 SUB #24 <- #1
	DO ,2 SUB #25 <- #4

	PLEASE NOTE proposal indices start at 1
	DO ,3 SUB #1 <- #1
	DO ,3 SUB #2 <- #1
	DO ,3 SUB #3 <- #1
	PLEASE DO ,3 SUB #4 <- #1
	DO ,3 SUB #5 <- #1

	PLEASE NOTE all partners and matches start at 0
	DO ,4 SUB #1 <- #0
	DO ,4 SUB #2 <- #0
	DO ,4 SUB #3 <- #0
	PLEASE DO ,4 SUB #4 <- #0
	DO ,4 SUB #5 <- #0
	DO ,5 SUB #1 <- #0
	DO ,5 SUB #2 <- #0
	DO ,5 SUB #3 <- #0
	PLEASE DO ,5 SUB #4 <- #0
	DO ,5 SUB #5 <- #0

	DO RESUME #1

	PLEASE NOTE =============================================
	PLEASE NOTE (9000) SOLVE: nested COME FROM loops
	PLEASE NOTE outer: while free men > 0
	PLEASE NOTE inner: for M = 1 to 5
	PLEASE NOTE =============================================
(9000)	DO .22 <- #5
	DO COME FROM (9099)
	DO .21 <- #0
	DO COME FROM (9089)

	PLEASE NOTE increment inner counter
	DO .1 <- .21
	DO .2 <- #1
	DO (1000) NEXT
	DO .21 <- .3

	PLEASE NOTE check .21 > 5: subtract 6, zero means .21=6=done
	DO .1 <- .21
	DO .2 <- #6
	PLEASE DO (1010) NEXT
	DO .4 <- .3 ~ #65535
	DO .5 <- "?'.4~.4'$#1"~#3

	PLEASE NOTE inner exit trampoline
	DO (9080) NEXT

	PLEASE NOTE check if man .21 is free
	DO .3 <- ,5 SUB .21
	DO .4 <- .3 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	PLEASE NOTE .5=1 (free): propose. .5=2 (matched): skip
	DO (9070) NEXT
	DO (9089) NEXT

(9089)	DO FORGET #1

(9080)	PLEASE DO (9081) NEXT
	DO FORGET #1
	PLEASE NOTE inner loop exited: check .22 = 0
	DO .4 <- .22 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	DO (9090) NEXT
(9099)	DO .6 <- #0
(9090)	PLEASE DO (9091) NEXT
	DO FORGET #1
	DO RESUME #1

	PLEASE NOTE =============================================
	PLEASE NOTE free check handler: man .21 is free
	PLEASE NOTE =============================================
(9070)	DO (9071) NEXT
	DO FORGET #1

	PLEASE NOTE get proposal index
	DO .25 <- ,3 SUB .21

	PLEASE NOTE compute linear index for man's pref
	DO .1 <- .21
	DO .2 <- .25
	PLEASE DO (9500) NEXT
	DO .23 <- ,1 SUB .3

	PLEASE NOTE advance proposal index
	DO .1 <- .25
	DO .2 <- #1
	DO (1000) NEXT
	DO ,3 SUB .21 <- .3

	PLEASE NOTE get woman's current partner
	DO .24 <- ,4 SUB .23

	PLEASE NOTE check if woman is free
	DO .4 <- .24 ~ #65535
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	PLEASE NOTE .5=1 (free): match. .5=2 (taken): compare
	DO (9060) NEXT

	PLEASE NOTE woman is taken: compare rankings
	PLEASE NOTE get rank of new man .21 by woman .23
	DO .1 <- .23
	DO .2 <- .21
	DO (9500) NEXT
	DO .25 <- ,2 SUB .3

	PLEASE NOTE get rank of current partner .24 by woman .23
	DO .1 <- .23
	DO .2 <- .24
	PLEASE DO (9500) NEXT
	DO .26 <- ,2 SUB .3

	PLEASE NOTE compare: .25 < .26 means prefers new
	DO .1 <- .25
	DO .2 <- .26
	DO (1010) NEXT
	DO .4 <- .3 ~ #32768
	PLEASE DO .5 <- "?'.4~.4'$#1"~#3
	PLEASE NOTE .5=1 (keep current): skip. .5=2 (prefers new): replace
	DO (9050) NEXT

	PLEASE NOTE prefers new: unmatch old partner
	DO ,5 SUB .24 <- #0
	DO .1 <- .22
	DO .2 <- #1
	DO (1000) NEXT
	DO .22 <- .3

	PLEASE NOTE match new pair
	DO (9120) NEXT
	DO (9089) NEXT

	PLEASE NOTE =============================================
	PLEASE NOTE woman free handler: match directly
	PLEASE NOTE =============================================
(9060)	DO (9061) NEXT
	DO FORGET #1
	DO (9120) NEXT
	PLEASE DO (9089) NEXT

	PLEASE NOTE =============================================
	PLEASE NOTE keep current handler: skip to loop bottom
	PLEASE NOTE =============================================
(9050)	DO (9051) NEXT
	DO FORGET #1
	DO (9089) NEXT

	PLEASE NOTE =============================================
	PLEASE NOTE RESUME labels for all trampolines
	PLEASE NOTE =============================================
(9071)	DO RESUME .5
(9061)	DO RESUME .5
(9051)	DO RESUME .5
(9081)	DO RESUME .5
(9091)	DO RESUME .5

	PLEASE NOTE =============================================
	PLEASE NOTE (9120) MATCH: match man .21 with woman .23
	PLEASE NOTE =============================================
(9120)	DO ,4 SUB .23 <- .21
	DO ,5 SUB .21 <- .23
	DO .1 <- .22
	DO .2 <- #1
	PLEASE DO (1010) NEXT
	DO .22 <- .3
	DO RESUME #1

	PLEASE NOTE =============================================
	PLEASE NOTE (9500) LINIDX: .3 = (.1-1)*5 + .2
	PLEASE NOTE =============================================
(9500)	DO .26 <- .2
	DO .2 <- .1
	DO (1000) NEXT
	DO .1 <- .3
	DO (1000) NEXT
	DO .1 <- .3
	PLEASE DO (1000) NEXT
	DO .1 <- .3
	DO (1000) NEXT
	DO .1 <- .3
	DO .2 <- #5
	PLEASE DO (1010) NEXT
	DO .1 <- .3
	DO .2 <- .26
	DO (1000) NEXT
	DO RESUME #1

	PLEASE NOTE =============================================
	PLEASE NOTE (9300) PRINT: output man-to-woman matches
	PLEASE NOTE =============================================
(9300)	DO .1 <- ,5 SUB #1
	DO READ OUT .1
	DO .1 <- ,5 SUB #2
	PLEASE DO READ OUT .1
	DO .1 <- ,5 SUB #3
	DO READ OUT .1
	DO .1 <- ,5 SUB #4
	DO READ OUT .1
	PLEASE DO .1 <- ,5 SUB #5
	DO READ OUT .1
	DO RESUME #1
