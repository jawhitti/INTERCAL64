    DO NOTE LESSON 8: FORGET
    DO NOTE
    DO NOTE FORGET removes entries from the NEXT stack WITHOUT
    DO NOTE returning to them. It's a one-way ticket.
    DO NOTE
    DO NOTE   DO (100) NEXT
    DO NOTE   ...
    DO NOTE   (100) DO FORGET #1    drops the return address
    DO NOTE
    DO NOTE After FORGET, execution continues forward.
    DO NOTE There is no going back.

    DO .1 <- #1
    PLEASE READ OUT .1
    DO (100) NEXT
    DO NOTE This line is never reached!
    DO .1 <- #999
    PLEASE READ OUT .1
    PLEASE GIVE UP

(100) DO FORGET #1
      DO NOTE We forgot where we came from. Continuing here.
      DO .1 <- #2
      PLEASE READ OUT .1
      PLEASE GIVE UP
