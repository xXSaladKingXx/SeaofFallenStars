using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Backing JSON model for Settlement data.
/// </summary>
[Serializable]
public class SettlementInfoData
{
    /// <summary>
    /// Indicates whether this settlement is populated.  Some features in the UI and
    /// hierarchy calculators ignore settlements marked as unpopulated.
    /// </summary>
    [JsonProperty("isPopulated")] public bool isPopulated;

    /// <summary>
    /// Optional list of character IDs associated with this settlement.  This list lives at
    /// the root for convenience when resolving characters outside of the main tab.
    /// </summary>
    [JsonProperty("characterIds")] public string[] characterIds = Array.Empty<string>();
    [JsonProperty("settlementId")] public string settlementId;
    [JsonProperty("displayName")] public string displayName;
    [JsonProperty("rulerCharacterId")] public string rulerCharacterId;
    [JsonProperty("liegeSettlementId")] public string liegeSettlementId;
    [JsonProperty("mapUrlOrPath")] public string mapUrlOrPath;

    /// <summary>
    /// The settlement that acts as the capital for this feudal domain.
    /// Kept at the root level for backwards compatibility; also mirrored in the feudal tab.
    /// </summary>
    [JsonProperty("capitalSettlementId")]
    public string capitalSettlementId;

    [JsonProperty("main")] public MainTab main = new MainTab();
    [JsonProperty("army")] public ArmyTab army = new ArmyTab();
    [JsonProperty("economy")] public EconomyTab economy = new EconomyTab();
    [JsonProperty("cultural")] public CulturalTab cultural = new CulturalTab();
    [JsonProperty("history")] public SettlementHistoryTab history = new SettlementHistoryTab();
    [JsonProperty("feudal")] public SettlementFeudalData feudal = new SettlementFeudalData();

    #region Tabs

    [Serializable]
    public class MainTab
    {
        [JsonProperty("description")] public string description;
        [JsonProperty("notableFacts")] public List<string> notableFacts = new List<string>();
        [JsonProperty("population")] public int population = 0;
        [JsonProperty("rulerDisplayName")] public string rulerDisplayName;

        /// <summary>
        /// Alternative display name for the settlement ruler.  Some UIs prefer this over
        /// <see cref="rulerDisplayName"/> if provided.
        /// </summary>
        [JsonProperty("rulerName")] public string rulerName;
        [JsonProperty("vassals")] public string[] vassals = Array.Empty<string>();
        [JsonProperty("characterIds")] public string[] characterIds = Array.Empty<string>();
    }

    [Serializable]
    public class ArmyTab
    {
        // List of armies attached to this settlement. All other army fields in this tab are derived.
        [JsonProperty("armyIds")] public string[] armyIds = Array.Empty<string>();

        [JsonProperty("totalArmy")] public int totalArmy = 0;
        [JsonProperty("menAtArms")] public string[] menAtArms = Array.Empty<string>();
        [JsonProperty("primaryCommanderDisplayName")] public string primaryCommanderDisplayName;
        [JsonProperty("primaryCommanderCharacterId")] public string primaryCommanderCharacterId;
    }

    [Serializable]
    public class EconomyTab
    {
        [JsonProperty("mainExports")] public string mainExports;
        [JsonProperty("mainImports")] public string mainImports;
        [JsonProperty("mainIndustries")] public string mainIndustries;
        [JsonProperty("notes")] public string notes;

        /// <summary>
        /// Total income generated per month by this settlement.  Used for economy tab and
        /// realm statistics calculations.
        /// </summary>
        [JsonProperty("totalIncomePerMonth")] public float totalIncomePerMonth;

        /// <summary>
        /// Total treasury holdings of this settlement.  Displayed in the economy tab.
        /// </summary>
        [JsonProperty("totalTreasury")] public float totalTreasury;

        /// <summary>
        /// Summary of court expenses, expressed as a string for flexibility.  This could
        /// contain multiple categories separated by commas or other delimiters.
        /// </summary>
        [JsonProperty("courtExpenses")] public string courtExpenses;

        /// <summary>
        /// Summary of army expenses, expressed as a string for flexibility.
        /// </summary>
        [JsonProperty("armyExpenses")] public string armyExpenses;

        /// <summary>
        /// List of projects currently under construction in this settlement.
        /// </summary>
        [JsonProperty("currentlyConstructing")] public string[] currentlyConstructing = Array.Empty<string>();
    }

    [Serializable]
    public class CulturalTab
    {
        [JsonProperty("culture")] public string culture;
        [JsonProperty("raceDistribution")] public List<PercentEntry> raceDistribution = new List<PercentEntry>();
        [JsonProperty("religion")] public string religion;
        [JsonProperty("traits")] public string[] traits = Array.Empty<string>();
        [JsonProperty("languages")] public string[] languages = Array.Empty<string>();
        [JsonProperty("customs")] public string[] customs = Array.Empty<string>();

        /// <summary>
        /// A textual description of how the population is distributed (e.g., urban vs rural).
        /// </summary>
        [JsonProperty("populationDistribution")] public string populationDistribution;

        /// <summary>
        /// High-level list of primary traits that characterise this settlement.  Distinct
        /// from <see cref="traits"/> which may contain granular descriptors.
        /// </summary>
        [JsonProperty("primaryTraits")] public string[] primaryTraits = Array.Empty<string>();

        /// <summary>
        /// Distribution of cultures represented within this settlement.  Each entry
        /// contains a culture key and its percentage share of the population.
        /// </summary>
        [JsonProperty("cultureDistribution")] public List<PercentEntry> cultureDistribution = new List<PercentEntry>();
    }

    [Serializable]
    public class SettlementHistoryTab
    {
        [JsonProperty("notes")] public string notes;

        /// <summary>
        /// List of timeline events for this settlement.  Stored as an array for
        /// convenient access to the Length property in UI code.
        /// </summary>
        [JsonProperty("timelineEntries")] public TimelineEntry[] timelineEntries = Array.Empty<TimelineEntry>();

        /// <summary>
        /// List of names of the ruling family members for this settlement.  Used by the
        /// history UI.
        /// </summary>
        [JsonProperty("rulingFamilyMembers")] public string[] rulingFamilyMembers = Array.Empty<string>();
    }

    [Serializable]
    public class SettlementFeudalData
    {
        [JsonProperty("castellanCharacterId")] public string castellanCharacterId;
        [JsonProperty("marshallCharacterId")] public string marshallCharacterId;
        [JsonProperty("stewardCharacterId")] public string stewardCharacterId;
        [JsonProperty("diplomatCharacterId")] public string diplomatCharacterId;
        [JsonProperty("spymasterCharacterId")] public string spymasterCharacterId;
        [JsonProperty("headPriestCharacterId")] public string headPriestCharacterId;
        [JsonProperty("vassalContracts")] public List<VassalContractData> vassalContracts = new List<VassalContractData>();

        /// <summary>
        /// Indicates whether this settlement participates in the feudal system.  Used by
        /// authoring tools to enable or disable feudal features.
        /// </summary>
        [JsonProperty("feudal")] public bool isFeudal;

        /// <summary>
        /// The liege of this feudal domain.  Provided here for backwards compatibility
        /// with older JSON data that nests the liege under the feudal object.
        /// </summary>
        [JsonProperty("liegeSettlementId")] public string liegeSettlementId;

        /// <summary>
        /// Arbitrary notes about the feudal arrangement.
        /// </summary>
        [JsonProperty("notes")] public string notes;

        /// <summary>
        /// Text describing the laws or legal framework governing this feudal domain.  In
        /// the realm management UI this text is edited directly by the user.
        /// </summary>
        [JsonProperty("laws")] public string laws;

        /// <summary>
        /// The settlement that acts as the capital for this feudal domain.
        /// Mirrors the root-level property for convenience.
        /// </summary>
        [JsonProperty("capitalSettlementId")]
        public string capitalSettlementId;
    }

    [Serializable]
    public class VassalContractData
    {
        [JsonProperty("vassalSettlementId")] public string vassalSettlementId;
        [JsonProperty("incomeTaxRate")] public float incomeTaxRate;
        [JsonProperty("troopTaxRate")] public float troopTaxRate;
        [JsonProperty("terms")] public string terms;
    }

    #endregion
}