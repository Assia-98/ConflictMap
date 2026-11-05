using ConflictMap.Infrastructure.Import;

namespace ConflictMap.Tests.Unit;

public class GwIsoCodesTests
{
    [Theory]
    [InlineData(2, "USA")]     // United States
    [InlineData(530, "ETH")]   // Ethiopia
    [InlineData(750, "IND")]   // India
    [InlineData(751, "IND")]   // Hyderabad, historical state, now part of India
    [InlineData(260, "DEU")]   // West Germany, shown as today's Germany
    [InlineData(55, "GRD")]    // Grenada, was missing in the first version
    public void ToIso_KnownCode_ReturnsIsoCode(int gwCode, string expected)
    {
        Assert.Equal(expected, GwIsoCodes.ToIso(gwCode));
    }

    [Fact]
    public void ToIso_UnknownCode_ReturnsEmptyString()
    {
        // Unknown codes must not crash the import, just return ""
        Assert.Equal(string.Empty, GwIsoCodes.ToIso(9999));
    }

    [Fact]
    public void FallbackName_CountryOnlyInMultiCountryConflicts_ReturnsName()
    {
        // Grenada only appears together with the USA in UCDP
        Assert.Equal("Grenada", GwIsoCodes.FallbackName(55));
    }

    [Fact]
    public void FallbackName_CountryNotInList_ReturnsNull()
    {
        Assert.Null(GwIsoCodes.FallbackName(750));
    }
}