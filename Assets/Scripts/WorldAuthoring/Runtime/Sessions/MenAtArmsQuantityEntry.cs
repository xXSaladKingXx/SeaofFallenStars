using System;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Represents a men-at-arms type assignment with a quantity of units.
    /// The unit count is later multiplied by the unit size defined in the men-at-arms catalog
    /// to compute total troops for that type.
    /// </summary>
    [Serializable]
    public sealed class MenAtArmsQuantityEntry
    {
        /// <summary>
        /// Identifier of the men-at-arms type as defined in the global men-at-arms catalog.
        /// </summary>
        public string menAtArmsId;

        /// <summary>
        /// Number of units of this type.
        /// </summary>
        public int units;
    }
}
