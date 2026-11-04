using ConflictMap.Contracts;

namespace ConflictMap.Web;

public static class Labels
{
    public static string Region(Region region) => region switch
    {
        Contracts.Region.Europe => "Europa",
        Contracts.Region.MiddleEast => "Mellanöstern",
        Contracts.Region.Asia => "Asien",
        Contracts.Region.Africa => "Afrika",
        Contracts.Region.Americas => "Amerika",
        _ => region.ToString()
    };

    public static string Type(ConflictType type) => type switch
    {
        ConflictType.Extrasystemic => "Kolonial (extrasystemisk)",
        ConflictType.Interstate => "Mellan stater",
        ConflictType.Intrastate => "Inomstatlig",
        ConflictType.InternationalizedIntrastate => "Internationaliserad inomstatlig",
        _ => type.ToString()
    };
}