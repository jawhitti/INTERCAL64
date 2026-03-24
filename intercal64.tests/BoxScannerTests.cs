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

        // === Tokenizer: ENTANGLE statement ===

        [Fact]
        public void Tokenizer_RecognizesEntangle()
        {
            var scanner = Scanner.CreateScanner("ENTANGLE");
            Assert.Equal(TokenType.Statement, scanner.Current.Type);
            Assert.Equal("ENTANGLE", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_RecognizesEntanglingGerund()
        {
            var scanner = Scanner.CreateScanner("ENTANGLING");
            Assert.Equal(TokenType.Gerund, scanner.Current.Type);
            Assert.Equal("ENTANGLING", scanner.Current.Value);
        }
    }
}
