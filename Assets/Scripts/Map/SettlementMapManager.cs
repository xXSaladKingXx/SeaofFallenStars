using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettlementMapManager : MonoBehaviour
{
    [Header("Core References (assign in prefab)")]
    [SerializeField] private RectTransform mapViewport;          // The “paper cutout” area
    [SerializeField] private RectTransform mapImageRect;         // RectTransform on MapImage
    [SerializeField] private Image mapImage;                     // Optional (use Image OR RawImage)
    [SerializeField] private RawImage mapRawImage;               // Optional (use Image OR RawImage)
    [SerializeField] private Transform hotspotsRoot;             // MapImage/Hotspots (recommended)
    [SerializeField] private TMP_Text titleText;                 // Optional

    [Header("Optional UI")]
    [SerializeField] private Button closeButton;                 // Optional
    [SerializeField] private Button backButton;                  // Optional
    [SerializeField] private bool closeOnEscape = true;

    [Header("Hotspot Raycast")]
    [Tooltip("Only colliders on these layers will be considered hotspots. Leave empty to hit everything.")]
    [SerializeField] private LayerMask hotspotLayerMask = ~0;

    // State
    private SettlementInfoData _data;
    private string _currentMapPath;
    private string _currentTitle;

    // For “Back”
    private struct MapState { public string path; public string title; }
    private readonly Stack<MapState> _backStack = new Stack<MapState>();

    // Texture lifetime (when loading from disk)
    private Texture2D _runtimeTexture;
    private Sprite _runtimeSprite;

    // Prefab-authored “baseline” sizing
    private Vector2 _baseMapImageSize;      // local rect size at prefab time
    private bool _capturedBaseSize;

    private void Awake()
    {
        // Auto-bind if possible (safe)
        if (mapImageRect == null)
            mapImageRect = GetComponentInChildren<RawImage>(true)?.rectTransform
                        ?? GetComponentInChildren<Image>(true)?.rectTransform;

        if (mapImage == null)
            mapImage = GetComponentInChildren<Image>(true);

        if (mapRawImage == null)
            mapRawImage = GetComponentInChildren<RawImage>(true);

        if (mapViewport == null && mapImageRect != null)
            mapViewport = mapImageRect.parent as RectTransform;

        if (hotspotsRoot == null && mapImageRect != null)
        {
            var t = mapImageRect.Find("Hotspots");
            if (t != null) hotspotsRoot = t;
        }

        // Capture baseline sizing ONCE (this is key to “don’t fight the prefab”)
        CaptureBaseMapSizeIfNeeded();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backButton != null)
            backButton.onClick.AddListener(Back);

        RefreshBackButton();
    }

    private void Update()
    {
        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    /// <summary>
    /// Called by InfoWindowManager after it spawns the Map panel.
    /// </summary>
    public void Initialize(SettlementInfoData data)
    {
        _data = data;
        _backStack.Clear();

        string title = _data != null ? _data.displayName : "Map";
        string path = _data != null ? _data.mapUrlOrPath : null;

        LoadMap(path, title, pushToBackStack: false);
    }

    /// <summary>
    /// Loads a map image into the panel and updates layout.
    /// </summary>
    public void LoadMap(string urlOrPath, string titleOverride = null, bool pushToBackStack = true)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath))
        {
            Debug.LogWarning("[SettlementMapManager] mapUrlOrPath is empty.");
            return;
        }

        // Push current onto back stack
        if (pushToBackStack && !string.IsNullOrWhiteSpace(_currentMapPath))
        {
            _backStack.Push(new MapState { path = _currentMapPath, title = _currentTitle });
        }

        _currentMapPath = urlOrPath;
        _currentTitle = string.IsNullOrWhiteSpace(titleOverride) ? "Map" : titleOverride;

        if (titleText != null)
            titleText.text = _currentTitle;

        // Load sprite/texture
        if (!MapSpriteLoader.TryLoadSprite(urlOrPath, out Sprite sprite, out Texture2D tex, out string err))
        {
            Debug.LogWarning("[SettlementMapManager] Failed to load map: " + err);
            return;
        }

        // Apply it
        ApplyLoadedSprite(sprite, tex);

        // Fit ONLY the map image area to the viewport, preserving aspect ratio
        FitMapImageToViewport();

        RefreshBackButton();
    }

    /// <summary>
    /// Called by the click-catcher when the user clicks on the map image area.
    /// Uses Physics2D overlap against PolygonCollider2D hotspots.
    /// </summary>
    public void HandleMapClick(Vector2 screenPos, Camera eventCamera)
    {
        if (mapImageRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(mapImageRect, screenPos, eventCamera, out Vector3 world))
            return;

        // Physics2D ignores Z; use x/y
        Vector2 p = new Vector2(world.x, world.y);

        Collider2D hit = Physics2D.OverlapPoint(p, hotspotLayerMask);
        if (hit == null)
            return;

        var region = hit.GetComponentInParent<SettlementLocalRegion>();
        if (region == null)
            return;

        // Default: if a hotspot defines a sub-map, load it. Otherwise just log it for now.
        if (!string.IsNullOrWhiteSpace(region.subMapUrlOrPath))
        {
            string title = string.IsNullOrWhiteSpace(region.displayName) ? "Sub-Map" : region.displayName;
            LoadMap(region.subMapUrlOrPath, title, pushToBackStack: true);
        }
        else
        {
            Debug.Log($"[SettlementMapManager] Hotspot clicked: {region.displayName} ({region.regionId})");
        }
    }

    private void ApplyLoadedSprite(Sprite sprite, Texture2D tex)
    {
        // Clean up old runtime assets (disk-loaded)
        CleanupRuntimeAssets();

        _runtimeSprite = sprite;
        _runtimeTexture = tex;

        if (mapImage != null)
        {
            mapImage.sprite = sprite;
            mapImage.preserveAspect = true; // helps if you temporarily disable fitting
        }

        if (mapRawImage != null)
        {
            mapRawImage.texture = tex != null ? tex : (sprite != null ? sprite.texture : null);
        }
    }

    private void FitMapImageToViewport()
    {
        if (mapViewport == null || mapImageRect == null)
            return;

        CaptureBaseMapSizeIfNeeded();

        // Determine sprite aspect
        float aspect = GetCurrentMapAspect();
        if (aspect <= 0.0001f)
            return;

        // Fit INSIDE viewport rect, preserving aspect.
        // This only changes the MapImage rect. It does NOT resize your whole window.
        Vector2 vp = mapViewport.rect.size;
        if (vp.x <= 1f || vp.y <= 1f)
            return;

        float vpAspect = vp.x / vp.y;

        float w, h;
        if (vpAspect >= aspect)
        {
            // viewport is wider than image -> height-bound
            h = vp.y;
            w = h * aspect;
        }
        else
        {
            // viewport is taller than image -> width-bound
            w = vp.x;
            h = w / aspect;
        }

        // Use the computed size as the MapImageRect sizeDelta while keeping anchors centered
        mapImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapImageRect.pivot = new Vector2(0.5f, 0.5f);
        mapImageRect.anchoredPosition = Vector2.zero;
        mapImageRect.sizeDelta = new Vector2(w, h);

        // Scale hotspots relative to the prefab baseline size (so your collider editing workflow is stable)
        if (hotspotsRoot != null && _capturedBaseSize && _baseMapImageSize.x > 0.001f && _baseMapImageSize.y > 0.001f)
        {
            float sx = w / _baseMapImageSize.x;
            float sy = h / _baseMapImageSize.y;
            hotspotsRoot.localScale = new Vector3(sx, sy, 1f);
        }
    }

    private float GetCurrentMapAspect()
    {
        // Prefer sprite aspect if using Image; otherwise texture aspect
        if (mapImage != null && mapImage.sprite != null && mapImage.sprite.texture != null)
        {
            var t = mapImage.sprite.texture;
            return (float)t.width / t.height;
        }

        if (mapRawImage != null && mapRawImage.texture != null)
        {
            return (float)mapRawImage.texture.width / mapRawImage.texture.height;
        }

        // Fallback to runtime texture if present
        if (_runtimeTexture != null)
            return (float)_runtimeTexture.width / _runtimeTexture.height;

        return 0f;
    }

    private void CaptureBaseMapSizeIfNeeded()
    {
        if (_capturedBaseSize || mapImageRect == null)
            return;

        // Baseline is the prefab-authored rect size (local)
        _baseMapImageSize = mapImageRect.rect.size;

        // If rect is 0 (rare), fall back to sizeDelta
        if (_baseMapImageSize.sqrMagnitude < 0.01f)
            _baseMapImageSize = mapImageRect.sizeDelta;

        _capturedBaseSize = _baseMapImageSize.sqrMagnitude > 0.01f;
    }

    private void Back()
    {
        if (_backStack.Count == 0)
            return;

        var prev = _backStack.Pop();
        LoadMap(prev.path, prev.title, pushToBackStack: false);
    }

    private void RefreshBackButton()
    {
        if (backButton != null)
            backButton.gameObject.SetActive(_backStack.Count > 0);
    }

    private void Close()
    {
        CleanupRuntimeAssets();
        Destroy(gameObject);
    }

    private void CleanupRuntimeAssets()
    {
        // Only destroy textures/sprites we created at runtime from disk
        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }

        if (_runtimeTexture != null)
        {
            Destroy(_runtimeTexture);
            _runtimeTexture = null;
        }
    }
}
