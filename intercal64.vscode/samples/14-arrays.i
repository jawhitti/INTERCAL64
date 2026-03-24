    DO NOTE LESSON 14: ARRAYS
    DO NOTE
    DO NOTE , = tail (16-bit array)
    DO NOTE ; = hybrid (32-bit array)
    DO NOTE ;; = double hybrid (64-bit array)
    DO NOTE
    DO NOTE Dimension with <- then index with SUB.

    DO ,1 <- #3
    DO NOTE ,1 is now a 3-element array

    DO ,1 SUB #1 <- #10
    DO ,1 SUB #2 <- #20
    PLEASE DO ,1 SUB #3 <- #30

    DO NOTE Read values back into spots
    DO .1 <- ,1 SUB #1
    DO .2 <- ,1 SUB #2
    DO .3 <- ,1 SUB #3

    PLEASE READ OUT .1
    DO READ OUT .2
    DO READ OUT .3

    PLEASE GIVE UP
