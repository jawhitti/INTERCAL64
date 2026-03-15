using INTERCAL;
using Xunit;

namespace intercal.tests
{
    public class ScannerTests
    {
        [Fact]
        public void CreateScanner_RecognizesLabel()
        {
            var scanner = Scanner.CreateScanner("(100)");
            Assert.True(scanner.Current.Groups["label"].Success);
            Assert.Equal("(100)", scanner.Current.Groups["label"].Value);
        }

        [Fact]
        public void CreateScanner_RecognizesDoPrefix()
        {
            var scanner = Scanner.CreateScanner("DO");
            Assert.True(scanner.Current.Groups["prefix"].Success);
            Assert.Equal("DO", scanner.Current.Value);
        }

        [Fact]
        public void CreateScanner_RecognizesPleasePrefix()
        {
            var scanner = Scanner.CreateScanner("PLEASE");
            Assert.True(scanner.Current.Groups["prefix"].Success);
            Assert.Equal("PLEASE", scanner.Current.Value);
        }

        [Fact]
        public void CreateScanner_RecognizesStatements()
        {
            var scanner = Scanner.CreateScanner("READ OUT");
            Assert.True(scanner.Current.Groups["statement"].Success);
            Assert.Equal("READ OUT", scanner.Current.Value);
        }

        [Fact]
        public void CreateScanner_RecognizesGiveUp()
        {
            var scanner = Scanner.CreateScanner("GIVE UP");
            Assert.True(scanner.Current.Groups["statement"].Success);
        }

        [Fact]
        public void CreateScanner_RecognizesVariableTypes()
        {
            var scanner = Scanner.CreateScanner(".");
            Assert.True(scanner.Current.Groups["var"].Success);

            scanner = Scanner.CreateScanner(",");
            Assert.True(scanner.Current.Groups["var"].Success);

            scanner = Scanner.CreateScanner(":");
            Assert.True(scanner.Current.Groups["var"].Success);

            scanner = Scanner.CreateScanner(";");
            Assert.True(scanner.Current.Groups["var"].Success);

            scanner = Scanner.CreateScanner("#");
            Assert.True(scanner.Current.Groups["var"].Success);
        }

        [Fact]
        public void CreateScanner_RecognizesUnaryOperators()
        {
            var scanner = Scanner.CreateScanner("&");
            Assert.True(scanner.Current.Groups["unary_op"].Success);

            scanner = Scanner.CreateScanner("V");
            Assert.True(scanner.Current.Groups["unary_op"].Success);

            scanner = Scanner.CreateScanner("?");
            Assert.True(scanner.Current.Groups["unary_op"].Success);
        }

        [Fact]
        public void CreateScanner_RecognizesBinaryOperators()
        {
            var scanner = Scanner.CreateScanner("$");
            Assert.True(scanner.Current.Groups["binary_op"].Success);

            scanner = Scanner.CreateScanner("~");
            Assert.True(scanner.Current.Groups["binary_op"].Success);
        }

        [Fact]
        public void CreateScanner_RecognizesGerunds()
        {
            var scanner = Scanner.CreateScanner("NEXTING");
            Assert.True(scanner.Current.Groups["gerund"].Success);

            scanner = Scanner.CreateScanner("CALCULATING");
            Assert.True(scanner.Current.Groups["gerund"].Success);

            scanner = Scanner.CreateScanner("COMING FROM");
            Assert.True(scanner.Current.Groups["gerund"].Success);
        }

        [Fact]
        public void MoveNext_AdvancesToken()
        {
            var scanner = Scanner.CreateScanner("DO GIVE UP");
            Assert.Equal("DO", scanner.Current.Value);
            scanner.MoveNext();
            Assert.Equal("GIVE UP", scanner.Current.Value);
        }

        [Fact]
        public void PeekNext_ShowsNextWithoutAdvancing()
        {
            var scanner = Scanner.CreateScanner("DO GIVE UP");
            Assert.Equal("GIVE UP", scanner.PeekNext.Value);
            Assert.Equal("DO", scanner.Current.Value);
        }

        [Fact]
        public void Scanner_SkipsNewlines()
        {
            // Newlines between tokens are consumed by MoveNext
            var scanner = Scanner.CreateScanner("DO\nGIVE UP");
            Assert.Equal("DO", scanner.Current.Value);
            scanner.MoveNext();
            // After MoveNext, newlines are swallowed
            scanner.MoveNext();
            Assert.Equal("GIVE UP", scanner.Current.Value);
        }

        [Fact]
        public void Scanner_RecognizesAssignment()
        {
            var scanner = Scanner.CreateScanner("<-");
            Assert.True(scanner.Current.Groups["statement"].Success);
            Assert.Equal("<-", scanner.Current.Value);
        }

        [Fact]
        public void Scanner_RecognizesDigits()
        {
            var scanner = Scanner.CreateScanner("42");
            Assert.True(scanner.Current.Groups["digits"].Success);
            Assert.Equal("42", scanner.Current.Value);
        }

        [Fact]
        public void ReadGroupValue_ThrowsOnWrongGroup()
        {
            var scanner = Scanner.CreateScanner("DO");
            Assert.ThrowsAny<Exception>(() => scanner.ReadGroupValue("label"));
        }

        [Fact]
        public void VerifyToken_ThrowsOnMismatch()
        {
            var scanner = Scanner.CreateScanner("DO");
            Assert.ThrowsAny<Exception>(() => scanner.VerifyToken("PLEASE"));
        }

        [Fact]
        public void VerifyToken_PassesOnMatch()
        {
            var scanner = Scanner.CreateScanner("DO");
            scanner.VerifyToken("DO"); // should not throw
        }
    }
}
