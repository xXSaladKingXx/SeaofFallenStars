#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(FloraCatalogAuthoringSession))]
    internal sealed class FloraCatalogAuthoringSessionEditor : Editor
    {
        // Hardcoded family list per spec (kept as string in JSON, but edited via dropdown)
        private static readonly string[] FamilyOptions = { "Tree", "Grass", "Fruit", "Vegetable", "Bush", "Mushroom" };

        // UI state: per-flora entry "Add trait" selection
        private readonly System.Collections.Generic.List<int> _addTraitSelection = new System.Collections.Generic.List<int>();

        // UI state: per-flora entry "Add yield item" selection
        private readonly System.Collections.Generic.List<int> _addYieldItemSelection = new System.Collections.Generic.List<int>();

        
        // UI state: per-flora entry \"Add habitat\" selection
        private readonly System.Collections.Generic.List<int> _addHabitatSelection = new System.Collections.Generic.List<int>();
public override void OnInspectorGUI()
        {
            var session = (FloraCatalogAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            EditorGUILayout.LabelField("Flora Catalog", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.catalogId = EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.notes = EditorGUILayout.TextField("Notes", session.data.notes);
            EditorGUI.indentLevel--;

            session.data.flora ??= new System.Collections.Generic.List<FloraEntryModel>();

            // Pre-fetch global trait definitions for dropdowns
            var traitEntries = WorldDataChoicesCache.GetTraitDefinitions();
            string[] traitIds = traitEntries?.Select(e => e.id).ToArray() ?? Array.Empty<string>();
            string[] traitNames = traitEntries?.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray() ?? Array.Empty<string>();

            // Pre-fetch item definitions for yieldItems dropdowns
            var itemEntries = WorldDataChoicesCache.GetItemDefinitions();
            string[] itemIds = itemEntries?.Select(e => e.id).ToArray() ?? Array.Empty<string>();
            string[] itemNames = itemEntries?.Select(e => string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName).ToArray() ?? Array.Empty<string>();

            var terrainEntries = WorldDataChoicesCache.GetTerrainDefinitions();
            string[] terrainIds = terrainEntries?.Select(e => e.id).ToArray() ?? System.Array.Empty<string>();
            string[] terrainNames = terrainEntries?.Select(e => string.IsNullOrEmpty(e.displayName) ? e.id : e.displayName).ToArray() ?? System.Array.Empty<string>();


            // Sync UI selection lists to flora count
            while (_addTraitSelection.Count < session.data.flora.Count) _addTraitSelection.Add(0);
            while (_addTraitSelection.Count > session.data.flora.Count) _addTraitSelection.RemoveAt(_addTraitSelection.Count - 1);

            while (_addYieldItemSelection.Count < session.data.flora.Count) _addYieldItemSelection.Add(0);
            while (_addYieldItemSelection.Count > session.data.flora.Count) _addYieldItemSelection.RemoveAt(_addYieldItemSelection.Count - 1);

            
            while (_addHabitatSelection.Count < session.data.flora.Count) _addHabitatSelection.Add(0);
            while (_addHabitatSelection.Count > session.data.flora.Count) _addHabitatSelection.RemoveAt(_addHabitatSelection.Count - 1);
EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Flora", EditorStyles.boldLabel);

            for (int i = 0; i < session.data.flora.Count; i++)
            {
                var f = session.data.flora[i] ?? (session.data.flora[i] = new FloraEntryModel());

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;

                f.id = EditorGUILayout.TextField("ID", f.id);
                f.displayName = EditorGUILayout.TextField("Display Name", f.displayName);

                // Family dropdown (string-backed)
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

                // Traits: must be chosen from trait catalog IDs (dropdowns)
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
                            // If there are no trait definitions, keep this non-blocking but still visible.
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

                // Description + optional lists
                EditorGUILayout.LabelField("Description");
                f.description = EditorGUILayout.TextArea(f.description, GUILayout.MinHeight(50));

                DrawTerrainIdList("Habitats", f.habitats, terrainIds, terrainNames, ref _addHabitatSelection[i]);
                DrawStringList("Seasons", f.seasons);

                // isEdible (nullable) - show as tri-state
                DrawNullableBool("Is Edible", ref f.isEdible);

                EditorGUILayout.LabelField("Toxicity Notes");
                f.toxicityNotes = EditorGUILayout.TextArea(f.toxicityNotes, GUILayout.MinHeight(40));

                // Yield items: itemId must be selected from ItemCatalog when available
                f.yieldItems ??= new System.Collections.Generic.List<ItemQuantityEntry>();
                EditorGUILayout.LabelField("Yield Items", EditorStyles.miniBoldLabel);

                for (int y = 0; y < f.yieldItems.Count; y++)
                {
                    var yi = f.yieldItems[y] ?? (f.yieldItems[y] = new ItemQuantityEntry());
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    if (itemIds.Length > 0)
                    {
                        int cur = Array.IndexOf(itemIds, yi.itemId);
                        if (cur < 0) cur = 0;
                        int next = EditorGUILayout.Popup("Item", cur, itemNames);
                        next = Mathf.Clamp(next, 0, itemIds.Length - 1);
                        string nextId = itemIds[next];
                        if (!string.Equals(nextId, yi.itemId, StringComparison.Ordinal))
                        {
                            yi.itemId = nextId;
                            EditorUtility.SetDirty(session);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No ItemCatalog entries found. Create items first.", MessageType.Warning);
                        yi.itemId = EditorGUILayout.TextField("Item Id", yi.itemId);
                    }

                    yi.quantity = EditorGUILayout.FloatField("Quantity", yi.quantity);
                    yi.unit = EditorGUILayout.TextField("Unit", yi.unit);

                    EditorGUILayout.LabelField("Notes");
                    yi.notes = EditorGUILayout.TextArea(yi.notes, GUILayout.MinHeight(30));

                    if (GUILayout.Button("Remove Yield Item"))
                    {
                        f.yieldItems.RemoveAt(y);
                        EditorUtility.SetDirty(session);
                        y--;
                        EditorGUILayout.EndVertical();
                        continue;
                    }

                    EditorGUILayout.EndVertical();
                }

                if (itemIds.Length > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _addYieldItemSelection[i] = EditorGUILayout.Popup("Add Yield Item", Mathf.Clamp(_addYieldItemSelection[i], 0, itemNames.Length - 1), itemNames);
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            int pick = Mathf.Clamp(_addYieldItemSelection[i], 0, itemIds.Length - 1);
                            string idToAdd = itemIds[pick];
                            if (!string.IsNullOrWhiteSpace(idToAdd))
                            {
                                f.yieldItems.Add(new ItemQuantityEntry { itemId = idToAdd, quantity = 1f });
                                EditorUtility.SetDirty(session);
                            }
                        }
                    }
                }

                EditorGUILayout.LabelField("Notes");
                f.notes = EditorGUILayout.TextArea(f.notes, GUILayout.MinHeight(35));

                EditorGUI.indentLevel--;

                if (GUILayout.Button("Remove Flora Entry"))
                {
                    session.data.flora.RemoveAt(i);
                    EditorUtility.SetDirty(session);
                    // keep selection lists aligned
                    _addTraitSelection.RemoveAt(i);
                    _addYieldItemSelection.RemoveAt(i);
                        _addHabitatSelection.RemoveAt(i);
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add New Flora"))
            {
                session.data.flora.Add(new FloraEntryModel
                {
                    id = NextId(session.data.flora.Select(x => x?.id), "newFlora"),
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
            // 0 = Unspecified, 1 = True, 2 = False
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
