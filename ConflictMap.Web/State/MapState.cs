using ConflictMap.Contracts;

namespace ConflictMap.Web.State;

/// <summary>Vad som ändrades, så att varje lyssnare bara gör det den behöver.</summary>
[Flags]
public enum StateChange
{
    None = 0,
    Filters = 1,     // år, region, typ eller land: karta och lista laddar om
    Page = 2,        // bara listans sida: bara listan laddar om
    Highlight = 4,   // markerade länder: bara kartan ritar om
    Chat = 8         // nytt chattsvar
}

/// <summary>
/// OBSERVER: det delade tillståndet är "subjektet".
/// Karta, lista, filter och chatt prenumererar på Changed.
/// Komponenterna anropar aldrig varandra. De ändrar tillståndet,
/// och alla som lyssnar uppdateras automatiskt.
/// </summary>
public sealed class MapState
{
    public const int FirstYear = 1946;
    public const int DefaultYear = 2023;   // justera när A vet senaste året i datan

    public int Year { get; private set; } = DefaultYear;
    public Region? Region { get; private set; }
    public ConflictType? Type { get; private set; }
    public string? CountryIso { get; private set; }
    public int Page { get; private set; } = 1;
    public int PageSize { get; } = 10;

    /// <summary>Länder som AI:n eller ett klick har markerat (ISO alpha-3).</summary>
    public IReadOnlyList<string> HighlightedCountries { get; private set; } = [];

    /// <summary>Senaste svaret från chatten.</summary>
    public ChatResponseDto? LastChatResponse { get; private set; }

    /// <summary>Eventet som lyssnarna prenumererar på.</summary>
    public event Action<StateChange>? Changed;

    /// <summary>Gör om tillståndet till ett filter för GET /api/conflicts.</summary>
    public ConflictQuery ToQuery() => new(Year, CountryIso, Region, Type, Page, PageSize);

    // ---------- Filter ----------

    public void SetYear(int year)
    {
        year = Math.Clamp(year, FirstYear, DateTime.Now.Year);
        if (year == Year) return;
        Year = year;
        Page = 1;
        Notify(StateChange.Filters);
    }

    public void SetRegion(Region? region)
    {
        if (region == Region) return;
        Region = region;
        Page = 1;
        Notify(StateChange.Filters);
    }

    public void SetType(ConflictType? type)
    {
        if (type == Type) return;
        Type = type;
        Page = 1;
        Notify(StateChange.Filters);
    }

    /// <summary>Väljer ett land, t.ex. vid klick på kartan. null tar bort valet.</summary>
    public void SelectCountry(string? iso)
    {
        iso = string.IsNullOrWhiteSpace(iso) ? null : iso.ToUpperInvariant();
        if (iso == CountryIso) return;
        CountryIso = iso;
        HighlightedCountries = iso is null ? [] : [iso];
        Page = 1;
        Notify(StateChange.Filters | StateChange.Highlight);
    }

    public void ResetFilters()
    {
        Year = DefaultYear;
        Region = null;
        Type = null;
        CountryIso = null;
        HighlightedCountries = [];
        Page = 1;
        Notify(StateChange.Filters | StateChange.Highlight);
    }

    // ---------- Lista ----------

    public void SetPage(int page)
    {
        page = Math.Max(1, page);
        if (page == Page) return;
        Page = page;
        Notify(StateChange.Page);
    }

    // ---------- Chatt ----------

    /// <summary>AI:n har svarat: spara svaret och markera länderna på kartan.</summary>
    public void ApplyChatResponse(ChatResponseDto response)
    {
        LastChatResponse = response;
        HighlightedCountries = response.HighlightCountries
            .Select(i => i.ToUpperInvariant())
            .Distinct()
            .ToList();

        var change = StateChange.Chat | StateChange.Highlight;

        // Ta bort landfiltret så att listan inte döljer länder som AI:n nämnde
        if (CountryIso is not null)
        {
            CountryIso = null;
            Page = 1;
            change |= StateChange.Filters;
        }

        Notify(change);
    }

    public void ClearHighlight()
    {
        if (HighlightedCountries.Count == 0) return;
        HighlightedCountries = [];
        Notify(StateChange.Highlight);
    }

    private void Notify(StateChange change) => Changed?.Invoke(change);
}
