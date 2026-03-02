using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Custom inspector for <see cref="WorldDataMasterAuthoring"/>.
    /// Provides simplified controls for creating new assets and loading
    /// existing data.  Non‑catalog categories can be created with an ID
    /// and display name.  Catalog categories are loaded from a single
    /// JSON file.  Existing files can be selected and loaded from
    /// drop‑down lists.  Refresh and clear buttons remain at the top.
    /// </summary>
    [CustomEditor(typeof(WorldDataMasterAuthoring))]
    public sealed class WorldDataMasterAuthoringEditor : Editor
    {
        private WorldDataCategory _newType = WorldDataCategory.Character;
        private string _newId = string.Empty;
        private string _newDisplay = string.Empty;
        private WorldDataCategory _catalogType = WorldDataCategory.CultureCatalog;
        private WorldDataCategory _loadCategory = WorldDataCategory.Character;
        private int _loadSelectedIndex;
        private readonly List<string> _loadOptions = new List<string>();
        private readonly List<string> _loadPaths = new List<string>();

        // Character subtype selection for new characters
        private int _newCharTypeIndex;
        private readonly string[] _charTypeNames = Enum.GetNames(typeof(CharacterType));

        public override void OnInspectorGUI()
        {
            var master = (WorldDataMasterAuthoring)target;
            // Top buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Index"))
            {
                Debug.Log("[WorldDataMasterAuthoringEditor] Refresh Index clicked.");
            }
            if (GUILayout.Button("Clear Active Session"))
            {
                Debug.Log("[WorldDataMasterAuthoringEditor] Clear Active Session clicked.");
                master.ClearActiveSession();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create New", EditorStyles.boldLabel);
            // Non‑catalog dropdown
            var allCats = Enum.GetValues(typeof(WorldDataCategory)).Cast<WorldDataCategory>();
            var nonCatalogs = allCats.Where(c => !c.ToString().EndsWith("Catalog", StringComparison.OrdinalIgnoreCase) && c != WorldDataCategory.Culture).ToArray();
            string[] nonCatNames = nonCatalogs.Select(c => c.ToString()).ToArray();
            int typeIndex = Array.IndexOf(nonCatalogs, _newType);
            int newTypeIndex = EditorGUILayout.Popup("Type", typeIndex >= 0 ? typeIndex : 0, nonCatNames);
            _newType = nonCatalogs[Mathf.Clamp(newTypeIndex, 0, nonCatalogs.Length - 1)];

            // If creating a character, allow selection of a character subtype
            if (_newType == WorldDataCategory.Character)
            {
                int subtypeIndex = Mathf.Clamp(_newCharTypeIndex, 0, _charTypeNames.Length - 1);
                subtypeIndex = EditorGUILayout.Popup("Character Subtype", subtypeIndex, _charTypeNames);
                _newCharTypeIndex = subtypeIndex;
            }
            // ID and display name
            _newId = EditorGUILayout.TextField("ID", _newId);
            _newDisplay = EditorGUILayout.TextField("Display Name", _newDisplay);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear"))
            {
                _newId = string.Empty;
                _newDisplay = string.Empty;
            }
            if (GUILayout.Button("Create & Save"))
            {
                CreateAndSave(master, _newType, _newId, _newDisplay, _newCharTypeIndex);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Load Catalog", EditorStyles.boldLabel);
            var catalogCats = allCats.Where(c => c.ToString().EndsWith("Catalog", StringComparison.OrdinalIgnoreCase)).ToArray();
            string[] catalogNames = catalogCats.Select(c => GetFriendlyCatalogName(c)).ToArray();
            int catIndex = Array.IndexOf(catalogCats, _catalogType);
            int newCatIndex = EditorGUILayout.Popup("Catalog", catIndex >= 0 ? catIndex : 0, catalogNames);
            _catalogType = catalogCats[Mathf.Clamp(newCatIndex, 0, catalogCats.Length - 1)];
            if (GUILayout.Button("Load Catalog"))
            {
                LoadCatalog(master, _catalogType);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Load Existing", EditorStyles.boldLabel);
            string[] allNames = allCats.Select(c => c.ToString()).ToArray();
            var allArray = allCats.ToArray();
            int loadCatIndex = Array.IndexOf(allArray, _loadCategory);
            int newLoadCatIndex = EditorGUILayout.Popup("Category", loadCatIndex >= 0 ? loadCatIndex : 0, allNames);
            WorldDataCategory newCatSel = allArray[Mathf.Clamp(newLoadCatIndex, 0, allArray.Length - 1)];
            if (newCatSel != _loadCategory)
            {
                _loadCategory = newCatSel;
                RefreshLoadList(_loadCategory);
                _loadSelectedIndex = 0;
            }
            if (_loadOptions.Count > 0)
            {
                _loadSelectedIndex = EditorGUILayout.Popup("Entry", _loadSelectedIndex, _loadOptions.ToArray());
                if (GUILayout.Button("Load Selected"))
                {
                    LoadExisting(master, _loadCategory, _loadPaths[_loadSelectedIndex]);
                }
            }
            else
            {
                EditorGUILayout.Popup("Entry", 0, new[] { "(None)" });
            }

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private void CreateAndSave(WorldDataMasterAuthoring master, WorldDataCategory category, string id, string displayName, int charTypeIndex)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
            {
                EditorUtility.DisplayDialog("Missing Fields", "Please specify both an ID and a Display Name.", "OK");
                return;
            }
            var session = master.CreateOrReplaceSession(category);
            if (session == null)
            {
                Debug.LogError($"[WorldDataMasterAuthoringEditor] Unable to create session for {category}");
                return;
            }
            var dataField = session.GetType().GetField("data");
            object data = dataField?.GetValue(session);
            if (data != null)
            {
                SetStringField(data, "characterId", id);
                SetStringField(data, "settlementId", id);
                SetStringField(data, "regionId", id);
                SetStringField(data, "pointId", id);
                SetStringField(data, "displayName", displayName);

                // If creating a character, set its characterType from the selected subtype index
                if (category == WorldDataCategory.Character)
                {
                    var ctField = data.GetType().GetField("characterType");
                    if (ctField != null)
                    {
                        try
                        {
                            var enumValues = Enum.GetValues(typeof(CharacterType));
                            if (charTypeIndex >= 0 && charTypeIndex < enumValues.Length)
                            {
                                ctField.SetValue(data, enumValues.GetValue(charTypeIndex));
                            }
                        }
                        catch { }
                    }
                }
            }
            string json;
            try
            {
                json = session.BuildJson();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldDataMasterAuthoringEditor] Failed to build JSON: {ex.Message}");
                return;
            }
            string dir = GetDefaultDirectoryForCategory(category);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string initialName = session.GetDefaultFileBaseName();
            string path = EditorUtility.SaveFilePanel("Save JSON", dir, initialName + ".json", "json");
            if (string.IsNullOrWhiteSpace(path)) return;
            File.WriteAllText(path, json);
            Debug.Log($"[WorldDataMasterAuthoringEditor] Saved new {category} JSON to {path}");
            SetStringField(session, "loadedFilePath", path);
            SetStringField(session, "timelineFilePath", path);
        }

        private void LoadCatalog(WorldDataMasterAuthoring master, WorldDataCategory cat)
        {
            var session = master.CreateOrReplaceSession(cat);
            if (session == null)
            {
                Debug.LogError($"[WorldDataMasterAuthoringEditor] Could not create session for {cat}");
                return;
            }
            string dir = GetDefaultDirectoryForCategory(cat);
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[WorldDataMasterAuthoringEditor] Directory {dir} does not exist for {cat}");
                return;
            }
            var files = Directory.GetFiles(dir, "*.json");
            if (files.Length == 0)
            {
                Debug.LogWarning($"[WorldDataMasterAuthoringEditor] No JSON files found for {cat} in {dir}");
                return;
            }
            string chosen;
            if (files.Length == 1)
            {
                chosen = files[0];
            }
            else
            {
                int result = EditorUtility.DisplayDialogComplex(
                    "Multiple Catalog Files Found",
                    $"More than one {cat} catalog file exists in {dir}. Choose which to load.",
                    Path.GetFileName(files[0]),
                    Path.GetFileName(files[1]),
                    "Cancel");
                if (result == 0) chosen = files[0];
                else if (result == 1) chosen = files[1];
                else return;
            }
            string json = File.ReadAllText(chosen);
            var apply = session.GetType().GetMethod("ApplyJson", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (apply != null)
            {
                apply.Invoke(session, new object[] { json });
                Debug.Log($"[WorldDataMasterAuthoringEditor] Loaded {cat} catalog from {chosen}");
                SetStringField(session, "loadedFilePath", chosen);
                SetStringField(session, "timelineFilePath", chosen);
            }
            else
            {
                Debug.LogWarning($"[WorldDataMasterAuthoringEditor] Session for {cat} has no ApplyJson method");
            }
        }

        private void RefreshLoadList(WorldDataCategory cat)
        {
            _loadOptions.Clear();
            _loadPaths.Clear();
            string dir = GetDefaultDirectoryForCategory(cat);
            if (!Directory.Exists(dir)) return;
            if (cat.ToString().EndsWith("Catalog", StringComparison.OrdinalIgnoreCase))
            {
                var files = Directory.GetFiles(dir, "*.json");
                if (files.Length == 0) return;
                string json = File.ReadAllText(files[0]);
                try
                {
                    var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var arr = jObj["entries"] as Newtonsoft.Json.Linq.JArray;
                    if (arr != null)
                    {
                        foreach (var e in arr)
                        {
                            string id = e.Value<string>("id");
                            string dn = e.Value<string>("displayName");
                            string label = !string.IsNullOrWhiteSpace(dn) ? dn : id;
                            _loadOptions.Add(label);
                            _loadPaths.Add(files[0] + "|" + id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldDataMasterAuthoringEditor] Failed parsing catalog {cat}: {ex.Message}");
                }
            }
            else
            {
                foreach (var file in Directory.GetFiles(dir, "*.json"))
                {
                    string json = File.ReadAllText(file);
                    try
                    {
                        var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                        string id = jObj.Value<string>("characterId") ?? jObj.Value<string>("settlementId") ?? jObj.Value<string>("regionId") ?? jObj.Value<string>("pointId");
                        string dn = jObj.Value<string>("displayName");
                        string label = !string.IsNullOrWhiteSpace(dn) ? dn : id;
                        if (string.IsNullOrWhiteSpace(label)) label = Path.GetFileNameWithoutExtension(file);
                        _loadOptions.Add(label);
                        _loadPaths.Add(file);
                    }
                    catch
                    {
                        // ignore invalid JSON
                    }
                }
            }
        }

        private void LoadExisting(WorldDataMasterAuthoring master, WorldDataCategory cat, string pathInfo)
        {
            var session = master.CreateOrReplaceSession(cat);
            if (session == null)
            {
                Debug.LogError($"[WorldDataMasterAuthoringEditor] Could not create session for {cat}");
                return;
            }
            string filePath;
            if (cat.ToString().EndsWith("Catalog", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = pathInfo.Split('|');
                filePath = parts[0];
            }
            else
            {
                filePath = pathInfo;
            }
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[WorldDataMasterAuthoringEditor] File not found: {filePath}");
                return;
            }
            string json = File.ReadAllText(filePath);
            var apply = session.GetType().GetMethod("ApplyJson", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (apply != null)
            {
                apply.Invoke(session, new object[] { json });
                Debug.Log($"[WorldDataMasterAuthoringEditor] Loaded {cat} entry from {filePath}");
                SetStringField(session, "loadedFilePath", filePath);
                SetStringField(session, "timelineFilePath", filePath);
            }
            else
            {
                Debug.LogWarning($"[WorldDataMasterAuthoringEditor] Session for {cat} lacks ApplyJson method");
            }
        }

        private static void SetStringField(object obj, string fieldName, string value)
        {
            if (obj == null || string.IsNullOrWhiteSpace(fieldName)) return;
            var t = obj.GetType();
            var field = t.GetField(fieldName);
            if (field != null && field.FieldType == typeof(string)) field.SetValue(obj, value);
            var prop = t.GetProperty(fieldName);
            if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite) prop.SetValue(obj, value);
        }

        private static string GetFriendlyCatalogName(WorldDataCategory cat)
        {
            string name = cat.ToString();
            if (name.EndsWith("Catalog", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - "Catalog".Length);
            }
            return name;
        }

        private static string GetDefaultDirectoryForCategory(WorldDataCategory cat)
        {
            if (cat == WorldDataCategory.Character)
                return DataPaths.Editor_CharactersPath;
            if (cat == WorldDataCategory.Settlement || cat == WorldDataCategory.Region || cat == WorldDataCategory.Unpopulated || cat == WorldDataCategory.Army)
                return DataPaths.Editor_MapDataPath;
            string root = DataPaths.Editor_SaveDataRoot;
            string name = cat.ToString();
            if (name.EndsWith("Catalog", StringComparison.OrdinalIgnoreCase))
            {
                // Strip the Catalog suffix and compute a lowercase folder name.
                string sub = name.Substring(0, name.Length - "Catalog".Length);
                string baseLower = sub.ToLowerInvariant();
                // Try pluralised directory (e.g. cultures, races, religions).  If it
                // exists, use it; otherwise fall back to the singular form.  This
                // logic avoids missing folders like culture vs cultures.
                string plural = baseLower.EndsWith("s") ? baseLower : baseLower + "s";
                string pluralPath = Path.Combine(root, plural);
                if (Directory.Exists(pluralPath))
                    return pluralPath;
                string singularPath = Path.Combine(root, baseLower);
                if (Directory.Exists(singularPath))
                    return singularPath;
                // If neither exists return plural path by default
                return pluralPath;
            }
            return root;
        }
    }
}