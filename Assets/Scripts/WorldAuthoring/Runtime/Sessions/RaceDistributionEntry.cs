using System;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Represents a single race assignment with a percentage of the total population.
    /// Percentages are expressed as fractions in the range [0, 1], where 0.5 represents 50%.
    /// </summary>
    [Serializable]
    public sealed class RaceDistributionEntry
    {
        /// <summary>
        /// Identifier of the race as defined in the global race catalog.
        /// </summary>
        public string raceId;

        /// <summary>
        /// Fraction of the population belonging to this race.
        /// </summary>
        public float percentage;
    }
}
