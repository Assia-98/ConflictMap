# Konfliktkartan (ConflictMap)

En webbapp som visar väpnade konflikter i världen 1946–2025 på en interaktiv karta, med data från UCDP (Uppsala Conflict Data Program). Användaren kan filtrera på år, region, konflikttyp och land, öppna en konflikt och se den år för år, och ställa frågor om datan till en AI-chatt. Länderna i AI:ns svar markeras på kartan.

## Innehåll

- [Arkitektur](#arkitektur)
- [Krav](#krav)
- [Kom igång](#kom-igång)
- [Köra testerna](#köra-testerna)
- [Konfiguration](#konfiguration)
- [Design patterns](#design-patterns)
- [Felsökning](#felsökning)
- [Datakälla](#datakälla)

## Arkitektur

Lösningen (`ConflictMap.slnx`) består av fem projekt:

| Projekt | Beskrivning |
|---|---|
| `ConflictMap.Contracts` | Delade DTO:er, enums och `IConflictApiClient`. Både API och Web refererar hit. Se `API-CONTRACT.md`. |
| `ConflictMap.Infrastructure` | EF Core med SQLite, import av UCDP-data (från CSV-filer eller UCDP:s API) och `ConflictService` som läser databasen. |
| `ConflictMap.Api` | ASP.NET Core Minimal API med endpoints för konflikter, landstatistik och AI-chatt (Ollama via Microsoft.Extensions.AI). |
| `ConflictMap.Web` | Blazor Server-frontend med karta (Leaflet via JS-interop), filter, konfliktlista, detaljsida och chatt. |
| `ConflictMap.Tests` | xUnit-tester: unit-tester för tjänster och mappning, och API-tester med `WebApplicationFactory`. |

```
ConflictMap.Web  ──HTTP──▶  ConflictMap.Api  ──▶  ConflictMap.Infrastructure  ──▶  SQLite (conflictmap.db)
       │                          │                         │
       └──────────┬───────────────┘                         └──▶  CSV-filer / UCDP API (import vid första start)
                  ▼                                   Ollama (lokal AI-modell) ◀── /api/chat
       ConflictMap.Contracts
```

## Krav

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Verktyget `dotnet-ef` för att skapa databasen:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- [Ollama](https://ollama.com/) med modellen `qwen2.5`. Behövs bara för AI-chatten, resten av appen fungerar utan.
  ```bash
  ollama pull qwen2.5
  ```

## Kom igång

Alla kommandon körs från rotmappen (där `ConflictMap.slnx` ligger).

### 1. Bygg lösningen

```bash
dotnet restore
dotnet build
```

### 2. Lägg in UCDP-token

Token sparas med User Secrets och hamnar aldrig i Git.

```bash
dotnet user-secrets set "Ucdp:ApiToken" "DIN-TOKEN" --project ConflictMap.Api
```

### 3. Skapa databasen

```bash
dotnet ef database update --project ConflictMap.Infrastructure --startup-project ConflictMap.Api
```

Det skapar `conflictmap.db` i `ConflictMap.Api`.

### 4. Starta Ollama (för AI-chatten)

Se till att Ollama körs på `http://localhost:11434`. Oftast startar den automatiskt efter installation, annars:

```bash
ollama serve
```

### 5. Starta API:t

```bash
dotnet run --project ConflictMap.Api
```

API:t körs på **http://localhost:5240**. Första gången hämtar appen all data från UCDP och sparar den i databasen. Det tar en stund, följ förloppet i loggen ("UCDP import finished: … conflicts imported"). Vid senare starter hoppas importen över.

### 6. Starta webbappen

Öppna en ny terminal:

```bash
dotnet run --project ConflictMap.Web
```

Gå till **http://localhost:5173** i webbläsaren.

> **Utan backend:** Om `Api:BaseUrl` inte är satt (se [Konfiguration](#konfiguration)) använder webbappen `MockConflictApiClient` med påhittad data. Då kan frontend köras helt fristående, utan API, databas, token eller Ollama.

## Köra testerna

```bash
dotnet test
```

Testerna använder en SQLite-databas i minnet och behöver varken UCDP-token, internet eller Ollama.

| Testfil | Testar |
|---|---|
| `unit/ConflictServiceTests.cs` | Filtrering, paginering, detaljer och landstatistik i `ConflictService` |
| `unit/ConflictToolsTests.cs` | AI-verktygen som chatten använder för att hämta data |
| `unit/UcdpMapperTests.cs` | Översättning av UCDP:s värden till egna typer |
| `unit/GwIsoCodesTests.cs` | Översättning från Gleditsch-Ward-koder till ISO-koder |
| `Api/ApiTests.cs` | Endpoints, statuskoder och validering i hela API:t |

## Konfiguration

**ConflictMap.Api** (`appsettings.json` och User Secrets)

| Nyckel | Standard | Beskrivning |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=conflictmap.db` | SQLite-databasen |
| `Ucdp:ApiToken` | *(tom)* | UCDP-token. Sätts med User Secrets, aldrig i `appsettings.json` |
| `Ai:Endpoint` | `http://localhost:11434` | Adress till Ollama |
| `Ai:Model` | `qwen2.5` | Vilken Ollama-modell chatten använder |

**ConflictMap.Web** (`appsettings.Development.json`)

| Nyckel | Standard | Beskrivning |
|---|---|---|
| `Api:BaseUrl` | `http://localhost:5240/` | Adress till API:t. Saknas den används låtsasdata. |

## API

| Metod | Endpoint | Beskrivning |
|---|---|---|
| `GET` | `/api/conflicts?year=&country=&region=&type=&page=&pageSize=` | Konflikter med filter och paginering |
| `GET` | `/api/conflicts/{id}` | En konflikt med alla år |
| `GET` | `/api/countries/stats?year=` | Statistik per land för kartan |
| `POST` | `/api/chat` | Fråga till AI:n, svarar med text och länder att markera |

Fullständigt kontrakt finns i [`API-CONTRACT.md`](API-CONTRACT.md).

## Design patterns

| Mönster | Var | Beskrivning |
|---|---|---|
| **Observer** | `ConflictMap.Web/State/MapState.cs` | `MapState` är subjektet och har eventet `Changed`. `WorldMap`, `ConflictList`, `FilterBar` och `ChatPanel` prenumererar på det och avregistrerar sig i `Dispose`. Komponenterna anropar aldrig varandra, de ändrar bara tillståndet. En `[Flags]`-enum (`StateChange`) talar om vad som ändrades, så att varje komponent bara laddar om det den behöver. |
| **Strategy** | `ConflictMap.Infrastructure/DataSources/IConflictDataSource.cs` | Importen (`ConflictImportService`) beror bara på interfacet `IConflictDataSource`, inte på hur datan hämtas. `UcdpApiSource` hämtar från UCDP:s API. Vilken källa som används bestäms i `Program.cs`. |

## Avancerade C#-koncept

- **Generics:** `PagedResult<T>`, `UcdpPagedResponse<T>`, `GetAllPagesAsync<T>` och `ParseEnum<T> where T : struct, Enum`
- **Asynkron programmering:** `async`/`await` och `CancellationToken` genom hela kedjan, från Blazor-komponenterna till EF Core, samt en `BackgroundService` för importen
- **Delegates, events och lambdas:** `event Action<StateChange>` i `MapState`, LINQ och lambdas i tjänsterna
- **Övrigt:** records, switch expressions, pattern matching, primary constructors och collection expressions

## Felsökning

| Problem | Lösning |
|---|---|
| `SQLite Error 1: 'no such table'` | Databasen är inte skapad. Kör steg 3. |
| "UCDP import failed" i loggen | Kontrollera att UCDP-token är satt (steg 2). Ta bort `conflictmap.db`, kör steg 3 igen och starta om. |
| Chatten svarar "AI-tjänsten svarar inte" | Kontrollera att Ollama körs och att `qwen2.5` är hämtad (`ollama list`). |
| Chatten är långsam | En lokal modell kan ta en stund, särskilt första frågan. Gränsen är 90 sekunder. |
| Kartan är tom | Kontrollera att API:t körs på port 5240 och att importen är klar. |

## Datakälla

- UCDP/PRIO Armed Conflict Dataset, version 26.1
- UCDP Battle-Related Deaths Dataset, version 26.1

Stridsrelaterade dödsfall finns bara från 1989. Konflikter räknas från 25 dödsfall per år, och nivå 2 (krig) betyder minst 1 000 dödsfall under året.
