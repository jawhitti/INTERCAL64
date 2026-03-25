#!/bin/bash
set -e

# Build the INTERCAL-64 distribution package
# Usage: ./build/package.sh [osx-arm64|osx-x64|win-x64|linux-x64] [version]

RID="${1:-osx-arm64}"
VERSION="${2:-2.0.0}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(dirname "$SCRIPT_DIR")"
OUT="$ROOT/dist/$RID"

echo "=== Building INTERCAL-64 $VERSION for $RID ==="

rm -rf "$OUT"
mkdir -p "$OUT/bin" "$OUT/lib" "$OUT/samples"

# 1. Publish compiler (self-contained single-file)
echo "--- Publishing compiler ---"
dotnet publish "$ROOT/churn/churn.csproj" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT/bin"

# 2. Publish DAP adapter (self-contained single-file)
echo "--- Publishing DAP adapter ---"
dotnet publish "$ROOT/intercal64.dap/intercal64.dap.csproj" \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT/bin"

# 3. Build syslib64
echo "--- Building syslib64 ---"
cd "$OUT/bin"
./churn$([ "$RID" = "win-x64" ] && echo ".exe" || echo "") \
    "$ROOT/syslib64/syslib64.ic64" -b -t:library -noplease 2>/dev/null || true
if [ -f syslib64.dll ]; then
    cp syslib64.dll "$OUT/lib/"
fi
cp intercal64.runtime.dll "$OUT/lib/" 2>/dev/null || true
cd "$ROOT"

# 4. Copy samples
echo "--- Copying samples ---"
SAMPLES=(
    hello.i
    fizzbuzz.i
    collatz.i
    beer.i
    rot13.i
    primes.i
    stable_marriage.i
)
for f in "${SAMPLES[@]}"; do
    [ -f "$ROOT/samples/$f" ] && cp "$ROOT/samples/$f" "$OUT/samples/"
done
# Copy learn-intercal
cp -r "$ROOT/samples/learn-intercal" "$OUT/samples/learn-intercal"
# Copy runtime and syslib to samples so they can compile out of the box
[ -f "$OUT/lib/intercal64.runtime.dll" ] && cp "$OUT/lib/intercal64.runtime.dll" "$OUT/samples/"
[ -f "$OUT/lib/syslib64.dll" ] && cp "$OUT/lib/syslib64.dll" "$OUT/samples/"

# 5. Package VS Code extension
echo "--- Packaging VS Code extension ---"
cd "$ROOT/intercal64.vscode"
if command -v npx &>/dev/null; then
    npx @vscode/vsce package -o "$OUT/intercal64-$VERSION.vsix" 2>/dev/null || echo "WARNING: vsix build failed"
else
    echo "WARNING: npx not found, skipping .vsix build"
fi
cd "$ROOT"

# 6. Copy docs
[ -f "$ROOT/doc/debugger-install.md" ] && cp "$ROOT/doc/debugger-install.md" "$OUT/"

# 7. Create install script
if [ "$RID" = "win-x64" ]; then
    cat > "$OUT/install.ps1" << 'PWSH'
# INTERCAL-64 Installer for Windows
$installDir = "$env:LOCALAPPDATA\intercal64"
Write-Host "Installing INTERCAL-64 to $installDir..."
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

Write-Host "Done. Restart your terminal, then try: churn samples\hello.i"
PWSH
else
    cat > "$OUT/install.sh" << 'BASH'
#!/bin/bash
set -e

INSTALL_DIR="${INTERCAL64_HOME:-$HOME/.intercal64}"
echo "Installing INTERCAL-64 to $INSTALL_DIR..."
mkdir -p "$INSTALL_DIR"
cp bin/* "$INSTALL_DIR/"
cp lib/* "$INSTALL_DIR/"
cp -r samples "$INSTALL_DIR/samples"
chmod +x "$INSTALL_DIR/churn" "$INSTALL_DIR/intercal64-dap"

# Add to PATH via shell profile
SHELL_RC="$HOME/.zshrc"
[ -f "$HOME/.bashrc" ] && SHELL_RC="$HOME/.bashrc"
if ! grep -q 'intercal64' "$SHELL_RC" 2>/dev/null; then
    echo '' >> "$SHELL_RC"
    echo '# INTERCAL-64' >> "$SHELL_RC"
    echo "export PATH=\"$INSTALL_DIR:\$PATH\"" >> "$SHELL_RC"
    echo "Added $INSTALL_DIR to PATH in $SHELL_RC"
fi

# Install VS Code extension if available
VSIX=$(ls *.vsix 2>/dev/null | head -1)
if [ -n "$VSIX" ] && command -v code &>/dev/null; then
    echo "Installing VS Code extension..."
    code --install-extension "$VSIX"
fi

echo "Done. Restart your terminal, then try: churn samples/hello.i"
BASH
    chmod +x "$OUT/install.sh"
fi

# 8. Create archive
echo "--- Creating archive ---"
cd "$ROOT/dist"
ARCHIVE="intercal64-$VERSION-$RID"
if [ "$RID" = "win-x64" ]; then
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
