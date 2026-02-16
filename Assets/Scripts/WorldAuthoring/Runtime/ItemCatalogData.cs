using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zana.WorldAuthoring
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemType5e
    {
        Weapon,
        Armor,
        AdventuringGear,
        Tool,
        Mount,
        Vehicle,
        TradeGood,
    }

    [Serializable]
    public sealed class ItemCatalogData
    {
        [JsonProperty("catalogId")]
        public string catalogId = "item_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Item Catalog";

        [JsonProperty("entries")]
        public List<ItemCatalogEntry> entries = new List<ItemCatalogEntry>();
    }

    [Serializable]
    public sealed class ItemCatalogEntry
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("type")]
        public ItemType5e type;

        [JsonProperty("description")]
        public string description;

        [JsonProperty("costGp")]
        public float costGp;

        [JsonProperty("weightLb")]
        public float weightLb;
    }
}
