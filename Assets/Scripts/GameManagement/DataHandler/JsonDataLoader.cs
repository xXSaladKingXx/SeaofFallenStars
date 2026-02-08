using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class JsonDataLoader
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public static T TryLoadFromEitherPath<T>(string runtimeFolder, string editorFolder, string baseName) where T : class
    {
        if (string.IsNullOrWhiteSpace(baseName))
            return null;

        string fileName = baseName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? baseName
            : (baseName + ".json");

        // Prefer Assets path in Editor; prefer persistent path in builds.
        string primary = Application.isEditor
            ? Path.Combine(editorFolder, fileName)
            : Path.Combine(runtimeFolder, fileName);

        string secondary = Application.isEditor
            ? Path.Combine(runtimeFolder, fileName)
            : Path.Combine(editorFolder, fileName);

        var a = TryLoadAtPath<T>(primary);
        if (a != null) return a;

        return TryLoadAtPath<T>(secondary);
    }

    public static T TryLoadAtPath<T>(string absolutePath) where T : class
    {
        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            return null;

        try
        {
            string json = File.ReadAllText(absolutePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[JsonDataLoader] Failed to parse JSON '{absolutePath}' as {typeof(T).Name}. {ex.Message}");
            return null;
        }
    }
}
