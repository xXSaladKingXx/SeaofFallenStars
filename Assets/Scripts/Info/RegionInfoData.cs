using System;
using System.Collections.Generic;

/// <summary>
/// Region-level InfoData used for Region/Country/Duchy/Lordship map layers.
///
/// Key design notes:
/// - Only terrain is tracked; biomes and travel notes are intentionally absent.
/// - Vassals are stored as MapPoint IDs only (no embedded summary objects).
/// - Derived fields (population, cultures, races, languages, terrain) should be recomputed
///   from child map points and not manually edited.
/// </summary>
[Serializable]
public class RegionInfoData
{
    /// <summary>
    /// Unique identifier for this region (typically the MapPoint ID).
    /// </summary>
    public string regionId;

    /// <summary>
    /// Human-readable display name for UI.
    /// </summary>
    public string displayName;

    /// <summary>
    /// Relative or absolute URL/path to an image of the region map.
    /// </summary>
    public string mapUrlOrPath;

    /// <summary>
    /// String form of the map layer (e.g. "Region", "Country", "Duchy").
    /// Stored as a string to avoid hard-depending on a particular MapLayer enum.
    /// </summary>
    public string layer;

    /// <summary>
    /// Main tab data (description, notable facts).
    /// </summary>
    public RegionMainTabData main = new RegionMainTabData();

    /// <summary>
    /// Geography tab data (overview, climate notes, terrain dominance).
    /// </summary>
    public RegionGeographyTabData geography = new RegionGeographyTabData();

    /// <summary>
    /// Direct vassals (MapPoint IDs). No other data should be stored here.
    /// </summary>
    public List<string> vassals = new List<string>();

    /// <summary>
    /// Computed values derived from all descendant map points.
    /// These are not meant to be directly edited in region authoring UI.
    /// </summary>
    public RegionDerivedInfo derived = new RegionDerivedInfo();
}

/// <summary>
/// Data for the "Main" tab of a region.
/// </summary>
[Serializable]
public class RegionMainTabData
{
    /// <summary>
    /// Descriptive text for this region.
    /// </summary>
    public string description;

    /// <summary>
    /// Notable facts or bullet points about this region.
    /// </summary>
    public List<string> notableFacts = new List<string>();
}

/// <summary>
/// Data for the "Geography" tab of a region.
/// </summary>
[Serializable]
public class RegionGeographyTabData
{
    /// <summary>
    /// Overview description of the region's geography.
    /// </summary>
    public string overview;

    /// <summary>
    /// Notes about the region's climate.
    /// </summary>
    public string climateNotes;

    /// <summary>
    /// Derived top terrain IDs. This is computed from RegionDerivedInfo.terrainBreakdown.
    /// </summary>
    public List<string> dominantTerrain = new List<string>();
}

/// <summary>
/// Derived statistics for a region, computed from all descendant map points.
/// </summary>
[Serializable]
public class RegionDerivedInfo
{
    /// <summary>
    /// Sum of settlement populations within this region (additive from descendants).
    /// </summary>
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