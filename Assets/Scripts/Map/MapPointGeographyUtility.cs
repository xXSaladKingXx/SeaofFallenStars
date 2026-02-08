using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapPointGeographyUtility
{
    public sealed class ChildBuckets
    {
        public readonly List<MapPoint> wilderness = new List<MapPoint>();
        public readonly List<MapPoint> naturalFormations = new List<MapPoint>();
        public readonly List<MapPoint> ruins = new List<MapPoint>();
    }

    public static ChildBuckets GetUnpopulatedChildrenBuckets(MapPoint parent)
    {
        var buckets = new ChildBuckets();
        if (parent == null) return buckets;

        var kids = parent.GetChildren();
        if (kids == null) return buckets;

        for (int i = 0; i < kids.Count; i++)
        {
            var c = kids[i];
            if (c == null || c.isPopulated) continue;

            switch (c.unpopulatedSubtype)
            {
                case MapPoint.UnpopulatedSubtype.Wilderness:
                    buckets.wilderness.Add(c);
                    break;
                case MapPoint.UnpopulatedSubtype.Water:
                    buckets.naturalFormations.Add(c);
                    break;
                case MapPoint.UnpopulatedSubtype.Ruins:
                    buckets.ruins.Add(c);
                    break;
            }
        }

        return buckets;
    }

    public static float ComputeMapPointAreaSqMi(MapPoint point, float unityUnitsToMiles)
    {
        if (point == null) return 0f;

        float milesPerUnit = Mathf.Max(0.0001f, unityUnitsToMiles);

        var col = point.GetComponent<Collider2D>();
        float areaWU2 = ComputeColliderAreaWorldUnits(col);

        if (areaWU2 > 0.00001f)
            return areaWU2 * milesPerUnit * milesPerUnit;

        // Fallback for unpopulated points with missing/unsupported colliders
        if (!point.isPopulated)
        {
            var d = point.GetUnpopulatedAreaData(); // existing back-compat accessor
            float a = d != null && d.geography != null ? d.geography.areaSqMi : 0f;
            return Mathf.Max(0f, a);
        }

        return 0f;
    }

    public static float ComputeColliderAreaSqMi(Collider2D col, float unityUnitsToMiles)
    {
        float milesPerUnit = Mathf.Max(0.0001f, unityUnitsToMiles);
        float areaWU2 = ComputeColliderAreaWorldUnits(col);
        return Mathf.Max(0f, areaWU2 * milesPerUnit * milesPerUnit);
    }

    public static Dictionary<string, float> ComputeWildernessAreaByTerrainSqMi(
        IEnumerable<MapPoint> wildernessChildren,
        float unityUnitsToMiles)
    {
        var dict = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        if (wildernessChildren == null) return dict;

        foreach (var w in wildernessChildren)
        {
            if (w == null) continue;

            string terrain = "Unknown";
            var d = w.GetUnpopulatedAreaData();
            if (d != null && d.geography != null && !string.IsNullOrWhiteSpace(d.geography.terrainType))
                terrain = d.geography.terrainType.Trim();

            float a = ComputeMapPointAreaSqMi(w, unityUnitsToMiles);
            if (a <= 0.00001f && d != null && d.geography != null)
                a = Mathf.Max(0f, d.geography.areaSqMi);

            if (!dict.ContainsKey(terrain))
                dict[terrain] = 0f;

            dict[terrain] += Mathf.Max(0f, a);
        }

        return dict;
    }

    public static float ComputeColliderAreaWorldUnits(Collider2D col)
    {
        if (col == null) return 0f;

        if (col is PolygonCollider2D poly) return ComputePolygonColliderAreaWorldUnits(poly);

        if (col is BoxCollider2D box)
        {
            Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
            return Mathf.Abs(size.x * size.y);
        }

        if (col is CircleCollider2D circ)
        {
            float r = circ.radius * Mathf.Max(circ.transform.lossyScale.x, circ.transform.lossyScale.y);
            return Mathf.PI * r * r;
        }

        if (col is CapsuleCollider2D cap)
        {
            Vector2 size = Vector2.Scale(cap.size, cap.transform.lossyScale);
            float radius = Mathf.Min(size.x, size.y) * 0.5f;
            float longLen = (cap.direction == CapsuleDirection2D.Horizontal) ? size.x : size.y;
            float rectLen = Mathf.Max(0f, longLen - 2f * radius);
            float rectWidth = 2f * radius;
            return rectLen * rectWidth + Mathf.PI * radius * radius;
        }

        // EdgeCollider2D and others -> not an area
        return 0f;
    }

    private static float ComputePolygonColliderAreaWorldUnits(PolygonCollider2D poly)
    {
        if (poly == null) return 0f;

        float total = 0f;

        int pathCount = Mathf.Max(1, poly.pathCount);
        for (int p = 0; p < pathCount; p++)
        {
            Vector2[] pts = poly.GetPath(p);
            if (pts == null || pts.Length < 3) continue;

            // Shoelace in WORLD space
            double sum = 0.0;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector3 a = poly.transform.TransformPoint(pts[i]);
                Vector3 b = poly.transform.TransformPoint(pts[(i + 1) % pts.Length]);
                sum += (a.x * b.y) - (b.x * a.y);
            }

            total += Mathf.Abs((float)(sum * 0.5));
        }

        return total;
    }

    /// <summary>
    /// Makes a dropdown selection behave as if the map point was clicked (uses MapManager's existing flow).
    /// </summary>
    public static void SimulateMapPointClick(MapPoint point)
    {
        if (point == null) return;

        var managers = UnityEngine.Object.FindObjectsOfType<MapManager>(true);
        if (managers == null || managers.Length == 0) return;

        // MapManager.OnPointClicked(MapPoint) is private in your setup; SendMessage will still hit it.
        managers[0].gameObject.SendMessage("OnPointClicked", point, SendMessageOptions.DontRequireReceiver);
    }
}
