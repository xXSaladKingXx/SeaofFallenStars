using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared editor UI helpers and session custom inspectors.
/// </summary>
public static class WorldAuthoringEditorUI
{
    public static void DrawHelpersHeader(string title)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    public static string[] BuildPopupLabelsWithNone(IReadOnlyList<WorldDataIndexEntry> list, string noneLabel)
    {
        int count = list?.Count ?? 0;
        string[] labels = new string[count + 1];
        labels[0] = string.IsNullOrWhiteSpace(noneLabel) ? "(none)" : noneLabel;
        for (int i = 0; i < count; i++)
        {
            var entry = list[i];
            string name = entry != null && !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : "(unnamed)";
            string id = entry != null ? entry.id : string.Empty;
            labels[i + 1] = string.IsNullOrWhiteSpace(id) ? name : $"{name} ({id})";
        }
        return labels;
    }

    public static int GetIndexByIdWithNone(IReadOnlyList<WorldDataIndexEntry> list, string id, int fallbackIndex)
    {
        int count = list?.Count ?? 0;
        if (count <= 0) return 0;
        if (string.IsNullOrWhiteSpace(id)) return 0;

        for (int i = 0; i < count; i++)
        {
            if (string.Equals(list[i]?.id, id, StringComparison.Ordinal))
                return i + 1;
        }

        // Clamp fallback to [0..count]
        if (fallbackIndex < 0) return 0;
        if (fallbackIndex > count) return count;
        return fallbackIndex;
    }

    public static WorldDataIndexEntry PopupChoiceWithNone(string label, IReadOnlyList<WorldDataIndexEntry> list, ref int pickIndex, string noneLabel)
    {
        if (list == null) list = Array.Empty<WorldDataIndexEntry>();

        string[] labels = BuildPopupLabelsWithNone(list, noneLabel);
        pickIndex = Mathf.Clamp(pickIndex, 0, labels.Length - 1);
        pickIndex = EditorGUILayout.Popup(label, pickIndex, labels);

        if (pickIndex <= 0) return null;
        int listIndex = pickIndex - 1;
        if (listIndex < 0 || listIndex >= list.Count) return null;
        return list[listIndex];
    }

    public static WorldDataIndexEntry PopupChoice(string label, IReadOnlyList<WorldDataIndexEntry> list, ref int pickIndex)
    {
        if (list == null) list = Array.Empty<WorldDataIndexEntry>();

        string[] labels = new string[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            var entry = list[i];
            string name = entry != null && !string.IsNullOrWhiteSpace(entry.displayName) ? entry.displayName : "(unnamed)";
            string id = entry != null ? entry.id : string.Empty;
            labels[i] = string.IsNullOrWhiteSpace(id) ? name : $"{name} ({id})";
        }

        if (labels.Length == 0)
        {
            EditorGUILayout.Popup(label, 0, new[] { "(none available)" });
            return null;
        }

        pickIndex = Mathf.Clamp(pickIndex, 0, labels.Length - 1);
        pickIndex = EditorGUILayout.Popup(label, pickIndex, labels);
        return (pickIndex >= 0 && pickIndex < list.Count) ? list[pickIndex] : null;
    }
}

[CustomEditor(typeof(SettlementAuthoringSession))]
public sealed class SettlementAuthoringSessionEditor : Editor
{
    private int _rulerPick;
    private int _liegePick;
    private int _addVassalPick;
    private int _addArmyPick;

    public override void OnInspectorGUI()
    {
        var s = (SettlementAuthoringSession)target;
        if (s == null)
        {
            DrawDefaultInspector();
            return;
        }

        // Ensure nested objects exist (avoid null refs in inspector)
        if (s.data == null) s.data = new SettlementInfoData();
        if (s.data.main == null) s.data.main = new SettlementInfoData.MainTab();
        if (s.data.army == null) s.data.army = new SettlementInfoData.ArmyTab();
        if (s.data.economy == null) s.data.economy = new SettlementInfoData.EconomyTab();
        if (s.data.cultural == null) s.data.cultural = new SettlementInfoData.CulturalTab();
        if (s.data.history == null) s.data.history = new SettlementInfoData.SettlementHistoryTab();
        if (s.data.feudal == null) s.data.feudal = new SettlementInfoData.SettlementFeudalData();

        var characters = WorldDataChoicesCache.GetCharacters();
        var settlements = WorldDataChoicesCache.GetSettlements();
        var armies = WorldDataChoicesCache.GetArmies();

        // Build quick lookup maps
        var characterNameById = characters.ToDictionary(e => e.id, e => e.displayName);
        var settlementNameById = settlements.ToDictionary(e => e.id, e => e.displayName);
        var armyNameById = armies.ToDictionary(e => e.id, e => e.displayName);

        bool changed = false;

        WorldAuthoringEditorUI.DrawHelpersHeader("Feudal Hierarchy");

        // Ruler (allow blank)
        _rulerPick = WorldAuthoringEditorUI.GetIndexByIdWithNone(characters, s.data.rulerCharacterId, _rulerPick);
        var rulerEntry = WorldAuthoringEditorUI.PopupChoiceWithNone("Ruler", characters, ref _rulerPick, "(none)");
        if (rulerEntry == null)
        {
            if (!string.IsNullOrEmpty(s.data.rulerCharacterId) || !string.IsNullOrEmpty(s.data.main.rulerDisplayName))
            {
                Undo.RecordObject(s, "Clear Ruler");
                s.data.rulerCharacterId = null;
                s.data.main.rulerDisplayName = null;
                changed = true;
            }
        }
        else if (s.data.rulerCharacterId != rulerEntry.id)
        {
            Undo.RecordObject(s, "Set Ruler");
            s.data.rulerCharacterId = rulerEntry.id;
            s.data.main.rulerDisplayName = rulerEntry.displayName;
            changed = true;
        }

        // Liege (allow blank)
        bool hasLiegeToggle = !string.IsNullOrWhiteSpace(s.data.liegeSettlementId);
        bool hasLiegeNew = EditorGUILayout.Toggle("Has Liege", hasLiegeToggle);
        if (!hasLiegeNew)
        {
            if (!string.IsNullOrWhiteSpace(s.data.liegeSettlementId))
            {
                Undo.RecordObject(s, "Clear Liege");
                s.data.liegeSettlementId = null;
                changed = true;
            }
        }
        else
        {
            _liegePick = WorldAuthoringEditorUI.GetIndexByIdWithNone(settlements, s.data.liegeSettlementId, _liegePick);
            var liegeEntry = WorldAuthoringEditorUI.PopupChoiceWithNone("Liege Settlement", settlements, ref _liegePick, "(none)");
            string newLiegeId = liegeEntry?.id;
            if (s.data.liegeSettlementId != newLiegeId)
            {
                Undo.RecordObject(s, "Set Liege");
                s.data.liegeSettlementId = newLiegeId;
                changed = true;
            }
        }

        // Vassals (list)
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Vassals", EditorStyles.boldLabel);

        var vassals = new List<string>((s.data.main.vassals ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        for (int i = 0; i < vassals.Count; i++)
        {
            string id = vassals[i];
            string label = settlementNameById.TryGetValue(id, out var nm) ? $"{nm} ({id})" : id;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                Undo.RecordObject(s, "Remove Vassal");
                vassals.RemoveAt(i);
                i--;
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        // Add vassal
        _addVassalPick = Mathf.Clamp(_addVassalPick, 0, Mathf.Max(0, settlements.Count));
        var addVassalEntry = WorldAuthoringEditorUI.PopupChoiceWithNone("Add Vassal", settlements, ref _addVassalPick, "(none)");
        if (addVassalEntry != null)
        {
            if (!vassals.Contains(addVassalEntry.id))
            {
                Undo.RecordObject(s, "Add Vassal");
                vassals.Add(addVassalEntry.id);
                changed = true;
            }
        }

        if (changed)
        {
            s.data.main.vassals = vassals.Distinct(StringComparer.Ordinal).ToArray();
        }

        // Council
        WorldAuthoringEditorUI.DrawHelpersHeader("Council");
        changed |= DrawCharacterIdFieldWithNone(s, "Castellan", characters, ref s.data.feudal.castellanCharacterId);
        changed |= DrawCharacterIdFieldWithNone(s, "Marshall", characters, ref s.data.feudal.marshallCharacterId);
        changed |= DrawCharacterIdFieldWithNone(s, "Steward", characters, ref s.data.feudal.stewardCharacterId);
        changed |= DrawCharacterIdFieldWithNone(s, "Diplomat", characters, ref s.data.feudal.diplomatCharacterId);
        changed |= DrawCharacterIdFieldWithNone(s, "Spymaster", characters, ref s.data.feudal.spymasterCharacterId);
        changed |= DrawCharacterIdFieldWithNone(s, "Head Priest", characters, ref s.data.feudal.headPriestCharacterId);

        // Armies
        WorldAuthoringEditorUI.DrawHelpersHeader("Armies");

        var armyIds = new List<string>((s.data.army.armyIds ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

        for (int i = 0; i < armyIds.Count; i++)
        {
            string id = armyIds[i];
            string label = armyNameById.TryGetValue(id, out var nm) ? $"{nm} ({id})" : id;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                Undo.RecordObject(s, "Remove Army");
                armyIds.RemoveAt(i);
                i--;
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        _addArmyPick = Mathf.Clamp(_addArmyPick, 0, Mathf.Max(0, armies.Count));
        var addArmyEntry = WorldAuthoringEditorUI.PopupChoiceWithNone("Add Army", armies, ref _addArmyPick, "(none)");
        if (addArmyEntry != null)
        {
            if (!armyIds.Contains(addArmyEntry.id))
            {
                Undo.RecordObject(s, "Add Army");
                armyIds.Add(addArmyEntry.id);
                changed = true;
            }
        }

        // Normalize and re-derive summary
        var normalized = armyIds.Distinct(StringComparer.Ordinal).ToArray();
        if (!Enumerable.SequenceEqual(normalized, s.data.army.armyIds ?? Array.Empty<string>(), StringComparer.Ordinal))
        {
            Undo.RecordObject(s, "Update Army List");
            s.data.army.armyIds = normalized;
            changed = true;
        }

        // Always re-derive in inspector so the user sees totals without needing to save.
        RecalculateDerivedArmySummary(s);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Total Army (Derived)", s.data.army.totalArmy);
            EditorGUILayout.TextField("Primary Commander (Derived)", s.data.army.primaryCommanderDisplayName ?? string.Empty);
        }

        // Draw the rest, avoiding duplicate fields we handle explicitly above.
        serializedObject.Update();

        HashSet<string> skip = new HashSet<string>
        {
            "m_Script",
            "data.rulerCharacterId",
            "data.liegeSettlementId",
            "data.main.rulerDisplayName",
            "data.main.vassals",
            "data.feudal.castellanCharacterId",
            "data.feudal.marshallCharacterId",
            "data.feudal.stewardCharacterId",
            "data.feudal.diplomatCharacterId",
            "data.feudal.spymasterCharacterId",
            "data.feudal.headPriestCharacterId",
            "data.army.armyIds",
            "data.army.totalArmy",
            "data.army.menAtArms",
            "data.army.primaryCommanderDisplayName",
            "data.army.primaryCommanderCharacterId",
        };

        var prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (skip.Contains(prop.propertyPath)) continue;
            // Draw without children; NextVisible() will respect foldout states and draw child fields
            // on subsequent iterations (avoids duplicate child rendering).
            EditorGUILayout.PropertyField(prop, false);
        }

        serializedObject.ApplyModifiedProperties();

        if (changed)
        {
            EditorUtility.SetDirty(s);
        }
    }

    private static bool DrawCharacterIdFieldWithNone(SettlementAuthoringSession s, string label, IReadOnlyList<WorldDataIndexEntry> characters, ref string characterId)
    {
        int idx = WorldAuthoringEditorUI.GetIndexByIdWithNone(characters, characterId, 0);
        int picked = EditorGUILayout.Popup(label, idx, WorldAuthoringEditorUI.BuildPopupLabelsWithNone(characters, "(none)"));
        string newId = picked <= 0 ? null : characters[picked - 1].id;
        if (characterId != newId)
        {
            Undo.RecordObject(s, $"Set {label}");
            characterId = newId;
            return true;
        }
        return false;
    }

    private static void RecalculateDerivedArmySummary(SettlementAuthoringSession s)
    {
        if (s == null || s.data == null) return;
        if (s.data.army == null) s.data.army = new SettlementInfoData.ArmyTab();

        // Load referenced armies and aggregate.
        string[] armyIds = s.data.army.armyIds ?? Array.Empty<string>();
        int total = 0;
        HashSet<string> menAtArmsIds = new HashSet<string>(StringComparer.Ordinal);

        string chosenCommanderId = null;
        string chosenCommanderName = null;
        int bestTotal = int.MinValue;

        foreach (string rawId in armyIds)
        {
            string id = string.IsNullOrWhiteSpace(rawId) ? null : rawId.Trim();
            if (string.IsNullOrEmpty(id)) continue;

            if (!TryLoadArmyJson(id, out JObject armyJson))
                continue;

            int armyTotal = armyJson.Value<int?>("totalArmy") ?? 0;
            if (armyTotal < 0) armyTotal = 0;
            total += armyTotal;

            // Support both "menAtArms" and "menAtArmsStacks" army schemas.
            var menToken = armyJson["menAtArms"] ?? armyJson["menAtArmsStacks"];
            if (menToken is JArray menArray)
            {
                foreach (var el in menArray)
                {
                    if (el is JObject obj)
                    {
                        string menId = obj.Value<string>("menAtArmsId") ?? obj.Value<string>("id");
                        if (!string.IsNullOrWhiteSpace(menId)) menAtArmsIds.Add(menId.Trim());
                    }
                    else if (el != null && el.Type == JTokenType.String)
                    {
                        string menId = el.ToString();
                        if (!string.IsNullOrWhiteSpace(menId)) menAtArmsIds.Add(menId.Trim());
                    }
                }
            }

            if (armyTotal > bestTotal)
            {
                bestTotal = armyTotal;
                chosenCommanderId = armyJson.Value<string>("primaryCommanderCharacterId");
                chosenCommanderName = armyJson.Value<string>("primaryCommanderDisplayName");
            }
        }

        s.data.army.totalArmy = total;
        s.data.army.menAtArms = menAtArmsIds.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        s.data.army.primaryCommanderCharacterId = string.IsNullOrWhiteSpace(chosenCommanderId) ? null : chosenCommanderId;
        s.data.army.primaryCommanderDisplayName = string.IsNullOrWhiteSpace(chosenCommanderName) ? null : chosenCommanderName;
    }

    private static bool TryLoadArmyJson(string armyId, out JObject armyRoot)
    {
        armyRoot = null;
        if (string.IsNullOrWhiteSpace(armyId)) return false;

        string fileName = armyId.Trim();
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            fileName += ".json";

        string primaryDir = Application.isEditor ? DataPaths.Editor_ArmiesPath : DataPaths.Runtime_ArmiesPath;
        string secondaryDir = Application.isEditor ? DataPaths.Runtime_ArmiesPath : DataPaths.Editor_ArmiesPath;

        string primaryPath = Path.Combine(primaryDir, fileName);
        string secondaryPath = Path.Combine(secondaryDir, fileName);

        string json = null;
        if (File.Exists(primaryPath)) json = File.ReadAllText(primaryPath);
        else if (File.Exists(secondaryPath)) json = File.ReadAllText(secondaryPath);

        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            armyRoot = JObject.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
