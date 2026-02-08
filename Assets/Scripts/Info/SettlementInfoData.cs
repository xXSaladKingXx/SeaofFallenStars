using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class SettlementInfoData
{
    [JsonProperty("displayName")]
    public string displayName;

    [JsonProperty("mapUrlOrPath")]
    public string mapUrlOrPath;

    // Cross-linking
    [JsonProperty("rulerCharacterId")]
    public string rulerCharacterId;

    [JsonProperty("characterIds")]
    public string[] characterIds = Array.Empty<string>();

    // Tabs
    [JsonProperty("main")]
    public MainTab main = new MainTab();

    [JsonProperty("army")]
    public ArmyTab army = new ArmyTab();

    [JsonProperty("economy")]
    public EconomyTab economy = new EconomyTab();

    [JsonProperty("cultural")]
    public CulturalTab cultural = new CulturalTab();

    [JsonProperty("history")]
    public HistoryTab history = new HistoryTab();

    // Source of truth for hierarchy metadata
    [JsonProperty("feudal")]
    public SettlementFeudalData feudal = new SettlementFeudalData();
   
    

    // ------------------------------------------------------------------
    // Back-compat: MANY scripts still reference these at the root level.
    // These proxy into feudal so JSON stays normalized under "feudal".
    // ------------------------------------------------------------------

    [JsonIgnore]
    public string settlementId
    {
        get => feudal != null ? feudal.settlementId : null;
        set { EnsureFeudal(); feudal.settlementId = value; }
    }

    [JsonIgnore]
    public MapLayer layer
    {
        get => feudal != null ? feudal.layer : default;
        set { EnsureFeudal(); feudal.layer = value; }
    }

    [JsonIgnore]
    public bool isPopulated
    {
        get => feudal == null || feudal.isPopulated;
        set { EnsureFeudal(); feudal.isPopulated = value; }
    }

    [JsonIgnore]
    public string capitalSettlementId
    {
        get => feudal != null ? feudal.capitalSettlementId : null;
        set { EnsureFeudal(); feudal.capitalSettlementId = value; }
    }

    [JsonIgnore]
    public string liegeSettlementId
    {
        get => feudal != null ? feudal.liegeSettlementId : null;
        set { EnsureFeudal(); feudal.liegeSettlementId = value; }
    }

    // SettlementHierarchyCalculator + InfoWindowManager currently expect a root-level "vassalContracts"
    // and element type "SettlementVassalContract". We keep feudal.vassalContracts as the real store
    // and project a view here.
    [JsonIgnore]
    public List<SettlementVassalContract> vassalContracts
    {
        get
        {
            _vassalContractsView ??= new List<SettlementVassalContract>();
            _vassalContractsView.Clear();

            if (feudal?.vassalContracts != null)
            {
                foreach (var c in feudal.vassalContracts)
                {
                    if (c == null) continue;
                    _vassalContractsView.Add(new SettlementVassalContract
                    {
                        vassalSettlementId = c.vassalSettlementId,
                        incomeTaxRate = c.incomeTaxRate,
                        troopTaxRate = c.troopTaxRate,
                        terms = c.terms
                    });
                }
            }

            return _vassalContractsView;
        }
        set
        {
            EnsureFeudal();
            feudal.vassalContracts.Clear();

            if (value == null) return;

            foreach (var c in value)
            {
                if (c == null) continue;
                feudal.vassalContracts.Add(new VassalContractData
                {
                    vassalSettlementId = c.vassalSettlementId,
                    incomeTaxRate = c.incomeTaxRate,
                    troopTaxRate = c.troopTaxRate,
                    terms = c.terms
                });
            }
        }
    }

    [NonSerialized] private List<SettlementVassalContract> _vassalContractsView;

    private void EnsureFeudal()
    {
        feudal ??= new SettlementFeudalData();
    }
}

#region Tabs

[Serializable]
public class MainTab
{
    [JsonProperty("description")]
    [TextArea(3, 12)]
    public string description;

    [JsonProperty("population")]
    public int population;

    [JsonProperty("rulerDisplayName")]
    public string rulerDisplayName;

    // Back-compat: some UI scripts still use "rulerName"
    [JsonIgnore]
    public string rulerName
    {
        get => rulerDisplayName;
        set => rulerDisplayName = value;
    }

    [JsonProperty("vassals")]
    public string[] vassals = Array.Empty<string>();
}

[Serializable]
public class ArmyTab
{
    [JsonProperty("totalArmy")]
    public int totalArmy;

    [JsonProperty("menAtArms")]
    public string[] menAtArms = Array.Empty<string>();

    [JsonProperty("primaryCommanderDisplayName")]
    public string primaryCommanderDisplayName;

    [JsonProperty("primaryCommanderCharacterId")]
    public string primaryCommanderCharacterId;
}

[Serializable]
public class EconomyTab
{
    // Keep float so existing UI formatting and comparisons compile cleanly
    [JsonProperty("totalIncomePerMonth")]
    public float totalIncomePerMonth;

    [JsonProperty("totalTreasury")]
    public float totalTreasury;

    [JsonProperty("courtExpenses")]
    public string courtExpenses;

    [JsonProperty("armyExpenses")]
    public string armyExpenses;

    [JsonProperty("currentlyConstructing")]
    public string[] currentlyConstructing = Array.Empty<string>();
}

[Serializable]
public class CulturalTab
{
    [JsonProperty("culture")]
    public string culture;

    [JsonProperty("populationDistribution")]
    public string populationDistribution;

    [JsonProperty("primaryTraits")]
    public string[] primaryTraits = Array.Empty<string>();

    // Required by SettlementStatsCache
    [JsonProperty("raceDistribution")]
    public List<PercentEntry> raceDistribution = new List<PercentEntry>();

    [JsonProperty("cultureDistribution")]
    public List<PercentEntry> cultureDistribution = new List<PercentEntry>();
}

[Serializable]
public class HistoryTab
{
    [JsonProperty("timelineEntries")]
    public string[] timelineEntries = Array.Empty<string>();

    [JsonProperty("rulingFamilyMembers")]
    public string[] rulingFamilyMembers = Array.Empty<string>();
}

#endregion

#region Feudal + Contracts

[Serializable]
public class SettlementFeudalData
{
    [JsonProperty("settlementId")]
    public string settlementId;

    [JsonProperty("layer")]
    public MapLayer layer;

    [JsonProperty("laws")]
    [TextArea(3, 12)]
    public string laws = "";

    [JsonProperty("isPopulated")]
    public bool isPopulated = true;

    [JsonProperty("capitalSettlementId")]
    public string capitalSettlementId;

    [JsonProperty("liegeSettlementId")]
    public string liegeSettlementId;

    // Source-of-truth storage
    [JsonProperty("vassalContracts")]
    public List<VassalContractData> vassalContracts = new List<VassalContractData>();
}

[Serializable]
public class VassalContractData
{
    [JsonProperty("vassalSettlementId")]
    public string vassalSettlementId;

    // 0..1 (recommended) or 0..100 (if you later decide) – consumers can normalize
    [JsonProperty("incomeTaxRate")]
    public float incomeTaxRate;

    [JsonProperty("troopTaxRate")]
    public float troopTaxRate;

    // IMPORTANT: keep this as string; several UI + cache scripts treat it as string
    [JsonProperty("terms")]
    public string terms;
}

// Alias type expected by some existing scripts
[Serializable]
public class SettlementVassalContract : VassalContractData { }

[Serializable]
public class PercentEntry
{
    [JsonProperty("key")]
    public string key;

    [JsonProperty("percent")]
    public float percent;
}

#endregion

#region Legacy type aliases (required by your current MapPoint fallback code)

[Serializable] public class SettlementMainTab : MainTab { }
[Serializable] public class SettlementArmyData : ArmyTab { }
[Serializable] public class SettlementEconomyData : EconomyTab { }
[Serializable] public class SettlementCulturalData : CulturalTab { }
[Serializable] public class SettlementHistoryData : HistoryTab { }

#endregion

