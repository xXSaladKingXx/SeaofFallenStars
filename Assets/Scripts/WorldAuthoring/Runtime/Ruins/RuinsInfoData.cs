using System.Collections.Generic;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Data model for an individual ruin.  A ruin represents an unpopulated
    /// location of historical or exploratory interest.  Each ruin has
    /// its own identifier, display name, description and an optional set
    /// of timeline event identifiers referencing the world timeline.
    /// </summary>
    [System.Serializable]
    public class RuinsInfoData
    {
        /// <summary>Unique identifier for this ruin.</summary>
        public string ruinId;

        /// <summary>User friendly display name for this ruin.</summary>
        public string displayName;

        /// <summary>Descriptive text about the ruin.</summary>
        public string description;

        /// <summary>
        /// Timeline events associated with this ruin.  Each entry should
        /// correspond to an event identifier defined in the TimelineCatalog.
        /// The list may be empty.
        /// </summary>
        public List<string> timelineEventIds = new List<string>();
    }
}