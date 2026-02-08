using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SeaOfFallenStars.WorldData;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MapPoint : MonoBehaviour
{
    public enum InfoKind
    {
        Region = 0,
        Settlement = 1,
        PointOfInterest = 2,
        Unpopulated = 3,
        TravelGroup = 4    // NEW: Travel group type
    }

    public enum UnpopulatedSubtype
    {
        Wilderness = 0,
        Water = 1,
        Ruins = 2
    }

    [Header("Identity")]
    public string pointId;
    public string displayName;

    [Header("Classification")]
    public MapLayer layer = MapLayer.Regional;
    public InfoKind infoKind = InfoKind.Region;

    [Tooltip("True only for Settlement / PointOfInterest (things that load SettlementInfoData).")]
    public bool isPopulated = false;

    [Tooltip("Only used if infoKind == Unpopulated.")]
    public UnpopulatedSubtype unpopulatedSubtype = UnpopulatedSubtype.Wilderness;

    [Header("Legacy (Compatibility)")]
    public Collider2D regionBoundaryCollider;

    [TextArea(3, 12)]
    public string mainDescription;

    [Header("Camera Focus")]
    [SerializeField] private Transform focusTransform;
    public float defaultZoom = 0f;

    [Tooltip("Legacy; MapManager now uses collider picking.")]
    public float selectionRadius = 0.75f;

    [Header("Theme Music")]
    public AudioClip themeMusic;

    [Header("Highlight / Outline")]
    [SerializeField] private bool useColliderOutlineHighlight = true;
    [SerializeField] private float outlineWidth = 1f;

    [Header("Hierarchy (Children)")]
    [SerializeField] private List<MapPoint> childMapPoints = new List<MapPoint>();

    [Header("Visuals (Optional)")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Graphic uiGraphic;

    [Header("Safety / Diagnostics")]
    [SerializeField] private bool autoDisableExtraColliders = true;

    // NEW: If this MapPoint is a Travel Group, these lists define its members
    [Header("Travel Group Members")]
    public string[] characterIds;  // Character IDs in this travel group
    public string[] armyIds;       // Army IDs in this travel group

    private Collider2D _collider;
    private SettlementInfoData _settlementCache;
    private UnpopulatedInfoData _unpopulatedCache;
    private RegionInfoData _regionCache;
    private bool _settlementLoaded;
    private bool _unpopLoaded;
    private bool _regionLoaded;

    public bool UseColliderOutlineHighlight => useColliderOutlineHighlight;
    public float OutlineWidth => outlineWidth;

    private void Awake()
    {
        EnsureReferences();
        if (string.IsNullOrWhiteSpace(pointId))
            pointId = gameObject.name;
    }

    private void OnValidate()
    {
        EnsureReferences();
        if (string.IsNullOrWhiteSpace(pointId))
            pointId = gameObject.name;

        // Keep these consistent so Region/Unpopulated never try to behave like Settlement.
        switch (infoKind)
        {
            case InfoKind.Settlement:
            case InfoKind.PointOfInterest:
                isPopulated = true;
                break;
            case InfoKind.Region:
            case InfoKind.Unpopulated:
            case InfoKind.TravelGroup:   // NEW: TravelGroup is not a populated settlement
            default:
                isPopulated = false;
                break;
        }
    }

    private void EnsureReferences()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (uiGraphic == null)
            uiGraphic = GetComponentInChildren<Graphic>(true);
        if (visualRoot == null)
        {
            if (spriteRenderer != null) visualRoot = spriteRenderer.gameObject;
            else if (uiGraphic != null) visualRoot = uiGraphic.gameObject;
        }

        var colliders = GetComponents<Collider2D>();
        if (colliders == null || colliders.Length == 0)
        {
            _collider = GetComponent<Collider2D>();
        }
        else
        {
            Collider2D primary = null;
            if (regionBoundaryCollider != null && regionBoundaryCollider.gameObject == gameObject)
                primary = regionBoundaryCollider;
            if (primary == null)
            {
                // Prefer any PolygonCollider2D as primary if present
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] is PolygonCollider2D)
                    {
                        primary = colliders[i];
                        break;
                    }
                }
            }
            if (primary == null)
                primary = colliders[0];
            _collider = primary;
            if (regionBoundaryCollider == null)
                regionBoundaryCollider = _collider;
            if (colliders.Length > 1 && autoDisableExtraColliders)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] == null) continue;
                    if (colliders[i] == primary) continue;
                    colliders[i].enabled = false;
                }
            }
        }
        if (regionBoundaryCollider == null)
            regionBoundaryCollider = _collider;
    }

    public string GetStableKey()
    {
        return !string.IsNullOrWhiteSpace(pointId) ? pointId.Trim() : gameObject.name;
    }

    public Vector3 GetBestFocusWorldPosition()
    {
        if (focusTransform != null)
            return focusTransform.position;
        EnsureReferences();
        if (_collider != null)
            return _collider.bounds.center;
        return transform.position;
    }

    public List<MapPoint> GetChildren()
    {
        if (childMapPoints == null)
            childMapPoints = new List<MapPoint>();
        childMapPoints.RemoveAll(p => p == null);
        return childMapPoints;
    }

    public void SetState(bool visible, bool interactable)
    {
        EnsureReferences();
        if (_collider != null) _collider.enabled = interactable;
        if (regionBoundaryCollider != null && regionBoundaryCollider != _collider)
            regionBoundaryCollider.enabled = interactable;
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
        if (uiGraphic != null)
            uiGraphic.enabled = visible;
        if (visualRoot != null && visualRoot != gameObject)
            visualRoot.SetActive(visible);
    }

    // -------------------------
    // Data loading + logging
    // -------------------------

    public SettlementInfoData GetSettlementInfoData(bool forceReload = false)
    {
        // Only Settlement/POI load SettlementInfoData
        if (infoKind != InfoKind.Settlement && infoKind != InfoKind.PointOfInterest)
            return null;
        if (!forceReload && _settlementLoaded)
            return _settlementCache;
        _settlementLoaded = true;
        _settlementCache = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
            DataPaths.RuntimeSettlementsDir, DataPaths.EditorSettlementsDir, GetStableKey());
        Debug.Log(_settlementCache != null
            ? $"[MapPoint] Loaded SettlementInfoData for '{GetStableKey()}'"
            : $"[MapPoint] Missing SettlementInfoData for '{GetStableKey()}'");
        if (_settlementCache != null)
        {
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(_settlementCache.displayName))
                displayName = _settlementCache.displayName;
            if (string.IsNullOrWhiteSpace(mainDescription) && _settlementCache.main != null && !string.IsNullOrWhiteSpace(_settlementCache.main.description))
                mainDescription = _settlementCache.main.description;
        }
        return _settlementCache;
    }

    public UnpopulatedInfoData GetUnpopulatedInfoData(bool forceReload = false)
    {
        // Only Unpopulated loads UnpopulatedInfoData
        if (infoKind != InfoKind.Unpopulated)
            return null;
        if (!forceReload && _unpopLoaded)
            return _unpopulatedCache;
        _unpopLoaded = true;
        // Primary: dedicated Unpopulated directories (if set)
        _unpopulatedCache = JsonDataLoader.TryLoadFromEitherPath<UnpopulatedInfoData>(
            DataPaths.RuntimeUnpopulatedDir, DataPaths.EditorUnpopulatedDir, GetStableKey());
        // Fallback: allow MapData/Unpopulated or MapData root
        if (_unpopulatedCache == null)
        {
            string rtRoot = DataPaths.Runtime_MapDataPath;
            string edRoot = DataPaths.Editor_MapDataPath;
            string rtFolder = !string.IsNullOrWhiteSpace(rtRoot) ? Path.Combine(rtRoot, "Unpopulated") : rtRoot;
            string edFolder = !string.IsNullOrWhiteSpace(edRoot) ? Path.Combine(edRoot, "Unpopulated") : edRoot;
            _unpopulatedCache = JsonDataLoader.TryLoadFromEitherPath<UnpopulatedInfoData>(rtFolder, edFolder, GetStableKey())
                                ?? JsonDataLoader.TryLoadFromEitherPath<UnpopulatedInfoData>(rtRoot, edRoot, GetStableKey());
        }
        Debug.Log(_unpopulatedCache != null
            ? $"[MapPoint] Loaded UnpopulatedInfoData for '{GetStableKey()}'"
            : $"[MapPoint] Missing UnpopulatedInfoData for '{GetStableKey()}'");
        if (_unpopulatedCache != null)
        {
            // If JSON specifies subtype, update enum
            if (!string.IsNullOrWhiteSpace(_unpopulatedCache.subtype))
            {
                if (Enum.TryParse(_unpopulatedCache.subtype, ignoreCase: true, out UnpopulatedSubtype parsed))
                    unpopulatedSubtype = parsed;
            }
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(_unpopulatedCache.displayName))
                displayName = _unpopulatedCache.displayName;
            if (string.IsNullOrWhiteSpace(mainDescription) && _unpopulatedCache.main != null && !string.IsNullOrWhiteSpace(_unpopulatedCache.main.description))
                mainDescription = _unpopulatedCache.main.description;
        }
        return _unpopulatedCache;
    }

    public RegionInfoData GetRegionInfoData(bool forceReload = false)
    {
        if (infoKind != InfoKind.Region)
            return null;
        if (!forceReload && _regionLoaded)
            return _regionCache;
        _regionLoaded = true;
        // Preferred: MapDataPath/Regions/<id>.json
        string rtRoot = DataPaths.Runtime_MapDataPath;
        string edRoot = DataPaths.Editor_MapDataPath;
        string rtRegions = !string.IsNullOrWhiteSpace(rtRoot) ? Path.Combine(rtRoot, "Regions") : rtRoot;
        string edRegions = !string.IsNullOrWhiteSpace(edRoot) ? Path.Combine(edRoot, "Regions") : edRoot;
        _regionCache = JsonDataLoader.TryLoadFromEitherPath<RegionInfoData>(rtRegions, edRegions, GetStableKey());
        // Fallback: allow region JSON to live under MapData root
        if (_regionCache == null)
            _regionCache = JsonDataLoader.TryLoadFromEitherPath<RegionInfoData>(rtRoot, edRoot, GetStableKey());
        Debug.Log(_regionCache != null
            ? $"[MapPoint] Loaded RegionInfoData for '{GetStableKey()}'"
            : $"[MapPoint] Missing RegionInfoData for '{GetStableKey()}'");
        if (_regionCache != null)
        {
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(_regionCache.displayName))
                displayName = _regionCache.displayName;
            if (string.IsNullOrWhiteSpace(mainDescription) && _regionCache.main != null && !string.IsNullOrWhiteSpace(_regionCache.main.description))
                mainDescription = _regionCache.main.description;
        }
        return _regionCache;
    }

    // Back-compat alias
    public UnpopulatedInfoData GetUnpopulatedAreaData() => GetUnpopulatedInfoData(false);
}
