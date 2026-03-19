# Homebrew formula for schrodie
# Install: brew tap jawhitti/schrodie && brew install schrodie
# Usage: schrodie hello.i && ./hello

class Schrodie < Formula
  desc "schrodie — a quantum programming language. Now with quantum cats."
  homepage "https://github.com/jawhitti/INTERCAL"
  version "1.5.0"

  if Hardware::CPU.arm?
    url "https://github.com/jawhitti/INTERCAL/releases/download/v1.5.0/schrodie-1.5.0-osx-arm64.tar.gz"
    sha256 "PLACEHOLDER_ARM64_SHA256"
  else
    url "https://github.com/jawhitti/INTERCAL/releases/download/v1.5.0/schrodie-1.5.0-osx-x64.tar.gz"
    sha256 "PLACEHOLDER_X64_SHA256"
  end

  def install
    bin.install "bin/schrodie"
    bin.install "bin/schrodie-dap"
    lib.install "lib/schrodie.runtime.dll"
    lib.install "lib/syslib64.dll"
    (share/"schrodie/samples").install Dir["samples/*.i"]
    # Copy runtime and syslib to samples so they compile out of the box
    cp lib/"schrodie.runtime.dll", share/"schrodie/samples/"
    cp lib/"syslib64.dll", share/"schrodie/samples/"
    doc.install "schrodie.md"
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
      Or search for "schrodie" in the VS Code extension marketplace.

      Sample programs are in:
        #{share}/schrodie/samples/

      Try:
        cd #{share}/schrodie/samples
        schrodie hello.i
        ./hello
    EOS
  end

  test do
    (testpath/"test.i").write("DO READ OUT #42\nPLEASE GIVE UP\n")
    system bin/"schrodie", "test.i", "-b"
    assert_predicate testpath/"test.exe", :exist?
  end
end
