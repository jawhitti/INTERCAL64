using INTERCAL;
using INTERCAL.Runtime;
using Xunit;
using System.IO;
using System.Linq;
using ExecutionContext = INTERCAL.Runtime.ExecutionContext;

namespace intercal.tests
{
    public class BraKetTests
    {
        // === Tokenizer ===

        [Fact]
        public void Tokenizer_UnicodeBraKet_ProducesBraToken()
        {
            var scanner = Scanner.CreateScanner("\u27E81\u007C\u03C8\u27E9");  // ⟨1|ψ⟩
            Assert.Equal(TokenType.Bra, scanner.Current.Type);
            Assert.Equal("1", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_UnicodeBraKet_MultiDigit()
        {
            var scanner = Scanner.CreateScanner("\u27E838\u007C\u03C8\u27E9");  // ⟨38|ψ⟩
            Assert.Equal(TokenType.Bra, scanner.Current.Type);
            Assert.Equal("38", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_WimpmodeBraKet_ProducesBraTokenWithBang()
        {
            var scanner = Scanner.CreateScanner("<5|?>");
            Assert.Equal(TokenType.Bra, scanner.Current.Type);
            Assert.Equal("!5", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_WimpmodeBraKet_MultiDigit()
        {
            var scanner = Scanner.CreateScanner("<38|?>");
            Assert.Equal(TokenType.Bra, scanner.Current.Type);
            Assert.Equal("!38", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_ArrowNotConfusedWithBra()
        {
            // <-  should still be an assignment, not a bra
            var scanner = Scanner.CreateScanner("<-");
            Assert.Equal(TokenType.Statement, scanner.Current.Type);
            Assert.Equal("<-", scanner.Current.Value);
        }

        [Fact]
        public void Tokenizer_MixedBrackets_AsciiBraUnicodeKet()
        {
            // <5|ψ⟩ — ASCII open, unicode close, should still work
            var scanner = Scanner.CreateScanner("<5\u007C\u03C8\u27E9");
            Assert.Equal(TokenType.Bra, scanner.Current.Type);
            Assert.Equal("!5", scanner.Current.Value);
        }

        // === Parser ===

        private Program ParseSource(string source)
        {
            string tmpFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tmpFile, source);
                return Program.CreateFromFile(tmpFile);
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public void Parse_BraKetGuard_SetsBoxGuard()
        {
            var p = ParseSource(
                "DO .1 <- #42\n" +
                "DO \u27E81\u007C\u03C8\u27E9 READ OUT .1\n" +  // DO ⟨1|ψ⟩ READ OUT .1
                "DO GIVE UP\n");
            Assert.Equal(3, p.StatementCount);
            Assert.Equal("[]1", p.Statements[1].BoxGuard);
            Assert.False(p.Statements[1].BoxGuardWimp);
        }

        [Fact]
        public void Parse_WimpmodeBraKet_SetsBoxGuardWimp()
        {
            var p = ParseSource(
                "DO .1 <- #42\n" +
                "DO <1|?> READ OUT .1\n" +
                "DO GIVE UP\n");
            Assert.Equal(3, p.StatementCount);
            Assert.Equal("[]1", p.Statements[1].BoxGuard);
            Assert.True(p.Statements[1].BoxGuardWimp);
        }

        [Fact]
        public void Parse_NoBraKet_BoxGuardIsNull()
        {
            var p = ParseSource(
                "DO .1 <- #42\n" +
                "DO READ OUT .1\n" +
                "DO GIVE UP\n");
            Assert.Null(p.Statements[1].BoxGuard);
        }

        [Fact]
        public void Parse_BoxAssignment_NotConfusedWithBraKet()
        {
            // DO []1 <- .1  should NOT set a box guard
            var p = ParseSource(
                "DO .1 <- #42\n" +
                "PLEASE DO []1 <- .1\n" +
                "DO GIVE UP\n");
            Assert.Equal(3, p.StatementCount);
            Assert.Null(p.Statements[1].BoxGuard);
        }

        [Fact]
        public void Parse_BraKetWithMash_FullProgram()
        {
            // Full box guard program should parse
            var p = ParseSource(
                "DO .1 <- #42\n" +
                "DO .2 <- #99\n" +
                "PLEASE DO []1 <- .1\n" +
                "DO []2 <- .2\n" +
                "DO ENTANGLE []1 + []2\n" +
                "DO \u27E81\u007C\u03C8\u27E9 READ OUT .1\n" +
                "PLEASE DO \u27E82\u007C\u03C8\u27E9 READ OUT .2\n" +
                "DO GIVE UP\n");
            Assert.Equal(8, p.StatementCount);
            Assert.Equal("[]1", p.Statements[5].BoxGuard);
            Assert.Equal("[]2", p.Statements[6].BoxGuard);
        }

        // === Runtime: IsBoxAlive ===

        [Fact]
        public void IsBoxAlive_CollapsesSingleBox()
        {
            var ctx = new ExecutionContext();
            ctx.CreateBox("[]1", 42);
            // Single box, 50/50 alive or dead
            bool result = ctx.IsBoxAlive("[]1");
            // Either way, it should be collapsed now
            var boxes = ctx.GetAllBoxVariables();
            Assert.True(boxes["[]1"] != "?", "Box should be collapsed after IsBoxAlive");
        }

        [Fact]
        public void IsBoxAlive_EntangledPair_ExactlyOneSurvives()
        {
            for (int i = 0; i < 100; i++)
            {
                var ctx = new ExecutionContext();
                ctx.CreateBox("[]1", 42);
                ctx.CreateBox("[]2", 99);
                ctx.EntangleBoxes("[]1", "[]2");

                bool alive1 = ctx.IsBoxAlive("[]1");
                bool alive2 = ctx.IsBoxAlive("[]2");

                Assert.True(alive1 != alive2,
                    $"Trial {i}: expected exactly one survivor but got alive1={alive1} alive2={alive2}");
            }
        }

        [Fact]
        public void IsBoxAlive_EntangledPair_CollapsesBoth()
        {
            var ctx = new ExecutionContext();
            ctx.CreateBox("[]1", 42);
            ctx.CreateBox("[]2", 99);
            ctx.EntangleBoxes("[]1", "[]2");

            // Collapsing one should collapse both
            ctx.IsBoxAlive("[]1");
            var boxes = ctx.GetAllBoxVariables();
            Assert.NotEqual("?", boxes["[]1"]);
            Assert.NotEqual("?", boxes["[]2"]);
        }

        [Fact]
        public void IsBoxAlive_UninitializedBox_Throws()
        {
            var ctx = new ExecutionContext();
            Assert.ThrowsAny<Exception>(() => ctx.IsBoxAlive("[]99"));
        }

        // === Runtime: GetAllBoxVariables display values ===

        [Fact]
        public void GetAllBoxVariables_Uncollapsed_ShowsQuestionMark()
        {
            var ctx = new ExecutionContext();
            ctx.CreateBox("[]1", 42);
            var boxes = ctx.GetAllBoxVariables();
            Assert.Equal("?", boxes["[]1"]);
        }

        [Fact]
        public void GetAllBoxVariables_CollapsedAlive_ShowsValue()
        {
            // Run until we get an alive result
            for (int i = 0; i < 100; i++)
            {
                var ctx = new ExecutionContext();
                ctx.CreateBox("[]1", 42);
                if (ctx.IsBoxAlive("[]1"))
                {
                    var boxes = ctx.GetAllBoxVariables();
                    Assert.Equal("42", boxes["[]1"]);
                    return;
                }
            }
            Assert.Fail("Never got an alive cat in 100 trials");
        }

        [Fact]
        public void GetAllBoxVariables_CollapsedDead_ShowsDead()
        {
            // Run until we get a dead result
            for (int i = 0; i < 100; i++)
            {
                var ctx = new ExecutionContext();
                ctx.CreateBox("[]1", 42);
                if (!ctx.IsBoxAlive("[]1"))
                {
                    var boxes = ctx.GetAllBoxVariables();
                    Assert.Equal("(dead)", boxes["[]1"]);
                    return;
                }
            }
            Assert.Fail("Never got a dead cat in 100 trials");
        }
    }
}
