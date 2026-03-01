#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Custom inspector for <see cref="TerrainCatalogAuthoringSession"/>.  This
    /// editor exposes catalog and entry fields in a structured UI and
    /// provides dropdown lists for selecting native flora and fauna based
    /// on existing catalog definitions.  Water entries are given a
    /// subtype and maximum boat size when the user marks a terrain as
    /// water.  This version avoids passing list indexers by <c>ref</c>
    /// which is illegal in C#, by copying the index to a local variable
    /// before passing it and writing back the result after the call.
    /// </summary>
    [CustomEditor(typeof(TerrainCatalogAuthoringSession))]
    internal sealed class TerrainCatalogAuthoringSessionEditor : Editor
    {
        // UI state: selection indices for per‑entry flora and fauna adds
        private readonly System.Collections.Generic.List<int> _addFloraSelection = new();
        private readonly System.Collections.Generic.List<int> _addFaunaSelection = new();

        public override void OnInspectorGUI()
        {
            var session = (TerrainCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            // Catalog metadata
            EditorGUILayout.LabelField("Terrain Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            session.data.entries ??= new System.Collections.Generic.List<TerrainEntryModel>();

            // Pre‑fetch flora and fauna definitions for dropdown lists
            var floraEntries = WorldDataChoicesCache.GetFloraDefinitions();
            string[] floraIds = floraEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] floraNames = floraEntries?.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray() ?? System.Array.Empty<string>();

            var faunaEntries = WorldDataChoicesCache.GetFaunaDefinitions();
            string[] faunaIds = faunaEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] faunaNames = faunaEntries?.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray() ?? System.Array.Empty<string>();

            // Extend our selection arrays to match the number of entries
            while (_addFloraSelection.Count < session.data.entries.Count) _addFloraSelection.Add(0);
            while (_addFloraSelection.Count > session.data.entries.Count && _addFloraSelection.Count > 0) _addFloraSelection.RemoveAt(_addFloraSelection.Count - 1);
            while (_addFaunaSelection.Count < session.data.entries.Count) _addFaunaSelection.Add(0);
            while (_addFaunaSelection.Count > session.data.entries.Count && _addFaunaSelection.Count > 0) _addFaunaSelection.RemoveAt(_addFaunaSelection.Count - 1);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Terrain Entries", EditorStyles.boldLabel);
            for (int i = 0; i < session.data.entries.Count; i++)
            {
                var entry = session.data.entries[i] ?? (session.data.entries[i] = new TerrainEntryModel());
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;

                entry.id = EditorGUILayout.TextField("ID", entry.id);
                entry.displayName = EditorGUILayout.TextField("Display Name", entry.displayName);

                EditorGUILayout.LabelField("Description");
                entry.description = EditorGUILayout.TextArea(entry.description ?? string.Empty, GUILayout.MinHeight(40));

                entry.movementModifier = EditorGUILayout.FloatField("Movement Modifier", entry.movementModifier);

                // Toggle whether this entry represents water
                bool wasWater = entry.isWater;
                entry.isWater = EditorGUILayout.Toggle("Is Water", entry.isWater);
                if (entry.isWater && !wasWater)
                {
                    // When switching from land to water pick a sensible default
                    entry.waterSubtype = string.IsNullOrWhiteSpace(entry.waterSubtype) ? "Ocean" : entry.waterSubtype;
                    entry.maxBoatSize = TerrainSubtypeDefaultMax(entry.waterSubtype);
                }

                if (entry.isWater)
                {
                    // Water subtype dropdown
                    string[] subTypes = { "Ocean", "Sea", "Bay", "Lake", "River", "Rapids", "Stream" };
                    int subIndex = System.Array.IndexOf(subTypes, entry.waterSubtype);
                    if (subIndex < 0) subIndex = 0;
                    int newSubIndex = EditorGUILayout.Popup("Water Subtype", subIndex, subTypes);
                    if (newSubIndex != subIndex)
                    {
                        string newSubtype = subTypes[newSubIndex];
                        entry.waterSubtype = newSubtype;
                        // Optionally reset boat size to subtype default
                        entry.maxBoatSize = TerrainSubtypeDefaultMax(newSubtype);
                    }

                    entry.maxBoatSize = EditorGUILayout.IntSlider("Max Boat Size", entry.maxBoatSize, 1, 6);
                }

                // Native flora list
                entry.nativeFlora ??= new System.Collections.Generic.List<string>();
                int floraSel = _addFloraSelection[i];
                DrawIdList("Native Flora", entry.nativeFlora, floraIds, floraNames, ref floraSel);
                _addFloraSelection[i] = floraSel;

                // Native fauna list
                entry.nativeFauna ??= new System.Collections.Generic.List<string>();
                int faunaSel = _addFaunaSelection[i];
                DrawIdList("Native Fauna", entry.nativeFauna, faunaIds, faunaNames, ref faunaSel);
                _addFaunaSelection[i] = faunaSel;

                EditorGUI.indentLevel--;

                if (GUILayout.Button("Remove Terrain Entry"))
                {
                    session.data.entries.RemoveAt(i);
                    _addFloraSelection.RemoveAt(i);
                    _addFaunaSelection.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add New Terrain"))
            {
                session.data.entries.Add(new TerrainEntryModel
                {
                    id = NextId(session.data.entries.Select(e => e?.id), "terrain"),
                    displayName = string.Empty,
                    movementModifier = 1f
                });
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Returns the default maximum boat size for a given water subtype.
        /// These defaults are hard coded but can be adjusted to suit
        /// gameplay.  Unknown subtypes return 4 (mid size).
        /// </summary>
        private static int TerrainSubtypeDefaultMax(string subType)
        {
            return subType switch
            {
                "Ocean" => 6,
                "Sea" => 5,
                "Bay" => 4,
                "Lake" => 4,
                "River" => 3,
                "Rapids" => 2,
                "Stream" => 1,
                _ => 4,
            };
        }

        /// <summary>
        /// Draws a list of string identifiers with add/remove UI and an
        /// optional dropdown when definitions are available.  This helper is
        /// adapted from the FloraCatalog editor to work generically.
        /// </summary>
        private static void DrawIdList(string label, System.Collections.Generic.List<string> ids, string[] optionIds, string[] optionLabels, ref int addSelection)
        {
            if (ids == null) return;
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            if (optionIds == null || optionIds.Length == 0 || optionLabels == null || optionLabels.Length == 0)
            {
                // Fallback to free text editing when no definitions exist
                for (int i = 0; i < ids.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        ids[i] = EditorGUILayout.TextField(ids[i] ?? string.Empty);
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            ids.RemoveAt(i);
                            i--;
                        }
                    }
                }
                if (GUILayout.Button("Add " + label, GUILayout.Width(120)))
                    ids.Add(string.Empty);
                return;
            }
            // Draw existing entries with dropdown
            for (int i = 0; i < ids.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string current = ids[i] ?? string.Empty;
                    int curIndex = System.Array.IndexOf(optionIds, current);
                    if (curIndex < 0) curIndex = 0;
                    int next = EditorGUILayout.Popup(curIndex, optionLabels);
                    if (next >= 0 && next < optionIds.Length)
                        ids[i] = optionIds[next];
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        ids.RemoveAt(i);
                        i--;
                    }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                addSelection = EditorGUILayout.Popup("Add", Mathf.Clamp(addSelection, 0, optionLabels.Length - 1), optionLabels);
                if (GUILayout.Button("Add", GUILayout.Width(70)))
                {
                    string toAdd = optionIds[Mathf.Clamp(addSelection, 0, optionIds.Length - 1)];
                    if (!string.IsNullOrEmpty(toAdd) && !ids.Contains(toAdd))
                        ids.Add(toAdd);
                }
            }
        }

        /// <summary>
        /// Generates a unique identifier by appending an incrementing
        /// integer suffix to a base string.  Existing identifiers are
        /// checked case‑insensitively.
        /// </summary>
        private static string NextId(System.Collections.Generic.IEnumerable<string> existing, string baseId)
        {
            int suffix = 1;
            var set = new System.Collections.Generic.HashSet<string>(existing?.Where(x => !string.IsNullOrWhiteSpace(x)) ?? System.Array.Empty<string>(), System.StringComparer.OrdinalIgnoreCase);
            while (set.Contains(baseId + suffix)) suffix++;
            return baseId + suffix;
        }
    }
}
#endif