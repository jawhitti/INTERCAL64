	DO NOTE HILBERT STATE MACHINE LOOKUP TABLES
	DO NOTE Verified against C# reference with round-trip test
	DO NOTE Index = state * 4 + quadrant + 1
	DO ;10 <- #16
	DO ;11 <- #16
	DO ;10 SUB #1 <- #0
	DO ;10 SUB #2 <- #1
	DO ;10 SUB #3 <- #3
PLEASE DO ;10 SUB #4 <- #2
	DO ;10 SUB #5 <- #0
	DO ;10 SUB #6 <- #3
	DO ;10 SUB #7 <- #1
PLEASE DO ;10 SUB #8 <- #2
	DO ;10 SUB #9 <- #2
	DO ;10 SUB #10 <- #3
	DO ;10 SUB #11 <- #1
PLEASE DO ;10 SUB #12 <- #0
	DO ;10 SUB #13 <- #2
	DO ;10 SUB #14 <- #1
	DO ;10 SUB #15 <- #3
PLEASE DO ;10 SUB #16 <- #0
	DO ;11 SUB #1 <- #1
	DO ;11 SUB #2 <- #0
	DO ;11 SUB #3 <- #3
PLEASE DO ;11 SUB #4 <- #0
	DO ;11 SUB #5 <- #0
	DO ;11 SUB #6 <- #2
	DO ;11 SUB #7 <- #1
PLEASE DO ;11 SUB #8 <- #1
	DO ;11 SUB #9 <- #3
	DO ;11 SUB #10 <- #2
	DO ;11 SUB #11 <- #1
PLEASE DO ;11 SUB #12 <- #2
	DO ;11 SUB #13 <- #2
	DO ;11 SUB #14 <- #3
	DO ;11 SUB #15 <- #0
PLEASE DO ;11 SUB #16 <- #3
