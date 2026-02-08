// Custom editor for trait catalogs. Allows editing trait definitions and
// adding/removing entries. Each trait includes an ID, display name,
// description, a bonus amount and the stat it applies to.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(TraitCatalogAuthoringSession))]
    internal sealed class TraitCatalogAuthoringSessionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var session = (TraitCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();
            EditorGUILayout.LabelField("Trait Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            session.data.traits ??= new System.Collections.Generic.List<TraitEntryModel>();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Traits", EditorStyles.boldLabel);
            for (int i = 0; i < session.data.traits.Count; i++)
            {
                var trait = session.data.traits[i];
                if (trait == null)
                {
                    trait = new TraitEntryModel();
                    session.data.traits[i] = trait;
                }
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                trait.id = EditorGUILayout.TextField("ID", trait.id);
                trait.name = EditorGUILayout.TextField("Display Name", trait.name);
                trait.description = EditorGUILayout.TextField("Description", trait.description);
                trait.bonus = EditorGUILayout.IntField("Bonus", trait.bonus);
                trait.stat = EditorGUILayout.TextField("Stat", trait.stat);
                EditorGUI.indentLevel--;
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    string removeId = trait.id;
                    session.data.traits.RemoveAt(i);
                    // Remove references from race or religion catalogs is handled elsewhere
                    EditorUtility.SetDirty(session);
                    i--;
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add New Trait"))
            {
                Undo.RecordObject(session, "Add trait");
                var newTrait = new TraitEntryModel
                {
                    id = string.Empty,
                    name = string.Empty,
                    description = string.Empty,
                    bonus = 0,
                    stat = string.Empty
                };
                session.data.traits.Add(newTrait);
                EditorUtility.SetDirty(session);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif