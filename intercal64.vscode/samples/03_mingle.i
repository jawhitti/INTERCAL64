    DO NOTE LESSON 3: MINGLE ($)
    DO NOTE
    DO NOTE Mingle interleaves the bits of two values.
    DO NOTE .1 $ .2 takes alternating bits: .1 in even positions, .2 in odd.
    DO NOTE Two 16-bit values mingle into one 32-bit value.
    DO NOTE
    DO NOTE Example: #3 = 11 binary, #5 = 101 binary
    DO NOTE   #3 $ #5 = 1 1 0 1 (interleaved) = 11 01 01 = 0b110101
    DO NOTE   That's... well, step through and find out.

    DO .1 <- #3
    DO .2 <- #5
    DO :1 <- .1 $ .2
    PLEASE READ OUT :1
    DO NOTE Now try with larger values
    DO .1 <- #255
    DO .2 <- #0
    DO :1 <- .1 $ .2
    DO READ OUT :1
    PLEASE GIVE UP
