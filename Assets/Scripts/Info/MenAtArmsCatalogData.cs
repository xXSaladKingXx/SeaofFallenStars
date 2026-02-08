using UnityEngine;

/// <summary>
/// Runtime representation of a catalog of men‑at‑arms units. This mirrors the
/// authoring-time catalog but is placed under the Info namespace to avoid
/// dependencies on the authoring assembly. Each entry defines the unit’s
/// stats, descriptive fields and terrain bonuses.
/// </summary>
public class MenAtArmsCatalogData
{
    public string catalogId = "men_at_arms_catalog";
    public string displayName = "Men-at-Arms Catalog";
    public System.Collections.Generic.List<MenAtArmsEntry> entries = new System.Collections.Generic.List<MenAtArmsEntry>();
}

/// <summary>
/// Runtime representation of a single men‑at‑arms unit. See the authoring
/// namespace for additional metadata used in the editor.
/// </summary>
[System.Serializable]
public class MenAtArmsEntry
{
    public string id;
    public string displayName;
    public string notes;
    public int attack;
    public int defense;
    public int size;
    public string role;
    public string qualityTier;
    public System.Collections.Generic.List<GeographyBonus> geographyBonuses = new System.Collections.Generic.List<GeographyBonus>();
}

/// <summary>
/// Defines a bonus applied to a men‑at‑arms unit when fighting in a specific
/// terrain or water subtype. Used to provide situational modifiers based on
/// geography. The subtypeId should match an identifier from your geography
/// catalog (e.g. "forest", "desert", "coastal").
/// </summary>
[System.Serializable]
public class GeographyBonus
{
    public string subtypeId;
    public int bonus;
}
