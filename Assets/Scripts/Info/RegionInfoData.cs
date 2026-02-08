using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class RegionInfoData
{
    [JsonProperty("regionId")]
    public string regionId;

    [JsonProperty("displayName")]
    public string displayName;

    // Used by the "Open Region Map" button
    [JsonProperty("mapUrlOrPath")]
    public string mapUrlOrPath;

    [JsonProperty("layer")]
    public MapLayer layer = MapLayer.Regional;

    // Tabs
    [JsonProperty("main")]
    public RegionMainTab main = new RegionMainTab();

    // NOTE: Your RegionInfoWindowManager currently computes geography values from colliders and MapPoints,
    // but having a geography object in JSON makes the Region template "complete" and future-proof.
    [JsonProperty("geography")]
    public RegionGeographyTab geography = new RegionGeographyTab();

    [JsonProperty("culture")]
    public RegionCultureTab culture = new RegionCultureTab();

    //  New: explicit vassal countries list (authoritative political list)
    [JsonProperty("vassals")]
    public RegionVassalsTab vassals = new RegionVassalsTab();

    // Optional extension object (future-proof)
    [JsonProperty("ext")]
    public Dictionary<string, object> ext = new Dictionary<string, object>();

    // Back-compat convenience alias
    [JsonIgnore]
    public string id
    {
        get => !string.IsNullOrWhiteSpace(regionId) ? regionId : null;
        set => regionId = value;
    }
}

[Serializable]
public class RegionMainTab
{
    [JsonProperty("description")]
    [TextArea(3, 12)]
    public string description = "";

    // Optional: quick lore snippets you might want later (not required by current UI)
    [JsonProperty("tagline")]
    public string tagline = "";

    [JsonProperty("notableFacts")]
    public List<string> notableFacts = new List<string>();
}

[Serializable]
public class RegionGeographyTab
{
    // Curated text you may want even though your UI currently computes the numeric parts.
    [JsonProperty("overview")]
    [TextArea(2, 12)]
    public string overview = "";

    // Optional overrides / editorial values (if you ever want to show fixed numbers instead of collider-derived)
    [JsonProperty("areaSqMiOverride")]
    public float? areaSqMiOverride = null;

    [JsonProperty("unityUnitsToMilesOverride")]
    public float? unityUnitsToMilesOverride = null;

    // Optional lists for lore / encyclopedia feel
    [JsonProperty("dominantBiomes")]
    public List<string> dominantBiomes = new List<string>();

    [JsonProperty("dominantTerrain")]
    public List<string> dominantTerrain = new List<string>();

    [JsonProperty("climateNotes")]
    [TextArea(2, 12)]
    public string climateNotes = "";

    [JsonProperty("travelNotes")]
    [TextArea(2, 12)]
    public string travelNotes = "";
}

[Serializable]
public class RegionCultureTab
{
    [JsonProperty("entries")]
    public List<RegionCultureEntry> entries = new List<RegionCultureEntry>();
}

[Serializable]
public class RegionCultureEntry
{
    [JsonProperty("name")]
    public string name;

    [JsonProperty("description")]
    [TextArea(2, 12)]
    public string description;
}

[Serializable]
public class RegionVassalsTab
{
    [JsonProperty("countries")]
    public List<RegionVassalCountry> countries = new List<RegionVassalCountry>();

    [JsonProperty("notes")]
    [TextArea(2, 12)]
    public string notes = "";
}

[Serializable]
public class RegionVassalCountry
{
    // Should match the MapPoint.pointId of the Country-layer MapPoint (recommended),
    // or your stable filename key for that country’s JSON.
    [JsonProperty("countryId")]
    public string countryId;

    // Optional: show-name override if you don't want to rely on the MapPoint’s displayName
    [JsonProperty("displayName")]
    public string displayName;

    // Optional: political relationship label (“Vassal”, “Protectorate”, “March”, etc.)
    [JsonProperty("relationship")]
    public string relationship = "Vassal";

    // Optional: short one-liner for the relationship / obligations / quirks
    [JsonProperty("summary")]
    public string summary = "";

    // Optional: if you want “at a glance” stats later (not required by current UI)
    [JsonProperty("estimatedPopulation")]
    public int? estimatedPopulation = null;

    [JsonProperty("estimatedIncome")]
    public float? estimatedIncome = null;

    [JsonProperty("estimatedLevies")]
    public int? estimatedLevies = null;

    [JsonProperty("ext")]
    public Dictionary<string, object> ext = new Dictionary<string, object>();
}
