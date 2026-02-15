// MapManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [Header("Camera Integration")]
    [SerializeField] private MapCameraController cameraController;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float mapPlaneZ = 0f;

    [Header("Scene Refs")]
    [SerializeField] private Transform pointsRoot;

    [Header("Manual Layer Selection (driven by hotkeys/UI)")]
    [SerializeField] private MapLayer selectedLayer = MapLayer.Regional;

    [Tooltip("When ON: shows Unpopulated points and hides populated (Settlement/POI). Regions may remain clickable depending on hideRegionsInGeographyMode.")]
    [SerializeField] private bool geographyMode = false;

    [Tooltip("If true, Regions are not interactable/visible during Geography Mode.")]
    [SerializeField] private bool hideRegionsInGeographyMode = false;

    [Header("Layer Fallback (avoid empty maps)")]
    [Tooltip("If selected layer has zero eligible points (anywhere), auto-fallback the visible layer to another layer that does.")]
    [SerializeField] private bool fallbackIfSelectedLayerEmpty = true;

    [Tooltip("Layer order from zoomed-out to zoomed-in.")]
    [SerializeField]
    private List<MapLayer> layerOrder = new List<MapLayer>()
    {
        MapLayer.Regional,
        MapLayer.Country,
        MapLayer.Duchy,
        MapLayer.Lordship,
        MapLayer.Point
    };

    [Header("Physics Picking")]
    [SerializeField] private LayerMask mapPointPhysicsMask = ~0;

    [Header("Outline Rendering Priority")]
    [SerializeField] private string outlineSortingLayerName = "MapPoint";   // create this Sorting Layer
    [SerializeField] private int outlineBaseSortingOrder = -1000;            // keep below world-space UI
    [SerializeField] private bool enableHoverOutline = true;
    [SerializeField] private bool enableSelectedOutline = true;

    [Header("Outline Highlighting")]
    [SerializeField]
    private List<Color> outlinePalette = new List<Color>()
    {
        new Color(1f, 0.92f, 0.16f, 1f),
        new Color(0.2f, 0.9f, 1f, 1f),
        new Color(1f, 0.35f, 0.75f, 1f),
        new Color(0.35f, 1f, 0.35f, 1f),
        new Color(1f, 0.5f, 0.2f, 1f),
    };
    [SerializeField] private bool colorByLayer = true;
    [SerializeField] private int hoverPaletteIndex = 0;
    [SerializeField] private int selectedPaletteIndex = 1;
    [SerializeField] private float outlineWidthWorldUnits = 0.06f;
    [SerializeField] private int circleSegments = 48;
    [SerializeField] private int outlineSortingOrderOffset = 10;

    [Header("Info Window Prefabs")]
    [SerializeField] private GameObject regionInfoWindowPrefab;
    [SerializeField] private GameObject unpopulatedInfoWindowPrefab;
    [SerializeField] private GameObject populatedInfoWindowPrefab;
    [SerializeField] private GameObject travelInfoWindowPrefab;   // Prefab for TravelGroup info window

    [Tooltip("Legacy/compat parent. If InfoLayer is assigned, it will be preferred.")]
    [SerializeField] private Transform uiParent;

    [Header("InfoLayer (preferred spawn + scale reference)")]
    [Tooltip("Assign your InfoLayer Canvas here. If null, MapManager will try to find a Canvas named 'InfoLayer' at runtime.")]
    [SerializeField] private Canvas infoLayerCanvas;
    [Tooltip("Optional: explicit parent under InfoLayer for spawned UI. If null, uses infoLayerCanvas.transform.")]
    [SerializeField] private Transform infoLayerParentOverride;

    [Header("Window Spawn Timing")]
    [SerializeField] private bool deferWindowSpawnUntilSnapComplete = true;

    [Header("Window Spawn Placement")]
    [Tooltip("If enabled, spawn Region/Settlement/Unpopulated windows near the map point instead of center.")]
    [SerializeField] private bool spawnWindowsAtPointFocus = true;
    [Tooltip("World offset north of the point for window spawn (if World Space).")]
    [SerializeField] private float windowNorthWorldPadding = 1.15f;
    [Tooltip("Screen offset for window spawn (if Screen Space).")]
    [SerializeField] private Vector2 windowScreenOffset = new Vector2(0f, 160f);

    [Header("Nameplates")]
    [SerializeField] private GameObject mapPointNameplatePrefab;
    [Tooltip("If InfoLayer is World Space, world offset north of the point for nameplates.")]
    [SerializeField] private float nameplateNorthWorldPadding = 0.25f;
    [Tooltip("If InfoLayer is Screen Space, screen offset north of the point (pixels).")]
    [SerializeField] private Vector2 nameplateScreenOffset = new Vector2(0f, 70f);

    

    [Header("Nameplate Base Scale by Layer")]
    [Tooltip("Base scale multiplier applied to nameplates based on the MapLayer they belong to (before any zoom-driven scaling).")]
    [SerializeField] private float nameplateScaleRegional = 1.25f;
    [SerializeField] private float nameplateScaleCountry  = 1.10f;
    [SerializeField] private float nameplateScaleDuchy    = 1.00f;
    [SerializeField] private float nameplateScaleLordship = 0.90f;
    [SerializeField] private float nameplateScalePoint    = 0.80f;
[Header("Nameplate Behavior")]
    [Tooltip("If true, clicking a MapPoint shows its CHILDREN nameplates. If false, clicking shows the clicked point's LAYER nameplates.")]
    [SerializeField] private bool clickShowsChildrenNameplates = true;

    [Tooltip("If true, when children are shown, nameplates may appear even if those child points are not currently visible on the active layer.")]
    [SerializeField] private bool allowNameplatesForHiddenChildren = true;

    [Tooltip("If true, clicking a point while children nameplates are shown (subset mode) will expand to showing ALL nameplates on the clicked point's layer.")]
    [SerializeField] private bool clickOnSubsetExpandsToLayer = true;

    [Header("Debug Logging")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private bool logClicks = true;
    [SerializeField] private bool logCameraResolve = true;

    public event Action<MapLayer, bool> LayerModeChanged;
    public event Action<bool> TravelModeChanged;

    // Travel mode state
    private bool travelMode = false;
    private bool wasGeographyModeBeforeTravel = false;

    [Header("Travel Mode Settings")]
    [Tooltip("Hex grid size (distance from center to hex corner in world units).")]
    [SerializeField] private float hexSizeUnits = 30f;
    [Tooltip("Miles per hex step (center-to-center distance).")]
    [SerializeField] private float milesPerHex = 25f;

    [Header("Travel Hex Grid Overlay")]
    [Tooltip("Optional: Assign a HexGridOverlayRenderer to draw + highlight hexes during Travel Mode. If null and AutoFind is enabled, MapManager will attempt to find one at runtime.")]
    [SerializeField] private HexGridOverlayRenderer hexGridOverlay;
    [SerializeField] private bool autoFindHexGridOverlay = true;
    [SerializeField] private bool showHexGridOnlyInTravelMode = true;
    [SerializeField] private bool highlightHoveredHexInTravelMode = true;
    [SerializeField] private bool highlightActiveTravelPath = true;

    // Runtime caches
    private readonly List<MapPoint> _allPoints = new List<MapPoint>(2048);
    private readonly Dictionary<MapPoint, MapPoint> _parentByChild = new Dictionary<MapPoint, MapPoint>();
    private MapLayer _effectiveLayer;
    private readonly HashSet<MapPoint> _interactableSet = new HashSet<MapPoint>();
    private readonly HashSet<MapPoint> _visibleSet = new HashSet<MapPoint>();

    private MapPoint _hovered;
    private Collider2D _hoveredCollider;
    private MapPoint _selected;
    private Collider2D _selectedCollider;

    private Collider2DOutlineHighlighter _hoverOutline;
    private Collider2DOutlineHighlighter _selectedOutline;

    private readonly Dictionary<string, GameObject> _openWindowsByKey = new Dictionary<string, GameObject>();

    private struct PendingWindowSpawn
    {
        public GameObject prefab;
        public MapPoint point;
        public string keyPrefix;
    }

    private bool _hasPendingWindowSpawn;
    private PendingWindowSpawn _pendingWindowSpawn;
    private Coroutine _pendingWindowSpawnRoutine;

    // Nameplate state
    private enum NameplateMode
    {
        Layer,
        ChildrenSubset
    }

    private NameplateMode _nameplateMode = NameplateMode.Layer;
    private MapLayer _nameplateLayer = MapLayer.Regional; // "active" layer being labeled when in Layer mode (or the subset's layer when in subset mode)
    private readonly Dictionary<MapPoint, MapPointNameplateUI> _nameplateByPoint = new Dictionary<MapPoint, MapPointNameplateUI>();
    private readonly HashSet<MapPoint> _activeNameplatePoints = new HashSet<MapPoint>();
    private readonly Dictionary<MapPoint, Sprite> _heraldrySpriteCache = new Dictionary<MapPoint, Sprite>();

    private MapCameraController _subscribedCameraController;

    private struct PickResult { public MapPoint point; public Collider2D collider; }

    public bool GeographyMode => geographyMode;
    public bool TravelMode => travelMode;
    public float HexSizeUnits => hexSizeUnits;
    public float MilesPerHex => milesPerHex;

    public MapLayer ActiveLayer => selectedLayer;
    public MapLayer EffectiveLayer => _effectiveLayer;

    public void SetActiveLayer(MapLayer layer) => SetSelectedLayer(layer);

    public void SetGeographyMode(bool enabled)
    {
        if (travelMode && enabled)
            SetTravelMode(false);

        geographyMode = enabled;

        if (enableLogging)
            Debug.Log($"[MapManager] Geography Mode set: {geographyMode} (SelectedLayer={selectedLayer})");

        RefreshVisibleAndInteractablePoints(force: true);
        ApplyNameplatesForManualLayerChange();
        LayerModeChanged?.Invoke(selectedLayer, geographyMode);
    }

    public void ToggleGeographyMode() => SetGeographyMode(!geographyMode);

    public void SetTravelMode(bool enabled)
    {
        if (enabled == travelMode) return;

        if (enabled)
        {
            wasGeographyModeBeforeTravel = geographyMode;
            geographyMode = false;

            if (enableLogging)
                Debug.Log("[MapManager] Travel Mode enabled");
        }
        else
        {
            geographyMode = wasGeographyModeBeforeTravel;

            if (enableLogging)
                Debug.Log($"[MapManager] Travel Mode disabled (restoring GeographyMode={geographyMode})");
        }

        travelMode = enabled;
        RefreshVisibleAndInteractablePoints(force: true);
        ApplyNameplatesForManualLayerChange();
        TravelModeChanged?.Invoke(travelMode);
        LayerModeChanged?.Invoke(selectedLayer, geographyMode);

        ResolveHexGridOverlay();
        UpdateTravelHexOverlay(force: true);
    }

    public void ToggleTravelMode() => SetTravelMode(!travelMode);

    private void OnEnable()
    {
        ResolveCamera(forceLog: true);
        ResolveInfoLayer();
        EnsureCameraControllerSubscription();
        RebuildPointCacheIfNeeded();
        RefreshVisibleAndInteractablePoints(force: true);
        EnsureOutlines();

        ResolveHexGridOverlay();
        UpdateTravelHexOverlay(force: true);

        // Initial nameplates: show current (effective) layer.
        ApplyNameplatesForManualLayerChange();
        UpdateActiveNameplatesPositions();
    }

    private void OnDisable()
    {
        if (_hoverOutline != null) _hoverOutline.Hide();
        if (_selectedOutline != null) _selectedOutline.Hide();

        if (_subscribedCameraController != null)
            _subscribedCameraController.OnSnapCompleted -= HandleCameraSnapCompleted;

        _subscribedCameraController = null;

        HideAllActiveNameplates();

        if (hexGridOverlay != null)
            hexGridOverlay.SetVisible(false);
    }

    private void Update()
    {
        ResolveCamera(forceLog: false);
        EnsureCameraControllerSubscription();

        if (worldCamera == null)
            return;

        HandleHoverAndClick();
        UpdateActiveNameplatesPositions();
        UpdateTravelHexOverlay();
    }

    // --------------------------
    // InfoLayer resolve
    // --------------------------
    private void ResolveInfoLayer()
    {
        if (infoLayerCanvas != null) return;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        if (canvases != null)
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null) continue;
                if (string.Equals(c.name, "InfoLayer", StringComparison.OrdinalIgnoreCase))
                {
                    infoLayerCanvas = c;
                    break;
                }
            }
        }

        if (infoLayerCanvas == null && canvases != null && canvases.Length > 0)
            infoLayerCanvas = canvases[0];

        if (enableLogging && logCameraResolve)
        {
            string name = infoLayerCanvas != null ? infoLayerCanvas.name : "null";
            Debug.Log($"[MapManager] Resolved InfoLayer canvas: {name}");
        }
    }

    private Transform GetPreferredUIParent()
    {
        if (infoLayerParentOverride != null) return infoLayerParentOverride;
        if (infoLayerCanvas != null) return infoLayerCanvas.transform;
        return uiParent;
    }

    // --------------------------
    // Camera resolve + logging
    // --------------------------
    private void ResolveCamera(bool forceLog)
    {
        Camera resolved = worldCamera;

        if (resolved == null)
        {
            if (cameraController == null)
            {
#if UNITY_2023_1_OR_NEWER
                cameraController = FindFirstObjectByType<MapCameraController>();
#else
                cameraController = FindObjectOfType<MapCameraController>();
#endif
            }

            if (cameraController != null)
                resolved = cameraController.GetComponent<Camera>();
        }

        if (resolved == null)
            resolved = Camera.main;

        if (resolved == null)
        {
#if UNITY_2023_1_OR_NEWER
            resolved = FindFirstObjectByType<Camera>();
#else
            resolved = FindObjectOfType<Camera>();
#endif
        }

        if (resolved != worldCamera)
        {
            worldCamera = resolved;

            if (enableLogging && logCameraResolve && (forceLog || worldCamera != null))
            {
                string camName = worldCamera != null ? worldCamera.name : "null";
                string ctrlName = cameraController != null ? cameraController.name : "null";
                Debug.Log($"[MapManager] Resolved world camera: {camName} (cameraController={ctrlName})");
            }
        }
    }

    private void EnsureCameraControllerSubscription()
    {
        if (cameraController == _subscribedCameraController)
            return;

        if (_subscribedCameraController != null)
            _subscribedCameraController.OnSnapCompleted -= HandleCameraSnapCompleted;

        _subscribedCameraController = cameraController;

        if (_subscribedCameraController != null)
            _subscribedCameraController.OnSnapCompleted += HandleCameraSnapCompleted;
    }

    // --------------------------
    // Layers
    // --------------------------
    private void SetSelectedLayer(MapLayer layer)
    {
        selectedLayer = layer;

        if (enableLogging)
            Debug.Log($"[MapManager] Selected layer set: {selectedLayer} (GeoMode={geographyMode})");

        RefreshVisibleAndInteractablePoints(force: true);
        ApplyNameplatesForManualLayerChange();
        LayerModeChanged?.Invoke(selectedLayer, geographyMode);
    }

    private void RefreshVisibleAndInteractablePoints(bool force)
    {
        RebuildPointCacheIfNeeded();
        EnsureLayerOrderValid();
        _effectiveLayer = ComputeEffectiveLayer(selectedLayer);

        _interactableSet.Clear();
        _visibleSet.Clear();

        if (travelMode)
        {
            // Travel Mode: TravelGroups interactable. SelectedLayer points may remain visible, but not interactable.
            for (int i = 0; i < _allPoints.Count; i++)
            {
                var p = _allPoints[i];
                if (p == null) continue;

                bool isTravel = (p.infoKind == MapPoint.InfoKind.TravelGroup);

                bool visible = isTravel || EqualityComparer<MapLayer>.Default.Equals(p.layer, selectedLayer);
                bool interactable = isTravel;

                p.SetState(visible: visible, interactable: interactable);

                if (visible) _visibleSet.Add(p);
                if (interactable) _interactableSet.Add(p);
            }
        }
        else
        {
            // Normal mode: ONLY points on the effective layer are visible AND interactable.
            for (int i = 0; i < _allPoints.Count; i++)
            {
                var p = _allPoints[i];
                if (p == null) continue;

                bool onEffectiveLayer = EqualityComparer<MapLayer>.Default.Equals(p.layer, _effectiveLayer);
                bool eligible = onEffectiveLayer && IsEligibleForLayerAndMode(p, _effectiveLayer);

                bool visible = eligible;
                bool interactable = eligible;

                p.SetState(visible: visible, interactable: interactable);

                if (visible) _visibleSet.Add(p);
                if (interactable) _interactableSet.Add(p);
            }
        }

        // Clear hover/selection if no longer interactable.
        if (_hovered != null && !_interactableSet.Contains(_hovered))
            SetHovered(default(MapPoint));

        if (_selected != null && !_interactableSet.Contains(_selected))
            SetSelected(default(MapPoint));
        else
            RefreshSelectionOutlines();
    }

    private MapLayer ComputeEffectiveLayer(MapLayer requested)
    {
        if (!fallbackIfSelectedLayerEmpty)
            return requested;

        if (HasAnyEligibleAtLayer(requested))
            return requested;

        int reqIdx = GetLayerIndexSafe(requested);

        for (int i = reqIdx; i >= 0; i--)
        {
            var layer = layerOrder[i];
            if (HasAnyEligibleAtLayer(layer))
                return layer;
        }

        for (int i = reqIdx + 1; i < layerOrder.Count; i++)
        {
            var layer = layerOrder[i];
            if (HasAnyEligibleAtLayer(layer))
                return layer;
        }

        return requested;
    }

    private bool HasAnyEligibleAtLayer(MapLayer layer)
    {
        for (int i = 0; i < _allPoints.Count; i++)
        {
            var p = _allPoints[i];
            if (p == null) continue;

            if (IsEligibleForLayerAndMode(p, layer))
                return true;
        }

        return false;
    }

    private bool IsEligibleForLayerAndMode(MapPoint p, MapLayer layer)
    {
        if (p == null) return false;

        if (!EqualityComparer<MapLayer>.Default.Equals(p.layer, layer))
            return false;

        if (p.infoKind == MapPoint.InfoKind.Region)
        {
            if (geographyMode && hideRegionsInGeographyMode)
                return false;
        }
        else if (geographyMode)
        {
            if (p.infoKind != MapPoint.InfoKind.Unpopulated)
                return false;
        }
        else
        {
            if (p.infoKind == MapPoint.InfoKind.Unpopulated || p.infoKind == MapPoint.InfoKind.TravelGroup)
                return false;
        }

        // Parent-child layering consistency
        int myIdx = GetLayerIndexSafe(p.layer);
        var parent = TryGetParentMapPoint(p);
        int safety = 0;

        while (parent != null && safety++ < 256)
        {
            int parentIdx = GetLayerIndexSafe(parent.layer);
            if (parentIdx >= myIdx)
                return false;

            parent = TryGetParentMapPoint(parent);
        }

        return true;
    }

    // --------------------------
    // Picking + hover/click
    // --------------------------
    private void HandleHoverAndClick()
    {
        if (MapUIRaycastUtil.IsPointerOverBlockingUI())
        {
            SetHovered(default(MapPoint));
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null || worldCamera == null)
            return;

        Vector2 mouseScreen = mouse.position.ReadValue();
        Vector3 world = ScreenToWorldOnZPlane(mouseScreen, mapPlaneZ);
        Vector2 mouseWorld = new Vector2(world.x, world.y);

        PickResult hit = PickByLayerStackAt(mouseWorld);
        SetHovered(hit);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (hit.point != null)
            {
                SetSelected(hit);
                OnPointClicked(hit.point);
            }
            else if (travelMode && _selected != null && _selected.infoKind == MapPoint.InfoKind.TravelGroup)
            {
                // Travel Mode: clicking empty map adds a waypoint for selected TravelGroup, if its window is open.
                HexAxial hex = HexGrid.LocalXYToAxial(mouseWorld, hexSizeUnits);
                string key = $"TravelInfo:{_selected.GetStableKey()}";

                if (_openWindowsByKey.TryGetValue(key, out var windowGo) && windowGo != null)
                {
                    TravelGroupWindowManager tgWindow = windowGo.GetComponentInChildren<TravelGroupWindowManager>();
                    if (tgWindow != null)
                        tgWindow.AddHexToPath(hex);
                }
            }
            else
            {
                // Empty click clears selection and returns to layer nameplates.
                SetSelected(default(MapPoint));
                ApplyNameplatesForManualLayerChange();
            }
        }
    }

    private PickResult PickByLayerStackAt(Vector2 mouseWorld)
    {
        var cols = Physics2D.OverlapPointAll(mouseWorld, mapPointPhysicsMask);
        if (cols == null || cols.Length == 0)
            return default;

        var hitPoints = new Dictionary<MapPoint, Collider2D>(64);

        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null || !c.enabled) continue;

            var p = c.GetComponentInParent<MapPoint>();
            if (p == null) continue;

            if (!_interactableSet.Contains(p))
                continue;

            if (!hitPoints.ContainsKey(p))
                hitPoints[p] = c;
        }

        if (hitPoints.Count == 0)
            return default;

        int curIdx = GetLayerIndexSafe(_effectiveLayer);

        var r = PickBestFromLayer(hitPoints, _effectiveLayer);
        if (r.point != null) return r;

        for (int i = curIdx + 1; i < layerOrder.Count; i++)
        {
            r = PickBestFromLayer(hitPoints, layerOrder[i]);
            if (r.point != null) return r;
        }

        for (int i = curIdx - 1; i >= 0; i--)
        {
            r = PickBestFromLayer(hitPoints, layerOrder[i]);
            if (r.point != null) return r;
        }

        return default;
    }

    private PickResult PickBestFromLayer(Dictionary<MapPoint, Collider2D> hits, MapLayer layer)
    {
        MapPoint bestPoint = null;
        Collider2D bestCollider = null;
        long bestScore = long.MinValue;

        foreach (var kv in hits)
        {
            var p = kv.Key;
            if (p == null) continue;

            if (!EqualityComparer<MapLayer>.Default.Equals(p.layer, layer))
                continue;

            long score = GetPickScoreWithinLayer(p);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = p;
                bestCollider = kv.Value;
            }
        }

        return new PickResult { point = bestPoint, collider = bestCollider };
    }

    private long GetPickScoreWithinLayer(MapPoint p)
    {
        if (p == null) return long.MinValue;

        // Prefer Regions within a layer
        int kindRank = (p.infoKind == MapPoint.InfoKind.Region) ? 0 : 1;
        return -kindRank;
    }

    private void SetHovered(PickResult result)
    {
        if (_hovered == result.point && _hoveredCollider == result.collider)
            return;

        _hovered = result.point;
        _hoveredCollider = result.collider;

        RefreshHoverOutline();
    }

    private void SetHovered(MapPoint point)
    {
        if (_hovered == point && _hoveredCollider != null)
            return;

        _hovered = point;
        _hoveredCollider = GetBestColliderForPoint(point, preferred: null);

        RefreshHoverOutline();
    }

    private void SetSelected(PickResult result)
    {
        SetSelected(result.point, result.collider);
    }

    private void SetSelected(MapPoint point, Collider2D collider = null)
    {
        if (_selected == point && _selectedCollider == collider)
            return;

        _selected = point;
        _selectedCollider = collider;

        RefreshSelectedOutline();
    }

    private void RefreshSelectionOutlines()
    {
        RefreshHoverOutline();
        RefreshSelectedOutline();
    }

    private void OnPointClicked(MapPoint point)
    {
        if (point == null) return;

        if (logClicks)
        {
            Debug.Log($"[MapManager] MapPoint clicked: key={point.GetStableKey()}, name={point.name}, " +
                      $"displayName={point.displayName}, layer={point.layer}, infoKind={point.infoKind}, geoMode={geographyMode}");
        }

        // Nameplates rule set (click-driven)
        ApplyNameplatesForClick(point);

        // Camera snap
        if (cameraController != null && point.defaultZoom > 0f)
            cameraController.SnapTo(point.GetBestFocusWorldPosition(), point.defaultZoom);

        // Windows
        if (point.infoKind == MapPoint.InfoKind.Region)
        {
            if (regionInfoWindowPrefab != null)
            {
                RequestWindow(regionInfoWindowPrefab, point, "RegionInfo");
                return;
            }
        }
        else if (point.infoKind == MapPoint.InfoKind.Unpopulated)
        {
            if (unpopulatedInfoWindowPrefab != null)
            {
                RequestWindow(unpopulatedInfoWindowPrefab, point, "UnpopulatedInfo");
                return;
            }
        }
        else if (point.infoKind == MapPoint.InfoKind.TravelGroup)
        {
            if (travelInfoWindowPrefab != null)
            {
                if (!travelMode) SetTravelMode(true);
                RequestWindow(travelInfoWindowPrefab, point, "TravelInfo");
                return;
            }
        }
        else
        {
            if (populatedInfoWindowPrefab != null)
            {
                RequestWindow(populatedInfoWindowPrefab, point, "PopulatedInfo");
                return;
            }
        }

        // Fallback
        point.gameObject.SendMessage("OpenInfoWindow", SendMessageOptions.DontRequireReceiver);
    }

    // --------------------------
    // Window spawning (deferred until snap completes)
    // --------------------------
    private void RequestWindow(GameObject prefab, MapPoint point, string keyPrefix)
    {
        if (prefab == null || point == null) return;

        string key = $"{keyPrefix}:{point.GetStableKey()}";
        if (TryFocusWindow(key))
            return;

        bool isSnapping = (deferWindowSpawnUntilSnapComplete && cameraController != null && cameraController.IsSnapping);

        if (isSnapping)
        {
            _pendingWindowSpawn = new PendingWindowSpawn
            {
                prefab = prefab,
                point = point,
                keyPrefix = keyPrefix
            };
            _hasPendingWindowSpawn = true;
            return; // last click wins
        }

        SpawnAndInitializeWindow(prefab, point, keyPrefix);
    }

    private void HandleCameraSnapCompleted()
    {
        // Spawn pending window (if any) AFTER snap completes.
        if (!_hasPendingWindowSpawn)
            return;

        if (_pendingWindowSpawnRoutine != null)
            StopCoroutine(_pendingWindowSpawnRoutine);

        _pendingWindowSpawnRoutine = StartCoroutine(SpawnPendingWindowEndOfFrame());
    }

    private IEnumerator SpawnPendingWindowEndOfFrame()
    {
        // Ensure final camera transform + UI layout (and DraggableWindow auto-fit) are settled this frame.
        yield return new WaitForEndOfFrame();

        if (_hasPendingWindowSpawn)
        {
            var req = _pendingWindowSpawn;
            _hasPendingWindowSpawn = false;

            if (req.prefab != null && req.point != null)
                SpawnAndInitializeWindow(req.prefab, req.point, req.keyPrefix);
        }

        _pendingWindowSpawnRoutine = null;
    }

    private void SpawnAndInitializeWindow(GameObject prefab, MapPoint point, string keyPrefix)
    {
        if (prefab == null || point == null) return;

        string key = $"{keyPrefix}:{point.GetStableKey()}";
        if (TryFocusWindow(key))
            return;

        Transform parent = GetPreferredUIParent();
        if (parent == null) return;

        var go = Instantiate(prefab, parent);

        EnsureIgnoreLayout(go);


        // IMPORTANT: Position the same RectTransform that DraggableWindow clamps/moves (if present),
        // otherwise DraggableWindow may clamp the window into a corner (often top-right).
        Transform moveRoot = ResolveWindowMoveRoot(go);
        ForceNonStretchAnchors(moveRoot as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        RegisterWindow(key, go);

        // Initialize window (look for Initialize(MapPoint))
        var comps = go.GetComponentsInChildren<MonoBehaviour>(true);
        bool inited = false;

        foreach (var mb in comps)
        {
            if (mb == null) continue;

            var m = mb.GetType().GetMethod(
                "Initialize",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(MapPoint) },
                null);

            if (m != null)
            {
                try
                {
                    m.Invoke(mb, new object[] { point });
                    inited = true;
                    break;
                }
                catch (TargetInvocationException tie)
                {
                    // WARNING only (avoid Error Pause)
                    Debug.LogWarning(
                        $"[MapManager] Initialize(MapPoint) threw in {mb.GetType().Name} for window '{key}'.\n" +
                        (tie.InnerException != null ? tie.InnerException.ToString() : tie.ToString()),
                        mb);

                    // keep searching for another Initialize(MapPoint) if present
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[MapManager] Failed invoking Initialize(MapPoint) in {mb.GetType().Name} for window '{key}'.\n{ex}",
                        mb);
                }

            }
        }

        if (!inited)
            go.SendMessage("Initialize", point, SendMessageOptions.DontRequireReceiver);

        // Spawn placement at focus point (post-pan because we spawn after snap).
        if (spawnWindowsAtPointFocus)
            PositionUIRootAtPoint(moveRoot, point, windowNorthWorldPadding, windowScreenOffset);

        if (enableLogging)
            Debug.Log($"[MapManager] Spawned window: {key}");
    }

    private static readonly BindingFlags _bfAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private Transform ResolveWindowMoveRoot(GameObject windowGo)
    {
        if (windowGo == null) return null;

        // Prefer DraggableWindow.dragTarget (private serialized) if present.
        var dw = windowGo.GetComponentInChildren<DraggableWindow>(true);
        if (dw != null)
        {
            try
            {
                var f = typeof(DraggableWindow).GetField("dragTarget", _bfAny);
                if (f != null)
                {
                    var rt = f.GetValue(dw) as RectTransform;
                    if (rt != null) return rt.transform;
                }
            }
            catch { /* ignore */ }

            // Fallback: the RectTransform on the DraggableWindow object itself
            var dwrt = dw.GetComponent<RectTransform>();
            if (dwrt != null) return dwrt.transform;
        }

        // Fallback: root RectTransform, else direct child RectTransform, else any RectTransform.
        var rootRt = windowGo.GetComponent<RectTransform>();
        if (rootRt != null) return rootRt.transform;

        RectTransform childRt = null;
        for (int i = 0; i < windowGo.transform.childCount; i++)
        {
            var c = windowGo.transform.GetChild(i) as RectTransform;
            if (c != null) { childRt = c; break; }
        }
        if (childRt != null) return childRt.transform;

        var any = windowGo.GetComponentInChildren<RectTransform>(true);
        if (any != null) return any.transform;

        return windowGo.transform;
    }

    private void PositionUIRootAtPoint(Transform uiRoot, MapPoint point, float northWorldPadding, Vector2 screenOffset)
    {
        if (uiRoot == null || point == null || worldCamera == null)
            return;

        Vector3 anchorWorld = point.GetBestFocusWorldPosition();

        if (infoLayerCanvas != null && infoLayerCanvas.renderMode == RenderMode.WorldSpace)
        {
            Vector3 pos = new Vector3(anchorWorld.x, anchorWorld.y + Mathf.Max(0.01f, northWorldPadding), infoLayerCanvas.transform.position.z);
            uiRoot.position = pos;
            return;
        }

        // Screen-space canvas
        Vector3 sp3 = worldCamera.WorldToScreenPoint(anchorWorld);
        Vector2 sp = new Vector2(sp3.x, sp3.y) + screenOffset;

        Canvas canvas = infoLayerCanvas != null ? infoLayerCanvas : uiRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        // Convert into the LOCAL space of the uiRoot's parent RectTransform (not always the canvas root).
        RectTransform rt = uiRoot as RectTransform;
        RectTransform referenceRect = (rt != null && rt.parent is RectTransform prt) ? prt : (canvas.transform as RectTransform);
        if (referenceRect == null)
            return;

        Camera uiCam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCam = null;
        }
        else
        {
            uiCam = canvas.worldCamera != null ? canvas.worldCamera : worldCamera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, sp, uiCam, out Vector2 local))
        {
            if (rt != null)
            {
                // If anchors were forced to center, anchoredPosition aligns with local point in parent space.
                rt.anchoredPosition = local;
                rt.anchoredPosition3D = new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, 0f);
            }
            else
            {
                // Non-UI Transform: best-effort fallback
                uiRoot.position = new Vector3(sp.x, sp.y, 0f);
            }
        }
        else
        {
            if (enableLogging)
            {
                Debug.LogWarning($"[MapManager] Failed to convert screen->local for UI spawn. Canvas={canvas.name}, RenderMode={canvas.renderMode}, UIcam={(uiCam != null ? uiCam.name : "null")}");
            }
        }
    }

    private bool TryFocusWindow(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_openWindowsByKey.TryGetValue(key, out var go) && go != null)
        {
            if (!go.activeSelf) go.SetActive(true);
            if (go.transform.parent != null) go.transform.SetAsLastSibling();
            go.SendMessage("Focus", SendMessageOptions.DontRequireReceiver);

            if (enableLogging)
                Debug.Log($"[MapManager] Focused existing window: {key}");

            return true;
        }

        _openWindowsByKey.Remove(key);
        return false;
    }

    private void RegisterWindow(string key, GameObject windowRoot)
    {
        if (string.IsNullOrWhiteSpace(key) || windowRoot == null)
            return;

        _openWindowsByKey[key] = windowRoot;

        var ident = windowRoot.GetComponent<MapInfoWindowIdentity>();
        if (ident == null) ident = windowRoot.AddComponent<MapInfoWindowIdentity>();
        ident.Initialize(this, key);

        if (enableLogging)
            Debug.Log($"[MapManager] Registered window: {key}");
    }

    public void UnregisterWindow(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        _openWindowsByKey.Remove(key);
    }

    // --------------------------
    // Nameplates (multi-instance, controlled by layer/click rules)
    // --------------------------
    private void ApplyNameplatesForManualLayerChange()
    {
        _nameplateMode = NameplateMode.Layer;

        // Use effective layer to match what is actually visible.
        _nameplateLayer = _effectiveLayer;

        SetActiveNameplatesForLayer(_nameplateLayer);
    }

    private void ApplyNameplatesForClick(MapPoint clicked)
    {
        if (!clickShowsChildrenNameplates || clicked == null)
        {
            ApplyNameplatesForLayer(clicked != null ? clicked.layer : _effectiveLayer);
            return;
        }

        // If we are currently showing a "children subset" and the user clicks one of those subset points,
        // expand to showing the entire clicked layer (rule: deeper click shows that layer's nameplates).
        if (clickOnSubsetExpandsToLayer
            && _nameplateMode == NameplateMode.ChildrenSubset
            && _activeNameplatePoints.Contains(clicked))
        {
            ApplyNameplatesForLayer(clicked.layer);
            return;
        }

        // Otherwise show children nameplates
        var kids = clicked.GetChildren();
        if (kids == null || kids.Count == 0)
        {
            ApplyNameplatesForLayer(clicked.layer);
            return;
        }

        _nameplateMode = NameplateMode.ChildrenSubset;

        // In subset mode, we define "nameplate layer" as the MOST COMMON child layer (for the "expand" rule),
        // or fallback to clicked.layer if mixed/empty.
        _nameplateLayer = ComputeDominantLayer(kids, fallback: clicked.layer);

        var next = new HashSet<MapPoint>();
        for (int i = 0; i < kids.Count; i++)
        {
            var c = kids[i];
            if (c == null) continue;

            if (!allowNameplatesForHiddenChildren && !_visibleSet.Contains(c))
                continue;

            next.Add(c);
        }

        if (next.Count == 0)
        {
            ApplyNameplatesForLayer(clicked.layer);
            return;
        }

        SetActiveNameplates(next);
    }

    private void ApplyNameplatesForLayer(MapLayer layer)
    {
        _nameplateMode = NameplateMode.Layer;
        _nameplateLayer = layer;

        SetActiveNameplatesForLayer(layer);
    }

    private void SetActiveNameplatesForLayer(MapLayer layer)
    {
        var next = new HashSet<MapPoint>();

        // Only enable nameplates for points that MapManager currently considers visible.
        foreach (var p in _visibleSet)
        {
            if (p == null) continue;
            if (!EqualityComparer<MapLayer>.Default.Equals(p.layer, layer))
                continue;

            next.Add(p);
        }

        SetActiveNameplates(next);
    }

    private void SetActiveNameplates(HashSet<MapPoint> next)
    {
        // Hide those not in next
        var toHide = new List<MapPoint>();
        foreach (var p in _activeNameplatePoints)
        {
            if (p == null) { toHide.Add(p); continue; }
            if (!next.Contains(p))
                toHide.Add(p);
        }
        for (int i = 0; i < toHide.Count; i++)
            HideNameplateForPoint(toHide[i]);

        // Show those in next
        foreach (var p in next)
        {
            if (p == null) continue;
            ShowNameplateForPoint(p);
        }

        _activeNameplatePoints.Clear();
        foreach (var p in next)
            if (p != null) _activeNameplatePoints.Add(p);
    }

    private void HideAllActiveNameplates()
    {
        var list = new List<MapPoint>(_activeNameplatePoints);
        for (int i = 0; i < list.Count; i++)
            HideNameplateForPoint(list[i]);

        _activeNameplatePoints.Clear();
    }

    private void ShowNameplateForPoint(MapPoint p)
    {
        if (p == null || mapPointNameplatePrefab == null)
            return;

        var ui = GetOrCreateNameplate(p);
        if (ui == null)
            return;

        string title = !string.IsNullOrWhiteSpace(p.displayName) ? p.displayName : p.name;
        TryResolveHeraldrySprite(p, out Sprite heraldry);

        ui.SetContent(title, heraldry);
        ui.SetLayerBaseScale(GetNameplateBaseScale(p.layer));
        ui.Show();
    }

    private void HideNameplateForPoint(MapPoint p)
    {
        if (p == null) return;

        if (_nameplateByPoint.TryGetValue(p, out var ui) && ui != null)
            ui.Hide();
    }

    private MapPointNameplateUI GetOrCreateNameplate(MapPoint p)
    {
        if (p == null) return null;

        if (_nameplateByPoint.TryGetValue(p, out var ui) && ui != null)
            return ui;

        Transform parent = GetPreferredUIParent();
        if (parent == null) return null;

        GameObject go = Instantiate(mapPointNameplatePrefab, parent);

        EnsureIgnoreLayout(go);

        go.name = $"__Nameplate_{p.GetStableKey()}";

        ui = go.GetComponent<MapPointNameplateUI>();
        if (ui == null)
        {
            if (enableLogging)
                Debug.LogError("[MapManager] Nameplate prefab is missing MapPointNameplateUI.");
            Destroy(go);
            return null;
        }

        Canvas bind = infoLayerCanvas != null ? infoLayerCanvas : go.GetComponentInParent<Canvas>();
        ui.BindCanvas(bind);
        ui.Hide();

        _nameplateByPoint[p] = ui;
        return ui;
    }

    private void UpdateActiveNameplatesPositions()
    {
        if (_activeNameplatePoints.Count == 0) return;
        if (worldCamera == null) return;

        // If pointer is over blocking UI, do not move nameplates around (keeps them stable during UI interactions).
        // Comment out if undesired.
        // if (MapUIRaycastUtil.IsPointerOverBlockingUI()) return;

        foreach (var p in _activeNameplatePoints)
        {
            if (p == null) continue;
            if (!_nameplateByPoint.TryGetValue(p, out var ui) || ui == null) continue;

            ui.SetLayerBaseScale(GetNameplateBaseScale(p.layer));

            var col = GetBestColliderForPoint(p, preferred: null);
            Vector3 anchorWorld = (col != null)
                ? new Vector3(col.bounds.center.x, col.bounds.center.y, p.GetBestFocusWorldPosition().z)
                : p.GetBestFocusWorldPosition();

            if (infoLayerCanvas != null && infoLayerCanvas.renderMode == RenderMode.WorldSpace)
            {
                Vector3 pos = new Vector3(
                    anchorWorld.x,
                    anchorWorld.y + Mathf.Max(0.01f, nameplateNorthWorldPadding),
                    infoLayerCanvas.transform.position.z);

                ui.SetScreenPosition(pos);
                continue;
            }

            Vector3 sp3 = worldCamera.WorldToScreenPoint(anchorWorld);
            Vector2 sp = new Vector2(sp3.x, sp3.y) + nameplateScreenOffset;
            ui.SetScreenPosition(sp);
        }
    }

    private MapLayer ComputeDominantLayer(List<MapPoint> points, MapLayer fallback)
    {
        if (points == null || points.Count == 0) return fallback;

        var counts = new Dictionary<MapLayer, int>();
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p == null) continue;

            if (!counts.TryGetValue(p.layer, out int c))
                c = 0;
            counts[p.layer] = c + 1;
        }

        int best = int.MinValue;
        MapLayer bestLayer = fallback;

        foreach (var kv in counts)
        {
            if (kv.Value > best)
            {
                best = kv.Value;
                bestLayer = kv.Key;
            }
        }

        return bestLayer;
    }

    private bool TryResolveHeraldrySprite(MapPoint point, out Sprite sprite)
    {
        sprite = null;
        if (point == null) return false;

        if (_heraldrySpriteCache.TryGetValue(point, out sprite) && sprite != null)
            return true;

        Sprite best = null;
        int bestScore = int.MinValue;

        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = point.GetType();

        var fields = type.GetFields(BF);
        for (int i = 0; i < fields.Length; i++)
        {
            var f = fields[i];
            if (f == null || f.FieldType != typeof(Sprite)) continue;

            var v = f.GetValue(point) as Sprite;
            if (v == null) continue;

            int s = ScoreSpriteMemberName(f.Name);
            if (s > bestScore) { bestScore = s; best = v; }
        }

        var props = type.GetProperties(BF);
        for (int i = 0; i < props.Length; i++)
        {
            var p = props[i];
            if (p == null || p.PropertyType != typeof(Sprite) || !p.CanRead) continue;
            if (p.GetIndexParameters() != null && p.GetIndexParameters().Length > 0) continue;

            Sprite v = null;
            try { v = p.GetValue(point, null) as Sprite; } catch { }
            if (v == null) continue;

            int s = ScoreSpriteMemberName(p.Name);
            if (s > bestScore) { bestScore = s; best = v; }
        }

        if (best == null)
        {
            var srs = point.GetComponentsInChildren<SpriteRenderer>(true);
            if (srs != null)
            {
                for (int i = 0; i < srs.Length; i++)
                {
                    var sr = srs[i];
                    if (sr == null || sr.sprite == null) continue;

                    int s = ScoreSpriteMemberName(sr.gameObject != null ? sr.gameObject.name : sr.name);
                    if (s > bestScore) { bestScore = s; best = sr.sprite; }
                }

                if (best == null)
                {
                    for (int i = 0; i < srs.Length; i++)
                    {
                        var sr = srs[i];
                        if (sr != null && sr.sprite != null)
                        {
                            best = sr.sprite;
                            break;
                        }
                    }
                }
            }
        }

        sprite = best;

        if (sprite != null)
            _heraldrySpriteCache[point] = sprite;

        return sprite != null;
    }

    private static int ScoreSpriteMemberName(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        string n = name.ToLowerInvariant();

        int score = 0;
        if (n.Contains("herald")) score += 100;
        if (n.Contains("crest")) score += 80;
        if (n.Contains("banner")) score += 70;
        if (n.Contains("emblem")) score += 60;
        if (n.Contains("icon")) score += 50;
        if (n.Contains("sigil")) score += 40;
        if (n.Contains("coat")) score += 30;
        return score;
    }

    // --------------------------
    // Outline helpers
    // --------------------------
    private void EnsureOutlines()
    {
        if (_hoverOutline == null)
            _hoverOutline = CreateOutlineHighlighter("__HoverOutline");
        if (_selectedOutline == null)
            _selectedOutline = CreateOutlineHighlighter("__SelectedOutline");

        _hoverOutline.Hide();
        _selectedOutline.Hide();
    }

    private Collider2DOutlineHighlighter CreateOutlineHighlighter(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.hideFlags = HideFlags.DontSave;

        int ignore = LayerMask.NameToLayer("Ignore Raycast");
        if (ignore >= 0) go.layer = ignore;

        var h = go.AddComponent<Collider2DOutlineHighlighter>();
        h.Hide();
        return h;
    }

    private void RefreshHoverOutline()
    {
        EnsureOutlines();

        ResolveHexGridOverlay();
        UpdateTravelHexOverlay(force: true);

        if (!enableHoverOutline || _hovered == null || _hoveredCollider == null)
        {
            _hoverOutline.Hide();
            return;
        }

        if (!_hovered.UseColliderOutlineHighlight)
        {
            _hoverOutline.Hide();
            return;
        }

        var col = GetBestColliderForPoint(_hovered, _hoveredCollider);
        if (col == null)
        {
            _hoverOutline.Hide();
            return;
        }

        int sortingLayerId = SortingLayer.NameToID(outlineSortingLayerName);
        int order = outlineBaseSortingOrder;

        Color c = GetOutlineColor(_hovered, isSelected: false);
        float width = Mathf.Max(0.0001f, outlineWidthWorldUnits * Mathf.Max(0.01f, _hovered.OutlineWidth));

        _hoverOutline.Show(col, c, width, circleSegments, sortingLayerId, order);
    }

    private void RefreshSelectedOutline()
    {
        EnsureOutlines();

        ResolveHexGridOverlay();
        UpdateTravelHexOverlay(force: true);

        if (!enableSelectedOutline || _selected == null)
        {
            _selectedOutline.Hide();
            return;
        }

        if (!_selected.UseColliderOutlineHighlight)
        {
            _selectedOutline.Hide();
            return;
        }

        var col = GetBestColliderForPoint(_selected, _selectedCollider);
        if (col == null)
        {
            _selectedOutline.Hide();
            return;
        }

        int sortingLayerId = SortingLayer.NameToID(outlineSortingLayerName);
        int order = outlineBaseSortingOrder + 1;

        Color c = GetOutlineColor(_selected, isSelected: true);
        float width = Mathf.Max(0.0001f, outlineWidthWorldUnits * Mathf.Max(0.01f, _selected.OutlineWidth));

        _selectedOutline.Show(col, c, width, circleSegments, sortingLayerId, order);
    }

    private Color GetOutlineColor(MapPoint p, bool isSelected)
    {
        if (outlinePalette == null || outlinePalette.Count == 0)
            return Color.white;

        if (!colorByLayer || p == null)
            return outlinePalette[Mathf.Clamp(isSelected ? selectedPaletteIndex : hoverPaletteIndex, 0, outlinePalette.Count - 1)];

        int idx = GetLayerIndexSafe(p.layer);
        int paletteIdx = idx % outlinePalette.Count;
        return outlinePalette[paletteIdx];
    }

    // --------------------------
    // Cache + parent mapping
    // --------------------------
    private void RebuildPointCacheIfNeeded()
    {
        if (_allPoints.Count == 0)
            RebuildPointCache();
    }

    public void RebuildPointCache()
    {
        _allPoints.Clear();
        _parentByChild.Clear();

#if UNITY_2023_1_OR_NEWER
        MapPoint[] found = pointsRoot != null
            ? pointsRoot.GetComponentsInChildren<MapPoint>(true)
            : FindObjectsByType<MapPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        MapPoint[] found = pointsRoot != null
            ? pointsRoot.GetComponentsInChildren<MapPoint>(true)
            : FindObjectsOfType<MapPoint>(true);
#endif

        if (found != null)
        {
            for (int i = 0; i < found.Length; i++)
                if (found[i] != null)
                    _allPoints.Add(found[i]);
        }

        for (int i = 0; i < _allPoints.Count; i++)
        {
            var parent = _allPoints[i];
            if (parent == null) continue;

            var kids = parent.GetChildren();
            if (kids == null) continue;

            for (int k = 0; k < kids.Count; k++)
            {
                var child = kids[k];
                if (child == null) continue;

                if (!_parentByChild.ContainsKey(child))
                    _parentByChild[child] = parent;
            }
        }
    }

    private MapPoint TryGetParentMapPoint(MapPoint p)
    {
        if (p == null) return null;

        if (_parentByChild.TryGetValue(p, out var parent) && parent != null)
            return parent;

        Transform pt = p.transform.parent;
        while (pt != null)
        {
            var mp = pt.GetComponent<MapPoint>();
            if (mp != null) return mp;
            pt = pt.parent;
        }

        return null;
    }

    // --------------------------
    // Layer order helpers
    // --------------------------
    private void EnsureLayerOrderValid()
    {
        if (layerOrder == null)
            layerOrder = new List<MapLayer>();

        if (layerOrder.Count == 0)
        {
            layerOrder.Add(MapLayer.Regional);
            layerOrder.Add(MapLayer.Country);
            layerOrder.Add(MapLayer.Duchy);
            layerOrder.Add(MapLayer.Lordship);
            layerOrder.Add(MapLayer.Point);
        }

        var seen = new HashSet<MapLayer>();
        for (int i = layerOrder.Count - 1; i >= 0; i--)
        {
            if (!seen.Add(layerOrder[i]))
                layerOrder.RemoveAt(i);
        }
    }

    private int GetLayerIndexSafe(MapLayer layer)
    {
        EnsureLayerOrderValid();
        int idx = layerOrder.IndexOf(layer);
        return idx >= 0 ? idx : 0;
    }


    private float GetNameplateBaseScale(MapLayer layer)
    {
        switch (layer)
        {
            case MapLayer.Regional: return Mathf.Max(0.01f, nameplateScaleRegional);
            case MapLayer.Country:  return Mathf.Max(0.01f, nameplateScaleCountry);
            case MapLayer.Duchy:    return Mathf.Max(0.01f, nameplateScaleDuchy);
            case MapLayer.Lordship: return Mathf.Max(0.01f, nameplateScaleLordship);
            case MapLayer.Point:    return Mathf.Max(0.01f, nameplateScalePoint);
            default:                return 1f;
        }
    }


    private static void EnsureIgnoreLayout(GameObject windowRoot)
    {
        if (windowRoot == null) return;

        // If the chosen UI parent has any LayoutGroup, child positioning may be overridden (often landing in a corner).
        // Setting ignoreLayout prevents that.
        var le = windowRoot.GetComponent<LayoutElement>();
        if (le == null) le = windowRoot.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    private static void ForceNonStretchAnchors(RectTransform rt, Vector2 anchor, Vector2 pivot)
    {
        if (rt == null) return;

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
    }

    // --------------------------
    // Utility
    // --------------------------
    private Vector3 ScreenToWorldOnZPlane(Vector2 screenPos, float zPlane)
    {
        if (worldCamera == null)
            return Vector3.zero;

        Ray ray = worldCamera.ScreenPointToRay(screenPos);

        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, zPlane));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        float zDist = Mathf.Abs(zPlane - worldCamera.transform.position.z);
        return worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDist));
    }

    private Collider2D GetBestColliderForPoint(MapPoint point, Collider2D preferred)
    {
        if (preferred != null) return preferred;
        if (point != null && point.regionBoundaryCollider != null) return point.regionBoundaryCollider;
        return point != null ? point.GetComponent<Collider2D>() : null;
    }

    // --------------------------
    // Travel Hex Grid Overlay
    // --------------------------
    private void ResolveHexGridOverlay()
    {
        if (hexGridOverlay != null) return;
        if (!autoFindHexGridOverlay) return;

#if UNITY_2023_1_OR_NEWER
        hexGridOverlay = FindFirstObjectByType<HexGridOverlayRenderer>();
#else
        hexGridOverlay = FindObjectOfType<HexGridOverlayRenderer>(true);
#endif
        if (hexGridOverlay != null && worldCamera != null)
            hexGridOverlay.BindCamera(worldCamera);
    }

    private TravelGroupWindowManager GetOpenTravelWindowFor(MapPoint travelGroupPoint)
    {
        if (travelGroupPoint == null) return null;

        string key = $"TravelInfo:{travelGroupPoint.GetStableKey()}";
        if (_openWindowsByKey.TryGetValue(key, out var go) && go != null)
            return go.GetComponentInChildren<TravelGroupWindowManager>(true);

        return null;
    }

    private void UpdateTravelHexOverlay(bool force = false)
    {
        ResolveHexGridOverlay();
        if (hexGridOverlay == null) return;

        bool show = !showHexGridOnlyInTravelMode || travelMode;
        hexGridOverlay.SetVisible(show);
        if (!show) return;

        if (worldCamera != null)
            hexGridOverlay.BindCamera(worldCamera);

        hexGridOverlay.SetGridParams(hexSizeUnits, mapPlaneZ);

        if (travelMode && highlightHoveredHexInTravelMode && !MapUIRaycastUtil.IsPointerOverBlockingUI() && Mouse.current != null)
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 world = ScreenToWorldOnZPlane(mouseScreen, mapPlaneZ);
            HexAxial hovered = HexGrid.LocalXYToAxial(new Vector2(world.x, world.y), hexSizeUnits);
            hexGridOverlay.SetHoveredHex(hovered);
        }
        else
        {
            hexGridOverlay.ClearHoveredHex();
        }

        if (travelMode && highlightActiveTravelPath && _selected != null && _selected.infoKind == MapPoint.InfoKind.TravelGroup)
        {
            HexAxial startHex = HexGrid.LocalXYToAxial(new Vector2(_selected.transform.position.x, _selected.transform.position.y), hexSizeUnits);
            var w = GetOpenTravelWindowFor(_selected);
            var path = (w != null) ? w.CurrentPath : null;
            hexGridOverlay.SetPath(startHex, path);
        }
        else
        {
            hexGridOverlay.ClearPath();
        }

        if (force)
            hexGridOverlay.ForceRebuild();
    }
}
