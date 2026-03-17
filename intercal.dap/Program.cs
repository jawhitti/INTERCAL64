using INTERCAL.Dap;

// DAP adapters communicate over stdin/stdout with JSON messages.
// VS Code launches this process and speaks the protocol.
var adapter = new DebugAdapter(Console.OpenStandardInput(), Console.OpenStandardOutput());
await adapter.RunAsync();
