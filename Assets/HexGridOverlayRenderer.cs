using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class HexGridOverlayRenderer : MonoBehaviour
{
    public enum CoverageMode
    {
        BackgroundBounds = 0,
        CameraView = 1
    }

    [Header("References")]
    [SerializeField] private Camera worldCamera;

    [Header("Coverage")]
    [SerializeField] private CoverageMode coverageMode = CoverageMode.BackgroundBounds;

    [Tooltip("Preferred: assign your map background SpriteRenderer/MeshRenderer here.")]
    [SerializeField] private Renderer backgroundRenderer;

    [Tooltip("If you don't have a renderer, you can use a collider for background bounds.")]
    [SerializeField] private Collider2D backgroundCollider2D;

    [Tooltip("Optional fallback if background is UI in a World Space canvas.")]
    [SerializeField] private RectTransform backgroundRectWorldSpace;

    [Tooltip("If the computed grid would exceed this, it will automatically fall back to CameraView for safety.")]
    [SerializeField] private int maxHexCountSafety = 60000;

    [Tooltip("Extra rings beyond bounds so you can see partial hexes at the edges.")]
    [SerializeField] private int paddingRings = 2;

    [Header("Grid")]
    [Tooltip("Distance from hex center to any corner in world units. Must match MapManager hexSizeUnits.")]
    [SerializeField] private float hexSizeUnits = 30f;

    [Tooltip("World Z plane where your map lies. Grid will render at mapPlaneZ + zOffset.")]
    [SerializeField] private float mapPlaneZ = 0f;

    [Tooltip("World origin of hex axial (0,0) center.")]
    [SerializeField] private Vector2 gridOriginWorld = Vector2.zero;

    [Tooltip("Z offset above the map plane.")]
    [SerializeField] private float zOffset = 0.001f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "MapGrid";
    [SerializeField] private int outlineSortingOrder = -900;
    [SerializeField] private int fillSortingOrder = -899;

    [Header("Colors")]
    [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.10f);
    [SerializeField] private Color hoveredFillColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color pathFillColor = new Color(0.2f, 0.9f, 1f, 0.16f);
    [SerializeField] private Color startFillColor = new Color(1f, 0.92f, 0.16f, 0.18f);

    [Header("Camera Coverage (only used in CameraView mode)")]
    [SerializeField] private int cameraPaddingRings = 2;

    private MeshFilter _outlineMF;
    private MeshRenderer _outlineMR;
    private Mesh _outlineMesh;

    private MeshFilter _fillMF;
    private MeshRenderer _fillMR;
    private Mesh _fillMesh;
    private string OutlineName => $"__HexGridOutline_{GetInstanceID()}";
    private string FillName => $"__HexGridFill_{GetInstanceID()}";

    private bool _visible;

    private bool _hasHover;
    private HexAxial _hoverHex;

    private bool _hasPath;
    private HexAxial _pathStart;
    private IReadOnlyList<HexAxial> _path;

    // Cached scaled corner offsets to avoid trig/alloc per hex
    private float _cachedCornerSize = float.NaN;
    private Vector3[] _cornerOffsets = new Vector3[6];

    // Reused lists to avoid GC
    private readonly List<Vector3> _outlineVerts = new List<Vector3>(16384);
    private readonly List<Color> _outlineCols = new List<Color>(16384);
    private readonly List<int> _outlineIdx = new List<int>(16384);

    private readonly List<Vector3> _fillVerts = new List<Vector3>(1024);
    private readonly List<Color> _fillCols = new List<Color>(1024);
    private readonly List<int> _fillIdx = new List<int>(2048);

    // Track last build inputs so we don’t rebuild constantly
    private bool _dirtyOutline = true;
    private float _lastHexSize = float.NaN;
    private float _lastPlaneZ = float.NaN;
    private int _lastPadding = int.MinValue;
    private CoverageMode _lastMode = (CoverageMode)(-1);

    // Cache last background bounds for rebuild triggering
    private Bounds _lastBgBounds;
    private bool _hasLastBgBounds;

    public void BindCamera(Camera cam) => worldCamera = cam;

    public void SetGridParams(float hexSize, float planeZ)
    {
        hexSizeUnits = Mathf.Max(0.0001f, hexSize);
        mapPlaneZ = planeZ;
        MarkOutlineDirty();
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
        EnsureRenderers();

        if (_outlineMR != null) _outlineMR.enabled = visible;
        if (_fillMR != null) _fillMR.enabled = visible;

        if (visible)
        {
            MarkOutlineDirty();
            RebuildIfDirty();
        }
        else
        {
            ClearHoveredHex();
            ClearPath();
        }
    }

    public void ForceRebuild()
    {
        MarkOutlineDirty(force: true);
        if (_visible) RebuildIfDirty();
        RebuildFill();
    }

    public void SetHoveredHex(HexAxial hex)
    {
        _hasHover = true;
        _hoverHex = hex;
        RebuildFill();
    }

    public void ClearHoveredHex()
    {
        if (!_hasHover) return;
        _hasHover = false;
        RebuildFill();
    }

    public void SetPath(HexAxial startHex, IReadOnlyList<HexAxial> path)
    {
        _hasPath = true;
        _pathStart = startHex;
        _path = path;
        RebuildFill();
    }

    public void ClearPath()
    {
        if (!_hasPath) return;
        _hasPath = false;
        _pathStart = default;
        _path = null;
        RebuildFill();
    }

    public void RefreshHighlights() => RebuildFill();

    private void Awake()
    {
        EnsureRenderers();
        SetVisible(_visible);
    }

    private void OnEnable()
    {
        EnsureRenderers();
        if (_visible)
        {
            MarkOutlineDirty();
            RebuildIfDirty();
        }
    }

    private void LateUpdate()
    {
        if (!_visible) return;

        // Keep overlay on correct Z without rebuilding geometry
        Vector3 pos = transform.position;
        pos.z = mapPlaneZ + zOffset;
        transform.position = pos;

        // In BackgroundBounds mode, do NOT rebuild every frame.
        // Only rebuild if background bounds changed (rare).
        if (coverageMode == CoverageMode.BackgroundBounds)
        {
            if (TryGetBackgroundBounds(out Bounds b))
            {
                if (!_hasLastBgBounds || BoundsChangedEnough(_lastBgBounds, b))
                {
                    _lastBgBounds = b;
                    _hasLastBgBounds = true;
                    MarkOutlineDirty();
                }
            }
        }

        RebuildIfDirty();
    }

    private static bool BoundsChangedEnough(Bounds a, Bounds b)
    {
        // Small threshold to avoid floating jitter rebuilds
        const float EPS = 0.01f;
        if ((a.center - b.center).sqrMagnitude > EPS * EPS) return true;
        if ((a.size - b.size).sqrMagnitude > EPS * EPS) return true;
        return false;
    }

    private void MarkOutlineDirty(bool force = false)
    {
        _dirtyOutline = true;
        if (force)
        {
            _lastHexSize = float.NaN;
            _lastPlaneZ = float.NaN;
            _lastPadding = int.MinValue;
            _lastMode = (CoverageMode)(-1);
            _hasLastBgBounds = false;
        }
    }

    private void RebuildIfDirty()
    {
        if (!_visible) return;

        // Detect parameter changes
        if (!Mathf.Approximately(_lastHexSize, hexSizeUnits)) _dirtyOutline = true;
        if (!Mathf.Approximately(_lastPlaneZ, mapPlaneZ)) _dirtyOutline = true;
        if (_lastPadding != paddingRings) _dirtyOutline = true;
        if (_lastMode != coverageMode) _dirtyOutline = true;

        if (_dirtyOutline)
        {
            RebuildOutline();
            _dirtyOutline = false;
            _lastHexSize = hexSizeUnits;
            _lastPlaneZ = mapPlaneZ;
            _lastPadding = paddingRings;
            _lastMode = coverageMode;
        }
    }

    private void EnsureScaledCornerOffsets()
    {
        if (Mathf.Approximately(_cachedCornerSize, hexSizeUnits))
            return;

        // Pointy-top corners at 30, 90, 150, 210, 270, 330
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i - 30f;
            float a = angleDeg * Mathf.Deg2Rad;
            float x = Mathf.Cos(a) * hexSizeUnits;
            float y = Mathf.Sin(a) * hexSizeUnits;
            _cornerOffsets[i] = new Vector3(x, y, 0f);
        }

        _cachedCornerSize = hexSizeUnits;
    }

    private void EnsureRenderers()
    {
        // Always rebuild references; never assume cached refs are valid.
        Transform outlineT = FindOrCreateChild(OutlineName);
        Transform fillT = FindOrCreateChild(FillName);

        // Force required components (repair if missing)
        MeshFilter outlineMF = GetOrAdd<MeshFilter>(outlineT.gameObject);
        MeshRenderer outlineMR = GetOrAdd<MeshRenderer>(outlineT.gameObject);

        MeshFilter fillMF = GetOrAdd<MeshFilter>(fillT.gameObject);
        MeshRenderer fillMR = GetOrAdd<MeshRenderer>(fillT.gameObject);

        if (_outlineMesh == null)
        {
            _outlineMesh = new Mesh { name = "HexGridOutlineMesh" };
            _outlineMesh.MarkDynamic();
            _outlineMesh.indexFormat = IndexFormat.UInt32;
        }

        if (_fillMesh == null)
        {
            _fillMesh = new Mesh { name = "HexGridFillMesh" };
            _fillMesh.MarkDynamic();
            _fillMesh.indexFormat = IndexFormat.UInt32;
        }

        // Assign meshes with a hard retry if Unity throws due to a "zombie" component
        if (!TryAssignSharedMesh(outlineMF, _outlineMesh))
        {
            outlineT = RecreateChild(outlineT, OutlineName);
            outlineMF = GetOrAdd<MeshFilter>(outlineT.gameObject);
            outlineMR = GetOrAdd<MeshRenderer>(outlineT.gameObject);
            outlineMF.sharedMesh = _outlineMesh;
        }

        if (!TryAssignSharedMesh(fillMF, _fillMesh))
        {
            fillT = RecreateChild(fillT, FillName);
            fillMF = GetOrAdd<MeshFilter>(fillT.gameObject);
            fillMR = GetOrAdd<MeshRenderer>(fillT.gameObject);
            fillMF.sharedMesh = _fillMesh;
        }

        if (outlineMR.sharedMaterial == null) outlineMR.sharedMaterial = CreateUnlitVertexColorMaterial();
        if (fillMR.sharedMaterial == null) fillMR.sharedMaterial = CreateUnlitVertexColorMaterial();

        outlineMR.sortingLayerName = sortingLayerName;
        outlineMR.sortingOrder = outlineSortingOrder;

        fillMR.sortingLayerName = sortingLayerName;
        fillMR.sortingOrder = fillSortingOrder;

        outlineMR.enabled = _visible;
        fillMR.enabled = _visible;

        int ignore = LayerMask.NameToLayer("Ignore Raycast");
        if (ignore >= 0)
        {
            outlineT.gameObject.layer = ignore;
            fillT.gameObject.layer = ignore;
        }

        // Update cached fields after we know they are valid
        _outlineMF = outlineMF;
        _outlineMR = outlineMR;
        _fillMF = fillMF;
        _fillMR = fillMR;
    }


    private Transform FindOrCreateChild(string name)
    {
        Transform t = transform.Find(name);
        if (t != null) return t;

        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private Transform RecreateChild(Transform old, string name)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(old.gameObject);
        else Destroy(old.gameObject);
#else
    Destroy(old.gameObject);
#endif

        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static bool TryAssignSharedMesh(MeshFilter mf, Mesh m)
    {
        try
        {
            mf.sharedMesh = m;
            return true;
        }
        catch (MissingComponentException)
        {
            return false;
        }
    }


    private static Material CreateUnlitVertexColorMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        var m = new Material(shader);
        m.color = Color.white;
        return m;
    }

    private bool TryGetBackgroundBounds(out Bounds b)
    {
        if (backgroundCollider2D != null)
        {
            b = backgroundCollider2D.bounds;
            return true;
        }

        if (backgroundRenderer != null)
        {
            b = backgroundRenderer.bounds;
            return true;
        }

        if (backgroundRectWorldSpace != null)
        {
            var corners = new Vector3[4];
            backgroundRectWorldSpace.GetWorldCorners(corners);
            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

            Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, mapPlaneZ);
            Vector3 size = new Vector3(Mathf.Max(0.0001f, maxX - minX), Mathf.Max(0.0001f, maxY - minY), 0.0001f);
            b = new Bounds(center, size);
            return true;
        }

        b = default;
        return false;
    }

    private void GetCameraWorldBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        Vector3 bl = ViewportToWorldOnPlane(0f, 0f);
        Vector3 tr = ViewportToWorldOnPlane(1f, 1f);

        minX = Mathf.Min(bl.x, tr.x);
        maxX = Mathf.Max(bl.x, tr.x);
        minY = Mathf.Min(bl.y, tr.y);
        maxY = Mathf.Max(bl.y, tr.y);
    }

    private Vector3 ViewportToWorldOnPlane(float vx, float vy)
    {
        if (worldCamera == null) return Vector3.zero;

        Ray ray = worldCamera.ViewportPointToRay(new Vector3(vx, vy, 0f));
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, mapPlaneZ));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }

    private void RebuildOutline()
    {
        EnsureRenderers();
        EnsureScaledCornerOffsets();

        float minX, maxX, minY, maxY;

        if (coverageMode == CoverageMode.BackgroundBounds)
        {
            if (!TryGetBackgroundBounds(out Bounds b))
                return;

            _lastBgBounds = b;
            _hasLastBgBounds = true;

            minX = b.min.x;
            maxX = b.max.x;
            minY = b.min.y;
            maxY = b.max.y;
        }
        else
        {
            if (worldCamera == null) return;
            GetCameraWorldBounds(out minX, out maxX, out minY, out maxY);
        }

        // Expand a bit to ensure edge coverage / partial hexes
        float expand = hexSizeUnits * 2f;
        minX -= expand; maxX += expand;
        minY -= expand; maxY += expand;

        // Convert rectangle corners into axial coords and range
        HexAxial a0 = HexGrid.LocalXYToAxial(new Vector2(minX, minY) - gridOriginWorld, hexSizeUnits);
        HexAxial a1 = HexGrid.LocalXYToAxial(new Vector2(minX, maxY) - gridOriginWorld, hexSizeUnits);
        HexAxial a2 = HexGrid.LocalXYToAxial(new Vector2(maxX, minY) - gridOriginWorld, hexSizeUnits);
        HexAxial a3 = HexGrid.LocalXYToAxial(new Vector2(maxX, maxY) - gridOriginWorld, hexSizeUnits);

        int pad = Mathf.Max(0, paddingRings) + (coverageMode == CoverageMode.CameraView ? cameraPaddingRings : 0);

        int qMin = Mathf.Min(a0.q, a1.q, a2.q, a3.q) - pad;
        int qMax = Mathf.Max(a0.q, a1.q, a2.q, a3.q) + pad;
        int rMin = Mathf.Min(a0.r, a1.r, a2.r, a3.r) - pad;
        int rMax = Mathf.Max(a0.r, a1.r, a2.r, a3.r) + pad;

        long est = (long)(qMax - qMin + 1) * (long)(rMax - rMin + 1);

        // Safety: if someone points this at a gigantic 16k-world-unit background with small hexes, this will explode.
        if (coverageMode == CoverageMode.BackgroundBounds && est > maxHexCountSafety)
        {
            // Fall back to camera view to avoid killing the game
            coverageMode = CoverageMode.CameraView;
            MarkOutlineDirty(force: true);
            return;
        }

        // Build line mesh with reduced duplication:
        // Draw interior edges only once: always draw dir(0,1,2) edges.
        // Draw boundary edges for dir(3,4,5) only when neighbor is outside the q/r range.
        //
        // Edge-to-dir mapping for pointy-top corners (indices based on our 30..330 corner order):
        // dir0 (1,0)  => edge 5 (corner5->corner0)
        // dir1 (1,-1) => edge 0 (corner0->corner1)
        // dir2 (0,-1) => edge 1 (corner1->corner2)
        // dir3 (-1,0) => edge 2 (corner2->corner3)
        // dir4 (-1,1) => edge 3 (corner3->corner4)
        // dir5 (0,1)  => edge 4 (corner4->corner5)

        _outlineVerts.Clear();
        _outlineCols.Clear();
        _outlineIdx.Clear();

        // Rough capacity to reduce reallocs
        int approxHex = (int)Mathf.Min(int.MaxValue, (float)est);
        int approxEdges = approxHex * 3 + (qMax - qMin + rMax - rMin) * 4;
        int approxVerts = approxEdges * 2;

        if (_outlineVerts.Capacity < approxVerts) _outlineVerts.Capacity = approxVerts;
        if (_outlineCols.Capacity < approxVerts) _outlineCols.Capacity = approxVerts;
        if (_outlineIdx.Capacity < approxVerts) _outlineIdx.Capacity = approxVerts;

        int idx = 0;
        float z = mapPlaneZ + zOffset;

        for (int r = rMin; r <= rMax; r++)
        {
            for (int q = qMin; q <= qMax; q++)
            {
                Vector2 centerLocal = HexGrid.AxialToLocalXY(new HexAxial(q, r), hexSizeUnits);
                Vector2 centerWorld2 = gridOriginWorld + centerLocal;

                // Fast AABB reject against expanded coverage rectangle
                if (centerWorld2.x < minX || centerWorld2.x > maxX || centerWorld2.y < minY || centerWorld2.y > maxY)
                    continue;

                Vector3 c = new Vector3(centerWorld2.x, centerWorld2.y, z);

                Vector3 c0 = c + _cornerOffsets[0];
                Vector3 c1 = c + _cornerOffsets[1];
                Vector3 c2 = c + _cornerOffsets[2];
                Vector3 c3 = c + _cornerOffsets[3];
                Vector3 c4 = c + _cornerOffsets[4];
                Vector3 c5 = c + _cornerOffsets[5];

                void AddEdge(Vector3 p0, Vector3 p1)
                {
                    _outlineVerts.Add(p0);
                    _outlineVerts.Add(p1);
                    _outlineCols.Add(gridLineColor);
                    _outlineCols.Add(gridLineColor);
                    _outlineIdx.Add(idx++);
                    _outlineIdx.Add(idx++);
                }

                // Always draw dir0/dir1/dir2 edges
                AddEdge(c5, c0); // dir0
                AddEdge(c0, c1); // dir1
                AddEdge(c1, c2); // dir2

                // Draw the other 3 edges only if neighbor would be outside the range (boundary)
                // dir3 neighbor is (q-1, r)
                if (q - 1 < qMin) AddEdge(c2, c3); // dir3 edge

                // dir4 neighbor is (q-1, r+1)
                if (q - 1 < qMin || r + 1 > rMax) AddEdge(c3, c4); // dir4 edge

                // dir5 neighbor is (q, r+1)
                if (r + 1 > rMax) AddEdge(c4, c5); // dir5 edge
            }
        }

        _outlineMesh.Clear(false);
        _outlineMesh.SetVertices(_outlineVerts);
        _outlineMesh.SetColors(_outlineCols);
        _outlineMesh.SetIndices(_outlineIdx, MeshTopology.Lines, 0, true);
        _outlineMesh.RecalculateBounds();
    }

    private void RebuildFill()
    {
        EnsureRenderers();
        EnsureScaledCornerOffsets();

        _fillVerts.Clear();
        _fillCols.Clear();
        _fillIdx.Clear();

        int baseIndex = 0;
        float z = mapPlaneZ + zOffset;

        void AddFilledHex(HexAxial hx, Color c)
        {
            Vector2 centerLocal = HexGrid.AxialToLocalXY(hx, hexSizeUnits);
            Vector2 centerWorld2 = gridOriginWorld + centerLocal;

            Vector3 center = new Vector3(centerWorld2.x, centerWorld2.y, z);
            Vector3 c0 = center + _cornerOffsets[0];
            Vector3 c1 = center + _cornerOffsets[1];
            Vector3 c2 = center + _cornerOffsets[2];
            Vector3 c3 = center + _cornerOffsets[3];
            Vector3 c4 = center + _cornerOffsets[4];
            Vector3 c5 = center + _cornerOffsets[5];

            _fillVerts.Add(center); _fillCols.Add(c);
            _fillVerts.Add(c0); _fillCols.Add(c);
            _fillVerts.Add(c1); _fillCols.Add(c);
            _fillVerts.Add(c2); _fillCols.Add(c);
            _fillVerts.Add(c3); _fillCols.Add(c);
            _fillVerts.Add(c4); _fillCols.Add(c);
            _fillVerts.Add(c5); _fillCols.Add(c);

            for (int i = 0; i < 6; i++)
            {
                int i0 = baseIndex;
                int i1 = baseIndex + 1 + i;
                int i2 = baseIndex + 1 + ((i + 1) % 6);
                _fillIdx.Add(i0); _fillIdx.Add(i1); _fillIdx.Add(i2);
            }

            baseIndex += 7;
        }

        if (_hasPath)
        {
            AddFilledHex(_pathStart, startFillColor);
            if (_path != null)
            {
                for (int i = 0; i < _path.Count; i++)
                    AddFilledHex(_path[i], pathFillColor);
            }
        }

        if (_hasHover)
            AddFilledHex(_hoverHex, hoveredFillColor);

        _fillMesh.Clear(false);
        _fillMesh.SetVertices(_fillVerts);
        _fillMesh.SetColors(_fillCols);
        _fillMesh.SetIndices(_fillIdx, MeshTopology.Triangles, 0, true);
        _fillMesh.RecalculateBounds();
    }
}
