using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Global registry of all statistics referenced across characters,
    /// settlements and other systems. Each entry defines metadata about
    /// a statistic such as its scope, data type and whether it may be
    /// modified by traits or items.
    /// </summary>
    [Serializable]
    public sealed class StatCatalogDataModel
    {
        [JsonProperty("catalogId")]
        public string catalogId = "stat_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Stat Catalog";

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;

        [JsonProperty("stats")]
        public List<StatEntryModel> stats = new List<StatEntryModel>();
    }

    /// <summary>
    /// Metadata for a single statistic. The fields in this class correspond
    /// directly to the authoring specification. Most are strings or lists
    /// to allow flexibility in how statistics are categorised and computed.
    /// </summary>
    [Serializable]
    public sealed class StatEntryModel
    {
        /// <summary>
        /// Stable identifier for the statistic. Used by StatModEntry and other
        /// systems to reference this stat.
        /// </summary>
        [JsonProperty("statId")]
        public string statId;

        [JsonProperty("displayName")]
        public string displayName;

        /// <summary>
        /// One or more scopes where this stat is valid (Character, Settlement,
        /// Army, Region, etc). An empty list implies a global stat.
        /// </summary>
        [JsonProperty("scope")]
        public List<string> scope = new List<string>();

        /// <summary>
        /// Underlying value type of the stat (Int, Float, Bool, String).
        /// </summary>
        [JsonProperty("valueType")]
        public string valueType;

        /// <summary>
        /// Indicates whether this stat is an editable input or a derived
        /// quantity. Valid values are "Input" and "Derived".
        /// </summary>
        [JsonProperty("kind")]
        public string kind;

        /// <summary>
        /// JSON path into the owning InfoData where the input value lives.
        /// Only used when kind=Input. Nullable when the path is not
        /// explicitly defined (e.g. compound inputs).
        /// </summary>
        [JsonProperty("jsonPath")]
        public string jsonPath;

        /// <summary>
        /// List of stat identifiers used to compute this stat. Only relevant
        /// when kind=Derived.
        /// </summary>
        [JsonProperty("derivedFrom")]
        public List<string> derivedFrom = new List<string>();

        /// <summary>
        /// Human readable formula explaining how this stat is derived. Not
        /// executed by the runtime but useful for authoring tools.
        /// </summary>
        [JsonProperty("derivedFormula")]
        [TextArea(2, 10)]
        public string derivedFormula;

        /// <summary>
        /// Unit of measure for the stat (e.g. "ft", "gp/month"). Optional.
        /// </summary>
        [JsonProperty("unit")]
        public string unit;

        /// <summary>
        /// Indicates whether modifiers may legally target this stat. Derived
        /// stats typically set this to false.
        /// </summary>
        [JsonProperty("isModifiable")]
        public bool isModifiable;
    }
}