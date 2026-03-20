# test-samples.ps1 — Clean build and run sample programs
# Usage: pwsh build/test-samples.ps1
# Requires: .NET 9 SDK, schrodie solution

param(
    [switch]$SkipUnitTests,
    [int]$Timeout = 15
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

Write-Host "`n=== CLEAN ===" -ForegroundColor Cyan
dotnet clean schrodie.sln -v q 2>&1 | Out-Null
Remove-Item samples/*.dll, samples/*.exe, samples/*.pdb -ErrorAction SilentlyContinue
Remove-Item samples/*.deps.json, samples/*.runtimeconfig.json -ErrorAction SilentlyContinue
Remove-Item *.exe, *.dll, *.pdb, *.deps.json, *.runtimeconfig.json, ~tmp.* -ErrorAction SilentlyContinue
Write-Host "All artifacts removed."

Write-Host "`n=== BUILD SOLUTION ===" -ForegroundColor Cyan
dotnet build schrodie.sln -v q
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED" -ForegroundColor Red; exit 1 }

Write-Host "`n=== COPY RUNTIME ===" -ForegroundColor Cyan
Copy-Item schrodie.runtime/bin/Debug/net9.0/schrodie.runtime.dll samples/
Write-Host "schrodie.runtime.dll -> samples/"

Write-Host "`n=== COMPILE SYSLIB64 ===" -ForegroundColor Cyan
$env:MSYS_NO_PATHCONV = "1"
dotnet run --project cringe -- samples/syslib64.schrodie -t:library -b -noplease 2>&1 | Select-String -Pattern "error" -CaseSensitive:$false
if (Test-Path syslib64.dll) {
    Copy-Item syslib64.dll samples/
    Write-Host "syslib64.dll -> samples/"
} else {
    Write-Host "SYSLIB COMPILE FAILED" -ForegroundColor Red; exit 1
}

if (-not $SkipUnitTests) {
    Write-Host "`n=== UNIT TESTS ===" -ForegroundColor Cyan
    dotnet test schrodie.tests --verbosity quiet 2>&1 | Select-Object -Last 3
}

# Compile samples
$samples = @(
    @{ Name="fizzbuzz";  File="samples/fizzbuzz.i";          Syslib=$true;  Please=$true  }
    @{ Name="fizzbuzz2"; File="samples/fizzbuzz2.i";         Syslib=$true;  Please=$true  }
    @{ Name="collatz";   File="samples/collatz.i";           Syslib=$true;  Please=$false }
    @{ Name="primes";    File="samples/primes.i";            Syslib=$true;  Please=$false }
    @{ Name="alice_bob"; File="samples/alice_bob.schrodie";  Syslib=$true;  Please=$true  }
)

Write-Host "`n=== COMPILE SAMPLES ===" -ForegroundColor Cyan
foreach ($s in $samples) {
    $flags = @("-b")
    if ($s.Syslib)  { $flags += "-r:samples/syslib64.dll" }
    if (-not $s.Please) { $flags += "-noplease" }

    $allArgs = @($s.File) + $flags
    dotnet run --project cringe -- @allArgs 2>&1 | Out-Null

    if (Test-Path "$($s.Name).exe") {
        Move-Item "$($s.Name).exe" samples/ -Force
        Move-Item "$($s.Name).dll" samples/ -Force -ErrorAction SilentlyContinue
        # Create runtimeconfig if compiler didn't
        if (-not (Test-Path "samples/$($s.Name).runtimeconfig.json")) {
            @'
{ "runtimeOptions": { "tfm": "net9.0", "framework": { "name": "Microsoft.NETCore.App", "version": "9.0.0" } } }
'@ | Set-Content "samples/$($s.Name).runtimeconfig.json"
        }
        Write-Host "  $($s.Name) - compiled" -ForegroundColor Green
    } else {
        Write-Host "  $($s.Name) - COMPILE FAILED" -ForegroundColor Red
    }
}

# Run samples
Write-Host "`n=== RUN SAMPLES ===" -ForegroundColor Cyan

function Run-Sample($name, $input, $expectTerminate, $headLines) {
    Write-Host "`n--- $name ---" -ForegroundColor Yellow
    $exe = "samples/$name.exe"
    if (-not (Test-Path $exe)) {
        Write-Host "  SKIP (not compiled)" -ForegroundColor DarkGray
        return
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "dotnet"
    $psi.Arguments = $exe
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = [System.Diagnostics.Process]::Start($psi)
    if ($input) {
        $proc.StandardInput.WriteLine($input)
        $proc.StandardInput.Close()
    }

    $exited = $proc.WaitForExit($Timeout * 1000)
    $output = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()

    if (-not $exited) {
        $proc.Kill()
        if ($expectTerminate) {
            Write-Host "  FAIL — timed out after ${Timeout}s" -ForegroundColor Red
        } else {
            $lines = ($output -split "`n").Count
            Write-Host "  OK (expected non-termination, got $lines lines)" -ForegroundColor DarkYellow
        }
    } else {
        $lines = ($output -split "`n" | Where-Object { $_ -ne "" })
        $lineCount = $lines.Count
        if ($headLines -and $lineCount -gt $headLines) {
            ($lines | Select-Object -First $headLines) -join "`n"
            Write-Host "  ... ($lineCount total lines)"
        } else {
            $output.TrimEnd()
        }
        Write-Host "  PASS — terminated ($lineCount lines)" -ForegroundColor Green
    }

    if ($stderr) { Write-Host "  stderr: $stderr" -ForegroundColor DarkGray }
}

Run-Sample "fizzbuzz"  "100" $true  10
Run-Sample "collatz"   "7"   $true  0
Run-Sample "primes"    $null $false 20
Run-Sample "alice_bob" $null $true  0
Run-Sample "fizzbuzz2" $null $false 10

Write-Host "`n=== DONE ===" -ForegroundColor Cyan
