using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Minimal, authoring-friendly models used by the WorldAuthoring tools.
    /// These are intentionally decoupled from your runtime data types so the authoring package
    /// remains compile-safe even if certain runtime types are not yet implemented.
    /// </summary>
    [Serializable]
    public sealed class CultureInfoDataModel
    {
        [JsonProperty("cultureId")] public string cultureId;
        [JsonProperty("displayName")] public string displayName;

        [JsonProperty("description")]
        [TextArea(3, 12)]
        public string description;

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;

        /// <summary>
        /// Identifiers of traits that belong to this culture. See CultureInfoData for more
        /// details. The authoring UI should present a drop‑down list of existing trait IDs.
        /// </summary>
        [JsonProperty("traits")]
        public List<string> traits = new List<string>();

        /// <summary>
        /// Languages spoken within this culture. Should map to language identifiers
        /// defined elsewhere in your project. Use a multi‑select list in the editor.
        /// </summary>
        [JsonProperty("languages")]
        public List<string> languages = new List<string>();

        /// <summary>
        /// Factions that are associated with this culture. Must reference existing
        /// faction IDs. Use a multi‑select list in the editor to assign these values.
        /// </summary>
        [JsonProperty("factions")]
        public List<string> factions = new List<string>();
    }

    [Serializable]
    public sealed class MenAtArmsCatalogDataModel
    {
        [JsonProperty("catalogId")] public string catalogId;
        [JsonProperty("displayName")] public string displayName;
        
        /// <summary>
        /// List of units available in this catalog. Each entry contains its own data
        /// including stats, role and geography bonuses. Older files that use the
        /// flat string list will continue to deserialize correctly into this structure.
        /// </summary>
        [JsonProperty("entries")]
        public List<MenAtArmsEntryModel> entries = new List<MenAtArmsEntryModel>();

        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;
    }

    /// <summary>
    /// Authoring model for a single men‑at‑arms unit. Mirrors the runtime MenAtArmsEntry
    /// but adds Unity attributes for improved editor usability.
    /// </summary>
    [Serializable]
    public sealed class MenAtArmsEntryModel
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("notes")] [TextArea(2, 10)] public string notes;
        [JsonProperty("attack")] public int attack;
        [JsonProperty("defense")] public int defense;
        [JsonProperty("size")] public int size;
        [JsonProperty("role")] public string role;
        [JsonProperty("qualityTier")] public string qualityTier;
        [JsonProperty("geographyBonuses")] public List<GeographyBonusModel> geographyBonuses = new List<GeographyBonusModel>();
    }

    /// <summary>
    /// Authoring model for geography bonuses associated with a unit. Matches the runtime
    /// GeographyBonus class but adds no editor‑specific attributes. The id should
    /// correspond to a terrain or water subtype defined in your project.
    /// </summary>
    [Serializable]
    public sealed class GeographyBonusModel
    {
        [JsonProperty("subtypeId")] public string subtypeId;
        [JsonProperty("bonus")] public int bonus;
    }

    /// <summary>
    /// Authoring model for a culture catalog. Stores culture entries along with
    /// the definitions of traits, languages and religions. This centralizes
    /// related data so that cultures can reference IDs instead of duplicating
    /// definition fields.
    /// </summary>
    [Serializable]
    public sealed class CultureCatalogDataModel
    {
        [JsonProperty("catalogId")] public string catalogId;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("cultures")] public List<CultureEntryModel> cultures = new List<CultureEntryModel>();
        [JsonProperty("traits")] public List<TraitEntryModel> traits = new List<TraitEntryModel>();
        [JsonProperty("languages")] public List<LanguageEntryModel> languages = new List<LanguageEntryModel>();
        [JsonProperty("religions")] public List<ReligionEntryModel> religions = new List<ReligionEntryModel>();
        [JsonProperty("notes")] [TextArea(2, 10)] public string notes;
    }

    /// <summary>
    /// Authoring model for a single culture entry. Contains identifiers for
    /// traits, languages and religions defined in the parent catalog. Add any
    /// extra metadata (e.g. notes) here that should not exist in runtime.
    /// </summary>
    [Serializable]
    public sealed class CultureEntryModel
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("description")]
        [TextArea(3, 12)] public string description;
        [JsonProperty("traits")] public List<string> traits = new List<string>();
        [JsonProperty("languages")] public List<string> languages = new List<string>();
        [JsonProperty("religions")] public List<string> religions = new List<string>();
        [JsonProperty("notes")] [TextArea(2, 8)] public string notes;
    }

    /// <summary>
    /// Authoring model for a trait definition. Includes a name and an effect
    /// description. Use TextArea for the effect to allow rich editing.
    /// </summary>
    [Serializable]
    public sealed class TraitEntryModel
    {
        [JsonProperty("id")] public string id;
        /// <summary>
        /// Human‑readable display name for the trait.
        /// </summary>
        [JsonProperty("name")] public string name;
        /// <summary>
        /// Description of the trait. Explains the narrative or mechanical meaning of
        /// the trait to the world author. Not used for game mechanics directly.
        /// </summary>
        [JsonProperty("description")] [TextArea(2, 8)] public string description;
        /// <summary>
        /// Numeric bonus (or penalty) value that this trait confers. Positive values
        /// increase the associated stat; negative values decrease it.
        /// </summary>
        [JsonProperty("bonus")] public int bonus;
        /// <summary>
        /// Name of the stat that the bonus applies to (e.g. Strength, Dexterity).
        /// </summary>
        [JsonProperty("stat")] public string stat;
        /// <summary>
        /// Legacy field retained for backwards compatibility. Previously used to
        /// store the effect string (e.g. "Strength:+2"). Not used in new editors,
        /// but preserved to avoid losing data on existing files.
        /// </summary>
        [JsonProperty("effect")] [TextArea(2, 8)] public string effect;
    }

    /// <summary>
    /// Authoring model for a language definition. Includes a name, native
    /// region identifier and description. Use TextArea for description.
    /// </summary>
    [Serializable]
    public sealed class LanguageEntryModel
    {
        [JsonProperty("id")] public string id;
        /// <summary>
        /// Human‑readable display name of the language.
        /// </summary>
        [JsonProperty("name")] public string name;
        /// <summary>
        /// Identifier of the culture considered primary for this language. Cultures
        /// reference languages by ID; this property indicates the culture this
        /// language originates from. Use the editor dropdown to select an existing
        /// culture. If not set, the language has no primary culture.
        /// </summary>
        [JsonProperty("primaryCultureId")] public string primaryCultureId;
        /// <summary>
        /// Legacy field retained for backwards compatibility. Not exposed in new
        /// editors, but still serialized to avoid losing data on existing files.
        /// </summary>
        [JsonProperty("nativeRegionId")] public string nativeRegionId;
        [JsonProperty("description")] [TextArea(2, 8)] public string description;
    }

    /// <summary>
    /// Authoring model for a religion definition. Contains descriptive fields,
    /// a list of traditions, a leader character ID and trait IDs. Use TextArea
    /// for descriptions and allow multiple traditions.
    /// </summary>
    [Serializable]
    public sealed class ReligionEntryModel
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("name")] public string name;
        [JsonProperty("description")] [TextArea(3, 12)] public string description;
        [JsonProperty("traditions")] public List<string> traditions = new List<string>();
        [JsonProperty("religiousLeaderCharacterId")] public string religiousLeaderCharacterId;
        [JsonProperty("traits")] public List<string> traits = new List<string>();
    }

    /// <summary>
    /// Catalog container for trait definitions. Traits defined here can be referenced by
    /// cultures, religions and races. Editing of trait entries should be done only
    /// through a TraitCatalogAuthoringSession.
    /// </summary>
    [Serializable]
    public sealed class TraitCatalogDataModel
    {
        [JsonProperty("catalogId")] public string catalogId;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("traits")] public List<TraitEntryModel> traits = new List<TraitEntryModel>();
        [JsonProperty("notes")] [TextArea(2, 10)] public string notes;
    }

    /// <summary>
    /// Catalog container for language definitions. Each language may reference a primary
    /// culture by ID. Editing of language entries should be done through a
    /// LanguageCatalogAuthoringSession.
    /// </summary>
    [Serializable]
    public sealed class LanguageCatalogDataModel
    {
        [JsonProperty("catalogId")] public string catalogId;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("languages")] public List<LanguageEntryModel> languages = new List<LanguageEntryModel>();
        [JsonProperty("notes")] [TextArea(2, 10)] public string notes;
    }

    /// <summary>
    /// Catalog container for religion definitions. Religions defined here can be assigned
    /// to cultures and races by their IDs. Editing of religion entries should be
    /// performed through a ReligionCatalogAuthoringSession.
    /// </summary>
    [Serializable]
    public sealed class ReligionCatalogDataModel
    {
        [JsonProperty("catalogId")] public string catalogId;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("religions")] public List<ReligionEntryModel> religions = new List<ReligionEntryModel>();
        [JsonProperty("notes")] [TextArea(2, 10)] public string notes;
    }

    /// <summary>
    /// Catalog container for race definitions. Each race can assign traits by ID. Editing
    /// of race entries should be done through a RaceCatalogAuthoringSession.
    /// </summary>
    [Serializable]
    public sealed class RaceCatalogDataModel
    {
        [JsonProperty("catalogId")] public string catalogId;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("races")] public List<RaceEntryModel> races = new List<RaceEntryModel>();
        [JsonProperty("notes")] [TextArea(2, 10)] public string notes;
    }

    /// <summary>
    /// Authoring model for a single race entry. Contains an ID, display name,
    /// description and a list of trait identifiers that apply to this race.
    /// </summary>
    [Serializable]
    public sealed class RaceEntryModel
    {
        [JsonProperty("id")] public string id;
        [JsonProperty("displayName")] public string displayName;
        [JsonProperty("description")] [TextArea(3, 12)] public string description;
        [JsonProperty("traits")] public List<string> traits = new List<string>();
    }
}
