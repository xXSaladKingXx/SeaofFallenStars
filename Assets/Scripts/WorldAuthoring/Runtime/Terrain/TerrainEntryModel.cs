using System.Collections.Generic;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Represents a single terrain definition within a terrain catalog.  Each
    /// terrain entry has its own identifier, display name, textual
    /// description and mechanical properties.  Movement modifier is used to
    /// adjust travel speed or cost through the terrain.  Native flora and
    /// fauna lists refer to entries in the flora and fauna catalogs
    /// respectively; these lists can be empty.
    /// </summary>
    [System.Serializable]
    public class TerrainEntryModel
    {
        /// <summary>Unique identifier for this terrain entry.</summary>
        public string id;

        /// <summary>User friendly display name for this terrain entry.</summary>
        public string displayName;

        /// <summary>Free‑form description of the terrain.</summary>
        public string description;

        /// <summary>
        /// Multiplier applied to travel speed or cost.  A value of 1.0
        /// represents normal movement; values above 1.0 slow movement and
        /// values below 1.0 accelerate movement.  The exact meaning is left
        /// to game logic.
        /// </summary>
        public float movementModifier = 1f;

        /// <summary>
        /// Identifiers of flora entries that are native to this terrain.  When
        /// editing in the Unity inspector these should be selected via
        /// dropdown from the FloraCatalog.  The list may be empty.
        /// </summary>
        public List<string> nativeFlora = new List<string>();

        /// <summary>
        /// Identifiers of fauna entries that are native to this terrain.  When
        /// editing in the Unity inspector these should be selected via
        /// dropdown from the FaunaCatalog.  The list may be empty.
        /// </summary>
        public List<string> nativeFauna = new List<string>();

        /// <summary>
        /// Flag indicating whether this terrain entry represents a body of
        /// water.  Water entries may specify a maximum boat size and
        /// subtype.  Non‑water entries ignore these fields.
        /// </summary>
        public bool isWater = false;

        /// <summary>
        /// Maximum vessel size that can safely navigate this water terrain.
        /// Valid values are from 1 (smallest craft) to 6 (largest ships).
        /// This value is only meaningful if <see cref="isWater"/> is true.
        /// </summary>
        public int maxBoatSize = 1;

        /// <summary>
        /// Subtype of water body.  Only applicable when <see cref="isWater"/>
        /// is true.  Supported values are:
        /// "Ocean", "Sea", "Bay", "Lake", "River", "Rapids", "Stream".
        /// </summary>
        public string waterSubtype;
    }
}