# Start här – innan ni skriver kod

Gå igenom listan i ordning. Bocka av genom att ändra `[ ]` till `[x]`.

## 1. Skydda hemligheter (FÖRST AV ALLT)

UCDP-token och AI-nyckeln får **aldrig** hamna i Git. En commit med en token ligger kvar i historiken även om den tas bort senare.

- [ ] `.gitignore` skapad i rotmappen innan första commit: `dotnet new gitignore`
- [ ] User Secrets aktiverat i `ConflictMap.Api`: `dotnet user-secrets init`
- [ ] UCDP-token sparad lokalt (båda personerna):
      `dotnet user-secrets set "Ucdp:ApiToken" "ER-TOKEN"`
- [ ] AI-nyckel sparad lokalt (båda personerna):
      `dotnet user-secrets set "Ai:ApiKey" "ER-NYCKEL"`
- [ ] `appsettings.json` har bara tomma platser, inga riktiga värden:

```json
{
  "Ucdp": { "ApiToken": "" },
  "Ai":   { "ApiKey": "" }
}
```

- [ ] Repot är **privat** tills ni har kontrollerat att inga hemligheter finns i historiken

**Om en token har committats:** att ta bort den räcker inte. Be UCDP spärra den och ge er en ny. Byt även AI-nyckeln.

## 2. Sätt upp lösningen

- [ ] Repo på GitHub skapat, partnern inbjuden
- [ ] `main` skyddad: ändringar bara via pull request som den andra godkänner
- [ ] Lösningen skapad med fem projekt:
      `ConflictMap.Contracts`, `ConflictMap.Api`, `ConflictMap.Infrastructure`, `ConflictMap.Web`, `ConflictMap.Tests`
- [ ] `Class1.cs` borttagen ur `ConflictMap.Contracts`
- [ ] `Contracts.cs` inlagd i `ConflictMap.Contracts`
- [ ] `API-CONTRACT.md` och den här filen i rotmappen
- [ ] `ConflictMap.Api` och `ConflictMap.Web` refererar till `ConflictMap.Contracts`
- [ ] Lösningen bygger utan fel, första commit gjord

## 3. Vid publicering (senare)

- [ ] Token och AI-nyckel inlagda som inställningar i värdtjänsten (t.ex. Application Settings i Azure)
- [ ] Om GitHub Actions används: hemligheterna under Settings → Secrets and variables → Actions
- [ ] Kontrollera att inga hemligheter syns i loggar eller felmeddelanden

## Arbetssätt varje dag

- Skapa en ny gren för varje funktion: `git checkout -b feature/karta`
- Små, tydliga commits
- Pull request när funktionen är klar, den andra granskar
- Ändringar i kontraktet görs i både `Contracts.cs` och `API-CONTRACT.md` i samma pull request
