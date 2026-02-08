using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPanelPinToOverlay : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private Button pinButton;

    [Header("Pinned Target (Optional)")]
    [Tooltip("If set, pin will move this panel to this canvas if it is screen-space and stable.")]
    [SerializeField] private Canvas cameraCanvasOverride;

    [Tooltip("If override is empty, the script will search for a Canvas named this.")]
    [SerializeField] private string cameraCanvasName = "CameraCanvas";

    [Tooltip("If no suitable canvas is found (or it is WorldSpace), auto-create a ScreenSpaceOverlay canvas for pinned UI.")]
    [SerializeField] private bool autoCreateOverlayCanvasIfNeeded = true;

    [Tooltip("Name used when auto-creating the pinned overlay canvas.")]
    [SerializeField] private string autoCreatedOverlayCanvasName = "__PinnedOverlayCanvas";

    [Header("InfoLayer Anchor (Recommended)")]
    [Tooltip("If true, pinned position is locked relative to InfoLayer's screen-rect (i.e., the map UI bounds).")]
    [SerializeField] private bool lockToInfoLayerRect = true;

    [Tooltip("Optional override. If empty, auto-discovers a Canvas named 'InfoLayer' (case-insensitive).")]
    [SerializeField] private Canvas infoLayerCanvasOverride;

    [Tooltip("Name used for auto-discovery (case-insensitive).")]
    [SerializeField] private string infoLayerCanvasName = "InfoLayer";

    [Header("Behavior")]
    [Tooltip("When pinned, keep the panel locked to its captured anchor (no world/map coupling).")]
    [SerializeField] private bool lockToViewport = true;

    [Tooltip("When pinned, disable DraggableWindow to prevent InfoLayer clamps from fighting pinned stability.")]
    [SerializeField] private bool disableDraggableWindowWhilePinned = true;

    [Header("Optional Visual")]
    [SerializeField] private Graphic pinVisual;

    private RectTransform _rect;
    private bool _isPinned;

    private Transform _originalParent;
    private int _originalSiblingIndex;

    private Canvas _originalCanvas;
    private RectTransform _originalCanvasRect;

    private Canvas _targetCanvas;
    private RectTransform _targetCanvasRect;

    private RectTransform _infoLayerRect;

    // Normalized anchor inside either InfoLayer screen rect or full screen.
    private Vector2 _anchor01 = new Vector2(0.5f, 0.5f);

    private DraggableWindow _draggable;
    private bool _draggableWasEnabled;

    private static readonly Vector3[] s_corners = new Vector3[4];
    private const float EPS = 0.0001f;

    private void Awake()
    {
        _rect = transform as RectTransform;
        if (_rect == null)
        {
            Debug.LogError($"[{nameof(UIPanelPinToOverlay)}] Must be on a UI object with RectTransform.", this);
            enabled = false;
            return;
        }

        if (pinButton == null)
        {
            Debug.LogError($"[{nameof(UIPanelPinToOverlay)}] Pin Button reference is required.", this);
            enabled = false;
            return;
        }

        _draggable = GetComponentInParent<DraggableWindow>();

        pinButton.onClick.AddListener(TogglePin);
        RefreshVisual();

        EnsureInfoLayerRect();
        EnsureTargetCanvas();
    }

    private void OnDestroy()
    {
        if (pinButton != null)
            pinButton.onClick.RemoveListener(TogglePin);
    }

    private void LateUpdate()
    {
        if (!_isPinned || !lockToViewport)
            return;

        EnsureInfoLayerRect();
        EnsureTargetCanvas();
        if (_targetCanvasRect == null)
            return;

        Vector2 screenPoint = GetDesiredScreenPointFromAnchor();
        Camera cam = GetCanvasEventCamera(_targetCanvas);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_targetCanvasRect, screenPoint, cam, out var local))
            _rect.anchoredPosition = local;
    }

    private void TogglePin() => SetPinned(!_isPinned);

    public void SetPinned(bool pinned)
    {
        if (pinned == _isPinned)
            return;

        EnsureInfoLayerRect();
        EnsureTargetCanvas();
        if (_targetCanvasRect == null)
        {
            Debug.LogWarning($"[{nameof(UIPanelPinToOverlay)}] Cannot pin: no suitable overlay canvas found/created.");
            return;
        }

        // Capture current screen rect (pixel size + center) BEFORE we move anything.
        Canvas srcCanvas = GetComponentInParent<Canvas>();
        Vector2 screenCenter;
        Vector2 screenSize;
        CaptureScreenCenterAndSize(_rect, GetCanvasEventCamera(srcCanvas), out screenCenter, out screenSize);

        // Compute normalized anchor
        _anchor01 = ComputeAnchor01(screenCenter);

        if (pinned)
        {
            _originalParent = _rect.parent;
            _originalSiblingIndex = _rect.GetSiblingIndex();

            _originalCanvas = _originalParent != null ? _originalParent.GetComponentInParent<Canvas>() : null;
            _originalCanvasRect = _originalCanvas != null ? _originalCanvas.transform as RectTransform : null;

            if (disableDraggableWindowWhilePinned && _draggable != null)
            {
                _draggableWasEnabled = _draggable.enabled;
                _draggable.enabled = false;
            }

            // Reparent to overlay canvas
            _rect.SetParent(_targetCanvasRect, worldPositionStays: false);
            _rect.SetAsLastSibling();

            // Place at same screen center
            Camera dstCam = GetCanvasEventCamera(_targetCanvas);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_targetCanvasRect, screenCenter, dstCam, out var local))
                _rect.anchoredPosition = local;

            // Force the pinned object to match the *exact same pixel size* as before pin.
            MatchScreenSize(_rect, screenSize, dstCam);
        }
        else
        {
            // Unpin back to original parent, but KEEP the current pixel size and screen position.
            if (_originalParent != null)
            {
                _rect.SetParent(_originalParent, worldPositionStays: false);

                Canvas dstCanvas = _originalCanvas != null ? _originalCanvas : (_originalParent != null ? _originalParent.GetComponentInParent<Canvas>() : null);
                RectTransform dstCanvasRect = dstCanvas != null ? dstCanvas.transform as RectTransform : null;

                if (dstCanvasRect != null)
                {
                    Camera dstCam = GetCanvasEventCamera(dstCanvas);
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dstCanvasRect, screenCenter, dstCam, out var local))
                        _rect.anchoredPosition = local;

                    MatchScreenSize(_rect, screenSize, dstCam);

                    int clampedIndex = Mathf.Clamp(_originalSiblingIndex, 0, _originalParent.childCount - 1);
                    _rect.SetSiblingIndex(clampedIndex);
                }
            }

            if (disableDraggableWindowWhilePinned && _draggable != null)
                _draggable.enabled = _draggableWasEnabled;
        }

        _isPinned = pinned;
        RefreshVisual();
    }

    public bool IsPinned => _isPinned;

    private void RefreshVisual()
    {
        if (pinVisual == null)
            return;

        Color c = pinVisual.color;
        c.a = _isPinned ? 1f : 0.5f;
        pinVisual.color = c;
    }

    private void EnsureInfoLayerRect()
    {
        if (_infoLayerRect != null)
            return;

        Canvas chosen = null;

        if (infoLayerCanvasOverride != null)
        {
            chosen = infoLayerCanvasOverride;
        }
        else
        {
            var canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (c == null) continue;

                if (string.Equals(c.gameObject.name, infoLayerCanvasName, StringComparison.OrdinalIgnoreCase))
                {
                    chosen = c;
                    break;
                }
            }

            if (chosen == null)
            {
                var go = GameObject.Find(infoLayerCanvasName);
                if (go != null)
                    chosen = go.GetComponent<Canvas>();
            }
        }

        if (chosen != null)
            _infoLayerRect = chosen.GetComponent<RectTransform>();
    }

    private void EnsureTargetCanvas()
    {
        if (_targetCanvas != null && _targetCanvasRect != null)
            return;

        // 1) Try override or name
        Canvas candidate = cameraCanvasOverride != null ? cameraCanvasOverride : FindCanvasByName(cameraCanvasName);

        // 2) If candidate is unsuitable (WorldSpace), optionally auto-create a stable overlay canvas
        if (candidate == null || candidate.renderMode == RenderMode.WorldSpace)
        {
            if (autoCreateOverlayCanvasIfNeeded)
                candidate = GetOrCreatePinnedOverlayCanvas();
        }

        _targetCanvas = candidate;
        _targetCanvasRect = _targetCanvas != null ? _targetCanvas.transform as RectTransform : null;
    }

    private Canvas FindCanvasByName(string nm)
    {
        if (!string.IsNullOrWhiteSpace(nm))
        {
            var go = GameObject.Find(nm);
            if (go != null)
            {
                var c = go.GetComponent<Canvas>();
                if (c != null)
                    return c;
            }
        }

        var canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (c != null && string.Equals(c.name, nm, StringComparison.OrdinalIgnoreCase))
                return c;
        }

        return null;
    }

    private Canvas GetOrCreatePinnedOverlayCanvas()
    {
        // If one already exists, reuse it.
        var existing = FindCanvasByName(autoCreatedOverlayCanvasName);
        if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
            return existing;

        var go = new GameObject(autoCreatedOverlayCanvasName);
        go.layer = gameObject.layer;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000; // keep above most UI; adjust if needed

        // Optional but recommended components
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private Vector2 ComputeAnchor01(Vector2 screenCenter)
    {
        if (lockToInfoLayerRect && _infoLayerRect != null)
        {
            Rect r = GetScreenRectOfRectTransform(_infoLayerRect, GetCanvasEventCamera(_infoLayerRect.GetComponentInParent<Canvas>()));
            if (r.width > EPS && r.height > EPS)
            {
                float ax = (screenCenter.x - r.xMin) / r.width;
                float ay = (screenCenter.y - r.yMin) / r.height;
                return new Vector2(Mathf.Clamp01(ax), Mathf.Clamp01(ay));
            }
        }

        // Fallback: full screen
        float x = Screen.width > 0 ? screenCenter.x / Screen.width : 0.5f;
        float y = Screen.height > 0 ? screenCenter.y / Screen.height : 0.5f;
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    private Vector2 GetDesiredScreenPointFromAnchor()
    {
        if (lockToInfoLayerRect && _infoLayerRect != null)
        {
            Rect r = GetScreenRectOfRectTransform(_infoLayerRect, GetCanvasEventCamera(_infoLayerRect.GetComponentInParent<Canvas>()));
            if (r.width > EPS && r.height > EPS)
            {
                return new Vector2(
                    r.xMin + _anchor01.x * r.width,
                    r.yMin + _anchor01.y * r.height
                );
            }
        }

        return new Vector2(_anchor01.x * Screen.width, _anchor01.y * Screen.height);
    }

    private static Camera GetCanvasEventCamera(Canvas c)
    {
        if (c == null)
            return null;

        if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (c.worldCamera != null)
            return c.worldCamera;

        return Camera.main;
    }

    private static void CaptureScreenCenterAndSize(RectTransform rt, Camera cam, out Vector2 center, out Vector2 size)
    {
        rt.GetWorldCorners(s_corners);

        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, s_corners[i]);
            minX = Mathf.Min(minX, sp.x);
            minY = Mathf.Min(minY, sp.y);
            maxX = Mathf.Max(maxX, sp.x);
            maxY = Mathf.Max(maxY, sp.y);
        }

        center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        size = new Vector2(Mathf.Max(1f, maxX - minX), Mathf.Max(1f, maxY - minY));
    }

    private static Rect GetScreenRectOfRectTransform(RectTransform rt, Camera cam)
    {
        rt.GetWorldCorners(s_corners);

        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, s_corners[i]);
            minX = Mathf.Min(minX, sp.x);
            minY = Mathf.Min(minY, sp.y);
            maxX = Mathf.Max(maxX, sp.x);
            maxY = Mathf.Max(maxY, sp.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static void MatchScreenSize(RectTransform rt, Vector2 desiredPixelSize, Camera cam)
    {
        // Compute current pixel size
        CaptureScreenCenterAndSize(rt, cam, out _, out var current);

        float rw = desiredPixelSize.x / Mathf.Max(1f, current.x);
        float rh = desiredPixelSize.y / Mathf.Max(1f, current.y);

        // Use the smaller ratio to preserve aspect (uniform scale)
        float ratio = Mathf.Min(rw, rh);
        if (Mathf.Abs(ratio - 1f) < 0.0005f)
            return;

        Vector3 s = rt.localScale;
        rt.localScale = new Vector3(s.x * ratio, s.y * ratio, s.z);
    }
}
