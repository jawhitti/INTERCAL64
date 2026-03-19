# Homebrew formula for INTERCAL
# Install: brew tap jawhitti/intercal && brew install intercal
# Usage: intercal hello.i && ./hello

class Intercal < Formula
  desc "Compiler for INTERCAL, the language with no pronounceable acronym. Now with quantum cats."
  homepage "https://github.com/jawhitti/INTERCAL"
  version "0.3.0"

  if Hardware::CPU.arm?
    url "https://github.com/jawhitti/INTERCAL/releases/download/v0.3.0/intercal-0.3.0-osx-arm64.tar.gz"
    sha256 "PLACEHOLDER_ARM64_SHA256"
  else
    url "https://github.com/jawhitti/INTERCAL/releases/download/v0.3.0/intercal-0.3.0-osx-x64.tar.gz"
    sha256 "PLACEHOLDER_X64_SHA256"
  end

  def install
    bin.install "bin/intercal"
    bin.install "bin/intercal-dap"
    lib.install "lib/intercal.runtime.dll"
    lib.install "lib/syslib64.dll"
    (share/"intercal/samples").install Dir["samples/*.i"]
    # Copy runtime and syslib to samples so they compile out of the box
    cp lib/"intercal.runtime.dll", share/"intercal/samples/"
    cp lib/"syslib64.dll", share/"intercal/samples/"
    doc.install "quantum-intercal.md"
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
      Or search for "INTERCAL" in the VS Code extension marketplace.

      Sample programs are in:
        #{share}/intercal/samples/

      Try:
        cd #{share}/intercal/samples
        intercal hello.i
        ./hello
    EOS
  end

  test do
    (testpath/"test.i").write("DO READ OUT #42\nPLEASE GIVE UP\n")
    system bin/"intercal", "test.i", "-b"
    assert_predicate testpath/"test.exe", :exist?
  end
end
