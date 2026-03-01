using System.Collections.Generic;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Represents a catalog of terrain definitions.  Each catalog has a
    /// unique identifier and optional display name and notes.  The
    /// contained list of entries defines individual terrain types that
    /// can be referenced elsewhere in the world authoring system.
    /// </summary>
    [System.Serializable]
    public class TerrainCatalogDataModel
    {
        /// <summary>Unique identifier for this catalog file.</summary>
        public string catalogId;

        /// <summary>Optional friendly name for the catalog.</summary>
        public string displayName;

        /// <summary>Optional notes about the catalog.</summary>
        public string notes;

        /// <summary>
        /// Collection of terrain entries contained within the catalog.  When
        /// editing in the Unity inspector this list is manipulated via the
        /// TerrainCatalogAuthoringSessionEditor.
        /// </summary>
        public List<TerrainEntryModel> entries = new List<TerrainEntryModel>();
    }
}