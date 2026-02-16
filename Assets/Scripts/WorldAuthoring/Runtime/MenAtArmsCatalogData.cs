using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Zana.WorldAuthoring
{
    [Serializable]
    public sealed class MenAtArmsCatalogData
    {
        [JsonProperty("catalogId")]
        public string catalogId = "men_at_arms_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Men-at-Arms Catalog";

        [JsonProperty("entries")]
        public List<MenAtArmsEntry> entries = new List<MenAtArmsEntry>();
    }

    [Serializable]
    public sealed class MenAtArmsEntry
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("notes")]
        public string notes;

        [JsonProperty("attack")]
        public int attack;

        [JsonProperty("defense")]
        public int defense;

        [JsonProperty("size")]
        public int size;

        /// <summary>
        /// Role of this men‑at‑arms unit (e.g. Infantry, Archer, Cavalry, Siege). The role can
        /// be used for UI grouping and combat resolution but does not carry any numeric
        /// modifiers by itself. Use a fixed set of roles defined in your code to drive
        /// drop‑down choices in the authoring UI.
        /// </summary>
        [JsonProperty("role")]
        public string role;

        /// <summary>
        /// A collection of geography bonuses that apply when this unit fights in specific
        /// terrain or water subtypes. Each bonus entry defines the subtype ID and a flat
        /// bonus value to apply to attack/defense rolls when in that environment. Use
        /// drop‑downs in the authoring UI to select valid subtype IDs from your
        /// terrain catalog. If no entry exists for a given subtype, the unit
        /// receives no special bonus in that terrain.
        /// </summary>
        [JsonProperty("geographyBonuses")]
        public List<GeographyBonus> geographyBonuses = new List<GeographyBonus>();
    }

    /// <summary>
    /// Represents a bonus that a men‑at‑arms unit receives when fighting in a specific
    /// geography subtype. For example, a naval unit might have a +2 bonus when in
    /// "coastal" water and +1 in "open sea". The subtypeId field should reference an ID
    /// defined in your Terrain catalog.
    /// </summary>
    [Serializable]
    public sealed class GeographyBonus
    {
        [JsonProperty("subtypeId")]
        public string subtypeId;

        [JsonProperty("bonus")]
        public int bonus;
    }
}
