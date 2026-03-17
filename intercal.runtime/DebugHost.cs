using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace INTERCAL.Runtime
{
    /// <summary>
    /// Runtime debug host. When active, the generated code calls OnStatement()
    /// before each INTERCAL statement. The host pauses execution and communicates
    /// with the DAP adapter over a named pipe, waiting for step/continue commands.
    ///
    /// The protocol is simple JSON lines over the pipe:
    ///   Adapter -> Host: {"command":"continue"} | {"command":"step"} | {"command":"setBreakpoints","lines":[5,10]} | ...
    ///   Host -> Adapter: {"event":"stopped","file":"foo.i","line":5,"reason":"step"} | {"event":"variables",...} | ...
    /// </summary>
    public class DebugHost
    {
        private static DebugHost? _instance;
        public static DebugHost? Instance => _instance;

        private readonly NamedPipeClientStream _pipe;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly object _writeLock = new();

        // Breakpoint state: file path -> set of line numbers
        private readonly Dictionary<string, HashSet<int>> _breakpoints = new();

        // Stepping mode
        private enum StepMode { Continue, StepOver, StepIn, StepOut }
        private StepMode _stepMode = StepMode.StepIn; // Start stopped at first statement
        private bool _stopOnEntry = true;

        // NEXT stack depth when step-over was initiated (for detecting FORGET)
        private int _stepOverDepth = 0;

        // Source info for COME FROM warnings: line -> "COME FROM target, jumps to line N"
        private readonly Dictionary<string, Dictionary<int, int>> _comeFromTargets = new();

        /// <summary>
        /// Initialize the debug host. Called early in the program if /debug-dap is set.
        /// Connects to the named pipe created by the DAP adapter.
        /// </summary>
        public static void Initialize(string pipeName)
        {
            _instance = new DebugHost(pipeName);
        }

        private DebugHost(string pipeName)
        {
            _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            _pipe.Connect(5000); // 5 second timeout
            _reader = new StreamReader(_pipe, Encoding.UTF8);
            _writer = new StreamWriter(_pipe, Encoding.UTF8) { AutoFlush = true };
        }

        /// <summary>
        /// Called by generated code before each INTERCAL statement.
        /// This is the main debug hook — it decides whether to pause.
        /// </summary>
        public void OnStatement(string file, int line, string statementText,
            ExecutionContext context, Stack<int> nextStack)
        {
            // Check if we should stop
            string? stopReason = null;

            if (_stopOnEntry)
            {
                stopReason = "entry";
                _stopOnEntry = false;
            }
            else if (_stepMode == StepMode.StepIn || _stepMode == StepMode.StepOver)
            {
                stopReason = "step";
            }
            else if (IsBreakpoint(file, line))
            {
                stopReason = "breakpoint";
            }

            if (stopReason == null) return;

            // Check for COME FROM warning
            CheckComeFromWarning(file, line);

            // Send stopped event with current state
            SendStopped(file, line, statementText, stopReason, context, nextStack);

            // Wait for command from adapter
            WaitForCommand(context, nextStack, file);
        }

        /// <summary>
        /// Called by generated code when FORGET executes.
        /// Notifies the adapter so it can update the NEXT stack display.
        /// </summary>
        public void OnForget(int count, Stack<int> nextStack, string file, int line)
        {
            Send(new
            {
                @event = "output",
                category = "console",
                output = $"FORGET #{count} at {Path.GetFileName(file)}:{line} — {count} entry(s) popped from NEXT stack\n"
            });
        }

        /// <summary>
        /// Called by generated code when RESUME executes.
        /// </summary>
        public void OnResume(int depth, int targetLabel, string file, int line)
        {
            Send(new
            {
                @event = "output",
                category = "console",
                output = $"RESUME #{depth} at {Path.GetFileName(file)}:{line} — returning to ({targetLabel})\n"
            });
        }

        /// <summary>
        /// Register a COME FROM target at compile time or program init.
        /// When execution reaches 'targetLine', we warn that control will transfer to 'comeFromLine'.
        /// </summary>
        public void RegisterComeFrom(string file, int targetLine, int comeFromLine)
        {
            if (!_comeFromTargets.ContainsKey(file))
                _comeFromTargets[file] = new Dictionary<int, int>();
            _comeFromTargets[file][targetLine] = comeFromLine;
        }

        /// <summary>
        /// Called when the program terminates normally (GIVE UP).
        /// </summary>
        public void OnTerminated(int exitCode = 0)
        {
            Send(new { @event = "terminated" });
            Send(new { @event = "exited", exitCode });
            _pipe.Close();
        }

        #region Private helpers

        private bool IsBreakpoint(string file, int line)
        {
            // Normalize path for comparison
            var key = Path.GetFullPath(file).ToLowerInvariant();
            return _breakpoints.TryGetValue(key, out var lines) && lines.Contains(line);
        }

        private void CheckComeFromWarning(string file, int line)
        {
            var key = Path.GetFullPath(file).ToLowerInvariant();
            if (_comeFromTargets.TryGetValue(key, out var targets) &&
                targets.TryGetValue(line, out var comeFromLine))
            {
                Send(new
                {
                    @event = "output",
                    category = "important",
                    output = $"WARNING: Line {line} is a COME FROM target — after execution, control transfers to line {comeFromLine}\n"
                });
            }
        }

        private void SendStopped(string file, int line, string statementText,
            string reason, ExecutionContext context, Stack<int> nextStack)
        {
            Send(new
            {
                @event = "stopped",
                file,
                line,
                statement = statementText,
                reason,
                variables = CollectVariables(context),
                nextStack = nextStack.ToArray()
            });
        }

        private Dictionary<string, object> CollectVariables(ExecutionContext context)
        {
            var vars = new Dictionary<string, object>();
            // GetAllVariables returns a snapshot of all INTERCAL variables
            foreach (var kvp in context.GetAllVariables())
            {
                vars[kvp.Key] = kvp.Value;
            }
            return vars;
        }

        private void WaitForCommand(ExecutionContext context, Stack<int> nextStack, string file)
        {
            while (true)
            {
                var line = _reader.ReadLine();
                if (line == null)
                {
                    // Pipe closed — adapter disconnected, terminate
                    Environment.Exit(0);
                    return;
                }

                var cmd = JsonDocument.Parse(line);
                var command = cmd.RootElement.GetProperty("command").GetString();

                switch (command)
                {
                    case "continue":
                        _stepMode = StepMode.Continue;
                        return;

                    case "step":
                    case "stepIn":
                        _stepMode = StepMode.StepIn;
                        return;

                    case "stepOver":
                    case "next":
                        _stepMode = StepMode.StepOver;
                        _stepOverDepth = nextStack.Count;
                        return;

                    case "stepOut":
                        _stepMode = StepMode.StepOut;
                        return;

                    case "setBreakpoints":
                        HandleSetBreakpoints(cmd.RootElement);
                        // Don't return — keep waiting for a run command
                        break;

                    case "getVariables":
                        Send(new
                        {
                            @event = "variables",
                            variables = CollectVariables(context)
                        });
                        break;

                    case "getStackTrace":
                        Send(new
                        {
                            @event = "stackTrace",
                            nextStack = nextStack.ToArray()
                        });
                        break;

                    case "disconnect":
                        Environment.Exit(0);
                        return;
                }
            }
        }

        private void HandleSetBreakpoints(JsonElement root)
        {
            if (root.TryGetProperty("file", out var fileEl))
            {
                var file = Path.GetFullPath(fileEl.GetString()!).ToLowerInvariant();
                var lines = new HashSet<int>();

                if (root.TryGetProperty("lines", out var linesEl))
                {
                    foreach (var l in linesEl.EnumerateArray())
                        lines.Add(l.GetInt32());
                }

                _breakpoints[file] = lines;
            }

            Send(new { @event = "breakpointsSet", success = true });
        }

        private void Send(object message)
        {
            var json = JsonSerializer.Serialize(message);
            lock (_writeLock)
            {
                _writer.WriteLine(json);
            }
        }

        #endregion
    }
}
