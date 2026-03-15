        DO WRITE IN :1
        DO :2 <- #1
        DO :3 <- #3
        DO :4 <- #2
        PLEASE DO (100) NEXT
        DO GIVE UP

(100)   DO .1 <- :1 ~ #65535
        DO .5 <- "?'.1~.1'$#1"~#3
        DO (111) NEXT

        DO STASH :1 + :2 + :3 + :4
        DO .1 <- :1 ~ #65535
        DO .2 <- #1
        PLEASE DO (1010) NEXT
        DO :1 <- .3
        DO .6 <- :3
        DO :3 <- :4
        DO :4 <- .6
        DO (100) NEXT
        PLEASE RETRIEVE :1 + :2 + :3 + :4

        DO READ OUT :2
        PLEASE READ OUT :3

        DO STASH :1 + :2 + :3 + :4
        DO .1 <- :1 ~ #65535
        DO .2 <- #1
        DO (1010) NEXT
        DO :1 <- .3
        DO .6 <- :2
        DO :2 <- :4
        DO :4 <- .6
        PLEASE DO (100) NEXT
        DO RETRIEVE :1 + :2 + :3 + :4

        PLEASE RESUME #1
(111)   DO (110) NEXT
        PLEASE RESUME #2
(110)   DO RESUME .5
