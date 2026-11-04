using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace ConflictMap.Api.Ai;

// Sends the user's question to the AI model, lets it use the tools,
// and returns the answer in the format of the contract (ChatResponseDto).
public class ChatService(IChatClient chatClient, ConflictTools tools, AppDbContext db)
{
    private const string Source =
        "UCDP/PRIO Armed Conflict Dataset + UCDP Battle-Related Deaths Dataset, version 26.1";

    private const string SystemPrompt = """
        You are an assistant in an app that shows armed conflict data from UCDP (Uppsala Conflict Data Program).
        Rules:
        - Always use the tools to get data. Only answer with numbers and facts the tools return.
        - Copy every number EXACTLY as the tool returns it. Never round, change or add digits.
        - Never guess or invent numbers. If the tools return no data, say that the data is missing.
        - The data covers 1946-2025. Battle-related deaths only exist from 1989.
        - Describe what the data shows. Do not take sides in conflicts.
        - Answer in the same language as the question, short and clear.
        - End your answer with: "Källa: UCDP."
        """;

    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken ct = default)
    {
        // If the user has chosen a year in the app, tell the AI about it
        var question = request.Year is null
            ? request.Question
            : $"{request.Question}\n(The year selected in the app is {request.Year}.)";

        List<ChatMessage> messages =
        [
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, question)
        ];

        var options = new ChatOptions
        {
            Temperature = 0,   // NEW: as little "creativity" as possible, copy the data
            Tools =
            [
                AIFunctionFactory.Create(tools.GetConflicts),
                AIFunctionFactory.Create(tools.GetConflictDetails),
                AIFunctionFactory.Create(tools.GetTopCountriesByDeaths)   // CHANGED
            ]
        };

        var response = await chatClient.GetResponseAsync(messages, options, ct);

        var highlight = await GetHighlightCountriesAsync(ct);
        return new ChatResponseDto(response.Text, highlight, Source);
    }

    // Turns the tools' notes into ISO codes for the map
    private async Task<List<string>> GetHighlightCountriesAsync(CancellationToken ct)
    {
        var isoCodes = new HashSet<string>(tools.FetchedIsoCodes);

        if (tools.FetchedConflictIds.Count > 0)
        {
            var ids = tools.FetchedConflictIds.ToList();

            var fromConflicts = await db.Conflicts
                .Where(c => ids.Contains(c.ConflictId))
                .SelectMany(c => c.Countries)
                .Select(country => country.IsoCode)
                .ToListAsync(ct);

            isoCodes.UnionWith(fromConflicts);
        }

        isoCodes.Remove(string.Empty);
        return isoCodes.ToList();
    }
}