# Homebrew formula for INTERCAL-64
# Install: brew tap jawhitti/intercal64 && brew install intercal64
# Usage: churn hello.i && ./hello

class Intercal64 < Formula
  desc "INTERCAL-64 — a 64-bit INTERCAL compiler and runtime"
  homepage "https://github.com/jawhitti/INTERCAL64"
  version "2.0.0"

  if Hardware::CPU.arm?
    url "https://github.com/jawhitti/INTERCAL64/releases/download/v2.0.0/intercal64-2.0.0-osx-arm64.tar.gz"
    sha256 "PLACEHOLDER_ARM64_SHA256"
  else
    url "https://github.com/jawhitti/INTERCAL64/releases/download/v2.0.0/intercal64-2.0.0-osx-x64.tar.gz"
    sha256 "PLACEHOLDER_X64_SHA256"
  end

  def install
    bin.install "bin/churn"
    bin.install "bin/intercal64-dap"
    lib.install "lib/intercal64.runtime.dll"
    lib.install "lib/syslib64.dll"
    (share/"intercal64/samples").install Dir["samples/*.i"]
    # Copy runtime and syslib to samples so they compile out of the box
    cp lib/"intercal64.runtime.dll", share/"intercal64/samples/"
    cp lib/"syslib64.dll", share/"intercal64/samples/"
    doc.install "debugger-install.md"
  end

  def post_install
    # Install VS Code extension if VS Code is present
    vsix = Dir[prefix/"*.vsix"].first
    if vsix && which("code")
      system "code", "--install-extension", vsix
    end
  end

  def caveats
    <<~EOS
      To use the VS Code debugger, install the extension:
        code --install-extension #{prefix}/*.vsix
      Or search for "intercal64" in the VS Code extension marketplace.

      Sample programs are in:
        #{share}/intercal64/samples/

      Try:
        cd #{share}/intercal64/samples
        churn hello.i
        ./hello
    EOS
  end

  test do
    (testpath/"test.i").write("DO READ OUT #42\nPLEASE GIVE UP\n")
    system bin/"churn", "test.i", "-b"
    assert_predicate testpath/"test.exe", :exist?
  end
end
