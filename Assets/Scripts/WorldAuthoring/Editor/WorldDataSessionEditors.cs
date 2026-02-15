#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    internal static class WorldAuthoringEditorUI
    {
        //
        // Utility methods to build dropdown label and id arrays with a '(none)' or 'select' entry.
        // These helpers ensure optional dropdowns start with an explicit empty option rather than
        // defaulting to the first catalogue entry. They accept a custom label for the empty entry
        // and return parallel arrays of labels and ids.  Consumers should treat index 0 as 'no
        // selection' and disable related actions when the user has not chosen a real entry.
        //
        /// <summary>
        /// Build an array of labels for a dropdown with a leading '(none)' option.  If the
        /// provided list is null or empty, returns an array containing only <paramref name="noneLabel"/>.
        /// </summary>
        /// <param name="list">The list of catalogue entries to build labels from.</param>
        /// <param name="noneLabel">The label to use for the 'none' option. Defaults to "(none)".</param>
        /// <returns>An array of strings with length list.Count + 1.</returns>
        public static string[] BuildPopupLabelsWithNone(IReadOnlyList<WorldDataIndexEntry> list, string noneLabel = "(none)")
        {
            if (list == null || list.Count == 0)
            {
                return new[] { noneLabel };
            }

            var labels = new string[list.Count + 1];
            labels[0] = noneLabel;
            for (int i = 0; i < list.Count; i++)
            {
                // Use displayName when available; fallback to id or empty string.
                labels[i + 1] = list[i]?.displayName ?? list[i]?.id ?? string.Empty;
            }
            return labels;
        }

        /// <summary>
        /// Build an array of ids for a dropdown with a leading null entry.  The returned array
        /// parallels the labels from <see cref="BuildPopupLabelsWithNone"/>, i.e. index 0 will
        /// always be null and indices 1..n will contain the ids from the list in the same order.
        /// </summary>
        /// <param name="list">The list of catalogue entries to build ids from.</param>
        /// <returns>An array of strings with length list.Count + 1.</returns>
        public static string[] BuildPopupIdsWithNone(IReadOnlyList<WorldDataIndexEntry> list)
        {
            if (list == null || list.Count == 0)
            {
                return new string[] { null };
            }

            var ids = new string[list.Count + 1];
            ids[0] = null;
            for (int i = 0; i < list.Count; i++)
            {
                ids[i + 1] = list[i]?.id;
            }
            return ids;
        }
        public static void DrawHelpersHeader(WorldDataCategory category)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Helper Dropdowns (Non-invasive)", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh choices", GUILayout.Width(140)))
                    WorldDataChoicesCache.RefreshAll();

                if (GUILayout.Button("Open Editor Data Folder", GUILayout.Width(170)))
                {
                    var dir = WorldDataDirectoryResolver.GetEditorDirForCategory(category);
                    if (!string.IsNullOrWhiteSpace(dir)) EditorUtility.RevealInFinder(dir);
                }
            }
        }

        public static WorldDataIndexEntry PopupChoice(string label, IReadOnlyList<WorldDataIndexEntry> list, ref int pickIndex)
        {
            if (list == null || list.Count == 0)
            {
                EditorGUILayout.HelpBox($"No saved JSON files found for {label}.", MessageType.Info);
                return null;
            }

            var labels = WorldDataChoicesCache.ToDisplayArray(list);
            pickIndex = Mathf.Clamp(pickIndex, 0, Mathf.Max(0, labels.Length - 1));
            pickIndex = EditorGUILayout.Popup(label, pickIndex, labels);

            if (pickIndex < 0 || pickIndex >= list.Count) return null;
            return list[pickIndex];
        }

        // ------------------------------------------------------------
        // Men-at-Arms helper that supports BOTH schemas:
        //  A) string[] menAtArms
        //  B) MenAtArmsStack[] menAtArms (or any array element with id+count)
        // ------------------------------------------------------------
        public static bool TryAddOrIncrementMenAtArms(object dataObj, string menAtArmsId, int addCount)
        {
            if (dataObj == null) return false;
            if (string.IsNullOrWhiteSpace(menAtArmsId)) return false;
            if (addCount < 1) addCount = 1;

            var t = dataObj.GetType();
            var f = t.GetField("menAtArms", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return false;

            object current = f.GetValue(dataObj);
            var ft = f.FieldType;

            // Case A: string[] menAtArms (older/simple schema)
            if (ft == typeof(string[]))
            {
                var arr = current as string[] ?? Array.Empty<string>();
                var list = new List<string>(arr);

                // In string[] schema we do NOT encode counts to avoid breaking consumers.
                // We simply ensure the id exists.
                if (!list.Contains(menAtArmsId))
                {
                    list.Add(menAtArmsId);
                    f.SetValue(dataObj, list.ToArray());
                    return true;
                }
                return false;
            }

            // Case B: array schema, e.g. MenAtArmsStack[] where element has:
            // - menAtArmsId (or id/entryId)
            // - count (or quantity/units)
            if (ft.IsArray)
            {
                var elemType = ft.GetElementType();
                if (elemType == null) return false;

                var arr = current as Array;
                var list = new List<object>();
                bool found = false;
                bool changed = false;

                int len = arr != null ? arr.Length : 0;
                for (int i = 0; i < len; i++)
                {
                    var el = arr.GetValue(i);
                    if (el == null) continue;

                    string id = TryGetStringMember(el, "menAtArmsId")
                                ?? TryGetStringMember(el, "id")
                                ?? TryGetStringMember(el, "entryId");

                    if (!found && string.Equals(id, menAtArmsId, StringComparison.Ordinal))
                    {
                        found = true;

                        int count = TryGetIntMember(el, "count")
                                    ?? TryGetIntMember(el, "quantity")
                                    ?? TryGetIntMember(el, "units")
                                    ?? 0;

                        int next = Math.Max(1, count + addCount);
                        changed |= TrySetIntMember(el, "count", next)
                                   || TrySetIntMember(el, "quantity", next)
                                   || TrySetIntMember(el, "units", next);
                    }

                    list.Add(el);
                }

                if (!found)
                {
                    var newEl = Activator.CreateInstance(elemType);

                    bool idSet = TrySetStringMember(newEl, "menAtArmsId", menAtArmsId)
                                 || TrySetStringMember(newEl, "id", menAtArmsId)
                                 || TrySetStringMember(newEl, "entryId", menAtArmsId);

                    // count/quantity may not exist on some element types; that is OK.
                    TrySetIntMember(newEl, "count", addCount);
                    TrySetIntMember(newEl, "quantity", addCount);
                    TrySetIntMember(newEl, "units", addCount);

                    if (!idSet) return false;

                    list.Add(newEl);
                    changed = true;
                }

                // write back typed array
                var newArr = Array.CreateInstance(elemType, list.Count);
                for (int i = 0; i < list.Count; i++) newArr.SetValue(list[i], i);

                f.SetValue(dataObj, newArr);
                return changed;
            }

            // Unsupported type
            return false;
        }

        public static string[] GetMenAtArmsDisplayStrings(object dataObj)
        {
            if (dataObj == null) return Array.Empty<string>();

            var t = dataObj.GetType();
            var f = t.GetField("menAtArms", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return Array.Empty<string>();

            var ft = f.FieldType;
            var current = f.GetValue(dataObj);

            if (ft == typeof(string[]))
                return (current as string[]) ?? Array.Empty<string>();

            if (ft.IsArray)
            {
                var arr = current as Array;
                if (arr == null || arr.Length == 0) return Array.Empty<string>();

                var outList = new List<string>();
                for (int i = 0; i < arr.Length; i++)
                {
                    var el = arr.GetValue(i);
                    if (el == null) continue;

                    string id = TryGetStringMember(el, "menAtArmsId")
                                ?? TryGetStringMember(el, "id")
                                ?? TryGetStringMember(el, "entryId")
                                ?? "(unknown)";

                    int count = TryGetIntMember(el, "count")
                                ?? TryGetIntMember(el, "quantity")
                                ?? TryGetIntMember(el, "units")
                                ?? 1;

                    outList.Add($"{id} x{Mathf.Max(1, count)}");
                }

                return outList.ToArray();
            }

            return Array.Empty<string>();
        }

        private static string TryGetStringMember(object obj, string name)
        {
            var t = obj.GetType();
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0)
                return p.GetValue(obj, null) as string;

            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (f != null && f.FieldType == typeof(string))
                return f.GetValue(obj) as string;

            return null;
        }

        private static int? TryGetIntMember(object obj, string name)
        {
            var t = obj.GetType();
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (p != null && p.PropertyType == typeof(int) && p.GetIndexParameters().Length == 0)
                return (int)p.GetValue(obj, null);

            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (f != null && f.FieldType == typeof(int))
                return (int)f.GetValue(obj);

            return null;
        }

        private static bool TrySetStringMember(object obj, string name, string value)
        {
            var t = obj.GetType();
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (p != null && p.PropertyType == typeof(string) && p.CanWrite && p.GetIndexParameters().Length == 0)
            {
                p.SetValue(obj, value, null);
                return true;
            }

            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (f != null && f.FieldType == typeof(string))
            {
                f.SetValue(obj, value);
                return true;
            }

            return false;
        }

        private static bool TrySetIntMember(object obj, string name, int value)
        {
            var t = obj.GetType();
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (p != null && p.PropertyType == typeof(int) && p.CanWrite && p.GetIndexParameters().Length == 0)
            {
                p.SetValue(obj, value, null);
                return true;
            }

            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
            if (f != null && f.FieldType == typeof(int))
            {
                f.SetValue(obj, value);
                return true;
            }

            return false;
        }
    }

    // ============================================================
    // Settlement
    // ============================================================
    [CustomEditor(typeof(SettlementAuthoringSession))]
    public sealed class SettlementAuthoringSessionEditor : Editor
    {
        // Cached picker indices for dropdowns
        private int _rulerIndex;
        private bool _hasLiege;
        private int _liegeIndex;
        private int _addVassalIndex;

        private int _commanderIndex;
        private int _addCommanderIndex;
        private int _addKnightIndex;

        private int _addMenAtArmsIndex;
        private int _addMenAtArmsCount = 1;

        private int _cultureIndex;
        private int _residentIndex;

        // For culture composition editing (not used in settlement editor)
        private int _addCultureIndex;

        // For race distribution editing (unused in settlement editor)
        private int _addRaceIndex;

        public override void OnInspectorGUI()
        {
            var s = (SettlementAuthoringSession)target;
            if (s == null || s.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            // Sync serialized object
            serializedObject.Update();

            // Fetch available choices
            var characters = WorldDataChoicesCache.GetCharacters();
            var settlements = WorldDataChoicesCache.GetSettlements();
            var cultures = WorldDataChoicesCache.GetCultures();
            var races = WorldDataChoicesCache.GetRaceDefinitions();
            var maaEntries = WorldDataChoicesCache.GetMenAtArmsEntries();

            // ------------------------------------------------------------------
            // Feudal Hierarchy Section
            // ------------------------------------------------------------------
            EditorGUILayout.LabelField("Feudal Hierarchy", EditorStyles.boldLabel);

            // Ruler dropdown
            _rulerIndex = GetIndexById(characters, s.data.rulerCharacterId, _rulerIndex);
            var newRuler = WorldAuthoringEditorUI.PopupChoice("Ruler", characters, ref _rulerIndex);
            if (newRuler != null && s.data.rulerCharacterId != newRuler.id)
            {
                s.data.rulerCharacterId = newRuler.id;
                // update display name on main tab
                if (s.data.main == null) s.data.main = new MainTab();
                s.data.main.rulerDisplayName = newRuler.displayName;
                EditorUtility.SetDirty(s);
            }

            // Has Liege checkbox
            bool currentlyHasLiege = !string.IsNullOrWhiteSpace(s.data.liegeSettlementId);
            _hasLiege = EditorGUILayout.Toggle("Has Liege", currentlyHasLiege);
            if (!_hasLiege && currentlyHasLiege)
            {
                // Clear liege when unchecked
                s.data.liegeSettlementId = null;
                EditorUtility.SetDirty(s);
            }

            // Liege dropdown
            if (_hasLiege)
            {
                _liegeIndex = GetIndexById(settlements, s.data.liegeSettlementId, _liegeIndex);
                var newLiege = WorldAuthoringEditorUI.PopupChoice("Liege Settlement", settlements, ref _liegeIndex);
                if (newLiege != null && s.data.liegeSettlementId != newLiege.id)
                {
                    s.data.liegeSettlementId = newLiege.id;
                    EditorUtility.SetDirty(s);
                }
            }

            // Vassals list
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Vassals", EditorStyles.boldLabel);
            // Ensure arrays exist
            if (s.data.main == null) s.data.main = new MainTab();
            if (s.data.main.vassals == null) s.data.main.vassals = Array.Empty<string>();
            var vassalsList = new List<string>(s.data.main.vassals);
            // Display existing vassals
            for (int i = 0; i < vassalsList.Count; i++)
            {
                string vassalId = vassalsList[i];
                string display = GetDisplayNameById(settlements, vassalId);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(display, GUILayout.MaxWidth(200));
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        // Remove from list
                        vassalsList.RemoveAt(i);
                        // Also remove from feudal contracts
                        if (s.data.feudal != null && s.data.feudal.vassalContracts != null)
                        {
                            s.data.feudal.vassalContracts.RemoveAll(c => c != null && c.vassalSettlementId == vassalId);
                        }
                        EditorUtility.SetDirty(s);
                        break;
                    }
                }
            }
            // Add new vassal via dropdown with a '(none)' option.  Use a custom list of labels/ids
            // so that no settlement is selected by default.  Only enable the Add button once the
            // user has picked a settlement (index > 0).
            if (settlements != null && settlements.Count > 0)
            {
                EditorGUILayout.Space(2);
                // Build dropdown arrays with a '(select)' option at index 0
                string[] vassalLabels = WorldAuthoringEditorUI.BuildPopupLabelsWithNone(settlements, "Select Settlement…");
                string[] vassalIds = WorldAuthoringEditorUI.BuildPopupIdsWithNone(settlements);
                _addVassalIndex = Mathf.Clamp(_addVassalIndex, 0, vassalLabels.Length - 1);
                int selectedVassalIndex = EditorGUILayout.Popup("Add Vassal", _addVassalIndex, vassalLabels);
                _addVassalIndex = selectedVassalIndex;

                using (new EditorGUI.DisabledScope(selectedVassalIndex <= 0))
                {
                    if (GUILayout.Button("Add Vassal"))
                    {
                        string newVassalId = vassalIds[selectedVassalIndex];
                        if (!string.IsNullOrEmpty(newVassalId) && !vassalsList.Contains(newVassalId) && newVassalId != s.data.settlementId)
                        {
                            vassalsList.Add(newVassalId);
                            // Add to feudal contracts with default rates if necessary
                            if (s.data.feudal == null) s.data.feudal = new SettlementFeudalData();
                            if (s.data.feudal.vassalContracts == null) s.data.feudal.vassalContracts = new List<VassalContractData>();
                            bool exists = s.data.feudal.vassalContracts.Exists(vc => vc != null && vc.vassalSettlementId == newVassalId);
                            if (!exists)
                            {
                                s.data.feudal.vassalContracts.Add(new VassalContractData
                                {
                                    vassalSettlementId = newVassalId,
                                    incomeTaxRate = 0f,
                                    troopTaxRate = 0f,
                                    terms = string.Empty
                                });
                            }
                            EditorUtility.SetDirty(s);
                        }
                    }
                }
                // Assign back to data
                s.data.main.vassals = vassalsList.ToArray();
            }

            // ------------------------------------------------------------------
            // Council Members
            // ------------------------------------------------------------------
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Council Members", EditorStyles.boldLabel);

            if (s.data.feudal == null)
                s.data.feudal = new SettlementFeudalData();

            // Castellan
            int castellanIndex = GetIndexById(characters, s.data.feudal.castellanCharacterId, 0);
            var newCastellan = WorldAuthoringEditorUI.PopupChoice("Castellan", characters, ref castellanIndex);
            if (newCastellan != null && s.data.feudal.castellanCharacterId != newCastellan.id)
            {
                s.data.feudal.castellanCharacterId = newCastellan.id;
                EditorUtility.SetDirty(s);
            }

            // Marshall
            int marshallIndex = GetIndexById(characters, s.data.feudal.marshallCharacterId, 0);
            var newMarshall = WorldAuthoringEditorUI.PopupChoice("Marshall", characters, ref marshallIndex);
            if (newMarshall != null && s.data.feudal.marshallCharacterId != newMarshall.id)
            {
                s.data.feudal.marshallCharacterId = newMarshall.id;
                EditorUtility.SetDirty(s);
            }

            // Steward
            int stewardIndex = GetIndexById(characters, s.data.feudal.stewardCharacterId, 0);
            var newSteward = WorldAuthoringEditorUI.PopupChoice("Steward", characters, ref stewardIndex);
            if (newSteward != null && s.data.feudal.stewardCharacterId != newSteward.id)
            {
                s.data.feudal.stewardCharacterId = newSteward.id;
                EditorUtility.SetDirty(s);
            }

            // Diplomat
            int diplomatIndex = GetIndexById(characters, s.data.feudal.diplomatCharacterId, 0);
            var newDiplomat = WorldAuthoringEditorUI.PopupChoice("Diplomat", characters, ref diplomatIndex);
            if (newDiplomat != null && s.data.feudal.diplomatCharacterId != newDiplomat.id)
            {
                s.data.feudal.diplomatCharacterId = newDiplomat.id;
                EditorUtility.SetDirty(s);
            }

            // Spymaster
            int spymasterIndex = GetIndexById(characters, s.data.feudal.spymasterCharacterId, 0);
            var newSpymaster = WorldAuthoringEditorUI.PopupChoice("Spymaster", characters, ref spymasterIndex);
            if (newSpymaster != null && s.data.feudal.spymasterCharacterId != newSpymaster.id)
            {
                s.data.feudal.spymasterCharacterId = newSpymaster.id;
                EditorUtility.SetDirty(s);
            }

            // Head Priest
            int headPriestIndex = GetIndexById(characters, s.data.feudal.headPriestCharacterId, 0);
            var newHeadPriest = WorldAuthoringEditorUI.PopupChoice("Head Priest", characters, ref headPriestIndex);
            if (newHeadPriest != null && s.data.feudal.headPriestCharacterId != newHeadPriest.id)
            {
                s.data.feudal.headPriestCharacterId = newHeadPriest.id;
                EditorUtility.SetDirty(s);
            }

            EditorGUILayout.Space(8);

            // ------------------------------------------------------------------
            // Army Section
            // ------------------------------------------------------------------
            EditorGUILayout.LabelField("Army", EditorStyles.boldLabel);
            // Primary Commander dropdown
            _commanderIndex = GetIndexById(characters, s.data.army?.primaryCommanderCharacterId, _commanderIndex);
            var newCommander = WorldAuthoringEditorUI.PopupChoice("Primary Commander", characters, ref _commanderIndex);
            if (newCommander != null)
            {
                if (s.data.army == null) s.data.army = new ArmyTab();
                if (s.data.army.primaryCommanderCharacterId != newCommander.id)
                {
                    s.data.army.primaryCommanderCharacterId = newCommander.id;
                    s.data.army.primaryCommanderDisplayName = newCommander.displayName;
                    EditorUtility.SetDirty(s);
                }
            }
            // Men-at-Arms (read-only types from global catalog; quantities stored on the session)
            if (s.data.army == null) s.data.army = new ArmyTab();
            if (s.menAtArmsStacks == null) s.menAtArmsStacks = new System.Collections.Generic.List<MenAtArmsQuantityEntry>();

            // Display existing stacks
            if (s.menAtArmsStacks.Count > 0)
            {
                EditorGUILayout.LabelField("Current Men-at-Arms", EditorStyles.miniBoldLabel);
                for (int i = 0; i < s.menAtArmsStacks.Count; i++)
                {
                    var stack = s.menAtArmsStacks[i] ?? (s.menAtArmsStacks[i] = new MenAtArmsQuantityEntry());

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Type dropdown
                        int idx = GetIndexById(maaEntries, stack.menAtArmsId, 0);
                        var chosen = WorldAuthoringEditorUI.PopupChoice($"Type {i + 1}", maaEntries, ref idx);
                        if (chosen != null && stack.menAtArmsId != chosen.id)
                        {
                            stack.menAtArmsId = chosen.id;
                            EditorUtility.SetDirty(s);
                        }

                        // Units quantity (no inline label to avoid the field collapsing)
                        GUILayout.Label("Units", GUILayout.Width(40));
                        int newUnits = EditorGUILayout.IntField(Mathf.Max(0, stack.units), GUILayout.Width(60));
                        if (newUnits != stack.units)
                        {
                            stack.units = Mathf.Max(0, newUnits);
                            EditorUtility.SetDirty(s);
                        }

                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            s.menAtArmsStacks.RemoveAt(i);
                            EditorUtility.SetDirty(s);
                            i--;
                            continue;
                        }
                    }
                }
            }

            // Add new men-at-arms stack (type + units)
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                _addMenAtArmsIndex = GetIndexById(maaEntries, null, _addMenAtArmsIndex);
                var toAdd = WorldAuthoringEditorUI.PopupChoice("Add Type", maaEntries, ref _addMenAtArmsIndex);
                GUILayout.Label("Units", GUILayout.Width(40));
                _addMenAtArmsCount = EditorGUILayout.IntField(Mathf.Max(0, _addMenAtArmsCount), GUILayout.Width(60));
                using (new EditorGUI.DisabledScope(toAdd == null))
                {
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        if (toAdd != null)
                        {
                            bool exists = s.menAtArmsStacks.Exists(x => x != null && x.menAtArmsId == toAdd.id);
                            if (!exists)
                            {
                                s.menAtArmsStacks.Add(new MenAtArmsQuantityEntry
                                {
                                    menAtArmsId = toAdd.id,
                                    units = Mathf.Max(0, _addMenAtArmsCount)
                                });
                                EditorUtility.SetDirty(s);
                            }
                        }
                    }
                }
            }

            // Keep legacy id list in sync for older runtime readers
            if (s.data.army.menAtArms == null) s.data.army.menAtArms = Array.Empty<string>();
            s.data.army.menAtArms = s.menAtArmsStacks
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.menAtArmsId))
                .Select(x => x.menAtArmsId)
                .Distinct()
                .ToArray();

            EditorGUILayout.Space(8);

            // … (the remainder of the script is identical to the original)
            // All other sections (Cultural Distribution, Race Distribution, Residents, etc.) remain unchanged.
            // Ensure the rest of the file is left intact, including other editors defined below.

            // Draw remaining properties (economy, history, etc.)
            var skipPaths = new HashSet<string>
            {
                "m_Script",
                "data.rulerCharacterId",
                "data.characterIds",
                "data.liegeSettlementId",
                "data.main.vassals",
                "data.main.rulerDisplayName",
                "data.feudal.vassalContracts",
                "data.army.primaryCommanderCharacterId",
                "data.army.primaryCommanderDisplayName",
                "data.army.menAtArms",
                "data.cultural.culture"
            };
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                string path = iterator.propertyPath;
                if (skipPaths.Contains(path)) continue;
                if (path.StartsWith("data.cultural", StringComparison.Ordinal)) continue;
                if (path.StartsWith("data.army.menAtArms", StringComparison.Ordinal)) continue;
                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Utility: find index of entry by id, with fallback to previous index
        private static int GetIndexById(IReadOnlyList<WorldDataIndexEntry> list, string id, int fallbackIndex)
        {
            if (list == null || list.Count == 0) return 0;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i].id, id, StringComparison.Ordinal))
                        return i;
                }
            }
            // If id not found, clamp fallback
            return Mathf.Clamp(fallbackIndex, 0, list.Count - 1);
        }

        // Utility: get display name from list by id
        private static string GetDisplayNameById(IReadOnlyList<WorldDataIndexEntry> list, string id)
        {
            if (list == null) return id;
            foreach (var e in list)
            {
                if (e != null && string.Equals(e.id, id, StringComparison.Ordinal))
                    return e.displayName ?? id;
            }
            return string.IsNullOrEmpty(id) ? "(none)" : id;
        }
    }

    // ============================================================
    // Army
    // ============================================================
    [CustomEditor(typeof(ArmyAuthoringSession))]
    public sealed class ArmyAuthoringSessionEditor : Editor
    {
        // Cached indices for dropdowns
        private int _primaryCommanderIndex;
        private int _addCommanderIndex;
        private int _addKnightIndex;
        private int _addMenAtArmsIndex;
        private int _addMenAtArmsCount = 1;

        public override void OnInspectorGUI()
        {
            var s = (ArmyAuthoringSession)target;
            if (s == null || s.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            // Load choices
            var characters = WorldDataChoicesCache.GetCharacters();
            var menAtArms = WorldDataChoicesCache.GetMenAtArmsEntries();

            // Identity & basic fields
            EditorGUILayout.LabelField("Army Identity", EditorStyles.boldLabel);
            s.data.armyId = EditorGUILayout.TextField("Army ID", s.data.armyId);
            s.data.displayName = EditorGUILayout.TextField("Display Name", s.data.displayName);
            s.data.description = EditorGUILayout.TextField("Description", s.data.description);
            s.data.notes = EditorGUILayout.TextField("Notes", s.data.notes);

            EditorGUILayout.Space(8);

            // Command Structure
            EditorGUILayout.LabelField("Command Structure", EditorStyles.boldLabel);
            // Primary commander
            _primaryCommanderIndex = GetIndexById(characters, s.data.primaryCommanderCharacterId, _primaryCommanderIndex);
            var newCommander = WorldAuthoringEditorUI.PopupChoice("Primary Commander", characters, ref _primaryCommanderIndex);
            if (newCommander != null && s.data.primaryCommanderCharacterId != newCommander.id)
            {
                s.data.primaryCommanderCharacterId = newCommander.id;
                s.data.primaryCommanderDisplayName = newCommander.displayName;
                EditorUtility.SetDirty(s);
            }

            // Additional commanders list
            EditorGUILayout.LabelField("Additional Commanders", EditorStyles.miniBoldLabel);
            if (s.commanderCharacterIds == null) s.commanderCharacterIds = new List<string>();
            for (int i = 0; i < s.commanderCharacterIds.Count; i++)
            {
                string id = s.commanderCharacterIds[i];
                string name = GetDisplayNameById(characters, id);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(name, GUILayout.MaxWidth(200));
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        s.commanderCharacterIds.RemoveAt(i);
                        EditorUtility.SetDirty(s);
                        break;
                    }
                }
            }
            // Add commander
            _addCommanderIndex = GetIndexById(characters, null, _addCommanderIndex);
            var addCommander = WorldAuthoringEditorUI.PopupChoice("Add Commander", characters, ref _addCommanderIndex);
            using (new EditorGUI.DisabledScope(addCommander == null))
            {
                if (GUILayout.Button("Add Commander"))
                {
                    if (addCommander != null && !s.commanderCharacterIds.Contains(addCommander.id))
                    {
                        s.commanderCharacterIds.Add(addCommander.id);
                        EditorUtility.SetDirty(s);
                    }
                }
            }

            EditorGUILayout.Space(4);

            // Knights list
            EditorGUILayout.LabelField("Knights", EditorStyles.miniBoldLabel);
            if (s.knightCharacterIds == null) s.knightCharacterIds = new List<string>();
            for (int i = 0; i < s.knightCharacterIds.Count; i++)
            {
                string id = s.knightCharacterIds[i];
                string name = GetDisplayNameById(characters, id);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(name, GUILayout.MaxWidth(200));
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        s.knightCharacterIds.RemoveAt(i);
                        EditorUtility.SetDirty(s);
                        break;
                    }
                }
            }
            // Add knight
            _addKnightIndex = GetIndexById(characters, null, _addKnightIndex);
            var addKnight = WorldAuthoringEditorUI.PopupChoice("Add Knight", characters, ref _addKnightIndex);
            using (new EditorGUI.DisabledScope(addKnight == null))
            {
                if (GUILayout.Button("Add Knight"))
                {
                    if (addKnight != null && !s.knightCharacterIds.Contains(addKnight.id))
                    {
                        s.knightCharacterIds.Add(addKnight.id);
                        EditorUtility.SetDirty(s);
                    }
                }
            }

            EditorGUILayout.Space(8);

            // Men-at-Arms
            EditorGUILayout.LabelField("Men-at-Arms", EditorStyles.boldLabel);
            // Use reflection to support both string[] and array element types with id+count/quantity/units
            var dataObj = s.data;
            if (dataObj != null)
            {
                var tData = dataObj.GetType();
                var fMen = tData.GetField("menAtArms", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fMen != null)
                {
                    var ft = fMen.FieldType;
                    var current = fMen.GetValue(dataObj);

                    // Case A: string[] menAtArms (simple list of ids, no counts)
                    if (ft == typeof(string[]))
                    {
                        var arr = current as string[] ?? Array.Empty<string>();
                        var list = new List<string>(arr);
                        // Display existing ids
                        if (list.Count > 0)
                        {
                            EditorGUILayout.LabelField("Current Units", EditorStyles.miniBoldLabel);
                            for (int i = 0; i < list.Count; i++)
                            {
                                string id = list[i];
                                string display = GetDisplayNameById(menAtArms, id);
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField(display, GUILayout.MaxWidth(140));
                                    // In string[] schema we cannot edit counts; just allow removal
                                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                                    {
                                        list.RemoveAt(i);
                                        fMen.SetValue(dataObj, list.ToArray());
                                        EditorUtility.SetDirty(s);
                                        break;
                                    }
                                }
                            }
                        }
                        // Add new id (without count)
                        _addMenAtArmsIndex = GetIndexById(menAtArms, null, _addMenAtArmsIndex);
                        var addChoice = WorldAuthoringEditorUI.PopupChoice("Add Unit Type", menAtArms, ref _addMenAtArmsIndex);
                        using (new EditorGUI.DisabledScope(addChoice == null))
                        {
                            if (GUILayout.Button("Add Unit"))
                            {
                                if (addChoice != null && !list.Contains(addChoice.id))
                                {
                                    list.Add(addChoice.id);
                                    fMen.SetValue(dataObj, list.ToArray());
                                    EditorUtility.SetDirty(s);
                                }
                            }
                        }
                    }
                    // Case B: array of objects with menAtArmsId/id/entryId and count/quantity/units
                    else if (ft.IsArray)
                    {
                        var elemType = ft.GetElementType();
                        if (elemType != null)
                        {
                            var arr = current as Array;
                            var list = new List<object>();
                            int len = arr != null ? arr.Length : 0;
                            for (int i = 0; i < len; i++)
                            {
                                var el = arr.GetValue(i);
                                if (el != null) list.Add(el);
                            }
                            // Display existing stacks
                            if (list.Count > 0)
                            {
                                EditorGUILayout.LabelField("Current Units", EditorStyles.miniBoldLabel);
                                for (int i = 0; i < list.Count; i++)
                                {
                                    var el = list[i];
                                    // Get id and count via helper functions
                                    string id = ArmyAuthoringSessionEditor.TryGetStringMember(el, "menAtArmsId", "id", "entryId") ?? "(unknown)";
                                    int count = ArmyAuthoringSessionEditor.TryGetIntMember(el, "count", "quantity", "units") ?? 1;
                                    string display = GetDisplayNameById(menAtArms, id);
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField(display, GUILayout.MaxWidth(140));
                                        int newCount = EditorGUILayout.IntField(count, GUILayout.Width(40));
                                        newCount = Mathf.Max(1, newCount);
                                        if (newCount != count)
                                        {
                                            ArmyAuthoringSessionEditor.TrySetIntMember(el, newCount, "count", "quantity", "units");
                                            list[i] = el;
                                            // update array
                                            var newArr = Array.CreateInstance(elemType, list.Count);
                                            for (int j = 0; j < list.Count; j++) newArr.SetValue(list[j], j);
                                            fMen.SetValue(dataObj, newArr);
                                            EditorUtility.SetDirty(s);
                                        }
                                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                                        {
                                            list.RemoveAt(i);
                                            // update array
                                            var newArr = Array.CreateInstance(elemType, list.Count);
                                            for (int j = 0; j < list.Count; j++) newArr.SetValue(list[j], j);
                                            fMen.SetValue(dataObj, newArr);
                                            EditorUtility.SetDirty(s);
                                            break;
                                        }
                                    }
                                }
                            }
                            // Add / increment via helper
                            // Quantity input (count to add)
                            _addMenAtArmsCount = Mathf.Max(1, EditorGUILayout.IntField("Quantity", _addMenAtArmsCount));
                            _addMenAtArmsIndex = GetIndexById(menAtArms, null, _addMenAtArmsIndex);
                            var addChoice = WorldAuthoringEditorUI.PopupChoice("Add Unit Type", menAtArms, ref _addMenAtArmsIndex);
                            using (new EditorGUI.DisabledScope(addChoice == null))
                            {
                                if (GUILayout.Button("Add / Increment Unit"))
                                {
                                    if (addChoice != null)
                                    {
                                        if (WorldAuthoringEditorUI.TryAddOrIncrementMenAtArms(dataObj, addChoice.id, _addMenAtArmsCount))
                                        {
                                            EditorUtility.SetDirty(s);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);

            // Summary Stats
            EditorGUILayout.LabelField("Summary Stats", EditorStyles.boldLabel);
            s.data.totalArmy = EditorGUILayout.IntField("Total Troops", s.data.totalArmy);
            s.data.attack = EditorGUILayout.IntField("Attack", s.data.attack);
            s.data.defense = EditorGUILayout.IntField("Defense", s.data.defense);
            s.data.speed = EditorGUILayout.IntField("Speed (mpd)", s.data.speed);

            EditorGUILayout.Space(8);

            // Travel / Map Placement
            EditorGUILayout.LabelField("Travel / Map Placement", EditorStyles.boldLabel);
            s.hasMapPosition = EditorGUILayout.Toggle("Has Map Position", s.hasMapPosition);
            if (s.hasMapPosition)
            {
                s.mapPosition = EditorGUILayout.Vector2Field("Map Position", s.mapPosition);
            }

            EditorGUILayout.Space(8);

            // Draw remaining serialized properties
            var excludedPaths = new HashSet<string>
            {
                "m_Script",
                "data.armyId",
                "data.displayName",
                "data.description",
                "data.notes",
                "data.primaryCommanderCharacterId",
                "data.primaryCommanderDisplayName",
                "data.menAtArms",
                "data.totalArmy",
                "data.attack",
                "data.defense",
                "data.speed",
                "hasMapPosition",
                "mapPosition",
                "commanderCharacterIds",
                "knightCharacterIds"
            };
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                string path = iterator.propertyPath;
                if (excludedPaths.Contains(path)) continue;
                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Helper to get index by id
        private static int GetIndexById(IReadOnlyList<WorldDataIndexEntry> list, string id, int fallback)
        {
            if (list == null || list.Count == 0) return 0;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i].id, id, StringComparison.Ordinal)) return i;
                }
            }
            return Mathf.Clamp(fallback, 0, list.Count - 1);
        }

        private static string GetDisplayNameById(IReadOnlyList<WorldDataIndexEntry> list, string id)
        {
            if (list == null) return id;
            foreach (var e in list)
            {
                if (e != null && string.Equals(e.id, id, StringComparison.Ordinal))
                    return e.displayName ?? id;
            }
            return string.IsNullOrEmpty(id) ? "(none)" : id;
        }

        // ------------------------------------------------------------------
        // Reflection helpers for men-at-arms generic editing.  These mirror
        // similar helpers defined in WorldAuthoringEditorUI but are scoped
        // here because the originals are private.
        // ------------------------------------------------------------------
        internal static string TryGetStringMember(object obj, params string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0)
                {
                    return p.GetValue(obj, null) as string;
                }
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(string))
                {
                    return f.GetValue(obj) as string;
                }
            }
            return null;
        }

        internal static int? TryGetIntMember(object obj, params string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(int) && p.GetIndexParameters().Length == 0)
                {
                    return (int)p.GetValue(obj, null);
                }
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(int))
                {
                    return (int)f.GetValue(obj);
                }
            }
            return null;
        }

        internal static bool TrySetIntMember(object obj, int value, params string[] names)
        {
            if (obj == null) return false;
            var t = obj.GetType();
            bool changed = false;
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(int) && p.CanWrite && p.GetIndexParameters().Length == 0)
                {
                    p.SetValue(obj, value, null);
                    changed = true;
                }
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(int))
                {
                    f.SetValue(obj, value);
                    changed = true;
                }
            }
            return changed;
        }
    }

    // ============================================================
    // Character
    // ============================================================
    [CustomEditor(typeof(CharacterAuthoringSession))]
    public sealed class CharacterAuthoringSessionEditor : Editor
    {
        // Persistent UI state for list "Add" dropdowns.
        private readonly Dictionary<string, int> _addPickByListPath = new Dictionary<string, int>();

        // Optional quick-add UI for relationships.other
        private bool _showQuickAdd = true;
        private int _otherTargetPick;
        private int _otherRelationTypePick;
        private string _customOtherRelation = "Ally";

        private static readonly string[] OtherRelationTypes =
        {
            "Ally", "Friend", "Rival", "Enemy", "Mentor", "Ward", "Liege", "Vassal", "Servant", "Patron", "Lover", "Companion", "Other"
        };

        public override void OnInspectorGUI()
        {
            var s = (CharacterAuthoringSession)target;
            if (s == null || s.data == null) return;

            // Keep the choices cache fresh for dropdowns.
            WorldAuthoringEditorUI.DrawHelpersHeader(WorldDataCategory.Character);

            serializedObject.Update();

            // Draw all serialized fields, replacing ID reference strings with dropdowns
            // where possible.
            DrawAllPropertiesWithReferencePickers(serializedObject);

            serializedObject.ApplyModifiedProperties();

            // Optional quick-add section for relationships.other (quality-of-life).
            DrawQuickAddRelationshipsOther(s);
        }

        private void DrawQuickAddRelationshipsOther(CharacterAuthoringSession s)
        {
            _showQuickAdd = EditorGUILayout.Foldout(_showQuickAdd, "Quick Add: relationships.other", true);
            if (!_showQuickAdd) return;

            var chars = WorldDataChoicesCache.GetCharacters();
            if (chars == null || chars.Count == 0)
            {
                EditorGUILayout.HelpBox("No Character JSONs found; cannot use quick-add tools.", MessageType.Info);
                return;
            }

            if (s.data.relationships == null) s.data.relationships = new Relationships();

            _otherRelationTypePick = EditorGUILayout.Popup("Relation type", _otherRelationTypePick, OtherRelationTypes);
            var rel = OtherRelationTypes[Mathf.Clamp(_otherRelationTypePick, 0, OtherRelationTypes.Length - 1)];
            if (string.Equals(rel, "Other", StringComparison.OrdinalIgnoreCase))
                _customOtherRelation = EditorGUILayout.TextField("Custom relation", _customOtherRelation);

            var otherTarget = WorldAuthoringEditorUI.PopupChoice("Target character", chars, ref _otherTargetPick);
            using (new EditorGUI.DisabledScope(otherTarget == null))
            {
                if (GUILayout.Button("Add Relationship"))
                {
                    var kind = string.Equals(rel, "Other", StringComparison.OrdinalIgnoreCase) ? _customOtherRelation : rel;
                    if (string.IsNullOrWhiteSpace(kind)) kind = "Other";

                    var list = new List<RelationshipOther>(s.data.relationships.other ?? Array.Empty<RelationshipOther>());
                    list.Add(new RelationshipOther { relation = kind, characterId = otherTarget.id });
                    s.data.relationships.other = list.ToArray();
                    EditorUtility.SetDirty(s);
                }
            }
        }

        private void DrawAllPropertiesWithReferencePickers(SerializedObject so)
        {
            var settlements = WorldDataChoicesCache.GetSettlements();
            // WorldDataChoicesCache does not provide a dedicated GetRegions() wrapper.
            // Use the generic Get(category) accessor for regions.
            var regions = WorldDataChoicesCache.Get(WorldDataCategory.Region);
            var armies = WorldDataChoicesCache.GetArmies();
            var characters = WorldDataChoicesCache.GetCharacters();
            var cultures = WorldDataChoicesCache.GetCultures();

            // Global catalogs flattened to ID lists.
            var raceDefs = WorldDataChoicesCache.GetRaceDefinitions();
            var religionDefs = WorldDataChoicesCache.GetReligionDefinitions();
            var traitDefs = WorldDataChoicesCache.GetTraitDefinitions();
            var languageDefs = WorldDataChoicesCache.GetLanguageDefinitions();

            SerializedProperty p = so.GetIterator();
            bool enterChildren = true;
            while (p.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (p.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(p, false);
                    continue;
                }

                // Skip the raw homeSettlementId field and replace with a combined toggle+dropdown.
                if (p.propertyPath == "homeSettlementId")
                {
                    DrawOptionalSettlementLink(so, settlements);
                    continue;
                }

                // Reference string fields
                if (p.propertyType == SerializedPropertyType.String)
                {
                    if (TryDrawReferenceString(p, settlements, regions, armies, characters, cultures, raceDefs, religionDefs, traitDefs, languageDefs))
                        continue;
                }

                // String arrays/lists of IDs
                if (p.isArray && p.propertyType != SerializedPropertyType.String)
                {
                    // Handle arrays of strings (Unity exposes string[] as isArray with element type string)
                    if (p.arrayElementType == "string")
                    {
                        if (TryDrawReferenceStringArray(p, settlements, regions, armies, characters, cultures, raceDefs, religionDefs, traitDefs, languageDefs))
                            continue;
                    }
                }

                // Default draw
                EditorGUILayout.PropertyField(p, false);
                if (p.hasVisibleChildren && p.isExpanded)
                    enterChildren = true;
            }
        }

        private void DrawOptionalSettlementLink(SerializedObject so, IReadOnlyList<WorldDataIndexEntry> settlements)
        {
            var hasProp = so.FindProperty("hasHomeSettlementId");
            var idProp = so.FindProperty("homeSettlementId");
            if (hasProp == null || idProp == null)
            {
                // fallback
                if (idProp != null) EditorGUILayout.PropertyField(idProp, false);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Home Settlement (Non-invasive)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(hasProp, new GUIContent("Has Home Settlement"));
                using (new EditorGUI.DisabledScope(!hasProp.boolValue))
                {
                    DrawIdPopupForStringProperty(idProp, "Home Settlement", settlements);
                }
                if (!hasProp.boolValue && !string.IsNullOrWhiteSpace(idProp.stringValue))
                    idProp.stringValue = string.Empty;
            }
        }

        private bool TryDrawReferenceString(
            SerializedProperty p,
            IReadOnlyList<WorldDataIndexEntry> settlements,
            IReadOnlyList<WorldDataIndexEntry> regions,
            IReadOnlyList<WorldDataIndexEntry> armies,
            IReadOnlyList<WorldDataIndexEntry> characters,
            IReadOnlyList<WorldDataIndexEntry> cultures,
            IReadOnlyList<WorldDataIndexEntry> raceDefs,
            IReadOnlyList<WorldDataIndexEntry> religionDefs,
            IReadOnlyList<WorldDataIndexEntry> traitDefs,
            IReadOnlyList<WorldDataIndexEntry> languageDefs)
        {
            // Do not treat the character's own ID as a reference.
            if (p.propertyPath == "data.characterId") return false;

            // Do not attempt to convert generic "id" fields into dropdowns (these are
            // commonly non-reference identifiers in many nested models).
            if (string.Equals(p.name, "id", StringComparison.OrdinalIgnoreCase)) return false;

            string name = (p.name ?? string.Empty).ToLowerInvariant();
            string path = (p.propertyPath ?? string.Empty).ToLowerInvariant();

            // Settlement references
            if (name.EndsWith("settlementid") || name == "settlementid" || name.EndsWith("rulessettlementid") ||
                (name.EndsWith("id") && name.Contains("settlement")))
                return DrawIdPopupForStringProperty(p, p.displayName, settlements);

            // Region references
            if (name.EndsWith("regionid") || name == "regionid" || (name.EndsWith("id") && name.Contains("region")))
                return DrawIdPopupForStringProperty(p, p.displayName, regions);

            // Army references
            if (name.EndsWith("armyid") || name == "armyid" || (name.EndsWith("id") && name.Contains("army")))
                return DrawIdPopupForStringProperty(p, p.displayName, armies);

            // Character references (spouse, liege, parent/child, relationship targets, etc.)
            if (name.EndsWith("characterid") || name == "spouse" || name.EndsWith("spouseid") || name.EndsWith("liegeid") ||
                (name.EndsWith("id") && (
                    name.Contains("character") || name.Contains("spouse") || name.Contains("liege") || name.Contains("vassal") ||
                    name.Contains("father") || name.Contains("mother") || name.Contains("child") || name.Contains("heir") ||
                    path.Contains("relationships"))))
                return DrawIdPopupForStringProperty(p, p.displayName, characters);

            // Culture reference (some models store this as 'culture' rather than 'cultureId')
            if (name.EndsWith("cultureid") || name == "cultureid" ||
                (name == "culture" && path.Contains("cultural")) ||
                (name.EndsWith("id") && name.Contains("culture")))
                return DrawIdPopupForStringProperty(p, p.displayName, cultures);

            // Race reference (some models store this as 'race' rather than 'raceId')
            if (name.EndsWith("raceid") || name == "raceid" ||
                (name == "race" && (path.Contains("cultural") || path.Contains("culture"))) ||
                (name.EndsWith("id") && name.Contains("race")))
                return DrawIdPopupForStringProperty(p, p.displayName, raceDefs);

            // Religion reference
            if (name.EndsWith("religionid") || name == "religionid" ||
                (name == "religion" && (path.Contains("cultural") || path.Contains("culture"))) ||
                (name.EndsWith("id") && name.Contains("relig")))
                return DrawIdPopupForStringProperty(p, p.displayName, religionDefs);

            // Trait reference (single)
            if (name.EndsWith("traitid") || name == "traitid" || (name.EndsWith("id") && name.Contains("trait")))
                return DrawIdPopupForStringProperty(p, p.displayName, traitDefs);

            // Language reference (single)
            if (name.EndsWith("languageid") || name == "languageid" || (name.EndsWith("id") && name.Contains("language")))
                return DrawIdPopupForStringProperty(p, p.displayName, languageDefs);

            return false;
        }

        private bool TryDrawReferenceStringArray(
            SerializedProperty p,
            IReadOnlyList<WorldDataIndexEntry> settlements,
            IReadOnlyList<WorldDataIndexEntry> regions,
            IReadOnlyList<WorldDataIndexEntry> armies,
            IReadOnlyList<WorldDataIndexEntry> characters,
            IReadOnlyList<WorldDataIndexEntry> cultures,
            IReadOnlyList<WorldDataIndexEntry> raceDefs,
            IReadOnlyList<WorldDataIndexEntry> religionDefs,
            IReadOnlyList<WorldDataIndexEntry> traitDefs,
            IReadOnlyList<WorldDataIndexEntry> languageDefs)
        {
            string name = (p.name ?? string.Empty).ToLowerInvariant();

            IReadOnlyList<WorldDataIndexEntry> choices = null;
            if (name.Contains("language")) choices = languageDefs;
            else if (name.Contains("religion")) choices = religionDefs;
            else if (name.Contains("trait")) choices = traitDefs;
            else if (name.Contains("race")) choices = raceDefs;
            else if (name.Contains("culture")) choices = cultures;
            else if (name.Contains("settlement")) choices = settlements;
            else if (name.Contains("region")) choices = regions;
            else if (name.Contains("army")) choices = armies;
            else if (name.Contains("character")) choices = characters;

            if (choices == null || choices.Count == 0) return false;

            DrawStringIdArrayWithDropdowns(p, choices);
            return true;
        }

        private void DrawStringIdArrayWithDropdowns(SerializedProperty arrayProp, IReadOnlyList<WorldDataIndexEntry> choices)
        {
            arrayProp.isExpanded = EditorGUILayout.Foldout(arrayProp.isExpanded, arrayProp.displayName, true);
            if (!arrayProp.isExpanded) return;

            EditorGUI.indentLevel++;

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var el = arrayProp.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawIdPopupForStringProperty(el, $"{i + 1}", choices);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        arrayProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            // Add new row
            using (new EditorGUILayout.HorizontalScope())
            {
                int pick = GetAddPick(arrayProp.propertyPath);
                pick = Mathf.Clamp(pick, 0, Mathf.Max(0, choices.Count));
                string[] labels = BuildPopupLabelsWithNone(choices);
                pick = EditorGUILayout.Popup("Add", pick, labels);
                SetAddPick(arrayProp.propertyPath, pick);
                using (new EditorGUI.DisabledScope(pick <= 0))
                {
                    if (GUILayout.Button("Add", GUILayout.Width(70)))
                    {
                        string id = choices[pick - 1].id;
                        if (!ContainsString(arrayProp, id))
                        {
                            int newIndex = arrayProp.arraySize;
                            arrayProp.InsertArrayElementAtIndex(newIndex);
                            var el = arrayProp.GetArrayElementAtIndex(newIndex);
                            el.stringValue = id;
                        }
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        private bool DrawIdPopupForStringProperty(SerializedProperty p, string label, IReadOnlyList<WorldDataIndexEntry> choices)
        {
            if (choices == null || choices.Count == 0)
                return false;

            string[] labels = BuildPopupLabelsWithNone(choices);
            string[] ids = BuildPopupIdsWithNone(choices);

            int current = 0;
            string curId = p.stringValue ?? string.Empty;
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.Equals(ids[i], curId, StringComparison.OrdinalIgnoreCase))
                {
                    current = i;
                    break;
                }
            }

            int next = EditorGUILayout.Popup(label, current, labels);
            next = Mathf.Clamp(next, 0, ids.Length - 1);
            if (next != current)
            {
                p.stringValue = ids[next];
            }
            return true;
        }

        private static string[] BuildPopupLabelsWithNone(IReadOnlyList<WorldDataIndexEntry> list)
        {
            var a = new string[(list?.Count ?? 0) + 1];
            a[0] = "(none)";
            for (int i = 0; i < (list?.Count ?? 0); i++)
                a[i + 1] = list[i]?.ToString() ?? "(null)";
            return a;
        }

        private static string[] BuildPopupIdsWithNone(IReadOnlyList<WorldDataIndexEntry> list)
        {
            var a = new string[(list?.Count ?? 0) + 1];
            a[0] = string.Empty;
            for (int i = 0; i < (list?.Count ?? 0); i++)
                a[i + 1] = list[i]?.id ?? string.Empty;
            return a;
        }

        private int GetAddPick(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return 0;
            return _addPickByListPath.TryGetValue(key, out var v) ? v : 0;
        }

        private void SetAddPick(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _addPickByListPath[key] = value;
        }

        private static bool ContainsString(SerializedProperty arrayProp, string value)
        {
            if (arrayProp == null || !arrayProp.isArray) return false;
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var el = arrayProp.GetArrayElementAtIndex(i);
                if (el != null && string.Equals(el.stringValue, value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    // ============================================================
    // Region
    // ============================================================
    [CustomEditor(typeof(RegionAuthoringSession))]
    public sealed class RegionAuthoringSessionEditor : Editor
    {
        // Cached indices for culture composition editing
        private int _addCultureIndex;
        private float _addCulturePercentage = 0f;

        public override void OnInspectorGUI()
        {
            var r = (RegionAuthoringSession)target;
            if (r == null)
            {
                DrawDefaultInspector();
                return;
            }

            // Ensure serialized object is synced
            serializedObject.Update();

            // Fetch available cultures from the choices cache
            var cultures = WorldDataChoicesCache.GetCultures();

            // Cultural composition editing
            EditorGUILayout.LabelField("Cultural Composition", EditorStyles.boldLabel);
            if (r.culturalComposition == null) r.culturalComposition = new System.Collections.Generic.List<CultureCompositionEntry>();

            // Display existing entries
            for (int i = 0; i < r.culturalComposition.Count; i++)
            {
                var entry = r.culturalComposition[i];
                if (entry == null)
                {
                    entry = new CultureCompositionEntry();
                    r.culturalComposition[i] = entry;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    int idx = GetIndexById(cultures, entry.cultureId, 0);
                    var chosen = WorldAuthoringEditorUI.PopupChoice($"Culture {i + 1}", cultures, ref idx);
                    if (chosen != null && entry.cultureId != chosen.id)
                    {
                        entry.cultureId = chosen.id;
                        EditorUtility.SetDirty(r);
                    }
                    float pct = EditorGUILayout.FloatField("%", entry.percentage, GUILayout.MaxWidth(80));
                    if (!Mathf.Approximately(pct, entry.percentage))
                    {
                        entry.percentage = pct;
                        EditorUtility.SetDirty(r);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        r.culturalComposition.RemoveAt(i);
                        EditorUtility.SetDirty(r);
                        i--;
                        continue;
                    }
                }
            }
            // Controls for adding a new entry
            if (cultures != null && cultures.Count > 0)
            {
                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Build dropdown arrays with a (select) option at index 0
                    string[] addLabels = WorldAuthoringEditorUI.BuildPopupLabelsWithNone(cultures, "Select Culture…");
                    string[] addIds = WorldAuthoringEditorUI.BuildPopupIdsWithNone(cultures);

                    _addCultureIndex = Mathf.Clamp(_addCultureIndex, 0, addLabels.Length - 1);
                    int selected = EditorGUILayout.Popup("Add Culture", _addCultureIndex, addLabels);
                    _addCultureIndex = selected;

                    _addCulturePercentage = EditorGUILayout.FloatField("%", _addCulturePercentage, GUILayout.MaxWidth(80));

                    using (new EditorGUI.DisabledScope(selected <= 0))
                    {
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            string cultureId = addIds[selected];
                            bool exists = false;
                            foreach (var entry in r.culturalComposition)
                            {
                                if (entry != null && entry.cultureId == cultureId)
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (!exists)
                            {
                                r.culturalComposition.Add(new CultureCompositionEntry
                                {
                                    cultureId = cultureId,
                                    percentage = _addCulturePercentage
                                });
                                _addCultureIndex = 0;
                                _addCulturePercentage = 0f;
                                EditorUtility.SetDirty(r);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);

            // Draw remaining properties excluding the culturalComposition list
            var skipPaths = new System.Collections.Generic.HashSet<string>
            {
                "m_Script",
                "culturalComposition"
            };
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                string path = iterator.propertyPath;
                if (skipPaths.Contains(path)) continue;
                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Helper to find index of an entry by id with fallback. This duplicates the
        // helper defined elsewhere in this file but scoped to this editor for
        // encapsulation.
        private static int GetIndexById(IReadOnlyList<WorldDataIndexEntry> list, string id, int fallback)
        {
            if (list == null || list.Count == 0) return 0;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i].id, id, StringComparison.Ordinal)) return i;
                }
            }
            return Mathf.Clamp(fallback, 0, list.Count - 1);
        }
    }

    // ============================================================
    // Unpopulated
    // ============================================================
    [CustomEditor(typeof(UnpopulatedAuthoringSession))]
    public sealed class UnpopulatedAuthoringSessionEditor : Editor
    {
        // Cached indices for dropdowns
        private int _subtypeIndex;
        private int _terrainIndex;
        private int _waterBodyIndex;
        private int _waterTypeIndex;

        // Available options for unpopulated data
        private static readonly string[] SubtypeOptions = { "Wilderness", "Water", "Ruins" };
        private static readonly string[] TerrainOptions = {
            "Forest","Hills","Mountains","Plains","Desert","Swamp","Coastal","River","Lake","Ocean","Ruins"
        };
        private static readonly string[] WaterBodyOptions = {
            "River","Lake","Sea","Ocean","Reef","Strait","Bay","Fjord"
        };
        private static readonly string[] WaterTypeOptions = {
            "Freshwater","Saltwater","Brackish"
        };

        // Cached indices for cultural composition editing
        private int _addCultureIndex;
        private float _addCulturePercentage = 0f;

        public override void OnInspectorGUI()
        {
            var session = (UnpopulatedAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            // Draw and assign subtype using dropdown
            // Determine current index based on data.subtype
            string currentSubtype = session.data.subtype;
            _subtypeIndex = Array.IndexOf(SubtypeOptions, currentSubtype);
            if (_subtypeIndex < 0) _subtypeIndex = 0;
            int selectedSubtype = EditorGUILayout.Popup("Subtype", _subtypeIndex, SubtypeOptions);
            if (selectedSubtype != _subtypeIndex)
            {
                session.data.subtype = SubtypeOptions[selectedSubtype];
                _subtypeIndex = selectedSubtype;
                EditorUtility.SetDirty(session);
            }

            // Draw geography section
            if (session.data.geography == null) session.data.geography = new UnpopulatedGeographyTab();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Geography", EditorStyles.boldLabel);
            session.data.geography.areaSqMi = EditorGUILayout.FloatField("Area (sq mi)", session.data.geography.areaSqMi);
            // Terrain type dropdown
            string currentTerrain = session.data.geography.terrainType;
            _terrainIndex = Array.IndexOf(TerrainOptions, currentTerrain);
            if (_terrainIndex < 0) _terrainIndex = 0;
            int selectedTerrain = EditorGUILayout.Popup("Terrain Type", _terrainIndex, TerrainOptions);
            if (selectedTerrain != _terrainIndex)
            {
                session.data.geography.terrainType = TerrainOptions[selectedTerrain];
                _terrainIndex = selectedTerrain;
                EditorUtility.SetDirty(session);
            }
            // Notes and breakdown via property fields
            var geoNotesProp = serializedObject.FindProperty("data.geography.notes");
            if (geoNotesProp != null)
            {
                EditorGUILayout.PropertyField(geoNotesProp, new GUIContent("Notes"), true);
            }
            var breakdownProp = serializedObject.FindProperty("data.geography.terrainBreakdown");
            if (breakdownProp != null)
            {
                EditorGUILayout.PropertyField(breakdownProp, new GUIContent("Terrain Breakdown"), true);
            }

            // Draw nature tab
            if (session.data.nature == null) session.data.nature = new UnpopulatedNatureTab();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Nature", EditorStyles.boldLabel);
            // Narrative notes
            var floraNotesProp = serializedObject.FindProperty("data.nature.floraNotes");
            if (floraNotesProp != null) EditorGUILayout.PropertyField(floraNotesProp, new GUIContent("Flora Notes"), true);
            var faunaNotesProp = serializedObject.FindProperty("data.nature.faunaNotes");
            if (faunaNotesProp != null) EditorGUILayout.PropertyField(faunaNotesProp, new GUIContent("Fauna Notes"), true);
            var resNotesProp = serializedObject.FindProperty("data.nature.resourcesNotes");
            if (resNotesProp != null) EditorGUILayout.PropertyField(resNotesProp, new GUIContent("Resources Notes"), true);
            // Distribution lists and structured resources
            var floraDistProp = serializedObject.FindProperty("data.nature.floraDistribution");
            if (floraDistProp != null)
            {
                EditorGUILayout.PropertyField(floraDistProp, new GUIContent("Flora Distribution"), true);
            }
            var faunaDistProp = serializedObject.FindProperty("data.nature.faunaDistribution");
            if (faunaDistProp != null)
            {
                EditorGUILayout.PropertyField(faunaDistProp, new GUIContent("Fauna Distribution"), true);
            }
            var resItemsProp = serializedObject.FindProperty("data.nature.resourceItems");
            if (resItemsProp != null)
            {
                EditorGUILayout.PropertyField(resItemsProp, new GUIContent("Resource Items"), true);
            }

            // Draw history tab
            if (session.data.history == null) session.data.history = new UnpopulatedHistoryTab();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("History", EditorStyles.boldLabel);
            var histNotesProp = serializedObject.FindProperty("data.history.notes");
            if (histNotesProp != null) EditorGUILayout.PropertyField(histNotesProp, true);
            var timelineProp = serializedObject.FindProperty("data.history.timelineEntries");
            if (timelineProp != null) EditorGUILayout.PropertyField(timelineProp, true);

            // Draw culture tab (used primarily by Ruins)
            if (session.data.culture == null) session.data.culture = new UnpopulatedCultureTab();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Culture", EditorStyles.boldLabel);
            var cultureNotesProp = serializedObject.FindProperty("data.culture.notes");
            if (cultureNotesProp != null) EditorGUILayout.PropertyField(cultureNotesProp, true);
            var peoplesProp = serializedObject.FindProperty("data.culture.peoples");
            if (peoplesProp != null) EditorGUILayout.PropertyField(peoplesProp, true);
            var factionsProp = serializedObject.FindProperty("data.culture.factions");
            if (factionsProp != null) EditorGUILayout.PropertyField(factionsProp, true);
            var languagesProp = serializedObject.FindProperty("data.culture.languages");
            if (languagesProp != null) EditorGUILayout.PropertyField(languagesProp, true);
            var customsProp = serializedObject.FindProperty("data.culture.customs");
            if (customsProp != null) EditorGUILayout.PropertyField(customsProp, true);
            var rumorsProp = serializedObject.FindProperty("data.culture.rumors");
            if (rumorsProp != null) EditorGUILayout.PropertyField(rumorsProp, true);

            // Cultural composition section for unpopulated areas. Allows multiple cultures
            // with fractional populations to be defined. Persisted in the session's
            // culturalComposition list and serialized to JSON.
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Cultural Composition", EditorStyles.boldLabel);
            if (session.culturalComposition == null) session.culturalComposition = new System.Collections.Generic.List<CultureCompositionEntry>();
            var culturesList = WorldDataChoicesCache.GetCultures();
            for (int i = 0; i < session.culturalComposition.Count; i++)
            {
                var entry = session.culturalComposition[i];
                if (entry == null)
                {
                    entry = new CultureCompositionEntry();
                    session.culturalComposition[i] = entry;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    int idx = GetIndexById(culturesList, entry.cultureId, 0);
                    var chosen = WorldAuthoringEditorUI.PopupChoice($"Culture {i + 1}", culturesList, ref idx);
                    if (chosen != null && entry.cultureId != chosen.id)
                    {
                        entry.cultureId = chosen.id;
                        EditorUtility.SetDirty(session);
                    }
                    float pct = EditorGUILayout.FloatField("%", entry.percentage, GUILayout.MaxWidth(80));
                    if (!Mathf.Approximately(pct, entry.percentage))
                    {
                        entry.percentage = pct;
                        EditorUtility.SetDirty(session);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        session.culturalComposition.RemoveAt(i);
                        EditorUtility.SetDirty(session);
                        i--;
                        continue;
                    }
                }
            }
            if (culturesList != null && culturesList.Count > 0)
            {
                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Build dropdown arrays with a (select) option at index 0
                    string[] addLabels = WorldAuthoringEditorUI.BuildPopupLabelsWithNone(culturesList, "Select Culture…");
                    string[] addIds = WorldAuthoringEditorUI.BuildPopupIdsWithNone(culturesList);

                    _addCultureIndex = Mathf.Clamp(_addCultureIndex, 0, addLabels.Length - 1);
                    int selected = EditorGUILayout.Popup("Add Culture", _addCultureIndex, addLabels);
                    _addCultureIndex = selected;

                    _addCulturePercentage = EditorGUILayout.FloatField("%", _addCulturePercentage, GUILayout.MaxWidth(80));

                    using (new EditorGUI.DisabledScope(selected <= 0))
                    {
                        if (GUILayout.Button("Add", GUILayout.Width(60)))
                        {
                            string cultureId = addIds[selected];
                            bool exists = false;
                            foreach (var entry in session.culturalComposition)
                            {
                                if (entry != null && entry.cultureId == cultureId)
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (!exists)
                            {
                                session.culturalComposition.Add(new CultureCompositionEntry
                                {
                                    cultureId = cultureId,
                                    percentage = _addCulturePercentage
                                });
                                _addCultureIndex = 0;
                                _addCulturePercentage = 0f;
                                EditorUtility.SetDirty(session);
                            }
                        }
                    }
                }
            }

            // Draw water tab (used for water subtype)
            if (session.data.water == null) session.data.water = new UnpopulatedWaterTab();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Water (if subtype is Water)", EditorStyles.boldLabel);
            // Water body type dropdown
            string currentWaterBody = session.data.water.waterBodyType;
            _waterBodyIndex = Array.IndexOf(WaterBodyOptions, currentWaterBody);
            if (_waterBodyIndex < 0) _waterBodyIndex = 0;
            int selectedBody = EditorGUILayout.Popup("Water Body Type", _waterBodyIndex, WaterBodyOptions);
            if (selectedBody != _waterBodyIndex)
            {
                session.data.water.waterBodyType = WaterBodyOptions[selectedBody];
                _waterBodyIndex = selectedBody;
                EditorUtility.SetDirty(session);
            }
            // Water type dropdown
            string currentWaterType = session.data.water.waterType;
            _waterTypeIndex = Array.IndexOf(WaterTypeOptions, currentWaterType);
            if (_waterTypeIndex < 0) _waterTypeIndex = 0;
            int selectedType = EditorGUILayout.Popup("Water Type", _waterTypeIndex, WaterTypeOptions);
            if (selectedType != _waterTypeIndex)
            {
                session.data.water.waterType = WaterTypeOptions[selectedType];
                _waterTypeIndex = selectedType;
                EditorUtility.SetDirty(session);
            }
            // Depth, currents, hazards, notable features, notes
            var depthProp = serializedObject.FindProperty("data.water.depth");
            if (depthProp != null) EditorGUILayout.PropertyField(depthProp, true);
            var currentsProp = serializedObject.FindProperty("data.water.currents");
            if (currentsProp != null) EditorGUILayout.PropertyField(currentsProp, true);
            var hazardsProp = serializedObject.FindProperty("data.water.hazards");
            if (hazardsProp != null) EditorGUILayout.PropertyField(hazardsProp, true);
            var featuresProp = serializedObject.FindProperty("data.water.notableFeatures");
            if (featuresProp != null) EditorGUILayout.PropertyField(featuresProp, true);
            var waterNotesProp = serializedObject.FindProperty("data.water.notes");
            if (waterNotesProp != null) EditorGUILayout.PropertyField(waterNotesProp, true);

            // Apply modifications
            serializedObject.ApplyModifiedProperties();
        }

        // Helper to find index of an entry by id for culture dropdowns in this editor.
        private static int GetIndexById(IReadOnlyList<WorldDataIndexEntry> list, string id, int fallback)
        {
            if (list == null || list.Count == 0) return 0;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i].id, id, StringComparison.Ordinal)) return i;
                }
            }
            return Mathf.Clamp(fallback, 0, list.Count - 1);
        }
    }

    // ============================================================
    // Culture
    // ============================================================
    [CustomEditor(typeof(CultureAuthoringSession))]
    public sealed class CultureAuthoringSessionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Cultures are no longer edited directly via individual CultureAuthoringSession
            // instances.  Present the underlying data in a read‑only inspector and inform
            // the user to use the Culture Catalog for modifications.  Disabling the GUI
            // prevents accidental edits from being applied to culture objects.
            EditorGUILayout.HelpBox(
                "Cultures are edited via the main Culture Catalog. This view is read‑only.",
                MessageType.Info);

            bool previousState = GUI.enabled;
            GUI.enabled = false;
            DrawDefaultInspector();
            GUI.enabled = previousState;
        }
    }

    // ============================================================
    // Men-at-Arms Catalogue
    // ============================================================
    [CustomEditor(typeof(MenAtArmsCatalogAuthoringSession))]
    public sealed class MenAtArmsCatalogAuthoringSessionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector for the Men-at-Arms catalog. Creation of entries is handled here.
            DrawDefaultInspector();
        }
    }
}
#endif
