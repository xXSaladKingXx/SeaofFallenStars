using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MapPointNameplateLayoutCoordinator : MonoBehaviour
{
    [Header("Overlap Avoidance")]
    [Tooltip("Minimum spacing between nameplate rectangles, in the canvas' local units (pixels for Screen Space).")]
    [SerializeField] private float minSpacing = 12f;

    [Tooltip("How many solver passes to run each LateUpdate. Higher = better separation, more CPU.")]
    [Range(1, 12)]
    [SerializeField] private int solverIterations = 5;

    [Tooltip("Maximum push applied per plate per frame (canvas local units).")]
    [SerializeField] private float maxPushPerFrame = 120f;

    [Tooltip("Prefer solving overlaps horizontally (side-to-side) before vertical separation.")]
    [SerializeField] private bool preferHorizontal = true;

    [Header("Filtering")]
    [Tooltip("If true, only consider nameplates that are active in hierarchy.")]
    [SerializeField] private bool onlyActivePlates = true;

    private RectTransform _refRect;

    // Working buffers (allocated once)
    private readonly List<RectTransform> _plates = new List<RectTransform>(512);
    private readonly List<Vector2> _baseLocal = new List<Vector2>(512);
    private readonly List<Vector2> _solvedLocal = new List<Vector2>(512);
    private readonly List<float> _worldZ = new List<float>(512);

    private void Awake()
    {
        ResolveReferenceRect();
    }

    private void OnEnable()
    {
        ResolveReferenceRect();
    }

    private void ResolveReferenceRect()
    {
        _refRect = transform as RectTransform;
        if (_refRect != null) return;

        var canvas = GetComponentInParent<Canvas>();
        _refRect = canvas != null ? canvas.transform as RectTransform : null;
    }

    private void LateUpdate()
    {
        if (_refRect == null)
            ResolveReferenceRect();
        if (_refRect == null)
            return;

        CollectPlates();

        int n = _plates.Count;
        if (n < 2) return;

        // Snapshot base positions AFTER MapManager has placed them this frame.
        _baseLocal.Clear();
        _solvedLocal.Clear();
        _worldZ.Clear();

        for (int i = 0; i < n; i++)
        {
            var rt = _plates[i];
            Vector3 w = rt.position;
            Vector3 l3 = _refRect.InverseTransformPoint(w);

            _baseLocal.Add(new Vector2(l3.x, l3.y));
            _solvedLocal.Add(new Vector2(l3.x, l3.y));
            _worldZ.Add(w.z);
        }

        // Iteratively push apart overlaps in reference-local space, then re-apply in world space.
        float maxPush = Mathf.Max(0f, maxPushPerFrame);
        float halfPad = Mathf.Max(0f, minSpacing) * 0.5f;

        for (int iter = 0; iter < solverIterations; iter++)
        {
            bool any = false;

            for (int a = 0; a < n; a++)
            {
                Rect ra = GetLocalRectAt(_plates[a], _solvedLocal[a]);
                ra = Expand(ra, halfPad);

                for (int b = a + 1; b < n; b++)
                {
                    Rect rb = GetLocalRectAt(_plates[b], _solvedLocal[b]);
                    rb = Expand(rb, halfPad);

                    if (!Overlaps(ra, rb))
                        continue;

                    any = true;

                    Vector2 ca = ra.center;
                    Vector2 cb = rb.center;

                    float ox = Mathf.Min(ra.xMax, rb.xMax) - Mathf.Max(ra.xMin, rb.xMin);
                    float oy = Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin);

                    if (ox <= 0f || oy <= 0f) continue;

                    // Choose axis: prefer horizontal when requested, otherwise smallest penetration.
                    bool pushX;
                    if (preferHorizontal)
                        pushX = true; // horizontal-first
                    else
                        pushX = ox < oy;

                    Vector2 deltaA = Vector2.zero;
                    Vector2 deltaB = Vector2.zero;

                    if (pushX)
                    {
                        float dir = (ca.x >= cb.x) ? 1f : -1f;
                        float push = ox + 0.01f;
                        float half = push * 0.5f;

                        deltaA.x += dir * half;
                        deltaB.x -= dir * half;
                    }
                    else
                    {
                        float dir = (ca.y >= cb.y) ? 1f : -1f;
                        float push = oy + 0.01f;
                        float half = push * 0.5f;

                        deltaA.y += dir * half;
                        deltaB.y -= dir * half;
                    }

                    // Clamp per interaction to avoid huge jumps.
                    deltaA = ClampMagnitude(deltaA, maxPush);
                    deltaB = ClampMagnitude(deltaB, maxPush);

                    _solvedLocal[a] = _solvedLocal[a] + deltaA;
                    _solvedLocal[b] = _solvedLocal[b] + deltaB;
                }
            }

            if (!any) break;
        }

        // Apply solved positions as world positions.
        for (int i = 0; i < n; i++)
        {
            var rt = _plates[i];
            Vector2 targetLocal = _solvedLocal[i];

            Vector3 world = _refRect.TransformPoint(new Vector3(targetLocal.x, targetLocal.y, 0f));
            world.z = _worldZ[i]; // preserve original Z (important for ScreenSpaceCamera/world-space canvases)

            rt.position = world;
        }
    }

    private void CollectPlates()
    {
        _plates.Clear();

        // Find all MapPointNameplateUI components under this coordinator.
        // We do NOT call any custom methods/properties on MapPointNameplateUI.
        var uis = GetComponentsInChildren<MapPointNameplateUI>(true);
        if (uis == null) return;

        for (int i = 0; i < uis.Length; i++)
        {
            var ui = uis[i];
            if (ui == null) continue;

            if (onlyActivePlates && !ui.gameObject.activeInHierarchy)
                continue;

            var rt = ui.transform as RectTransform;
            if (rt == null) continue;

            _plates.Add(rt);
        }
    }

    private Rect GetLocalRectAt(RectTransform rt, Vector2 localCenter)
    {
        if (rt == null) return new Rect(localCenter, Vector2.zero);

        Vector2 size = rt.rect.size;
        // If the rect is zero (common if layout hasn't run), fall back to sizeDelta.
        if (size.x <= 0.01f || size.y <= 0.01f)
            size = rt.sizeDelta;

        // IMPORTANT:
        // RectTransform.rect is unscaled; if we scale the plate (e.g., per MapLayer), overlap tests must
        // consider that scale, otherwise the solver will "think" plates are smaller than they appear.
        // We are operating in _refRect LOCAL space, so we convert the plate's lossy scale into
        // a relative scale against the root.
        Vector3 plateLossy = rt.lossyScale;
        Vector3 rootLossy = _refRect != null ? _refRect.lossyScale : Vector3.one;

        float sx = 1f;
        float sy = 1f;
        if (Mathf.Abs(rootLossy.x) > 0.00001f) sx = Mathf.Abs(plateLossy.x) / Mathf.Abs(rootLossy.x);
        if (Mathf.Abs(rootLossy.y) > 0.00001f) sy = Mathf.Abs(plateLossy.y) / Mathf.Abs(rootLossy.y);

        size = new Vector2(size.x * sx, size.y * sy);

        Vector2 half = size * 0.5f;
        return new Rect(localCenter.x - half.x, localCenter.y - half.y, size.x, size.y);
    }

    private static Rect Expand(Rect r, float pad)
    {
        if (pad <= 0f) return r;
        return new Rect(r.xMin - pad, r.yMin - pad, r.width + pad * 2f, r.height + pad * 2f);
    }

    private static bool Overlaps(Rect a, Rect b)
    {
        return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
    }

    private static Vector2 ClampMagnitude(Vector2 v, float max)
    {
        float m = v.magnitude;
        if (m <= max || m <= 0.00001f) return v;
        return v * (max / m);
    }
}
