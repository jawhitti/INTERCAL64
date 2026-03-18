using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace INTERCAL.Dap;

/// <summary>
/// The INTERCAL debug adapter. Speaks DAP over stdin/stdout to VS Code.
/// Compiles .i files, launches them with debug hooks, and communicates
/// with the runtime DebugHost over a named pipe. The user never sees C#.
/// </summary>
public class DebugAdapter
{
    private readonly DapTransport _transport;
    private int _seq = 1;
    private readonly object _sendLock = new();

    // Debuggee state
    private Process? _process;
    private NamedPipeServerStream? _pipe;
    private StreamReader? _pipeReader;
    private StreamWriter? _pipeWriter;
    private string _pipeName = "";

    // Cached state from last stopped event
    private string _currentFile = "";
    private int _currentLine = 0;
    private string _currentStatement = "";
    private Dictionary<string, string> _currentVariables = new();
    private int[] _currentNextStack = Array.Empty<int>();
    // Gerund state: gerund name -> list of {slot, line, abstained}
    private Dictionary<string, List<GerundEntry>> _currentGerundState = new();
    private record GerundEntry(int Slot, int Line, bool Abstained);

    // Pending breakpoints (set before launch)
    private readonly Dictionary<string, List<int>> _pendingBreakpoints = new();

    // Stashed initial stopped event (read during launch, consumed by configurationDone)
    private JsonDocument? _initialStoppedEvent;

    // Launch config
    private string _programPath = "";
    private string _compilerPath = "";
    private string _syslibPath = "";

    public DebugAdapter(Stream input, Stream output)
    {
        _transport = new DapTransport(input, output);
    }

    public async Task RunAsync()
    {
        while (true)
        {
            var request = await _transport.ReadRequestAsync();
            if (request == null) break;

            HandleRequest(request);
        }
    }

    private void HandleRequest(DapRequest request)
    {
        switch (request.Command)
        {
            case "initialize":
                HandleInitialize(request);
                break;
            case "launch":
                HandleLaunch(request);
                break;
            case "setBreakpoints":
                HandleSetBreakpoints(request);
                break;
            case "threads":
                HandleThreads(request);
                break;
            case "stackTrace":
                HandleStackTrace(request);
                break;
            case "scopes":
                HandleScopes(request);
                break;
            case "variables":
                HandleVariables(request);
                break;
            case "continue":
                HandleContinue(request);
                break;
            case "next":
                HandleNext(request);
                break;
            case "stepIn":
                HandleStepIn(request);
                break;
            case "stepOut":
                HandleStepOut(request);
                break;
            case "pause":
                HandlePause(request);
                break;
            case "setVariable":
                HandleSetVariable(request);
                break;
            case "evaluate":
                HandleEvaluate(request);
                break;
            case "disconnect":
                HandleDisconnect(request);
                break;
            case "configurationDone":
                HandleConfigurationDone(request);
                break;
            default:
                SendResponse(request);
                break;
        }
    }

    private void HandleInitialize(DapRequest request)
    {
        SendResponse(request, body: new
        {
            supportsConfigurationDoneRequest = true,
            supportsFunctionBreakpoints = false,
            supportsConditionalBreakpoints = false,
            supportsEvaluateForHovers = false,
            supportsStepBack = false,
            supportsSetVariable = true,
            supportTerminateDebuggee = true,
        });

        SendEvent("initialized");
    }

    private void HandleLaunch(DapRequest request)
    {
        if (!request.Arguments.HasValue)
        {
            SendResponse(request, success: false, message: "E000 NO LAUNCH ARGUMENTS");
            return;
        }

        var args = request.Arguments.Value;

        // Required: path to the .i source file
        if (!args.TryGetProperty("program", out var progEl))
        {
            SendResponse(request, success: false, message: "E000 NO PROGRAM SPECIFIED");
            return;
        }
        _programPath = progEl.GetString()!;

        // Optional: path to compiler (default: find cringe in same dir as adapter)
        if (args.TryGetProperty("compiler", out var compEl))
            _compilerPath = compEl.GetString()!;
        else
            _compilerPath = FindCompiler();

        // Optional: syslib reference
        if (args.TryGetProperty("syslib", out var sysEl))
            _syslibPath = sysEl.GetString()!;
        else
            _syslibPath = FindSyslib();

        // Stable pipe name derived from source path — allows reusing cached builds
        var pathHash = Math.Abs(_programPath.GetHashCode()).ToString("x8");
        _pipeName = "intercal-dap-" + pathHash;
        _pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut,
            1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        // Compile the .i file with debug-dap flag
        try
        {
            CompileProgram();
        }
        catch (Exception ex)
        {
            SendResponse(request, success: false,
                message: $"E000 COMPILATION FAILED: {ex.Message}");
            return;
        }

        SendResponse(request);

        // Launch the compiled program
        LaunchDebuggee();
    }

    private void CompileProgram()
    {
        var programDir = Path.GetDirectoryName(Path.GetFullPath(_programPath)) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(_programPath);
        var exePath = Path.Combine(programDir, baseName + ".exe");
        var sourcePath = Path.GetFullPath(_programPath);

        // Skip compilation if the exe exists, is newer than the source,
        // and was built with debug hooks (contains our pipe name)
        var tmpCs = Path.Combine(programDir, "~tmp.cs");
        bool hasDebugHooks = File.Exists(tmpCs) &&
            File.ReadAllText(tmpCs).Contains($"DebugHost.Initialize(\"{_pipeName}\")");
        if (File.Exists(exePath) && hasDebugHooks &&
            File.GetLastWriteTimeUtc(exePath) > File.GetLastWriteTimeUtc(sourcePath))
        {
            SendEvent("output", new
            {
                category = "console",
                output = $"{Path.GetFileName(_programPath)} is up to date.\n"
            });
            return;
        }

        var compilerArgs = $"{_programPath} -debug-dap:{_pipeName} -b";

        if (!string.IsNullOrEmpty(_syslibPath))
            compilerArgs += $" -r:{_syslibPath}";

        SendEvent("output", new
        {
            category = "console",
            output = $"Compiling {Path.GetFileName(_programPath)}...\n"
        });

        // Clean stale build artifacts so dotnet build doesn't use cached output
        foreach (var ext in new[] { ".exe", ".dll", ".pdb", ".deps.json", ".runtimeconfig.json" })
        {
            var stale = Path.Combine(programDir, baseName + ext);
            try { File.Delete(stale); } catch { }
        }
        // Also clean the generated csproj/cs to force full recompile
        try { File.Delete(Path.Combine(programDir, "~tmp.cs")); } catch { }
        try { File.Delete(Path.Combine(programDir, "~tmp.csproj")); } catch { }
        try { Directory.Delete(Path.Combine(programDir, "obj"), true); } catch { }
        try { Directory.Delete(Path.Combine(programDir, "bin"), true); } catch { }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_compilerPath}\" -- {compilerArgs}",
            WorkingDirectory = programDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(30000);

        if (proc.ExitCode != 0)
        {
            throw new Exception(stdout + stderr);
        }

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            SendEvent("output", new { category = "console", output = stdout });
        }

        // The compiler may return exit code 0 even when the inner dotnet build fails.
        // Verify the expected output actually exists.
        var expectedExe = Path.Combine(programDir,
            Path.GetFileNameWithoutExtension(_programPath) + ".exe");
        var expectedDll = Path.Combine(programDir,
            Path.GetFileNameWithoutExtension(_programPath) + ".dll");
        if (!File.Exists(expectedExe) && !File.Exists(expectedDll))
        {
            throw new Exception("Build produced no output. Check for compilation errors above.");
        }
    }

    private void LaunchDebuggee()
    {
        var exeName = Path.GetFileNameWithoutExtension(_programPath) + ".exe";
        var programDir = Path.GetDirectoryName(Path.GetFullPath(_programPath)) ?? ".";
        var exePath = Path.Combine(programDir, exeName);

        // Also try .dll with dotnet
        var dllPath = Path.Combine(programDir, Path.GetFileNameWithoutExtension(_programPath) + ".dll");

        ProcessStartInfo psi;
        if (File.Exists(exePath))
        {
            psi = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = programDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = dllPath,
                WorkingDirectory = programDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }

        _process = Process.Start(psi)!;

        // Forward stdout/stderr as output events
        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                SendEvent("output", new { category = "stdout", output = e.Data + "\n" });
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                SendEvent("output", new { category = "stderr", output = e.Data + "\n" });
        };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _process.Exited += (_, _) =>
        {
            SendEvent("exited", new { exitCode = _process.ExitCode });
            SendEvent("terminated");
        };
        _process.EnableRaisingEvents = true;

        // Wait for the debuggee to connect to our pipe
        SendEvent("output", new
        {
            category = "console",
            output = "Waiting for debuggee to connect...\n"
        });

        _pipe!.WaitForConnection();
        _pipeReader = new StreamReader(_pipe);
        _pipeWriter = new StreamWriter(_pipe) { AutoFlush = true };

        SendEvent("output", new
        {
            category = "console",
            output = "Debuggee connected. Debugging started.\n"
        });

        // The debuggee immediately stops on entry and sends a "stopped" event.
        // We MUST read it now before sending any commands, otherwise
        // ReadFromDebuggee() in breakpoint handling will consume it.
        _initialStoppedEvent = ReadFromDebuggeeSkippingOutput();

        // Now send any pending breakpoints — the debuggee is in WaitForCommand
        foreach (var kvp in _pendingBreakpoints)
        {
            SendToDebuggee(new
            {
                command = "setBreakpoints",
                file = kvp.Key,
                lines = kvp.Value.ToArray()
            });
            ReadFromDebuggeeSkippingOutput(); // ack
        }
    }

    private void HandleConfigurationDone(DapRequest request)
    {
        SendResponse(request);

        // Process the stashed initial stopped event
        if (_initialStoppedEvent != null)
        {
            ProcessStoppedEvent(_initialStoppedEvent.RootElement);
            _initialStoppedEvent = null;
        }
        else
        {
            // Shouldn't happen, but fall back to reading from pipe
            WaitForDebuggeeStop();
        }
    }

    private void HandleSetBreakpoints(DapRequest request)
    {
        var breakpoints = new List<object>();
        string? file = null;
        var lines = new List<int>();

        if (request.Arguments.HasValue)
        {
            var args = request.Arguments.Value;
            if (args.TryGetProperty("source", out var source) &&
                source.TryGetProperty("path", out var pathEl))
            {
                file = pathEl.GetString();
            }

            if (args.TryGetProperty("breakpoints", out var bps))
            {
                foreach (var bp in bps.EnumerateArray())
                {
                    var line = bp.GetProperty("line").GetInt32();
                    lines.Add(line);
                    breakpoints.Add(new { id = line, verified = true, line });
                }
            }
        }

        // Normalize the file path for consistent matching
        if (file != null)
            file = Path.GetFullPath(file);

        // If debuggee is connected, send breakpoints to it
        if (_pipeWriter != null && file != null)
        {
            SendToDebuggee(new
            {
                command = "setBreakpoints",
                file,
                lines = lines.ToArray()
            });
            ReadFromDebuggeeSkippingOutput(); // ack

            SendEvent("output", new
            {
                category = "console",
                output = $"Breakpoints set: {Path.GetFileName(file)} lines [{string.Join(", ", lines)}]\n"
            });
        }
        else if (file != null)
        {
            // Store for later — debuggee not launched yet
            _pendingBreakpoints[file] = lines;
        }

        SendResponse(request, body: new { breakpoints });
    }

    private void HandleThreads(DapRequest request)
    {
        SendResponse(request, body: new
        {
            threads = new[]
            {
                new { id = 1, name = "INTERCAL" }
            }
        });
    }

    private void HandleStackTrace(DapRequest request)
    {
        var frames = new List<object>();

        // Frame 0: current execution position
        frames.Add(new
        {
            id = 0,
            name = _currentStatement,
            source = new { name = Path.GetFileName(_currentFile), path = _currentFile },
            line = _currentLine,
            column = 1,
        });

        // Additional frames from the NEXT stack
        for (int i = 0; i < _currentNextStack.Length; i++)
        {
            var label = _currentNextStack[i];
            frames.Add(new
            {
                id = i + 1,
                name = label == 0 ? "(entry)" : $"NEXT ({label})",
                source = new { name = Path.GetFileName(_currentFile), path = _currentFile },
                line = 0,
                column = 1,
                presentationHint = "subtle",
            });
        }

        SendResponse(request, body: new
        {
            stackFrames = frames.ToArray(),
            totalFrames = frames.Count
        });
    }

    private void HandleScopes(DapRequest request)
    {
        var scopes = new List<object>
        {
            new { name = "INTERCAL Variables", variablesReference = 1, expensive = false }
        };

        // Only show Gerund State scope if there are abstainable statements
        if (_currentGerundState.Count > 0)
        {
            scopes.Add(new { name = "Gerund State", variablesReference = 2, expensive = false });
        }

        SendResponse(request, body: new { scopes });
    }

    // variablesReference scheme:
    //   1 = INTERCAL Variables (flat list of .X, :X, ::X)
    //   2 = Gerund State (top-level gerund names)
    //   100+ = individual gerund entries (gerund name -> stable ref)
    private readonly Dictionary<string, int> _gerundNameToRef = new();
    private readonly Dictionary<int, string> _gerundRefMap = new();
    private int _nextGerundRef = 100;

    private void HandleVariables(DapRequest request)
    {
        int varRef = 1;
        if (request.Arguments.HasValue &&
            request.Arguments.Value.TryGetProperty("variablesReference", out var vr))
            varRef = vr.GetInt32();

        var variables = new List<object>();

        if (varRef == 1)
        {
            // INTERCAL Variables scope
            foreach (var kvp in _currentVariables.OrderBy(v => v.Key))
            {
                variables.Add(new
                {
                    name = kvp.Key,
                    value = kvp.Value.ToString(),
                    type = GetVariableType(kvp.Key),
                    variablesReference = 0,
                });
            }
        }
        else if (varRef == 2)
        {
            // Gerund State scope — flat list, one entry per abstainable statement
            // Grouped visually by gerund name in the variable name
            foreach (var kvp in _currentGerundState.OrderBy(g => g.Key))
            {
                var gerund = kvp.Key;
                var entries = kvp.Value;

                foreach (var entry in entries.OrderBy(e => e.Line))
                {
                    variables.Add(new
                    {
                        name = $"({entry.Line}) {gerund}",
                        value = entry.Abstained ? "ABSTAINED" : "enabled",
                        variablesReference = 0,
                    });
                }
            }
        }

        SendResponse(request, body: new { variables });
    }

    private void HandleSetVariable(DapRequest request)
    {
        if (!request.Arguments.HasValue)
        {
            SendResponse(request, success: false, message: "No arguments");
            return;
        }

        var args = request.Arguments.Value;
        var name = args.GetProperty("name").GetString()!;

        // Is this a box variable? If so, collapse it — you opened the box.
        if (name.StartsWith("[]"))
        {
            SendEvent("output", new
            {
                category = "console",
                output = $">> You opened the box. Collapsing {name}...\n"
            });

            SendToDebuggee(new { command = "collapseBox", name });
            var response = ReadFromDebuggeeSkippingOutput();

            // Update all box variables — collapse propagates through entanglement
            if (response != null)
            {
                var root = response.RootElement;
                if (root.TryGetProperty("boxes", out var boxes))
                {
                    foreach (var prop in boxes.EnumerateObject())
                        _currentVariables[prop.Name] = prop.Value.GetString()!;
                }
            }

            var newValue = _currentVariables.GetValueOrDefault(name, "?");

            SendResponse(request, body: new
            {
                value = newValue,
                type = GetVariableType(name),
                variablesReference = 0,
            });

            // Force VS Code to refresh the variables panel so entangled boxes update
            SendEvent("stopped", new
            {
                reason = "step",
                threadId = 1,
                allThreadsStopped = true
            });
            return;
        }

        // Classical variables cannot be modified — this is INTERCAL
        SendResponse(request, success: false,
            message: "E180 OUR ADJUSTMENT BUREAU CANNOT HELP YOU WITH THAT");
    }

    private void HandleEvaluate(DapRequest request)
    {
        if (!request.Arguments.HasValue)
        {
            SendResponse(request);
            return;
        }

        var args = request.Arguments.Value;
        var expression = args.TryGetProperty("expression", out var expr)
            ? expr.GetString() ?? "" : "";
        var context = args.TryGetProperty("context", out var ctx)
            ? ctx.GetString() ?? "" : "";

        // In the "repl" context, forward input to the debuggee's stdin
        if (context == "repl" && _process != null && !_process.HasExited)
        {
            try
            {
                _process.StandardInput.WriteLine(expression);
                SendResponse(request, body: new { result = expression, variablesReference = 0 });
            }
            catch
            {
                SendResponse(request, success: false,
                    message: "E579 INPUT FELL OFF THE END OF THE TAPE");
            }
            return;
        }

        SendResponse(request, body: new { result = "", variablesReference = 0 });
    }

    private void HandleContinue(DapRequest request)
    {
        SendResponse(request, body: new { allThreadsContinued = true });
        SendToDebuggee(new { command = "continue" });
        WaitForDebuggeeStopAsync();
    }

    private void HandleNext(DapRequest request)
    {
        SendResponse(request);
        SendToDebuggee(new { command = "next" });
        WaitForDebuggeeStopAsync();
    }

    private void HandleStepIn(DapRequest request)
    {
        SendResponse(request);
        SendToDebuggee(new { command = "stepIn" });
        WaitForDebuggeeStopAsync();
    }

    private void HandleStepOut(DapRequest request)
    {
        SendResponse(request);
        SendToDebuggee(new { command = "stepOut" });
        WaitForDebuggeeStopAsync();
    }

    private void HandlePause(DapRequest request)
    {
        // Not easily supported — the debuggee is either stopped or running
        SendResponse(request);
    }

    private void HandleDisconnect(DapRequest request)
    {
        SendToDebuggee(new { command = "disconnect" });
        try { _process?.Kill(); } catch { }
        try { _pipe?.Close(); } catch { }
        SendResponse(request);
    }

    #region Debuggee communication

    private void SendToDebuggee(object message)
    {
        if (_pipeWriter == null) return;
        var json = JsonSerializer.Serialize(message);
        _pipeWriter.WriteLine(json);
    }

    private JsonDocument? ReadFromDebuggee()
    {
        if (_pipeReader == null) return null;
        var line = _pipeReader.ReadLine();
        if (line == null) return null;

        return JsonDocument.Parse(line);
    }

    /// <summary>
    /// Read from the debuggee, forwarding any output events to VS Code,
    /// until we get a non-output message (like an ack or stopped event).
    /// </summary>
    private JsonDocument? ReadFromDebuggeeSkippingOutput()
    {
        while (true)
        {
            var msg = ReadFromDebuggee();
            if (msg == null) return null;

            var root = msg.RootElement;
            if (root.TryGetProperty("event", out var ev) && ev.GetString() == "output")
            {
                // Forward output to VS Code and keep reading
                ProcessDebuggeeMessage(root);
                continue;
            }
            return msg;
        }
    }

    private void ProcessStoppedEvent(JsonElement root)
    {
        if (root.TryGetProperty("file", out var f))
            _currentFile = f.GetString() ?? "";
        if (root.TryGetProperty("line", out var l))
            _currentLine = l.GetInt32();
        if (root.TryGetProperty("statement", out var s))
            _currentStatement = s.GetString() ?? "";
        if (root.TryGetProperty("variables", out var v))
        {
            _currentVariables.Clear();
            foreach (var prop in v.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                    _currentVariables[prop.Name] = prop.Value.GetString()!;
                else
                {
                    var val = prop.Value.GetUInt64();
                    _currentVariables[prop.Name] = val == 0x4445444B49545459
                        ? "DEDKITTY" : val.ToString();
                }
            }
        }
        if (root.TryGetProperty("nextStack", out var ns))
            _currentNextStack = ns.EnumerateArray()
                .Select(x => x.GetInt32()).ToArray();
        if (root.TryGetProperty("gerundState", out var gs))
        {
            _currentGerundState.Clear();
            foreach (var entry in gs.EnumerateArray())
            {
                var gerund = entry.GetProperty("gerund").GetString()!;
                var ge = new GerundEntry(
                    0,
                    entry.GetProperty("line").GetInt32(),
                    entry.GetProperty("abstained").GetBoolean());

                if (!_currentGerundState.ContainsKey(gerund))
                    _currentGerundState[gerund] = new List<GerundEntry>();
                _currentGerundState[gerund].Add(ge);
            }

        }

        var reason = root.TryGetProperty("reason", out var r)
            ? r.GetString() : "step";

        string? description = null;
        if (root.TryGetProperty("comeFrom", out var cf) && cf.ValueKind == JsonValueKind.String)
            description = cf.GetString();

        SendEvent("stopped", new
        {
            reason,
            description,
            threadId = 1,
            allThreadsStopped = true
        });
    }

    private void ProcessDebuggeeMessage(JsonElement root)
    {
        if (!root.TryGetProperty("event", out var eventEl)) return;
        var eventName = eventEl.GetString();

        switch (eventName)
        {
            case "stopped":
                ProcessStoppedEvent(root);
                break;
            case "output":
                var category = root.TryGetProperty("category", out var c)
                    ? c.GetString() : "console";
                var output = root.TryGetProperty("output", out var o)
                    ? o.GetString() : "";
                SendEvent("output", new { category, output });
                break;
            case "terminated":
                SendEvent("terminated");
                break;
            case "exited":
                var exitCode = root.TryGetProperty("exitCode", out var ec)
                    ? ec.GetInt32() : 0;
                SendEvent("exited", new { exitCode });
                SendEvent("terminated");
                break;
        }
    }

    /// <summary>
    /// Run WaitForDebuggeeStop on a background thread so the main DAP
    /// message loop stays responsive. This allows evaluate requests
    /// (debug console input for WRITE IN) to be processed while the
    /// debuggee is running.
    /// </summary>
    private void WaitForDebuggeeStopAsync()
    {
        Task.Run(() => WaitForDebuggeeStop());
    }

    private void WaitForDebuggeeStop()
    {
        while (true)
        {
            var msg = ReadFromDebuggee();
            if (msg == null)
            {
                SendEvent("exited", new { exitCode = _process?.ExitCode ?? 0 });
                SendEvent("terminated");
                return;
            }

            var root = msg.RootElement;
            if (!root.TryGetProperty("event", out var eventEl)) continue;
            var eventName = eventEl.GetString();

            ProcessDebuggeeMessage(root);

            // Return once we've hit a terminal state
            if (eventName == "stopped" || eventName == "terminated" || eventName == "exited")
                return;
        }
    }

    #endregion

    #region Helpers

    private string FindCompiler()
    {
        // Look for cringe project relative to the adapter
        var adapterDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(adapterDir, "..", "..", "..", "..", "cringe", "cringe.csproj"),
            Path.Combine(adapterDir, "..", "cringe", "cringe.csproj"),
            Path.Combine(adapterDir, "cringe", "cringe.csproj"),
        };

        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }

        return "cringe/cringe.csproj";
    }

    private string FindSyslib()
    {
        var programDir = Path.GetDirectoryName(Path.GetFullPath(_programPath)) ?? ".";
        var candidates = new[]
        {
            Path.Combine(programDir, "syslib64.dll"),
            Path.Combine(programDir, "..", "syslib64.dll"),
        };

        foreach (var s in candidates)
        {
            if (File.Exists(s)) return Path.GetFullPath(s);
        }

        return "";
    }

    private static string GetVariableType(string name)
    {
        if (name.StartsWith("::")) return "clover (64-bit)";
        if (name.StartsWith(":")) return "two-spot (32-bit)";
        if (name.StartsWith(".")) return "one-spot (16-bit)";
        if (name.StartsWith("[")) return "box (quantum)";
        if (name.StartsWith(",")) return "tail (array)";
        if (name.StartsWith(";")) return "hybrid (array)";
        return "unknown";
    }

    private void SendResponse(DapRequest request, bool success = true,
        string? message = null, object? body = null)
    {
        lock (_sendLock)
        {
            _transport.Send(new DapResponse
            {
                Seq = _seq++,
                RequestSeq = request.Seq,
                Success = success,
                Command = request.Command,
                Message = message,
                Body = body
            });
        }
    }

    private void SendEvent(string eventName, object? body = null)
    {
        lock (_sendLock)
        {
            _transport.Send(new DapEvent
            {
                Seq = _seq++,
                Event = eventName,
                Body = body
            });
        }
    }

    #endregion
}
