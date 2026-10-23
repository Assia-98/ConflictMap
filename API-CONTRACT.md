# API-kontrakt – Konfliktkartan

Det här är "menyn" mellan backend (Person A) och frontend (Person B).
Båda följer den här filen. Ändringar görs bara via pull request som båda godkänner.

C#-klasserna för alla svar finns i `Contracts.cs`, som ligger i projektet `ConflictMap.Contracts`.
Både `ConflictMap.Api` och `ConflictMap.Web` refererar till det projektet.

---

## 1. Lista med konflikter

```
GET /api/conflicts?year=2020&region=Africa&type=Intrastate&page=1&pageSize=20
```

Alla filter är valfria.

| Parameter | Typ | Exempel | Betydelse |
|---|---|---|---|
| `year` | heltal | `2020` | Bara konflikter aktiva det året |
| `country` | text | `Ethiopia` | Land där konflikten utspelar sig |
| `region` | text | `Africa` | `Europe`, `MiddleEast`, `Asia`, `Africa`, `Americas` |
| `type` | text | `Intrastate` | `Extrasystemic`, `Interstate`, `Intrastate`, `InternationalizedIntrastate` |
| `page` | heltal | `1` | Sidnummer, börjar på 1. Standard: 1 |
| `pageSize` | heltal | `20` | Antal per sida. Standard: 20, max: 100 |

**Svar 200 OK** – `PagedResult<ConflictSummaryDto>`

```json
{
  "items": [
    {
      "conflictId": 11345,
      "location": "Ethiopia",
      "sideA": "Government of Ethiopia",
      "sideB": "TPLF",
      "year": 2020,
      "type": "Intrastate",
      "intensity": 2,
      "battleDeaths": 1200
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 54
}
```

`intensity`: 1 = 25–999 stridsrelaterade dödsfall det året, 2 = 1 000 eller fler (krig).

---

## 2. En enskild konflikt

```
GET /api/conflicts/{id}
```

**Svar 200 OK** – `ConflictDetailDto`

```json
{
  "conflictId": 11345,
  "location": "Ethiopia",
  "sideA": "Government of Ethiopia",
  "sideB": "TPLF",
  "type": "Intrastate",
  "region": "Africa",
  "startYear": 2020,
  "years": [
    { "year": 2020, "intensity": 2, "battleDeathsBest": 1200, "battleDeathsLow": 1100, "battleDeathsHigh": 1500 },
    { "year": 2021, "intensity": 2, "battleDeathsBest": 8000, "battleDeathsLow": 7000, "battleDeathsHigh": 9500 }
  ],
  "source": "UCDP/PRIO Armed Conflict Dataset + UCDP Battle-Related Deaths Dataset, version 25.1"
}
```

Siffrorna ovan är påhittade exempel. `battleDeathsLow` och `battleDeathsHigh` är UCDP:s osäkerhetsintervall.

**Svar 404 Not Found** – om id saknas.

---

## 3. Siffror per land (för kartan)

```
GET /api/countries/stats?year=2020
```

`year` är obligatorisk.

**Svar 200 OK** – lista av `CountryStatDto`

```json
[
  { "isoCode": "ETH", "countryName": "Ethiopia", "year": 2020, "activeConflicts": 2, "battleDeaths": 1350, "maxIntensity": 2 },
  { "isoCode": "NGA", "countryName": "Nigeria", "year": 2020, "activeConflicts": 1, "battleDeaths": 640, "maxIntensity": 1 }
]
```

- Länder utan konflikt det året skickas inte med. Frontend färgar dem som "ingen konflikt".
- `isoCode` är ISO 3166-1 alpha-3 (tre bokstäver). Backend översätter från UCDP:s Gleditsch–Ward-koder.

---

## 4. Fråga till AI:n

```
POST /api/chat
```

**Anrop** – `ChatRequestDto`

```json
{
  "question": "Vilka konflikter fanns i Östafrika 2020?",
  "year": 2020
}
```

`year` är valfri och skickas med om användaren har valt ett år i filtret.

**Svar 200 OK** – `ChatResponseDto`

```json
{
  "answer": "Enligt UCDP-datan fanns tre aktiva konflikter i Östafrika 2020 ...",
  "highlightCountries": ["ETH", "SOM", "SSD"],
  "source": "UCDP Armed Conflict Dataset 25.1"
}
```

- `answer` bygger bara på data som hämtats via verktygen (tool calling). Aldrig gissade siffror.
- `highlightCountries` använder samma ISO-koder som endpoint 3, så kartan kan markera dem direkt.
- Om datan saknas svarar AI:n det i `answer`, och `highlightCountries` är en tom lista.

---

## Fel

Alla fel returneras som standardformatet **ProblemDetails** (inbyggt i ASP.NET Core).

| Kod | När |
|---|---|
| `400 Bad Request` | Ogiltiga parametrar, t.ex. `pageSize` över 100 |
| `404 Not Found` | Konflikten finns inte |
| `500 Internal Server Error` | Oväntat fel i servern |
| `503 Service Unavailable` | AI-tjänsten svarar inte (bara `/api/chat`) |

```json
{
  "status": 404,
  "title": "Konflikten hittades inte",
  "detail": "Ingen konflikt med id 99999."
}
```

---

## Regler

- JSON använder camelCase (standard i ASP.NET Core).
- Alla svar som innehåller UCDP-siffror anger källa och datasetversion.
- UCDP-token ligger i User Secrets lokalt och i appens konfiguration vid publicering. Aldrig i Git.
