using ConflictMap.Contracts;

namespace ConflictMap.Infrastructure.Import;

// Translates UCDP's text values into our own types
public static class UcdpMapper
{
    // type_of_conflict: "1"-"4"
    public static ConflictType ToConflictType(string value) => value.Trim() switch
    {
        "1" => ConflictType.Extrasystemic,
        "2" => ConflictType.Interstate,
        "3" => ConflictType.Intrastate,
        "4" => ConflictType.InternationalizedIntrastate,
        _ => throw new ArgumentException($"Unknown type_of_conflict: {value}")
    };

    // region can contain several values, e.g. "1, 3". We use the first one.
    public static Region ToRegion(string value) => FirstPart(value) switch
    {
        "1" => Region.Europe,
        "2" => Region.MiddleEast,
        "3" => Region.Asia,
        "4" => Region.Africa,
        "5" => Region.Americas,
        _ => throw new ArgumentException($"Unknown region: {value}")
    };

    // "1997-05-29" -> 1997
    public static int ToYear(string date) => int.Parse(date[..4]);

    // gwno_loc can be "750" or "750, 770" -> [750, 770]
    public static List<int> ToGwCodes(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToList();

    private static string FirstPart(string value) => value.Split(',')[0].Trim();
}