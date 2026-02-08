using Newtonsoft.Json;
using System.Collections.Generic;

// Men-at-Arms unit definition aligned with world_templates/menatarms_template.json.
namespace SeaOfFallenStars.WorldData
{
    [System.Serializable]
    public class UpkeepCost
    {
        [JsonProperty("gold")]
        public int gold;

        [JsonProperty("food")]
        public int food;
    }

    [System.Serializable]
    public class MenAtArmsUnit
    {
        [JsonProperty("unitTypeId")]
        public string unitTypeId;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("category")]
        public string category;

        [JsonProperty("description")]
        public string description;

        [JsonProperty("menCount")]
        public int menCount;

        [JsonProperty("equipment")]
        public List<string> equipment = new List<string>();

        [JsonProperty("upkeepCost")]
        public UpkeepCost upkeepCost = new UpkeepCost();

        [JsonProperty("speed")]
        public int speed;

        [JsonProperty("strengths")]
        public List<string> strengths = new List<string>();

        [JsonProperty("weaknesses")]
        public List<string> weaknesses = new List<string>();

        [JsonProperty("ext")]
        public Dictionary<string, object> ext = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class MenAtArmsCatalogData
    {
        [JsonProperty("catalogId")]
        public string catalogId = "men_at_arms_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Men-at-Arms Catalog";

        [JsonProperty("entries")]
        public List<MenAtArmsUnit> entries = new List<MenAtArmsUnit>();
    }
}