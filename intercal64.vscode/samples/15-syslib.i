    DO NOTE LESSON 15: THE SYSTEM LIBRARY
    DO NOTE
    DO NOTE INTERCAL has no built-in arithmetic. Addition, subtraction,
    DO NOTE and multiplication are provided by the system library via
    DO NOTE labeled subroutines called with NEXT.
    DO NOTE
    DO NOTE (1000) ADD16:    .1 + .2 -> .3
    DO NOTE (1010) MINUS16:  .1 - .2 -> .3
    DO NOTE (1040) TIMES16:  .1 * .2 -> :3 (32-bit result)
    DO NOTE (1030) DIVIDE16: .1 / .2 -> .3 quotient, .4 remainder
    DO NOTE
    DO NOTE Step through and watch .3 update after each RESUME.

    DO NOTE 10 + 25 = 35
    DO .1 <- #10
    DO .2 <- #25
    PLEASE DO (1000) NEXT
    DO READ OUT .3

    DO NOTE 100 - 42 = 58
    DO .1 <- #100
    DO .2 <- #42
    DO (1010) NEXT
    PLEASE READ OUT .3

    DO NOTE 7 * 6 = 42 (result in :3, 32-bit)
    DO .1 <- #7
    PLEASE DO .2 <- #6
    DO (1040) NEXT
    DO READ OUT :3

    DO NOTE 17 / 5 = 3 remainder 2
    DO .1 <- #17
    DO .2 <- #5
    PLEASE DO (1030) NEXT
    DO READ OUT .3
    DO READ OUT .4

    PLEASE GIVE UP
