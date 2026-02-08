using Newtonsoft.Json;

// This class provides backward compatibility for code that still references
// `SettlementVassalContract` in the `SettlementHierarchyCalculator`. It
// inherits from `VassalContract`, so it has the same properties (vassalSettlementId,
// taxRate, otherDuties, startDate, endDate, notes) and does not introduce
// any new serialization keys. If you no longer need this alias, update
// your code to reference `VassalContract` directly.
namespace SeaOfFallenStars.WorldData
{
    [System.Serializable]
    public class SettlementVassalContract : VassalContract
    {
        // No additional fields; this class exists solely for backward compatibility.
    }
}