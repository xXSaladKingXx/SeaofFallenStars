using System;
using System.Collections.Generic;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Data model representing a catalog of relationship types.  Each entry in the catalog
    /// defines a relationship between one or more entities (characters, settlements,
    /// armies, etc.) and specifies whether the relationship is directional or
    /// bidirectional.  Authors create a single instance of this catalog and populate
    /// it with all supported relationship definitions (e.g. Father→Child, Siblings↔,
    /// Liege→Vassal, Vassal→Liege, Knight↔Squire, At War↔, Alliance↔, etc.).  The
    /// relationship catalog is referenced by character and settlement data to
    /// provide a consistent set of relationship semantics across the world.
    /// </summary>
    [Serializable]
    public sealed class RelationshipCatalogDataModel
    {
        /// <summary>
        /// Unique identifier for this catalog.  There should only ever be one
        /// relationship catalog in a world, so this can remain the default value.
        /// </summary>
        public string catalogId = "relationship_catalog";

        /// <summary>
        /// Human‑readable name for the catalog.  Displayed in the authoring UI.
        /// </summary>
        public string displayName = "Relationship Catalog";

        /// <summary>
        /// Freeform notes associated with the catalog.
        /// </summary>
        public string notes;

        /// <summary>
        /// List of relationship type definitions.  Each entry defines the
        /// participant constraints, directionality and display name for a
        /// relationship.
        /// </summary>
        public List<RelationshipTypeModel> entries = new List<RelationshipTypeModel>();

        /// <summary>
        /// Alias for entries to maintain consistency with other catalog models.
        /// </summary>
        public List<RelationshipTypeModel> relationships => entries;
    }

    /// <summary>
    /// Enumeration of high‑level relationship categories.  These categories can
    /// be used to group relationships in the authoring UI and help authors
    /// quickly locate the desired type when assigning relationships to
    /// characters or settlements.
    /// </summary>
    public enum RelationshipCategory
    {
        Family,
        Employment,
        Feudal,
        Diplomatic,
        Other
    }

    /// <summary>
    /// Enumeration describing whether a relationship is directional (one way)
    /// or bidirectional (two way).  Directional relationships have a clear
    /// source and target (e.g. Father→Child, Liege→Vassal).  Bidirectional
    /// relationships apply equally to both participants (e.g. Spouse↔, Siblings↔,
    /// Alliance↔).
    /// </summary>
    public enum RelationshipDirection
    {
        OneWay,
        TwoWay
    }

    /// <summary>
    /// Defines a single relationship type.  An author can specify the
    /// display name, category, directionality and participant constraints.
    /// Participant constraints describe which kinds of entities may appear on
    /// each side of the relationship and how many participants are required.
    /// </summary>
    [Serializable]
    public class RelationshipTypeModel
    {
        /// <summary>
        /// Unique identifier for this relationship type.  Used as the key when
        /// referencing relationships in character or settlement data.
        /// </summary>
        public string id;

        /// <summary>
        /// Human‑readable name for the relationship type (e.g. "Father", "Sibling",
        /// "Liege", "Vassal", "Alliance").  Displayed in the authoring UI.
        /// </summary>
        public string displayName;

        /// <summary>
        /// Category grouping for this relationship type.
        /// </summary>
        public RelationshipCategory category = RelationshipCategory.Other;

        /// <summary>
        /// Directionality of the relationship (OneWay or TwoWay).
        /// </summary>
        public RelationshipDirection direction = RelationshipDirection.TwoWay;

        /// <summary>
        /// Minimum number of participants required on side A (source) of the
        /// relationship.  For example, Kill requires at least one attacker on
        /// side A and one victim on side B.
        /// </summary>
        public int minSideA = 1;

        /// <summary>
        /// Minimum number of participants required on side B (target) of the
        /// relationship.
        /// </summary>
        public int minSideB = 1;

        /// <summary>
        /// Allowed participant types on side A.  These strings should match
        /// entity categories such as "Character", "Settlement", "Army",
        /// "TravelGroup".  Authors can restrict relationships to specific
        /// entity kinds.
        /// </summary>
        public string[] sideAParticipantTypes;

        /// <summary>
        /// Allowed participant types on side B.
        /// </summary>
        public string[] sideBParticipantTypes;
    }
}