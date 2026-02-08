using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class CharacterDataLoader
{
    public static CharacterSheetData TryLoad(string characterId, out JObject raw, out string rawJson)
    {
        raw = null;
        rawJson = null;

        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        string fileName = characterId.Trim() + ".json";

        string pathRuntime = Path.Combine(DataPaths.Runtime_CharactersPath, fileName);
        string pathEditor = Path.Combine(DataPaths.Editor_CharactersPath, fileName);

        string json = null;

        if (File.Exists(pathRuntime))
            json = File.ReadAllText(pathRuntime);
        else if (File.Exists(pathEditor))
            json = File.ReadAllText(pathEditor);

        if (string.IsNullOrWhiteSpace(json))
            return null;

        rawJson = json;

        try { raw = JObject.Parse(json); }
        catch { raw = null; }

        try
        {
            return JsonConvert.DeserializeObject<CharacterSheetData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CharacterDataLoader] Failed to parse '{fileName}': {ex.Message}");
            return null;
        }
    }

    public static CharacterSheetData TryLoad(string characterId)
    {
        return TryLoad(characterId, out _, out _);
    }
}
