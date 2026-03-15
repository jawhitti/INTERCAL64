using INTERCAL.Runtime;
using Xunit;

namespace intercal.tests
{
    public class LibTests
    {
        // Mingle interleaves the bits of two 16-bit values into a 32-bit result.
        // E.g. Mingle(0, 0xFFFF) puts all 1s in the even bit positions.
        [Fact]
        public void Mingle_ZeroWithZero_ReturnsZero()
        {
            Assert.Equal(0u, Lib.Mingle(0, 0));
        }

        [Fact]
        public void Mingle_OneWithZero_ReturnsTwo()
        {
            // bit 0 of 'men' goes to bit 1 of result
            Assert.Equal(2u, Lib.Mingle(1, 0));
        }

        [Fact]
        public void Mingle_ZeroWithOne_ReturnsOne()
        {
            // bit 0 of 'ladies' goes to bit 0 of result
            Assert.Equal(1u, Lib.Mingle(0, 1));
        }

        [Fact]
        public void Mingle_OneWithOne_ReturnsThree()
        {
            Assert.Equal(3u, Lib.Mingle(1, 1));
        }

        [Fact]
        public void Mingle_MaxValues_ReturnsAllOnes()
        {
            Assert.Equal(0xFFFFFFFF, Lib.Mingle(0xFFFF, 0xFFFF));
        }

        [Fact]
        public void Mingle_ZeroWithMax_ReturnsEvenBits()
        {
            // All 'ladies' bits set, all 'men' bits zero → every even bit set
            Assert.Equal(0x55555555u, Lib.Mingle(0, 0xFFFF));
        }

        [Fact]
        public void Mingle_MaxWithZero_ReturnsOddBits()
        {
            Assert.Equal(0xAAAAAAAAu, Lib.Mingle(0xFFFF, 0));
        }

        // Select extracts bits from 'a' at positions where 'b' has 1s,
        // then packs them into the low-order bits.
        [Fact]
        public void Select32_AllZeros_ReturnsZero()
        {
            Assert.Equal(0u, Lib.Select(0u, 0u));
        }

        [Fact]
        public void Select32_AllOnes_MaskAllOnes_ReturnsSame()
        {
            Assert.Equal(0xFFFFFFFFu, Lib.Select(0xFFFFFFFF, 0xFFFFFFFF));
        }

        [Fact]
        public void Select32_ExtractsCorrectBits()
        {
            // a = 0b1010, b = 0b1100 → selected bits are {1,0} → packed = 0b10 = 2
            Assert.Equal(2u, Lib.Select(0b1010u, 0b1100u));
        }

        [Fact]
        public void Select16_ExtractsCorrectBits()
        {
            Assert.Equal((ushort)2, Lib.Select((ushort)0b1010, (ushort)0b1100));
        }

        // Rotate shifts right by 1, wrapping the low bit to the high bit.
        [Fact]
        public void Rotate32_One_WrapsToHighBit()
        {
            Assert.Equal(0x80000000u, Lib.Rotate(1u));
        }

        [Fact]
        public void Rotate32_Two_ReturnsOne()
        {
            Assert.Equal(1u, Lib.Rotate(2u));
        }

        [Fact]
        public void Rotate32_Zero_ReturnsZero()
        {
            Assert.Equal(0u, Lib.Rotate(0u));
        }

        [Fact]
        public void Rotate16_One_WrapsToHighBit()
        {
            Assert.Equal((ushort)0x8000, Lib.Rotate((ushort)1));
        }

        [Fact]
        public void Rotate16_Two_ReturnsOne()
        {
            Assert.Equal((ushort)1, Lib.Rotate((ushort)2));
        }

        // Reverse reverses the bit order of a 16-bit value.
        [Fact]
        public void Reverse_One_Returns0x8000()
        {
            Assert.Equal((ushort)0x8000, Lib.Reverse(1));
        }

        [Fact]
        public void Reverse_Zero_ReturnsZero()
        {
            Assert.Equal((ushort)0, Lib.Reverse(0));
        }

        [Fact]
        public void Reverse_IsOwnInverse()
        {
            ushort val = 0x1234;
            Assert.Equal(val, Lib.Reverse(Lib.Reverse(val)));
        }

        // And = val & Rotate(val)
        [Fact]
        public void And_Zero_ReturnsZero()
        {
            Assert.Equal(0u, Lib.And(0));
        }

        [Fact]
        public void And_AllOnes32_ReturnsAllOnes()
        {
            Assert.Equal(0xFFFFFFFFu, Lib.And(0xFFFFFFFF));
        }

        // Or = val | Rotate(val)
        [Fact]
        public void Or_Zero_ReturnsZero()
        {
            Assert.Equal(0u, Lib.Or(0));
        }

        [Fact]
        public void Or_AllOnes32_ReturnsAllOnes()
        {
            Assert.Equal(0xFFFFFFFFu, Lib.Or(0xFFFFFFFF));
        }

        [Fact]
        public void Or_One_Expands()
        {
            // Or(1) = 1 | Rotate(1) = 1 | 0x8000 = 0x8001 (16-bit path)
            Assert.Equal(0x8001u, Lib.Or(1));
        }

        // Xor = val ^ Rotate(val)
        [Fact]
        public void Xor_Zero_ReturnsZero()
        {
            Assert.Equal(0u, Lib.Xor(0));
        }

        [Fact]
        public void Xor_AllOnes32_ReturnsZero()
        {
            // 0xFFFFFFFF ^ Rotate(0xFFFFFFFF) = 0xFFFFFFFF ^ 0xFFFFFFFF = 0
            Assert.Equal(0u, Lib.Xor(0xFFFFFFFF));
        }

        // Mingle32 interleaves the bits of two 32-bit values into a 64-bit result.
        [Fact]
        public void Mingle32_ZeroWithZero_ReturnsZero()
        {
            Assert.Equal(0UL, Lib.Mingle32(0, 0));
        }

        [Fact]
        public void Mingle32_OneWithZero_ReturnsTwo()
        {
            Assert.Equal(2UL, Lib.Mingle32(1, 0));
        }

        [Fact]
        public void Mingle32_ZeroWithOne_ReturnsOne()
        {
            Assert.Equal(1UL, Lib.Mingle32(0, 1));
        }

        [Fact]
        public void Mingle32_MaxValues_ReturnsAllOnes()
        {
            Assert.Equal(0xFFFFFFFFFFFFFFFFUL, Lib.Mingle32(0xFFFFFFFF, 0xFFFFFFFF));
        }

        [Fact]
        public void Mingle32_ZeroWithMax_ReturnsEvenBits()
        {
            Assert.Equal(0x5555555555555555UL, Lib.Mingle32(0, 0xFFFFFFFF));
        }

        [Fact]
        public void Mingle32_MaxWithZero_ReturnsOddBits()
        {
            Assert.Equal(0xAAAAAAAAAAAAAAAAUL, Lib.Mingle32(0xFFFFFFFF, 0));
        }

        // Select64
        [Fact]
        public void Select64_AllZeros_ReturnsZero()
        {
            Assert.Equal(0UL, Lib.Select(0UL, 0UL));
        }

        [Fact]
        public void Select64_AllOnes_MaskAllOnes_ReturnsSame()
        {
            Assert.Equal(ulong.MaxValue, Lib.Select(ulong.MaxValue, ulong.MaxValue));
        }

        [Fact]
        public void Select64_ExtractsCorrectBits()
        {
            Assert.Equal(2UL, Lib.Select(0b1010UL, 0b1100UL));
        }

        // Rotate64
        [Fact]
        public void Rotate64_One_WrapsToHighBit()
        {
            Assert.Equal(0x8000000000000000UL, Lib.Rotate(1UL));
        }

        [Fact]
        public void Rotate64_Two_ReturnsOne()
        {
            Assert.Equal(1UL, Lib.Rotate(2UL));
        }

        // 64-bit unary ops
        [Fact]
        public void And64_AllOnes_ReturnsAllOnes()
        {
            Assert.Equal(ulong.MaxValue, Lib.UnaryAnd64(ulong.MaxValue));
        }

        [Fact]
        public void Or64_AllOnes_ReturnsAllOnes()
        {
            Assert.Equal(ulong.MaxValue, Lib.UnaryOr64(ulong.MaxValue));
        }

        [Fact]
        public void Xor64_AllOnes_ReturnsZero()
        {
            Assert.Equal(0UL, Lib.UnaryXor64(ulong.MaxValue));
        }

        [Fact]
        public void And_Dispatches_To64Bit_ForLargeValues()
        {
            ulong val = 1UL << 40;
            Assert.Equal(Lib.UnaryAnd64(val), Lib.And(val));
        }

        [Fact]
        public void Or_Dispatches_To64Bit_ForLargeValues()
        {
            ulong val = 1UL << 40;
            Assert.Equal(Lib.UnaryOr64(val), Lib.Or(val));
        }

        [Fact]
        public void Xor_Dispatches_To64Bit_ForLargeValues()
        {
            ulong val = 1UL << 40;
            Assert.Equal(Lib.UnaryXor64(val), Lib.Xor(val));
        }

        [Fact]
        public void Fail_ThrowsIntercalException()
        {
            Assert.Throws<IntercalException>(() => Lib.Fail("test error"));
        }

        [Fact]
        public void Fail_IncludesErrorCode()
        {
            var ex = Assert.Throws<IntercalException>(() => Lib.Fail("E999"));
            Assert.Contains("E999", ex.Message);
        }
    }
}
