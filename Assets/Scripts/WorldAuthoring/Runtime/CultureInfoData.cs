using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Zana.WorldAuthoring
{
    [Serializable]
    public sealed class CultureInfoData
    {
        [JsonProperty("cultureId")]
        public string cultureId;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("description")]
        public string description;

        [JsonProperty("tags")]
        public List<string> tags = new List<string>();

        /// <summary>
        /// Identifiers of traits that belong to this culture. The traits are defined in a
        /// separate TraitCatalogData asset and referenced by their IDs. When authoring
        /// cultures in the editor, you should populate this list using a drop‑down that
        /// enumerates all existing trait entries. New traits can be created via the trait
        /// catalog authoring tools but should not be created ad‑hoc on a culture.
        /// </summary>
        [JsonProperty("traits")]
        public List<string> traits = new List<string>();

        /// <summary>
        /// Languages spoken within this culture. These are simple string identifiers and
        /// should correspond to language records stored elsewhere in your project. The
        /// authoring UI should provide a multi‑select list of existing languages rather
        /// than free‑form text entry.
        /// </summary>
        [JsonProperty("languages")]
        public List<string> languages = new List<string>();

        /// <summary>
        /// The factions that are associated with this culture. Factions are defined in
        /// your game’s faction data and referenced here by ID. Use a drop‑down in the
        /// authoring UI to select existing factions. Cultures should not create new
        /// factions directly.
        /// </summary>
        [JsonProperty("factions")]
        public List<string> factions = new List<string>();
    }
}
