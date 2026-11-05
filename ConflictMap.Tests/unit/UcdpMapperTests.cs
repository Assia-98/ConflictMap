using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Import;

namespace ConflictMap.Tests.Unit;

public class UcdpMapperTests
{
    [Theory]
    [InlineData("1", ConflictType.Extrasystemic)]
    [InlineData("2", ConflictType.Interstate)]
    [InlineData("3", ConflictType.Intrastate)]
    [InlineData("4", ConflictType.InternationalizedIntrastate)]
    public void ToConflictType_KnownCode_ReturnsCorrectType(string code, ConflictType expected)
    {
        // Act
        var result = UcdpMapper.ToConflictType(code);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToConflictType_UnknownCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => UcdpMapper.ToConflictType("9"));
    }

    [Fact]
    public void ToRegion_SeveralRegions_UsesTheFirst()
    {
        // Arrange: UCDP can send several regions, e.g. "1, 3"
        var value = "1, 3";

        // Act
        var result = UcdpMapper.ToRegion(value);

        // Assert
        Assert.Equal(Region.Europe, result);
    }

    [Fact]
    public void ToYear_Date_ReturnsYear()
    {
        Assert.Equal(1997, UcdpMapper.ToYear("1997-05-29"));
    }

    [Fact]
    public void ToGwCodes_TwoCountries_ReturnsBothCodes()
    {
        // Arrange: the India-Pakistan example
        var value = "750, 770";

        // Act
        var result = UcdpMapper.ToGwCodes(value);

        // Assert
        Assert.Equal([750, 770], result);
    }
}