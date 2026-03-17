const vscode = require('vscode');
const path = require('path');

function activate(context) {
    context.subscriptions.push(
        vscode.debug.registerDebugAdapterDescriptorFactory('intercal', {
            createDebugAdapterDescriptor(_session) {
                // The adapter is a .NET executable. We run it via 'dotnet run'
                // pointing at the intercal.dap project, or directly if published.
                const adapterProject = path.join(
                    context.extensionPath, '..', 'intercal.dap', 'intercal.dap.csproj'
                );

                // Try to find a pre-built adapter first
                const adapterExe = path.join(
                    context.extensionPath, '..', 'intercal.dap', 'bin', 'Debug', 'net9.0', 'intercal-dap.exe'
                );

                const fs = require('fs');
                if (fs.existsSync(adapterExe)) {
                    return new vscode.DebugAdapterExecutable(adapterExe);
                }

                // Fall back to dotnet run
                return new vscode.DebugAdapterExecutable('dotnet', [
                    'run', '--project', adapterProject, '--no-build'
                ]);
            }
        })
    );
}

function deactivate() {}

module.exports = { activate, deactivate };
