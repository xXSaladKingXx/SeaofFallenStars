using System.Collections.Generic;

public static class CharacterNameResolver
{
    private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

    public static string Resolve(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return "";

        if (_cache.TryGetValue(characterId, out var cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var sheet = CharacterDataLoader.TryLoad(characterId);
        string name = sheet != null ? sheet.GetBestDisplayName() : characterId;

        _cache[characterId] = name;
        return name;
    }

    public static void ClearCache() => _cache.Clear();
}
