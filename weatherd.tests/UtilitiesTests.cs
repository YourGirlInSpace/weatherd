using FluentAssertions;
using Xunit;

namespace weatherd.tests
{
    public class UtilitiesTests
    {
        [Fact]
        public void IntBitsToFloat_WithInputs_ShouldEmitValidData()
        {
            //Utilities.IntBitsToFloat(0x44DCA875).Should().BeApproximately(1765.2643f, 1);
            Utilities.IntBitsToFloat(0x4680267C).Should().BeApproximately(32.037582f, 1);
            Utilities.IntBitsToFloat(0x4194922C).Should().BeApproximately(1.1607108f, 1);
            Utilities.IntBitsToFloat(0x3faa5812).Should().BeApproximately(0.3327032f, 1);
        }

        [Fact]
        public void USec_To_NSec()
        {
            ulong data = 0x5DC3C2CBBB1C;

            ulong seconds = data / 100000;

        }
    }
}
