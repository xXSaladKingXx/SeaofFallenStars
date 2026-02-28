using System;
using Newtonsoft.Json;

/// <summary>
/// Represents a distribution entry consisting of a key (e.g. culture or race id) and
/// a percentage value.  Used in cultural and race distribution lists on settlements.
/// </summary>
[Serializable]
public class PercentEntry
{
    /// <summary>
    /// Identifier for the culture, race, or other category represented by this entry.
    /// </summary>
    [JsonProperty("key")] public string key;

    /// <summary>
    /// Percentage share of the category within the settlement's population (0–100).
    /// </summary>
    [JsonProperty("percent")] public float percent;
}