    DO NOTE LESSON 5: UNARY OPERATORS
    DO NOTE
    DO NOTE Unary operators work on adjacent bit pairs within a value:
    DO NOTE   & = AND adjacent bits
    DO NOTE   V = OR adjacent bits
    DO NOTE   ? = XOR adjacent bits
    DO NOTE
    DO NOTE Bit N of result = bit N op bit N-1 (wrapping around).
    DO NOTE Sparks ' and ears " group expressions.

    DO .1 <- #255
    DO NOTE .1 = 0000000011111111

    DO .2 <- '&.1'
    DO NOTE unary AND: each bit ANDed with its neighbor
    PLEASE READ OUT .2

    DO .3 <- 'V.1'
    DO NOTE unary OR: each bit ORed with its neighbor
    DO READ OUT .3

    DO .4 <- '?.1'
    DO NOTE unary XOR: each bit XORed with its neighbor
    DO NOTE XOR detects where bits CHANGE
    PLEASE READ OUT .4

    PLEASE GIVE UP
