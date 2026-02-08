// Custom editor for culture catalogs. This editor allows authoring of cultures
// and their associated trait, language and religion definitions. A culture
// catalog centralizes all related definitions so that cultures can reference
// them by ID. Editors outside of the culture catalog should not allow
// editing of these definitions – only assignment via dropdowns. When
// assigning traits, languages or religions to a culture you can pick from
// existing definitions or choose to add a new definition. New definitions
// created in this editor are added to the catalog's respective lists.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Zana.WorldAuthoring;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(CultureCatalogAuthoringSession))]
    internal sealed class CultureCatalogAuthoringSessionEditor : Editor
    {
        // Track foldout state for each culture entry to allow collapsing/expanding details.
        private readonly List<bool> _cultureFoldouts = new List<bool>();

        // Track "Add ..." dropdown selections per culture so we only add when the user clicks.
        private readonly List<int> _addTraitSelection = new List<int>();
        private readonly List<int> _addLanguageSelection = new List<int>();
        private readonly List<int> _addReligionSelection = new List<int>();

        public override void OnInspectorGUI()
        {
            var session = (CultureCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            // Header fields for the catalog
            EditorGUILayout.LabelField("Culture Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            // Ensure collections are initialized
            session.data.cultures ??= new List<CultureEntryModel>();
            session.data.traits ??= new List<TraitEntryModel>();
            session.data.languages ??= new List<LanguageEntryModel>();
            session.data.religions ??= new List<ReligionEntryModel>();

            // Sync foldout states with cultures list
            while (_cultureFoldouts.Count < session.data.cultures.Count) _cultureFoldouts.Add(true);
            while (_cultureFoldouts.Count > session.data.cultures.Count) _cultureFoldouts.RemoveAt(_cultureFoldouts.Count - 1);

            // Sync add-selection lists
            while (_addTraitSelection.Count < session.data.cultures.Count) _addTraitSelection.Add(0);
            while (_addTraitSelection.Count > session.data.cultures.Count) _addTraitSelection.RemoveAt(_addTraitSelection.Count - 1);

            while (_addLanguageSelection.Count < session.data.cultures.Count) _addLanguageSelection.Add(0);
            while (_addLanguageSelection.Count > session.data.cultures.Count) _addLanguageSelection.RemoveAt(_addLanguageSelection.Count - 1);

            while (_addReligionSelection.Count < session.data.cultures.Count) _addReligionSelection.Add(0);
            while (_addReligionSelection.Count > session.data.cultures.Count) _addReligionSelection.RemoveAt(_addReligionSelection.Count - 1);

            // Collect arrays of trait, language and religion names for dropdowns from global catalogs.
            var traitDefsGlobal = WorldDataChoicesCache.GetTraitDefinitions();
            string[] traitIds = traitDefsGlobal.Select(e => e.id).ToArray();
            string[] traitNames = traitDefsGlobal.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray();

            var languageDefsGlobal = WorldDataChoicesCache.GetLanguageDefinitions();
            string[] languageIds = languageDefsGlobal.Select(e => e.id).ToArray();
            string[] languageNames = languageDefsGlobal.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray();

            var religionDefsGlobal = WorldDataChoicesCache.GetReligionDefinitions();
            string[] religionIds = religionDefsGlobal.Select(e => e.id).ToArray();
            string[] religionNames = religionDefsGlobal.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Cultures", EditorStyles.boldLabel);
            for (int i = 0; i < session.data.cultures.Count; i++)
            {
                var culture = session.data.cultures[i];
                if (culture == null)
                {
                    culture = new CultureEntryModel();
                    session.data.cultures[i] = culture;
                }
                string label = !string.IsNullOrWhiteSpace(culture.displayName) ? culture.displayName :
                               !string.IsNullOrWhiteSpace(culture.id) ? culture.id :
                               $"Culture {i + 1}";
                _cultureFoldouts[i] = EditorGUILayout.Foldout(_cultureFoldouts[i], label, true);
                if (_cultureFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    culture.id = EditorGUILayout.TextField("ID", culture.id);
                    culture.displayName = EditorGUILayout.TextField("Display Name", culture.displayName);
                    culture.description = EditorGUILayout.TextField("Description", culture.description);
                    culture.notes = EditorGUILayout.TextField("Notes", culture.notes);

                    // Traits assignment
                    culture.traits ??= new List<string>();
                    EditorGUILayout.LabelField("Traits", EditorStyles.miniBoldLabel);
                    for (int j = 0; j < culture.traits.Count; j++)
                    {
                        string currentId = culture.traits[j];
                        EditorGUILayout.BeginHorizontal();
                        // Popup to select existing trait
                        if (traitIds.Length > 0)
                        {
                            int currentIndex = Array.IndexOf(traitIds, currentId);
                            if (currentIndex < 0) currentIndex = 0;
                            int chosen = EditorGUILayout.Popup(currentIndex, traitNames);
                            if (chosen >= 0 && chosen < traitIds.Length)
                            {
                                string newId = traitIds[chosen];
                                if (!string.Equals(newId, currentId, StringComparison.Ordinal))
                                {
                                    culture.traits[j] = newId;
                                    EditorUtility.SetDirty(session);
                                }
                            }
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.Popup(0, new[] { "(no traits available)" });
                            }
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            culture.traits.RemoveAt(j);
                            EditorUtility.SetDirty(session);
                            j--;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    // Add trait dropdown (pick from global trait definitions). Only adds when the button is clicked.
                    if (traitNames.Length > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        _addTraitSelection[i] = EditorGUILayout.Popup("Add Trait", Mathf.Clamp(_addTraitSelection[i], 0, traitNames.Length - 1), traitNames);
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            int addSel = Mathf.Clamp(_addTraitSelection[i], 0, traitIds.Length - 1);
                            string idToAdd = traitIds[addSel];
                            if (!culture.traits.Contains(idToAdd))
                            {
                                Undo.RecordObject(session, "Add culture trait");
                                culture.traits.Add(idToAdd);
                                EditorUtility.SetDirty(session);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    // Languages assignment
                    culture.languages ??= new List<string>();
                    EditorGUILayout.LabelField("Languages", EditorStyles.miniBoldLabel);
                    for (int j = 0; j < culture.languages.Count; j++)
                    {
                        string currentId = culture.languages[j];
                        EditorGUILayout.BeginHorizontal();
                        if (languageIds.Length > 0)
                        {
                            int currentIndex = Array.IndexOf(languageIds, currentId);
                            if (currentIndex < 0) currentIndex = 0;
                            int chosen = EditorGUILayout.Popup(currentIndex, languageNames);
                            if (chosen >= 0 && chosen < languageIds.Length)
                            {
                                string newId = languageIds[chosen];
                                if (!string.Equals(newId, currentId, StringComparison.Ordinal))
                                {
                                    culture.languages[j] = newId;
                                    EditorUtility.SetDirty(session);
                                }
                            }
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.Popup(0, new[] { "(no languages available)" });
                            }
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            culture.languages.RemoveAt(j);
                            EditorUtility.SetDirty(session);
                            j--;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    // Add language dropdown (pick from global language definitions). Only adds when the button is clicked.
                    if (languageNames.Length > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        _addLanguageSelection[i] = EditorGUILayout.Popup("Add Language", Mathf.Clamp(_addLanguageSelection[i], 0, languageNames.Length - 1), languageNames);
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            int addSel = Mathf.Clamp(_addLanguageSelection[i], 0, languageIds.Length - 1);
                            string idToAdd = languageIds[addSel];
                            if (!culture.languages.Contains(idToAdd))
                            {
                                Undo.RecordObject(session, "Add culture language");
                                culture.languages.Add(idToAdd);
                                EditorUtility.SetDirty(session);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    // Religions assignment
                    culture.religions ??= new List<string>();
                    EditorGUILayout.LabelField("Religions", EditorStyles.miniBoldLabel);
                    for (int j = 0; j < culture.religions.Count; j++)
                    {
                        string currentId = culture.religions[j];
                        EditorGUILayout.BeginHorizontal();
                        if (religionIds.Length > 0)
                        {
                            int currentIndex = Array.IndexOf(religionIds, currentId);
                            if (currentIndex < 0) currentIndex = 0;
                            int chosen = EditorGUILayout.Popup(currentIndex, religionNames);
                            if (chosen >= 0 && chosen < religionIds.Length)
                            {
                                string newId = religionIds[chosen];
                                if (!string.Equals(newId, currentId, StringComparison.Ordinal))
                                {
                                    culture.religions[j] = newId;
                                    EditorUtility.SetDirty(session);
                                }
                            }
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.Popup(0, new[] { "(no religions available)" });
                            }
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            culture.religions.RemoveAt(j);
                            EditorUtility.SetDirty(session);
                            j--;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    // Add religion dropdown (pick from global religion definitions). Only adds when the button is clicked.
                    if (religionNames.Length > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        _addReligionSelection[i] = EditorGUILayout.Popup("Add Religion", Mathf.Clamp(_addReligionSelection[i], 0, religionNames.Length - 1), religionNames);
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            int addSel = Mathf.Clamp(_addReligionSelection[i], 0, religionIds.Length - 1);
                            string idToAdd = religionIds[addSel];
                            if (!culture.religions.Contains(idToAdd))
                            {
                                Undo.RecordObject(session, "Add culture religion");
                                culture.religions.Add(idToAdd);
                                EditorUtility.SetDirty(session);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(4);
                    EditorGUI.indentLevel--;
                }
            }

            // Button to add a new culture
            if (GUILayout.Button("Add New Culture"))
            {
                Undo.RecordObject(session, "Add culture");
                var newCulture = new CultureEntryModel
                {
                    id = string.Empty,
                    displayName = string.Empty,
                    description = string.Empty,
                    notes = string.Empty,
                    traits = new List<string>(),
                    languages = new List<string>(),
                    religions = new List<string>()
                };
                session.data.cultures.Add(newCulture);
                _cultureFoldouts.Add(true);
                _addTraitSelection.Add(0);
                _addLanguageSelection.Add(0);
                _addReligionSelection.Add(0);
                EditorUtility.SetDirty(session);
            }

            EditorGUILayout.Space(8);

            // Display a notice that definitions cannot be edited here.
            EditorGUILayout.HelpBox(
                "Trait, Language and Religion definitions cannot be edited in the Culture Catalog editor.\n"
                + "Use the dedicated Trait Catalog, Language Catalog and Religion Catalog sessions via the World Data Master to add or edit definitions.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
