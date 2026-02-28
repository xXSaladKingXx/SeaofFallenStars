using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared editor UI helpers and session custom inspectors.
/// This file defines the editor UI for the world authoring tools.  It has been
/// updated to support the expanded settlement model, including levy tax
/// terminology, councillor salaries, additional economy resources, and
/// aggregated army statistics.  The editor hides or exposes fields based on
/// whether the current settlement has vassals and whether it has a liege or
/// serves as a liege’s capital.  When vassals exist, certain fields become
/// read‑only or are derived from the capital settlement.  New UI panels are
/// provided for editing councillor salaries when there are no vassals.
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
    // Editor for SettlementAuthoringSession.  Allows editing of settlement data including feudal hierarchy,
    // culture and demographics, army and economy.  This editor has been extended to support levy tax,
    // councillor salaries, new resource fields and derived army stats.
    private int _rulerPick;
    private int _liegePick;
    private int _addVassalPick;
    private int _addArmyPick;
    // Culture and language state
    private readonly List<int> _culturePickIndices = new List<int>();
    private int _addCulturePick;
    private int _primaryLanguagePick;
    private readonly List<bool> _languageSelections = new List<bool>();
    // Race distribution state
    private readonly List<int> _racePickIndices = new List<int>();
    private int _addRacePick;
    private int _religionPick;
    // Capital selection index
    private int _capitalPick;
    // Additional fields for councillor salary editing
    private int _addCouncillorPick;
    private int _addCouncillorSalary;

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

        // Flag to track whether any modifications have been made during this GUI pass.
        bool changed = false;

        // Ensure feudal mode is enabled so that liege and vassal options are available.  Some
        // UI elements may be disabled if this flag is false.  If it is currently false,
        // automatically enable it and record an undo so the user can revert this change.
        if (s.data.feudal != null && !s.data.feudal.isFeudal)
        {
            Undo.RecordObject(s, "Enable Feudal");
            s.data.feudal.isFeudal = true;
            changed = true;
        }

        // Draw basic settlement identifiers.  The settlement ID is displayed but not editable to avoid
        // breaking references in lieges and vassals.  Only the display name can be edited.
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        string currentId = s.data.settlementId ?? string.Empty;
        // Display the settlement ID in a read-only label so it cannot be changed inadvertently.
        EditorGUILayout.LabelField("Settlement ID", string.IsNullOrEmpty(currentId) ? "(no id)" : currentId);
        string currentName = s.data.displayName ?? string.Empty;
        string newName = EditorGUILayout.TextField("Display Name", currentName);
        if (newName != currentName)
        {
            Undo.RecordObject(s, "Set Display Name");
            s.data.displayName = newName;
            changed = true;
        }

        var characters = WorldDataChoicesCache.GetCharacters();
        var settlements = WorldDataChoicesCache.GetSettlements();
        var armies = WorldDataChoicesCache.GetArmies();

        // Build quick lookup maps
        var characterNameById = characters.ToDictionary(e => e.id, e => e.displayName);
        var settlementNameById = settlements.ToDictionary(e => e.id, e => e.displayName);
        var armyNameById = armies.ToDictionary(e => e.id, e => e.displayName);

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

        // Feudal section.  Combine the former "Feudal Hierarchy" and "Liege" panels into one cohesive Feudal panel.
        WorldAuthoringEditorUI.DrawHelpersHeader("Feudal");

        // Liege selection: show a dropdown of settlements with a "(none)" option.
        _liegePick = WorldAuthoringEditorUI.GetIndexByIdWithNone(settlements, s.data.liegeSettlementId, _liegePick);
        var liegeSelection = WorldAuthoringEditorUI.PopupChoiceWithNone("Liege", settlements, ref _liegePick, "(none)");
        string newLiegeId = liegeSelection?.id;
        if (s.data.liegeSettlementId != newLiegeId)
        {
            Undo.RecordObject(s, "Set Liege");
            s.data.liegeSettlementId = newLiegeId;
            // Mirror on the feudal object for backwards compatibility
            if (s.data.feudal != null)
            {
                s.data.feudal.liegeSettlementId = newLiegeId;
            }
            changed = true;
        }

        // Determine whether this settlement is the capital of its liege.  If so, do not show contract editing fields.
        bool isCapitalForLiege = false;
        if (!string.IsNullOrWhiteSpace(s.data.liegeSettlementId))
        {
            var allSettlements = WorldDataChoicesCache.GetSettlements();
            var liegeEntry = allSettlements != null
                ? allSettlements.FirstOrDefault(ent => ent != null && string.Equals(ent.id, s.data.liegeSettlementId, StringComparison.OrdinalIgnoreCase))
                : null;
            if (liegeEntry != null && !string.IsNullOrWhiteSpace(liegeEntry.filePath) && File.Exists(liegeEntry.filePath))
            {
                try
                {
                    var jsonText = File.ReadAllText(liegeEntry.filePath);
                    var liegeRoot = JObject.Parse(jsonText);
                    // Check both root-level and feudal nested capital ID fields.
                    var capId = liegeRoot.Value<string>("capitalSettlementId");
                    if (string.IsNullOrWhiteSpace(capId))
                    {
                        var feudalObj = liegeRoot["feudal"] as JObject;
                        capId = feudalObj?.Value<string>("capitalSettlementId");
                    }
                    if (!string.IsNullOrWhiteSpace(capId) && string.Equals(capId, s.data.settlementId, StringComparison.OrdinalIgnoreCase))
                    {
                        isCapitalForLiege = true;
                    }
                }
                catch
                {
                    // ignore parsing errors
                }
            }
        }
        // When a liege is selected, expose editable contract fields only if this settlement is not the liege's capital.
        if (!string.IsNullOrWhiteSpace(s.data.liegeSettlementId) && !isCapitalForLiege)
        {
            float incomeRate = s.data.feudal?.incomeTaxRate ?? 0f;
            float newIncomeRate = EditorGUILayout.Slider("Income Tax Rate", incomeRate, 0f, 1f);
            if (!Mathf.Approximately(newIncomeRate, incomeRate))
            {
                Undo.RecordObject(s, "Set Income Tax Rate");
                s.data.feudal.incomeTaxRate = newIncomeRate;
                changed = true;
            }

            // Levy tax rate (formerly troop tax rate)
            float levyRate = s.data.feudal?.levyTaxRate ?? 0f;
            float newLevyRate = EditorGUILayout.Slider("Levy Tax Rate", levyRate, 0f, 1f);
            if (!Mathf.Approximately(newLevyRate, levyRate))
            {
                Undo.RecordObject(s, "Set Levy Tax Rate");
                s.data.feudal.levyTaxRate = newLevyRate;
                changed = true;
            }

            string terms = s.data.feudal?.contractTerms ?? string.Empty;
            string newTerms = EditorGUILayout.TextField("Contract Terms", terms);
            if (newTerms != terms)
            {
                Undo.RecordObject(s, "Set Contract Terms");
                s.data.feudal.contractTerms = newTerms;
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

        // Determine whether this settlement has any vassals.  Use non-blank check to avoid false positives.
        bool hasVassalsLocal = s.data.main != null && s.data.main.vassals != null &&
            s.data.main.vassals.Any(id => !string.IsNullOrWhiteSpace(id));

        // Council: static council positions (castellan, marshall, steward, diplomat, spymaster, head priest)
        WorldAuthoringEditorUI.DrawHelpersHeader("Council");
        {
            string id = s.data.feudal.castellanCharacterId;
            bool c = DrawCharacterIdFieldWithNone(s, "Castellan", characters, ref id);
            if (c)
            {
                s.data.feudal.castellanCharacterId = id;
                changed = true;
            }
        }
        {
            string id = s.data.feudal.marshallCharacterId;
            bool c = DrawCharacterIdFieldWithNone(s, "Marshall", characters, ref id);
            if (c)
            {
                s.data.feudal.marshallCharacterId = id;
                changed = true;
            }
        }
        {
            string id = s.data.feudal.stewardCharacterId;
            bool c = DrawCharacterIdFieldWithNone(s, "Steward", characters, ref id);
            if (c)
            {
                s.data.feudal.stewardCharacterId = id;
                changed = true;
            }
        }
        {
            string id = s.data.feudal.diplomatCharacterId;
            bool c = DrawCharacterIdFieldWithNone(s, "Diplomat", characters, ref id);
            if (c)
            {
                s.data.feudal.diplomatCharacterId = id;
                changed = true;
            }
        }
        {
            string id = s.data.feudal.spymasterCharacterId;
            bool c = DrawCharacterIdFieldWithNone(s, "Spymaster", characters, ref id);
            if (c)
            {
                s.data.feudal.spymasterCharacterId = id;
                changed = true;
            }
        }
        {
            string id = s.data.feudal.headPriestCharacterId;
            bool c = DrawCharacterIdFieldWithNone(s, "Head Priest", characters, ref id);
            if (c)
            {
                s.data.feudal.headPriestCharacterId = id;
                changed = true;
            }
        }

        // Councillor salary editor: only when the settlement has no vassals.  If vassals exist, the councillors are
        // inherited from the capital settlement and salaries cannot be edited directly.
        if (!hasVassalsLocal)
        {
            WorldAuthoringEditorUI.DrawHelpersHeader("Councillor Salaries");
            if (s.data.feudal.councillorSalaries == null)
                s.data.feudal.councillorSalaries = new List<SettlementInfoData.CouncillorSalaryEntry>();
            var salaryList = s.data.feudal.councillorSalaries;
            for (int i = 0; i < salaryList.Count; i++)
            {
                var entry = salaryList[i];
                if (entry == null)
                {
                    entry = new SettlementInfoData.CouncillorSalaryEntry();
                    salaryList[i] = entry;
                }
                EditorGUILayout.BeginHorizontal();
                // Select councillor character
                int pickIdx = WorldAuthoringEditorUI.GetIndexByIdWithNone(characters, entry.characterId, 0);
                var selectedChar = WorldAuthoringEditorUI.PopupChoiceWithNone("Councillor", characters, ref pickIdx, "(none)");
                string newCid = selectedChar?.id;
                if (newCid != entry.characterId)
                {
                    Undo.RecordObject(s, "Change Councillor");
                    entry.characterId = newCid;
                    changed = true;
                }
                // Salary field
                // Councillor salary is stored as a float in the data model.  For editing convenience, use an integer field.
                int sal = (int)entry.salary;
                int newSal = EditorGUILayout.IntField("Salary", sal);
                if (newSal != sal)
                {
                    Undo.RecordObject(s, "Change Councillor Salary");
                    entry.salary = newSal;
                    changed = true;
                }
                // Remove button
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(s, "Remove Councillor Salary");
                    salaryList.RemoveAt(i);
                    changed = true;
                    EditorGUILayout.EndHorizontal();
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();
            }
            // Add new councillor entry
            EditorGUILayout.BeginHorizontal();
            _addCouncillorPick = Mathf.Clamp(_addCouncillorPick, 0, characters.Count);
            var newCouncillor = WorldAuthoringEditorUI.PopupChoiceWithNone("Add Councillor", characters, ref _addCouncillorPick, "(none)");
            // Salary input for new councillor
            _addCouncillorSalary = EditorGUILayout.IntField("Salary", _addCouncillorSalary);
            if (newCouncillor != null && GUILayout.Button("Add", GUILayout.Width(60)))
            {
                // Only add if the character ID is not already present in the list to avoid duplicates
                if (!salaryList.Any(cs => string.Equals(cs?.characterId, newCouncillor.id, StringComparison.OrdinalIgnoreCase)))
                {
                    Undo.RecordObject(s, "Add Councillor Salary");
                    salaryList.Add(new SettlementInfoData.CouncillorSalaryEntry { characterId = newCouncillor.id, salary = _addCouncillorSalary });
                    changed = true;
                    _addCouncillorPick = 0;
                    _addCouncillorSalary = 0;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

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
        // Normalize army list and update summary
        var normalizedArmies = armyIds.Distinct(StringComparer.Ordinal).ToArray();
        if (!Enumerable.SequenceEqual(normalizedArmies, s.data.army.armyIds ?? Array.Empty<string>(), StringComparer.Ordinal))
        {
            Undo.RecordObject(s, "Update Army List");
            s.data.army.armyIds = normalizedArmies;
            changed = true;
        }

        // Culture distribution and languages: show editing controls only when there are no vassals
        if (!hasVassalsLocal)
        {
            WorldAuthoringEditorUI.DrawHelpersHeader("Culture Distribution");
            if (s.data.cultural.cultureDistribution == null)
                s.data.cultural.cultureDistribution = new List<PercentEntry>();
            if (s.data.cultural.languages == null)
                s.data.cultural.languages = Array.Empty<string>();
            while (_culturePickIndices.Count < s.data.cultural.cultureDistribution.Count)
                _culturePickIndices.Add(0);
            while (_culturePickIndices.Count > s.data.cultural.cultureDistribution.Count)
                _culturePickIndices.RemoveAt(_culturePickIndices.Count - 1);
            var cultureEntries = WorldDataChoicesCache.GetCultureEntries();
            for (int i = 0; i < s.data.cultural.cultureDistribution.Count; i++)
            {
                var entry = s.data.cultural.cultureDistribution[i];
                if (entry == null)
                {
                    entry = new PercentEntry { key = null, percent = 0f };
                    s.data.cultural.cultureDistribution[i] = entry;
                }
                // Before rendering the culture picker, ensure the cached index matches the current entry key.
                if (!string.IsNullOrWhiteSpace(entry.key))
                {
                    for (int j = 0; j < cultureEntries.Count; j++)
                    {
                        var ce = cultureEntries[j];
                        if (ce != null && string.Equals(ce.id, entry.key, StringComparison.OrdinalIgnoreCase))
                        {
                            _culturePickIndices[i] = j;
                            break;
                        }
                    }
                }
                EditorGUILayout.BeginHorizontal();
                int culturePickIdx = _culturePickIndices[i];
                var selectedCulture = WorldAuthoringEditorUI.PopupChoice("Culture", cultureEntries, ref culturePickIdx);
                if (culturePickIdx != _culturePickIndices[i])
                {
                    _culturePickIndices[i] = culturePickIdx;
                }
                var newKey = selectedCulture?.id;
                if (entry.key != newKey)
                {
                    Undo.RecordObject(s, "Change Culture Entry");
                    entry.key = newKey;
                    changed = true;
                }
                float percent = EditorGUILayout.FloatField(entry.percent);
                percent = Mathf.Clamp(percent, 0f, 100f);
                if (!Mathf.Approximately(percent, entry.percent))
                {
                    Undo.RecordObject(s, "Change Culture Percent");
                    entry.percent = percent;
                    changed = true;
                }
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(s, "Remove Culture Entry");
                    s.data.cultural.cultureDistribution.RemoveAt(i);
                    _culturePickIndices.RemoveAt(i);
                    changed = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            _addCulturePick = Mathf.Clamp(_addCulturePick, 0, cultureEntries.Count - 1);
            EditorGUILayout.BeginHorizontal();
            var addCultureEntry = WorldAuthoringEditorUI.PopupChoice("Add culture", cultureEntries, ref _addCulturePick);
            if (addCultureEntry != null && GUILayout.Button("Add", GUILayout.Width(60)))
            {
                Undo.RecordObject(s, "Add Culture Entry");
                s.data.cultural.cultureDistribution.Add(new PercentEntry { key = addCultureEntry.id, percent = 0f });
                _culturePickIndices.Add(_addCulturePick);
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
            // Remove automatic normalization of culture distribution percentages.  Users may enter percentages directly.
            if (s.data.cultural.cultureDistribution.Count > 0)
            {
                var topEntry = s.data.cultural.cultureDistribution.OrderByDescending(e => e.percent).FirstOrDefault();
                if (topEntry != null && !string.IsNullOrWhiteSpace(topEntry.key))
                {
                    string derivedRel = DeriveReligionFromCulture(topEntry.key);
                    if (!string.IsNullOrWhiteSpace(derivedRel) && s.data.cultural.religion != derivedRel)
                    {
                        Undo.RecordObject(s, "Derive Religion");
                        s.data.cultural.religion = derivedRel;
                        changed = true;
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(s.data.cultural.culture))
            {
                string derivedRel = DeriveReligionFromCulture(s.data.cultural.culture);
                if (!string.IsNullOrWhiteSpace(derivedRel) && s.data.cultural.religion != derivedRel)
                {
                    Undo.RecordObject(s, "Derive Religion");
                    s.data.cultural.religion = derivedRel;
                    changed = true;
                }
            }
            WorldAuthoringEditorUI.DrawHelpersHeader("Languages");
            var languageDefs = WorldDataChoicesCache.GetLanguageDefinitions();
            while (_languageSelections.Count < languageDefs.Count) _languageSelections.Add(false);
            while (_languageSelections.Count > languageDefs.Count) _languageSelections.RemoveAt(_languageSelections.Count - 1);
            if (s.data.cultural.languages != null && s.data.cultural.languages.Length > 0)
            {
                string currentPrimaryId = s.data.cultural.languages[0];
                int idx = 0;
                for (int i = 0; i < languageDefs.Count; i++)
                {
                    if (string.Equals(languageDefs[i].id, currentPrimaryId, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
                _primaryLanguagePick = idx;
            }
            var primaryLangEntry = WorldAuthoringEditorUI.PopupChoice("Primary Language", languageDefs, ref _primaryLanguagePick);
            string newPrimaryId = primaryLangEntry?.id;
            List<string> selectedLangs = new List<string>();
            if (!string.IsNullOrWhiteSpace(newPrimaryId)) selectedLangs.Add(newPrimaryId);
            EditorGUILayout.LabelField("Secondary Languages", EditorStyles.boldLabel);
            for (int i = 0; i < languageDefs.Count; i++)
            {
                if (i == _primaryLanguagePick) continue;
                string langId = languageDefs[i].id;
                bool curSel = s.data.cultural.languages != null && s.data.cultural.languages.Skip(1).Contains(langId);
                bool newSel = EditorGUILayout.ToggleLeft(languageDefs[i].displayName ?? langId, curSel);
                if (newSel != curSel)
                {
                    changed = true;
                }
                _languageSelections[i] = newSel;
            }
            for (int i = 0; i < languageDefs.Count; i++)
            {
                if (i == _primaryLanguagePick) continue;
                if (_languageSelections[i])
                {
                    selectedLangs.Add(languageDefs[i].id);
                }
            }
            s.data.cultural.languages = selectedLangs.ToArray();
        }

        // Race distribution and religion selection: only available when there are no vassals.
        if (!hasVassalsLocal)
        {
            WorldAuthoringEditorUI.DrawHelpersHeader("Race Distribution");
            if (s.data.cultural.raceDistribution == null)
                s.data.cultural.raceDistribution = new List<PercentEntry>();
            while (_racePickIndices.Count < s.data.cultural.raceDistribution.Count)
                _racePickIndices.Add(0);
            while (_racePickIndices.Count > s.data.cultural.raceDistribution.Count)
                _racePickIndices.RemoveAt(_racePickIndices.Count - 1);
            var raceDefs = WorldDataChoicesCache.GetRaceDefinitions();
            for (int i = 0; i < s.data.cultural.raceDistribution.Count; i++)
            {
                var entry = s.data.cultural.raceDistribution[i];
                if (entry == null)
                {
                    entry = new PercentEntry { key = null, percent = 0f };
                    s.data.cultural.raceDistribution[i] = entry;
                }
                int currentPick = 0;
                if (!string.IsNullOrWhiteSpace(entry.key))
                {
                    for (int j = 0; j < raceDefs.Count; j++)
                    {
                        var rd = raceDefs[j];
                        if (rd != null && string.Equals(rd.id, entry.key, StringComparison.OrdinalIgnoreCase))
                        {
                            currentPick = j + 1;
                            break;
                        }
                    }
                }
                _racePickIndices[i] = currentPick;
                EditorGUILayout.BeginHorizontal();
                int pick = _racePickIndices[i];
                var raceEntry = WorldAuthoringEditorUI.PopupChoiceWithNone("Race", raceDefs, ref pick, "(none)");
                if (pick != _racePickIndices[i])
                    _racePickIndices[i] = pick;
                string newRaceKey = null;
                if (pick > 0 && pick - 1 < raceDefs.Count)
                {
                    var def = raceDefs[pick - 1];
                    newRaceKey = def?.id;
                }
                if (!string.Equals(entry.key, newRaceKey, StringComparison.Ordinal))
                {
                    Undo.RecordObject(s, "Change Race Entry");
                    entry.key = newRaceKey;
                    changed = true;
                }
                float racePercent = EditorGUILayout.FloatField(entry.percent);
                racePercent = Mathf.Clamp(racePercent, 0f, 100f);
                if (!Mathf.Approximately(racePercent, entry.percent))
                {
                    Undo.RecordObject(s, "Change Race Percent");
                    entry.percent = racePercent;
                    changed = true;
                }
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(s, "Remove Race Entry");
                    s.data.cultural.raceDistribution.RemoveAt(i);
                    _racePickIndices.RemoveAt(i);
                    changed = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            _addRacePick = Mathf.Clamp(_addRacePick, 0, raceDefs.Count - 1);
            EditorGUILayout.BeginHorizontal();
            var addRaceEntry = WorldAuthoringEditorUI.PopupChoice("Add race", raceDefs, ref _addRacePick);
            if (addRaceEntry != null && GUILayout.Button("Add", GUILayout.Width(60)))
            {
                Undo.RecordObject(s, "Add Race Entry");
                s.data.cultural.raceDistribution.Add(new PercentEntry { key = addRaceEntry.id, percent = 0f });
                _racePickIndices.Add(_addRacePick + 1);
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
            // Religion selection
            WorldAuthoringEditorUI.DrawHelpersHeader("Religion");
            var religionDefs = WorldDataChoicesCache.GetReligionDefinitions();
            if (religionDefs != null && religionDefs.Count > 0)
            {
                int currentRelIndex = 0;
                if (!string.IsNullOrWhiteSpace(s.data.cultural.religion))
                {
                    for (int i = 0; i < religionDefs.Count; i++)
                    {
                        if (string.Equals(religionDefs[i]?.id, s.data.cultural.religion, StringComparison.OrdinalIgnoreCase))
                        {
                            currentRelIndex = i;
                            break;
                        }
                    }
                }
                _religionPick = currentRelIndex;
                var chosenRel = WorldAuthoringEditorUI.PopupChoice("Religion", religionDefs, ref _religionPick);
                string newRelId = chosenRel?.id;
                if (s.data.cultural.religion != newRelId)
                {
                    Undo.RecordObject(s, "Change Religion");
                    s.data.cultural.religion = newRelId;
                    changed = true;
                }
            }
        }

        // Capital and aggregated stats display for settlements with vassals
        if (hasVassalsLocal)
        {
            // Capital selection from among vassals
            var vassalEntries = new List<WorldDataIndexEntry>();
            foreach (var vid in s.data.main.vassals)
            {
                if (string.IsNullOrWhiteSpace(vid)) continue;
                string nm = settlementNameById.TryGetValue(vid, out var disp) ? disp : vid;
                vassalEntries.Add(new WorldDataIndexEntry { id = vid, displayName = nm });
            }
            if (vassalEntries.Count > 0)
            {
                int capIndex = 0;
                if (!string.IsNullOrWhiteSpace(s.data.feudal?.capitalSettlementId))
                {
                    for (int i = 0; i < vassalEntries.Count; i++)
                    {
                        if (string.Equals(vassalEntries[i].id, s.data.feudal.capitalSettlementId, StringComparison.OrdinalIgnoreCase))
                        {
                            capIndex = i;
                            break;
                        }
                    }
                }
                _capitalPick = capIndex;
                WorldAuthoringEditorUI.DrawHelpersHeader("Capital");
                var capitalChoice = WorldAuthoringEditorUI.PopupChoice("Capital Settlement", vassalEntries, ref _capitalPick);
                string newCapId = capitalChoice?.id;
                if (s.data.feudal.capitalSettlementId != newCapId)
                {
                    Undo.RecordObject(s, "Set Capital");
                    s.data.feudal.capitalSettlementId = newCapId;
                    s.data.capitalSettlementId = newCapId;
                    changed = true;
                }
            }
            // Recompute aggregated stats using SettlementStatsCache
            SettlementStatsCache.Invalidate();
            var stats = SettlementStatsCache.GetStatsOrNull(s.data.settlementId);
            if (stats != null)
            {
                WorldAuthoringEditorUI.DrawHelpersHeader("Aggregated Totals (derived)");
                EditorGUILayout.LabelField("Total Population", stats.totalPopulation.ToString());
                EditorGUILayout.LabelField("Gross Income", stats.grossIncome.ToString("F1"));
                EditorGUILayout.LabelField("Income Tax Paid Up", stats.incomePaidUp.ToString("F1"));
                EditorGUILayout.LabelField("Net Income", stats.netIncome.ToString("F1"));
                EditorGUILayout.LabelField("Gross Troops", stats.grossTroops.ToString());
                // Label renamed to Levies Tax Paid Up to reflect levy terminology
                EditorGUILayout.LabelField("Levies Tax Paid Up", stats.troopsPaidUp.ToString());
                EditorGUILayout.LabelField("Net Troops", stats.netTroops.ToString());
                // Aggregated demographics
                if (stats.totalPopulation > 0)
                {
                    EditorGUILayout.LabelField("Races", EditorStyles.boldLabel);
                    var allRaceDefs = WorldDataChoicesCache.GetRaceDefinitions();
                    var raceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var def in allRaceDefs)
                    {
                        if (def != null && !string.IsNullOrWhiteSpace(def.id))
                            raceMap[def.id] = def.displayName;
                    }
                    foreach (var kv in stats.populationByRace.OrderByDescending(kv => kv.Value))
                    {
                        float pct = stats.totalPopulation > 0 ? (float)kv.Value / stats.totalPopulation * 100f : 0f;
                        string rn = raceMap.TryGetValue(kv.Key, out var disp) ? disp : kv.Key;
                        EditorGUILayout.LabelField($"- {rn} : {pct:F1}%");
                    }
                }
                if (stats.totalPopulation > 0)
                {
                    EditorGUILayout.LabelField("Cultures", EditorStyles.boldLabel);
                    var cultDefs = WorldDataChoicesCache.GetCultureEntries();
                    var cultMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var def in cultDefs)
                    {
                        if (def != null && !string.IsNullOrWhiteSpace(def.id))
                            cultMap[def.id] = def.displayName;
                    }
                    foreach (var kv in stats.populationByCulture.OrderByDescending(kv => kv.Value))
                    {
                        float pct = stats.totalPopulation > 0 ? (float)kv.Value / stats.totalPopulation * 100f : 0f;
                        string cn = cultMap.TryGetValue(kv.Key, out var disp) ? disp : kv.Key;
                        EditorGUILayout.LabelField($"- {cn} : {pct:F1}%");
                    }
                }
                if (stats.totalPopulation > 0)
                {
                    EditorGUILayout.LabelField("Religions", EditorStyles.boldLabel);
                    var relDefs = WorldDataChoicesCache.GetReligionDefinitions();
                    var relMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var def in relDefs)
                    {
                        if (def != null && !string.IsNullOrWhiteSpace(def.id))
                            relMap[def.id] = def.displayName;
                    }
                    foreach (var kv in stats.populationByReligion.OrderByDescending(kv => kv.Value))
                    {
                        float pct = stats.totalPopulation > 0 ? (float)kv.Value / stats.totalPopulation * 100f : 0f;
                        string rn = relMap.TryGetValue(kv.Key, out var disp) ? disp : kv.Key;
                        EditorGUILayout.LabelField($"- {rn} : {pct:F1}%");
                    }
                }
                if (stats.populationByLanguage.Count > 0)
                {
                    EditorGUILayout.LabelField("Languages", EditorStyles.boldLabel);
                    var langDefs = WorldDataChoicesCache.GetLanguageDefinitions();
                    var langMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var def in langDefs)
                    {
                        if (def != null && !string.IsNullOrWhiteSpace(def.id))
                            langMap[def.id] = def.displayName;
                    }
                    foreach (var kv in stats.populationByLanguage)
                    {
                        string ln = langMap.TryGetValue(kv.Key, out var disp) ? disp : kv.Key;
                        EditorGUILayout.LabelField($"- {ln}");
                    }
                }
            }
        }

        // Recalculate derived army summary after modifications
        RecalculateDerivedArmySummary(s);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Total Army (Derived)", s.data.army.totalArmy);
            EditorGUILayout.TextField("Primary Commander (Derived)", s.data.army.primaryCommanderDisplayName ?? string.Empty);
            // Additional derived army fields
            EditorGUILayout.IntField("Total Levies (Derived)", s.data.army.totalLevies);
            EditorGUILayout.FloatField("Raised Maintenance Costs (Derived)", s.data.army.raisedMaintenanceCosts);
            EditorGUILayout.FloatField("Unraised Maintenance Costs (Derived)", s.data.army.unraisedMaintenanceCosts);
            EditorGUILayout.FloatField("Attack (Derived)", s.data.army.attack);
            EditorGUILayout.FloatField("Defense (Derived)", s.data.army.defense);
            EditorGUILayout.FloatField("Speed (Derived)", s.data.army.speed);
            // Display derived knights as names
            var knightLabels = new List<string>();
            if (s.data.army.knightCharacterIds != null)
            {
                foreach (var kId in s.data.army.knightCharacterIds)
                {
                    if (string.IsNullOrWhiteSpace(kId)) continue;
                    knightLabels.Add(characterNameById.TryGetValue(kId, out var dnm) ? dnm : kId);
                }
            }
            EditorGUILayout.LabelField("Knights (Derived)", knightLabels.Count > 0 ? string.Join(", ", knightLabels) : "(none)");
        }

        // Update serializedObject, skip fields accordingly.
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
            "data.army.totalLevies",
            "data.army.raisedMaintenanceCosts",
            "data.army.unraisedMaintenanceCosts",
            "data.army.attack",
            "data.army.defense",
            "data.army.speed",
            "data.army.knightCharacterIds",
            "data.cultural.languages",
            "data.cultural.raceDistribution",
            "data.cultural.cultureDistribution",
            "data.cultural.religion",
            "data.feudal.liegeSettlementId",
            "data.feudal.incomeTaxRate",
            // skip the old troop tax property (alias) to prevent editing it directly
            "data.feudal.troopTaxRate",
            "data.feudal.levyTaxRate",
            "data.feudal.contractTerms",
            "data.feudal.councillorSalaries"
        };
        // Hide economy fields when this settlement has vassals.  When vassals exist,
        // economy statistics are derived and should not be edited directly.  When
        // there are no vassals, these fields remain editable and are therefore not skipped.
        if (hasVassalsLocal)
        {
            skip.Add("data.economy.mainExports");
            skip.Add("data.economy.mainImports");
            skip.Add("data.economy.mainIndustries");
            skip.Add("data.economy.notes");
            skip.Add("data.economy.totalIncomePerMonth");
            skip.Add("data.economy.totalTreasury");
            skip.Add("data.economy.totalProfitPerMonth");
            skip.Add("data.economy.courtExpenses");
            skip.Add("data.economy.armyExpenses");
            skip.Add("data.economy.wheat");
            skip.Add("data.economy.bread");
            skip.Add("data.economy.meat");
            skip.Add("data.economy.wood");
            skip.Add("data.economy.stone");
            skip.Add("data.economy.iron");
            skip.Add("data.economy.steel");
            skip.Add("data.economy.currentlyConstructing");
        }
        var prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (skip.Contains(prop.propertyPath)) continue;
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
        string[] armyIds = s.data.army.armyIds ?? Array.Empty<string>();
        int total = 0;
        HashSet<string> menAtArmsIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> knightIdsAgg = new HashSet<string>(StringComparer.Ordinal);
        int totalLevies = 0;
        float raisedMaint = 0f;
        float unraisedMaint = 0f;
        float attackSum = 0f;
        float defenseSum = 0f;
        float speedSum = 0f;
        int speedCount = 0;
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
            // men-at-arms
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
            // Knights list (from extra field)
            if (armyJson["knightCharacterIds"] is JArray knightsArr)
            {
                foreach (var kid in knightsArr)
                {
                    if (kid == null) continue;
                    string sKid = kid.ToString();
                    if (!string.IsNullOrWhiteSpace(sKid)) knightIdsAgg.Add(sKid.Trim());
                }
            }
            // Total levies
            totalLevies += armyJson.Value<int?>("totalLevies") ?? 0;
            // Maintenance costs and stats
            raisedMaint += armyJson.Value<float?>("raisedMaintenanceCosts") ?? 0f;
            unraisedMaint += armyJson.Value<float?>("unraisedMaintenanceCosts") ?? 0f;
            attackSum += armyJson.Value<float?>("attack") ?? 0f;
            defenseSum += armyJson.Value<float?>("defense") ?? 0f;
            float spd = armyJson.Value<float?>("speed") ?? 0f;
            if (spd > 0f)
            {
                speedSum += spd;
                speedCount++;
            }
            // Primary commander selection
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
        // Assign aggregated values to the settlement's ArmyTab
        s.data.army.knightCharacterIds = knightIdsAgg.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        s.data.army.totalLevies = totalLevies;
        s.data.army.raisedMaintenanceCosts = raisedMaint;
        s.data.army.unraisedMaintenanceCosts = unraisedMaint;
        s.data.army.attack = attackSum;
        s.data.army.defense = defenseSum;
        s.data.army.speed = speedCount > 0 ? speedSum / speedCount : 0f;
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

    private static string DeriveReligionFromCulture(string cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId)) return null;
        var cultures = WorldDataChoicesCache.GetCultureEntries();
        var entry = cultures.FirstOrDefault(e => e != null && string.Equals(e.id, cultureId, StringComparison.OrdinalIgnoreCase));
        if (entry == null || string.IsNullOrWhiteSpace(entry.filePath)) return null;
        try
        {
            var jsonText = File.ReadAllText(entry.filePath);
            var jo = JObject.Parse(jsonText);
            var rel = jo["religions"] as JArray;
            if (rel != null && rel.Count > 0)
                return (string)rel[0];
            var cArray = jo["cultures"] as JArray;
            if (cArray != null)
            {
                foreach (var c in cArray)
                {
                    if (c is JObject obj && string.Equals((string)obj["id"], cultureId, StringComparison.OrdinalIgnoreCase))
                    {
                        var r = obj["religions"] as JArray;
                        if (r != null && r.Count > 0) return (string)r[0];
                    }
                }
            }
        }
        catch { }
        return null;
    }
}