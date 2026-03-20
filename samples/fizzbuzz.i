        DO ,1 <- #6
        DO ,1SUB#1 <- #158
        PLEASE DO ,1SUB#2 <- #208
        DO ,1SUB#3 <- #56
        DO ,1SUB#4 <- #0
        DO ,1SUB#5 <- #10
        DO ,1SUB#6 <- ##4294966864
        PLEASE DO ,2 <- #6
        DO ,2SUB#1 <- #190
        DO ,2SUB#2 <- #152
        DO ,2SUB#3 <- #80
        DO ,2SUB#4 <- #0
        PLEASE DO ,2SUB#5 <- #10
        DO ,2SUB#6 <- ##4294966864
        DO :1 <- #1
        DO :2 <- #2
        PLEASE DO :3 <- #4
        DO (100) NEXT
        DO GIVE UP

(100)   DO FORGET #1
        PLEASE ABSTAIN FROM (310)
        DO ABSTAIN FROM (320)
        DO REINSTATE (330)

        DO .1 <- :2 ~ #65535
        PLEASE DO .5 <- "?'.1~.1'$#1"~#3
        DO .5 <- .5 ~ #1
        DO (210) NEXT
        DO REINSTATE (310)
        PLEASE DO ABSTAIN FROM (330)
        DO :2 <- #3
        DO .5 <- #0
(210)   DO RESUME .5

        PLEASE DO .1 <- :3 ~ #65535
        DO .5 <- "?'.1~.1'$#1"~#3
        DO .5 <- .5 ~ #1
        DO (220) NEXT
        PLEASE DO REINSTATE (320)
        DO ABSTAIN FROM (330)
        DO :3 <- #5
        DO .5 <- #0
(220)   DO RESUME .5

(310)   DO READ OUT ,1
(320)   PLEASE DO READ OUT ,2
(330)   DO READ OUT :1

        DO .1 <- :2 ~ #65535
        DO .2 <- #1
        PLEASE DO (1010) NEXT
        DO :2 <- .3
        DO .1 <- :3 ~ #65535
        DO .2 <- #1
        PLEASE DO (1010) NEXT
        DO :3 <- .3
        DO .1 <- :1 ~ #65535
        DO .2 <- #1
        DO (1009) NEXT
        PLEASE DO :1 <- .3
        DO .1 <- :1 ~ #65535
        DO .2 <- #101
        DO (1010) NEXT
        PLEASE DO .1 <- .3 ~ #65535
        DO .5 <- "?'.1~.1'$#1"~#3
        DO .5 <- .5 ~ #1
        DO (300) NEXT
        PLEASE DO GIVE UP
(300)   DO RESUME .5
        DO (100) NEXT
        PLEASE RESUME #1
