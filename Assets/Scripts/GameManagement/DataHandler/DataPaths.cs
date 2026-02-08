using System.IO;
using UnityEngine;

public static class DataPaths
{
    public static string Editor_SaveDataRoot => Path.Combine(Application.dataPath, "SaveData");
    public static string Editor_MapDataPath
    {
        get
        {
            string a = Path.Combine(Editor_SaveDataRoot, "MapData");
            if (Directory.Exists(a)) return a;
            string b = Path.Combine(Editor_SaveDataRoot, "Mapdata");
            if (Directory.Exists(b)) return b;
            return a;
        }
    }
    public static string Editor_CharactersPath => Path.Combine(Editor_SaveDataRoot, "Characters");

    public static string Runtime_SaveDataRoot => Application.persistentDataPath;
    public static string Runtime_MapDataPath
    {
        get
        {
            string a = Path.Combine(Runtime_SaveDataRoot, "MapData");
            if (Directory.Exists(a)) return a;
            string b = Path.Combine(Runtime_SaveDataRoot, "Mapdata");
            if (Directory.Exists(b)) return b;
            return a;
        }
    }
    public static string Runtime_CharactersPath => Path.Combine(Runtime_SaveDataRoot, "Characters");

    // Aliases for existing data directories
    public static string EditorSettlementsDir => Editor_MapDataPath;
    public static string RuntimeSettlementsDir => Runtime_MapDataPath;
    public static string EditorUnpopulatedDir => Editor_MapDataPath;
    public static string RuntimeUnpopulatedDir => Runtime_MapDataPath;

    // NEW: Paths for Army data
    public static string Editor_ArmiesPath => Path.Combine(Editor_SaveDataRoot, "Armies");
    public static string Runtime_ArmiesPath => Path.Combine(Runtime_SaveDataRoot, "Armies");

    // NEW: Global catalogs
    // NOTE: Windows is case-insensitive, but we use lowercase on disk for consistency.
    public static string Editor_CulturesPath => Path.Combine(Editor_SaveDataRoot, "cultures");
    public static string Runtime_CulturesPath => Path.Combine(Runtime_SaveDataRoot, "cultures");

    public static string Editor_MenAtArmsPath => Path.Combine(Editor_SaveDataRoot, "menatarms");
    public static string Runtime_MenAtArmsPath => Path.Combine(Runtime_SaveDataRoot, "menatarms");
}
