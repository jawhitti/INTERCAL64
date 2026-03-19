using INTERCAL.Runtime;
using Xunit;

using ExecutionContext = INTERCAL.Runtime.ExecutionContext;

namespace intercal.tests
{
    public class BoxVariableTests
    {
        [Fact]
        public void Box_UninitializedBox_ThrowsE200()
        {
            var ctx = new ExecutionContext();
            Assert.Throws<IntercalException>(() => ctx.CollapseBox("[]1"));
        }

        [Fact]
        public void Box_Collapse_ReturnsValueOrDead()
        {
            var ctx = new ExecutionContext();
            ctx.CreateBox("[]1", 42);
            ulong result = ctx.CollapseBox("[]1");
            // Single-value box: 50/50 alive (42) or dead (VACANT)
            Assert.True(result == 42 || result == QValue.VACANT);
        }

        [Fact]
        public void Box_Collapse_BoxRetainsChosenValue()
        {
            var ctx = new ExecutionContext();
            ctx.CreateBox("[]1", 42);
            ulong first = ctx.CollapseBox("[]1");
            ulong second = ctx.CollapseBox("[]1");
            Assert.Equal(first, second);
        }

        [Fact]
        public void Box_MismatchedTypes_ErrorMessageExists()
        {
            Assert.Contains("DIFFERENT SIZE", Messages.E2010);
        }
    }
}
