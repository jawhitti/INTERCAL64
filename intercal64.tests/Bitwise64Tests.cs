using INTERCAL;
using Xunit;
using System.IO;
using System.Diagnostics;

namespace intercal.tests
{
    public class Bitwise64Tests
    {
        // Helper: compile and run a schrodie program using bin/churn.exe
        private string CompileAndRun(string source)
        {
            var dir = Path.Combine(Path.GetTempPath(), "schrodie_test_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(dir);
            try
            {
                // Locate bin/ directory (contains compiler, runtime, syslib)
                var binDir = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory, "..", "..", "..", "..", "bin"));
                var compilerExe = Path.Combine(binDir, "churn.exe");
                if (!File.Exists(compilerExe))
                    return "COMPILER_NOT_FOUND";

                // Copy runtime and syslib to temp dir
                var runtimeSrc = Path.Combine(binDir, "intercal64.runtime.dll");
                if (File.Exists(runtimeSrc))
                    File.Copy(runtimeSrc, Path.Combine(dir, "intercal64.runtime.dll"), true);
                var syslibSrc = Path.Combine(binDir, "syslib64.dll");
                if (File.Exists(syslibSrc))
                    File.Copy(syslibSrc, Path.Combine(dir, "syslib64.dll"), true);

                var srcFile = Path.Combine(dir, "test.ic64");
                File.WriteAllText(srcFile, source);

                // Compile using bin/churn.exe directly
                var compile = Process.Start(new ProcessStartInfo
                {
                    FileName = compilerExe,
                    Arguments = "test.ic64 -b -r:syslib64.dll",
                    WorkingDirectory = dir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                var compileStdout = compile!.StandardOutput.ReadToEnd();
                var compileStderr = compile.StandardError.ReadToEnd();
                compile.WaitForExit(30000);

                // Run
                var exePath = Path.Combine(dir, "test.exe");
                if (!File.Exists(exePath))
                {
                    var files = string.Join(", ", Directory.GetFiles(dir).Select(Path.GetFileName));
                    return $"COMPILE_FAILED: exit={compile.ExitCode} files=[{files}] stdout=[{compileStdout}] stderr=[{compileStderr}] binDir=[{binDir}]";
                }
                var run = Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = dir,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                var output = run!.StandardOutput.ReadToEnd().Trim();
                run.WaitForExit(10000);
                return output;
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void AND64_SameValue_ReturnsSame()
        {
            var result = CompileAndRun(
                "DO ::1 <- ####255\n" +
                "DO ::2 <- ####255\n" +
                "DO (4705773660240084992) NEXT\n" +
                "PLEASE DO READ OUT ::3\n" +
                "DO GIVE UP\n");
            Assert.Equal("255", result);
        }

        [Fact]
        public void AND64_Disjoint_ReturnsZero()
        {
            var result = CompileAndRun(
                "DO ::1 <- ####18374966859414961920\n" +
                "PLEASE DO ::2 <- ####71777214294589695\n" +
                "DO (4705773660240084992) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n");
            Assert.Equal("0", result);
        }

        [Fact]
        public void OR64_Complementary_ReturnsAllBits()
        {
            var result = CompileAndRun(
                "DO ::1 <- ####240\n" +
                "PLEASE DO ::2 <- ####15\n" +
                "DO (5715690474052780032) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n");
            Assert.Equal("255", result);
        }

        [Fact]
        public void XOR64_ReturnsCorrect()
        {
            var result = CompileAndRun(
                "DO ::1 <- ####255\n" +
                "PLEASE DO ::2 <- ####170\n" +
                "DO (6363395191251927040) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n");
            Assert.Equal("85", result);
        }

        [Fact]
        public void XOR64_HighBits_ReturnsCorrect()
        {
            var result = CompileAndRun(
                "DO ::1 <- ####255\n" +
                "PLEASE DO ::2 <- ####18446744073709551615\n" +
                "DO (6363395191251927040) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n");
            Assert.Equal("18446744073709551360", result);
        }
    }
}
