using Xunit;
using System.IO;
using System.Diagnostics;

namespace intercal.tests
{
    public class WarnsdorffTests
    {
        private static readonly string BinDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "bin"));
        private static readonly string WarnsdorffDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "samples", "warnsdorff"));

        /// <summary>
        /// Compile and run a test program with the warnsdorff subroutine files.
        /// sourceFiles are compiled in order; the test program should be last.
        /// </summary>
        private string CompileAndRun(string testSource, params string[] libFiles)
        {
            var dir = Path.Combine(Path.GetTempPath(), "schrodie_test_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(dir);
            try
            {
                var compilerExe = Path.Combine(BinDir, "schrodie.exe");
                if (!File.Exists(compilerExe)) return "COMPILER_NOT_FOUND";

                // Copy runtime and syslib
                File.Copy(Path.Combine(BinDir, "schrodie.runtime.dll"),
                    Path.Combine(dir, "schrodie.runtime.dll"), true);
                File.Copy(Path.Combine(BinDir, "syslib64.dll"),
                    Path.Combine(dir, "syslib64.dll"), true);

                // Separate data files (arrays/tables, must init first) from
                // subroutine files (label-wrapped, must come after main program).
                // Data files have .i extension, subroutines have .schrodie.
                var dataFiles = new List<string>();
                var subFiles = new List<string>();
                foreach (var lib in libFiles)
                {
                    var src = Path.Combine(WarnsdorffDir, lib);
                    var dst = Path.Combine(dir, lib);
                    File.Copy(src, dst, true);
                    if (lib.EndsWith(".i"))
                        dataFiles.Add(lib);
                    else
                        subFiles.Add(lib);
                }

                // Write test source
                var testFile = Path.Combine(dir, "test.schrodie");
                File.WriteAllText(testFile, testSource);

                // Order: data tables first, then test program, then subroutines
                var argFiles = new List<string>();
                argFiles.AddRange(dataFiles);
                argFiles.Add("test.schrodie");
                argFiles.AddRange(subFiles);

                // Compile
                var args = string.Join(" ", argFiles) + " -b -r:syslib64.dll -noplease";
                var compile = Process.Start(new ProcessStartInfo
                {
                    FileName = compilerExe,
                    Arguments = args,
                    WorkingDirectory = dir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                compile!.WaitForExit(30000);

                // Output is named after the first file
                var exeName = Path.GetFileNameWithoutExtension(argFiles[0]) + ".exe";
                var exePath = Path.Combine(dir, exeName);
                if (!File.Exists(exePath)) return "COMPILE_FAILED";

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

        // ================================================================
        // ADD64
        // ================================================================

        [Theory]
        [InlineData(0, 0, "0")]
        [InlineData(85, 85, "170")]
        [InlineData(100, 200, "300")]
        [InlineData(4294967295, 1, "4294967296")]  // crosses 32-bit boundary
        [InlineData(9999999999999999999, 1, "10000000000000000000")]
        public void ADD64_ReturnsCorrectSum(ulong a, ulong b, string expected)
        {
            var result = CompileAndRun(
                $"DO ::1 <- ####{a}\n" +
                $"PLEASE DO ::2 <- ####{b}\n" +
                "DO (4702958910472978432) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n",
                "my_add64.schrodie");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ADD64_Commutative()
        {
            var result1 = CompileAndRun(
                "DO ::1 <- ####12345\n" +
                "PLEASE DO ::2 <- ####67890\n" +
                "DO (4702958910472978432) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n",
                "my_add64.schrodie");
            var result2 = CompileAndRun(
                "DO ::1 <- ####67890\n" +
                "PLEASE DO ::2 <- ####12345\n" +
                "DO (4702958910472978432) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n",
                "my_add64.schrodie");
            Assert.Equal(result1, result2);
            Assert.Equal("80235", result1);
        }

        // ================================================================
        // POPCOUNT64
        // ================================================================

        [Theory]
        [InlineData(0, "0")]
        [InlineData(1, "1")]
        [InlineData(255, "8")]
        [InlineData(4294967295, "32")]           // 0xFFFFFFFF
        [InlineData(18446744073709551614, "63")]  // all bits except bit 0
        public void Popcount64_ReturnsCorrectCount(ulong input, string expected)
        {
            var result = CompileAndRun(
                $"DO ::10 <- ####{input}\n" +
                "DO (9100) NEXT\n" +
                "PLEASE READ OUT ::10\n" +
                "DO GIVE UP\n",
                "my_add64.schrodie", "popcount.schrodie");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Popcount64_PowersOfTwo_AllReturnOne()
        {
            // Test a few powers of 2: bit 0, 15, 31, 47, 62
            ulong[] powers = { 1, 32768, 2147483648, 140737488355328, 4611686018427387904 };
            foreach (var p in powers)
            {
                var result = CompileAndRun(
                    $"DO ::10 <- ####{p}\n" +
                    "DO (9100) NEXT\n" +
                    "PLEASE READ OUT ::10\n" +
                    "DO GIVE UP\n",
                    "my_add64.schrodie", "popcount.schrodie");
                Assert.Equal("1", result);
            }
        }

        // ================================================================
        // BIT-TO-INDEX
        // ================================================================

        [Theory]
        [InlineData(1, "0")]               // bit 0
        [InlineData(2, "1")]               // bit 1
        [InlineData(128, "7")]             // bit 7
        [InlineData(268435456, "28")]      // bit 28 = e4 square
        [InlineData(9223372036854775808, "63")]  // bit 63
        public void BitToIndex_ReturnsCorrectIndex(ulong bit, string expected)
        {
            var result = CompileAndRun(
                $"DO ::10 <- ####{bit}\n" +
                "DO (9200) NEXT\n" +
                "PLEASE READ OUT .10\n" +
                "DO GIVE UP\n",
                "my_add64.schrodie", "popcount.schrodie", "bit_to_index.schrodie");
            Assert.Equal(expected, result);
        }

        // ================================================================
        // LOWBIT64
        // ================================================================

        [Theory]
        [InlineData(1, "1")]                        // single bit
        [InlineData(6, "2")]                        // 0b110 -> 0b010
        [InlineData(255, "1")]                      // 0xFF -> bit 0
        [InlineData(256, "256")]                    // bit 8 only
        [InlineData(4294967296, "4294967296")]      // bit 32 only
        [InlineData(18446744073709551614, "2")]     // all bits except 0 -> bit 1
        public void Lowbit64_ReturnsLowestBit(ulong input, string expected)
        {
            var result = CompileAndRun(
                $"DO ::10 <- ####{input}\n" +
                "DO (9300) NEXT\n" +
                "PLEASE READ OUT ::10\n" +
                "DO GIVE UP\n",
                "my_add64.schrodie", "lowbit.schrodie");
            Assert.Equal(expected, result);
        }

        // ================================================================
        // Lookup Tables
        // ================================================================

        [Fact]
        public void KnightAttacks_CornerA1_ReachesCorrectSquares()
        {
            // a1 (sq 0): knight can reach b3 (sq 17) and c2 (sq 10)
            var result = CompileAndRun(
                "DO ::10 <- ;;1 SUB #1\n" +
                "PLEASE READ OUT ::10\n" +
                "DO GIVE UP\n",
                "knight_attacks.i");
            Assert.Equal(((1UL << 10) | (1UL << 17)).ToString(), result);
        }

        [Fact]
        public void KnightAttacks_CornerH8_ReachesCorrectSquares()
        {
            // h8 (sq 63): knight can reach g6 (sq 46) and f7 (sq 53)
            var result = CompileAndRun(
                "DO ::10 <- ;;1 SUB #64\n" +
                "PLEASE READ OUT ::10\n" +
                "DO GIVE UP\n",
                "knight_attacks.i");
            Assert.Equal(((1UL << 46) | (1UL << 53)).ToString(), result);
        }

        [Fact]
        public void KnightAttacks_CenterD4_Has8Moves()
        {
            // d4 (sq 27) is a center square — knight has 8 possible moves
            // Use POPCOUNT to verify
            var result = CompileAndRun(
                "DO ::10 <- ;;1 SUB #28\n" +
                "DO (9100) NEXT\n" +
                "PLEASE READ OUT ::10\n" +
                "DO GIVE UP\n",
                "knight_attacks.i", "my_add64.schrodie", "popcount.schrodie");
            Assert.Equal("8", result);
        }

        [Fact]
        public void ClearMask_Bit0_AllExceptBit0()
        {
            var result = CompileAndRun(
                "DO ::10 <- ;;2 SUB #1\n" +
                "PLEASE READ OUT ::10\n" +
                "DO GIVE UP\n",
                "clear_mask.i");
            Assert.Equal((~0UL ^ 1UL).ToString(), result);
        }

        [Fact]
        public void ClearMask_Bit63_AllExceptBit63()
        {
            var result = CompileAndRun(
                "DO ::10 <- ;;2 SUB #64\n" +
                "PLEASE READ OUT ::10\n" +
                "DO GIVE UP\n",
                "clear_mask.i");
            Assert.Equal((~(1UL << 63)).ToString(), result);
        }

        [Fact]
        public void ClearMask_AND_ClearsSquare()
        {
            // AND64(all-ones, clear_mask[1]) should clear bit 0
            var result = CompileAndRun(
                "DO ::1 <- ####18446744073709551614\n" +
                "PLEASE DO ::2 <- ;;2 SUB #1\n" +
                "DO (4705773660240084992) NEXT\n" +
                "DO READ OUT ::3\n" +
                "PLEASE GIVE UP\n",
                "clear_mask.i");
            // 18446744073709551614 AND (all except bit 0) = 18446744073709551614 (bit 0 was already 0)
            Assert.Equal("18446744073709551614", result);
        }
    }
}
