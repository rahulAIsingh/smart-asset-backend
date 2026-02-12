using System.Text.Json;

namespace SmartAssetManager.Api.Models.Compat;

public class CompatListRequest
{
    public Dictionary<string, JsonElement>? Where { get; set; }
    public Dictionary<string, string>? OrderBy { get; set; }
    public int? Limit { get; set; }
}

public class CompatBatchStatement
{
    public string Sql { get; set; } = string.Empty;
}

public class CompatBatchRequest
{
    public List<CompatBatchStatement> Statements { get; set; } = new();
    public string Mode { get; set; } = "write";
}
