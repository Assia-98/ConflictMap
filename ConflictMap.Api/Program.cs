using ConflictMap.Infrastructure.Data;
using ConflictMap.Infrastructure.DataSources;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// TEMPORARY: test that the UCDP token works. Remove later.
app.MapGet("/test/ucdp", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("Ucdp");
    var json = await client.GetStringAsync("ucdpprioconflict/26.1?pagesize=1");
    return Results.Content(json, "application/json");
});

// TEMPORARY: test that UcdpApiSource fetches all pages. Remove later.
app.MapGet("/test/conflicts", async (IConflictDataSource source, CancellationToken ct) =>
{
    var rows = await source.GetConflictRowsAsync(ct);
    return new { Count = rows.Count, First = rows.FirstOrDefault() };
});

app.Run();
