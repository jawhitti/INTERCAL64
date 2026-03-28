const vscode = require('vscode');
const path = require('path');
const fs = require('fs');
const os = require('os');

// Installed layout: all binaries in one directory
// Dev layout: intercal64.dap/bin/Debug/net9.0/intercal64-dap[.exe]
//             churn/churn.csproj

function findAdapter() {
    const exeName = process.platform === 'win32' ? 'intercal64-dap.exe' : 'intercal64-dap';

    // 1. Check VS Code setting
    const config = vscode.workspace.getConfiguration('intercal');
    const configured = config.get('projectRoot');
    if (configured) {
        const installed = path.join(configured, exeName);
        if (fs.existsSync(installed)) return installed;
        const dev = path.join(configured, 'intercal64.dap', 'bin', 'Debug', 'net9.0', exeName);
        if (fs.existsSync(dev)) return dev;
    }

    // 2. Check standard install locations
    const installPaths = process.platform === 'darwin' ? [
        '/usr/local/lib/intercal64',
        '/opt/homebrew/lib/intercal64',
        '/usr/local/bin',
        '/opt/homebrew/bin',
        path.join(os.homedir(), '.intercal64'),
    ] : process.platform === 'win32' ? [
        path.join(process.env.ProgramFiles || 'C:\\Program Files', 'intercal64', 'bin'),
        path.join(process.env.ProgramFiles || 'C:\\Program Files', 'intercal64'),
        path.join(process.env.LOCALAPPDATA || '', 'intercal64', 'bin'),
        path.join(process.env.LOCALAPPDATA || '', 'intercal64'),
    ] : [
        '/usr/local/lib/intercal64',
        '/usr/local/bin',
        path.join(os.homedir(), '.intercal64'),
    ];

    for (const dir of installPaths) {
        const candidate = path.join(dir, exeName);
        if (fs.existsSync(candidate)) return candidate;
    }

    // 3. Walk up from extension path (dev mode — extension in project tree)
    let dir = vscode.extensions.getExtension('jawhitti.intercal64')?.extensionPath || '';
    for (let i = 0; i < 5; i++) {
        const candidate = path.join(dir, 'intercal64.dap', 'bin', 'Debug', 'net9.0', exeName);
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
                const dev = path.join(base, 'intercal64.dap', 'bin', 'Debug', 'net9.0', exeName);
                if (fs.existsSync(dev)) return dev;
            }
        }
    }

    return null;
}

function activate(context) {
    // Provide a default launch config so F5 works without launch.json
    context.subscriptions.push(
        vscode.debug.registerDebugConfigurationProvider('intercal', {
            provideDebugConfigurations() {
                return [{
                    type: 'intercal',
                    request: 'launch',
                    name: 'Debug INTERCAL-64 program',
                    program: '${file}'
                }];
            },
            resolveDebugConfiguration(_folder, config) {
                if (!config.type && !config.request && !config.name) {
                    const editor = vscode.window.activeTextEditor;
                    if (editor && (editor.document.languageId === 'intercal' ||
                        editor.document.fileName.endsWith('.i') ||
                        editor.document.fileName.endsWith('.ic64'))) {
                        config.type = 'intercal';
                        config.request = 'launch';
                        config.name = 'Debug INTERCAL-64 program';
                        config.program = '${file}';
                    }
                }
                if (!config.program) return undefined;
                return config;
            }
        })
    );

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
                const adapterProject = path.join(root, 'intercal64.dap', 'intercal64.dap.csproj');
                return new vscode.DebugAdapterExecutable('dotnet', [
                    'run', '--project', adapterProject, '--no-build'
                ]);
            }
        })
    );
}

function deactivate() {}

module.exports = { activate, deactivate };
