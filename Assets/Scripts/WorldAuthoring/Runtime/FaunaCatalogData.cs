using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zana.WorldAuthoring
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FaunaFamily
    {
        Mammals,
        Reptiles,
        Avians,
        Fish,
        Cephalapod,
        Insect,
    }

    [Serializable]
    public sealed class FaunaCatalogData
    {
        [JsonProperty("catalogId")]
        public string catalogId = "fauna_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Fauna Catalog";

        [JsonProperty("entries")]
        public List<FaunaCatalogEntry> entries = new List<FaunaCatalogEntry>();
    }

    [Serializable]
    public sealed class FaunaCatalogEntry
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("family")]
        public FaunaFamily family;

        /// <summary>
        /// Trait IDs from the global Trait catalog.
        /// </summary>
        [JsonProperty("traits")]
        public List<string> traits = new List<string>();
    }
}
