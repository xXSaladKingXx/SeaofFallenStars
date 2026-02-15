using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Runtime data container for all plant life entries in the world. Each entry
    /// represents a single species of plant and includes practical metadata
    /// useful for both narrative description and gameplay mechanics.
    /// </summary>
    [Serializable]
    public sealed class FloraCatalogDataModel
    {
        /// <summary>
        /// Stable identifier for this catalog. The authoring tools use this
        /// value to locate and persist the correct JSON file.
        /// </summary>
        [JsonProperty("catalogId")]
        public string catalogId = "flora_catalog";

        /// <summary>
        /// Human‑readable name for the catalog. Displayed in the editor UI.
        /// </summary>
        [JsonProperty("displayName")]
        public string displayName = "Flora Catalog";

        /// <summary>
        /// Free‑form notes about the catalog itself. Authors can record
        /// high‑level design notes or migration comments here.
        /// </summary>
        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;

        /// <summary>
        /// Collection of all flora entries defined in this catalog.
        /// </summary>
        [JsonProperty("flora")]
        public List<FloraEntryModel> flora = new List<FloraEntryModel>();
    }

    /// <summary>
    /// Represents a single plant species. The fields in this class mirror
    /// the design described in the world authoring specification. Optional
    /// fields are nullable or lists so that authors are never forced to fill
    /// out data they do not care about.
    /// </summary>
    [Serializable]
    public sealed class FloraEntryModel
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        /// <summary>
        /// Broad biological grouping for this plant (Tree, Grass, Fruit, etc).
        /// The editor presents a hardcoded dropdown of allowed values.
        /// </summary>
        [JsonProperty("family")]
        public string family;

        /// <summary>
        /// Trait identifiers applied to this species. Points into the global
        /// TraitCatalog.
        /// </summary>
        [JsonProperty("traits")]
        public List<string> traits = new List<string>();

        [JsonProperty("description")]
        [TextArea(2, 10)]
        public string description;

        /// <summary>
        /// Narrative tags describing where this plant can be found (e.g.
        /// "temperate forest", "marsh"). Authors are free to use any strings
        /// here; the game does not attach mechanics directly to them.
        /// </summary>
        [JsonProperty("habitats")]
        public List<string> habitats = new List<string>();

        /// <summary>
        /// Seasons during which this plant is most prevalent. Values such as
        /// "spring" or "winter" are recommended but not enforced.
        /// </summary>
        [JsonProperty("seasons")]
        public List<string> seasons = new List<string>();

        /// <summary>
        /// Indicates whether the plant is edible by humanoids. When null the
        /// edibility is unspecified.
        /// </summary>
        [JsonProperty("isEdible")]
        public bool? isEdible;

        /// <summary>
        /// Free‑form notes about toxicity or hazardous properties. Kept
        /// separate from the description so that editors can present it
        /// alongside safety warnings.
        /// </summary>
        [JsonProperty("toxicityNotes")]
        [TextArea(2, 10)]
        public string toxicityNotes;

        /// <summary>
        /// Structured list of items produced when harvesting this plant. Each
        /// entry references an item in the ItemCatalog and provides a quantity
        /// and optional unit/notes.
        /// </summary>
        [JsonProperty("yieldItems")]
        public List<ItemQuantityEntry> yieldItems = new List<ItemQuantityEntry>();

        /// <summary>
        /// Additional notes about this plant that do not fit into other
        /// structured fields.
        /// </summary>
        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;
    }
}