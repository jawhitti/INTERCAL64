const vscode = require('vscode');
const path = require('path');
const fs = require('fs');

function findProjectRoot(extensionPath) {
    // Check VS Code setting first
    const config = vscode.workspace.getConfiguration('intercal');
    const configured = config.get('projectRoot');
    if (configured && fs.existsSync(configured)) {
        return configured;
    }

    // If extension is in the project tree (dev mode), walk up to find it
    let dir = extensionPath;
    for (let i = 0; i < 5; i++) {
        const candidate = path.join(dir, 'intercal.dap', 'intercal.dap.csproj');
        if (fs.existsSync(candidate)) {
            return dir;
        }
        dir = path.dirname(dir);
    }

    // Last resort: check if workspace folders contain the project
    if (vscode.workspace.workspaceFolders) {
        for (const folder of vscode.workspace.workspaceFolders) {
            const candidate = path.join(folder.uri.fsPath, 'intercal.dap', 'intercal.dap.csproj');
            if (fs.existsSync(candidate)) {
                return folder.uri.fsPath;
            }
            // Also check parent (if workspace is opened to samples/)
            const parent = path.dirname(folder.uri.fsPath);
            const parentCandidate = path.join(parent, 'intercal.dap', 'intercal.dap.csproj');
            if (fs.existsSync(parentCandidate)) {
                return parent;
            }
        }
    }

    // Give up — return relative to extension (original behavior)
    return path.join(extensionPath, '..');
}

function activate(context) {
    context.subscriptions.push(
        vscode.debug.registerDebugAdapterDescriptorFactory('intercal', {
            createDebugAdapterDescriptor(_session) {
                const projectRoot = findProjectRoot(context.extensionPath);

                const adapterExe = path.join(
                    projectRoot, 'intercal.dap', 'bin', 'Debug', 'net9.0', 'intercal-dap.exe'
                );

                if (fs.existsSync(adapterExe)) {
                    return new vscode.DebugAdapterExecutable(adapterExe);
                }

                // Fall back to dotnet run
                const adapterProject = path.join(
                    projectRoot, 'intercal.dap', 'intercal.dap.csproj'
                );
                return new vscode.DebugAdapterExecutable('dotnet', [
                    'run', '--project', adapterProject, '--no-build'
                ]);
            }
        })
    );
}

function deactivate() {}

module.exports = { activate, deactivate };
