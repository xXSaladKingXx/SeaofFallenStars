using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Data model for a global building catalog.  Each world contains a single
    /// instance of this catalog which defines all buildings that can be
    /// constructed on settlements.  Each entry includes cost, category,
    /// prerequisites, build time and modifiers that affect settlement stats.
    /// </summary>
    [Serializable]
    public sealed class BuildingCatalogDataModel
    {
        [JsonProperty("catalogId")]
        public string catalogId = "building_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Building Catalog";

        [JsonProperty("entries")]
        public List<BuildingEntryModel> entries = new List<BuildingEntryModel>();
    }

    /// <summary>
    /// Categories used to group buildings in the catalog.  These categories
    /// correspond to sections in the building reference document.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BuildingCategory
    {
        SettlementTier,
        KeepCastle,
        Walls,
        CivicCultural,
        Military,
        AgricultureIndustry,
        CommerceTrade,
        Other
    }

    /// <summary>
    /// A single building entry definition.  Buildings modify settlement or
    /// realm stats when constructed.  Most modifiers are integers which are
    /// interpreted by authoring tools when computing derived values.
    /// </summary>
    [Serializable]
    public sealed class BuildingEntryModel
    {
        /// <summary>
        /// Unique identifier for this building.  Should be lowercase and
        /// unique within the catalog.
        /// </summary>
        [JsonProperty("id")]
        public string id;

        /// <summary>
        /// Human friendly name for this building.
        /// </summary>
        [JsonProperty("displayName")]
        public string displayName;

        /// <summary>
        /// Category used for grouping in the authoring UI.
        /// </summary>
        [JsonProperty("category")]
        public BuildingCategory category = BuildingCategory.Other;

        /// <summary>
        /// Cost to construct this building, expressed in gold pieces (gp).
        /// </summary>
        [JsonProperty("cost")]
        public float cost;

        /// <summary>
        /// Optional string describing prerequisites required before this
        /// building can be constructed.  This may list other buildings or
        /// settlement tier requirements.
        /// </summary>
        [JsonProperty("prerequisites")]
        public string prerequisites;

        /// <summary>
        /// The base time required to construct the building.  Expressed as a
        /// human readable string (e.g. "1 year", "6 months").
        /// </summary>
        [JsonProperty("buildTime")]
        public string buildTime;

        /// <summary>
        /// Income points provided by this building.  Each point increases
        /// monthly income by 100 gp.  For example, a value of 2 adds 200 gp
        /// per month to the settlement's income.
        /// </summary>
        [JsonProperty("income")]
        public int income;

        /// <summary>
        /// Levy points provided by this building.  Each point increases the
        /// number of levies by 80 soldiers.  For example, a value of 2 adds
        /// 160 levies to the settlement.
        /// </summary>
        [JsonProperty("levies")]
        public int levies;

        /// <summary>
        /// Defense bonus conferred by this building.  Defense improves the
        /// settlement's resilience against attacks and sieges.
        /// </summary>
        [JsonProperty("defense")]
        public int defense;

        /// <summary>
        /// Stability bonus conferred by this building.  Stability improves
        /// internal order and reduces rebellion chances.
        /// </summary>
        [JsonProperty("stability")]
        public int stability;

        /// <summary>
        /// Prestige bonus conferred by this building.  Prestige influences
        /// diplomacy and fame.
        /// </summary>
        [JsonProperty("prestige")]
        public int prestige;

        /// <summary>
        /// Happiness bonus conferred by this building.  Happiness improves
        /// population growth and reduces unrest.
        /// </summary>
        [JsonProperty("happiness")]
        public int happiness;

        /// <summary>
        /// Population added to the settlement when this building is constructed.
        /// </summary>
        [JsonProperty("population")]
        public int population;

        /// <summary>
        /// Additional building slots unlocked by constructing this building.
        /// </summary>
        [JsonProperty("buildingSlots")]
        public int buildingSlots;

        /// <summary>
        /// Additional trade capacity unlocked by this building.
        /// </summary>
        [JsonProperty("tradeCapacity")]
        public int tradeCapacity;

        /// <summary>
        /// Arbitrary notes or further descriptions of the building's effects.
        /// </summary>
        [JsonProperty("notes")]
        public string notes;
    }
}