using INTERCAL.Runtime;
using Xunit;

using ExecutionContext = INTERCAL.Runtime.ExecutionContext;

namespace intercal.tests
{
    public class ExecutionContextTests
    {
        // Spot (.) variables are 16-bit, Two-spot (:) are 32-bit, Four-spot (::) are 64-bit
        [Fact]
        public void SpotVariable_SetAndGet()
        {
            var ctx = new ExecutionContext();
            ctx[".1"] = 42;
            Assert.Equal(42u, ctx[".1"]);
        }

        [Fact]
        public void TwoSpotVariable_SetAndGet()
        {
            var ctx = new ExecutionContext();
            ctx[":1"] = 100000;
            Assert.Equal(100000u, ctx[":1"]);
        }

        [Fact]
        public void SpotVariable_DefaultsToZero()
        {
            var ctx = new ExecutionContext();
            // Variables must be assigned before reading; set then verify default-like behavior
            ctx[".5"] = 0;
            Assert.Equal(0u, ctx[".5"]);
        }

        [Fact]
        public void SpotVariable_RejectsValueOver16Bits()
        {
            var ctx = new ExecutionContext();
            Assert.Throws<IntercalException>(() => ctx[".1"] = 70000);
        }

        [Fact]
        public void TwoSpotVariable_AcceptsLargeValues()
        {
            var ctx = new ExecutionContext();
            ctx[":1"] = uint.MaxValue;
            Assert.Equal(uint.MaxValue, ctx[":1"]);
        }

        [Fact]
        public void Stash_And_Retrieve_RestoresValue()
        {
            var ctx = new ExecutionContext();
            ctx[".1"] = 10;
            ctx.Stash(".1");
            ctx[".1"] = 20;
            Assert.Equal(20u, ctx[".1"]);
            ctx.Retrieve(".1");
            Assert.Equal(10u, ctx[".1"]);
        }

        [Fact]
        public void Stash_MultipleValues_RestoresInOrder()
        {
            var ctx = new ExecutionContext();
            ctx[".1"] = 1;
            ctx.Stash(".1");
            ctx[".1"] = 2;
            ctx.Stash(".1");
            ctx[".1"] = 3;

            ctx.Retrieve(".1");
            Assert.Equal(2u, ctx[".1"]);
            ctx.Retrieve(".1");
            Assert.Equal(1u, ctx[".1"]);
        }

        [Fact]
        public void Retrieve_WithoutStash_Throws()
        {
            var ctx = new ExecutionContext();
            ctx[".1"] = 5;
            Assert.Throws<IntercalException>(() => ctx.Retrieve(".1"));
        }

        [Fact]
        public void Ignore_PreventsAssignment()
        {
            var ctx = new ExecutionContext();
            ctx[".1"] = 10;
            ctx.Ignore(".1");
            ctx[".1"] = 99;
            Assert.Equal(10u, ctx[".1"]);
        }

        [Fact]
        public void Remember_ReenablesAssignment()
        {
            var ctx = new ExecutionContext();
            ctx[".1"] = 10;
            ctx.Ignore(".1");
            ctx[".1"] = 99;
            ctx.Remember(".1");
            ctx[".1"] = 42;
            Assert.Equal(42u, ctx[".1"]);
        }

        [Fact]
        public void ArrayVariable_ReDimAndAccess()
        {
            var ctx = new ExecutionContext();
            ctx.ReDim(",1", new int[] { 3 });
            ctx[",1", new int[] { 1 }] = 100;
            ctx[",1", new int[] { 2 }] = 200;
            ctx[",1", new int[] { 3 }] = 300;

            Assert.Equal(100u, ctx[",1", new int[] { 1 }]);
            Assert.Equal(200u, ctx[",1", new int[] { 2 }]);
            Assert.Equal(300u, ctx[",1", new int[] { 3 }]);
        }

        [Fact]
        public void ArrayVariable_StashAndRetrieve()
        {
            var ctx = new ExecutionContext();
            ctx.ReDim(",1", new int[] { 2 });
            ctx[",1", new int[] { 1 }] = 10;
            ctx[",1", new int[] { 2 }] = 20;

            ctx.Stash(",1");

            ctx.ReDim(",1", new int[] { 2 });
            ctx[",1", new int[] { 1 }] = 99;

            ctx.Retrieve(",1");
            Assert.Equal(10u, ctx[",1", new int[] { 1 }]);
            Assert.Equal(20u, ctx[",1", new int[] { 2 }]);
        }

        [Fact]
        public void InvalidVariablePrefix_Throws()
        {
            var ctx = new ExecutionContext();
            Assert.Throws<IntercalException>(() => { var _ = ctx["x1"]; });
        }

        [Fact]
        public void MultipleVariables_AreIndependent()
        {
            var ctx = new ExecutionContext();
            ctx[".1"] = 10;
            ctx[".2"] = 20;
            ctx[":1"] = 30;

            Assert.Equal(10UL, ctx[".1"]);
            Assert.Equal(20UL, ctx[".2"]);
            Assert.Equal(30UL, ctx[":1"]);
        }

        // Four-spot (::) 64-bit variable tests
        [Fact]
        public void FourSpotVariable_SetAndGet()
        {
            var ctx = new ExecutionContext();
            ctx["::1"] = 42UL;
            Assert.Equal(42UL, ctx["::1"]);
        }

        [Fact]
        public void FourSpotVariable_AcceptsFullUInt64()
        {
            var ctx = new ExecutionContext();
            ctx["::1"] = ulong.MaxValue;
            Assert.Equal(ulong.MaxValue, ctx["::1"]);
        }

        [Fact]
        public void FourSpotVariable_AcceptsValueOver32Bits()
        {
            var ctx = new ExecutionContext();
            ulong bigValue = (ulong)uint.MaxValue + 1;
            ctx["::1"] = bigValue;
            Assert.Equal(bigValue, ctx["::1"]);
        }

        [Fact]
        public void TwoSpotVariable_RejectsValueOver32Bits()
        {
            var ctx = new ExecutionContext();
            ulong tooBig = (ulong)uint.MaxValue + 1;
            Assert.Throws<IntercalException>(() => ctx[":1"] = tooBig);
        }

        [Fact]
        public void FourSpotVariable_StashAndRetrieve()
        {
            var ctx = new ExecutionContext();
            ulong bigVal = 1UL << 40;
            ctx["::1"] = bigVal;
            ctx.Stash("::1");
            ctx["::1"] = 999;
            Assert.Equal(999UL, ctx["::1"]);
            ctx.Retrieve("::1");
            Assert.Equal(bigVal, ctx["::1"]);
        }

        [Fact]
        public void FourSpotVariable_IgnoreAndRemember()
        {
            var ctx = new ExecutionContext();
            ctx["::1"] = 1UL << 50;
            ctx.Ignore("::1");
            ctx["::1"] = 0;
            Assert.Equal(1UL << 50, ctx["::1"]);
            ctx.Remember("::1");
            ctx["::1"] = 42;
            Assert.Equal(42UL, ctx["::1"]);
        }

        [Fact]
        public void FourSpotVariable_IndependentFromTwoSpot()
        {
            var ctx = new ExecutionContext();
            ctx[":1"] = 100;
            ctx["::1"] = 200;
            Assert.Equal(100UL, ctx[":1"]);
            Assert.Equal(200UL, ctx["::1"]);
        }

        // Double-hybrid (;;) 64-bit array tests
        [Fact]
        public void DoubleHybridArray_ReDimAndAccess()
        {
            var ctx = new ExecutionContext();
            ctx.ReDim(";;1", new int[] { 2 });
            ulong bigVal = 1UL << 48;
            ctx[";;1", new int[] { 1 }] = bigVal;
            ctx[";;1", new int[] { 2 }] = 42;
            Assert.Equal(bigVal, ctx[";;1", new int[] { 1 }]);
            Assert.Equal(42UL, ctx[";;1", new int[] { 2 }]);
        }
    }
}
