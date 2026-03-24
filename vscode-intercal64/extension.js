const vscode = require('vscode');
const path = require('path');
const fs = require('fs');
const os = require('os');

// Installed layout: all binaries in one directory
// Dev layout: schrodie.dap/bin/Debug/net9.0/schrodie-dap[.exe]
//             cringe/cringe.csproj

function findAdapter() {
    const exeName = process.platform === 'win32' ? 'schrodie-dap.exe' : 'schrodie-dap';

    // 1. Check VS Code setting
    const config = vscode.workspace.getConfiguration('intercal');
    const configured = config.get('projectRoot');
    if (configured) {
        const installed = path.join(configured, exeName);
        if (fs.existsSync(installed)) return installed;
        const dev = path.join(configured, 'schrodie.dap', 'bin', 'Debug', 'net9.0', exeName);
        if (fs.existsSync(dev)) return dev;
    }

    // 2. Check standard install locations
    const installPaths = process.platform === 'darwin' ? [
        '/usr/local/lib/schrodie',
        '/opt/homebrew/lib/schrodie',
        '/usr/local/bin',
        '/opt/homebrew/bin',
        path.join(os.homedir(), '.schrodie'),
    ] : process.platform === 'win32' ? [
        path.join(process.env.ProgramFiles || 'C:\\Program Files', 'schrodie'),
        path.join(process.env.LOCALAPPDATA || '', 'schrodie'),
    ] : [
        '/usr/local/lib/schrodie',
        '/usr/local/bin',
        path.join(os.homedir(), '.schrodie'),
    ];

    for (const dir of installPaths) {
        const candidate = path.join(dir, exeName);
        if (fs.existsSync(candidate)) return candidate;
    }

    // 3. Walk up from extension path (dev mode — extension in project tree)
    let dir = vscode.extensions.getExtension('jawhitti.intercal')?.extensionPath || '';
    for (let i = 0; i < 5; i++) {
        const candidate = path.join(dir, 'schrodie.dap', 'bin', 'Debug', 'net9.0', exeName);
        if (fs.existsSync(candidate)) return candidate;
        dir = path.dirname(dir);
    }

    // 4. Check workspace folders and parents
    if (vscode.workspace.workspaceFolders) {
        for (const folder of vscode.workspace.workspaceFolders) {
            for (const base of [folder.uri.fsPath, path.dirname(folder.uri.fsPath)]) {
                // Installed layout
                const installed = path.join(base, exeName);
                if (fs.existsSync(installed)) return installed;
                // Dev layout
                const dev = path.join(base, 'schrodie.dap', 'bin', 'Debug', 'net9.0', exeName);
                if (fs.existsSync(dev)) return dev;
            }
        }
    }

    return null;
}

function activate(context) {
    context.subscriptions.push(
        vscode.debug.registerDebugAdapterDescriptorFactory('intercal', {
            createDebugAdapterDescriptor(_session) {
                const adapter = findAdapter();

                if (adapter) {
                    return new vscode.DebugAdapterExecutable(adapter);
                }

                // Last resort: try dotnet run (requires source checkout + SDK)
                const config = vscode.workspace.getConfiguration('intercal');
                const root = config.get('projectRoot') || '';
                const adapterProject = path.join(root, 'schrodie.dap', 'schrodie.dap.csproj');
                return new vscode.DebugAdapterExecutable('dotnet', [
                    'run', '--project', adapterProject, '--no-build'
                ]);
            }
        })
    );
}

function deactivate() {}

module.exports = { activate, deactivate };
