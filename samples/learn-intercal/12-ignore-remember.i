    DO NOTE LESSON 12: IGNORE AND REMEMBER
    DO NOTE
    DO NOTE IGNORE makes a variable read-only. Any assignment
    DO NOTE to it silently does nothing. REMEMBER unlocks it.
    DO NOTE
    DO NOTE This is INTERCAL's access control.

    DO .1 <- #10
    PLEASE READ OUT .1

    DO IGNORE .1
    DO NOTE .1 is now locked.

    DO .1 <- #99
    DO NOTE That assignment was silently ignored!
    PLEASE READ OUT .1
    DO NOTE Still 10.

    DO REMEMBER .1
    DO NOTE .1 is unlocked.

    DO .1 <- #99
    PLEASE READ OUT .1
    DO NOTE Now it's 99.

    PLEASE GIVE UP
