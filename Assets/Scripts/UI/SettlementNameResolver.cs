using System.Collections.Generic;

public static class SettlementNameResolver
{
    private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

    public static string Resolve(string settlementId)
    {
        if (string.IsNullOrWhiteSpace(settlementId))
            return "";

        if (_cache.TryGetValue(settlementId, out var cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var data = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
            DataPaths.Runtime_MapDataPath,
            DataPaths.Editor_MapDataPath,
            settlementId
        );

        string name = (data != null && !string.IsNullOrWhiteSpace(data.displayName))
            ? data.displayName
            : settlementId;

        _cache[settlementId] = name;
        return name;
    }

    public static string[] ResolveMany(string[] settlementIds)
    {
        if (settlementIds == null || settlementIds.Length == 0)
            return System.Array.Empty<string>();

        var result = new string[settlementIds.Length];
        for (int i = 0; i < settlementIds.Length; i++)
            result[i] = Resolve(settlementIds[i]);

        return result;
    }

    public static void ClearCache() => _cache.Clear();
}
