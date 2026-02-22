using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Standalone version of the Settlement main tab used by editor tooling.  This
    /// class inherits from <see cref="SettlementInfoData.MainTab"/> so that all
    /// properties defined on the nested main tab (population, notable facts,
    /// vassals, characterIds, etc.) are available.  The editor seeds a new
    /// instance of this type when creating a settlement from a MapPoint.
    /// </summary>
    [Serializable]
    public class MainTab : SettlementInfoData.MainTab
    {
        // No additional fields are needed; the nested MainTab already defines
        // description, notableFacts, population, rulerDisplayName, rulerName,
        // vassals and characterIds.  We simply inherit to satisfy editor
        // references to a top-level MainTab type.
    }
}