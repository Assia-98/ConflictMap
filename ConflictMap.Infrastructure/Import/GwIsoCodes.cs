namespace ConflictMap.Infrastructure.Import;

// Translates Gleditsch-Ward country codes (used by UCDP) to ISO 3166-1 alpha-3 (used by maps)
public static class GwIsoCodes
{
    private static readonly Dictionary<int, string> Map = new()
    {
        // Americas
        [2] = "USA", [20] = "CAN", [40] = "CUB", [41] = "HTI", [42] = "DOM", [51] = "JAM",
        [52] = "TTO", [55] = "GRD", [70] = "MEX", [90] = "GTM", [91] = "HND", [92] = "SLV", [93] = "NIC",
        [94] = "CRI", [95] = "PAN", [100] = "COL", [101] = "VEN", [110] = "GUY", [115] = "SUR",
        [130] = "ECU", [135] = "PER", [140] = "BRA", [145] = "BOL", [150] = "PRY", [155] = "CHL",
        [160] = "ARG", [165] = "URY",
        // Europe
        [200] = "GBR", [205] = "IRL", [210] = "NLD", [211] = "BEL", [212] = "LUX", [220] = "FRA",
        [225] = "CHE", [230] = "ESP", [235] = "PRT", [255] = "DEU", [260] = "DEU", [265] = "DEU",
        [290] = "POL", [305] = "AUT", [310] = "HUN", [315] = "CZE", [316] = "CZE", [317] = "SVK",
        [325] = "ITA", [339] = "ALB", [341] = "MNE", [343] = "MKD", [344] = "HRV", [345] = "SRB",
        [346] = "BIH", [347] = "XKX", [349] = "SVN", [350] = "GRC", [352] = "CYP", [355] = "BGR",
        [359] = "MDA", [360] = "ROU", [365] = "RUS", [366] = "EST", [367] = "LVA", [368] = "LTU",
        [369] = "UKR", [370] = "BLR", [371] = "ARM", [372] = "GEO", [373] = "AZE", [375] = "FIN",
        [380] = "SWE", [385] = "NOR", [390] = "DNK", [395] = "ISL",
        // Africa
        [402] = "CPV", [404] = "GNB", [411] = "GNQ", [420] = "GMB", [432] = "MLI", [433] = "SEN",
        [434] = "BEN", [435] = "MRT", [436] = "NER", [437] = "CIV", [438] = "GIN", [439] = "BFA",
        [450] = "LBR", [451] = "SLE", [452] = "GHA", [461] = "TGO", [471] = "CMR", [475] = "NGA",
        [481] = "GAB", [482] = "CAF", [483] = "TCD", [484] = "COG", [490] = "COD", [500] = "UGA",
        [501] = "KEN", [510] = "TZA", [516] = "BDI", [517] = "RWA", [520] = "SOM", [522] = "DJI",
        [530] = "ETH", [531] = "ERI", [540] = "AGO", [541] = "MOZ", [551] = "ZMB", [552] = "ZWE",
        [553] = "MWI", [560] = "ZAF", [565] = "NAM", [570] = "LSO", [571] = "BWA", [572] = "SWZ",
        [580] = "MDG", [581] = "COM", [590] = "MUS", [600] = "MAR", [615] = "DZA", [616] = "TUN",
        [620] = "LBY", [625] = "SDN", [626] = "SSD",
        // Middle East
        [630] = "IRN", [640] = "TUR", [645] = "IRQ", [651] = "EGY", [652] = "SYR", [660] = "LBN",
        [663] = "JOR", [666] = "ISR", [670] = "SAU", [678] = "YEM", [679] = "YEM", [680] = "YEM",
        [690] = "KWT", [692] = "BHR", [694] = "QAT", [696] = "ARE", [698] = "OMN",
        // Asia and Oceania
        [700] = "AFG", [701] = "TKM", [702] = "TJK", [703] = "KGZ", [704] = "UZB", [705] = "KAZ",
        [710] = "CHN", [712] = "MNG", [713] = "TWN", [731] = "PRK", [732] = "KOR", [740] = "JPN",
        [750] = "IND", [751] = "IND", [760] = "BTN", [770] = "PAK", [771] = "BGD", [775] = "MMR", [780] = "LKA",
        [781] = "MDV", [790] = "NPL", [800] = "THA", [811] = "KHM", [812] = "LAO", [816] = "VNM",
        [817] = "VNM", [820] = "MYS", [830] = "SGP", [835] = "BRN", [840] = "PHL", [850] = "IDN",
        [860] = "TLS", [900] = "AUS", [910] = "PNG", [920] = "NZL", [940] = "SLB", [950] = "FJI"
    };
    // Names for countries that only appear in multi-country conflicts in UCDP
    private static readonly Dictionary<int, string> FallbackNames = new()
    {
        [55] = "Grenada", [91] = "Honduras", [130] = "Ecuador", [210] = "Netherlands",
        [310] = "Hungary", [339] = "Albania", [690] = "Kuwait", [703] = "Kyrgyzstan",
        [713] = "Taiwan", [731] = "North Korea", [732] = "South Korea", [900] = "Australia"
    };

    public static string? FallbackName(int gwCode) =>
        FallbackNames.TryGetValue(gwCode, out var name) ? name : null;

    // Returns the ISO code, or an empty string if the code is unknown
    public static string ToIso(int gwCode) =>
        Map.TryGetValue(gwCode, out var iso) ? iso : string.Empty;
}