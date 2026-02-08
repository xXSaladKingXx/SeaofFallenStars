using Newtonsoft.Json;
using System.Collections.Generic;

// Settlement data model aligned with world_templates/settlement_template.json. It
// contains nested structures for main details, army (garrison), economy,
// cultural composition, history, vassal contracts and hex coordinates.
namespace SeaOfFallenStars.WorldData
{
    [System.Serializable]
    public class SettlementMain
    {
        [JsonProperty("type")]
        public string type;

        [JsonProperty("population")]
        public int population;

        [JsonProperty("description")]
        public string description;

        // Optional display name of the settlement's ruler.  This property was
        // present in earlier versions of the data model and is used by the
        // InfoWindowManager to populate the main tab.  If it is not
        // available in your JSON, it will simply be null.
        [JsonProperty("rulerDisplayName")]
        public string rulerDisplayName;

        // Optional list of vassal settlement IDs associated with this settlement.  Earlier
        // versions of the UI expected a "vassals" field on the main tab.  When
        // provided in JSON it should contain the IDs of direct vassal settlements.
        // Represented as an array for compatibility with code that references the
        // Length property on this collection.
        [JsonProperty("vassals")]
        public string[] vassals = new string[0];
    }

    [System.Serializable]
    public class SettlementArmy
    {
        // Each garrison entry defines a unit type, its count and condition.
        // Use an array here rather than a list to support code that expects
        // to call .Length on this collection.
        [JsonProperty("garrison")]
        public ArmyUnitStack[] garrison = new ArmyUnitStack[0];

        [JsonProperty("defenses")]
        public string[] defenses = new string[0];

        // Compatibility fields for UI components that expect summary army data.
        // These fields are optional and can be left null in JSON. They were
        // included in earlier versions of the data model.

        // Identifier of the primary commander character.
        [JsonProperty("primaryCommanderCharacterId")]
        public string primaryCommanderCharacterId;

        // Display name of the primary commander character.
        [JsonProperty("primaryCommanderDisplayName")]
        public string primaryCommanderDisplayName;

        // The total size of the army (sum of all unit counts).  If not
        // provided in JSON, UI code will compute it from the garrison list.
        [JsonProperty("totalArmy")]
        public int totalArmy;

        // Legacy list of men‑at‑arms unit names. This was previously used by
        // the InfoWindowManager to display a simple bullet list.  You can
        // populate this array in your JSON or generate it from the garrison
        // entries when saving.
        [JsonProperty("menAtArms")]
        public string[] menAtArms;
    }

    [System.Serializable]
    public class SettlementEconomy
    {
        [JsonProperty("primaryResource")]
        public string primaryResource;

        [JsonProperty("tradeGoods")]
        public string[] tradeGoods = new string[0];

        [JsonProperty("taxRate")]
        public float taxRate;

        [JsonProperty("wealth")]
        public string wealth;

        // Additional economic metrics used by legacy UI components.  These
        // fields are not part of the world_templates schema but are retained
        // for backwards compatibility.  They can be omitted from JSON.

        [JsonProperty("totalIncomePerMonth")]
        public float totalIncomePerMonth;

        [JsonProperty("totalTreasury")]
        public float totalTreasury;

        [JsonProperty("courtExpenses")]
        public string courtExpenses;

        [JsonProperty("armyExpenses")]
        public string armyExpenses;

        [JsonProperty("currentlyConstructing")]
        public string[] currentlyConstructing;
    }

    [System.Serializable]
    public class CultureCompositionEntry
    {
        [JsonProperty("cultureId")]
        public string cultureId;

        [JsonProperty("percentage")]
        public float percentage;
    }

    [System.Serializable]
    public class SettlementCultural
    {
        [JsonProperty("cultureComposition")]
        public List<CultureCompositionEntry> cultureComposition = new List<CultureCompositionEntry>();

        [JsonProperty("languagesSpoken")]
        public string[] languagesSpoken = new string[0];

        [JsonProperty("religionIds")]
        public string[] religionIds = new string[0];

        [JsonProperty("traitIds")]
        public string[] traitIds = new string[0];

        // Legacy fields used by older UI components.  These were originally
        // defined as strings or lists in the previous schema.  They may
        // remain empty or null if not used.

        [JsonProperty("culture")]
        public string culture;

        [JsonProperty("populationDistribution")]
        public string populationDistribution;

        [JsonProperty("primaryTraits")]
        public string[] primaryTraits;

        [JsonProperty("raceDistribution")]
        public List<PercentEntry> raceDistribution = new List<PercentEntry>();

        [JsonProperty("cultureDistribution")]
        public List<PercentEntry> cultureDistribution = new List<PercentEntry>();
    }

    [System.Serializable]
    public class SettlementHistory
    {
        [JsonProperty("founding")]
        public string founding;

        [JsonProperty("notableEvents")]
        public string[] notableEvents = new string[0];

        // Timeline entries and ruling family members were part of earlier
        // versions of the data model.  These optional arrays are retained
        // for compatibility with UI code that may still reference them.
        [JsonProperty("timelineEntries")]
        public string[] timelineEntries;

        [JsonProperty("rulingFamilyMembers")]
        public string[] rulingFamilyMembers;
    }

    // A simple key‑percentage pair used by raceDistribution and cultureDistribution.
    [System.Serializable]
    public class PercentEntry
    {
        [JsonProperty("key")]
        public string key;

        [JsonProperty("percent")]
        public float percent;
    }

    // Feudal information for the settlement.  This class mirrors the earlier
    // settlement feudal structure, allowing the UI to display laws or other
    // feudal data without breaking the new JSON schema.
    [System.Serializable]
    public class SettlementFeudal
    {
        // Laws or legal structure governing the settlement.  Optional.
        [JsonProperty("laws")]
        public string laws;

        // Optional list of vassal contracts associated with the settlement.
        [JsonProperty("contracts")]
        public List<VassalContract> contracts = new List<VassalContract>();

        // Optional capital settlement identifier for this holding.  Some UI
        // components expect a "capitalSettlementId" field under the feudal
        // section rather than at the top level.  When present, this value
        // should mirror the top-level capitalSettlementId.
        [JsonProperty("capitalSettlementId")]
        public string capitalSettlementId;

        // Optional list of vassal contracts for this settlement.  This
        // duplicates the top-level vassalContracts list and exists for
        // compatibility with older code paths that expected it here.
        [JsonProperty("vassalContracts")]
        public VassalContract[] vassalContracts = new VassalContract[0];
    }

    [System.Serializable]
    public class VassalContract
    {
        [JsonProperty("vassalSettlementId")]
        public string vassalSettlementId;

        [JsonProperty("taxRate")]
        public float taxRate;

        [JsonProperty("otherDuties")]
        public string otherDuties;

        [JsonProperty("startDate")]
        public string startDate;

        [JsonProperty("endDate")]
        public string endDate;

        [JsonProperty("notes")]
        public string notes;
    }

    [System.Serializable]
    public class HexCoordinates
    {
        [JsonProperty("x")]
        public int x;
        [JsonProperty("y")]
        public int y;
        [JsonProperty("z")]
        public int z;
    }

    [System.Serializable]
    public class SettlementInfoData
    {
        [JsonProperty("settlementId")]
        public string settlementId;

        [JsonProperty("displayName")]
        public string displayName;

        [JsonProperty("mapUrlOrPath")]
        public string mapUrlOrPath;

        [JsonProperty("layer")]
        public string layer;

        [JsonProperty("isPopulated")]
        public bool isPopulated;

        [JsonProperty("capitalSettlementId")]
        public string capitalSettlementId;

        [JsonProperty("liegeSettlementId")]
        public string liegeSettlementId;

        [JsonProperty("rulerCharacterId")]
        public string rulerCharacterId;

        [JsonProperty("characterIds")]
        public string[] characterIds = new string[0];

        [JsonProperty("main")]
        public SettlementMain main = new SettlementMain();

        [JsonProperty("army")]
        public SettlementArmy army = new SettlementArmy();

        [JsonProperty("economy")]
        public SettlementEconomy economy = new SettlementEconomy();

        [JsonProperty("cultural")]
        public SettlementCultural cultural = new SettlementCultural();

        [JsonProperty("history")]
        public SettlementHistory history = new SettlementHistory();

        [JsonProperty("vassalContracts")]
        public VassalContract[] vassalContracts = new VassalContract[0];

        [JsonProperty("hex")]
        public HexCoordinates hex = new HexCoordinates();

        [JsonProperty("ext")]
        public Dictionary<string, object> ext = new Dictionary<string, object>();

        // Additional notes for editorial purposes
        [JsonProperty("notes")]
        public string notes;

        // Feudal information (legacy).  This property is not part of the
        // world_templates schema but is required by some UI code.  It is
        // optional in JSON and can be null.
        [JsonProperty("feudal")]
        public SettlementFeudal feudal = new SettlementFeudal();
    }
}