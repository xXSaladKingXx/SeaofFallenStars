using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    public static class WorldDataMapPositionUtil
    {
        private const string MapPositionKey = "mapPosition";
        private const string AltCoordinatesKey = "coordinates";

        private static readonly Dictionary<string, string> CachedPathByKey =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static bool TryLoadMapPosition(WorldDataCategory category, string id, out Vector2 pos)
        {
            pos = Vector2.zero;
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (!TryResolveFilePath(category, id, out string path)) return false;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;

                var j = JObject.Parse(json);
                var mp = j[MapPositionKey] ?? j[AltCoordinatesKey];
                if (mp is JObject o)
                {
                    float x = o.Value<float?>("x") ?? 0f;
                    float y = o.Value<float?>("y") ?? 0f;
                    pos = new Vector2(x, y);
                    return true;
                }
            }
            catch
            {
                // Ignore parse or IO errors.
            }

            return false;
        }

        public static bool TrySaveMapPosition(WorldDataCategory category, string id, Vector2 pos)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (!TryResolveFilePath(category, id, out string path)) return false;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) json = "{}";

                var j = JObject.Parse(json);
                j[MapPositionKey] = new JObject
                {
                    ["x"] = pos.x,
                    ["y"] = pos.y
                };

                string outJson = j.ToString(Formatting.Indented);
                File.WriteAllText(path, outJson);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryResolveFilePath(WorldDataCategory category, string id, out string filePath)
        {
            filePath = null;
            if (string.IsNullOrWhiteSpace(id)) return false;

            string key = category.ToString() + ":" + id;

            if (CachedPathByKey.TryGetValue(key, out string cached) &&
                !string.IsNullOrWhiteSpace(cached) &&
                File.Exists(cached))
            {
                filePath = cached;
                return true;
            }

            string dir = WorldDataDirectoryResolver.GetRuntimeDirectory(category);
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return false;

            // Fast path: filename matches id.
            string direct = Path.Combine(dir, id + ".json");
            if (File.Exists(direct))
            {
                CachedPathByKey[key] = direct;
                filePath = direct;
                return true;
            }

            // Slow path: scan directory and match by id field.
            string idField = GetIdField(category);
            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return false;
            }

            for (int i = 0; i < files.Length; i++)
            {
                string pth = files[i];
                if (string.IsNullOrWhiteSpace(pth)) continue;

                try
                {
                    string json = File.ReadAllText(pth);
                    if (string.IsNullOrWhiteSpace(json)) continue;

                    var j = JObject.Parse(json);
                    string found = j.Value<string>(idField);

                    if (!string.IsNullOrWhiteSpace(found) &&
                        string.Equals(found.Trim(), id.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        CachedPathByKey[key] = pth;
                        filePath = pth;
                        return true;
                    }
                }
                catch
                {
                    // Skip unreadable files.
                }
            }

            return false;
        }

        private static string GetIdField(WorldDataCategory category)
        {
            switch (category)
            {
                case WorldDataCategory.Character: return "characterId";
                case WorldDataCategory.Army: return "armyId";
                case WorldDataCategory.Settlement: return "settlementId";
                case WorldDataCategory.Region: return "regionId";
                case WorldDataCategory.Unpopulated: return "unpopulatedId";
                case WorldDataCategory.Culture: return "cultureId";
                case WorldDataCategory.CultureCatalog: return "catalogId";
                case WorldDataCategory.MenAtArmsCatalog: return "catalogId";
                default: return "id";
            }
        }
    }
}
