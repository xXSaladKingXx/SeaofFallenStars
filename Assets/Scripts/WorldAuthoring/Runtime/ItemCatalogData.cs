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

    public enum WeaponSubtype
    {
        Martial_Melee,
        Martial_Ranged,
        Simple_Melee,
        Simple_Ranged,
        Special,
        Thrown,
        Firearm,
    }
    public enum ArmorSubtype
    {
        Clothes,
        Light,
        Medium,
        Heavy,
        Shields,
        Special,
    }

    public enum ToolSubtype
    {
        AlchemistsTools,
        BrewersTools,
        TailorsTools,
        TinkerersTools,
        HerbalistsTools,
        SmithsTools,
        PaintersTools,
        ThievesTools,
        CarpenersTools,
        FletchersTools,
        CobblersTools,
        MagicSmithsTools,
        JewellersTools,
        CalligraphyTools,
        CartographyTools,
        DisguiseTools,
        ForgeryTools,
        MasonsTools,
        CooksTools,
        LeatherworkersTools,
        MinersTools,

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
