using System;
using System.Collections.Generic;

/// <summary>
/// Region-level InfoData used for Region/Country/Duchy/Lordship map layers.
///
/// Implemented plan changes:
/// - "Biomes" are removed; only Terrain is tracked.
/// - "Travel notes" and any dominant-biome fields are intentionally absent.
/// - Vassals are stored as MapPoint IDs only (no embedded summary objects).
/// - Derived fields (population, cultures, races, languages, terrain) should be recomputed
///   from child map points and not manually edited.
/// </summary>
[Serializable]
public class RegionInfoData
{
    public string regionId;
    public string displayName;
    public string mapUrlOrPath;

    /// <summary>
    /// String form of the map layer (e.g. "Region", "Country", "Duchy").
    /// Stored as a string to avoid hard-depending on a particular MapLayer enum.
    /// </summary>
    public string layer;

    public RegionMainTabData main = new RegionMainTabData();
    public RegionGeographyTabData geography = new RegionGeographyTabData();

    /// <summary>
    /// Direct vassals (MapPoint IDs). No other data should be stored here.
    /// </summary>
    public List<string> vassals = new List<string>();

    /// <summary>
    /// Computed values derived from all descendant map points. These are not meant
    /// to be directly edited in region authoring UI.
    /// </summary>
    public RegionDerivedInfo derived = new RegionDerivedInfo();
}

[Serializable]
public class RegionMainTabData
{
    public string description;
    public List<string> notableFacts = new List<string>();
}

[Serializable]
public class RegionGeographyTabData
{
    public string overview;
    public string climateNotes;

    /// <summary>
    /// Derived top terrain IDs. This is computed from RegionDerivedInfo.terrainBreakdown.
    /// </summary>
    public List<string> dominantTerrain = new List<string>();
}

[Serializable]
public class RegionDerivedInfo
{
    public int totalPopulation;

    /// <summary>Weighted by settlement population.</summary>
    public List<PercentEntry> raceDistribution = new List<PercentEntry>();

    /// <summary>Weighted by settlement population.</summary>
    public List<PercentEntry> cultureDistribution = new List<PercentEntry>();

    /// <summary>Weighted by settlement population; computed from primaryLanguage.</summary>
    public List<PercentEntry> languageDistribution = new List<PercentEntry>();

    /// <summary>Weighted by unpopulated areaSqMi (if present), otherwise equal-weighted.</summary>
    public List<PercentEntry> terrainBreakdown = new List<PercentEntry>();
}
