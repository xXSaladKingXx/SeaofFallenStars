using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zana.WorldAuthoring
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FloraFamily
    {
        Tree,
        Grass,
        Fruit,
        Vegetable,
        Bush,
        Mushroom,
    }

    [Serializable]
    public sealed class FloraCatalogData
    {
        [JsonProperty("catalogId")]
        public string catalogId = "flora_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Flora Catalog";

        [JsonProperty("entries")]
        public List<FloraCatalogEntry> entries = new List<FloraCatalogEntry>();
    }

    [Serializable]
    public sealed class FloraCatalogEntry
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("family")]
        public FloraFamily family;

        /// <summary>
        /// Trait IDs from the global Trait catalog.
        /// </summary>
        [JsonProperty("traits")]
        public List<string> traits = new List<string>();
    }
}
