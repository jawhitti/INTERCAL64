#!/bin/bash
set -e

# Build the schrodie distribution package
# Usage: ./build/package.sh [osx-arm64|osx-x64|win-x64|linux-x64]

RID="${1:-osx-arm64}"
VERSION="${2:-1.5.0}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(dirname "$SCRIPT_DIR")"
OUT="$ROOT/dist/$RID"

echo "=== Building schrodie $VERSION for $RID ==="

rm -rf "$OUT"
mkdir -p "$OUT/bin" "$OUT/lib" "$OUT/samples"

# 1. Publish compiler (self-contained single-file)
echo "--- Publishing compiler ---"
dotnet publish "$ROOT/cringe/cringe.csproj" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT/bin"

# The assembly is already named 'schrodie' via the csproj

# 2. Publish DAP adapter (self-contained single-file)
echo "--- Publishing DAP adapter ---"
dotnet publish "$ROOT/schrodie.dap/schrodie.dap.csproj" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT/bin"

# 3. Build syslib64
echo "--- Building syslib64 ---"
cd "$ROOT/samples"
dotnet run --project "$ROOT/cringe/cringe.csproj" -- \
    syslib64.schrodie -b -t:library -noplease
cp syslib64.dll "$OUT/lib/"
cp schrodie.runtime.dll "$OUT/lib/"
cd "$ROOT"

# 4. Copy samples
echo "--- Copying samples ---"
SAMPLES=(
    hello.i
    fizzbuzz.i
    collatz.i
    fourspot.schrodie
    mingle64.schrodie
    test_quantum.schrodie
    eve.schrodie
    hello_schrodie.schrodie
    alice_bob.schrodie
    quantum_next.schrodie
    roulette4.schrodie
    roulette5.schrodie
    shores_algorithm.schrodie
)
for f in "${SAMPLES[@]}"; do
    cp "$ROOT/samples/$f" "$OUT/samples/"
done
# Also copy runtime and syslib to samples so they can compile out of the box
cp "$OUT/lib/schrodie.runtime.dll" "$OUT/samples/"
cp "$OUT/lib/syslib64.dll" "$OUT/samples/"

# 5. Package VS Code extension
echo "--- Packaging VS Code extension ---"
cd "$ROOT/vscode-schrodie"
if command -v npx &>/dev/null; then
    npx @vscode/vsce package -o "$OUT/schrodie-$VERSION.vsix"
else
    echo "WARNING: npx not found, skipping .vsix build"
    echo "Install Node.js and run: npm install -g @vscode/vsce"
fi
cd "$ROOT"

# 6. Copy docs
cp "$ROOT/doc/schrodie.md" "$OUT/"
cp "$ROOT/doc/debugger-install.md" "$OUT/"

# 7. Create install script
if [ "$RID" = "win-x64" ]; then
    cat > "$OUT/install.ps1" << 'PWSH'
# schrodie Installer for Windows
$installDir = "$env:LOCALAPPDATA\schrodie"
Write-Host "Installing schrodie to $installDir..."
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -Recurse -Force "bin\*" $installDir
Copy-Item -Recurse -Force "lib\*" $installDir
Copy-Item -Recurse -Force "samples" "$installDir\samples"

# Add to PATH
$path = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($path -notlike "*$installDir*") {
    [Environment]::SetEnvironmentVariable("PATH", "$path;$installDir", "User")
    Write-Host "Added $installDir to PATH"
}

# Install VS Code extension if available
$vsix = Get-ChildItem "*.vsix" | Select-Object -First 1
if ($vsix -and (Get-Command code -ErrorAction SilentlyContinue)) {
    Write-Host "Installing VS Code extension..."
    code --install-extension $vsix.FullName
}

Write-Host "Done. Restart your terminal, then try: schrodie samples\hello.i"
PWSH
else
    cat > "$OUT/install.sh" << 'BASH'
#!/bin/bash
set -e

INSTALL_DIR="${SCHRODIE_HOME:-$HOME/.schrodie}"
echo "Installing schrodie to $INSTALL_DIR..."
mkdir -p "$INSTALL_DIR"
cp bin/* "$INSTALL_DIR/"
cp lib/* "$INSTALL_DIR/"
cp -r samples "$INSTALL_DIR/samples"
chmod +x "$INSTALL_DIR/schrodie" "$INSTALL_DIR/schrodie-dap"

# Add to PATH via shell profile
SHELL_RC="$HOME/.zshrc"
[ -f "$HOME/.bashrc" ] && SHELL_RC="$HOME/.bashrc"
if ! grep -q 'INTERCAL' "$SHELL_RC" 2>/dev/null; then
    echo '' >> "$SHELL_RC"
    echo '# schrodie' >> "$SHELL_RC"
    echo "export PATH=\"$INSTALL_DIR:\$PATH\"" >> "$SHELL_RC"
    echo "Added $INSTALL_DIR to PATH in $SHELL_RC"
fi

# Install VS Code extension if available
VSIX=$(ls *.vsix 2>/dev/null | head -1)
if [ -n "$VSIX" ] && command -v code &>/dev/null; then
    echo "Installing VS Code extension..."
    code --install-extension "$VSIX"
fi

echo "Done. Restart your terminal, then try: schrodie samples/hello.i"
BASH
    chmod +x "$OUT/install.sh"
fi

# 8. Create archive
echo "--- Creating archive ---"
cd "$ROOT/dist"
ARCHIVE="schrodie-$VERSION-$RID"
if [ "$RID" = "win-x64" ]; then
    # zip for Windows
    if command -v zip &>/dev/null; then
        (cd "$RID" && zip -r "../$ARCHIVE.zip" .)
    else
        echo "WARNING: zip not found, skipping archive"
    fi
else
    tar czf "$ARCHIVE.tar.gz" -C "$RID" .
fi

echo ""
echo "=== Package built: dist/$ARCHIVE ==="
echo "Contents:"
ls -la "$OUT/bin/"
echo ""
echo "To install: cd dist/$RID && ./install.sh"
