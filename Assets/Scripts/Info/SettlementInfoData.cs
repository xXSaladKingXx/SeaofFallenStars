using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Backing JSON model for Settlement data.
/// </summary>
/// <remarks>
/// This class has been updated to align the settlement data model with the latest
/// economy and army specifications.  Court and army expenses are now stored as
/// numeric values instead of strings, a profit field has been added, and
/// resources (wheat, bread, meat, wood, stone, iron, steel) can now be tracked
/// directly in the JSON.  The army tab includes new aggregate fields such as
/// knights, levies, maintenance costs and derived stats (attack, defense, speed).
/// The feudal tab uses a unified levy tax rate in place of the former troop tax
/// rate and supports councillor salaries.  A deprecated alias property for the
/// old troop tax rate remains for backwards compatibility.
/// </remarks>
// Remove namespace so SettlementInfoData is in the global namespace.  This aligns with existing
// project files where SettlementInfoData is referenced without a namespace.  The curly brace
// following this comment remains to wrap only the class definitions.

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
            /// <summary>
            /// List of armies attached to this settlement. All other army fields in this tab are derived.
            /// </summary>
            [JsonProperty("armyIds")] public string[] armyIds = Array.Empty<string>();

            /// <summary>
            /// Total army size (levies + men at arms) aggregated from all attached armies.
            /// </summary>
            [JsonProperty("totalArmy")] public int totalArmy = 0;

            /// <summary>
            /// Unique identifiers of men at arms units present in this settlement’s armies.
            /// </summary>
            [JsonProperty("menAtArms")] public string[] menAtArms = Array.Empty<string>();

            /// <summary>
            /// The display name of the commander of the largest attached army.  Derived during authoring.
            /// </summary>
            [JsonProperty("primaryCommanderDisplayName")] public string primaryCommanderDisplayName;

            /// <summary>
            /// The character ID of the commander of the largest attached army.  Derived during authoring.
            /// </summary>
            [JsonProperty("primaryCommanderCharacterId")] public string primaryCommanderCharacterId;

            /// <summary>
            /// List of knight character IDs present across all attached armies.  This is aggregated during authoring and read at runtime.
            /// </summary>
            [JsonProperty("knightCharacterIds")] public string[] knightCharacterIds = Array.Empty<string>();

            /// <summary>
            /// Total number of levies across all attached armies.  Derived during authoring.
            /// </summary>
            [JsonProperty("totalLevies")] public int totalLevies = 0;

            /// <summary>
            /// Combined monthly maintenance cost when armies are raised.  Derived during authoring.
            /// </summary>
            [JsonProperty("raisedMaintenanceCosts")] public float raisedMaintenanceCosts = 0f;

            /// <summary>
            /// Combined monthly maintenance cost when armies are unraised.  Derived during authoring.
            /// </summary>
            [JsonProperty("unraisedMaintenanceCosts")] public float unraisedMaintenanceCosts = 0f;

            /// <summary>
            /// Aggregated attack value of all attached armies.  Derived during authoring.
            /// </summary>
            [JsonProperty("attack")] public int attack = 0;

            /// <summary>
            /// Aggregated defense value of all attached armies.  Derived during authoring.
            /// </summary>
            [JsonProperty("defense")] public int defense = 0;

            /// <summary>
            /// Aggregate army speed.  Derived during authoring.  Defaults to 20 when levies are present and may be overridden by men‑at‑arms speed when no levies exist.
            /// </summary>
            [JsonProperty("speed")] public float speed = 20f;
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
            /// realm statistics calculations.  When profit tracking is enabled, this value may
            /// be left at zero and instead <see cref="totalProfitPerMonth"/> is used.
            /// </summary>
            [JsonProperty("totalIncomePerMonth")] public float totalIncomePerMonth;

            /// <summary>
            /// Total profit generated per month by this settlement.  This is defined as
            /// income minus court and army expenses.  When provided in the JSON, it
            /// supersedes <see cref="totalIncomePerMonth"/> when computing realm stats.
            /// </summary>
            [JsonProperty("totalProfitPerMonth")] public float totalProfitPerMonth;

            /// <summary>
            /// Total treasury holdings of this settlement.  Displayed in the economy tab.
            /// </summary>
            [JsonProperty("totalTreasury")] public float totalTreasury;

            /// <summary>
            /// Court expenses expressed as a numeric value.  This should reflect the sum of
            /// salaries paid to councillors.  When the settlement has vassals, the court
            /// expenses of the capital settlement should be used instead.
            /// </summary>
            [JsonProperty("courtExpenses")] public float courtExpenses;

            /// <summary>
            /// Army expenses expressed as a numeric value.  This reflects the sum of
            /// maintenance costs for all attached armies, both raised and unraised.  When
            /// the settlement has vassals, the army expenses of the capital settlement
            /// should be used instead.
            /// </summary>
            [JsonProperty("armyExpenses")] public float armyExpenses;

            /// <summary>
            /// List of projects currently under construction in this settlement.
            /// </summary>
            [JsonProperty("currentlyConstructing")] public string[] currentlyConstructing = Array.Empty<string>();

            /// <summary>
            /// List of building identifiers present in this settlement.  Each identifier
            /// references a building entry in the global BuildingCatalog.  Buildings are
            /// only applicable to settlements that do not have vassals (i.e. the settlement
            /// is at the bottom of the feudal chain).  When loaded into the authoring UI,
            /// the stats provided by these buildings will be aggregated to show adjusted
            /// values for income and levies.  See modifiedIncomePerMonth and
            /// modifiedTotalLevies for the computed results.
            /// </summary>
            [JsonProperty("buildings")] public string[] buildings = Array.Empty<string>();

            /// <summary>
            /// Total income per month adjusted by buildings.  This value is computed
            /// automatically by the authoring tools based on the base income
            /// (<see cref="totalIncomePerMonth"/>) and the income modifiers of each
            /// building present in <see cref="buildings"/>.  Building income modifiers
            /// are multiplied by 100 to convert from catalog points to monthly gold.
            /// </summary>
            [JsonProperty("modifiedIncomePerMonth")] public float modifiedIncomePerMonth;

            /// <summary>
            /// Total levies adjusted by buildings.  This value is computed
            /// automatically by the authoring tools based on the base levies
            /// (<see cref="army.totalLevies"/>) and the levy modifiers of each
            /// building present in <see cref="buildings"/>.  Building levy modifiers
            /// are multiplied by 80 to convert from catalog points to levies.
            /// </summary>
            [JsonProperty("modifiedTotalLevies")] public int modifiedTotalLevies;

        /// <summary>
        /// Base monthly rebellion chance for this settlement, expressed as a
        /// percentage (0–100).  This value represents the unrest risk before
        /// accounting for buildings or vassal averaging.  Authoring tools may
        /// expose this field on settlements without vassals.  For settlements
        /// with vassals this value is ignored and the aggregated rebellion
        /// chance is stored on the feudal data.
        /// </summary>
        [JsonProperty("baseRebellionChance")] public float baseRebellionChance;

        /// <summary>
        /// Total rebellion chance after applying building modifiers or vassal
        /// aggregation.  For settlements without vassals this is computed
        /// from <see cref="baseRebellionChance"/> plus the sum of
        /// rebellionChance modifiers of each building present in
        /// <see cref="buildings"/>.  For settlements with vassals this
        /// represents the aggregated average of the capital and its direct
        /// vassals.
        /// </summary>
        [JsonProperty("modifiedRebellionChance")] public float modifiedRebellionChance;

            /// <summary>
            /// Base monthly rebellion chance for this settlement, expressed as a
            /// percentage (0–100).  This value represents the unrest risk before
            /// accounting for buildings or vassal averaging.  Authoring tools may
            /// expose this field on settlements without vassals.  For settlements
            /// with vassals this value is ignored and the aggregated rebellion
            /// chance is stored on the feudal data.
            /// </summary>
            [JsonProperty("baseRebellionChance")] public float baseRebellionChance;

            /// <summary>
            /// Total rebellion chance after applying building modifiers or vassal
            /// aggregation.  For settlements without vassals this is computed
            /// from <see cref="baseRebellionChance"/> plus the sum of
            /// rebellionChance modifiers of each building present in
            /// <see cref="buildings"/>.  For settlements with vassals this
            /// represents the aggregated average of the capital and its direct
            /// vassals.
            /// </summary>
            [JsonProperty("modifiedRebellionChance")] public float modifiedRebellionChance;

            // Resources tracked by this settlement.  These values represent monthly
            // production or stockpiles, depending on the context in which they are used.
            [JsonProperty("wheat")] public float wheat;
            [JsonProperty("bread")] public float bread;
            [JsonProperty("meat")] public float meat;
            [JsonProperty("wood")] public float wood;
            [JsonProperty("stone")] public float stone;
            [JsonProperty("iron")] public float iron;
            [JsonProperty("steel")] public float steel;
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
            /// High‑level list of primary traits that characterise this settlement.  Distinct
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
            /// List of timeline event identifiers for this settlement.  Each entry points
            /// into the global timeline catalog rather than embedding a full event.
            /// </summary>
            [JsonProperty("timelineEntries")] public string[] timelineEntries = Array.Empty<string>();

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
            /// Unique identifier for this settlement.  Duplicated from the root for
            /// backwards compatibility; used by some editor scripts when seeding
            /// settlement data from a MapPoint.
            /// </summary>
            [JsonProperty("settlementId")] public string settlementId;

            /// <summary>
            /// The map layer this settlement belongs to.  Stored as a string to
            /// support conversions between enum and string representations.  This
            /// value is copied from the MapPoint when creating a new settlement
            /// from a MapPoint in the editor.
            /// </summary>
            [JsonProperty("layer")] public string layer;

            /// <summary>
            /// Indicates whether this settlement is populated.  When true, the
            /// settlement is considered active; when false, some UI panels may
            /// ignore it.  Copied from the MapPoint during seeding.
            /// </summary>
            [JsonProperty("isPopulated")] public bool isPopulated;

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

            /// <summary>
            /// Optional contract fields used when this settlement is a vassal with a liege. These values
            /// represent the income and levy tax rates, as well as optional contract terms, that
            /// govern the relationship with the liege. When <see cref="liegeSettlementId"/> is not empty,
            /// the settlement editor may allow editing these fields directly in the Feudal section.
            /// These fields default to zero or empty and are only meaningful for vassal settlements.
            /// </summary>
            [JsonProperty("incomeTaxRate")] public float incomeTaxRate;

            /// <summary>
            /// Levy tax rate applied to troops contributed by this vassal.  Replaces the old
            /// troop tax rate field.  Values should be between 0 and 1.
            /// </summary>
            [JsonProperty("levyTaxRate")] public float levyTaxRate;

            /// <summary>
            /// Deprecated property preserved for backwards compatibility.  When JSON
            /// specifies a troop tax rate this alias forwards the value to
            /// <see cref="levyTaxRate"/>.  When the levy tax rate is changed, this
            /// property reads the updated value.  Authoring tools should migrate
            /// entirely to <see cref="levyTaxRate"/>.
            /// </summary>
            [Obsolete("Use levyTaxRate instead")]
            [JsonProperty("troopTaxRate")]
            public float troopTaxRate
            {
                get => levyTaxRate;
                set => levyTaxRate = value;
            }

            [JsonProperty("contractTerms")] public string contractTerms;

        /// <summary>
        /// Aggregated monthly rebellion chance for feudal domains.  When this settlement
        /// has direct vassals, this value is computed as the average of its own
        /// modified rebellion chance and the modified rebellion chances of all
        /// its direct vassals.  When there are no vassals this value is unused
        /// and may be left at zero.  Authoring tools should display this field
        /// on settlements with vassals.
        /// </summary>
        [JsonProperty("rebellionChance")] public float rebellionChance;

            /// <summary>
            /// List of councillor salary entries.  Each entry associates a councillor
            /// character ID with the salary paid to them.  When the settlement has vassals,
            /// councillors from the capital settlement should be used instead of this list.
            /// </summary>
            [JsonProperty("councillorSalaries")] public List<CouncillorSalaryEntry> councillorSalaries = new List<CouncillorSalaryEntry>();

            /// <summary>
            /// Aggregated monthly rebellion chance for this domain.  When this settlement
            /// has vassals, this value is computed as the average of the modified
            /// rebellion chance of the capital settlement and each direct vassal.
            /// When there are no vassals, this value is unused and may be left at zero.
            /// Authoring tools should display this field on settlements with vassals.
            /// </summary>
            [JsonProperty("rebellionChance")] public float rebellionChance;
        }

        [Serializable]
        public class VassalContractData
        {
            [JsonProperty("vassalSettlementId")] public string vassalSettlementId;
            [JsonProperty("incomeTaxRate")] public float incomeTaxRate;
            [JsonProperty("levyTaxRate")] public float levyTaxRate;

            /// <summary>
            /// Deprecated troop tax rate property retained for backwards compatibility with
            /// existing JSON.  This alias reads and writes the same value as
            /// <see cref="levyTaxRate"/>.
            /// </summary>
            [Obsolete("Use levyTaxRate instead")]
            [JsonProperty("troopTaxRate")]
            public float troopTaxRate
            {
                get => levyTaxRate;
                set => levyTaxRate = value;
            }
            [JsonProperty("terms")] public string terms;
        }

        /// <summary>
        /// Defines a councillor salary entry linking a character ID to a salary value.  Used by
        /// settlements to pay salaries to council members.
        /// </summary>
        [Serializable]
        public class CouncillorSalaryEntry
        {
            [JsonProperty("characterId")] public string characterId;
            [JsonProperty("salary")] public float salary;
        }
        #endregion
    }