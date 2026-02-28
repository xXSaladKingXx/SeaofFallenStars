using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Import the Zana.WorldAuthoring namespace so that WorldDataAuthoringSessionBase and related
// authoring classes are available without specifying a fully qualified name.  In the original
// project, SettlementAuthoringSession derived from WorldDataAuthoringSessionBase which is
// defined in the Zana.WorldAuthoring namespace.
using Zana.WorldAuthoring;

/// <summary>
/// Authoring session wrapper for Settlement JSON.
/// 
/// Key behavior:
/// - Settlement's Army and Economy tabs are derived from referenced Army JSON objects and
///   internal councillor salary lists.  The settlement stores only armyIds and councillor salary
///   data; all other army and economy fields are re-generated on save.
/// - Levy tax rate is used in place of the old troop tax rate when calculating contract terms.
/// - This implementation extends the original authoring session to compute aggregated knights,
///   levies, maintenance costs, attack, defense, speed and profit for settlements based on
///   attached armies and councillor salaries.
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

        // Normalize and derive the Army and Economy tabs from referenced JSONs and councillor lists.
        RecalculateDerivedArmyTab();
        RecalculateDerivedEconomy();

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
        RecalculateDerivedEconomy();
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
        data.army.knightCharacterIds ??= Array.Empty<string>();

        data.cultural.traits ??= Array.Empty<string>();
        data.cultural.languages ??= Array.Empty<string>();
        data.cultural.customs ??= Array.Empty<string>();

        data.main.notableFacts ??= new List<string>();
        // Ensure the various distribution lists and arrays are initialised to avoid null reference errors.
        data.cultural.raceDistribution ??= new List<PercentEntry>();
        data.cultural.cultureDistribution ??= new List<PercentEntry>();
        data.cultural.primaryTraits ??= Array.Empty<string>();
        // timelineEntries is now an array rather than a List, so ensure an empty array instead of a List
        data.history.timelineEntries ??= Array.Empty<TimelineEntry>();
        data.history.rulingFamilyMembers ??= Array.Empty<string>();
        data.feudal.vassalContracts ??= new List<SettlementInfoData.VassalContractData>();
        // Ensure currently constructing projects is an empty array
        data.economy.currentlyConstructing ??= Array.Empty<string>();
        // Ensure councillor salary list exists
        if (data.feudal.councillorSalaries == null)
            data.feudal.councillorSalaries = new List<SettlementInfoData.CouncillorSalaryEntry>();
    }

    /// <summary>
    /// Derive the army tab values from attached army JSONs.  This method normalizes army IDs
    /// (trimming, removing blanks, de-duplicating) and aggregates totals for levies,
    /// maintenance costs, attack, defense, speed, and knights in addition to the existing
    /// total army and men-at-arms computations.
    /// </summary>
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
        int totalLevies = 0;
        float raisedCosts = 0f;
        float unraisedCosts = 0f;
        int totalAttack = 0;
        int totalDefense = 0;
        float maxSpeed = 20f;
        bool speedInitialized = false;

        HashSet<string> menAtArmsIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> knightIds = new HashSet<string>(StringComparer.Ordinal);

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

            // Levies and maintenance costs
            int levies = armyRoot.Value<int?>("totalLevies") ?? 0;
            totalLevies += levies;
            float raised = armyRoot.Value<float?>("raisedMaintenanceCosts") ?? 0f;
            float unraised = armyRoot.Value<float?>("unraisedMaintenanceCosts") ?? 0f;
            raisedCosts += raised;
            unraisedCosts += unraised;
            int attack = armyRoot.Value<int?>("attack") ?? 0;
            int defense = armyRoot.Value<int?>("defense") ?? 0;
            totalAttack += attack;
            totalDefense += defense;
            float spd = armyRoot.Value<float?>("speed") ?? 20f;
            if (!speedInitialized || spd > maxSpeed)
            {
                maxSpeed = spd;
                speedInitialized = true;
            }

            // Knights (array of character IDs)
            JToken knightsToken = armyRoot["knightCharacterIds"];
            if (knightsToken is JArray kArr)
            {
                foreach (var el in kArr)
                {
                    if (el != null && el.Type == JTokenType.String)
                    {
                        string kid = el.ToString();
                        if (!string.IsNullOrWhiteSpace(kid))
                            knightIds.Add(kid.Trim());
                    }
                }
            }

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
        data.army.totalLevies = totalLevies;
        data.army.raisedMaintenanceCosts = raisedCosts;
        data.army.unraisedMaintenanceCosts = unraisedCosts;
        data.army.attack = totalAttack;
        data.army.defense = totalDefense;
        data.army.speed = maxSpeed;
        data.army.knightCharacterIds = knightIds.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        data.army.menAtArms = menAtArmsIds.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        data.army.primaryCommanderCharacterId = string.IsNullOrWhiteSpace(commanderId) ? null : commanderId;
        data.army.primaryCommanderDisplayName = string.IsNullOrWhiteSpace(commanderName) ? null : commanderName;
    }

    /// <summary>
    /// Derive court and army expenses as well as profit for the economy tab.  Court expenses
    /// are computed as the sum of councillor salaries when this settlement has no vassals; for
    /// settlements with vassals the authoring tool should copy expenses from the capital
    /// settlement outside of this method.  Army expenses are the sum of raised and unraised
    /// maintenance costs from the aggregated army tab.  Profit is computed as income minus
    /// expenses.
    /// </summary>
    private void RecalculateDerivedEconomy()
    {
        if (data == null)
            return;
        data.economy ??= new SettlementInfoData.EconomyTab();
        data.feudal ??= new SettlementInfoData.SettlementFeudalData();

        // Determine if this settlement has direct vassals.
        bool hasVassals = data.main != null && data.main.vassals != null && data.main.vassals.Any(v => !string.IsNullOrWhiteSpace(v));

        // Compute court expenses.  When there are vassals, the capital's court expenses should be used.
        float courtExpenses = 0f;
        if (!hasVassals)
        {
            var salaries = data.feudal.councillorSalaries ?? new List<SettlementInfoData.CouncillorSalaryEntry>();
            foreach (var entry in salaries)
            {
                if (entry != null)
                    courtExpenses += entry.salary;
            }
        }
        // Otherwise leave courtExpenses unchanged (authoring UI may set it manually when using the capital's value).

        data.economy.courtExpenses = courtExpenses;

        // Compute army expenses from aggregated army stats.  This simple implementation sums raised and
        // unraised maintenance costs.  In a full game, whether armies are raised would depend on map state.
        float armyExpenses = data.army.raisedMaintenanceCosts + data.army.unraisedMaintenanceCosts;
        data.economy.armyExpenses = armyExpenses;

        // Compute profit: income minus expenses.  When totalIncomePerMonth is zero, profit can be negative.
        float income = data.economy.totalIncomePerMonth;
        data.economy.totalProfitPerMonth = income - (courtExpenses + armyExpenses);
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