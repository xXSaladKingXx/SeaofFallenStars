// Custom editor for race catalogs. Allows editing race definitions including
// assigning existing trait definitions to each race.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(RaceCatalogAuthoringSession))]
    internal sealed class RaceCatalogAuthoringSessionEditor : Editor
    {
        private readonly System.Collections.Generic.List<int> _addTraitSelection = new System.Collections.Generic.List<int>();

        public override void OnInspectorGUI()
        {
            var session = (RaceCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }
            serializedObject.Update();
            EditorGUILayout.LabelField("Race Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            session.data.races ??= new System.Collections.Generic.List<RaceEntryModel>();

            // Sync add-trait selection state with race list.
            while (_addTraitSelection.Count < session.data.races.Count) _addTraitSelection.Add(0);
            while (_addTraitSelection.Count > session.data.races.Count) _addTraitSelection.RemoveAt(_addTraitSelection.Count - 1);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Races", EditorStyles.boldLabel);
            // Pre-fetch trait definitions for trait dropdowns
            var traitEntries = WorldDataChoicesCache.GetTraitDefinitions();
            string[] traitIds = traitEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] traitNames = traitEntries?.Select(e => !string.IsNullOrWhiteSpace(e.displayName) ? e.displayName : e.id).ToArray() ?? System.Array.Empty<string>();

            for (int i = 0; i < session.data.races.Count; i++)
            {
                var race = session.data.races[i];
                if (race == null)
                {
                    race = new RaceEntryModel();
                    session.data.races[i] = race;
                }
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                race.id = EditorGUILayout.TextField("ID", race.id);
                race.displayName = EditorGUILayout.TextField("Display Name", race.displayName);
                race.description = EditorGUILayout.TextField("Description", race.description);
                // Trait assignments
                race.traits ??= new System.Collections.Generic.List<string>();
                EditorGUILayout.LabelField("Traits", EditorStyles.miniBoldLabel);
                for (int j = 0; j < race.traits.Count; j++)
                {
                    string currentId = race.traits[j];
                    EditorGUILayout.BeginHorizontal();
                    if (traitIds.Length > 0)
                    {
                        int currentIndex = System.Array.IndexOf(traitIds, currentId);
                        if (currentIndex < 0) currentIndex = 0;
                        int newIndex = EditorGUILayout.Popup(string.Empty, currentIndex, traitNames);
                        if (newIndex >= 0 && newIndex < traitIds.Length)
                        {
                            string newId = traitIds[newIndex];
                            if (!string.Equals(newId, currentId, System.StringComparison.Ordinal))
                            {
                                race.traits[j] = newId;
                                EditorUtility.SetDirty(session);
                            }
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.Popup(string.Empty, 0, new[] { "(no traits available)" });
                        }
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        race.traits.RemoveAt(j);
                        EditorUtility.SetDirty(session);
                        j--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                // Add trait dropdown. Only adds when the button is clicked.
                if (traitIds.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    _addTraitSelection[i] = EditorGUILayout.Popup("Add Trait", Mathf.Clamp(_addTraitSelection[i], 0, traitNames.Length - 1), traitNames);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        int idx = Mathf.Clamp(_addTraitSelection[i], 0, traitIds.Length - 1);
                        string idToAdd = traitIds[idx];
                        if (!race.traits.Contains(idToAdd))
                        {
                            Undo.RecordObject(session, "Add race trait");
                            race.traits.Add(idToAdd);
                            EditorUtility.SetDirty(session);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    session.data.races.RemoveAt(i);
                    EditorUtility.SetDirty(session);
                    i--;
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add New Race"))
            {
                Undo.RecordObject(session, "Add race");
                var newRace = new RaceEntryModel
                {
                    id = string.Empty,
                    displayName = string.Empty,
                    description = string.Empty,
                    traits = new System.Collections.Generic.List<string>()
                };
                session.data.races.Add(newRace);
                _addTraitSelection.Add(0);
                EditorUtility.SetDirty(session);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
