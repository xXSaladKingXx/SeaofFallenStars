#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Fallback inspector for any WorldDataAuthoringSession that does not have a bespoke editor.
    /// Enforces "reference-by-id" fields to be selected from the existing catalog/index entries via dropdowns.
    /// </summary>
    [CustomEditor(typeof(WorldDataAuthoringSessionBase), true)]
    public sealed class WorldDataAuthoringSessionSmartEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawToolbar();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator, true);
                    continue;
                }

                if (iterator.propertyType == SerializedPropertyType.String)
                {
                    if (WorldDataReferenceDropdowns.TryDrawStringReference(iterator))
                        continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Choices", GUILayout.Width(130)))
                {
                    WorldDataChoicesCache.RefreshAll(true);
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(6);
        }
    }

    internal static class WorldDataReferenceDropdowns
    {
        public static bool TryDrawStringReference(SerializedProperty prop)
        {
            if (prop == null || prop.propertyType != SerializedPropertyType.String)
                return false;

            // Do not convert the primary key field "id" into a dropdown.
            if (string.Equals(prop.name, "id", StringComparison.OrdinalIgnoreCase))
                return false;

            IReadOnlyList<WorldDataIndexEntry> choices = ResolveChoices(prop);

            if (choices == null)
                return false;

            DrawPopup(prop, prop.displayName, choices);
            return true;
        }

        private static IReadOnlyList<WorldDataIndexEntry> ResolveChoices(SerializedProperty prop)
        {
            string path = (prop.propertyPath ?? string.Empty).ToLowerInvariant();
            string name = (prop.name ?? string.Empty).ToLowerInvariant();

            // For list/array elements (e.g., ".traits.Array.data[0]"), the leaf name is "data[0]".
            // In that case, use the parent array field name to infer intent.
            string parentArray = GetParentArrayName(prop.propertyPath);
            string token = ChooseToken(name, parentArray, path);

            // Terrain
            if (token.Contains("terrain") || token.Contains("habitat") || token.Contains("subtype"))
                return WorldDataChoicesCache.GetTerrainDefinitions();

            // Flora / Fauna / Items
            if (token.Contains("flora"))
                return WorldDataChoicesCache.GetFloraDefinitions();
            if (token.Contains("fauna"))
                return WorldDataChoicesCache.GetFaunaDefinitions();
            if (token.EndsWith("itemid") || token == "itemid" || (token.Contains("item") && token.EndsWith("id")))
                return WorldDataChoicesCache.GetItemDefinitions();

            // Men-at-arms
            if (token.Contains("menatarms") || token.Contains("men-at-arms") || token.Contains("men_at_arms"))
                return WorldDataChoicesCache.GetMenAtArmsEntries();

            // Traits (avoid CharacterSheet personality traits, etc.)
            if (token == "traits" || token.Contains("trait"))
            {
                if (!path.Contains("personality") && !path.Contains("ideal") && !path.Contains("bond") && !path.Contains("flaw"))
                    return WorldDataChoicesCache.GetTraitDefinitions();
            }

            // Languages / Religions / Races / Cultures
            if (token == "languages" || token.Contains("language"))
                return WorldDataChoicesCache.GetLanguageDefinitions();
            if (token == "religions" || token.Contains("religion"))
                return WorldDataChoicesCache.GetReligionDefinitions();
            if (token == "races" || token.Contains("race"))
                return WorldDataChoicesCache.GetRaceDefinitions();
            if (token == "cultures" || token.Contains("culture"))
                return WorldDataChoicesCache.GetCultures();

            // World data (map points & such)
            if (token.Contains("settlement"))
                return WorldDataChoicesCache.GetSettlements();
            if (token.Contains("region"))
                return WorldDataChoicesCache.Get(WorldDataCategory.Region);
            if (token.Contains("army"))
                return WorldDataChoicesCache.GetArmies();
            if (token.Contains("character"))
                return WorldDataChoicesCache.GetCharacters();

            return null;
        }

        private static string ChooseToken(string leafName, string parentArray, string pathLower)
        {
            if (!string.IsNullOrEmpty(leafName) && !leafName.StartsWith("data[", StringComparison.Ordinal))
                return leafName;

            if (!string.IsNullOrEmpty(parentArray))
                return parentArray.ToLowerInvariant();

            // Fallback: try to infer from well-known path segments for element values.
            if (pathLower.Contains(".traits."))
                return "traits";
            if (pathLower.Contains(".languages."))
                return "languages";
            if (pathLower.Contains(".religions."))
                return "religions";
            if (pathLower.Contains(".habitats."))
                return "habitats";

            return leafName ?? string.Empty;
        }

        private static string GetParentArrayName(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return null;

            const string marker = ".Array.data[";
            int idx = propertyPath.LastIndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
                return null;

            string before = propertyPath.Substring(0, idx);
            int dot = before.LastIndexOf('.');
            return dot >= 0 ? before.Substring(dot + 1) : before;
        }

        private static void DrawPopup(SerializedProperty prop, string label, IReadOnlyList<WorldDataIndexEntry> choices)
        {
            int count = choices?.Count ?? 0;

            // Always provide a "(none)" option at index 0.
            string[] ids = new string[count + 1];
            string[] labels = new string[count + 1];

            ids[0] = string.Empty;
            labels[0] = "(none)";

            for (int i = 0; i < count; i++)
            {
                string id = choices[i].id ?? string.Empty;
                string displayName = choices[i].displayName;
                ids[i + 1] = id;
                labels[i + 1] = string.IsNullOrEmpty(displayName) ? id : displayName;
            }

            string current = prop.stringValue ?? string.Empty;

            int curIndex = 0;
            if (!string.IsNullOrEmpty(current))
            {
                for (int i = 1; i < ids.Length; i++)
                {
                    if (ids[i] == current)
                    {
                        curIndex = i;
                        break;
                    }
                }
            }

            int nextIndex = EditorGUILayout.Popup(label, curIndex, labels);
            if (nextIndex != curIndex && nextIndex >= 0 && nextIndex < ids.Length)
                prop.stringValue = ids[nextIndex];

            // If the value is non-empty but not found, keep it editable as raw text as a recovery path.
            if (!string.IsNullOrEmpty(current) && curIndex == 0)
            {
                EditorGUILayout.HelpBox($"'{current}' is not present in the currently loaded choices list. It may have been deleted or not loaded yet.", MessageType.Warning);
                prop.stringValue = EditorGUILayout.TextField("Raw Value", prop.stringValue);
            }
        }
    }
}
#endif
