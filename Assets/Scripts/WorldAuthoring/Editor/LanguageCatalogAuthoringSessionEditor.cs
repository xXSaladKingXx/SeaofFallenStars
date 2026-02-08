// Custom editor for language catalogs. Allows editing language definitions, assigning
// a primary culture from existing cultures, and adding/removing entries.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(LanguageCatalogAuthoringSession))]
    internal sealed class LanguageCatalogAuthoringSessionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var session = (LanguageCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();
            EditorGUILayout.LabelField("Language Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            session.data.languages ??= new System.Collections.Generic.List<LanguageEntryModel>();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Languages", EditorStyles.boldLabel);
            // Retrieve list of existing cultures for primary culture selection
            var cultureEntries = WorldDataChoicesCache.Get(WorldDataCategory.Culture);
            string[] cultureIds = cultureEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] cultureNames = cultureEntries?.Select(e => !string.IsNullOrWhiteSpace(e.displayName) ? e.displayName : e.id).ToArray() ?? System.Array.Empty<string>();

            for (int i = 0; i < session.data.languages.Count; i++)
            {
                var lang = session.data.languages[i];
                if (lang == null)
                {
                    lang = new LanguageEntryModel();
                    session.data.languages[i] = lang;
                }
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                lang.id = EditorGUILayout.TextField("ID", lang.id);
                lang.name = EditorGUILayout.TextField("Display Name", lang.name);
                lang.description = EditorGUILayout.TextField("Description", lang.description);
                // Primary culture dropdown
                int currentIndex = -1;
                if (!string.IsNullOrWhiteSpace(lang.primaryCultureId) && cultureIds.Length > 0)
                {
                    currentIndex = System.Array.IndexOf(cultureIds, lang.primaryCultureId);
                }
                if (cultureNames.Length > 0)
                {
                    // Prepend an empty option to represent no primary culture
                    string[] options = new string[cultureNames.Length + 1];
                    options[0] = "(None)";
                    for (int j = 0; j < cultureNames.Length; j++) options[j + 1] = cultureNames[j];
                    int sel = currentIndex >= 0 ? currentIndex + 1 : 0;
                    int chosen = EditorGUILayout.Popup("Primary Culture", sel, options);
                    if (chosen <= 0)
                    {
                        lang.primaryCultureId = null;
                    }
                    else
                    {
                        lang.primaryCultureId = cultureIds[chosen - 1];
                    }
                }
                else
                {
                    // If no cultures exist, fallback to simple text field
                    lang.primaryCultureId = EditorGUILayout.TextField("Primary Culture ID", lang.primaryCultureId);
                }
                EditorGUI.indentLevel--;
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    session.data.languages.RemoveAt(i);
                    EditorUtility.SetDirty(session);
                    i--;
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add New Language"))
            {
                var newLang = new LanguageEntryModel();
                string baseId = "newLanguage";
                int suffix = 1;
                while (session.data.languages.Exists(l => string.Equals(l?.id, baseId + suffix, System.StringComparison.Ordinal))) suffix++;
                newLang.id = baseId + suffix;
                newLang.name = string.Empty;
                newLang.description = string.Empty;
                newLang.primaryCultureId = null;
                session.data.languages.Add(newLang);
                EditorUtility.SetDirty(session);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif