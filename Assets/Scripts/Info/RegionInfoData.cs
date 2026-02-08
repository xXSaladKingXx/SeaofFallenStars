using Newtonsoft.Json;
using System.Collections.Generic;

// Region data model aligned with world_templates/region_template.json. It
// includes main details, geography, culture composition and vassal relationships.
namespace SeaOfFallenStars.WorldData
{
    [System.Serializable]
    public class RegionMain
    {
        [JsonProperty("description")]
        public string description;

        [JsonProperty("importance")]
        public string importance;

        [JsonProperty("capitalSettlementId")]
        public string capitalSettlementId;
    }

    [System.Serializable]
    public class RegionGeography
    {
        [JsonProperty("terrainTypes")]
        public List<string> terrainTypes = new List<string>();

        [JsonProperty("climate")]
        public string climate;

        [JsonProperty("borders")]
        public List<string> borders = new List<string>();

        [JsonProperty("resources")]
        public List<string> resources = new List<string>();
    }

    [System.Serializable]
    public class RegionCultureComposition
    {
        [JsonProperty("cultureId")]
        public string cultureId;

        [JsonProperty("percentage")]
        public float percentage;
    }

    [System.Serializable]
    public class RegionVassals
    {
        [JsonProperty("independentSettlementIds")]
        public List<string> independentSettlementIds = new List<string>();

        [JsonProperty("vassalSettlementIds")]
        public List<string> vassalSettlementIds = new List<string>();

        [JsonProperty("childRegionIds")]
        public List<string> childRegionIds = new List<string>();
    }

    [System.Serializable]
    public class RegionInfoData
    {
        [JsonProperty("regionId")]
        public string regionId;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("mapUrlOrPath")]
        public string mapUrlOrPath;

        [JsonProperty("layer")]
        public string layer;

        [JsonProperty("main")]
        public RegionMain main = new RegionMain();

        [JsonProperty("geography")]
        public RegionGeography geography = new RegionGeography();

        [JsonProperty("cultureComposition")]
        public List<RegionCultureComposition> cultureComposition = new List<RegionCultureComposition>();

        [JsonProperty("religionIds")]
        public List<string> religionIds = new List<string>();

        [JsonProperty("languageIds")]
        public List<string> languageIds = new List<string>();

        [JsonProperty("vassals")]
        public RegionVassals vassals = new RegionVassals();

        [JsonProperty("ext")]
        public Dictionary<string, object> ext = new Dictionary<string, object>();
    }
}