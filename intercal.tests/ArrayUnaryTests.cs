using INTERCAL;
using INTERCAL.Runtime;
using Xunit;

namespace intercal.tests
{
    public class ArrayUnaryTests
    {
        // === 1D Mirror (|): reverse + bit-invert ===

        [Fact]
        public void Mirror1D_ReversesAndInverts()
        {
            var arr = Array.CreateInstance(typeof(uint), new[] { 3 }, new[] { 1 });
            arr.SetValue(1u, 1);
            arr.SetValue(2u, 2);
            arr.SetValue(3u, 3);

            var result = Lib.MirrorArray(arr);

            Assert.Equal(~3u, (uint)result.GetValue(1));
            Assert.Equal(~2u, (uint)result.GetValue(2));
            Assert.Equal(~1u, (uint)result.GetValue(3));
        }

        // === 1D Invert (-): bit-invert only, no reverse ===

        [Fact]
        public void Invert1D_InvertsWithoutReversing()
        {
            var arr = Array.CreateInstance(typeof(uint), new[] { 3 }, new[] { 1 });
            arr.SetValue(1u, 1);
            arr.SetValue(2u, 2);
            arr.SetValue(3u, 3);

            var result = Lib.InvertArray(arr);

            Assert.Equal(~1u, (uint)result.GetValue(1));
            Assert.Equal(~2u, (uint)result.GetValue(2));
            Assert.Equal(~3u, (uint)result.GetValue(3));
        }

        // === 1D Identity: |-|- is identity ===

        [Fact]
        public void Mirror_Invert_Mirror_Invert_1D_IsIdentity()
        {
            var arr = Array.CreateInstance(typeof(uint), new[] { 3 }, new[] { 1 });
            arr.SetValue(42u, 1);
            arr.SetValue(99u, 2);
            arr.SetValue(7u, 3);

            var result = Lib.InvertArray(Lib.MirrorArray(Lib.InvertArray(Lib.MirrorArray(arr))));

            Assert.Equal(42u, (uint)result.GetValue(1));
            Assert.Equal(99u, (uint)result.GetValue(2));
            Assert.Equal(7u, (uint)result.GetValue(3));
        }

        // === 2D Mirror (|): reverse columns + bit-invert ===

        [Fact]
        public void Mirror2D_ReversesColumnsAndInverts()
        {
            // 1 2 3     ~3 ~2 ~1
            // 4 5 6  -> ~6 ~5 ~4
            // 7 8 9     ~9 ~8 ~7
            var arr = Array.CreateInstance(typeof(uint), new[] { 3, 3 }, new[] { 1, 1 });
            uint v = 1;
            for (int r = 1; r <= 3; r++)
                for (int c = 1; c <= 3; c++)
                    arr.SetValue(v++, r, c);

            var result = Lib.MirrorArray(arr);

            Assert.Equal(~3u, (uint)result.GetValue(1, 1));
            Assert.Equal(~2u, (uint)result.GetValue(1, 2));
            Assert.Equal(~1u, (uint)result.GetValue(1, 3));
            Assert.Equal(~6u, (uint)result.GetValue(2, 1));
            Assert.Equal(~5u, (uint)result.GetValue(2, 2));
            Assert.Equal(~4u, (uint)result.GetValue(2, 3));
            Assert.Equal(~9u, (uint)result.GetValue(3, 1));
            Assert.Equal(~8u, (uint)result.GetValue(3, 2));
            Assert.Equal(~7u, (uint)result.GetValue(3, 3));
        }

        // === 2D Invert (-): reverse rows + bit-invert ===

        [Fact]
        public void Invert2D_ReversesRowsAndInverts()
        {
            // 1 2 3     ~7 ~8 ~9
            // 4 5 6  -> ~4 ~5 ~6
            // 7 8 9     ~1 ~2 ~3
            var arr = Array.CreateInstance(typeof(uint), new[] { 3, 3 }, new[] { 1, 1 });
            uint v = 1;
            for (int r = 1; r <= 3; r++)
                for (int c = 1; c <= 3; c++)
                    arr.SetValue(v++, r, c);

            var result = Lib.InvertArray(arr);

            Assert.Equal(~7u, (uint)result.GetValue(1, 1));
            Assert.Equal(~8u, (uint)result.GetValue(1, 2));
            Assert.Equal(~9u, (uint)result.GetValue(1, 3));
            Assert.Equal(~4u, (uint)result.GetValue(2, 1));
            Assert.Equal(~5u, (uint)result.GetValue(2, 2));
            Assert.Equal(~6u, (uint)result.GetValue(2, 3));
            Assert.Equal(~1u, (uint)result.GetValue(3, 1));
            Assert.Equal(~2u, (uint)result.GetValue(3, 2));
            Assert.Equal(~3u, (uint)result.GetValue(3, 3));
        }

        // === 2D Identity: |-|- is identity ===

        [Fact]
        public void Mirror_Invert_Mirror_Invert_2D_IsIdentity()
        {
            var arr = Array.CreateInstance(typeof(uint), new[] { 3, 3 }, new[] { 1, 1 });
            uint v = 1;
            for (int r = 1; r <= 3; r++)
                for (int c = 1; c <= 3; c++)
                    arr.SetValue(v++, r, c);

            var result = Lib.InvertArray(Lib.MirrorArray(Lib.InvertArray(Lib.MirrorArray(arr))));

            v = 1;
            for (int r = 1; r <= 3; r++)
                for (int c = 1; c <= 3; c++)
                    Assert.Equal(v++, (uint)result.GetValue(r, c));
        }

        // === 64-bit arrays ===

        [Fact]
        public void Mirror1D_64bit_ReversesAndInverts()
        {
            var arr = Array.CreateInstance(typeof(ulong), new[] { 2 }, new[] { 1 });
            arr.SetValue(1UL, 1);
            arr.SetValue(2UL, 2);

            var result = Lib.MirrorArray(arr);

            Assert.Equal(~2UL, (ulong)result.GetValue(1));
            Assert.Equal(~1UL, (ulong)result.GetValue(2));
        }

        // === Does not modify original ===

        [Fact]
        public void Mirror_DoesNotModifyOriginal()
        {
            var arr = Array.CreateInstance(typeof(uint), new[] { 3 }, new[] { 1 });
            arr.SetValue(1u, 1);
            arr.SetValue(2u, 2);
            arr.SetValue(3u, 3);

            Lib.MirrorArray(arr);

            Assert.Equal(1u, (uint)arr.GetValue(1));
            Assert.Equal(2u, (uint)arr.GetValue(2));
            Assert.Equal(3u, (uint)arr.GetValue(3));
        }

        // === Parser tests ===

        private Program ParseSource(string source)
        {
            string tmpFile = System.IO.Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(tmpFile, source);
                return Program.CreateFromFile(tmpFile);
            }
            finally
            {
                System.IO.File.Delete(tmpFile);
            }
        }

        [Fact]
        public void Parse_MirrorArray()
        {
            var p = ParseSource(
                "DO ,1 <- #3\n" +
                "DO ,2 <- #3\n" +
                "PLEASE DO ,2 <- |,1\n" +
                "DO GIVE UP\n");
            Assert.Equal(4, p.StatementCount);
        }

        [Fact]
        public void Parse_InvertArray()
        {
            var p = ParseSource(
                "DO ,1 <- #3\n" +
                "DO ,2 <- #3\n" +
                "DO ,2 <- -,1\n" +
                "PLEASE GIVE UP\n");
            Assert.Equal(4, p.StatementCount);
        }

        [Fact]
        public void Parse_ChainedMirrorInvertArray()
        {
            var p = ParseSource(
                "DO ,1 <- #3\n" +
                "DO ,2 <- #3\n" +
                "PLEASE DO ,2 <- -|,1\n" +
                "DO GIVE UP\n");
            Assert.Equal(4, p.StatementCount);
        }

        [Fact]
        public void Parse_IdentityChainArray()
        {
            var p = ParseSource(
                "DO ,1 <- #3\n" +
                "DO ,2 <- #3\n" +
                "DO ,2 <- |-|-,1\n" +
                "PLEASE GIVE UP\n");
            Assert.Equal(4, p.StatementCount);
        }
    }
}
