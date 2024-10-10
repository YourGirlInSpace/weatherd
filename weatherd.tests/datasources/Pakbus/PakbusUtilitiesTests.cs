using FluentAssertions;
using weatherd.datasources.pakbus;
using Xunit;

namespace weatherd.tests.datasources.Pakbus;

public class PakbusUtilitiesTests
{

    [Fact]
    public void PakbusUtilities_ComputeSignature_ValidResultWithTable()
    {
        var data = DecodeHex(
            "53746174757300000000010d0000000000000842617474657279000001015761746368446f67000001014f76657252756e7300000102496e4c6f6373000001025072676d467265650000010353746f72616765000001025461626c6573000001084461797346756c6c00000102486f6c6573000001025072676d536967000001024f53536967000001084f534944000001024f626a53726c4e6f0000010253746e4944000001084c6974684261740000010241647669736553696700000100");

        var result = PakbusUtilities.ComputeSignature(data, data.Length);

        result.Should().Be(38930);
    }
        
    private static byte[] DecodeHex(string hex)
        => Utilities.StringToByteArrayFastest(hex.Replace(" ", ""));
}