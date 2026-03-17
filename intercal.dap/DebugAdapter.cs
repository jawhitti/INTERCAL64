using System.Text.Json;

namespace INTERCAL.Dap;

/// <summary>
/// The INTERCAL debug adapter. Speaks DAP over stdin/stdout.
/// Launches compiled INTERCAL programs and controls them via
/// the .NET managed debugger (vsdbg), translating everything
/// into INTERCAL terms so the user never sees C#.
/// </summary>
public class DebugAdapter
{
    private readonly DapTransport _transport;
    private int _seq = 1;

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

            await HandleRequestAsync(request);
        }
    }

    private async Task HandleRequestAsync(DapRequest request)
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
            case "disconnect":
                HandleDisconnect(request);
                break;
            case "configurationDone":
                SendResponse(request);
                break;
            default:
                SendResponse(request, success: false,
                    message: $"E000 UNRECOGNIZED REQUEST '{request.Command}'");
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
            supportsEvaluateForHovers = true,
            supportsStepBack = false,
            supportsSetVariable = true,
            // We'll report COME FROM targets and FORGET events here
            supportsOutputEvents = true,
        });

        // After responding to initialize, send the "initialized" event
        SendEvent("initialized");
    }

    private void HandleLaunch(DapRequest request)
    {
        // TODO: Extract launch config (program path, syslib path, etc.)
        // TODO: Compile the .i file if needed
        // TODO: Launch the compiled exe under the managed debugger
        SendResponse(request);

        // For now, immediately signal that we stopped at entry
        SendEvent("stopped", new { reason = "entry", threadId = 1 });
    }

    private void HandleSetBreakpoints(DapRequest request)
    {
        // TODO: Map INTERCAL source lines to breakpoint locations
        // For now, acknowledge all requested breakpoints as verified
        var breakpoints = new List<object>();

        if (request.Arguments.HasValue)
        {
            var args = request.Arguments.Value;
            if (args.TryGetProperty("breakpoints", out var bps))
            {
                foreach (var bp in bps.EnumerateArray())
                {
                    var line = bp.GetProperty("line").GetInt32();
                    breakpoints.Add(new { id = line, verified = true, line });
                }
            }
        }

        SendResponse(request, body: new { breakpoints });
    }

    private void HandleThreads(DapRequest request)
    {
        // INTERCAL has a single thread of execution
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
        // TODO: Build stack frames from:
        // 1. Current execution position (source line)
        // 2. The NEXT stack entries (with source lines where NEXT was called)
        SendResponse(request, body: new
        {
            stackFrames = Array.Empty<object>(),
            totalFrames = 0
        });
    }

    private void HandleScopes(DapRequest request)
    {
        // INTERCAL has one flat global scope — all variables are global
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
        // TODO: Read all .X, :X, ::X, []X from the execution context
        // Translate C# local names (dot_1, colon_1, etc.) back to INTERCAL notation
        SendResponse(request, body: new
        {
            variables = Array.Empty<object>()
        });
    }

    private void HandleContinue(DapRequest request)
    {
        // TODO: Resume execution until next breakpoint
        SendResponse(request, body: new { allThreadsContinued = true });
    }

    private void HandleNext(DapRequest request)
    {
        // Step Over — but in INTERCAL, this is complicated:
        // If we're on a NEXT statement, the called code might FORGET our
        // return address, meaning "step over" never returns.
        // TODO: Set breakpoint at return label, watch for FORGET
        SendResponse(request);
    }

    private void HandleStepIn(DapRequest request)
    {
        // Step Into — follow NEXT into the target label
        // TODO: Single-step the underlying .NET debugger
        SendResponse(request);
    }

    private void HandleStepOut(DapRequest request)
    {
        // Step Out — continue until RESUME returns us
        // TODO: Watch for RESUME/FORGET on the NEXT stack
        SendResponse(request);
    }

    private void HandleDisconnect(DapRequest request)
    {
        // TODO: Kill the debuggee process
        SendResponse(request);
    }

    #region Helpers

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
