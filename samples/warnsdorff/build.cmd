@echo off
REM Build Warnsdorff's Knight's Tour
REM Data tables first, then main program, then subroutine libraries
..\..\bin\churn.exe knight_attacks.i clear_mask.i center_dist.i warnsdorff.ic64 lowbit.ic64 popcount.ic64 my_add64.ic64 -b -r:syslib64.dll -noplease
if exist knight_attacks.exe (
    echo Build succeeded: knight_attacks.exe
    copy ..\..\bin\schrodie.runtime.dll . >nul 2>&1
    copy ..\..\bin\syslib64.dll . >nul 2>&1
) else (
    echo Build FAILED
)
