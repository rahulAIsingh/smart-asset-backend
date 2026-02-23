namespace SmartAssetManager.Api.Models.Common;

public sealed class ApiEnvelope
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }

    public static ApiEnvelope Ok(object? data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiEnvelope Fail(string message, object? data = null) =>
        new() { Success = false, Message = message, Data = data };
}
