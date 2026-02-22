using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Backing JSON model for Settlement data.
/// </summary>
[Serializable]
public class SettlementInfoData
{
    [JsonProperty("settlementId")] public string settlementId;
    [JsonProperty("displayName")] public string displayName;
    [JsonProperty("rulerCharacterId")] public string rulerCharacterId;
    [JsonProperty("liegeSettlementId")] public string liegeSettlementId;
    [JsonProperty("mapUrlOrPath")] public string mapUrlOrPath;

    [JsonProperty("main")] public MainTab main = new MainTab();
    [JsonProperty("army")] public ArmyTab army = new ArmyTab();
    [JsonProperty("economy")] public EconomyTab economy = new EconomyTab();
    [JsonProperty("cultural")] public CulturalTab cultural = new CulturalTab();
    [JsonProperty("history")] public SettlementHistoryTab history = new SettlementHistoryTab();
    [JsonProperty("feudal")] public SettlementFeudalData feudal = new SettlementFeudalData();

    #region Tabs

    [Serializable]
    public class MainTab
    {
        [JsonProperty("description")] public string description;
        [JsonProperty("notableFacts")] public List<string> notableFacts = new List<string>();
        [JsonProperty("population")] public int population = 0;
        [JsonProperty("rulerDisplayName")] public string rulerDisplayName;
        [JsonProperty("vassals")] public string[] vassals = Array.Empty<string>();
        [JsonProperty("characterIds")] public string[] characterIds = Array.Empty<string>();
    }

    [Serializable]
    public class ArmyTab
    {
        // List of armies attached to this settlement. All other army fields in this tab are derived.
        [JsonProperty("armyIds")] public string[] armyIds = Array.Empty<string>();

        [JsonProperty("totalArmy")] public int totalArmy = 0;
        [JsonProperty("menAtArms")] public string[] menAtArms = Array.Empty<string>();
        [JsonProperty("primaryCommanderDisplayName")] public string primaryCommanderDisplayName;
        [JsonProperty("primaryCommanderCharacterId")] public string primaryCommanderCharacterId;
    }

    [Serializable]
    public class EconomyTab
    {
        [JsonProperty("mainExports")] public string mainExports;
        [JsonProperty("mainImports")] public string mainImports;
        [JsonProperty("mainIndustries")] public string mainIndustries;
        [JsonProperty("notes")] public string notes;
    }

    [Serializable]
    public class CulturalTab
    {
        [JsonProperty("culture")] public string culture;
        [JsonProperty("raceDistribution")] public List<PercentEntry> raceDistribution = new List<PercentEntry>();
        [JsonProperty("religion")] public string religion;
        [JsonProperty("traits")] public string[] traits = Array.Empty<string>();
        [JsonProperty("languages")] public string[] languages = Array.Empty<string>();
        [JsonProperty("customs")] public string[] customs = Array.Empty<string>();
    }

    [Serializable]
    public class SettlementHistoryTab
    {
        [JsonProperty("notes")] public string notes;
        [JsonProperty("timelineEntries")] public List<TimelineEntry> timelineEntries = new List<TimelineEntry>();
    }

    [Serializable]
    public class SettlementFeudalData
    {
        [JsonProperty("castellanCharacterId")] public string castellanCharacterId;
        [JsonProperty("marshallCharacterId")] public string marshallCharacterId;
        [JsonProperty("stewardCharacterId")] public string stewardCharacterId;
        [JsonProperty("diplomatCharacterId")] public string diplomatCharacterId;
        [JsonProperty("spymasterCharacterId")] public string spymasterCharacterId;
        [JsonProperty("headPriestCharacterId")] public string headPriestCharacterId;
        [JsonProperty("vassalContracts")] public List<VassalContractData> vassalContracts = new List<VassalContractData>();
    }

    [Serializable]
    public class VassalContractData
    {
        [JsonProperty("vassalSettlementId")] public string vassalSettlementId;
        [JsonProperty("incomeTaxRate")] public float incomeTaxRate;
        [JsonProperty("troopTaxRate")] public float troopTaxRate;
        [JsonProperty("terms")] public string terms;
    }

    #endregion
}
