using System.Text.Json.Serialization;
using ConflictMap.Api.Ai;
using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Data;
using ConflictMap.Infrastructure.DataSources;
using ConflictMap.Infrastructure.Import;
using ConflictMap.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// HttpClient for the UCDP API, with the token from User Secrets
builder.Services.AddHttpClient("Ucdp", client =>
{
    client.BaseAddress = new Uri("https://ucdpapi.pcr.uu.se/api/");
    client.DefaultRequestHeaders.Add("x-ucdp-access-token",
        builder.Configuration["Ucdp:ApiToken"]);
});

// Strategy: use the UCDP API as data source
builder.Services.AddHttpClient<IConflictDataSource, UcdpApiSource>(client =>
{
    client.BaseAddress = new Uri("https://ucdpapi.pcr.uu.se/api/");
    client.DefaultRequestHeaders.Add("x-ucdp-access-token",
        builder.Configuration["Ucdp:ApiToken"]);
});

builder.Services.AddScoped<ConflictImportService>();
// Runs the UCDP import once when the app starts
builder.Services.AddHostedService<UcdpImportBackgroundService>();
builder.Services.AddScoped<IConflictService, ConflictService>();

// NEW: AI with a local model via Ollama.
// UseFunctionInvocation lets the AI call our tools automatically.
builder.Services.AddChatClient(new OllamaApiClient(
        new Uri(builder.Configuration["Ai:Endpoint"]!),
        builder.Configuration["Ai:Model"]!))
    .UseFunctionInvocation();

builder.Services.AddScoped<ConflictTools>();   // NEW: a new toolbox (with empty notes) for every request
builder.Services.AddScoped<ChatService>();     // NEW

// Send enums as text ("Intrastate") instead of numbers, as in the contract
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ---------- API endpoints (from the contract) ----------

// 1. List of conflicts with filters and paging
app.MapGet("/api/conflicts", async (
    IConflictService service,
    CancellationToken ct,
    int? year,
    string? country,
    Region? region,
    ConflictType? type,
    int page = 1,
    int pageSize = 20) =>
{
    if (page < 1 || pageSize < 1 || pageSize > 100)
        return Results.Problem(
            statusCode: 400,
            title: "Ogiltig paginering",
            detail: "page måste vara minst 1 och pageSize mellan 1 och 100.");

    var query = new ConflictQuery(year, country, region, type, page, pageSize);
    return Results.Ok(await service.GetConflictsAsync(query, ct));
});

// 2. One conflict with all its years
app.MapGet("/api/conflicts/{id:int}", async (int id, IConflictService service, CancellationToken ct) =>
{
    var conflict = await service.GetConflictAsync(id, ct);

    return conflict is null
        ? Results.Problem(statusCode: 404, title: "Konflikten hittades inte", detail: $"Ingen konflikt med id {id}.")
        : Results.Ok(conflict);
});

// 3. Stats per country for a given year (used to colour the map)
app.MapGet("/api/countries/stats", async (int year, IConflictService service, CancellationToken ct) =>
    Results.Ok(await service.GetCountryStatsAsync(year, ct)));

// 4. NEW: ask the AI a question about the data
app.MapPost("/api/chat", async (
    ChatRequestDto request,
    ChatService chat,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
        return Results.Problem(statusCode: 400, title: "Frågan saknas", detail: "Skriv en fråga.");

    try
    {
        return Results.Ok(await chat.AskAsync(request, ct));
    }
    catch (Exception ex) when (!ct.IsCancellationRequested)
    {
        logger.LogError(ex, "The AI request failed.");
        return Results.Problem(
            statusCode: 503,
            title: "AI-tjänsten svarar inte",
            detail: "Kontrollera att Ollama körs och att modellen är hämtad.");
    }
});

// ---------- TEMPORARY test endpoints ----------

app.MapGet("/test/ucdp", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("Ucdp");
    var json = await client.GetStringAsync("ucdpprioconflict/26.1?pagesize=1");
    return Results.Content(json, "application/json");
});

app.MapGet("/test/conflicts", async (IConflictDataSource source, CancellationToken ct) =>
{
    var rows = await source.GetConflictRowsAsync(ct);
    return new { Count = rows.Count, First = rows.FirstOrDefault() };
});

app.MapGet("/test/countries", async (AppDbContext db, CancellationToken ct) =>
{
    var total = await db.Countries.CountAsync(ct);
    var missingIso = await db.Countries.Where(c => c.IsoCode == "")
        .Select(c => new { c.GwCode, c.Name }).ToListAsync(ct);
    var missingName = await db.Countries.Where(c => c.Name.StartsWith("GW "))
        .Select(c => new { c.GwCode, c.IsoCode }).ToListAsync(ct);

    return new { Total = total, MissingIso = missingIso, MissingName = missingName };
});

app.Run();