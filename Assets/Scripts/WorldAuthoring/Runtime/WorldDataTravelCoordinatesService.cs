using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Runtime helper for reading/writing the non-invasive "mapPosition {x,y}" field that the
    /// authoring sessions add to travelable types (eg Character, Army).
    ///
    /// This does not require your runtime data classes to include a coordinates field, because it
    /// edits the JSON at the JObject level.
    ///
    /// Integration point:
    /// - When Travel Map Mode enables, load the relevant JSON and position your on-map representation.
    /// - When a move is confirmed, write the new position back via the appropriate Save* method.
    /// </summary>
    public static class WorldDataTravelCoordinatesService
    {
        private const string MAP_POS_FIELD = "mapPosition";
        private const string X_FIELD = "x";
        private const string Y_FIELD = "y";

        public static bool TryLoadCharacterMapPosition(string characterId, out Vector2 pos, bool preferRuntimePath = true)
            => TryLoadMapPosition(WorldDataCategory.Character, characterId, out pos, preferRuntimePath);

        public static bool TryLoadArmyMapPosition(string armyId, out Vector2 pos, bool preferRuntimePath = true)
            => TryLoadMapPosition(WorldDataCategory.Army, armyId, out pos, preferRuntimePath);

        public static bool SaveCharacterMapPosition(string characterId, Vector2 pos, bool preferRuntimePath = true)
            => SaveMapPosition(WorldDataCategory.Character, characterId, pos, preferRuntimePath);

        public static bool SaveArmyMapPosition(string armyId, Vector2 pos, bool preferRuntimePath = true)
            => SaveMapPosition(WorldDataCategory.Army, armyId, pos, preferRuntimePath);

        private static bool TryLoadMapPosition(WorldDataCategory category, string fileBaseName, out Vector2 pos, bool preferRuntimePath)
        {
            pos = default;
            if (string.IsNullOrWhiteSpace(fileBaseName)) return false;

            var filePath = ResolveFilePath(category, fileBaseName, preferRuntimePath);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

            try
            {
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return false;

                var root = JObject.Parse(json);
                var mp = root[MAP_POS_FIELD] as JObject;
                if (mp == null) return false;

                float x = mp[X_FIELD] != null ? mp[X_FIELD].Value<float>() : 0f;
                float y = mp[Y_FIELD] != null ? mp[Y_FIELD].Value<float>() : 0f;
                pos = new Vector2(x, y);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"WorldDataTravelCoordinatesService: Failed to read {category} mapPosition for '{fileBaseName}': {e.Message}");
                return false;
            }
        }

        private static bool SaveMapPosition(WorldDataCategory category, string fileBaseName, Vector2 pos, bool preferRuntimePath)
        {
            if (string.IsNullOrWhiteSpace(fileBaseName)) return false;

            var filePath = ResolveFilePath(category, fileBaseName, preferRuntimePath);
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            if (!File.Exists(filePath)) return false;

            try
            {
                var json = File.ReadAllText(filePath);
                var root = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);

                var mp = root[MAP_POS_FIELD] as JObject;
                if (mp == null)
                {
                    mp = new JObject();
                    root[MAP_POS_FIELD] = mp;
                }

                mp[X_FIELD] = pos.x;
                mp[Y_FIELD] = pos.y;

                File.WriteAllText(filePath, root.ToString(Formatting.Indented));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"WorldDataTravelCoordinatesService: Failed to write {category} mapPosition for '{fileBaseName}': {e.Message}");
                return false;
            }
        }

        private static string ResolveFilePath(WorldDataCategory category, string fileBaseName, bool preferRuntimePath)
        {
            // The directory resolver exposes GetRuntimeDirectory/GetEditorDirectory.
            // (Some earlier iterations referenced GetRuntimeDir/GetEditorDir.)
            string dir = preferRuntimePath
                ? WorldDataDirectoryResolver.GetRuntimeDirectory(category)
                : WorldDataDirectoryResolver.GetEditorDirectory(category);

            if (string.IsNullOrWhiteSpace(dir)) return null;
            return Path.Combine(dir, fileBaseName + ".json");
        }
    }
}
