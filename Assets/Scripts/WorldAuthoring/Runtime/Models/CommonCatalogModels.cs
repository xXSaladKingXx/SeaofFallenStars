using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Shared catalog models used by multiple catalog types.
    /// </summary>
    public static class CommonCatalogModels
    {
    }

    /// <summary>
    /// A reusable data structure representing a quantity of some item. Used
    /// throughout the catalog system for harvest drops, resource lists and
    /// inventory definitions. The unit and notes fields are optional and
    /// allow authors to clarify measurements or special conditions.
    /// </summary>
    [Serializable]
    public sealed class ItemQuantityEntry
    {
        [JsonProperty("itemId")]
        public string itemId;

        [JsonProperty("quantity")]
        public float quantity;

        [JsonProperty("unit")]
        public string unit;

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;
    }

    /// <summary>
    /// Describes a modification to a numeric statistic. Traits and items
    /// reference stats declared in the global StatCatalog using the statId
    /// property. The mode determines how the value is applied to the base
    /// stat (Add, Multiply, Set, Min or Max). Non‑numeric stats should be
    /// excluded from modification by authoring and validation tools.
    /// </summary>
    [Serializable]
    public sealed class StatModEntry
    {
        [JsonProperty("statId")]
        public string statId;

        [JsonProperty("mode")]
        public string mode;

        [JsonProperty("value")]
        public float value;

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;
    }
}