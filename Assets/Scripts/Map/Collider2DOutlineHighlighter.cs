using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Collider2DOutlineHighlighter : MonoBehaviour
{
    [SerializeField] private bool enabledByDefault = false;

    [Header("Layer Control (GameObject Layer ONLY)")]
    [SerializeField] private string uiLayerName = "UI";

    [Tooltip("Unity layer the outline GameObject will ALWAYS use (must be rendered by your world camera, and excluded from your UI camera).")]
    [SerializeField] private string outlineLayerName = "Ignore Raycast";

    [Header("Depth / Z-Fight Avoidance")]
    [Tooltip("Offsets the rendered line in Z to reduce depth fighting with map sprites/meshes. Negative is toward a default 2D camera at z=-10.")]
    [SerializeField] private float zOffsetTowardCamera = -0.01f;

    private LineRenderer _lr;
    private Collider2D _current;
    private readonly List<Vector3> _points = new List<Vector3>(512);

    private static Material _mat;

    private void Awake()
    {
        EnsureLineRenderer();
        ApplyOutlineLayer();
        if (!enabledByDefault) Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void EnsureLineRenderer()
    {
        if (_lr != null) return;

        _lr = gameObject.GetComponent<LineRenderer>();
        if (_lr == null) _lr = gameObject.AddComponent<LineRenderer>();

        _lr.useWorldSpace = true;
        _lr.loop = true;
        _lr.textureMode = LineTextureMode.Stretch;
        _lr.alignment = LineAlignment.View;

        _lr.numCornerVertices = 4;
        _lr.numCapVertices = 4;

        _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows = false;
        _lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        if (_mat == null)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            _mat = new Material(shader);
            _mat.color = Color.white;
        }

        _lr.material = _mat;
        _lr.enabled = false;
    }

    private void ApplyOutlineLayer()
    {
        int ui = LayerMask.NameToLayer(uiLayerName);
        int outline = LayerMask.NameToLayer(outlineLayerName);

        if (outline >= 0)
            gameObject.layer = outline;

        // Hard safety: never allow UI layer.
        if (ui >= 0 && gameObject.layer == ui && outline >= 0)
            gameObject.layer = outline;
    }

    public void Hide()
    {
        EnsureLineRenderer();
        _current = null;
        _lr.enabled = false;
        _lr.positionCount = 0;
        ApplyOutlineLayer();
    }

    public void Unsubscribe() => Hide();

    public void Show(
        Collider2D col,
        Color color,
        float widthWorldUnits,
        int circleSegments,
        int sortingLayerId,
        int sortingOrder)
    {
        EnsureLineRenderer();

        if (col == null)
        {
            Hide();
            return;
        }

        _current = col;

        // Always keep outline on the outline layer (world-only).
        ApplyOutlineLayer();

        _lr.startColor = color;
        _lr.endColor = color;

        widthWorldUnits = Mathf.Max(0.0001f, widthWorldUnits);
        _lr.startWidth = widthWorldUnits;
        _lr.endWidth = widthWorldUnits;

        // These do not solve UI overlap by themselves, but kept since your caller provides them.
        _lr.sortingLayerID = sortingLayerId;
        _lr.sortingOrder = sortingOrder;

        BuildWorldPoints(col, circleSegments, _points);

        if (_points.Count < 2)
        {
            Hide();
            return;
        }

        for (int i = 0; i < _points.Count; i++)
        {
            var p = _points[i];
            p.z += zOffsetTowardCamera;
            _points[i] = p;
        }

        _lr.loop = !(col is EdgeCollider2D);
        _lr.positionCount = _points.Count;
        _lr.SetPositions(_points.ToArray());
        _lr.enabled = true;
    }

    private static void BuildWorldPoints(Collider2D col, int circleSegments, List<Vector3> outPts)
    {
        outPts.Clear();

        if (col is PolygonCollider2D poly)
        {
            int bestPath = -1;
            float bestAbsArea = float.NegativeInfinity;

            for (int p = 0; p < poly.pathCount; p++)
            {
                var pts = poly.GetPath(p);
                if (pts == null || pts.Length < 2) continue;

                float abs = Mathf.Abs(SignedArea(pts));
                if (abs > bestAbsArea)
                {
                    bestAbsArea = abs;
                    bestPath = p;
                }
            }

            if (bestPath >= 0)
            {
                var pts = poly.GetPath(bestPath);
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 local = pts[i] + poly.offset;
                    outPts.Add(poly.transform.TransformPoint(new Vector3(local.x, local.y, 0f)));
                }
            }
            return;
        }

        if (col is CompositeCollider2D comp)
        {
            if (comp.pathCount <= 0) return;

            int bestPath = -1;
            float bestAbsArea = float.NegativeInfinity;

            for (int p = 0; p < comp.pathCount; p++)
            {
                int n = comp.GetPathPointCount(p);
                if (n < 2) continue;

                var pts = new Vector2[n];
                comp.GetPath(p, pts);

                float abs = Mathf.Abs(SignedArea(pts));
                if (abs > bestAbsArea)
                {
                    bestAbsArea = abs;
                    bestPath = p;
                }
            }

            if (bestPath >= 0)
            {
                int n = comp.GetPathPointCount(bestPath);
                var pts = new Vector2[n];
                comp.GetPath(bestPath, pts);

                for (int i = 0; i < pts.Length; i++)
                    outPts.Add(comp.transform.TransformPoint(new Vector3(pts[i].x, pts[i].y, 0f)));
            }
            return;
        }

        if (col is BoxCollider2D box)
        {
            Vector2 c = box.offset;
            Vector2 s = box.size * 0.5f;

            Vector2[] local =
            {
                new Vector2(c.x - s.x, c.y - s.y),
                new Vector2(c.x - s.x, c.y + s.y),
                new Vector2(c.x + s.x, c.y + s.y),
                new Vector2(c.x + s.x, c.y - s.y),
            };

            for (int i = 0; i < local.Length; i++)
                outPts.Add(box.transform.TransformPoint(new Vector3(local[i].x, local[i].y, 0f)));

            return;
        }

        if (col is CircleCollider2D circle)
        {
            circleSegments = Mathf.Clamp(circleSegments, 12, 256);
            Vector2 centerLocal = circle.offset;
            float r = Mathf.Max(0.0001f, circle.radius);

            for (int i = 0; i < circleSegments; i++)
            {
                float a = (i / (float)circleSegments) * Mathf.PI * 2f;
                float x = centerLocal.x + Mathf.Cos(a) * r;
                float y = centerLocal.y + Mathf.Sin(a) * r;
                outPts.Add(circle.transform.TransformPoint(new Vector3(x, y, 0f)));
            }
            return;
        }

        if (col is EdgeCollider2D edge)
        {
            var pts = edge.points;
            if (pts == null || pts.Length < 2) return;

            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 local = pts[i] + edge.offset;
                outPts.Add(edge.transform.TransformPoint(new Vector3(local.x, local.y, 0f)));
            }
            return;
        }

        Bounds b = col.bounds;
        outPts.Add(new Vector3(b.min.x, b.min.y, 0f));
        outPts.Add(new Vector3(b.min.x, b.max.y, 0f));
        outPts.Add(new Vector3(b.max.x, b.max.y, 0f));
        outPts.Add(new Vector3(b.max.x, b.min.y, 0f));
    }

    private static float SignedArea(IReadOnlyList<Vector2> pts)
    {
        if (pts == null || pts.Count < 3) return 0f;

        float sum = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            int j = (i + 1) % pts.Count;
            sum += pts[i].x * pts[j].y - pts[j].x * pts[i].y;
        }
        return sum * 0.5f;
    }
}
