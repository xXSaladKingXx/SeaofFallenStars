using System;

/// <summary>
/// Represents a percentage entry with an associated key.
/// This simple structure is used throughout the project to
/// store percentage distributions (e.g., race, culture, language).
/// </summary>
[Serializable]
public class PercentEntry
{
    /// <summary>
    /// Identifier or name for this entry.
    /// </summary>
    public string key;

    /// <summary>
    /// Percentage value (0–100).
    /// </summary>
    public float percent;
}
