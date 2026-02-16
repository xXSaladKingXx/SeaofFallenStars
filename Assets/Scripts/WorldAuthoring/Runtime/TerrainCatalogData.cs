using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Zana.WorldAuthoring
{
    [Serializable]
    public sealed class TerrainCatalogData
    {
        [JsonProperty("catalogId")]
        public string catalogId = "terrain_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Terrain Catalog";

        [JsonProperty("entries")]
        public List<TerrainCatalogEntry> entries = new List<TerrainCatalogEntry>();
    }

    [Serializable]
    public sealed class TerrainCatalogEntry
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("notes")]
        public string notes;
    }
}
