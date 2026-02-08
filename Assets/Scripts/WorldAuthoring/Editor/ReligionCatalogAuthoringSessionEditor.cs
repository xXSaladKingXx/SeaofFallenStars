// Custom editor for religion catalogs. Allows editing religion definitions including
// assigning a religious leader (from existing characters) and trait assignments.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(ReligionCatalogAuthoringSession))]
    internal sealed class ReligionCatalogAuthoringSessionEditor : Editor
    {
        private int _addTraitSelection = 0;

        public override void OnInspectorGUI()
        {
            var session = (ReligionCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();
            EditorGUILayout.LabelField("Religion Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            session.data.religions ??= new System.Collections.Generic.List<ReligionEntryModel>();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Religions", EditorStyles.boldLabel);
            // Pre-fetch character entries for leader dropdown
            var charEntries = WorldDataChoicesCache.Get(WorldDataCategory.Character);
            string[] charIds = charEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] charNames = charEntries?.Select(e => !string.IsNullOrWhiteSpace(e.displayName) ? e.displayName : e.id).ToArray() ?? System.Array.Empty<string>();
            // Pre-fetch trait definitions for trait dropdowns
            var traitEntries = WorldDataChoicesCache.GetTraitDefinitions();
            string[] traitIds = traitEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] traitNames = traitEntries?.Select(e => !string.IsNullOrWhiteSpace(e.displayName) ? e.displayName : e.id).ToArray() ?? System.Array.Empty<string>();

            for (int i = 0; i < session.data.religions.Count; i++)
            {
                var rel = session.data.religions[i];
                if (rel == null)
                {
                    rel = new ReligionEntryModel();
                    session.data.religions[i] = rel;
                }
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                rel.id = EditorGUILayout.TextField("ID", rel.id);
                rel.name = EditorGUILayout.TextField("Display Name", rel.name);
                rel.description = EditorGUILayout.TextField("Description", rel.description);
                // Religious leader dropdown
                if (charNames.Length > 0)
                {
                    // Prepend a none option
                    string[] options = new string[charNames.Length + 1];
                    options[0] = "(None)";
                    for (int j = 0; j < charNames.Length; j++) options[j + 1] = charNames[j];
                    int currentIndex = -1;
                    if (!string.IsNullOrWhiteSpace(rel.religiousLeaderCharacterId))
                    {
                        currentIndex = System.Array.IndexOf(charIds, rel.religiousLeaderCharacterId);
                    }
                    int sel = currentIndex >= 0 ? currentIndex + 1 : 0;
                    int chosen = EditorGUILayout.Popup("Religious Leader", sel, options);
                    if (chosen <= 0)
                        rel.religiousLeaderCharacterId = null;
                    else
                        rel.religiousLeaderCharacterId = charIds[chosen - 1];
                }
                else
                {
                    rel.religiousLeaderCharacterId = EditorGUILayout.TextField("Religious Leader ID", rel.religiousLeaderCharacterId);
                }
                // Trait assignments
                rel.traits ??= new System.Collections.Generic.List<string>();
                EditorGUILayout.LabelField("Traits", EditorStyles.miniBoldLabel);
                for (int j = 0; j < rel.traits.Count; j++)
                {
                    string currentId = rel.traits[j];
                    int currentIndex = -1;
                    if (traitIds.Length > 0)
                    {
                        currentIndex = System.Array.IndexOf(traitIds, currentId);
                        if (currentIndex < 0) currentIndex = 0;
                        EditorGUILayout.BeginHorizontal();
                        int newIndex = EditorGUILayout.Popup(string.Empty, currentIndex, traitNames);
                        if (newIndex >= 0 && newIndex < traitIds.Length)
                        {
                            string newId = traitIds[newIndex];
                            if (!string.Equals(newId, currentId, System.StringComparison.Ordinal))
                            {
                                rel.traits[j] = newId;
                                EditorUtility.SetDirty(session);
                            }
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            rel.traits.RemoveAt(j);
                            EditorUtility.SetDirty(session);
                            j--;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        // Fallback to manual entry if no traits available
                        string newVal = EditorGUILayout.TextField(currentId);
                        if (newVal != currentId) rel.traits[j] = newVal;
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            rel.traits.RemoveAt(j);
                            EditorUtility.SetDirty(session);
                            j--;
                        }
                    }
                }
                // Add trait dropdown if there are traits defined. Only adds when the button is clicked.
                if (traitIds.Length > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    _addTraitSelection = EditorGUILayout.Popup("Add Trait", Mathf.Clamp(_addTraitSelection, 0, traitNames.Length - 1), traitNames);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        int idx = Mathf.Clamp(_addTraitSelection, 0, traitIds.Length - 1);
                        string idToAdd = traitIds[idx];
                        if (!rel.traits.Contains(idToAdd))
                        {
                            Undo.RecordObject(session, "Add religion trait");
                            rel.traits.Add(idToAdd);
                            EditorUtility.SetDirty(session);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    string removeId = rel.id;
                    session.data.religions.RemoveAt(i);
                    EditorUtility.SetDirty(session);
                    i--;
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add New Religion"))
            {
                Undo.RecordObject(session, "Add religion");
                var newRel = new ReligionEntryModel
                {
                    id = string.Empty,
                    name = string.Empty,
                    description = string.Empty,
                    religiousLeaderCharacterId = null,
                    traits = new System.Collections.Generic.List<string>()
                };
                session.data.religions.Add(newRel);
                EditorUtility.SetDirty(session);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif