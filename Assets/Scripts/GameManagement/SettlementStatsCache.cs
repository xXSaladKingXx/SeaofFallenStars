using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Computes realm totals bottom-up using direct vassal lists + vassal contracts stored on the liege settlement JSON.
/// - Income and Troops are taxed.
/// - Population is additive only (not taxed).
/// - Country capital is included at 0% income/troop tax.
/// 
/// This cache is safe to call from any UI panel; it rebuilds lazily.
/// </summary>
public static class SettlementStatsCache
{
    public class VassalComputedSummary
    {
        public string vassalSettlementId;
        public string vassalDisplayName;

        public float incomeTaxRate;
        public float troopTaxRate;
        public string terms;

        public double vassalGrossIncome;
        public double incomePaidUp;
        public double vassalNetIncome;

        public int vassalGrossTroops;
        public int troopsPaidUp;
        public int vassalNetTroops;

        public int vassalTotalPopulation;
        public bool isCapital;
    }

    public class SettlementComputedStats
    {
        public string settlementId;

        public double localIncome;
        public int localTroops;
        public int localPopulation;

        public double grossIncome;
        public int grossTroops;
        public int totalPopulation;

        public double netIncome;
        public int netTroops;

        public double incomePaidUp;
        public int troopsPaidUp;

        public Dictionary<string, int> populationByRace = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> populationByCulture = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public List<VassalComputedSummary> directVassals = new List<VassalComputedSummary>();
    }

    private static bool _built;
    private static readonly Dictionary<string, SettlementInfoData> _dataById = new Dictionary<string, SettlementInfoData>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SettlementComputedStats> _statsById = new Dictionary<string, SettlementComputedStats>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static void Invalidate()
    {
        _built = false;
        _dataById.Clear();
        _statsById.Clear();
        _visiting.Clear();
    }

    public static bool TryGetStats(string settlementId, out SettlementComputedStats stats)
    {
        EnsureBuilt();
        return _statsById.TryGetValue(settlementId ?? "", out stats);
    }

    public static SettlementComputedStats GetStatsOrNull(string settlementId)
    {
        EnsureBuilt();
        _statsById.TryGetValue(settlementId ?? "", out var stats);
        return stats;
    }

    private static void EnsureBuilt()
    {
        if (_built) return;

        BuildDataIndexFromScene();
        ComputeAllFromRoots();

        _built = true;
    }

    private static void BuildDataIndexFromScene()
    {
        _dataById.Clear();
        _statsById.Clear();

        var points = UnityEngine.Object.FindObjectsOfType<MapPoint>(true);
        foreach (var p in points)
        {
            if (p == null) continue;
            if (string.IsNullOrWhiteSpace(p.pointId)) continue;

            // Only populated settlements are computed here.
            // If you later add unpopulated type, keep it in separate cache.
            var d = p.GetSettlementInfoData();
            if (d == null) continue;

            if (string.IsNullOrWhiteSpace(d.settlementId))
                d.settlementId = p.pointId;

            _dataById[d.settlementId] = d;
        }

        // Also load any settlements referenced as vassals/capital even if no MapPoint exists in scene.
        // This keeps your computations stable if you forgot to place a MapPoint.
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in _dataById)
        {
            var d = kv.Value;
            if (d?.main?.vassals != null)
                foreach (var v in d.main.vassals)
                    if (!string.IsNullOrWhiteSpace(v)) referenced.Add(v);

            if (!string.IsNullOrWhiteSpace(d?.feudal?.capitalSettlementId))
                referenced.Add(d.feudal.capitalSettlementId);
        }

        foreach (var id in referenced)
        {
            if (_dataById.ContainsKey(id)) continue;

            var d = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
                DataPaths.Runtime_MapDataPath,
                DataPaths.Editor_MapDataPath,
                id
            );

            if (d == null) continue;
            if (string.IsNullOrWhiteSpace(d.settlementId))
                d.settlementId = id;

            _dataById[id] = d;
        }
    }

    private static void ComputeAllFromRoots()
    {
        // Find roots: settlements that are not listed as a vassal of anyone else (ignoring capital edges)
        var isVassal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in _dataById)
        {
            var d = kv.Value;
            if (d?.main?.vassals == null) continue;
            foreach (var v in d.main.vassals)
                if (!string.IsNullOrWhiteSpace(v))
                    isVassal.Add(v);
        }

        foreach (var kv in _dataById)
        {
            var id = kv.Key;
            if (isVassal.Contains(id)) continue;

            // root compute
            ComputeGrossRecursive(id, parentId: null, parentIncomeTax: 0f, parentTroopTax: 0f, isCapital: false, parentTerms: null);
        }

        // If anything was missed due to cycles or disconnected graphs, compute individually.
        foreach (var kv in _dataById)
        {
            if (_statsById.ContainsKey(kv.Key)) continue;
            ComputeGrossRecursive(kv.Key, parentId: null, parentIncomeTax: 0f, parentTroopTax: 0f, isCapital: false, parentTerms: null);
        }
    }

    private static SettlementComputedStats ComputeGrossRecursive(
        string id,
        string parentId,
        float parentIncomeTax,
        float parentTroopTax,
        bool isCapital,
        string parentTerms)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (_statsById.TryGetValue(id, out var cached))
        {
            // Ensure net (paid up) is set relative to parent when called from parent
            ApplyParentTaxIfNeeded(cached, parentIncomeTax, parentTroopTax);
            return cached;
        }

        if (_visiting.Contains(id))
        {
            Debug.LogWarning($"[SettlementStatsCache] Cycle detected at '{id}'. Treating as leaf to prevent infinite recursion.");
            var leaf = BuildLeaf(id);
            if (leaf != null)
            {
                ApplyParentTaxIfNeeded(leaf, parentIncomeTax, parentTroopTax);
                _statsById[id] = leaf;
            }
            return leaf;
        }

        _visiting.Add(id);

        var data = _dataById.TryGetValue(id, out var d) ? d : null;
        if (data == null)
        {
            _visiting.Remove(id);
            return null;
        }

        var stats = new SettlementComputedStats();
        stats.settlementId = id;

        stats.localPopulation = Mathf.Max(0, data.main != null ? data.main.population : 0);
        stats.localIncome = data.economy != null ? data.economy.totalIncomePerMonth : 0.0;
        stats.localTroops = Mathf.Max(0, data.army != null ? data.army.totalArmy : 0);

        // Start gross as local
        stats.totalPopulation = stats.localPopulation;
        stats.grossIncome = stats.localIncome;
        stats.grossTroops = stats.localTroops;

        // Local demographics
        AddLocalDemographics(stats, data);

        // Build child list: direct vassals + (optional) capital edge
        var childIds = new List<string>();
        if (data.main?.vassals != null)
        {
            foreach (var v in data.main.vassals)
                if (!string.IsNullOrWhiteSpace(v))
                    childIds.Add(v);
        }

        // Country/realm capital included at 0% tax (do not require it to be in main.vassals)
        string capitalId = data.feudal != null ? data.feudal.capitalSettlementId : null;
        if (!string.IsNullOrWhiteSpace(capitalId) && !childIds.Contains(capitalId, StringComparer.OrdinalIgnoreCase))
            childIds.Add(capitalId);

        // Direct vassal summaries
        if (childIds.Count > 0)
        {
            foreach (var childId in childIds)
            {
                bool childIsCapital = !string.IsNullOrWhiteSpace(capitalId) &&
                                      string.Equals(childId, capitalId, StringComparison.OrdinalIgnoreCase);

                // Determine contract terms (stored on THIS liege)
                float incomeTax = 0f;
                float troopTax = 0f;
                string terms = null;

                if (childIsCapital)
                {
                    incomeTax = 0f;
                    troopTax = 0f;
                    terms = "Capital holding (no tax)";
                }
                else
                {
                    var c = FindContract(data, childId);
                    if (c != null)
                    {
                        incomeTax = Mathf.Clamp01(c.incomeTaxRate);
                        troopTax = Mathf.Clamp01(c.troopTaxRate);
                        terms = c.terms;
                    }
                    else
                    {
                        incomeTax = 0f;
                        troopTax = 0f;
                        terms = "No contract found (0% assumed)";
                    }
                }

                var childStats = ComputeGrossRecursive(childId, id, incomeTax, troopTax, childIsCapital, terms);
                if (childStats == null) continue;

                // Parent receives ONLY the taxed portion (income/troops).
                // Population is additive (full child population counts toward realm total).
                double incomePaidUp = childStats.incomePaidUp;
                int troopsPaidUp = childStats.troopsPaidUp;

                stats.grossIncome += incomePaidUp;
                stats.grossTroops += troopsPaidUp;
                stats.totalPopulation += childStats.totalPopulation;

                MergeCounts(stats.populationByRace, childStats.populationByRace);
                MergeCounts(stats.populationByCulture, childStats.populationByCulture);

                stats.directVassals.Add(new VassalComputedSummary
                {
                    vassalSettlementId = childId,
                    vassalDisplayName = SettlementNameResolver.Resolve(childId),
                    incomeTaxRate = incomeTax,
                    troopTaxRate = troopTax,
                    terms = terms,
                    vassalGrossIncome = childStats.grossIncome,
                    incomePaidUp = incomePaidUp,
                    vassalNetIncome = childStats.netIncome,
                    vassalGrossTroops = childStats.grossTroops,
                    troopsPaidUp = troopsPaidUp,
                    vassalNetTroops = childStats.netTroops,
                    vassalTotalPopulation = childStats.totalPopulation,
                    isCapital = childIsCapital
                });
            }
        }

        // Apply parent tax to determine this node's net + what it pays up.
        ApplyParentTaxIfNeeded(stats, parentIncomeTax, parentTroopTax);

        _statsById[id] = stats;
        _visiting.Remove(id);
        return stats;
    }

    private static void ApplyParentTaxIfNeeded(SettlementComputedStats stats, float parentIncomeTax, float parentTroopTax)
    {
        parentIncomeTax = Mathf.Clamp01(parentIncomeTax);
        parentTroopTax = Mathf.Clamp01(parentTroopTax);

        // What this node pays upward is based on its GROSS totals.
        stats.incomePaidUp = stats.grossIncome * parentIncomeTax;
        stats.troopsPaidUp = Mathf.RoundToInt(stats.grossTroops * parentTroopTax);

        // What remains is NET for display within this node
        stats.netIncome = stats.grossIncome - stats.incomePaidUp;
        stats.netTroops = Mathf.Max(0, stats.grossTroops - stats.troopsPaidUp);
    }

    private static SettlementComputedStats BuildLeaf(string id)
    {
        var data = _dataById.TryGetValue(id, out var d) ? d : null;
        if (data == null) return null;

        var stats = new SettlementComputedStats();
        stats.settlementId = id;
        stats.localPopulation = Mathf.Max(0, data.main != null ? data.main.population : 0);
        stats.localIncome = data.economy != null ? data.economy.totalIncomePerMonth : 0.0;
        stats.localTroops = Mathf.Max(0, data.army != null ? data.army.totalArmy : 0);

        stats.totalPopulation = stats.localPopulation;
        stats.grossIncome = stats.localIncome;
        stats.grossTroops = stats.localTroops;

        AddLocalDemographics(stats, data);

        return stats;
    }

    private static VassalContractData FindContract(SettlementInfoData liegeData, string vassalSettlementId)
    {
        if (liegeData?.feudal?.vassalContracts == null) return null;

        foreach (var c in liegeData.feudal.vassalContracts)
        {
            if (c == null) continue;
            if (string.Equals(c.vassalSettlementId, vassalSettlementId, StringComparison.OrdinalIgnoreCase))
                return c;
        }

        return null;
    }

    private static void AddLocalDemographics(SettlementComputedStats stats, SettlementInfoData data)
    {
        // Race distribution
        if (data?.cultural?.raceDistribution != null && data.cultural.raceDistribution.Count > 0)
        {
            AddPercentDistribution(stats.populationByRace, stats.localPopulation, data.cultural.raceDistribution);
        }
        else
        {
            // Fallback: if no structure, count all as "Unspecified"
            AddCount(stats.populationByRace, "Unspecified", stats.localPopulation);
        }

        // Culture distribution
        if (data?.cultural?.cultureDistribution != null && data.cultural.cultureDistribution.Count > 0)
        {
            AddPercentDistribution(stats.populationByCulture, stats.localPopulation, data.cultural.cultureDistribution);
        }
        else if (!string.IsNullOrWhiteSpace(data?.cultural?.culture))
        {
            AddCount(stats.populationByCulture, data.cultural.culture.Trim(), stats.localPopulation);
        }
        else
        {
            AddCount(stats.populationByCulture, "Unspecified", stats.localPopulation);
        }
    }

    private static void AddPercentDistribution(Dictionary<string, int> dict, int totalPop, List<PercentEntry> dist)
    {
        if (dict == null) return;
        if (dist == null || dist.Count == 0)
        {
            AddCount(dict, "Unspecified", totalPop);
            return;
        }

        float sum = 0f;
        for (int i = 0; i < dist.Count; i++)
            sum += Mathf.Max(0f, dist[i] != null ? dist[i].percent : 0f);

        if (sum <= 0.0001f)
        {
            AddCount(dict, "Unspecified", totalPop);
            return;
        }

        int assigned = 0;
        for (int i = 0; i < dist.Count; i++)
        {
            var e = dist[i];
            if (e == null) continue;

            string key = string.IsNullOrWhiteSpace(e.key) ? "Unspecified" : e.key.Trim();
            float pct = Mathf.Max(0f, e.percent) / sum;
            int count = (i == dist.Count - 1)
                ? Mathf.Max(0, totalPop - assigned)
                : Mathf.RoundToInt(totalPop * pct);

            assigned += count;
            AddCount(dict, key, count);
        }
    }

    private static void AddCount(Dictionary<string, int> dict, string key, int delta)
    {
        if (dict == null) return;
        if (delta == 0) return;

        key = string.IsNullOrWhiteSpace(key) ? "Unspecified" : key.Trim();

        if (dict.TryGetValue(key, out var cur))
            dict[key] = cur + delta;
        else
            dict[key] = delta;
    }

    private static void MergeCounts(Dictionary<string, int> into, Dictionary<string, int> from)
    {
        if (into == null || from == null) return;
        foreach (var kv in from)
            AddCount(into, kv.Key, kv.Value);
    }
}
