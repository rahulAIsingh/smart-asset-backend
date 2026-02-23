using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartAssetManager.Api.Data;
using SmartAssetManager.Api.Domain.Entities;
using SmartAssetManager.Api.Models.Compat;

namespace SmartAssetManager.Api.Services;

public class CompatDbService : ICompatDbService
{
    private static readonly string[] AllowedBatchPrefixes =
    [
        "create table",
        "create unique index",
        "insert into",
        "update ",
        "delete from"
    ];

    private static readonly Regex DangerousSqlPattern = new(
        @"(--|/\*|\*/|\b(exec|execute|xp_|sp_|drop\s+table|drop\s+database|truncate\s+table|alter\s+login|create\s+login|grant\s+|revoke\s+|deny\s+|union\s+select|waitfor)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlPattern = new("<[^>]+>", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CompatDbService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<object>> ListAsync(string entity, CompatListRequest request, CancellationToken cancellationToken)
    {
        IEnumerable<object> list = entity.ToLowerInvariant() switch
        {
            "users" => (await _db.Users.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "assets" => (await _db.Assets.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "issuances" => (await _db.Issuances.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "maintenance" => (await _db.Maintenance.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "stocktransactions" => (await _db.StockTransactions.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "categories" => (await _db.Categories.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "departments" => (await _db.Departments.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "vendors" => (await _db.Vendors.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "financeprofiles" => (await _db.FinanceProfiles.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            "financeassetoverrides" => (await _db.FinanceAssetOverrides.AsNoTracking().ToListAsync(cancellationToken)).Cast<object>(),
            _ => throw new InvalidOperationException($"Unsupported entity '{entity}'.")
        };

        if (request.Where is { Count: > 0 })
        {
            list = list.Where(item => MatchesWhere(item, request.Where));
        }

        if (request.OrderBy is { Count: > 0 })
        {
            var first = request.OrderBy.First();
            var isDesc = string.Equals(first.Value, "desc", StringComparison.OrdinalIgnoreCase);
            list = isDesc
                ? list.OrderByDescending(x => GetComparable(x, first.Key))
                : list.OrderBy(x => GetComparable(x, first.Key));
        }

        if (request.Limit.HasValue && request.Limit.Value > 0)
        {
            list = list.Take(request.Limit.Value);
        }

        return list.ToList();
    }

    public async Task<object> CreateAsync(string entity, JsonElement payload, CancellationToken cancellationToken)
    {
        object row = entity.ToLowerInvariant() switch
        {
            "users" => Add(JsonSerializer.Deserialize<UserEntity>(payload.GetRawText(), JsonOptions)!, _db.Users),
            "assets" => Add(NormalizeAndValidateAsset(JsonSerializer.Deserialize<AssetEntity>(payload.GetRawText(), JsonOptions)!), _db.Assets),
            "issuances" => Add(JsonSerializer.Deserialize<IssuanceEntity>(payload.GetRawText(), JsonOptions)!, _db.Issuances),
            "maintenance" => Add(JsonSerializer.Deserialize<MaintenanceEntity>(payload.GetRawText(), JsonOptions)!, _db.Maintenance),
            "stocktransactions" => Add(JsonSerializer.Deserialize<StockTransactionEntity>(payload.GetRawText(), JsonOptions)!, _db.StockTransactions),
            "categories" => Add(JsonSerializer.Deserialize<CategoryEntity>(payload.GetRawText(), JsonOptions)!, _db.Categories),
            "departments" => Add(JsonSerializer.Deserialize<DepartmentEntity>(payload.GetRawText(), JsonOptions)!, _db.Departments),
            "vendors" => Add(JsonSerializer.Deserialize<VendorEntity>(payload.GetRawText(), JsonOptions)!, _db.Vendors),
            "financeprofiles" => Add(JsonSerializer.Deserialize<FinanceProfileEntity>(payload.GetRawText(), JsonOptions)!, _db.FinanceProfiles),
            "financeassetoverrides" => Add(JsonSerializer.Deserialize<FinanceAssetOverrideEntity>(payload.GetRawText(), JsonOptions)!, _db.FinanceAssetOverrides),
            _ => throw new InvalidOperationException($"Unsupported entity '{entity}'.")
        };

        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<object?> UpdateAsync(string entity, string id, JsonElement payload, CancellationToken cancellationToken)
    {
        object? target = entity.ToLowerInvariant() switch
        {
            "users" => await _db.Users.FindAsync([id], cancellationToken),
            "assets" => await _db.Assets.FindAsync([id], cancellationToken),
            "issuances" => await _db.Issuances.FindAsync([id], cancellationToken),
            "maintenance" => await _db.Maintenance.FindAsync([id], cancellationToken),
            "stocktransactions" => await _db.StockTransactions.FindAsync([id], cancellationToken),
            "categories" => await _db.Categories.FindAsync([id], cancellationToken),
            "departments" => await _db.Departments.FindAsync([id], cancellationToken),
            "vendors" => await _db.Vendors.FindAsync([id], cancellationToken),
            "financeprofiles" => await _db.FinanceProfiles.FindAsync([id], cancellationToken),
            "financeassetoverrides" => await _db.FinanceAssetOverrides.FindAsync([id], cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported entity '{entity}'.")
        };

        if (target is null) return null;
        ApplyPatch(target, payload);
        if (target is AssetEntity asset)
        {
            NormalizeAndValidateAsset(asset);
            asset.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return target;
    }

    public async Task<bool> DeleteAsync(string entity, string id, CancellationToken cancellationToken)
    {
        var deleted = entity.ToLowerInvariant() switch
        {
            "users" => await DeleteById(_db.Users, id, cancellationToken),
            "assets" => await DeleteAssetById(id, cancellationToken),
            "issuances" => await DeleteById(_db.Issuances, id, cancellationToken),
            "maintenance" => await DeleteById(_db.Maintenance, id, cancellationToken),
            "stocktransactions" => await DeleteById(_db.StockTransactions, id, cancellationToken),
            "categories" => await DeleteById(_db.Categories, id, cancellationToken),
            "departments" => await DeleteById(_db.Departments, id, cancellationToken),
            "vendors" => await DeleteById(_db.Vendors, id, cancellationToken),
            "financeprofiles" => await DeleteById(_db.FinanceProfiles, id, cancellationToken),
            "financeassetoverrides" => await DeleteById(_db.FinanceAssetOverrides, id, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported entity '{entity}'.")
        };

        if (!deleted) return false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> DeleteAssetById(string id, CancellationToken cancellationToken)
    {
        var asset = await _db.Assets.FindAsync([id], cancellationToken);
        if (asset is null) return false;

        var issuances = await _db.Issuances.Where(x => x.AssetId == id).ToListAsync(cancellationToken);
        var maintenance = await _db.Maintenance.Where(x => x.AssetId == id).ToListAsync(cancellationToken);
        var stockTransactions = await _db.StockTransactions.Where(x => x.AssetId == id).ToListAsync(cancellationToken);
        var financeOverrides = await _db.FinanceAssetOverrides.Where(x => x.AssetId == id).ToListAsync(cancellationToken);

        if (issuances.Count > 0) _db.Issuances.RemoveRange(issuances);
        if (maintenance.Count > 0) _db.Maintenance.RemoveRange(maintenance);
        if (stockTransactions.Count > 0) _db.StockTransactions.RemoveRange(stockTransactions);
        if (financeOverrides.Count > 0) _db.FinanceAssetOverrides.RemoveRange(financeOverrides);

        _db.Assets.Remove(asset);
        return true;
    }

    public async Task<int> ExecuteBatchAsync(CompatBatchRequest request, CancellationToken cancellationToken)
    {
        if (request.Statements.Count == 0) return 0;

        if (!string.Equals(request.Mode, "write", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only write mode is supported for batch execution.");
        }

        var affected = 0;
        foreach (var statement in request.Statements)
        {
            if (string.IsNullOrWhiteSpace(statement.Sql)) continue;

            var sql = statement.Sql.Trim();
            if (!IsAllowedBatchStatement(sql))
            {
                throw new InvalidOperationException("Batch statement blocked by SQL safety policy.");
            }

            affected += await _db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        return affected;
    }

    private static bool IsAllowedBatchStatement(string sql)
    {
        if (DangerousSqlPattern.IsMatch(sql)) return false;

        var normalized = Regex.Replace(sql.Trim(), @"\s+", " ").ToLowerInvariant();
        return AllowedBatchPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static bool MatchesWhere(object item, Dictionary<string, JsonElement> where)
    {
        var type = item.GetType();
        foreach (var pair in where)
        {
            var prop = type.GetProperty(pair.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) return false;
            var left = prop.GetValue(item);
            var right = JsonElementToDotNet(pair.Value, prop.PropertyType);
            if (!object.Equals(left, right)) return false;
        }

        return true;
    }

    private static object? GetComparable(object item, string key)
    {
        var prop = item.GetType().GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item);
    }

    private static T Add<T>(T entity, DbSet<T> dbSet) where T : class
    {
        dbSet.Add(entity);
        return entity;
    }

    private static async Task<bool> DeleteById<T>(DbSet<T> dbSet, string id, CancellationToken cancellationToken) where T : class
    {
        var row = await dbSet.FindAsync([id], cancellationToken);
        if (row is null) return false;
        dbSet.Remove(row);
        return true;
    }

    private static object? JsonElementToDotNet(JsonElement value, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (nullableType == typeof(string)) return value.GetString();
        if (nullableType == typeof(int)) return value.GetInt32();
        if (nullableType == typeof(decimal)) return value.GetDecimal();
        if (nullableType == typeof(double)) return value.GetDouble();
        if (nullableType == typeof(bool)) return value.GetBoolean();
        if (nullableType == typeof(DateTimeOffset)) return value.ValueKind == JsonValueKind.String ? DateTimeOffset.Parse(value.GetString()!) : value.GetDateTimeOffset();
        if (nullableType == typeof(DateOnly)) return DateOnly.Parse(value.GetString()!);
        return JsonSerializer.Deserialize(value.GetRawText(), nullableType, JsonOptions);
    }

    private static void ApplyPatch(object target, JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return;

        foreach (var property in payload.EnumerateObject())
        {
            if (string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase)) continue;

            var propInfo = target.GetType().GetProperty(property.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo is null || !propInfo.CanWrite) continue;

            var nextValue = JsonElementToDotNet(property.Value, propInfo.PropertyType);
            propInfo.SetValue(target, nextValue);
        }
    }

    private static AssetEntity NormalizeAndValidateAsset(AssetEntity asset)
    {
        asset.Name = (asset.Name ?? string.Empty).Trim();
        asset.Category = (asset.Category ?? string.Empty).Trim();
        asset.SerialNumber = NormalizeNullable(asset.SerialNumber);
        asset.DeviceSerialNumber = NormalizeNullable(asset.DeviceSerialNumber);
        asset.Company = NormalizeNullable(asset.Company);
        asset.Model = NormalizeNullable(asset.Model);
        asset.Department = NormalizeNullable(asset.Department);
        asset.WarrantyStart = NormalizeNullable(asset.WarrantyStart);
        asset.WarrantyEnd = NormalizeNullable(asset.WarrantyEnd);
        asset.WarrantyVendor = NormalizeNullable(asset.WarrantyVendor);
        asset.Configuration = NormalizeNullable(asset.Configuration);
        asset.Location = NormalizeNullable(asset.Location);
        asset.Status = string.IsNullOrWhiteSpace(asset.Status) ? "available" : asset.Status.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(asset.Name))
        {
            throw new InvalidOperationException("Asset name is required.");
        }

        if (string.IsNullOrWhiteSpace(asset.Category))
        {
            throw new InvalidOperationException("Asset category is required.");
        }

        var unsafeField = FindUnsafeTextField(asset);
        if (unsafeField is not null)
        {
            throw new InvalidOperationException($"Invalid text in '{unsafeField}'. HTML/script input is not allowed.");
        }

        return asset;
    }

    private static string? FindUnsafeTextField(AssetEntity asset)
    {
        return FindUnsafeTextFieldCore(
            ("name", asset.Name),
            ("category", asset.Category),
            ("serialNumber", asset.SerialNumber),
            ("deviceSerialNumber", asset.DeviceSerialNumber),
            ("company", asset.Company),
            ("model", asset.Model),
            ("department", asset.Department),
            ("warrantyStart", asset.WarrantyStart),
            ("warrantyEnd", asset.WarrantyEnd),
            ("warrantyVendor", asset.WarrantyVendor),
            ("configuration", asset.Configuration),
            ("location", asset.Location),
            ("status", asset.Status)
        );
    }

    private static string? FindUnsafeTextFieldCore(params (string Name, string? Value)[] fields)
    {
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Value)) continue;
            if (HtmlPattern.IsMatch(field.Value))
            {
                return field.Name;
            }
        }

        return null;
    }

    private static string? NormalizeNullable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }
}
