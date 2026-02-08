using Newtonsoft.Json;
using System.Collections.Generic;

// Unpopulated area data model aligned with world_templates/unpopulated_template.json.
namespace SeaOfFallenStars.WorldData
{
    [System.Serializable]
    public class UnpopulatedMain
    {
        [JsonProperty("description")]
        public string description;

        [JsonProperty("keyFeature")]
        public string keyFeature;

        [JsonProperty("resourceAbundance")]
        public string[] resourceAbundance = new string[0];
    }

    [System.Serializable]
    public class UnpopulatedGeography
    {
        [JsonProperty("terrain")]
        public string terrain;

        // Optional: more specific terrain type if your UI expects it
        [JsonProperty("terrainType")]
        public string terrainType;

        [JsonProperty("climate")]
        public string climate;

        [JsonProperty("floraFauna")]
        public string[] floraFauna = new string[0];

        // Optional: separate flora and fauna lists for legacy UI support
        [JsonProperty("flora")]
        public string[] flora = new string[0];

        [JsonProperty("fauna")]
        public string[] fauna = new string[0];
    }

    [System.Serializable]
    public class UnpopulatedNature
    {
        [JsonProperty("dangerLevel")]
        public string dangerLevel;

        [JsonProperty("wildlife")]
        public string[] wildlife = new string[0];

        // Legacy fields that may be used by certain UI components.  They can
        // remain empty if not used.
        [JsonProperty("flora")]
        public string[] flora = new string[0];

        [JsonProperty("fauna")]
        public string[] fauna = new string[0];
    }

    [System.Serializable]
    public class UnpopulatedHistory
    {
        [JsonProperty("historicalEvents")]
        public string[] historicalEvents = new string[0];

        // Optional notes field for additional context
        [JsonProperty("notes")]
        public string notes;

        // Optional timeline entries for legacy UI support
        [JsonProperty("timelineEntries")]
        public string[] timelineEntries;
    }

    [System.Serializable]
    public class CulturalInfluence
    {
        [JsonProperty("cultureId")]
        public string cultureId;

        [JsonProperty("percentage")]
        public float percentage;
    }

    [System.Serializable]
    public class UnpopulatedCulture
    {
        [JsonProperty("culturalInfluences")]
        public List<CulturalInfluence> culturalInfluences = new List<CulturalInfluence>();

        // Optional fields for extended cultural data.  These fields are not
        // part of the world template but may be referenced by legacy UI.
        [JsonProperty("notes")]
        public string notes;

        [JsonProperty("peoples")]
        public string[] peoples = new string[0];

        [JsonProperty("factions")]
        public string[] factions = new string[0];

        [JsonProperty("languages")]
        public string[] languages = new string[0];

        [JsonProperty("customs")]
        public string[] customs = new string[0];

        [JsonProperty("rumors")]
        public string[] rumors = new string[0];
    }

    [System.Serializable]
    public class UnpopulatedWater
    {
        [JsonProperty("hasWater")]
        public bool hasWater;

        [JsonProperty("waterBodies")]
        public string[] waterBodies = new string[0];

        // Additional water-related details for legacy UI compatibility.
        [JsonProperty("depth")]
        public string depth;

        [JsonProperty("waterBodyType")]
        public string waterBodyType;

        [JsonProperty("waterType")]
        public string waterType;

        [JsonProperty("currents")]
        public string[] currents = new string[0];

        [JsonProperty("hazards")]
        public string[] hazards = new string[0];

        [JsonProperty("notableFeatures")]
        public string[] notableFeatures = new string[0];

        [JsonProperty("notes")]
        public string notes;
    }

    [System.Serializable]
    public class UnpopulatedInfoData
    {
        [JsonProperty("areaId")]
        public string areaId;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("mapUrlOrPath")]
        public string mapUrlOrPath;

        [JsonProperty("layer")]
        public string layer;

        [JsonProperty("isPopulated")]
        public bool isPopulated = false;

        [JsonProperty("subtype")]
        public string subtype;

        [JsonProperty("main")]
        public UnpopulatedMain main = new UnpopulatedMain();

        [JsonProperty("geography")]
        public UnpopulatedGeography geography = new UnpopulatedGeography();

        [JsonProperty("nature")]
        public UnpopulatedNature nature = new UnpopulatedNature();

        [JsonProperty("history")]
        public UnpopulatedHistory history = new UnpopulatedHistory();

        [JsonProperty("culture")]
        public UnpopulatedCulture culture = new UnpopulatedCulture();

        [JsonProperty("water")]
        public UnpopulatedWater water = new UnpopulatedWater();

        [JsonProperty("comments")]
        public string comments;

        [JsonProperty("ext")]
        public Dictionary<string, object> ext = new Dictionary<string, object>();
    }
}