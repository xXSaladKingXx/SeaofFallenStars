using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class UnpopulatedInfoData
{
    [JsonProperty("areaId")]
    public string areaId;

    [JsonProperty("displayName")]
    public string displayName;

    [JsonProperty("mapUrlOrPath")]
    public string mapUrlOrPath;

    [JsonProperty("layer")]
    public MapLayer layer;

    // Always false for these records, but keep it to satisfy callers
    [JsonProperty("isPopulated")]
    public bool isPopulated = false;

    // NEW (optional): authoring hint; MapPoint can optionally read this and set its enum.
    // Expected values: "Wilderness" | "Water" | "Ruins"
    [JsonProperty("subtype")]
    public string subtype;

    [JsonProperty("main")]
    public UnpopulatedMainTab main = new UnpopulatedMainTab();

    [JsonProperty("geography")]
    public UnpopulatedGeographyTab geography = new UnpopulatedGeographyTab();

    [JsonProperty("nature")]
    public UnpopulatedNatureTab nature = new UnpopulatedNatureTab();

    [JsonProperty("history")]
    public UnpopulatedHistoryTab history = new UnpopulatedHistoryTab();

    // NEW: used primarily by Ruins subtype
    [JsonProperty("culture")]
    public UnpopulatedCultureTab culture = new UnpopulatedCultureTab();

    // NEW: used primarily by Water subtype
    [JsonProperty("water")]
    public UnpopulatedWaterTab water = new UnpopulatedWaterTab();

    // --------------------------------------------------------------------
    // Back-compat: MapPoint.cs expects UnpopulatedInfoData.vassals
    // Canonical storage remains main.vassals (JSON: main.vassals).
    // --------------------------------------------------------------------
    [JsonIgnore]
    public string[] vassals
    {
        get => main != null && main.vassals != null ? main.vassals : Array.Empty<string>();
        set
        {
            if (main == null) main = new UnpopulatedMainTab();
            main.vassals = value ?? Array.Empty<string>();
        }
    }
}

[Serializable]
public class UnpopulatedMainTab
{
    [JsonProperty("description")]
    [TextArea(3, 12)]
    public string description;

    [JsonProperty("vassals")]
    public string[] vassals = Array.Empty<string>();
}

[Serializable]
public class UnpopulatedGeographyTab
{
    // Source-of-truth
    [JsonProperty("areaSqMi")]
    public float areaSqMi;

    // Back-compat alias (your MapPoint currently expects squareMiles)
    [JsonIgnore]
    public float squareMiles
    {
        get => areaSqMi;
        set => areaSqMi = value;
    }

    // Wilderness: use one of the allowed terrain types.
    // Water: you can use "Coastal" or leave blank.
    // Ruins: leave blank or describe surrounding terrain.
    [JsonProperty("terrainType")]
    public string terrainType;

    // Keep notes (lots of systems expect this)
    [JsonProperty("notes")]
    [TextArea(2, 10)]
    public string notes;

    // Optional breakdown (mostly useful for Wilderness)
    [JsonProperty("terrainBreakdown")]
    public List<TerrainBreakdownEntry> terrainBreakdown = new List<TerrainBreakdownEntry>();
}

[Serializable]
public class TerrainBreakdownEntry
{
    [JsonProperty("terrainType")]
    public string terrainType;

    // Prefer 0..100 in authoring
    [JsonProperty("percent")]
    public float percent;
}

[Serializable]
public class UnpopulatedNatureTab
{
    [JsonProperty("flora")]
    [TextArea(2, 12)]
    public string flora;

    [JsonProperty("fauna")]
    [TextArea(2, 12)]
    public string fauna;

    [JsonProperty("resources")]
    [TextArea(2, 12)]
    public string resources;
}

[Serializable]
public class UnpopulatedHistoryTab
{
    [JsonProperty("notes")]
    [TextArea(2, 12)]
    public string notes;

    [JsonProperty("timelineEntries")]
    public string[] timelineEntries = Array.Empty<string>();
}

[Serializable]
public class UnpopulatedCultureTab
{
    [JsonProperty("notes")]
    [TextArea(2, 12)]
    public string notes;

    [JsonProperty("peoples")]
    public string[] peoples = Array.Empty<string>();

    [JsonProperty("factions")]
    public string[] factions = Array.Empty<string>();

    [JsonProperty("languages")]
    public string[] languages = Array.Empty<string>();

    [JsonProperty("customs")]
    [TextArea(2, 12)]
    public string customs;

    [JsonProperty("rumors")]
    public string[] rumors = Array.Empty<string>();
}

[Serializable]
public class UnpopulatedWaterTab
{
    // e.g. "River", "Lake", "Sea", "Ocean", "Reef", "Strait", "Bay", "Fjord"
    [JsonProperty("waterBodyType")]
    public string waterBodyType;

    // e.g. "Freshwater", "Saltwater", "Brackish"
    [JsonProperty("waterType")]
    public string waterType;

    [JsonProperty("depth")]
    public string depth;

    [JsonProperty("currents")]
    [TextArea(2, 12)]
    public string currents;

    [JsonProperty("hazards")]
    [TextArea(2, 12)]
    public string hazards;

    [JsonProperty("notableFeatures")]
    public string[] notableFeatures = Array.Empty<string>();

    [JsonProperty("notes")]
    [TextArea(2, 12)]
    public string notes;
}
