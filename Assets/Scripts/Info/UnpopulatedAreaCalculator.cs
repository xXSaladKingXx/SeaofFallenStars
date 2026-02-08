using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputedAreaStats
{
    public float totalAreaSqMi;
    public readonly Dictionary<string, float> areaByTerrain = new Dictionary<string, float>();
}

public static class UnpopulatedAreaCalculator
{
    public static ComputedAreaStats Compute(MapPoint point)
    {
        if (point == null || point.isPopulated)
            return null;

        var stats = new ComputedAreaStats();
        ComputeInternal(point, stats);
        return stats;
    }

    private static void ComputeInternal(MapPoint point, ComputedAreaStats acc)
    {
        if (point == null || acc == null)
            return;

        // Back-compat accessor exists in your MapPoint: GetUnpopulatedAreaData() -> GetUnpopulatedInfoData()
        var data = point.GetUnpopulatedAreaData();
        float area = data?.geography?.areaSqMi ?? 0f;
        area = Mathf.Max(0f, area);

        // Allocate terrain
        // Your current models have drifted between array vs List, so treat as IList at runtime.
        object breakdownObj = data?.geography?.terrainBreakdown;
        IList breakdown = breakdownObj as IList;

        if (breakdown != null && breakdown.Count > 0)
        {
            for (int i = 0; i < breakdown.Count; i++)
            {
                var entry = breakdown[i] as TerrainBreakdownEntry;
                if (entry == null) continue;

                // Newer model: entry.terrainType and percent is 0..100
                string terrain = string.IsNullOrWhiteSpace(entry.terrainType) ? "Unknown" : entry.terrainType.Trim();

                float pct01 = Mathf.Clamp01(entry.percent / 100f);
                float allocArea = area * pct01;

                AddTerrain(acc, terrain, allocArea);
            }
        }
        else
        {
            // Fallback to single terrain type
            string t = string.IsNullOrWhiteSpace(data?.geography?.terrainType) ? "Unknown" : data.geography.terrainType.Trim();
            AddTerrain(acc, t, area);
        }

        acc.totalAreaSqMi += area;

        // Recurse to child unpopulated points (sub-regions)
        // childMapPoints is private; use MapPoint.GetChildren()
        var children = point.GetChildren();
        if (children == null) return;

        for (int i = 0; i < children.Count; i++)
        {
            var c = children[i];
            if (c == null || c.isPopulated) continue;
            ComputeInternal(c, acc);
        }
    }

    private static void AddTerrain(ComputedAreaStats acc, string terrain, float area)
    {
        area = Mathf.Max(0f, area);
        if (string.IsNullOrWhiteSpace(terrain))
            terrain = "Unknown";

        if (!acc.areaByTerrain.ContainsKey(terrain))
            acc.areaByTerrain[terrain] = 0f;

        acc.areaByTerrain[terrain] += area;
    }
}
