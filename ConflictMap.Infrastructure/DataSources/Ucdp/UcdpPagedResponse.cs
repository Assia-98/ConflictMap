namespace ConflictMap.Infrastructure.DataSources.Ucdp;

// The outer part of every UCDP response: paging info + a list of rows
public class UcdpPagedResponse<T>
{
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? NextPageUrl { get; set; }
    public List<T> Result { get; set; } = [];
}
