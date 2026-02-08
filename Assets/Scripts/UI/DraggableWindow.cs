using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableWindow : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Target (Window Root RectTransform)")]
    [SerializeField] private RectTransform dragTarget;

    [Header("Optional: Canvas override (World Space support)")]
    [SerializeField] private Canvas parentCanvas;

    [Header("Resize (Diagonal / Proportional)")]
    [Tooltip("A small UI element (usually bottom-right) used as the resize handle.")]
    [SerializeField] private RectTransform resizeHandle;

    [Tooltip("Uniform scale clamp used by manual resizing.")]
    [SerializeField] private float minUniformScale = 0.60f;

    [Tooltip("Uniform scale clamp used by manual resizing.")]
    [SerializeField] private float maxUniformScale = 2.00f;

    [Tooltip("How many pixels of diagonal drag roughly equals +1.0 scale multiplier.")]
    [SerializeField] private float pixelsPerScaleMultiplier = 10f;

    [Header("Resize Pivot (Recommended)")]
    [Tooltip("If true, forces the window pivot so resizing from the handle feels natural (grows down/right).")]
    [SerializeField] private bool forceResizePivot = true;

    [Tooltip("Common choice for a bottom-right handle is Top-Left pivot (0,1) so growth is down/right.")]
    [SerializeField] private Vector2 resizePivot = new Vector2(0f, 1f);

    [Header("InfoLayer Auto-Fit + Constraints (uses existing bool)")]
    [SerializeField] private bool trySizeBasedOnCurrentCameraOrthoSize = false;

    [Tooltip("Optional override. If empty, auto-discovers a Canvas named 'InfoLayer' (case-insensitive).")]
    [SerializeField] private Canvas infoLayerCanvasOverride;

    [Tooltip("Name used for auto-discovery (case-insensitive).")]
    [SerializeField] private string infoLayerCanvasName = "InfoLayer";

    [Tooltip("If true, movement/resizing is clamped so the window stays fully inside InfoLayer.")]
    [SerializeField] private bool constrainToInfoLayerBounds = true;

    [Tooltip("When auto-fitting, the window will fit within this fraction of InfoLayer size (e.g. 0.92 = 92% of width/height).")]
    [Range(0.50f, 1.00f)]
    [SerializeField] private float autoFitPaddingFraction = 0.92f;

    [Header("Default Size (NEW)")]
    [Tooltip("When auto-fit is enabled, apply this ONCE on initialization to make the window bigger/smaller relative to the InfoLayer.\n" +
             "1.00 = unchanged. < 1 makes it smaller. > 1 makes it larger (still clamped to fit InfoLayer).")]
    [Range(0.25f, 2.00f)]
    [SerializeField] private float defaultSizeMultiplierRelativeToInfoLayer = 1.00f;

    [Header("Init Stabilization (NEW)")]
    [Tooltip("If enabled, after first-fit, waits until InfoLayer stops changing (e.g. zoom animation completes) then clamps position/scale once.")]
    [SerializeField] private bool deferFinalClampUntilInfoLayerSettles = true;

    [SerializeField] private float infoLayerSettleDelaySeconds = 0.10f;
    [SerializeField] private float infoLayerSettleTimeoutSeconds = 2.00f;

    private RectTransform infoLayerRect;

    private enum DragMode { Move, Resize }
    private DragMode mode = DragMode.Move;

    private Vector2 moveOffsetInParent;
    private Vector2 resizeStartPointerLocal;
    private float resizeStartScale;

    private bool didInitialAutoFit;
    private bool didApplyDefaultMultiplier;
    private Coroutine deferredClampRoutine;

    private static readonly Vector3[] s_worldCorners = new Vector3[4];
    private const float EPS = 0.0001f;

    private void Awake()
    {
        if (dragTarget == null)
            dragTarget = GetComponentInParent<RectTransform>();

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        EnsureInfoLayerFound();
        TryInitialAutoFitIfNeeded();
        StartDeferredFinalClampIfNeeded();
    }

    private void OnEnable()
    {
        EnsureInfoLayerFound();
        TryInitialAutoFitIfNeeded();
        StartDeferredFinalClampIfNeeded();
    }

    public void Configure(RectTransform target, Canvas canvas)
    {
        dragTarget = target;
        parentCanvas = canvas;

        EnsureInfoLayerFound();
        didInitialAutoFit = false;
        didApplyDefaultMultiplier = false;

        TryInitialAutoFitIfNeeded();
        StartDeferredFinalClampIfNeeded();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (dragTarget != null)
            dragTarget.SetAsLastSibling();

        mode = IsResizePress(eventData) ? DragMode.Resize : DragMode.Move;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragTarget == null || dragTarget.parent == null)
            return;

        RectTransform parentRect = dragTarget.parent as RectTransform;
        if (parentRect == null)
            return;

        Camera cam = ResolveEventCamera(eventData);

        if (mode == DragMode.Move)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, cam, out Vector2 pointerLocalInParent))
            {
                moveOffsetInParent = (Vector2)dragTarget.localPosition - pointerLocalInParent;
            }
        }
        else // Resize
        {
            if (forceResizePivot)
                SetPivotWithoutMoving(dragTarget, resizePivot);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, cam, out Vector2 pointerLocalInParent))
                return;

            resizeStartPointerLocal = pointerLocalInParent;
            resizeStartScale = dragTarget.localScale.x; // uniform
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragTarget == null || dragTarget.parent == null)
            return;

        RectTransform parentRect = dragTarget.parent as RectTransform;
        if (parentRect == null)
            return;

        Camera cam = ResolveEventCamera(eventData);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, cam, out Vector2 pointerLocalInParent))
            return;

        if (mode == DragMode.Move)
        {
            dragTarget.localPosition = pointerLocalInParent + moveOffsetInParent;

            if (trySizeBasedOnCurrentCameraOrthoSize && constrainToInfoLayerBounds && !IsPinned())
                ClampPositionToInfoLayer();
        }
        else // Resize
        {
            Vector2 delta = pointerLocalInParent - resizeStartPointerLocal;

            float diagonalPixels = delta.x + (-delta.y);
            float multiplier = 1f + (diagonalPixels / Mathf.Max(1f, pixelsPerScaleMultiplier));

            float raw = resizeStartScale * multiplier;
            float newScale = Mathf.Clamp(raw, minUniformScale, maxUniformScale);
            SetUniformScale(newScale);

            if (trySizeBasedOnCurrentCameraOrthoSize && constrainToInfoLayerBounds && !IsPinned())
            {
                ClampScaleToFitInfoLayer();
                ClampPositionToInfoLayer();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        mode = DragMode.Move;

        if (trySizeBasedOnCurrentCameraOrthoSize && constrainToInfoLayerBounds && !IsPinned())
        {
            ClampScaleToFitInfoLayer();
            ClampPositionToInfoLayer();
        }
    }

    private bool IsPinned()
    {
        var p = GetComponentInParent<UIPanelPinToOverlay>();
        return p != null && p.IsPinned;
    }

    // -------------------- One-Time Fit --------------------

    private void EnsureInfoLayerFound()
    {
        if (infoLayerRect != null)
            return;

        Canvas chosen = null;

        if (infoLayerCanvasOverride != null)
        {
            chosen = infoLayerCanvasOverride;
        }
        else
        {
            var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
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
            infoLayerRect = chosen.GetComponent<RectTransform>();
    }

    private void TryInitialAutoFitIfNeeded()
    {
        if (!trySizeBasedOnCurrentCameraOrthoSize)
            return;

        if (didInitialAutoFit)
            return;

        if (IsPinned())
            return;

        if (dragTarget == null)
            return;

        EnsureInfoLayerFound();
        if (infoLayerRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        Rect infoRect = infoLayerRect.rect;
        if (infoRect.width <= EPS || infoRect.height <= EPS)
            return;

        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(infoLayerRect, dragTarget);
        float windowW = b.size.x;
        float windowH = b.size.y;

        float maxAllowedW = infoRect.width * Mathf.Clamp01(autoFitPaddingFraction);
        float maxAllowedH = infoRect.height * Mathf.Clamp01(autoFitPaddingFraction);

        bool oversized = windowW > maxAllowedW + EPS || windowH > maxAllowedH + EPS;

        if (oversized)
        {
            float ratioW = maxAllowedW / Mathf.Max(EPS, windowW);
            float ratioH = maxAllowedH / Mathf.Max(EPS, windowH);
            float ratio = Mathf.Min(ratioW, ratioH);

            float currentScale = Mathf.Max(EPS, dragTarget.localScale.x);
            float newScale = Mathf.Clamp(currentScale * ratio, minUniformScale, maxUniformScale);
            SetUniformScale(newScale);
        }

        ApplyDefaultSizeMultiplierOnce();

        didInitialAutoFit = true;

        if (constrainToInfoLayerBounds)
        {
            ClampScaleToFitInfoLayer();
            ClampPositionToInfoLayer();
        }
    }

    private void ApplyDefaultSizeMultiplierOnce()
    {
        if (didApplyDefaultMultiplier)
            return;

        float mult = Mathf.Clamp(defaultSizeMultiplierRelativeToInfoLayer, 0.25f, 2.0f);
        if (Mathf.Abs(mult - 1f) < 0.0005f)
        {
            didApplyDefaultMultiplier = true;
            return;
        }

        float s = Mathf.Max(EPS, dragTarget.localScale.x);
        float desired = Mathf.Clamp(s * mult, minUniformScale, maxUniformScale);
        SetUniformScale(desired);

        if (constrainToInfoLayerBounds)
        {
            ClampScaleToFitInfoLayer();
            ClampPositionToInfoLayer();
        }

        didApplyDefaultMultiplier = true;
    }

    private void StartDeferredFinalClampIfNeeded()
    {
        if (!trySizeBasedOnCurrentCameraOrthoSize || !constrainToInfoLayerBounds || !deferFinalClampUntilInfoLayerSettles)
            return;

        if (IsPinned())
            return;

        if (deferredClampRoutine != null)
            return;

        deferredClampRoutine = StartCoroutine(DeferredFinalClampRoutine());
    }

    private IEnumerator DeferredFinalClampRoutine()
    {
        // Let layout and any spawn positioning complete.
        yield return new WaitForEndOfFrame();

        EnsureInfoLayerFound();
        if (infoLayerRect == null || dragTarget == null)
        {
            deferredClampRoutine = null;
            yield break;
        }

        float stableFor = 0f;
        float elapsed = 0f;

        Vector3[] prevCorners = new Vector3[4];
        infoLayerRect.GetWorldCorners(prevCorners);

        while (elapsed < Mathf.Max(0.05f, infoLayerSettleTimeoutSeconds))
        {
            yield return null;

            elapsed += Time.unscaledDeltaTime;

            Vector3[] curCorners = new Vector3[4];
            infoLayerRect.GetWorldCorners(curCorners);

            float maxDelta = 0f;
            for (int i = 0; i < 4; i++)
                maxDelta = Mathf.Max(maxDelta, (curCorners[i] - prevCorners[i]).magnitude);

            bool stable = maxDelta < 0.01f; // screen/world jitter tolerance

            if (stable)
                stableFor += Time.unscaledDeltaTime;
            else
                stableFor = 0f;

            prevCorners = curCorners;

            if (stableFor >= Mathf.Max(0.01f, infoLayerSettleDelaySeconds))
                break;
        }

        // Re-attempt initial fit if it couldn't happen earlier (e.g., InfoLayer rect was 0 in Awake)
        if (!didInitialAutoFit)
            TryInitialAutoFitIfNeeded();

        if (!IsPinned())
        {
            ClampScaleToFitInfoLayer();
            ClampPositionToInfoLayer();
        }

        deferredClampRoutine = null;
    }

    // -------------------- InfoLayer Constraints --------------------

    private void ClampPositionToInfoLayer()
    {
        if (infoLayerRect == null || dragTarget == null || dragTarget.parent == null)
            return;

        Rect infoRect = infoLayerRect.rect;
        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(infoLayerRect, dragTarget);

        float dx = 0f;
        if (b.min.x < infoRect.xMin) dx = infoRect.xMin - b.min.x;
        else if (b.max.x > infoRect.xMax) dx = infoRect.xMax - b.max.x;

        float dy = 0f;
        if (b.min.y < infoRect.yMin) dy = infoRect.yMin - b.min.y;
        else if (b.max.y > infoRect.yMax) dy = infoRect.yMax - b.max.y;

        if (Mathf.Abs(dx) < EPS && Mathf.Abs(dy) < EPS)
            return;

        Vector3 worldDelta = infoLayerRect.TransformVector(new Vector3(dx, dy, 0f));

        RectTransform parentRect = dragTarget.parent as RectTransform;
        if (parentRect == null)
            return;

        Vector3 parentDelta = parentRect.InverseTransformVector(worldDelta);
        dragTarget.localPosition += parentDelta;
    }

    private void ClampScaleToFitInfoLayer()
    {
        if (infoLayerRect == null || dragTarget == null)
            return;

        float maxFit = ComputeMaxUniformScaleToFitInfoLayer();
        if (maxFit <= EPS)
            return;

        float current = dragTarget.localScale.x;
        if (current > maxFit + 0.0001f)
            SetUniformScale(maxFit);
    }

    private float ComputeMaxUniformScaleToFitInfoLayer()
    {
        if (infoLayerRect == null || dragTarget == null)
            return 0f;

        float currentScale = Mathf.Max(EPS, dragTarget.localScale.x);

        Rect infoRect = infoLayerRect.rect;
        if (infoRect.width <= EPS || infoRect.height <= EPS)
            return currentScale;

        Vector3 pivotWorld = dragTarget.TransformPoint(Vector3.zero);
        Vector3 pivotLocal = infoLayerRect.InverseTransformPoint(pivotWorld);

        dragTarget.GetWorldCorners(s_worldCorners);

        float ratioMin = 0f;
        float ratioMax = float.PositiveInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector3 vWorld = s_worldCorners[i] - pivotWorld;
            Vector3 vLocal = infoLayerRect.InverseTransformVector(vWorld);

            if (Mathf.Abs(vLocal.x) >= EPS)
            {
                float t1 = (infoRect.xMin - pivotLocal.x) / vLocal.x;
                float t2 = (infoRect.xMax - pivotLocal.x) / vLocal.x;
                float lo = Mathf.Min(t1, t2);
                float hi = Mathf.Max(t1, t2);
                ratioMin = Mathf.Max(ratioMin, lo);
                ratioMax = Mathf.Min(ratioMax, hi);
            }

            if (Mathf.Abs(vLocal.y) >= EPS)
            {
                float t1 = (infoRect.yMin - pivotLocal.y) / vLocal.y;
                float t2 = (infoRect.yMax - pivotLocal.y) / vLocal.y;
                float lo = Mathf.Min(t1, t2);
                float hi = Mathf.Max(t1, t2);
                ratioMin = Mathf.Max(ratioMin, lo);
                ratioMax = Mathf.Min(ratioMax, hi);
            }
        }

        ratioMin = Mathf.Max(0f, ratioMin);
        if (ratioMax < ratioMin)
            return 0f;

        float maxScale = currentScale * ratioMax;
        return Mathf.Max(0f, maxScale);
    }

    private void SetUniformScale(float s)
    {
        s = Mathf.Max(0.0001f, s);
        dragTarget.localScale = new Vector3(s, s, 1f);
    }

    // -------------------- Input / Utility --------------------

    private bool IsResizePress(PointerEventData eventData)
    {
        if (resizeHandle == null || eventData == null)
            return false;

        GameObject hit = eventData.pointerPressRaycast.gameObject != null
            ? eventData.pointerPressRaycast.gameObject
            : eventData.pointerCurrentRaycast.gameObject;

        if (hit == null)
            return false;

        return hit.transform == resizeHandle || hit.transform.IsChildOf(resizeHandle);
    }

    private Camera ResolveEventCamera(PointerEventData eventData)
    {
        if (eventData != null && eventData.pressEventCamera != null)
            return eventData.pressEventCamera;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            if (parentCanvas.worldCamera != null)
                return parentCanvas.worldCamera;

            if (Camera.main != null)
                return Camera.main;
        }

        return null; // overlay
    }

    private static void SetPivotWithoutMoving(RectTransform rt, Vector2 newPivot)
    {
        if (rt == null) return;

        Vector2 size = rt.rect.size;
        Vector2 deltaPivot = rt.pivot - newPivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x * rt.localScale.x,
                                           deltaPivot.y * size.y * rt.localScale.y,
                                           0f);

        rt.pivot = newPivot;
        rt.localPosition -= deltaPosition;
    }
}
