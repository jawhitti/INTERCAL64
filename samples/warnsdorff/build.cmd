@echo off
REM Build Warnsdorff's Knight's Tour
REM Data tables first, then main program, then subroutine libraries
..\..\bin\schrodie.exe knight_attacks.i clear_mask.i center_dist.i warnsdorff.schrodie lowbit.schrodie popcount.schrodie bit_to_index.schrodie my_add64.schrodie -b -r:syslib64.dll -noplease
if exist knight_attacks.exe (
    echo Build succeeded: knight_attacks.exe
    copy ..\..\bin\schrodie.runtime.dll . >nul 2>&1
    copy ..\..\bin\syslib64.dll . >nul 2>&1
) else (
    echo Build FAILED
)
