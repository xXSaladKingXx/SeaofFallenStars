using System;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Represents a single culture assignment with a percentage of the total population.
    /// This type is used by settlement, region and unpopulated authoring sessions to
    /// model populations containing multiple cultures. The percentage should be in the
    /// range [0, 1], where 0.5 represents 50% of the population.
    /// </summary>
    [Serializable]
    public sealed class CultureCompositionEntry
    {
        /// <summary>
        /// The identifier of the culture. This should match a culture entry defined in
        /// a culture catalog. The authoring UI will provide a dropdown list of valid
        /// culture IDs.
        /// </summary>
        public string cultureId;

        /// <summary>
        /// The proportion of the population that belongs to this culture. Values
        /// should sum to 1.0 across all entries, but this is not enforced at runtime.
        /// </summary>
        public float percentage;
    }
}