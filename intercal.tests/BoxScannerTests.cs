using INTERCAL;
using Xunit;

namespace intercal.tests
{
    public class BoxScannerTests
    {
        // === Tokenizer: [] cat box prefix ===

        [Fact]
        public void Tokenizer_RecognizesCatBox()
        {
            var scanner = Scanner.CreateScanner("[]");
            Assert.Equal(TokenType.Var, scanner.Current.Type);
            Assert.Equal("[]", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_CatBoxWithDigits()
        {
            var scanner = Scanner.CreateScanner("DO []1 <- #1 = #2");
            Assert.Equal("DO", scanner.Current.Value);
            scanner.MoveNext();
            Assert.Equal("[]", scanner.Current.Value);
            Assert.Equal(TokenType.Var, scanner.Current.Type);
            scanner.MoveNext();
            Assert.Equal("1", scanner.Current.Value);
            Assert.Equal(TokenType.Digits, scanner.Current.Type);
        }

        // === Tokenizer: = double worm operator ===

        [Fact]
        public void Tokenizer_RecognizesDoubleWorm()
        {
            var scanner = Scanner.CreateScanner("=");
            Assert.Equal(TokenType.BinaryOp, scanner.Current.Type);
            Assert.Equal("=", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_DoubleWormInExpression()
        {
            var scanner = Scanner.CreateScanner("#1 = #2");
            Assert.Equal("#", scanner.Current.Value);
            scanner.MoveNext();
            Assert.Equal("1", scanner.Current.Value);
            scanner.MoveNext();
            Assert.Equal("=", scanner.Current.Value);
            Assert.Equal(TokenType.BinaryOp, scanner.Current.Type);
            scanner.MoveNext();
            Assert.Equal("#", scanner.Current.Value);
        }

        // === Tokenizer: MASH statement ===

        [Fact]
        public void Tokenizer_RecognizesMash()
        {
            var scanner = Scanner.CreateScanner("MASH");
            Assert.Equal(TokenType.Statement, scanner.Current.Type);
            Assert.Equal("MASH", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_RecognizesMashingGerund()
        {
            var scanner = Scanner.CreateScanner("MASHING");
            Assert.Equal(TokenType.Gerund, scanner.Current.Type);
            Assert.Equal("MASHING", scanner.Current.Value);
        }
    }
}
