#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(FaunaCatalogAuthoringSession))]
    internal sealed class FaunaCatalogAuthoringSessionEditor : Editor
    {
        // Hardcoded family list per spec (string-backed in JSON)
        private static readonly string[] FamilyOptions = { "mammals", "reptiles", "avians", "fish", "cephalapod", "insect" };

        private readonly System.Collections.Generic.List<int> _addTraitSelection = new System.Collections.Generic.List<int>();
        private readonly System.Collections.Generic.List<int> _addDropItemSelection = new System.Collections.Generic.List<int>();

        
        // UI state: per-fauna entry \"Add habitat\" selection
        private readonly System.Collections.Generic.List<int> _addHabitatSelection = new System.Collections.Generic.List<int>();
public override void OnInspectorGUI()
        {
            var session = (FaunaCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            EditorGUILayout.LabelField("Fauna Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            session.data.fauna ??= new System.Collections.Generic.List<FaunaEntryModel>();

            var traitEntries = WorldDataChoicesCache.GetTraitDefinitions();
            string[] traitIds = traitEntries?.Select(e => e.id).ToArray() ?? Array.Empty<string>();
            string[] traitNames = traitEntries?.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray() ?? Array.Empty<string>();

            var itemEntries = WorldDataChoicesCache.GetItemDefinitions();
            string[] itemIds = itemEntries?.Select(e => e.id).ToArray() ?? Array.Empty<string>();
            string[] itemNames = itemEntries?.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray() ?? Array.Empty<string>();

            var terrainEntries = WorldDataChoicesCache.GetTerrainDefinitions();
            string[] terrainIds = terrainEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] terrainNames = terrainEntries?.Select(e => string.IsNullOrEmpty(e.displayName) ? e.id : e.displayName).ToArray() ?? System.Array.Empty<string>();


            while (_addTraitSelection.Count < session.data.fauna.Count) _addTraitSelection.Add(0);
            while (_addTraitSelection.Count > session.data.fauna.Count) _addTraitSelection.RemoveAt(_addTraitSelection.Count - 1);

            while (_addDropItemSelection.Count < session.data.fauna.Count) _addDropItemSelection.Add(0);
            while (_addDropItemSelection.Count > session.data.fauna.Count) _addDropItemSelection.RemoveAt(_addDropItemSelection.Count - 1);

            
            while (_addHabitatSelection.Count < session.data.fauna.Count) _addHabitatSelection.Add(0);
            while (_addHabitatSelection.Count > session.data.fauna.Count) _addHabitatSelection.RemoveAt(_addHabitatSelection.Count - 1);
EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Fauna", EditorStyles.boldLabel);

            for (int i = 0; i < session.data.fauna.Count; i++)
            {
                var f = session.data.fauna[i] ?? (session.data.fauna[i] = new FaunaEntryModel());

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;

                f.id = EditorGUILayout.TextField("ID", f.id);
                f.displayName = EditorGUILayout.TextField("Display Name", f.displayName);

                int famIndex = IndexOfIgnoreCase(FamilyOptions, f.family);
                famIndex = Mathf.Clamp(famIndex, 0, Mathf.Max(0, FamilyOptions.Length - 1));
                int famNew = EditorGUILayout.Popup("Family", famIndex, FamilyOptions);
                if (FamilyOptions.Length > 0)
                {
                    string famVal = FamilyOptions[Mathf.Clamp(famNew, 0, FamilyOptions.Length - 1)];
                    if (!string.Equals(famVal, f.family, StringComparison.Ordinal))
                    {
                        f.family = famVal;
                        EditorUtility.SetDirty(session);
                    }
                }

                f.traits ??= new System.Collections.Generic.List<string>();
                EditorGUILayout.LabelField("Traits", EditorStyles.miniBoldLabel);
                for (int t = 0; t < f.traits.Count; t++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (traitIds.Length > 0)
                        {
                            int cur = Array.IndexOf(traitIds, f.traits[t]);
                            if (cur < 0) cur = 0;
                            int next = EditorGUILayout.Popup(cur, traitNames);
                            next = Mathf.Clamp(next, 0, traitIds.Length - 1);
                            string nextId = traitIds[next];
                            if (!string.Equals(nextId, f.traits[t], StringComparison.Ordinal))
                            {
                                f.traits[t] = nextId;
                                EditorUtility.SetDirty(session);
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("No TraitCatalog entries found. Create traits first.", MessageType.Warning);
                            f.traits[t] = EditorGUILayout.TextField(f.traits[t] ?? string.Empty);
                        }

                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            f.traits.RemoveAt(t);
                            EditorUtility.SetDirty(session);
                            t--;
                        }
                    }
                }

                if (traitIds.Length > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _addTraitSelection[i] = EditorGUILayout.Popup("Add Trait", Mathf.Clamp(_addTraitSelection[i], 0, traitNames.Length - 1), traitNames);
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            int pick = Mathf.Clamp(_addTraitSelection[i], 0, traitIds.Length - 1);
                            string idToAdd = traitIds[pick];
                            if (!string.IsNullOrWhiteSpace(idToAdd) && !f.traits.Contains(idToAdd))
                            {
                                f.traits.Add(idToAdd);
                                EditorUtility.SetDirty(session);
                            }
                        }
                    }
                }

                EditorGUILayout.LabelField("Description");
                f.description = EditorGUILayout.TextArea(f.description, GUILayout.MinHeight(50));

                int habitatPick = _addHabitatSelection[i];
                DrawTerrainIdList("Habitats", f.habitats, terrainIds, terrainNames, ref habitatPick);
                _addHabitatSelection[i] = habitatPick;

                f.sizeCategory = EditorGUILayout.TextField("Size Category", f.sizeCategory);
                f.temperament = EditorGUILayout.TextField("Temperament", f.temperament);

                DrawNullableBool("Is Domesticated", ref f.isDomesticated);

                // Drop items (catalog-backed)
                f.dropItems ??= new System.Collections.Generic.List<ItemQuantityEntry>();
                EditorGUILayout.LabelField("Drop Items", EditorStyles.miniBoldLabel);

                for (int d = 0; d < f.dropItems.Count; d++)
                {
                    var di = f.dropItems[d] ?? (f.dropItems[d] = new ItemQuantityEntry());
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    if (itemIds.Length > 0)
                    {
                        int cur = Array.IndexOf(itemIds, di.itemId);
                        if (cur < 0) cur = 0;
                        int next = EditorGUILayout.Popup("Item", cur, itemNames);
                        next = Mathf.Clamp(next, 0, itemIds.Length - 1);
                        string nextId = itemIds[next];
                        if (!string.Equals(nextId, di.itemId, StringComparison.Ordinal))
                        {
                            di.itemId = nextId;
                            EditorUtility.SetDirty(session);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No ItemCatalog entries found. Create items first.", MessageType.Warning);
                        di.itemId = EditorGUILayout.TextField("Item Id", di.itemId);
                    }

                    di.quantity = EditorGUILayout.FloatField("Quantity", di.quantity);
                    di.unit = EditorGUILayout.TextField("Unit", di.unit);
                    EditorGUILayout.LabelField("Notes");
                    di.notes = EditorGUILayout.TextArea(di.notes, GUILayout.MinHeight(30));

                    if (GUILayout.Button("Remove Drop Item"))
                    {
                        f.dropItems.RemoveAt(d);
                        EditorUtility.SetDirty(session);
                        d--;
                        EditorGUILayout.EndVertical();
                        continue;
                    }

                    EditorGUILayout.EndVertical();
                }

                if (itemIds.Length > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _addDropItemSelection[i] = EditorGUILayout.Popup("Add Drop Item", Mathf.Clamp(_addDropItemSelection[i], 0, itemNames.Length - 1), itemNames);
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            int pick = Mathf.Clamp(_addDropItemSelection[i], 0, itemIds.Length - 1);
                            string idToAdd = itemIds[pick];
                            if (!string.IsNullOrWhiteSpace(idToAdd))
                            {
                                f.dropItems.Add(new ItemQuantityEntry { itemId = idToAdd, quantity = 1f });
                                EditorUtility.SetDirty(session);
                            }
                        }
                    }
                }

                EditorGUILayout.LabelField("Notes");
                f.notes = EditorGUILayout.TextArea(f.notes, GUILayout.MinHeight(35));

                EditorGUI.indentLevel--;

                if (GUILayout.Button("Remove Fauna Entry"))
                {
                    session.data.fauna.RemoveAt(i);
                    EditorUtility.SetDirty(session);
                    _addTraitSelection.RemoveAt(i);
                    _addDropItemSelection.RemoveAt(i);
                        _addHabitatSelection.RemoveAt(i);
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add New Fauna"))
            {
                session.data.fauna.Add(new FaunaEntryModel
                {
                    id = NextId(session.data.fauna.Select(x => x?.id), "newFauna"),
                    displayName = string.Empty,
                    family = FamilyOptions.Length > 0 ? FamilyOptions[0] : string.Empty
                });
                EditorUtility.SetDirty(session);
            }

            serializedObject.ApplyModifiedProperties();
        }

        
        private static void DrawTerrainIdList(string label, System.Collections.Generic.List<string> ids, string[] optionIds, string[] optionLabels, ref int addSelection)
        {
            if (ids == null) return;

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            if (optionIds == null || optionLabels == null || optionIds.Length == 0 || optionLabels.Length == 0)
            {
                EditorGUILayout.HelpBox("No matching Terrain definitions are loaded. This list will be edited as free text.", MessageType.Warning);
                DrawStringList(null, ids);
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string currentId = ids[i] ?? string.Empty;
                    int curIndex = System.Array.IndexOf(optionIds, currentId);
                    if (curIndex < 0) curIndex = 0;

                    int nextIndex = EditorGUILayout.Popup(curIndex, optionLabels);
                    if (nextIndex != curIndex && nextIndex >= 0 && nextIndex < optionIds.Length)
                        ids[i] = optionIds[nextIndex];

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

private static void DrawStringList(string label, System.Collections.Generic.List<string> list)
        {
            list ??= new System.Collections.Generic.List<string>();
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            int removeAt = -1;
            for (int i = 0; i < list.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    list[i] = EditorGUILayout.TextField(list[i] ?? string.Empty);
                    if (GUILayout.Button("Remove", GUILayout.Width(60))) removeAt = i;
                }
                if (removeAt == i) break;
            }

            if (removeAt >= 0 && removeAt < list.Count) list.RemoveAt(removeAt);

            if (GUILayout.Button("Add " + label, GUILayout.Width(120)))
                list.Add(string.Empty);
        }

        private static void DrawNullableBool(string label, ref bool? value)
        {
            int cur = value.HasValue ? (value.Value ? 1 : 2) : 0;
            int next = EditorGUILayout.Popup(label, cur, new[] { "(Unspecified)", "True", "False" });
            if (next == 0) value = null;
            else if (next == 1) value = true;
            else value = false;
        }

        private static int IndexOfIgnoreCase(string[] options, string value)
        {
            if (options == null || options.Length == 0) return 0;
            if (string.IsNullOrWhiteSpace(value)) return 0;
            for (int i = 0; i < options.Length; i++)
                if (string.Equals(options[i], value, StringComparison.OrdinalIgnoreCase))
                    return i;
            return 0;
        }

        private static string NextId(System.Collections.Generic.IEnumerable<string> existing, string baseId)
        {
            int suffix = 1;
            var set = new System.Collections.Generic.HashSet<string>(existing?.Where(x => !string.IsNullOrWhiteSpace(x)) ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            while (set.Contains(baseId + suffix)) suffix++;
            return baseId + suffix;
        }
    }
}
#endif