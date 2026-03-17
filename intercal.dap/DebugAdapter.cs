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
    private Dictionary<string, ulong> _currentVariables = new();
    private int[] _currentNextStack = Array.Empty<int>();

    // Pending breakpoints (set before launch)
    private readonly Dictionary<string, List<int>> _pendingBreakpoints = new();

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
            supportsSetVariable = false,
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

        // Create named pipe for debug communication
        _pipeName = "intercal-dap-" + Guid.NewGuid().ToString("N")[..8];
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
        var compilerArgs = $"{_programPath} /debug-dap:{_pipeName} /b";

        if (!string.IsNullOrEmpty(_syslibPath))
            compilerArgs += $" /r:{_syslibPath}";

        SendEvent("output", new
        {
            category = "console",
            output = $"Compiling {Path.GetFileName(_programPath)}...\n"
        });

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

        // Send any pending breakpoints to the debuggee
        foreach (var kvp in _pendingBreakpoints)
        {
            SendToDebuggee(new
            {
                command = "setBreakpoints",
                file = kvp.Key,
                lines = kvp.Value.ToArray()
            });
            // Read ack
            ReadFromDebuggee();
        }
    }

    private void HandleConfigurationDone(DapRequest request)
    {
        SendResponse(request);

        // The debuggee is already running and stopped on entry.
        // Wait for the first stopped event from it.
        WaitForDebuggeeStop();
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

        // If debuggee is running, send breakpoints to it
        if (_pipeWriter != null && file != null)
        {
            SendToDebuggee(new
            {
                command = "setBreakpoints",
                file,
                lines = lines.ToArray()
            });
            ReadFromDebuggee(); // ack
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
        SendResponse(request, body: new
        {
            scopes = new[]
            {
                new { name = "INTERCAL Variables", variablesReference = 1, expensive = false }
            }
        });
    }

    private void HandleVariables(DapRequest request)
    {
        var variables = new List<object>();

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

        SendResponse(request, body: new { variables });
    }

    private void HandleContinue(DapRequest request)
    {
        SendResponse(request, body: new { allThreadsContinued = true });
        SendToDebuggee(new { command = "continue" });
        WaitForDebuggeeStop();
    }

    private void HandleNext(DapRequest request)
    {
        SendResponse(request);
        SendToDebuggee(new { command = "next" });
        WaitForDebuggeeStop();
    }

    private void HandleStepIn(DapRequest request)
    {
        SendResponse(request);
        SendToDebuggee(new { command = "stepIn" });
        WaitForDebuggeeStop();
    }

    private void HandleStepOut(DapRequest request)
    {
        SendResponse(request);
        SendToDebuggee(new { command = "stepOut" });
        WaitForDebuggeeStop();
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

    private void WaitForDebuggeeStop()
    {
        // Read messages from the debuggee until we get a "stopped" event
        while (true)
        {
            var msg = ReadFromDebuggee();
            if (msg == null)
            {
                // Pipe closed — debuggee exited
                SendEvent("exited", new { exitCode = _process?.ExitCode ?? 0 });
                SendEvent("terminated");
                return;
            }

            var root = msg.RootElement;
            if (!root.TryGetProperty("event", out var eventEl)) continue;
            var eventName = eventEl.GetString();

            switch (eventName)
            {
                case "stopped":
                    // Update cached state
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
                            _currentVariables[prop.Name] = prop.Value.GetUInt64();
                    }
                    if (root.TryGetProperty("nextStack", out var ns))
                        _currentNextStack = ns.EnumerateArray()
                            .Select(x => x.GetInt32()).ToArray();

                    var reason = root.TryGetProperty("reason", out var r)
                        ? r.GetString() : "step";

                    // Send DAP stopped event to VS Code
                    SendEvent("stopped", new
                    {
                        reason,
                        threadId = 1,
                        allThreadsStopped = true
                    });
                    return;

                case "output":
                    var category = root.TryGetProperty("category", out var c)
                        ? c.GetString() : "console";
                    var output = root.TryGetProperty("output", out var o)
                        ? o.GetString() : "";
                    SendEvent("output", new { category, output });
                    break;

                case "terminated":
                    SendEvent("terminated");
                    return;

                case "exited":
                    var exitCode = root.TryGetProperty("exitCode", out var ec)
                        ? ec.GetInt32() : 0;
                    SendEvent("exited", new { exitCode });
                    SendEvent("terminated");
                    return;
            }
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

    private void SendEvent(string eventName, object? body = null)
    {
        _transport.Send(new DapEvent
        {
            Seq = _seq++,
            Event = eventName,
            Body = body
        });
    }

    #endregion
}
