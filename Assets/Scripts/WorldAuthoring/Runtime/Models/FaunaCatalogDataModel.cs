using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Runtime data container for all animal life entries in the world. Each
    /// entry represents a single species of creature and includes metadata
    /// appropriate for narrative description and basic gameplay hooks.
    /// </summary>
    [Serializable]
    public sealed class FaunaCatalogDataModel
    {
        [JsonProperty("catalogId")]
        public string catalogId = "fauna_catalog";

        [JsonProperty("displayName")]
        public string displayName = "Fauna Catalog";

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;

        [JsonProperty("fauna")]
        public List<FaunaEntryModel> fauna = new List<FaunaEntryModel>();
    }

    /// <summary>
    /// Represents a single creature species. See the authoring specification
    /// for descriptions of each field.
    /// </summary>
    [Serializable]
    public sealed class FaunaEntryModel
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("displayName")]
        public string displayName;

        /// <summary>
        /// Broad biological grouping for this creature (mammals, reptiles,
        /// avians, fish, cephalapod, insect, etc).
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
        /// Free‑form tags describing where this creature can be found (e.g.
        /// "mountain", "underground"). The system does not enforce any
        /// particular values here.
        /// </summary>
        [JsonProperty("habitats")]
        public List<string> habitats = new List<string>();

        /// <summary>
        /// A coarse size category such as Tiny, Small, Medium, Large or
        /// similar. Useful for sorting and derived mechanics.
        /// </summary>
        [JsonProperty("sizeCategory")]
        public string sizeCategory;

        /// <summary>
        /// Narrative temperament descriptor (e.g. "skittish", "aggressive").
        /// </summary>
        [JsonProperty("temperament")]
        public string temperament;

        /// <summary>
        /// Indicates whether this creature has been domesticated. Null means
        /// the author has not specified a value.
        /// </summary>
        [JsonProperty("isDomesticated")]
        public bool? isDomesticated;

        /// <summary>
        /// Structured list of items produced when harvesting this creature
        /// (hides, meat, ink sacs, etc). Each entry references an item in
        /// the ItemCatalog and provides a quantity and optional unit/notes.
        /// </summary>
        [JsonProperty("dropItems")]
        public List<ItemQuantityEntry> dropItems = new List<ItemQuantityEntry>();

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;
    }
}