using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Authoring session wrapper for Settlement JSON.
/// 
/// Key behavior:
/// - Settlement's Army tab is derived from referenced Army JSON objects.
/// - The settlement stores only armyIds (references). All other army fields are re-generated on save.
/// </summary>
public sealed class SettlementAuthoringSession : WorldDataAuthoringSessionBase
{
    public SettlementInfoData data = new SettlementInfoData();

    // Non-invasive / editor-only helper lists persisted in JSON at top-level.
    public List<CultureCompositionEntry> culturalComposition = new List<CultureCompositionEntry>();
    public List<RaceDistributionEntry> raceDistribution = new List<RaceDistributionEntry>();

    // Legacy public fields kept for compatibility with old inspectors (not injected into JSON anymore).
    public string castellanCharacterId;
    public string marshallCharacterId;
    public string stewardCharacterId;
    public string diplomatCharacterId;
    public string spymasterCharacterId;
    public string headPriestCharacterId;

    public override WorldDataCategory Category => WorldDataCategory.Settlement;

    public override string GetDefaultFileBaseName()
        => data != null && !string.IsNullOrWhiteSpace(data.settlementId)
            ? data.settlementId
            : "new_settlement";

    public override string BuildJson()
    {
        EnsureDataShape();

        // Normalize and derive the Army tab from referenced Army JSONs.
        RecalculateDerivedArmyTab();

        JObject root = JObject.FromObject(data);

        if (culturalComposition != null)
            root["culturalComposition"] = JArray.FromObject(culturalComposition);

        if (raceDistribution != null)
            root["raceDistribution"] = JArray.FromObject(raceDistribution);

        return root.ToString();
    }

    public override void ApplyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            data = new SettlementInfoData();
            culturalComposition = new List<CultureCompositionEntry>();
            raceDistribution = new List<RaceDistributionEntry>();
            EnsureDataShape();
            return;
        }

        JObject root = JObject.Parse(json);

        data = root.ToObject<SettlementInfoData>() ?? new SettlementInfoData();

        culturalComposition = root["culturalComposition"]?.ToObject<List<CultureCompositionEntry>>() ?? new List<CultureCompositionEntry>();
        raceDistribution = root["raceDistribution"]?.ToObject<List<RaceDistributionEntry>>() ?? new List<RaceDistributionEntry>();

        EnsureDataShape();

        // Keep derived fields consistent when loading older files.
        RecalculateDerivedArmyTab();
    }

    private void EnsureDataShape()
    {
        if (data == null)
            data = new SettlementInfoData();

        data.main ??= new SettlementInfoData.MainTab();
        data.army ??= new SettlementInfoData.ArmyTab();
        data.economy ??= new SettlementInfoData.EconomyTab();
        data.cultural ??= new SettlementInfoData.CulturalTab();
        data.history ??= new SettlementInfoData.SettlementHistoryTab();
        data.feudal ??= new SettlementInfoData.SettlementFeudalData();

        if (string.IsNullOrWhiteSpace(data.settlementId))
            data.settlementId = "new_settlement_" + Guid.NewGuid().ToString("N").Substring(0, 8);

        data.main.vassals ??= Array.Empty<string>();
        data.main.characterIds ??= Array.Empty<string>();

        data.army.armyIds ??= Array.Empty<string>();
        data.army.menAtArms ??= Array.Empty<string>();

        data.cultural.traits ??= Array.Empty<string>();
        data.cultural.languages ??= Array.Empty<string>();
        data.cultural.customs ??= Array.Empty<string>();

        data.main.notableFacts ??= new List<string>();
        data.cultural.raceDistribution ??= new List<PercentEntry>();
        data.history.timelineEntries ??= new List<TimelineEntry>();
        data.feudal.vassalContracts ??= new List<SettlementInfoData.VassalContractData>();
    }

    private void RecalculateDerivedArmyTab()
    {
        if (data == null)
            return;

        data.army ??= new SettlementInfoData.ArmyTab();

        // Normalize ids (trim, remove blanks, de-dupe)
        string[] idsRaw = data.army.armyIds ?? Array.Empty<string>();
        List<string> ids = idsRaw
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        data.army.armyIds = ids.ToArray();

        int totalArmy = 0;
        HashSet<string> menAtArmsIds = new HashSet<string>(StringComparer.Ordinal);

        int bestArmyTotal = int.MinValue;
        string commanderId = null;
        string commanderName = null;

        foreach (string armyId in ids)
        {
            if (!TryLoadArmyRoot(armyId, out JObject armyRoot))
                continue;

            int armyTotal = armyRoot.Value<int?>("totalArmy") ?? 0;
            if (armyTotal < 0) armyTotal = 0;
            totalArmy += armyTotal;

            // Men-at-arms IDs can be encoded either as strings (legacy) or as objects
            // [{ menAtArmsId: "...", units|count: N }, ...]
            // Support both "menAtArms" and "menAtArmsStacks" army schemas.
            JToken menToken = armyRoot["menAtArms"] ?? armyRoot["menAtArmsStacks"];
            if (menToken is JArray menArr)
            {
                foreach (JToken el in menArr)
                {
                    if (el is JObject menObj)
                    {
                        string menId = menObj.Value<string>("menAtArmsId")
                                       ?? menObj.Value<string>("id");
                        if (!string.IsNullOrWhiteSpace(menId))
                            menAtArmsIds.Add(menId.Trim());
                    }
                    else if (el.Type == JTokenType.String)
                    {
                        string menId = el.ToString();
                        if (!string.IsNullOrWhiteSpace(menId))
                            menAtArmsIds.Add(menId.Trim());
                    }
                }
            }

            // Primary commander: use the commander from the largest referenced army.
            if (armyTotal > bestArmyTotal)
            {
                bestArmyTotal = armyTotal;
                commanderId = armyRoot.Value<string>("primaryCommanderCharacterId");
                commanderName = armyRoot.Value<string>("primaryCommanderDisplayName");
            }
        }

        data.army.totalArmy = totalArmy;
        data.army.menAtArms = menAtArmsIds.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        data.army.primaryCommanderCharacterId = string.IsNullOrWhiteSpace(commanderId) ? null : commanderId;
        data.army.primaryCommanderDisplayName = string.IsNullOrWhiteSpace(commanderName) ? null : commanderName;
    }

    private static bool TryLoadArmyRoot(string armyId, out JObject root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(armyId))
            return false;

        string fileName = armyId.Trim();
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            fileName += ".json";

        // Prefer the path that matches the current runtime/editor context.
        string primaryDir = Application.isEditor ? DataPaths.Editor_ArmiesPath : DataPaths.Runtime_ArmiesPath;
        string secondaryDir = Application.isEditor ? DataPaths.Runtime_ArmiesPath : DataPaths.Editor_ArmiesPath;

        string primaryPath = Path.Combine(primaryDir, fileName);
        string secondaryPath = Path.Combine(secondaryDir, fileName);

        string json = null;
        if (File.Exists(primaryPath))
            json = File.ReadAllText(primaryPath);
        else if (File.Exists(secondaryPath))
            json = File.ReadAllText(secondaryPath);

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            root = JObject.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
