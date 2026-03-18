# Installing the INTERCAL Debugger for VS Code

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [VS Code](https://code.visualstudio.com/)
- Git (to clone the repo)

## Step 1: Clone and Build

```bash
git clone https://github.com/jawhitti/INTERCAL.git
cd INTERCAL

# Build everything
dotnet build intercal.sln

# Build the DAP adapter
dotnet build intercal.dap/intercal.dap.csproj

# Build the syslib (needed for programs that use standard library routines)
cd samples
dotnet run --project ../cringe/cringe.csproj -- syslib64.i -b -t:library -noplease
cd ..
```

## Step 2: Install the VS Code Extension

Copy the extension files to your VS Code extensions directory:

**Windows:**
```bash
mkdir %USERPROFILE%\.vscode\extensions\intercal
xcopy /s /y vscode-intercal\* %USERPROFILE%\.vscode\extensions\intercal\
```

**macOS / Linux:**
```bash
mkdir -p ~/.vscode/extensions/intercal
cp -r vscode-intercal/* ~/.vscode/extensions/intercal/
```

Restart VS Code after copying.

## Step 3: Configure the Project Root

The extension needs to find the compiler and DAP adapter. It auto-detects the project root by searching:

1. The `intercal.projectRoot` VS Code setting (if set)
2. Parent directories of the extension install path
3. Open workspace folders and their parents

**If auto-detection fails**, set the path manually in VS Code settings:

```json
{
    "intercal.projectRoot": "/path/to/INTERCAL"
}
```

This should point to the repo root — the directory containing `intercal.dap/`, `cringe/`, and `samples/`.

## Step 4: Set Up Your Working Directory

Your `.i` source files need access to `intercal.runtime.dll` and (optionally) `syslib64.dll`. The simplest approach is to work in the `samples/` directory, which already has these. If you want to work elsewhere, copy them:

```bash
cp samples/intercal.runtime.dll /your/project/dir/
cp samples/syslib64.dll /your/project/dir/       # optional, for syslib routines
```

The debugger looks for `syslib64.dll` in the same directory as your `.i` file and in its parent directory.

## Step 5: Debug

1. Open a `.i` file in VS Code
2. Set breakpoints by clicking in the gutter
3. Press F5 (or Run > Start Debugging)
4. If prompted, select "INTERCAL Debugger"

The debugger will compile your program with debug hooks and launch it. On the first run this takes a few seconds; subsequent runs skip compilation if the source hasn't changed.

### launch.json (Optional)

VS Code will prompt you to create a launch configuration. The default works for most cases:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "type": "intercal",
            "request": "launch",
            "name": "Debug INTERCAL Program",
            "program": "${file}"
        }
    ]
}
```

**Optional properties:**

| Property | Description |
|----------|-------------|
| `compiler` | Path to `cringe.csproj` (auto-detected if not set) |
| `syslib` | Path to `syslib64.dll` (auto-detected if not set) |

## What Works

- **Breakpoints** — set, hit, and verified
- **Stepping** — Step In, Step Over, Continue
- **Variables panel** — spot (`.`), two-spot (`:`), four-spot (`::`), and cat box (`[]`) variables
- **Quantum boxes** — shows `?` (uncollapsed), the value (alive), or `(dead)` (DEDKITTY)
- **Collapse via edit** — double-click a box variable in the Variables panel to collapse it (and all entangled boxes)
- **ABSTAIN tracking** — abstained statements are auto-skipped during stepping, with messages in the debug console
- **Gerund State** — expandable scope in the Variables panel showing which statements are abstained
- **Program output** — READ OUT output appears in the debug console
- **Program input** — type in the debug console when the program executes WRITE IN
- **NEXT stack** — displayed in the Call Stack panel
- **Bralette guards** — `⟨N|ψ⟩` notation highlighted in bold red

## Troubleshooting

### "Waiting for debuggee to connect..." then timeout

The compiled program doesn't have debug hooks. This happens when:
- The program was previously compiled from the command line (without `-debug-dap`)
- The cached `.exe` is stale

**Fix:** Delete the `.exe` file next to your `.i` file and try again. The debugger will recompile with debug hooks.

### Breakpoints not hitting

Make sure the breakpoint is on a line that contains an executable INTERCAL statement. Lines that are continuations (starting with whitespace) or NOTEs are not breakpointable.

### "E998 EXCUSE ME, YOU MUST HAVE ME CONFUSED WITH SOME OTHER COMPILER"

The compiler can't find your source file. Check that the `program` path in `launch.json` points to a valid `.i` file.

### Variables panel shows nothing

The program hasn't executed any statements yet. Step forward at least once.

### syslib routines not found (E129)

Your program uses system library labels (1000-series) but `syslib64.dll` isn't in scope. Either:
- Copy `syslib64.dll` to the same directory as your `.i` file
- Set the `syslib` property in `launch.json`

### Extension not activating

Make sure the file has a `.i` extension. The extension only activates for files with this extension.

## Architecture

```
VS Code  <--DAP over stdin/stdout-->  intercal-dap.exe  <--named pipe-->  your-program.exe
                                      (C# console app)                    (compiled INTERCAL)
                                                                          with DebugHost hooks
```

The extension (`vscode-intercal/extension.js`) launches the DAP adapter (`intercal.dap/`). The adapter compiles your `.i` file using the cringe compiler with the `-debug-dap:<pipename>` flag, which injects `DebugHost.OnStatement()` calls before every statement. The compiled program connects back to the adapter over a named pipe. The adapter translates between DAP protocol (VS Code) and the simple JSON-lines protocol (DebugHost).
