using System.Text.Json;
using SmartAssetManager.Api.Models.Compat;

namespace SmartAssetManager.Api.Services;

public interface ICompatDbService
{
    Task<IReadOnlyList<object>> ListAsync(string entity, CompatListRequest request, CancellationToken cancellationToken);
    Task<object> CreateAsync(string entity, JsonElement payload, CancellationToken cancellationToken);
    Task<object?> UpdateAsync(string entity, string id, JsonElement payload, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string entity, string id, CancellationToken cancellationToken);
    Task<int> ExecuteBatchAsync(CompatBatchRequest request, CancellationToken cancellationToken);
}
