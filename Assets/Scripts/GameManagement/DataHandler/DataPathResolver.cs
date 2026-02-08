using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

public static class DataPathResolver
{
    // Tries to read your existing DataPaths constants if present; otherwise uses sane defaults.
    private static string TryGetDataPathsString(string fieldName)
    {
        try
        {
            var t = Type.GetType("DataPaths");
            if (t == null) return null;

            var f = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (f == null) return null;

            return f.GetValue(null) as string;
        }
        catch { return null; }
    }

    public static string RuntimeMapDataPath =>
        TryGetDataPathsString("Runtime_MapDataPath")
        ?? Path.Combine(UnityEngine.Application.persistentDataPath, "MapData");

    public static string EditorMapDataPath =>
        TryGetDataPathsString("Editor_MapDataPath")
        ?? Path.Combine(UnityEngine.Application.dataPath, "SaveData", "MapData");

    public static string RuntimeCharactersPath =>
        TryGetDataPathsString("Runtime_CharactersPath")
        ?? Path.Combine(UnityEngine.Application.persistentDataPath, "Characters");

    public static string EditorCharactersPath =>
        TryGetDataPathsString("Editor_CharactersPath")
        ?? Path.Combine(UnityEngine.Application.dataPath, "SaveData", "Characters");

    // Optional: global catalogs (culture / men-at-arms). These are used by the authoring
    // and editor tooling via WorldDataDirectoryResolver, but can also be used by runtime loaders.
    public static string EditorCulturesPath =>
        TryGetDataPathsString("Editor_CulturesPath")
        ?? Path.Combine(UnityEngine.Application.dataPath, "SaveData", "cultures");

    public static string RuntimeCulturesPath =>
        TryGetDataPathsString("Runtime_CulturesPath")
        ?? Path.Combine(UnityEngine.Application.persistentDataPath, "cultures");

    public static string EditorMenAtArmsPath =>
        TryGetDataPathsString("Editor_MenAtArmsPath")
        ?? Path.Combine(UnityEngine.Application.dataPath, "SaveData", "menatarms");

    public static string RuntimeMenAtArmsPath =>
        TryGetDataPathsString("Runtime_MenAtArmsPath")
        ?? Path.Combine(UnityEngine.Application.persistentDataPath, "menatarms");
}

public static class DualPathJsonLoader
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include
    };

    public static bool TryLoad<T>(string id, string runtimeDir, string editorDir, out T data) where T : class
    {
        data = null;
        if (string.IsNullOrWhiteSpace(id)) return false;

        string file = id.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? id : (id + ".json");
        string runtimePath = Path.Combine(runtimeDir ?? "", file);
        string editorPath = Path.Combine(editorDir ?? "", file);

        string chosen = File.Exists(runtimePath) ? runtimePath : (File.Exists(editorPath) ? editorPath : null);
        if (string.IsNullOrWhiteSpace(chosen)) return false;

        try
        {
            string json = File.ReadAllText(chosen);
            data = JsonConvert.DeserializeObject<T>(json, Settings);
            return data != null;
        }
        catch
        {
            return false;
        }
    }
}
