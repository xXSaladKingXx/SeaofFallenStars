using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPOverflowToScrollRect : MonoBehaviour
{
    [Header("Build")]
    [Tooltip("If true, will build wrapper automatically, but ONLY during Play Mode.")]
    [SerializeField] private bool autoBuildAtRuntime = true;

    [Header("Scrolling")]
    [SerializeField] private bool onlyEnableWhenOverflowing = true;
    [SerializeField] private bool keepPinnedToTopOnTextChange = true;
    [SerializeField] private bool enableMouseWheel = true;
    [SerializeField] private float scrollSensitivity = 25f;

    [Tooltip("Padding inside the viewport. X = left+right, Y = top+bottom.")]
    [SerializeField] private Vector2 padding = Vector2.zero;

    [Header("MapInputBlocker Integration")]
    [Tooltip("Drag your MapInputBlocker here. If empty, the script will try to find one at runtime.")]
    [SerializeField] private MonoBehaviour mapInputBlocker;

    [Tooltip("If true, blocks map zoom/pan while pointer is over this scroll viewport and when scrolling.")]
    [SerializeField] private bool blockMapInput = true;

    [Tooltip("Unblock when pointer exits the viewport.")]
    [SerializeField] private bool unblockOnExit = true;

    private TextMeshProUGUI tmp;
    private ScrollRect scrollRect;
    private RectTransform viewportRT;
    private RectTransform contentRT;

    private string lastText;
    private Coroutine refreshCo;

    // Reflection cache for MapInputBlocker calls (keeps this compatible with your existing class)
    private MethodInfo cachedBoolSetter;
    private MethodInfo cachedPush;
    private MethodInfo cachedPop;
    private PropertyInfo cachedBoolProperty;
    private FieldInfo cachedBoolField;

    // Track block state for this region (avoids spam)
    private bool regionIsBlocking;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        if (!Application.isPlaying) return;

        EnsureBuiltOrWired(autoBuildAtRuntime);
        EnsureMapBlockerWired();

        // Defer sizing until UI/layout has valid rects
        refreshCo = StartCoroutine(DeferredRefresh());

        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMPTextChanged);
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMPTextChanged);

        if (refreshCo != null)
        {
            StopCoroutine(refreshCo);
            refreshCo = null;
        }

        if (blockMapInput && unblockOnExit)
            SetMapBlocked(false);
    }

    private void OnTMPTextChanged(UnityEngine.Object changed)
    {
        if (changed != tmp) return;

        if (refreshCo != null) StopCoroutine(refreshCo);
        refreshCo = StartCoroutine(DeferredRefresh());
    }

    private IEnumerator DeferredRefresh()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        RefreshLayout(forceRecalc: true);

        // Retry a few times if rects were 0 because of layout groups / animated windows
        for (int i = 0; i < 3; i++)
        {
            if (viewportRT != null && viewportRT.rect.width > 1f && viewportRT.rect.height > 1f)
                break;

            yield return null;
            yield return new WaitForEndOfFrame();
            RefreshLayout(forceRecalc: true);
        }

        refreshCo = null;
    }

    private void EnsureBuiltOrWired(bool allowBuild)
    {
        // Try to find existing ScrollRect in parents
        scrollRect = GetComponentInParent<ScrollRect>(includeInactive: true);

        if (scrollRect == null)
        {
            if (!allowBuild) return;
            BuildWrapper();
        }

        if (scrollRect == null) return;

        // Ensure viewport/content are wired
        viewportRT = scrollRect.viewport;
        if (viewportRT == null)
        {
            var vp = scrollRect.transform.Find("Viewport");
            if (vp) viewportRT = vp as RectTransform;
        }

        contentRT = scrollRect.content;
        if (contentRT == null && viewportRT != null)
        {
            var c = viewportRT.Find("Content");
            if (c) contentRT = c as RectTransform;
        }

        if (viewportRT == null || contentRT == null) return;

        // Ensure TMP is a child of Content
        if (tmp.transform.parent != contentRT)
            tmp.transform.SetParent(contentRT, worldPositionStays: false);

        ConfigureTMP();
        ConfigureScrollRect();

        // Ensure viewport can receive pointer + scroll events
        EnsureViewportRaycastTarget();
    }

    private void ConfigureScrollRect()
    {
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = enableMouseWheel ? scrollSensitivity : 0f;

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;

        // Leave enabled here; overflow gating happens in RefreshLayout after valid measurement
        scrollRect.enabled = true;
    }

    private void ConfigureTMP()
    {
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private void EnsureViewportRaycastTarget()
    {
        if (viewportRT == null) return;
        var img = viewportRT.GetComponent<Image>();
        if (img == null) img = viewportRT.gameObject.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0);
        img.raycastTarget = true;

        // Prefer RectMask2D for clipping stability
        if (viewportRT.GetComponent<RectMask2D>() == null)
            viewportRT.gameObject.AddComponent<RectMask2D>();
    }

    private void EnsureMapBlockerWired()
    {
        if (!blockMapInput) return;
        if (viewportRT == null) return;

        if (mapInputBlocker == null)
            mapInputBlocker = FindMapInputBlockerInScene();

        CacheMapBlockerMembers();

        // Add internal region hook to the Viewport so pointer enter/exit works reliably
        var region = viewportRT.GetComponent<ViewportRegionHook>();
        if (region == null) region = viewportRT.gameObject.AddComponent<ViewportRegionHook>();
        region.Owner = this;
        region.UnblockOnExit = unblockOnExit;
    }

    private MonoBehaviour FindMapInputBlockerInScene()
    {
        // Unity 6 / newer: FindObjectsByType supports includeInactive + sort mode
#if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
        var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
    // Older versions: includeInactive bool overload (if available) or fallback to active-only.
    var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif

        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;

            var typeName = mb.GetType().Name;
            if (string.Equals(typeName, "MapInputBlocker", StringComparison.Ordinal) ||
                typeName.IndexOf("MapInputBlocker", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return mb;
            }
        }

        return null;
    }


    public void RefreshLayout(bool forceRecalc)
    {
        if (!Application.isPlaying) return;
        if (scrollRect == null || viewportRT == null || contentRT == null || tmp == null) return;

        Canvas.ForceUpdateCanvases();

        float vw = viewportRT.rect.width;
        float vh = viewportRT.rect.height;

        // If invalid, bail; deferred refresh will retry
        if (vw <= 1f || vh <= 1f) return;

        vw = Mathf.Max(0f, vw - padding.x);
        vh = Mathf.Max(0f, vh - padding.y);

        if (forceRecalc)
            tmp.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);

        Vector2 preferred = tmp.GetPreferredValues(tmp.text, vw, Mathf.Infinity);
        float preferredHeight = Mathf.Max(0f, preferred.y);

        // Size content so it can scroll
        var sd = contentRT.sizeDelta;
        contentRT.sizeDelta = new Vector2(sd.x, preferredHeight);

        bool overflowing = preferredHeight > vh + 0.5f;

        if (onlyEnableWhenOverflowing)
            scrollRect.enabled = overflowing;
        else
            scrollRect.enabled = true;

        if (!overflowing)
        {
            contentRT.anchoredPosition = Vector2.zero;
        }
        else if (keepPinnedToTopOnTextChange && tmp.text != lastText)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        lastText = tmp.text;
    }

    [ContextMenu("Build Scroll Wrapper (RectMask2D)")]
    private void BuildWrapper()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        var textRT = tmp.rectTransform;

        var oldParent = textRT.parent;
        int siblingIndex = textRT.GetSiblingIndex();

        // Root ScrollRect
        GameObject scrollGO = new GameObject($"{gameObject.name}_ScrollRect", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(oldParent, worldPositionStays: false);
        scrollGO.transform.SetSiblingIndex(siblingIndex);

        var scrollRootRT = scrollGO.GetComponent<RectTransform>();
        CopyRectTransformLayout(textRT, scrollRootRT);

        scrollRect = scrollGO.GetComponent<ScrollRect>();

        // Viewport
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform));
        viewportGO.transform.SetParent(scrollGO.transform, worldPositionStays: false);
        viewportRT = viewportGO.GetComponent<RectTransform>();
        StretchToParent(viewportRT);

        // Content
        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, worldPositionStays: false);
        contentRT = contentGO.GetComponent<RectTransform>();
        ConfigureContentRect(contentRT);

        // Move TMP under Content
        tmp.transform.SetParent(contentGO.transform, worldPositionStays: false);

        ConfigureTMP();
        ConfigureScrollRect();
        EnsureViewportRaycastTarget();

        // If we’re in play mode, wire blocker hook too
        if (Application.isPlaying)
            EnsureMapBlockerWired();
    }

    private static void ConfigureContentRect(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
    }

    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }

    private static void CopyRectTransformLayout(RectTransform src, RectTransform dst)
    {
        dst.anchorMin = src.anchorMin;
        dst.anchorMax = src.anchorMax;
        dst.pivot = src.pivot;
        dst.anchoredPosition = src.anchoredPosition;
        dst.sizeDelta = src.sizeDelta;
        dst.localRotation = src.localRotation;
        dst.localScale = src.localScale;
    }

    // ---------------------------
    // MapInputBlocker integration
    // ---------------------------

    private static bool FindMapInputBlockerPredicate(MonoBehaviour mb)
    {
        if (mb == null) return false;
        var n = mb.GetType().Name;
        return string.Equals(n, "MapInputBlocker", StringComparison.Ordinal) ||
               n.IndexOf("MapInputBlocker", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void CacheMapBlockerMembers()
    {
        cachedBoolSetter = null;
        cachedPush = null;
        cachedPop = null;
        cachedBoolProperty = null;
        cachedBoolField = null;

        if (mapInputBlocker == null) return;

        var t = mapInputBlocker.GetType();

        cachedBoolSetter =
            t.GetMethod("SetBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null) ??
            t.GetMethod("SetUIBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null) ??
            t.GetMethod("SetInputBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null) ??
            t.GetMethod("SetIsBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);

        cachedPush =
            t.GetMethod("PushBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(object) }, null) ??
            t.GetMethod("PushUIBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);

        cachedPop =
            t.GetMethod("PopBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(object) }, null) ??
            t.GetMethod("PopUIBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(object) }, null);

        cachedBoolProperty =
            t.GetProperty("IsBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            t.GetProperty("Blocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            t.GetProperty("UIBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        cachedBoolField =
            t.GetField("IsBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            t.GetField("Blocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
            t.GetField("UIBlocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void SetMapBlocked(bool blocked)
    {
        if (!blockMapInput) return;

        if (blocked == regionIsBlocking) return;
        regionIsBlocking = blocked;

        if (mapInputBlocker == null) return;

        // 1) Setter
        if (cachedBoolSetter != null)
        {
            cachedBoolSetter.Invoke(mapInputBlocker, new object[] { blocked });
            return;
        }

        // 2) Stack-style
        if (blocked && cachedPush != null)
        {
            cachedPush.Invoke(mapInputBlocker, new object[] { this });
            return;
        }
        if (!blocked && cachedPop != null)
        {
            cachedPop.Invoke(mapInputBlocker, new object[] { this });
            return;
        }

        // 3) Property/field
        if (cachedBoolProperty != null && cachedBoolProperty.CanWrite)
        {
            cachedBoolProperty.SetValue(mapInputBlocker, blocked);
            return;
        }
        if (cachedBoolField != null)
        {
            cachedBoolField.SetValue(mapInputBlocker, blocked);
            return;
        }

        // 4) Last resort
        mapInputBlocker.SendMessage("SetBlocked", blocked, SendMessageOptions.DontRequireReceiver);
        mapInputBlocker.SendMessage("SetUIBlocked", blocked, SendMessageOptions.DontRequireReceiver);
        mapInputBlocker.SendMessage("SetInputBlocked", blocked, SendMessageOptions.DontRequireReceiver);
    }

    // This lives in the same file so you “only input one script”.
    // It is automatically added to the Viewport at runtime/build.
    private class ViewportRegionHook : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
    {
        public TMPOverflowToScrollRect Owner { get; set; }
        public bool UnblockOnExit { get; set; } = true;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Owner?.SetMapBlocked(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (UnblockOnExit)
                Owner?.SetMapBlocked(false);
        }

        public void OnScroll(PointerEventData eventData)
        {
            // Keep blocked while scrolling over the viewport
            Owner?.SetMapBlocked(true);

            // Mark UI event used (map zoom should be gated by MapInputBlocker anyway)
            eventData.Use();
        }
    }
}
