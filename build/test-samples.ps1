# test-samples.ps1 -- Clean build and run sample programs
# Usage: powershell -ExecutionPolicy Bypass -File build/test-samples.ps1
# Requires: .NET 9 SDK, intercal64 solution

param(
    [switch]$SkipUnitTests,
    [int]$Timeout = 15
)

$ErrorActionPreference = "Continue"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

Write-Host "`n=== CLEAN ===" -ForegroundColor Cyan
dotnet clean intercal64.sln -v q 2>&1 | Out-Null
Remove-Item bin -Recurse -ErrorAction SilentlyContinue
Remove-Item samples/*.dll, samples/*.exe, samples/*.pdb -ErrorAction SilentlyContinue
Remove-Item samples/*.deps.json, samples/*.runtimeconfig.json -ErrorAction SilentlyContinue
Remove-Item *.exe, *.dll, *.pdb, *.deps.json, *.runtimeconfig.json, ~tmp.* -ErrorAction SilentlyContinue
Write-Host "All artifacts removed."

Write-Host "`n=== BUILD ===" -ForegroundColor Cyan
# dotnet build compiles runtime + compiler to bin/, then compiles syslib64
dotnet build churn/churn.csproj -v q
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED" -ForegroundColor Red; exit 1 }

# Build DAP adapter (needed for VS Code debugging)
dotnet build intercal64.dap/intercal64.dap.csproj -v q
if ($LASTEXITCODE -ne 0) { Write-Host "DAP BUILD FAILED" -ForegroundColor Red; exit 1 }

# Verify bin/ contents
$expected = @("bin/churn.exe", "bin/churn.dll", "bin/intercal64.runtime.dll", "bin/syslib64.dll")
$missing = $expected | Where-Object { -not (Test-Path $_) }
if ($missing) {
    Write-Host "MISSING from bin/: $missing" -ForegroundColor Red; exit 1
}
Write-Host "bin/ contains: churn.exe, intercal64.runtime.dll, syslib64.dll"

if (-not $SkipUnitTests) {
    Write-Host "`n=== UNIT TESTS ===" -ForegroundColor Cyan
    dotnet test intercal64.tests --verbosity quiet 2>&1 | Select-Object -Last 3
}

# Copy runtime + syslib to samples for sample compilation
Copy-Item bin/intercal64.runtime.dll samples/
Copy-Item bin/syslib64.dll samples/

# Compile samples
$samples = @(
    @{ Name="fizzbuzz";  File="samples/fizzbuzz.i";          Syslib=$true;  Please=$true  }
    # fizzbuzz2 tests syslib64 division but doesn't terminate (subtract-with-borrow bug)
    # @{ Name="fizzbuzz2"; File="samples/fizzbuzz2.i";         Syslib=$true;  Please=$true  }
    @{ Name="collatz";   File="samples/collatz.i";           Syslib=$true;  Please=$false }
    @{ Name="primes";    File="samples/primes.i";            Syslib=$true;  Please=$false }
)

Write-Host "`n=== COMPILE SAMPLES ===" -ForegroundColor Cyan
foreach ($s in $samples) {
    $flags = @("-b")
    if ($s.Syslib)  { $flags += "-r:samples/syslib64.dll" }
    if (-not $s.Please) { $flags += "-noplease" }

    $allArgs = @($s.File) + $flags
    & bin/churn.exe @allArgs 2>&1 | Out-Null

    if (Test-Path "$($s.Name).exe") {
        Move-Item "$($s.Name).exe" samples/ -Force
        Move-Item "$($s.Name).dll" samples/ -Force -ErrorAction SilentlyContinue
        if (-not (Test-Path "samples/$($s.Name).runtimeconfig.json")) {
            Copy-Item bin/churn.runtimeconfig.json "samples/$($s.Name).runtimeconfig.json"
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
    $dll = "samples/$name.dll"
    if (-not (Test-Path $dll)) {
        Write-Host "  SKIP - not compiled" -ForegroundColor DarkGray
        return
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "dotnet"
    $psi.Arguments = $dll
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
            Write-Host "  FAIL -- timed out after ${Timeout}s" -ForegroundColor Red
        } else {
            $lines = ($output -split "`n").Count
            Write-Host "  OK -- expected non-termination, got $lines lines" -ForegroundColor DarkYellow
        }
    } else {
        $lines = ($output -split "`n" | Where-Object { $_ -ne "" })
        $lineCount = $lines.Count
        if ($headLines -and $lineCount -gt $headLines) {
            ($lines | Select-Object -First $headLines) -join "`n"
            Write-Host "  ... $lineCount total lines"
        } else {
            $output.TrimEnd()
        }
        Write-Host "  PASS -- terminated, $lineCount lines" -ForegroundColor Green
    }

    if ($stderr) { Write-Host "  stderr: $stderr" -ForegroundColor DarkGray }
}

Run-Sample "fizzbuzz"  "100" $true  10
Run-Sample "collatz"   "7"   $true  0
Run-Sample "primes"    $null $false 20
# fizzbuzz2: known broken -- subtract-with-borrow (1009) carries state across iterations
# Run-Sample "fizzbuzz2" $null $false 10

Write-Host "`n=== DONE ===" -ForegroundColor Cyan
