using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ArmyDataLoader
{
    private static readonly Dictionary<string, ArmyInfoData> _cache = new Dictionary<string, ArmyInfoData>();

    public static void ClearCache() => _cache.Clear();

    public static ArmyInfoData TryLoad(string armyId)
    {
        if (string.IsNullOrWhiteSpace(armyId)) return null;

        armyId = NormalizeId(armyId);

        if (_cache.TryGetValue(armyId, out var cached) && cached != null)
            return cached;

        // Decide search priority:
        // - In Editor Play Mode, prefer runtime first (persistentDataPath), then fall back to editor (Assets/SaveData)
        // - In Editor Edit Mode, prefer editor first
        // - In builds, only runtime exists
#if UNITY_EDITOR
        bool playing = Application.isPlaying;
        string firstDir = playing ? DataPaths.Runtime_ArmiesPath : DataPaths.Editor_ArmiesPath;
        string secondDir = playing ? DataPaths.Editor_ArmiesPath : DataPaths.Runtime_ArmiesPath;

        var data = TryLoadFromDir(firstDir, armyId);
        if (data == null)
            data = TryLoadFromDir(secondDir, armyId);
#else
        var data = TryLoadFromDir(DataPaths.Runtime_ArmiesPath, armyId);
#endif

        if (data != null)
            _cache[armyId] = data;

        return data;
    }

    private static ArmyInfoData TryLoadFromDir(string dir, string armyId)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return null;

        string path = ResolveJsonPath(dir, armyId);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<ArmyInfoData>(json);

            // If your ArmyInfoData has an id field, keep it coherent (optional)
            // (Do not hard-rely on it existing.)
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ArmyDataLoader: Failed to load army '{armyId}' from '{path}'. {e.Message}");
            return null;
        }
    }

    private static string ResolveJsonPath(string rootDir, string id)
    {
        // Direct hit
        string direct = Path.Combine(rootDir, $"{id}.json");
        if (File.Exists(direct)) return direct;

        // If files are organized into subfolders, try recursive search
        try
        {
            var hits = Directory.GetFiles(rootDir, $"{id}.json", SearchOption.AllDirectories);
            if (hits != null && hits.Length > 0) return hits[0];
        }
        catch
        {
            // ignore
        }

        return direct; // return the expected path for debugging
    }

    private static string NormalizeId(string id)
    {
        id = id.Trim();
        if (id.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            id = Path.GetFileNameWithoutExtension(id);
        return id;
    }
}
