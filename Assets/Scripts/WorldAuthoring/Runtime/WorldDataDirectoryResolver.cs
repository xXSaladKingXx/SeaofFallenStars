using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Centralized directory resolution for world-data JSON files.
    ///
    /// Goals:
    /// - Prefer your existing DataPathResolver/DataPaths constants when present.
    /// - Provide BOTH Editor (Assets/SaveData/...) and Runtime (persistentDataPath/...) directories.
    /// - Keep simple fallbacks so this package compiles in isolation.
    /// </summary>
    public static class WorldDataDirectoryResolver
    {
        // -------------------- Reflection helpers --------------------

        private static Type FindType(string simpleOrFullName)
        {
            if (string.IsNullOrWhiteSpace(simpleOrFullName)) return null;

            // Type.GetType may return null unless the type is assembly-qualified.
            var t = Type.GetType(simpleOrFullName);
            if (t != null) return t;

            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                var asm = asms[i];
                try
                {
                    var types = asm.GetTypes();
                    for (int j = 0; j < types.Length; j++)
                    {
                        var tt = types[j];
                        if (tt == null) continue;
                        if (string.Equals(tt.Name, simpleOrFullName, StringComparison.Ordinal) ||
                            string.Equals(tt.FullName, simpleOrFullName, StringComparison.Ordinal))
                            return tt;
                    }
                }
                catch
                {
                    // Some assemblies throw in GetTypes(); ignore.
                }
            }

            return null;
        }

        private static string TryReadStaticStringMember(string typeName, string memberName)
        {
            try
            {
                var t = FindType(typeName);
                if (t == null) return null;

                var p = t.GetProperty(memberName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0)
                    return p.GetValue(null, null) as string;

                var f = t.GetField(memberName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(string))
                    return f.GetValue(null) as string;
            }
            catch
            {
                // Ignore.
            }

            return null;
        }

        // -------------------- Editor directories --------------------

        public static string GetEditorMapDataDir()
        {
            return TryReadStaticStringMember("DataPathResolver", "EditorMapDataPath")
                   ?? TryReadStaticStringMember("DataPaths", "Editor_MapDataPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "MapData");
        }

        public static string GetEditorCharactersDir()
        {
            return TryReadStaticStringMember("DataPathResolver", "EditorCharactersPath")
                   ?? TryReadStaticStringMember("DataPaths", "Editor_CharactersPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "Characters");
        }

        public static string GetEditorArmiesDir()
        {
            return TryReadStaticStringMember("DataPathResolver", "EditorArmiesPath")
                   ?? TryReadStaticStringMember("DataPaths", "Editor_ArmiesPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "Armies");
        }

        public static string GetEditorCulturesDir()
        {
            // If you later add DataPaths fields for cultures, they'll be picked up automatically.
            return TryReadStaticStringMember("DataPaths", "Editor_CulturesPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "cultures");
        }

        public static string GetEditorMenAtArmsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_MenAtArmsPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "menatarms");
        }

        // --- New catalog editor directories ---

        /// <summary>
        /// Editor directory for flora catalogs. Returns the configured path if present in
        /// DataPaths (Editor_FloraPath); otherwise defaults to Assets/SaveData/flora.
        /// </summary>
        public static string GetEditorFloraDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_FloraPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "flora");
        }

        /// <summary>
        /// Editor directory for fauna catalogs. Returns the configured path if present in
        /// DataPaths (Editor_FaunaPath); otherwise defaults to Assets/SaveData/fauna.
        /// </summary>
        public static string GetEditorFaunaDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_FaunaPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "fauna");
        }

        /// <summary>
        /// Editor directory for item catalogs. Returns the configured path if present in
        /// DataPaths (Editor_ItemsPath); otherwise defaults to Assets/SaveData/items.
        /// </summary>
        public static string GetEditorItemsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_ItemsPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "items");
        }

        /// <summary>
        /// Editor directory for stat catalogs. Returns the configured path if present in
        /// DataPaths (Editor_StatsPath); otherwise defaults to Assets/SaveData/stats.
        /// </summary>
        public static string GetEditorStatsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_StatsPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "stats");
        }

        // Additional editor directories for new catalog types
        public static string GetEditorTraitsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_TraitsPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "traits");
        }

        public static string GetEditorLanguagesDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_LanguagesPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "languages");
        }

        public static string GetEditorReligionsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_ReligionsPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "religions");
        }

        public static string GetEditorRacesDir()
        {
            return TryReadStaticStringMember("DataPaths", "Editor_RacesPath")
                   ?? Path.Combine(Application.dataPath, "SaveData", "races");
        }

        // -------------------- Runtime directories --------------------

        public static string GetRuntimeMapDataDir()
        {
            return TryReadStaticStringMember("DataPathResolver", "RuntimeMapDataPath")
                   ?? TryReadStaticStringMember("DataPaths", "Runtime_MapDataPath")
                   ?? Path.Combine(Application.persistentDataPath, "MapData");
        }

        public static string GetRuntimeCharactersDir()
        {
            return TryReadStaticStringMember("DataPathResolver", "RuntimeCharactersPath")
                   ?? TryReadStaticStringMember("DataPaths", "Runtime_CharactersPath")
                   ?? Path.Combine(Application.persistentDataPath, "Characters");
        }

        public static string GetRuntimeArmiesDir()
        {
            return TryReadStaticStringMember("DataPathResolver", "RuntimeArmiesPath")
                   ?? TryReadStaticStringMember("DataPaths", "Runtime_ArmiesPath")
                   ?? Path.Combine(Application.persistentDataPath, "Armies");
        }

        public static string GetRuntimeCulturesDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_CulturesPath")
                   ?? Path.Combine(Application.persistentDataPath, "cultures");
        }

        public static string GetRuntimeMenAtArmsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_MenAtArmsPath")
                   ?? Path.Combine(Application.persistentDataPath, "menatarms");
        }

        // --- New catalog runtime directories ---

        /// <summary>
        /// Runtime directory for flora catalogs. Returns the configured path if present in
        /// DataPaths (Runtime_FloraPath); otherwise defaults to persistentDataPath/flora.
        /// </summary>
        public static string GetRuntimeFloraDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_FloraPath")
                   ?? Path.Combine(Application.persistentDataPath, "flora");
        }

        /// <summary>
        /// Runtime directory for fauna catalogs. Returns the configured path if present in
        /// DataPaths (Runtime_FaunaPath); otherwise defaults to persistentDataPath/fauna.
        /// </summary>
        public static string GetRuntimeFaunaDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_FaunaPath")
                   ?? Path.Combine(Application.persistentDataPath, "fauna");
        }

        /// <summary>
        /// Runtime directory for item catalogs. Returns the configured path if present in
        /// DataPaths (Runtime_ItemsPath); otherwise defaults to persistentDataPath/items.
        /// </summary>
        public static string GetRuntimeItemsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_ItemsPath")
                   ?? Path.Combine(Application.persistentDataPath, "items");
        }

        /// <summary>
        /// Runtime directory for stat catalogs. Returns the configured path if present in
        /// DataPaths (Runtime_StatsPath); otherwise defaults to persistentDataPath/stats.
        /// </summary>
        public static string GetRuntimeStatsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_StatsPath")
                   ?? Path.Combine(Application.persistentDataPath, "stats");
        }

        // Additional runtime directories for new catalog types
        public static string GetRuntimeTraitsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_TraitsPath")
                   ?? Path.Combine(Application.persistentDataPath, "traits");
        }

        public static string GetRuntimeLanguagesDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_LanguagesPath")
                   ?? Path.Combine(Application.persistentDataPath, "languages");
        }

        public static string GetRuntimeReligionsDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_ReligionsPath")
                   ?? Path.Combine(Application.persistentDataPath, "religions");
        }

        public static string GetRuntimeRacesDir()
        {
            return TryReadStaticStringMember("DataPaths", "Runtime_RacesPath")
                   ?? Path.Combine(Application.persistentDataPath, "races");
        }

        // -------------------- Category mapping --------------------

        public static string GetEditorDirForCategory(WorldDataCategory category)
        {
            switch (category)
            {
                case WorldDataCategory.Character:
                    return GetEditorCharactersDir();
                case WorldDataCategory.Army:
                    return GetEditorArmiesDir();
                case WorldDataCategory.Settlement:
                case WorldDataCategory.Region:
                case WorldDataCategory.Unpopulated:
                    return GetEditorMapDataDir();
                case WorldDataCategory.Culture:
                    return GetEditorCulturesDir();
                case WorldDataCategory.CultureCatalog:
                    return GetEditorCulturesDir();
                case WorldDataCategory.MenAtArmsCatalog:
                    return GetEditorMenAtArmsDir();
                case WorldDataCategory.TraitCatalog:
                    return GetEditorTraitsDir();
                case WorldDataCategory.LanguageCatalog:
                    return GetEditorLanguagesDir();
                case WorldDataCategory.ReligionCatalog:
                    return GetEditorReligionsDir();
                case WorldDataCategory.RaceCatalog:
                    return GetEditorRacesDir();
                case WorldDataCategory.FloraCatalog:
                    return GetEditorFloraDir();
                case WorldDataCategory.FaunaCatalog:
                    return GetEditorFaunaDir();
                case WorldDataCategory.ItemCatalog:
                    return GetEditorItemsDir();
                case WorldDataCategory.StatCatalog:
                    return GetEditorStatsDir();
                default:
                    return GetEditorMapDataDir();
            }
        }

        public static string GetRuntimeDirForCategory(WorldDataCategory category)
        {
            switch (category)
            {
                case WorldDataCategory.Character:
                    return GetRuntimeCharactersDir();
                case WorldDataCategory.Army:
                    return GetRuntimeArmiesDir();
                case WorldDataCategory.Settlement:
                case WorldDataCategory.Region:
                case WorldDataCategory.Unpopulated:
                    return GetRuntimeMapDataDir();
                case WorldDataCategory.Culture:
                    return GetRuntimeCulturesDir();
                case WorldDataCategory.CultureCatalog:
                    return GetRuntimeCulturesDir();
                case WorldDataCategory.MenAtArmsCatalog:
                    return GetRuntimeMenAtArmsDir();
                case WorldDataCategory.TraitCatalog:
                    return GetRuntimeTraitsDir();
                case WorldDataCategory.LanguageCatalog:
                    return GetRuntimeLanguagesDir();
                case WorldDataCategory.ReligionCatalog:
                    return GetRuntimeReligionsDir();
                case WorldDataCategory.RaceCatalog:
                    return GetRuntimeRacesDir();
                case WorldDataCategory.FloraCatalog:
                    return GetRuntimeFloraDir();
                case WorldDataCategory.FaunaCatalog:
                    return GetRuntimeFaunaDir();
                case WorldDataCategory.ItemCatalog:
                    return GetRuntimeItemsDir();
                case WorldDataCategory.StatCatalog:
                    return GetRuntimeStatsDir();
                default:
                    return GetRuntimeMapDataDir();
            }
        }

        // -------------------- Public aliases (back-compat) --------------------

        /// <summary>Alias: some earlier iterations referenced GetEditorDirectory.</summary>
        public static string GetEditorDirectory(WorldDataCategory category) => GetEditorDirForCategory(category);
        // Back-compat alias used by some callers (e.g., WorldDataLinkedAuthoring)
        public static string GetEditorDir(WorldDataCategory category) => GetEditorDirForCategory(category);
        public static string GetRuntimeDir(WorldDataCategory category) => GetRuntimeDirForCategory(category);

        /// <summary>Alias: some earlier iterations referenced GetRuntimeDirectory.</summary>
        public static string GetRuntimeDirectory(WorldDataCategory category) => GetRuntimeDirForCategory(category);

        public static string EnsureEditorDirectory(WorldDataCategory category)
        {
            string dir = GetEditorDirForCategory(category);
            EnsureDir(dir);
            return dir;
        }

        public static string EnsureRuntimeDirectory(WorldDataCategory category)
        {
            string dir = GetRuntimeDirForCategory(category);
            EnsureDir(dir);
            return dir;
        }

        public static void EnsureDirectory(WorldDataCategory category)
        {
            EnsureEditorDirectory(category);
            EnsureRuntimeDirectory(category);
        }

        private static void EnsureDir(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
