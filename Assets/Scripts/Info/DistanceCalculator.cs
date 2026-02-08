using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DistanceCalculator : MonoBehaviour
{
    // Hard-coded as requested:
    public const double TOTAL_MAP_AREA_SQMI = 36451406.25;

    [Header("Calibration (Map Rectangle)")]
    [Tooltip("Transforms placed on the map borders (ideally the 4 corners). Min/Max of these defines the map rectangle.")]
    [SerializeField] private Transform[] borderMarkers;

    [Tooltip("If set, all measurements are done in this transform's local XY space (recommended). If null, world XY is used.")]
    [SerializeField] private Transform mapSpace;

    [Header("Projection")]
    [Tooltip("Camera used to project screen position onto the map plane.")]
    [SerializeField] private Camera mapCamera;

    [Header("Defaults")]
    [SerializeField] private bool clampToMapBoundsByDefault = true;

    public Camera MapCamera => mapCamera;
    public Transform MapSpace => mapSpace;

    private void Awake()
    {
        if (mapCamera == null) mapCamera = Camera.main;
    }

    // ----------------------------
    // Calibration / conversion
    // ----------------------------

    public bool TryGetMapRect(out Rect rect)
    {
        rect = default;

        if (borderMarkers == null || borderMarkers.Length < 2)
        {
            Debug.LogWarning($"{nameof(DistanceCalculator)}: Assign at least 2 borderMarkers (4 corners recommended).");
            return false;
        }

        bool any = false;
        float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;

        foreach (var t in borderMarkers)
        {
            if (t == null) continue;
            Vector2 p = WorldToMapPoint(t.position);

            if (!any)
            {
                minX = maxX = p.x;
                minY = maxY = p.y;
                any = true;
            }
            else
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
        }

        if (!any) return false;

        float w = maxX - minX;
        float h = maxY - minY;
        if (w <= Mathf.Epsilon || h <= Mathf.Epsilon)
        {
            Debug.LogWarning($"{nameof(DistanceCalculator)}: Border markers define a degenerate rectangle.");
            return false;
        }

        rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    public double GetSqMiPerMapUnitSquared()
    {
        if (!TryGetMapRect(out Rect mapRect))
            return 0.0;

        double unitsArea = (double)mapRect.width * (double)mapRect.height;
        if (unitsArea <= double.Epsilon) return 0.0;

        return TOTAL_MAP_AREA_SQMI / unitsArea;
    }

    public double GetMilesPerMapUnit()
    {
        double sqMiPerUnit2 = GetSqMiPerMapUnitSquared();
        if (sqMiPerUnit2 <= 0.0) return 0.0;
        return Math.Sqrt(sqMiPerUnit2);
    }

    public float UnitsToMiles(float units) => (float)(units * GetMilesPerMapUnit());
    public float MilesToUnits(float miles)
    {
        double mpu = GetMilesPerMapUnit();
        if (mpu <= 0.0) return 0f;
        return (float)(miles / mpu);
    }

    // ----------------------------
    // Coordinate conversions
    // ----------------------------

    public Vector2 WorldToMapPoint(Vector3 worldPos)
    {
        if (mapSpace == null)
            return new Vector2(worldPos.x, worldPos.y);

        Vector3 local = mapSpace.InverseTransformPoint(worldPos);
        return new Vector2(local.x, local.y);
    }

    public Vector3 MapPointToWorld(Vector2 mapPt)
    {
        if (mapSpace == null)
            return new Vector3(mapPt.x, mapPt.y, 0f);

        return mapSpace.TransformPoint(new Vector3(mapPt.x, mapPt.y, 0f));
    }

    public bool TryProjectScreenToMapPoint(Vector2 screenPos, out Vector2 mapPt, out Vector3 worldPt)
    {
        mapPt = default;
        worldPt = default;

        if (mapCamera == null) return false;

        Ray ray = mapCamera.ScreenPointToRay(screenPos);

        Plane plane = (mapSpace != null)
            ? new Plane(mapSpace.forward, mapSpace.position)
            : new Plane(Vector3.forward, Vector3.zero);

        if (!plane.Raycast(ray, out float enter))
            return false;

        worldPt = ray.GetPoint(enter);
        mapPt = WorldToMapPoint(worldPt);
        return true;
    }

    public bool TryGetMouseMapPoint(out Vector2 mapPt, out Vector3 worldPt)
    {
        mapPt = default;
        worldPt = default;

        if (Mouse.current == null) return false;
        Vector2 screen = Mouse.current.position.ReadValue();

        return TryProjectScreenToMapPoint(screen, out mapPt, out worldPt);
    }

    public Vector2 ClampToMapBounds(Vector2 mapPt)
    {
        if (!TryGetMapRect(out Rect r))
            return mapPt;

        mapPt.x = Mathf.Clamp(mapPt.x, r.xMin, r.xMax);
        mapPt.y = Mathf.Clamp(mapPt.y, r.yMin, r.yMax);
        return mapPt;
    }

    // ----------------------------
    // Distance (polyline)
    // ----------------------------

    public float CalculatePolylineLengthUnits(IReadOnlyList<Vector2> mapPoints, bool clampToBounds = false)
    {
        if (mapPoints == null || mapPoints.Count < 2) return 0f;

        float sum = 0f;
        Vector2 prev = clampToBounds ? ClampToMapBounds(mapPoints[0]) : mapPoints[0];

        for (int i = 1; i < mapPoints.Count; i++)
        {
            Vector2 curr = clampToBounds ? ClampToMapBounds(mapPoints[i]) : mapPoints[i];
            sum += Vector2.Distance(prev, curr);
            prev = curr;
        }

        return sum;
    }

    public float CalculatePolylineLengthMiles(IReadOnlyList<Vector2> mapPoints, bool clampToBounds = false)
        => UnitsToMiles(CalculatePolylineLengthUnits(mapPoints, clampToBounds));

    public float CalculateLineDistanceUnits(Vector2 a, Vector2 b, bool clampToBounds = false)
    {
        if (clampToBounds)
        {
            a = ClampToMapBounds(a);
            b = ClampToMapBounds(b);
        }
        return Vector2.Distance(a, b);
    }

    public float CalculateLineDistanceMiles(Vector2 a, Vector2 b, bool clampToBounds = false)
        => UnitsToMiles(CalculateLineDistanceUnits(a, b, clampToBounds));

    // ----------------------------
    // Area (polygon)
    // ----------------------------

    public float CalculatePolygonAreaUnits2(IReadOnlyList<Vector2> mapPolygonPoints, bool clampToBounds = false)
    {
        if (mapPolygonPoints == null || mapPolygonPoints.Count < 3) return 0f;

        List<Vector2> pts = new List<Vector2>(mapPolygonPoints.Count);
        for (int i = 0; i < mapPolygonPoints.Count; i++)
            pts.Add(clampToBounds ? ClampToMapBounds(mapPolygonPoints[i]) : mapPolygonPoints[i]);

        // If you want strict "within borders" for arbitrary polygons, you'd clip here (rect clip).
        // For now, clamp is usually sufficient for user-drawn shapes.

        return Mathf.Abs(SignedArea(pts));
    }

    public float CalculatePolygonAreaSqMi(IReadOnlyList<Vector2> mapPolygonPoints, bool clampToBounds = false)
        => (float)(CalculatePolygonAreaUnits2(mapPolygonPoints, clampToBounds) * GetSqMiPerMapUnitSquared());

    private static float SignedArea(IReadOnlyList<Vector2> pts)
    {
        int n = pts.Count;
        double sum = 0.0;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[(i + 1) % n];
            sum += (double)a.x * b.y - (double)b.x * a.y;
        }

        return (float)(0.5 * sum);
    }

    public bool ClampToMapBoundsByDefault => clampToMapBoundsByDefault;
}
