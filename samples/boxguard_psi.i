	DO .1 <- #42
	DO .2 <- #99
	PLEASE DO []1 <- .1
	DO []2 <- .2
	DO ENTANGLE []1 + []2
	DO ⟨1|ψ⟩ READ OUT .1
	PLEASE DO ⟨2|ψ⟩ READ OUT .2
	DO GIVE UP
