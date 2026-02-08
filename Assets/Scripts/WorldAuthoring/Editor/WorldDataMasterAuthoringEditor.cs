#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(WorldDataMasterAuthoring))]
    internal sealed class WorldDataMasterAuthoringEditor : UnityEditor.Editor
    {
        private WorldDataCategory _createCategory;
        private string _newId;
        private string _newDisplayName;

        private WorldDataCategory _loadCategory;
        private int _loadIndex;

        private bool _showIndex = true;

        private UnityEditor.Editor _cachedSessionEditor;
        private bool _showSessionEditor = true;

        private void OnDisable()
        {
            if (_cachedSessionEditor != null)
            {
                DestroyImmediate(_cachedSessionEditor);
                _cachedSessionEditor = null;
            }
        }

        public override void OnInspectorGUI()
        {
            var master = (WorldDataMasterAuthoring)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World Data Master Author", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Index"))
                    WorldDataChoicesCache.RefreshAll(force: true);

                if (GUILayout.Button("Clear Active Session"))
                    master.ClearActiveSession();
            }

            EditorGUILayout.Space();
            DrawCreateSection(master);

            EditorGUILayout.Space();
            DrawLoadSection(master);

            EditorGUILayout.Space();
            DrawActiveSessionSection(master);

            EditorGUILayout.Space();
            _showIndex = EditorGUILayout.Foldout(_showIndex, "All Data Files (Index)", true);
            if (_showIndex)
            {
                DrawIndexSection();
            }

            // The editing of the active session is handled in DrawActiveSessionSection. Remove duplicate rendering here.
        }

        private void DrawCreateSection(WorldDataMasterAuthoring master)
        {
            EditorGUILayout.LabelField("Create New", EditorStyles.boldLabel);
            // Present a filtered list of categories for creation. Exclude the deprecated Culture
            // category to force use of the CultureCatalog. Include new catalog types for traits,
            // languages, religions and races.
            var createCategories = System.Enum.GetValues(typeof(WorldDataCategory))
                .Cast<WorldDataCategory>()
                .Where(cat => cat != WorldDataCategory.Culture)
                .ToArray();
            // Determine current index; if not found default to first
            int selectedCreateIndex = System.Array.IndexOf(createCategories, _createCategory);
            if (selectedCreateIndex < 0) selectedCreateIndex = 0;
            string[] createLabels = createCategories.Select(c => c.ToString()).ToArray();
            selectedCreateIndex = EditorGUILayout.Popup("Category", selectedCreateIndex, createLabels);
            _createCategory = createCategories[selectedCreateIndex];

            _newId = EditorGUILayout.TextField("Id (optional)", _newId);
            _newDisplayName = EditorGUILayout.TextField("Display Name (optional)", _newDisplayName);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Session"))
                {
                    var session = master.CreateOrReplaceSession(_createCategory);
                    TrySeedNew(session, _newId, _newDisplayName);
                    EditorUtility.SetDirty(master);
                }

                if (GUILayout.Button("Create + Save"))
                {
                    var session = master.CreateOrReplaceSession(_createCategory);
                    TrySeedNew(session, _newId, _newDisplayName);
                    session.SaveNow();
                    EditorUtility.SetDirty(master);
                }
            }
        }

        private void DrawLoadSection(WorldDataMasterAuthoring master)
        {
            EditorGUILayout.LabelField("Load Existing", EditorStyles.boldLabel);

            // Present filtered categories for loading. Exclude deprecated Culture category.
            var loadCategories = System.Enum.GetValues(typeof(WorldDataCategory))
                .Cast<WorldDataCategory>()
                .Where(cat => cat != WorldDataCategory.Culture)
                .ToArray();
            int selectedLoadIndex = System.Array.IndexOf(loadCategories, _loadCategory);
            if (selectedLoadIndex < 0) selectedLoadIndex = 0;
            string[] loadLabels = loadCategories.Select(c => c.ToString()).ToArray();
            selectedLoadIndex = EditorGUILayout.Popup("Category", selectedLoadIndex, loadLabels);
            _loadCategory = loadCategories[selectedLoadIndex];
            var entries = WorldDataChoicesCache.Get(_loadCategory);

            if (entries == null || entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No JSON files found for this category in the expected directory.", MessageType.Info);
                return;
            }

            string[] labels = entries.Select(e => e.ToString()).ToArray();
            _loadIndex = Mathf.Clamp(_loadIndex, 0, labels.Length - 1);
            _loadIndex = EditorGUILayout.Popup("File", _loadIndex, labels);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load"))
                {
                    var session = master.CreateOrReplaceSession(_loadCategory);
                    session.TryLoadFromFile(entries[_loadIndex].filePath);
                    EditorUtility.SetDirty(master);
                }

                if (GUILayout.Button("Ping In Project"))
                {
                    var assetPath = ToAssetPath(entries[_loadIndex].filePath);
                    if (!string.IsNullOrWhiteSpace(assetPath))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        if (obj != null) EditorGUIUtility.PingObject(obj);
                    }
                }
            }
        }

        private void DrawActiveSessionSection(WorldDataMasterAuthoring master)
        {
            var session = master.ActiveSession;

            EditorGUILayout.LabelField("Active Session", EditorStyles.boldLabel);

            if (session == null)
            {
                EditorGUILayout.HelpBox("No active session. Use Create or Load above.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Category", session.Category.ToString());
            EditorGUILayout.LabelField("Loaded File", string.IsNullOrWhiteSpace(session.LoadedFilePath) ? "(not saved yet)" : session.LoadedFilePath);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save"))
                {
                    string p = session.SaveNow();
                    Debug.Log($"[WorldDataMasterAuthoring] Saved: {p}");
                    WorldDataChoicesCache.RefreshAll(force: true);
                }

                if (GUILayout.Button("Save As..."))
                {
                    string dir = session.GetDirectoryEnsured();
                    string defaultName = session.GetDefaultFileBaseName();
                    string chosen = EditorUtility.SaveFilePanel("Save JSON", dir, defaultName, "json");
                    if (!string.IsNullOrWhiteSpace(chosen))
                    {
                        File.WriteAllText(chosen, session.BuildJson());
                        session.SetLoadedFilePath(chosen);
                        WorldDataChoicesCache.RefreshAll(force: true);
                    }
                }

                if (GUILayout.Button("Reload"))
                {
                    if (session.HasLoadedFile)
                    {
                        session.TryLoadFromFile(session.LoadedFilePath);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(session.LastLoadError))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(session.LastLoadError, MessageType.Warning);
            }

            EditorGUILayout.Space();
            _showSessionEditor = EditorGUILayout.Foldout(_showSessionEditor, "Edit Loaded Data", true);
            if (_showSessionEditor)
            {
                // Render the per-session custom editor inline so the master behaves like the specific author tool.
                if (_cachedSessionEditor == null || _cachedSessionEditor.target != session)
                {
                    if (_cachedSessionEditor != null) DestroyImmediate(_cachedSessionEditor);
                    _cachedSessionEditor = UnityEditor.Editor.CreateEditor(session);
                }

                if (_cachedSessionEditor != null)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        _cachedSessionEditor.OnInspectorGUI();
                    }
                }
            }

            // (end Active Session)
        }

        private void DrawIndexSection()
        {
            WorldDataChoicesCache.RefreshAll(false);

            foreach (WorldDataCategory cat in Enum.GetValues(typeof(WorldDataCategory)))
            {
                var entries = WorldDataChoicesCache.Get(cat);
                EditorGUILayout.LabelField($"{cat} ({entries.Count})", EditorStyles.boldLabel);

                int show = Mathf.Min(30, entries.Count);
                for (int i = 0; i < show; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(entries[i].ToString(), GUILayout.MaxWidth(260));
                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        {
                            var ap = ToAssetPath(entries[i].filePath);
                            if (!string.IsNullOrWhiteSpace(ap))
                            {
                                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ap);
                                if (obj != null) EditorGUIUtility.PingObject(obj);
                            }
                        }
                    }
                }

                if (entries.Count > show)
                    EditorGUILayout.LabelField($"... {entries.Count - show} more");

                EditorGUILayout.Space();
            }
        }

        private static void TrySeedNew(WorldDataAuthoringSessionBase session, string id, string displayName)
        {
            if (session == null) return;

            // Best-effort seeding per type; non-invasive.
            if (session is CharacterAuthoringSession ch)
            {
                if (!string.IsNullOrWhiteSpace(id)) ch.data.characterId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) ch.data.displayName = displayName;
                return;
            }

            if (session is SettlementAuthoringSession st)
            {
                if (!string.IsNullOrWhiteSpace(id)) st.data.settlementId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) st.data.displayName = displayName;
                return;
            }

            if (session is RegionAuthoringSession rg)
            {
                if (!string.IsNullOrWhiteSpace(id)) rg.data.regionId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) rg.data.displayName = displayName;
                return;
            }

            if (session is UnpopulatedAuthoringSession up)
            {
                if (!string.IsNullOrWhiteSpace(id)) up.data.areaId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) up.data.displayName = displayName;
                return;
            }

            if (session is ArmyAuthoringSession ar)
            {
                if (!string.IsNullOrWhiteSpace(id)) ar.data.armyId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) ar.data.primaryCommanderDisplayName = displayName;
                return;
            }


            // CultureAuthoringSession is no longer used for editing cultures.  Cultures are
            // defined within the CultureCatalog.  Do not modify individual culture data here.
            if (session is CultureAuthoringSession)
            {
                // Intentionally left blank.  Editing of culture IDs or names is performed via
                // the CultureCatalogAuthoringSession instead.  See case below.
                return;
            }

            // Support editing of the Culture Catalog (catalogId and displayName)
            if (session is CultureCatalogAuthoringSession cat)
            {
                if (!string.IsNullOrWhiteSpace(id)) cat.data.catalogId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) cat.data.displayName = displayName;
                return;
            }

            if (session is MenAtArmsCatalogAuthoringSession ma)
            {
                if (!string.IsNullOrWhiteSpace(id)) ma.data.catalogId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) ma.data.displayName = displayName;
                return;
            }

            // Seed new LanguageCatalogAuthoringSession
            if (session is LanguageCatalogAuthoringSession langCat)
            {
                if (!string.IsNullOrWhiteSpace(id)) langCat.data.catalogId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) langCat.data.displayName = displayName;
                return;
            }

            // Seed new TraitCatalogAuthoringSession
            if (session is TraitCatalogAuthoringSession traitCat)
            {
                if (!string.IsNullOrWhiteSpace(id)) traitCat.data.catalogId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) traitCat.data.displayName = displayName;
                return;
            }

            // Seed new ReligionCatalogAuthoringSession
            if (session is ReligionCatalogAuthoringSession relCat)
            {
                if (!string.IsNullOrWhiteSpace(id)) relCat.data.catalogId = id;
                if (!string.IsNullOrWhiteSpace(displayName)) relCat.data.displayName = displayName;
                return;
            }

        // Seed new RaceCatalogAuthoringSession
        if (session is RaceCatalogAuthoringSession raceCat)
        {
            if (!string.IsNullOrWhiteSpace(id)) raceCat.data.catalogId = id;
            if (!string.IsNullOrWhiteSpace(displayName)) raceCat.data.displayName = displayName;
            // If there are no races defined yet, populate the catalog with a set of
            // default fantasy races. Each race has a simple ID and display name. The
            // description is left blank and traits can be assigned later via the
            // Race Catalog editor. This provides a useful starting point for new
            // projects without requiring manual entry of common races.
            if (raceCat.data.races == null) raceCat.data.races = new System.Collections.Generic.List<RaceEntryModel>();
            if (raceCat.data.races.Count == 0)
            {
                var defaults = new[]
                {
                    new RaceEntryModel { id = "human", displayName = "Human", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "elf", displayName = "Elf", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "dwarf", displayName = "Dwarf", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "halfling", displayName = "Halfling", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "gnome", displayName = "Gnome", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "tiefling", displayName = "Tiefling", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "dragonborn", displayName = "Dragonborn", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "halfelf", displayName = "Half-Elf", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                    new RaceEntryModel { id = "halforc", displayName = "Half-Orc", description = string.Empty, traits = new System.Collections.Generic.List<string>() },
                };
                raceCat.data.races.AddRange(defaults);
            }
            return;
        }
        }

        private static string ToAssetPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;
            fullPath = fullPath.Replace('\\', '/');
            string assets = Application.dataPath.Replace('\\', '/');
            if (!fullPath.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
                return null;
            return "Assets" + fullPath.Substring(assets.Length);
        }
    }
}
#endif
