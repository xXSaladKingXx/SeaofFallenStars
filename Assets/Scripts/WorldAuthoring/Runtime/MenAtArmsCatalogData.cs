using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime representation of a catalog of men‑at‑arms units. This mirrors the
/// authoring-time catalog but is placed under the Info namespace to avoid
/// dependencies on the authoring assembly. Each entry defines the unit’s
/// stats, descriptive fields and terrain bonuses.
/// </summary>
// Remove namespace so MenAtArmsCatalogData and MenAtArmsEntry are at global scope.  This aligns
// with existing project files that reference these types without a namespace.

public class MenAtArmsCatalogData
    {
        public string catalogId = "men_at_arms_catalog";
        public string displayName = "Men-at-Arms Catalog";
        public List<MenAtArmsEntry> entries = new List<MenAtArmsEntry>();
    }

    /// <summary>
    /// Runtime representation of a single men‑at‑arms unit.  Updated to include
    /// movement speed and maintenance costs in addition to combat stats.
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
        public List<GeographyBonus> geographyBonuses = new List<GeographyBonus>();

        /// <summary>
        /// Movement speed of this unit.  Defaults to 20.  If the unit is faster than 20 and no levies are present
        /// in an army, the army’s speed may be raised to match this value.
        /// </summary>
        public float speed = 20f;

        /// <summary>
        /// Monthly maintenance cost when the unit is raised for war.  This cost is
        /// aggregated into the settlement’s raised maintenance expenses.
        /// </summary>
        public float raisedMaintenanceCost = 0f;

        /// <summary>
        /// Monthly maintenance cost when the unit is unraised.  This cost is
        /// aggregated into the settlement’s unraised maintenance expenses.
        /// </summary>
        public float unraisedMaintenanceCost = 0f;
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