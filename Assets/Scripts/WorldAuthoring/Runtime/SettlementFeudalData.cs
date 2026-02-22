using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Standalone feudal data model used by editor tooling when seeding a
    /// settlement from a MapPoint.  This class extends the nested
    /// <see cref="SettlementInfoData.SettlementFeudalData"/> to reuse the
    /// council and vassal contract fields, and adds additional fields such as
    /// settlementId, layer and isPopulated which are used by MapPoint
    /// authoring.
    /// </summary>
    [Serializable]
    public class SettlementFeudalData : SettlementInfoData.SettlementFeudalData
    {
        /// <summary>
        /// Unique identifier for the settlement.  Populated when seeding from a
        /// MapPoint (StableKey).
        /// </summary>
        [JsonProperty("settlementId")] public string settlementId;

        /// <summary>
        /// String representation of the map layer this settlement belongs to.
        /// When seeding from a MapPoint, this is copied from MapPoint.layer.
        /// </summary>
        [JsonProperty("layer")] public string layer;

        /// <summary>
        /// Indicates whether this settlement is populated.  Defaults to true
        /// when creating a new settlement via the editor.
        /// </summary>
        [JsonProperty("isPopulated")] public bool isPopulated;
    }
}