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
    // Culture and language state
    private readonly List<int> _culturePickIndices = new List<int>();
    private int _addCulturePick;
    private int _primaryLanguagePick;
    private readonly List<bool> _languageSelections = new List<bool>();

    // Track race selection indices for the race distribution editor.  Mirrors the culture
    // distribution state management.  Each element corresponds to one PercentEntry in
    // s.data.cultural.raceDistribution.
    private readonly List<int> _racePickIndices = new List<int>();
    // Index of the race to add when the user chooses from the "Add race" dropdown.
    private int _addRacePick;
    // Track the currently selected religion.  We do not persist this index; instead,
    // we look up the current religion id and compute the appropriate index on each
    // inspector draw.
    private int _religionPick;

    // Track selected capital index for capital dropdown
    private int _capitalPick;

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

        var characters = WorldDataChoicesCache.GetCharacters();
        var settlements = WorldDataChoicesCache.GetSettlements();
        var armies = WorldDataChoicesCache.GetArmies();

        // Build quick lookup maps
        var characterNameById = characters.ToDictionary(e => e.id, e => e.displayName);
        var settlementNameById = settlements.ToDictionary(e => e.id, e => e.displayName);
        var armyNameById = armies.ToDictionary(e => e.id, e => e.displayName);

        // 'changed' is declared above; do not re-declare here.

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

        // Liege (allow blank).  We first render a toggle to indicate whether this
        // settlement has a liege.  Changing the toggle will clear or enable
        // selection of a liege settlement.  We explicitly record undo for
        // toggling to preserve undo/redo state.
        bool currentHasLiege = !string.IsNullOrWhiteSpace(s.data.liegeSettlementId);
        bool newHasLiege = EditorGUILayout.Toggle("Has Liege", currentHasLiege);
        if (newHasLiege != currentHasLiege)
        {
            Undo.RecordObject(s, newHasLiege ? "Enable Liege" : "Disable Liege");
            if (!newHasLiege)
            {
                // Clearing the toggle removes the liege ID.
                s.data.liegeSettlementId = null;
            }
            changed = true;
        }
        if (newHasLiege)
        {
            // Show the settlement picker for selecting a liege.  Allow "(none)" to
            // represent no liege.  Retrieve the index based on the current
            // liegeSettlementId and assign the selection back to the data model.
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
        // We cannot pass properties or indexers by ref directly, so copy values to local variables,
        // invoke the helper, then assign back if changed.
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

        // Culture distribution and languages
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

            EditorGUILayout.BeginHorizontal();
            // Copy the indexer value to a local variable. We cannot pass a list indexer directly to a ref parameter.
            int culturePickIdx = _culturePickIndices[i];
            var selectedCulture = WorldAuthoringEditorUI.PopupChoice("Culture", cultureEntries, ref culturePickIdx);
            // Update the stored index if it changed
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

        if (s.data.cultural.cultureDistribution.Count > 0)
        {
            float sum = s.data.cultural.cultureDistribution.Sum(e => Mathf.Max(0f, e?.percent ?? 0f));
            if (sum > 0.0001f)
            {
                foreach (var e in s.data.cultural.cultureDistribution)
                {
                    float normalizedPct = Mathf.Max(0f, e?.percent ?? 0f) / sum * 100f;
                    if (!Mathf.Approximately(normalizedPct, e.percent))
                    {
                        e.percent = normalizedPct;
                        changed = true;
                    }
                }
            }
        }

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

        // -------------------------------------------------------------------------
        // Race distribution and religion selection.  For settlements without
        // vassals we expose full editing controls.  For settlements with vassals
        // these values are determined by the aggregated stats and are displayed
        // separately in the Aggregated Stats section.
        // -------------------------------------------------------------------------

        // Build a quick lookup for whether this settlement has direct vassals.  If
        // there are any vassals, we treat the local cultural demographics as the
        // capital stats and display aggregated values below.
        bool hasVassals = s.data.main != null && s.data.main.vassals != null && s.data.main.vassals.Length > 0;

        // Race distribution and religion can always be edited.  When the settlement
        // has vassals, these fields represent the capital settlement's base stats.
        // --- Race Distribution Editor ---
        WorldAuthoringEditorUI.DrawHelpersHeader("Race Distribution");
        if (s.data.cultural.raceDistribution == null)
            s.data.cultural.raceDistribution = new List<PercentEntry>();

        // Ensure the helper lists match the size of the distribution list.
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
            EditorGUILayout.BeginHorizontal();
            // Drop-down for race ID
            // Copy the pick index to a local variable so we don't pass a list indexer as ref.  
            int racePick = _racePickIndices[i];
            var selectedRace = WorldAuthoringEditorUI.PopupChoice("Race", raceDefs, ref racePick);
            // Write the updated pick index back to our list.
            if (racePick != _racePickIndices[i])
                _racePickIndices[i] = racePick;
            var newRaceKey = selectedRace?.id;
            if (entry.key != newRaceKey)
            {
                Undo.RecordObject(s, "Change Race Entry");
                entry.key = newRaceKey;
                changed = true;
            }
            // Percentage field (0–100)
            float racePercent = EditorGUILayout.FloatField(entry.percent);
            racePercent = Mathf.Clamp(racePercent, 0f, 100f);
            if (!Mathf.Approximately(racePercent, entry.percent))
            {
                Undo.RecordObject(s, "Change Race Percent");
                entry.percent = racePercent;
                changed = true;
            }
            // Remove button
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
        // Add race dropdown
        _addRacePick = Mathf.Clamp(_addRacePick, 0, raceDefs.Count - 1);
        EditorGUILayout.BeginHorizontal();
        var addRaceEntry = WorldAuthoringEditorUI.PopupChoice("Add race", raceDefs, ref _addRacePick);
        if (addRaceEntry != null && GUILayout.Button("Add", GUILayout.Width(60)))
        {
            Undo.RecordObject(s, "Add Race Entry");
            s.data.cultural.raceDistribution.Add(new PercentEntry { key = addRaceEntry.id, percent = 0f });
            _racePickIndices.Add(_addRacePick);
            changed = true;
        }
        EditorGUILayout.EndHorizontal();
        // Normalize race percentages to sum to 100
        if (s.data.cultural.raceDistribution.Count > 0)
        {
            float raceSum = 0f;
            foreach (var e in s.data.cultural.raceDistribution)
                raceSum += Mathf.Max(0f, e?.percent ?? 0f);
            if (raceSum > 0.0001f)
            {
                foreach (var e in s.data.cultural.raceDistribution)
                {
                    float norm = Mathf.Max(0f, e?.percent ?? 0f) / raceSum * 100f;
                    if (!Mathf.Approximately(norm, e.percent))
                    {
                        e.percent = norm;
                        changed = true;
                    }
                }
            }
        }

        // --- Religion Selection Editor ---
        WorldAuthoringEditorUI.DrawHelpersHeader("Religion");
        var religionDefs = WorldDataChoicesCache.GetReligionDefinitions();
        if (religionDefs != null && religionDefs.Count > 0)
        {
            // Determine current index based on the selected religion ID.
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
        // End race and religion editing

        // -------------------------------------------------------------------------
        // Aggregated stats and capital selection for settlements with vassals.
        // When a settlement has vassals, its economy, army and demographic stats
        // are derived from the capital settlement and aggregated from its vassals.
        // We therefore show non-editable totals along with editable base values from
        // the designated capital.  If there are no vassals, no aggregated stats are
        // shown.
        // -------------------------------------------------------------------------

        if (hasVassals)
        {
            // Capital selection.  Build a list of vassal entries for the dropdown.  We
            // rely on the settlement name lookup built earlier from the world data.
            var vassalEntries = new List<WorldDataIndexEntry>();
            foreach (var vid in s.data.main.vassals)
            {
                if (string.IsNullOrWhiteSpace(vid)) continue;
                string nm = settlementNameById.TryGetValue(vid, out var disp) ? disp : vid;
                vassalEntries.Add(new WorldDataIndexEntry { id = vid, displayName = nm });
            }
            if (vassalEntries.Count > 0)
            {
                // Determine the current capital index.  If the existing capital ID is not
                // among the vassal list, default to the first entry.
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
                    // Mirror the root-level capital field for backwards compatibility.
                    s.data.capitalSettlementId = newCapId;
                    changed = true;
                }
            }

            // Recompute aggregated stats using the SettlementStatsCache.  We call
            // Invalidate() before computing to ensure the cache is fresh; this
            // operation is safe but can be expensive if called repeatedly.  In
            // practice, authoring sessions are short-lived and the settlement tree
            // small, so this performance cost is acceptable.
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
                EditorGUILayout.LabelField("Troops Tax Paid Up", stats.troopsPaidUp.ToString());
                EditorGUILayout.LabelField("Net Troops", stats.netTroops.ToString());
                // Display aggregated race distribution.
                if (stats.totalPopulation > 0)
                {
                    EditorGUILayout.LabelField("Races", EditorStyles.boldLabel);
                    // Build display name lookup for races.  Use a distinct variable name to
                    // avoid colliding with the raceDefs defined in the race editor above.
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
                // Display aggregated culture distribution.
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
                // Display aggregated religions.
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
                // Display aggregated languages (unique list).
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

        // Recalculate army summary after modifications
        RecalculateDerivedArmySummary(s);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Total Army (Derived)", s.data.army.totalArmy);
            EditorGUILayout.TextField("Primary Commander (Derived)", s.data.army.primaryCommanderDisplayName ?? string.Empty);
        }

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
            "data.army.primaryCommanderCharacterId"
            ,"data.cultural.languages"
            ,"data.cultural.raceDistribution"
            ,"data.cultural.cultureDistribution"
            ,"data.cultural.religion"
        };
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