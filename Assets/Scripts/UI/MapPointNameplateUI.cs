using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapPointNameplateUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private RectTransform backgroundRect;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image heraldryImage;
    [SerializeField] private TMP_Text nameText;

    [Header("Sizing")]
    [Tooltip("Horizontal padding (left+right) and vertical padding (top+bottom) inside the background.")]
    [SerializeField] private Vector2 padding = new Vector2(18f, 10f);

    [Tooltip("Heraldry icon size (pixels).")]
    [SerializeField] private float iconSize = 32f;

    [Tooltip("Horizontal gap between icon and text.")]
    [SerializeField] private float iconTextSpacing = 10f;

    [Tooltip("Optional minimum background width (pixels).")]
    [SerializeField] private float minWidth = 120f;

    [Tooltip("Optional minimum background height (pixels).")]
    [SerializeField] private float minHeight = 44f;

    [Tooltip("Maximum text width used for sizing calculations (prevents runaway nameplates).")]
    [SerializeField] private float maxTextWidth = 900f;

    [Header("Behavior")]
    [SerializeField] private bool hideIconIfMissing = true;

    private Canvas _canvas;


    // Cached initial scale so layer scaling composes correctly with any parent scaling (e.g., camera zoom / canvas scaling).
    private bool _initialScaleCaptured;
    private Vector3 _initialLocalScale = Vector3.one;
    private void Awake()
    {
        // Capture the prefab's initial scale once (allows SetLayerBaseScale to remain multiplicative).
        if (!_initialScaleCaptured)
        {
            _initialLocalScale = transform.localScale;
            if (_initialLocalScale == Vector3.zero) _initialLocalScale = Vector3.one;
            _initialScaleCaptured = true;
        }
    }


    private void Reset()
    {
        AutoWire();
    }

    private void OnValidate()
    {
        // Keep it robust even if prefab wiring drifts.
        AutoWire();
    }

    private void AutoWire()
    {
        if (rootRect == null) rootRect = transform as RectTransform;
        if (backgroundRect == null) backgroundRect = GetComponent<RectTransform>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(true);

        if (heraldryImage == null)
            heraldryImage = FindBestHeraldryImage();
        else
        {
            // If someone accidentally wired the background panel as "heraldryImage",
            // try to recover automatically.
            if (backgroundImage != null && heraldryImage == backgroundImage)
                heraldryImage = FindBestHeraldryImage();
        }
    }

    private Image FindBestHeraldryImage()
    {
        // Prefer an Image that is NOT the background and is NOT a parent of the TMP text.
        var imgs = GetComponentsInChildren<Image>(true);
        Image best = null;

        for (int i = 0; i < imgs.Length; i++)
        {
            var img = imgs[i];
            if (img == null) continue;
            if (backgroundImage != null && img == backgroundImage) continue;

            // If the text is under this image, it's likely a panel container (avoid).
            if (nameText != null && nameText.transform != null && nameText.transform.IsChildOf(img.transform))
                continue;

            best = img;
            break;
        }

        // Fallback: first Image that isn't background.
        if (best == null)
        {
            for (int i = 0; i < imgs.Length; i++)
            {
                var img = imgs[i];
                if (img == null) continue;
                if (backgroundImage != null && img == backgroundImage) continue;
                best = img;
                break;
            }
        }

        return best;
    }

    public void BindCanvas(Canvas canvas)
    {
        _canvas = canvas;
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
    }

    public void Show()
    {
        // IMPORTANT:
        // Do NOT toggle child panel GameObjects here; keep Show/Hide at the root.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    public void SetContent(string displayName, Sprite heraldry)
    {
        if (nameText != null)
            nameText.text = displayName ?? string.Empty;

        // The bug you hit:
        // If "heraldryImage" was accidentally wired to a PANEL (FramePanel),
        // calling SetActive(false) on it disables the entire nameplate visuals.
        //
        // Fix:
        // Never SetActive() here. Only enable/disable the Image component.
        if (heraldryImage != null)
        {
            bool has = heraldry != null;
            heraldryImage.sprite = heraldry;

            if (hideIconIfMissing)
                heraldryImage.enabled = has;
            else
                heraldryImage.enabled = true;

            // Some prefabs have an extra container GameObject for the icon.
            // If we can safely find a direct parent container that ONLY holds the icon,
            // keep it on; do not disable arbitrary panels.
        }

        ResizeToContent();
    }

    // Screen-space canvas: pass SCREEN coords.
    // World-space canvas: pass WORLD coords.
    public void SetScreenPosition(Vector2 screenPoint)
    {
        if (rootRect == null)
            rootRect = transform as RectTransform;

        if (rootRect == null)
            return;

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (_canvas == null)
        {
            rootRect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
            return;
        }

        var canvasRect = _canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            rootRect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
            return;
        }

        Camera uiCam = null;
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = _canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCam, out Vector2 localPoint))
            rootRect.anchoredPosition = localPoint;
        else
            rootRect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
    }

    // Overload for World Space convenience (MapManager sometimes passes a Vector3).
    public void SetScreenPosition(Vector3 worldPointOrScreenPoint)
    {
        // If this canvas is world-space, treat as world position.
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

        if (_canvas != null && _canvas.renderMode == RenderMode.WorldSpace)
        {
            if (rootRect == null) rootRect = transform as RectTransform;
            if (rootRect != null) rootRect.position = worldPointOrScreenPoint;
            return;
        }

        // Otherwise treat XY as screen coords.
        SetScreenPosition(new Vector2(worldPointOrScreenPoint.x, worldPointOrScreenPoint.y));
    }

    private void ResizeToContent()
    {
        if (backgroundRect == null)
            backgroundRect = GetComponent<RectTransform>();

        if (backgroundRect == null || nameText == null)
            return;

        bool iconActive = (heraldryImage != null) && heraldryImage.enabled;

        string s = nameText.text ?? string.Empty;
        Vector2 pref = nameText.GetPreferredValues(s);

        float textW = Mathf.Min(pref.x, maxTextWidth);
        float textH = pref.y;

        float w = padding.x * 2f + textW;
        float h = padding.y * 2f + textH;

        if (iconActive)
        {
            w += iconSize + iconTextSpacing;
            h = Mathf.Max(h, padding.y * 2f + iconSize);

            var irt = heraldryImage.rectTransform;
            irt.sizeDelta = new Vector2(iconSize, iconSize);
        }

        w = Mathf.Max(minWidth, w);
        h = Mathf.Max(minHeight, h);

        backgroundRect.sizeDelta = new Vector2(w, h);
    }

    // Helper for coordinator (does NOT mutate anything).
    public RectTransform GetRectTransform()
    {
        return transform as RectTransform;
    }

    public void SetLayerBaseScale(float scale)
    {
        if (scale <= 0f) scale = 1f;

        if (!_initialScaleCaptured)
        {
            _initialLocalScale = transform.localScale;
            if (_initialLocalScale == Vector3.zero) _initialLocalScale = Vector3.one;
            _initialScaleCaptured = true;
        }

        transform.localScale = _initialLocalScale * scale;
    }
}
