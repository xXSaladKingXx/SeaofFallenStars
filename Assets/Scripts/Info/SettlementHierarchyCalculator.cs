using System;
using System.Collections.Generic;
using UnityEngine;

public static class SettlementHierarchyCalculator
{
    public class ComputedStats
    {
        public string settlementId;

        // Realm totals (additive; NOT reduced by taxes)
        public int realmPopulation;
        public int realmTroops;
        public float realmIncomeGross;

        // Ruler “take” (what this settlement’s ruler directly controls/collects)
        public int rulerTroopsNet;
        public float rulerIncomeNet;

        // If this settlement owes a liege and we can resolve the contract,
        // these are the post-tax net values for THIS ruler.
        public int rulerTroopsAfterLiegeTax;
        public float rulerIncomeAfterLiegeTax;

        public string resolvedLiegeSettlementId;
        public float resolvedLiegeIncomeTaxRate;
        public float resolvedLiegeTroopTaxRate;
    }

    private static readonly Dictionary<string, ComputedStats> _cache = new Dictionary<string, ComputedStats>();
    private static readonly Dictionary<string, string> _parentIndex = new Dictionary<string, string>();
    private static bool _indexBuilt;

    public static void ClearCache()
    {
        _cache.Clear();
        _parentIndex.Clear();
        _indexBuilt = false;
    }

    // Builds child->parent using scene MapPoints (preferred, because that matches your inspector-driven hierarchy).
    public static void BuildParentIndexFromScene()
    {
        _parentIndex.Clear();

        var points = UnityEngine.Object.FindObjectsByType<MapPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in points)
        {
            if (p == null) continue;
            var children = p.GetChildren();
            if (children == null) continue;

            for (int i = 0; i < children.Count; i++)
            {
                var c = children[i];
                if (c == null) continue;

                string childId = c.GetStableKey();
                string parentId = p.GetStableKey();

                if (string.IsNullOrWhiteSpace(childId) || string.IsNullOrWhiteSpace(parentId))
                    continue;

                // first parent wins
                if (!_parentIndex.ContainsKey(childId))
                    _parentIndex.Add(childId, parentId);
            }
        }

        _indexBuilt = true;
        _cache.Clear();
    }

    public static ComputedStats Compute(string settlementId)
    {
        if (string.IsNullOrWhiteSpace(settlementId))
            return null;

        if (!_indexBuilt)
            BuildParentIndexFromScene();

        if (_cache.TryGetValue(settlementId, out var cached) && cached != null)
            return cached;

        var data = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
            DataPaths.Runtime_MapDataPath,
            DataPaths.Editor_MapDataPath,
            settlementId
        );

        // If no JSON exists, return null (MapPoint fallback is handled elsewhere)
        if (data == null)
            return null;

        if (!data.isPopulated)
            return null;

        var result = new ComputedStats();
        result.settlementId = settlementId;

        // Determine direct vassals (excluding capital)
        var vassals = (data.main != null && data.main.vassals != null) ? data.main.vassals : Array.Empty<string>();

        // Capital acts like a vassal with 0 tax, fully counted both in realm totals and ruler take
        if (!string.IsNullOrWhiteSpace(data.capitalSettlementId))
        {
            var cap = Compute(data.capitalSettlementId);
            if (cap != null)
            {
                result.realmPopulation += cap.realmPopulation;
                result.realmTroops += cap.realmTroops;
                result.realmIncomeGross += cap.realmIncomeGross;

                // capital is fully held by ruler
                result.rulerTroopsNet += cap.rulerTroopsAfterLiegeTax > 0 ? cap.rulerTroopsAfterLiegeTax : cap.rulerTroopsNet;
                result.rulerIncomeNet += cap.rulerIncomeAfterLiegeTax > 0 ? cap.rulerIncomeAfterLiegeTax : cap.rulerIncomeNet;
            }
        }
        else
        {
            // If no capital defined, point-tier settlements can contribute their own base values.
            // For non-point tiers, you typically leave these 0 and rely on vassals.
            int basePop = (data.main != null) ? data.main.population : 0;
            int baseTroops = SettlementArmyResolver.Resolve(data).totalTroops;
            float baseIncome = (data.economy != null) ? data.economy.totalIncomePerMonth : 0f;

            // Treat these as both realm and ruler take for “single holding” settlements
            result.realmPopulation += basePop;
            result.realmTroops += baseTroops;
            result.realmIncomeGross += baseIncome;

            result.rulerTroopsNet += baseTroops;
            result.rulerIncomeNet += baseIncome;
        }

        // Aggregate from direct vassals
        for (int i = 0; i < vassals.Length; i++)
        {
            string childId = vassals[i];
            if (string.IsNullOrWhiteSpace(childId)) continue;

            // Safety: ignore if it equals capital
            if (!string.IsNullOrWhiteSpace(data.capitalSettlementId) && childId == data.capitalSettlementId)
                continue;

            var child = Compute(childId);
            if (child == null) continue;

            // Realm totals are additive (not reduced by taxes)
            result.realmPopulation += child.realmPopulation;
            result.realmTroops += child.realmTroops;
            result.realmIncomeGross += child.realmIncomeGross;

            // Determine contract for THIS direct vassal (liege-side)
            float incomeRate = 0f;
            float troopRate = 0f;

            var contract = FindContract(data.vassalContracts, childId);
            if (contract != null)
            {
                incomeRate = Mathf.Clamp01(contract.incomeTaxRate);
                troopRate = Mathf.Clamp01(contract.troopTaxRate);
            }

            // Tax is assessed against the vassal ruler’s net (what they control after collecting from THEIR vassals)
            float taxedIncome = child.rulerIncomeNet * incomeRate;
            float taxedTroopsF = child.rulerTroopsNet * troopRate;

            result.rulerIncomeNet += taxedIncome;
            result.rulerTroopsNet += Mathf.RoundToInt(taxedTroopsF);
        }

        // Resolve liege contract for “after liege tax” display
        ResolveLiegeTax(result, data);

        _cache[settlementId] = result;
        return result;
    }

    private static SettlementVassalContract FindContract(List<SettlementVassalContract> list, string vassalId)
    {
        if (list == null || list.Count == 0 || string.IsNullOrWhiteSpace(vassalId))
            return null;

        for (int i = 0; i < list.Count; i++)
        {
            var c = list[i];
            if (c == null) continue;
            if (c.vassalSettlementId == vassalId)
                return c;
        }

        return null;
    }

    private static void ResolveLiegeTax(ComputedStats stats, SettlementInfoData selfData)
    {
        stats.rulerIncomeAfterLiegeTax = stats.rulerIncomeNet;
        stats.rulerTroopsAfterLiegeTax = stats.rulerTroopsNet;

        string liegeId = null;

        // Prefer explicit field
        if (!string.IsNullOrWhiteSpace(selfData.liegeSettlementId))
            liegeId = selfData.liegeSettlementId;
        else
        {
            // Fall back to scene-derived parent
            _parentIndex.TryGetValue(stats.settlementId, out liegeId);
        }

        if (string.IsNullOrWhiteSpace(liegeId))
            return;

        stats.resolvedLiegeSettlementId = liegeId;

        var liegeData = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
            DataPaths.Runtime_MapDataPath,
            DataPaths.Editor_MapDataPath,
            liegeId
        );

        if (liegeData == null || liegeData.vassalContracts == null)
            return;

        var contract = FindContract(liegeData.vassalContracts, stats.settlementId);
        if (contract == null)
            return;

        float incomeRate = Mathf.Clamp01(contract.incomeTaxRate);
        float troopRate = Mathf.Clamp01(contract.troopTaxRate);

        stats.resolvedLiegeIncomeTaxRate = incomeRate;
        stats.resolvedLiegeTroopTaxRate = troopRate;

        stats.rulerIncomeAfterLiegeTax = stats.rulerIncomeNet * (1f - incomeRate);
        stats.rulerTroopsAfterLiegeTax = Mathf.RoundToInt(stats.rulerTroopsNet * (1f - troopRate));
    }
}
