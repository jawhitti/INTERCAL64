    DO NOTE LESSON 4: SELECT (~)
    DO NOTE
    DO NOTE Select extracts bits using a mask.
    DO NOTE .1 ~ .2 takes the bits of .1 where .2 has a 1,
    DO NOTE and packs them right-justified into the result.
    DO NOTE
    DO NOTE Select is the inverse of mingle. Together they form
    DO NOTE a complete bit manipulation system.

    DO .1 <- #255
    DO NOTE .1 = 11111111 binary
    DO .2 <- #170
    DO NOTE .2 = 10101010 binary (mask: every other bit)
    DO .3 <- .1 ~ .2
    PLEASE READ OUT .3
    DO NOTE Result: the 4 bits where the mask had 1s = 1111 = 15

    DO NOTE Select can also extract from 32-bit values
    DO :1 <- ##65535
    DO .3 <- :1 ~ ##255
    DO READ OUT .3
    PLEASE GIVE UP
