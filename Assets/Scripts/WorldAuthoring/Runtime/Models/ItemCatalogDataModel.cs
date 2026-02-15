using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Global registry of all items available in the world. Items include
    /// mundane equipment, trade goods and optional magical artefacts. The
    /// catalog structure supports the rich data needed by authoring tools
    /// without enforcing any particular rules at runtime.
    /// </summary>
    [Serializable]
    public sealed class ItemCatalogDataModel
    {
        [JsonProperty("catalogId")]
        public string catalogId = "item_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Item Catalog";

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;

        [JsonProperty("items")]
        public List<ItemEntryModel> items = new List<ItemEntryModel>();
    }

    /// <summary>
    /// Represents a single entry in the item catalog. Items can be simple
    /// trade goods or complex objects with nested data describing their
    /// mechanical effects. Optional fields are declared as nullable to
    /// minimise friction in the authoring process.
    /// </summary>
    [Serializable]
    public sealed class ItemEntryModel
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        /// <summary>
        /// The broad category of the item. Valid values are enumerated
        /// in the authoring specification (Weapon, Armor, AdventuringGear,
        /// Tool, Mount, Vehicle, TradeGood, MagicItem).
        /// </summary>
        [JsonProperty("type")]
        public string type;

        [JsonProperty("description")]
        [TextArea(2, 10)]
        public string description;

        /// <summary>
        /// Narrative rarity tier such as Common, Uncommon, Rare, etc.
        /// </summary>
        [JsonProperty("rarity")]
        public string rarity;

        [JsonProperty("costGp")]
        public float? costGp;

        [JsonProperty("weightLb")]
        public float? weightLb;

        [JsonProperty("stackable")]
        public bool stackable;

        [JsonProperty("tags")]
        public List<string> tags = new List<string>();

        /// <summary>
        /// Trait identifiers applied to this item. Points into the global
        /// TraitCatalog.
        /// </summary>
        [JsonProperty("traits")]
        public List<string> traits = new List<string>();

        /// <summary>
        /// Stat modifiers applied when the item is equipped, used or owned.
        /// See StatModEntry for the structure of each modification.
        /// </summary>
        [JsonProperty("statModifiers")]
        public List<StatModEntry> statModifiers = new List<StatModEntry>();

        /// <summary>
        /// Whether the item requires attunement (relevant for magical items).
        /// Null means the author has not specified a value.
        /// </summary>
        [JsonProperty("requiresAttunement")]
        public bool? requiresAttunement;

        /// <summary>
        /// Maximum number of charges on the item (for charged magic items).
        /// </summary>
        [JsonProperty("chargesMax")]
        public int? chargesMax;

        [JsonProperty("chargesCurrent")]
        public int? chargesCurrent;

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;

        /// <summary>
        /// Optional block describing weapon specific data. Present when
        /// type=Weapon.
        /// </summary>
        [JsonProperty("weaponData")]
        public WeaponData weaponData;

        /// <summary>
        /// Optional block describing armour specific data. Present when
        /// type=Armor.
        /// </summary>
        [JsonProperty("armorData")]
        public ArmorData armorData;

        /// <summary>
        /// Optional block describing container specific data. Present when
        /// the item is a container.
        /// </summary>
        [JsonProperty("containerData")]
        public ContainerData containerData;

        /// <summary>
        /// Optional block describing consumable specific data. Present when
        /// the item is consumable (potions, scrolls, etc).
        /// </summary>
        [JsonProperty("consumableData")]
        public ConsumableData consumableData;

        /// <summary>
        /// Optional block describing tool specific data. Present when
        /// type=Tool.
        /// </summary>
        [JsonProperty("toolData")]
        public ToolData toolData;

        /// <summary>
        /// Optional block describing vehicle specific data. Present when
        /// type=Vehicle or Mount.
        /// </summary>
        [JsonProperty("vehicleData")]
        public VehicleData vehicleData;
    }

    /// <summary>
    /// Weapon specific data for an item. Fields correspond to SRD weapon
    /// definitions and allow the author to fully specify a weapon's
    /// mechanical properties.
    /// </summary>
    [Serializable]
    public sealed class WeaponData
    {
        [JsonProperty("weaponCategory")]
        public string weaponCategory;

        [JsonProperty("rangeType")]
        public string rangeType;

        [JsonProperty("damageDice")]
        public string damageDice;

        [JsonProperty("damageType")]
        public string damageType;

        [JsonProperty("properties")]
        public List<string> properties = new List<string>();
    }

    /// <summary>
    /// Armour specific data for an item.
    /// </summary>
    [Serializable]
    public sealed class ArmorData
    {
        [JsonProperty("armorCategory")]
        public string armorCategory;

        [JsonProperty("baseAC")]
        public int? baseAC;

        /// <summary>
        /// Maximum dexterity bonus that can be applied to the armour class.
        /// Null means no cap.
        /// </summary>
        [JsonProperty("dexBonusCap")]
        public int? dexBonusCap;

        [JsonProperty("strengthRequirement")]
        public int? strengthRequirement;

        [JsonProperty("stealthDisadvantage")]
        public bool? stealthDisadvantage;
    }

    /// <summary>
    /// Container specific data for an item. Used for backpacks, chests and
    /// other items that can store things.
    /// </summary>
    [Serializable]
    public sealed class ContainerData
    {
        [JsonProperty("capacityLb")]
        public float? capacityLb;

        [JsonProperty("capacityCuFt")]
        public float? capacityCuFt;
    }

    /// <summary>
    /// Consumable specific data for an item.
    /// </summary>
    [Serializable]
    public sealed class ConsumableData
    {
        [JsonProperty("usesPerItem")]
        public int? usesPerItem;

        [JsonProperty("effectNotes")]
        [TextArea(2, 10)]
        public string effectNotes;
    }

    /// <summary>
    /// Tool specific data for an item.
    /// </summary>
    [Serializable]
    public sealed class ToolData
    {
        [JsonProperty("toolCategory")]
        public string toolCategory;
    }

    /// <summary>
    /// Vehicle specific data for an item.
    /// </summary>
    [Serializable]
    public sealed class VehicleData
    {
        [JsonProperty("speed")]
        public string speed;

        [JsonProperty("crew")]
        public string crew;

        [JsonProperty("cargo")]
        public string cargo;
    }
}