using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

public static class CharacterRepository
{
    private static readonly Dictionary<string, string> _displayNameById = new Dictionary<string, string>();
    private static readonly Dictionary<string, List<string>> _charactersByHomeSettlement = new Dictionary<string, List<string>>();
    private static bool _indexed;

    public static void EnsureIndexed()
    {
        if (_indexed) return;

        _displayNameById.Clear();
        _charactersByHomeSettlement.Clear();

        IndexDirectory(DataPathResolver.RuntimeCharactersPath);
        IndexDirectory(DataPathResolver.EditorCharactersPath);

        _indexed = true;
    }

    private static void IndexDirectory(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return;

        foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var jo = JObject.Parse(File.ReadAllText(file));

                string id =
                    (string)jo["characterId"]
                    ?? (string)jo.SelectToken("identity.characterId")
                    ?? Path.GetFileNameWithoutExtension(file);

                string name =
                    (string)jo["displayName"]
                    ?? (string)jo.SelectToken("identity.displayName")
                    ?? id;

                if (!_displayNameById.ContainsKey(id))
                    _displayNameById[id] = name;

                // Accept multiple possible home shapes:
                // - homeSettlementIds: ["camelot", ...]
                // - homes: ["camelot", ...]
                // - feudal.rulesSettlementId: "camelot" (treated as home)
                var homes = new List<string>();

                var homeArr = jo["homeSettlementIds"] as JArray ?? jo["homes"] as JArray;
                if (homeArr != null)
                {
                    foreach (var v in homeArr)
                    {
                        var s = (string)v;
                        if (!string.IsNullOrWhiteSpace(s)) homes.Add(s.Trim());
                    }
                }

                var rules = (string)jo.SelectToken("feudal.rulesSettlementId");
                if (!string.IsNullOrWhiteSpace(rules)) homes.Add(rules.Trim());

                foreach (var h in homes)
                {
                    if (!_charactersByHomeSettlement.TryGetValue(h, out var list))
                    {
                        list = new List<string>();
                        _charactersByHomeSettlement[h] = list;
                    }
                    if (!list.Contains(id))
                        list.Add(id);
                }
            }
            catch
            {
                // Ignore malformed files; do not break indexing.
            }
        }
    }

    public static string ResolveDisplayName(string characterId)
    {
        EnsureIndexed();
        if (string.IsNullOrWhiteSpace(characterId)) return "Unknown";
        return _displayNameById.TryGetValue(characterId.Trim(), out var n) ? n : characterId.Trim();
    }

    public static string[] FindByHomeSettlement(string settlementId)
    {
        EnsureIndexed();
        if (string.IsNullOrWhiteSpace(settlementId)) return System.Array.Empty<string>();

        if (_charactersByHomeSettlement.TryGetValue(settlementId.Trim(), out var list))
            return list.ToArray();

        return System.Array.Empty<string>();
    }
}
