using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TimelineManager : MonoBehaviour
{
    public enum TimelineSizingMode
    {
        // Recommended: use prefab sizes as baseline and only scale if background changes size/scale.
        PrefabBaseline = 0,

        // Compute sizes from background % (more volatile in World Space).
        Proportional = 1
    }

    [Header("Close / Exit")]
    [SerializeField] private Button closeButton;
    [SerializeField] private bool closeOnEscape = true;

    [Header("Layout References (Required)")]
    [SerializeField] private RectTransform background;               // Background rect defines "timeline space"
    [SerializeField] private RectTransform majorTickContainer;       // keep positioned/sized as in prefab
    [SerializeField] private RectTransform eventTickContainer;       // keep positioned/sized as in prefab
    [SerializeField] private RectTransform labelContainer;           // keep positioned/sized as in prefab
    [SerializeField] private RectTransform line;                     // optional

    [Header("Prefabs (Recommended)")]
    [SerializeField] private RectTransform majorTickPrefab;          // Image tick
    [SerializeField] private RectTransform eventTickPrefab;          // Image icon
    [SerializeField] private TMP_Text labelPrefab;                   // TMP label prefab

    [Header("Sizing Mode")]
    [SerializeField] private TimelineSizingMode sizingMode = TimelineSizingMode.PrefabBaseline;

    [Tooltip("IMPORTANT: Leave ON. This prevents the script from resizing/reanchoring your containers.")]
    [SerializeField] private bool preservePrefabContainerLayout = true;

    [Header("Event Icon Sprites")]
    [SerializeField] private Sprite defaultEventIcon;
    [SerializeField] private Sprite birthIcon;
    [SerializeField] private Sprite deathIcon;
    [SerializeField] private Sprite warIcon;
    [SerializeField] private Sprite plagueIcon;
    [SerializeField] private Sprite coronationIcon;

    [Header("Placement Proportions (relative to Background rect)")]
    [Range(0f, 0.5f)][SerializeField] private float leftPaddingPct = 0.08f;
    [Range(0f, 0.5f)][SerializeField] private float rightPaddingPct = 0.08f;
    [Range(0f, 1f)][SerializeField] private float lineYPct = 0.55f;
    [Range(0f, 0.5f)][SerializeField] private float labelOffsetPct = 0.10f;

    [Header("Proportional Sizes (ONLY used when SizingMode=Proportional)")]
    [Range(0f, 0.5f)][SerializeField] private float majorTickHeightPct = 0.12f;
    [Range(0f, 0.5f)][SerializeField] private float eventIconSizePct = 0.06f;
    [Range(0f, 0.5f)][SerializeField] private float labelMaxWidthPct = 0.18f;

    [Header("Text Sizing (Event-count scaling)")]
    [Range(0.0001f, 1f)]
    [SerializeField] private float minLabelScaleAtMaxDensity = 0.55f;

    [SerializeField] private int labelShrinkStartsAtEvents = 6;
    [SerializeField] private int labelShrinkFullyAtEvents = 30;

    [Header("Tick Density Rules")]
    [SerializeField] private int step1YearMaxRange = 10;
    [SerializeField] private int step5YearMaxRange = 50;
    [SerializeField] private int step10YearMaxRange = 150;
    [SerializeField] private int step25YearMaxRange = 500;

    [Header("Safety")]
    [Tooltip("If prefabs/containers have Layout components, they can override sizeDelta/positions.")]
    [SerializeField] private bool disableLayoutComponentsOnSpawnedObjects = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    // Data
    private SettlementInfoData _data;
    private readonly List<TimelineEvent> _events = new List<TimelineEvent>();
    private int _minYear;
    private int _maxYear;

    // Baseline sizing
    private bool _baselineCaptured;
    private Vector2 _baselineBgRectSize;
    private Vector3 _baselineBgLossyScale;

    private Vector2 _baselineMajorTickSize;
    private Vector2 _baselineEventIconSize;
    private Vector2 _baselineLabelSize;
    private float _baselineLabelFontSize;
    private Vector2 _baselineLineSize;

    // Change detection
    private Vector2 _lastBgRectSize;
    private Vector3 _lastBgLossyScale;
    private int _lastSettingsHash;

    private static readonly Regex YearRegex = new Regex(@"^\s*(\d{1,5})\b", RegexOptions.Compiled);

    [Serializable]
    private struct TimelineEvent
    {
        public int year;
        public string raw;
        public string text;
    }

    // -------------------------
    // Public API
    // -------------------------
    public void Initialize(SettlementInfoData data)
    {
        _data = data;
        BuildFromSettlementData();

        CaptureBaselineIfNeeded(force: false);
        CacheBgMetrics();
        _lastSettingsHash = ComputeSettingsHash();

        RebuildVisuals();
    }

    private void Awake()
    {
        WireClose();
        CaptureBaselineIfNeeded(force: false);
        CacheBgMetrics();
        _lastSettingsHash = ComputeSettingsHash();
    }

    private void Update()
    {
        if (closeOnEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (background == null)
            return;

        // Rebuild if background rect or scale changes (World Space resizes often change scale, not sizeDelta)
        if (_lastBgRectSize != background.rect.size || _lastBgLossyScale != background.lossyScale)
        {
            CacheBgMetrics();
            RebuildVisuals();
            return;
        }

        int nowHash = ComputeSettingsHash();
        if (nowHash != _lastSettingsHash)
        {
            _lastSettingsHash = nowHash;
            RebuildVisuals();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _lastSettingsHash = 0;
    }
#endif

    private void WireClose()
    {
        if (closeButton == null) return;
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);
    }

    private void Close()
    {
        Destroy(gameObject);
    }

    private void CacheBgMetrics()
    {
        if (background == null) return;
        _lastBgRectSize = background.rect.size;
        _lastBgLossyScale = background.lossyScale;
    }

    private void CaptureBaselineIfNeeded(bool force)
    {
        if (background == null) return;
        if (_baselineCaptured && !force) return;

        _baselineCaptured = true;
        _baselineBgRectSize = background.rect.size;
        _baselineBgLossyScale = background.lossyScale;

        _baselineMajorTickSize = majorTickPrefab != null ? majorTickPrefab.sizeDelta : new Vector2(0.02f, 0.2f);
        _baselineEventIconSize = eventTickPrefab != null ? eventTickPrefab.sizeDelta : new Vector2(0.2f, 0.2f);

        if (labelPrefab != null)
        {
            _baselineLabelSize = labelPrefab.rectTransform.sizeDelta;
            _baselineLabelFontSize = labelPrefab.fontSize;
        }
        else
        {
            _baselineLabelSize = new Vector2(2f, 0.5f);
            _baselineLabelFontSize = 18f;
        }

        _baselineLineSize = line != null ? line.sizeDelta : new Vector2(5f, 0.02f);

        if (verboseLogs)
        {
            Debug.Log($"[TimelineManager] Baseline captured. bgRect={_baselineBgRectSize}, bgScale={_baselineBgLossyScale}");
        }
    }

    private int ComputeSettingsHash()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + sizingMode.GetHashCode();
            h = h * 31 + (preservePrefabContainerLayout ? 1 : 0);

            h = h * 31 + Q(leftPaddingPct);
            h = h * 31 + Q(rightPaddingPct);
            h = h * 31 + Q(lineYPct);
            h = h * 31 + Q(labelOffsetPct);

            h = h * 31 + Q(majorTickHeightPct);
            h = h * 31 + Q(eventIconSizePct);
            h = h * 31 + Q(labelMaxWidthPct);

            h = h * 31 + minLabelScaleAtMaxDensity.GetHashCode();
            h = h * 31 + labelShrinkStartsAtEvents;
            h = h * 31 + labelShrinkFullyAtEvents;

            h = h * 31 + step1YearMaxRange;
            h = h * 31 + step5YearMaxRange;
            h = h * 31 + step10YearMaxRange;
            h = h * 31 + step25YearMaxRange;

            h = h * 31 + (disableLayoutComponentsOnSpawnedObjects ? 1 : 0);
            h = h * 31 + (_events != null ? _events.Count : 0);

            return h;
        }

        int Q(float f) => Mathf.RoundToInt(f * 100000f);
    }

    // -------------------------
    // Parsing from SettlementInfoData.history.timelineEntries (string[])
    // -------------------------
    private void BuildFromSettlementData()
    {
        _events.Clear();
        _minYear = 0;
        _maxYear = 0;

        if (_data == null || _data.history == null || _data.history.timelineEntries == null)
        {
            if (verboseLogs) Debug.Log("[TimelineManager] No settlement data / no timelineEntries.");
            return;
        }

        foreach (string entry in _data.history.timelineEntries)
        {
            if (TryParseTimelineEntry(entry, out TimelineEvent ev))
                _events.Add(ev);
            else if (verboseLogs)
                Debug.LogWarning($"[TimelineManager] Could not parse timeline entry: '{entry}'");
        }

        if (_events.Count == 0)
        {
            if (verboseLogs) Debug.Log("[TimelineManager] 0 parsed events.");
            return;
        }

        _events.Sort((a, b) => a.year.CompareTo(b.year));
        _minYear = _events[0].year;
        _maxYear = _events[_events.Count - 1].year;

        if (_minYear == _maxYear)
            _maxYear = _minYear + 1;

        if (verboseLogs)
            Debug.Log($"[TimelineManager] Parsed {_events.Count} events. Range={_minYear}..{_maxYear}");
    }

    private bool TryParseTimelineEntry(string entry, out TimelineEvent ev)
    {
        ev = default;

        if (string.IsNullOrWhiteSpace(entry))
            return false;

        Match m = YearRegex.Match(entry);
        if (!m.Success)
            return false;

        if (!int.TryParse(m.Groups[1].Value, out int year))
            return false;

        // Strip "1345 DR: ..." / "1345: ..."
        string text = Regex.Replace(entry, @"^\s*\d{1,5}\s*(DR)?\s*:\s*", "", RegexOptions.IgnoreCase).Trim();

        ev = new TimelineEvent
        {
            year = year,
            raw = entry,
            text = text
        };
        return true;
    }

    // -------------------------
    // Core Rebuild
    // -------------------------
    private void RebuildVisuals()
    {
        if (background == null || majorTickContainer == null || eventTickContainer == null || labelContainer == null)
        {
            Debug.LogWarning("[TimelineManager] Missing required references. Assign Background + the three containers.");
            return;
        }

        // IMPORTANT: do NOT stretch/re-anchor containers. We want your prefab layout to remain intact.
        // preservePrefabContainerLayout exists mainly as a “guardrail” toggle.
        // If you ever WANT the script to override layout, you would implement that as a separate mode.
        // For now, we never touch container anchors/size/pos if preservePrefabContainerLayout=true.

        ClearChildren(majorTickContainer);
        ClearChildren(eventTickContainer);
        ClearChildren(labelContainer);

        EnsureLineSizedInBackgroundSpace();

        if (_events.Count == 0)
            return;

        // Compute “timeline space” positions in BACKGROUND LOCAL SPACE first.
        float bgW = background.rect.width;
        float bgH = background.rect.height;

        float leftXbg = (-bgW * 0.5f) + (bgW * leftPaddingPct);
        float rightXbg = (bgW * 0.5f) - (bgW * rightPaddingPct);
        float usableWbg = Mathf.Max(0.0001f, rightXbg - leftXbg);

        float lineYbg = (-bgH * 0.5f) + (bgH * lineYPct);
        float yearLabelYbg = lineYbg + (bgH * labelOffsetPct);

        // Scale relative to baseline using BOTH rect size and world scale
        float uniformScale = ComputeUniformScale();

        // Event-count scaling for labels
        float densityT = Mathf.InverseLerp(labelShrinkStartsAtEvents,
            Mathf.Max(labelShrinkStartsAtEvents + 1, labelShrinkFullyAtEvents),
            _events.Count);
        float densityScale = Mathf.Lerp(1f, minLabelScaleAtMaxDensity, densityT);

        float labelFont = GetLabelFontSize(bgH, uniformScale) * densityScale;
        Vector2 labelSize = GetLabelSize(bgW, uniformScale);

        int rangeYears = Mathf.Abs(_maxYear - _minYear);
        int majorStep = ChooseMajorStep(rangeYears);

        int firstMajor = FloorToStep(_minYear, majorStep);
        int lastMajor = CeilToStep(_maxYear, majorStep);

        float majorTickHeight = GetMajorTickHeight(bgH, uniformScale);
        Vector2 majorTickSize = GetMajorTickSize(majorTickHeight, uniformScale);
        Vector2 iconSize = GetEventIconSize(bgH, uniformScale);

        // Precompute container-space coordinates for left/right X and the baseline Y positions
        float majorLineY = BgLocalToContainerLocalY(majorTickContainer, lineYbg);
        float eventLineY = BgLocalToContainerLocalY(eventTickContainer, lineYbg);
        float labelYearY = BgLocalToContainerLocalY(labelContainer, yearLabelYbg);
        float labelBaseY = BgLocalToContainerLocalY(labelContainer, lineYbg + (bgH * (labelOffsetPct * 1.7f)));

        float leftX_major = BgLocalToContainerLocalX(majorTickContainer, leftXbg, lineYbg);
        float rightX_major = BgLocalToContainerLocalX(majorTickContainer, rightXbg, lineYbg);

        float leftX_event = BgLocalToContainerLocalX(eventTickContainer, leftXbg, lineYbg);
        float rightX_event = BgLocalToContainerLocalX(eventTickContainer, rightXbg, lineYbg);

        float leftX_label = BgLocalToContainerLocalX(labelContainer, leftXbg, yearLabelYbg);
        float rightX_label = BgLocalToContainerLocalX(labelContainer, rightXbg, yearLabelYbg);

        // Major ticks + year labels
        for (int y = firstMajor; y <= lastMajor; y += majorStep)
        {
            float t = Mathf.InverseLerp(_minYear, _maxYear, y);

            float xMajor = Mathf.Lerp(leftX_major, rightX_major, t);
            float xLabel = Mathf.Lerp(leftX_label, rightX_label, t);

            CreateMajorTick(xMajor, majorLineY, majorTickSize);
            CreateYearLabel(y, xLabel, labelYearY, labelFont * 0.9f, labelSize);
        }

        // Event icons + labels (stack per year)
        var perYearCount = new Dictionary<int, int>();

        foreach (var ev in _events)
        {
            float t = Mathf.InverseLerp(_minYear, _maxYear, ev.year);

            float xEvent = Mathf.Lerp(leftX_event, rightX_event, t);
            float xLabel = Mathf.Lerp(leftX_label, rightX_label, t);

            CreateEventIcon(ev, xEvent, eventLineY, iconSize);

            int stackIndex = 0;
            if (perYearCount.TryGetValue(ev.year, out int existing))
            {
                stackIndex = existing;
                perYearCount[ev.year] = existing + 1;
            }
            else
            {
                perYearCount[ev.year] = 1;
            }

            // Stack spacing: use label prefab baseline height when possible
            float stackStep = Mathf.Max(0.05f, (labelSize.y > 0.0001f ? labelSize.y : 0.3f) * 1.1f);
            float y = labelBaseY + (stackIndex * stackStep);

            CreateEventLabel(ev, xLabel, y, labelFont, labelSize);
        }
    }

    private float ComputeUniformScale()
    {
        // Effective size = rect size * lossy scale. This handles both scale-based resize and sizeDelta-based resize.
        float baseW = Mathf.Max(0.0001f, _baselineBgRectSize.x * Mathf.Max(0.0001f, _baselineBgLossyScale.x));
        float baseH = Mathf.Max(0.0001f, _baselineBgRectSize.y * Mathf.Max(0.0001f, _baselineBgLossyScale.y));

        float curW = Mathf.Max(0.0001f, background.rect.width * Mathf.Max(0.0001f, background.lossyScale.x));
        float curH = Mathf.Max(0.0001f, background.rect.height * Mathf.Max(0.0001f, background.lossyScale.y));

        float sx = curW / baseW;
        float sy = curH / baseH;
        return Mathf.Min(sx, sy);
    }

    // -------------------------
    // Background-local -> Container-local mapping
    // -------------------------
    private Vector2 BgLocalToContainerLocal(RectTransform container, Vector2 bgLocal)
    {
        // Convert a local point on Background into a local point for the container.
        Vector3 world = background.TransformPoint(new Vector3(bgLocal.x, bgLocal.y, 0f));
        Vector3 local = container.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }

    private float BgLocalToContainerLocalX(RectTransform container, float bgX, float bgY)
    {
        return BgLocalToContainerLocal(container, new Vector2(bgX, bgY)).x;
    }

    private float BgLocalToContainerLocalY(RectTransform container, float bgY)
    {
        return BgLocalToContainerLocal(container, new Vector2(0f, bgY)).y;
    }

    // -------------------------
    // Smarter sizing (prefab baseline)
    // -------------------------
    private float GetMajorTickHeight(float bgH, float uniformScale)
    {
        if (sizingMode == TimelineSizingMode.PrefabBaseline && majorTickPrefab != null)
            return Mathf.Max(0.0001f, _baselineMajorTickSize.y * uniformScale);

        return Mathf.Max(0.0001f, bgH * majorTickHeightPct);
    }

    private Vector2 GetMajorTickSize(float desiredHeight, float uniformScale)
    {
        if (sizingMode == TimelineSizingMode.PrefabBaseline && majorTickPrefab != null)
        {
            float w = Mathf.Max(0.0001f, _baselineMajorTickSize.x * uniformScale);
            float h = Mathf.Max(0.0001f, desiredHeight);
            return new Vector2(w, h);
        }

        float fallbackW = majorTickPrefab != null ? Mathf.Max(0.0001f, majorTickPrefab.sizeDelta.x) : 0.02f;
        return new Vector2(fallbackW, Mathf.Max(0.0001f, desiredHeight));
    }

    private Vector2 GetEventIconSize(float bgH, float uniformScale)
    {
        if (sizingMode == TimelineSizingMode.PrefabBaseline && eventTickPrefab != null)
        {
            float s = Mathf.Max(0.0001f, _baselineEventIconSize.x * uniformScale);
            return new Vector2(s, s);
        }

        float sp = Mathf.Max(0.0001f, bgH * eventIconSizePct);
        return new Vector2(sp, sp);
    }

    private float GetLabelFontSize(float bgH, float uniformScale)
    {
        if (sizingMode == TimelineSizingMode.PrefabBaseline && labelPrefab != null)
            return Mathf.Max(1f, _baselineLabelFontSize * uniformScale);

        return Mathf.Max(10f, bgH * 0.06f);
    }

    private Vector2 GetLabelSize(float bgW, float uniformScale)
    {
        if (sizingMode == TimelineSizingMode.PrefabBaseline && labelPrefab != null)
        {
            float w = Mathf.Max(0.1f, _baselineLabelSize.x * uniformScale);
            float h = Mathf.Max(0.1f, _baselineLabelSize.y * uniformScale);
            return new Vector2(w, h);
        }

        float maxW = Mathf.Max(0.1f, bgW * labelMaxWidthPct);
        return new Vector2(maxW, 0.5f);
    }

    // -------------------------
    // Line sizing (in Background space only)
    // -------------------------
    private void EnsureLineSizedInBackgroundSpace()
    {
        if (line == null || background == null)
            return;

        float bgW = background.rect.width;
        float bgH = background.rect.height;

        float leftXbg = (-bgW * 0.5f) + (bgW * leftPaddingPct);
        float rightXbg = (bgW * 0.5f) - (bgW * rightPaddingPct);
        float lineYbg = (-bgH * 0.5f) + (bgH * lineYPct);

        float uniformScale = ComputeUniformScale();

        // If line isn't under background, do nothing (user might place it elsewhere)
        if (line.parent != background)
            return;

        line.anchorMin = new Vector2(0.5f, 0.5f);
        line.anchorMax = new Vector2(0.5f, 0.5f);
        line.pivot = new Vector2(0.5f, 0.5f);

        line.anchoredPosition = new Vector2((leftXbg + rightXbg) * 0.5f, lineYbg);

        float w = Mathf.Max(0.0001f, rightXbg - leftXbg);

        float h;
        if (sizingMode == TimelineSizingMode.PrefabBaseline)
            h = Mathf.Max(0.0001f, _baselineLineSize.y * uniformScale);
        else
            h = Mathf.Max(0.0001f, line.sizeDelta.y);

        line.sizeDelta = new Vector2(w, h);
        line.localScale = Vector3.one;
    }

    // -------------------------
    // Spawners
    // -------------------------
    private void CreateMajorTick(float x, float y, Vector2 size)
    {
        RectTransform rt = InstantiateRect(majorTickContainer, majorTickPrefab, "MajorTick");
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = size;
    }

    private void CreateEventIcon(TimelineEvent ev, float x, float y, Vector2 size)
    {
        RectTransform rt = InstantiateRect(eventTickContainer, eventTickPrefab, "EventIcon");
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = size;

        Image img = rt.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = false;
            img.sprite = ChooseIconForEvent(ev);
            img.preserveAspect = true;
        }
    }

    private Sprite ChooseIconForEvent(TimelineEvent ev)
    {
        string s = (ev.text ?? ev.raw ?? string.Empty).ToLowerInvariant();

        if (s.Contains("birth")) return birthIcon != null ? birthIcon : defaultEventIcon;
        if (s.Contains("death")) return deathIcon != null ? deathIcon : defaultEventIcon;
        if (s.Contains("war")) return warIcon != null ? warIcon : defaultEventIcon;
        if (s.Contains("plague")) return plagueIcon != null ? plagueIcon : defaultEventIcon;
        if (s.Contains("coronation")) return coronationIcon != null ? coronationIcon : defaultEventIcon;

        return defaultEventIcon;
    }

    private void CreateYearLabel(int year, float x, float y, float fontSize, Vector2 labelSize)
    {
        TMP_Text t = InstantiateLabel(labelContainer, labelPrefab, "YearLabel");
        t.text = year.ToString();
        t.fontSize = fontSize;
        t.enableWordWrapping = false;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, y);

        if (labelSize.x > 0.0001f)
            rt.sizeDelta = new Vector2(labelSize.x, rt.sizeDelta.y);
    }

    private void CreateEventLabel(TimelineEvent ev, float x, float y, float fontSize, Vector2 labelSize)
    {
        TMP_Text t = InstantiateLabel(labelContainer, labelPrefab, "EventLabel");
        t.text = $"{ev.year}: {ev.text}";
        t.fontSize = fontSize;
        t.enableWordWrapping = true;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, y);

        if (labelSize.x > 0.0001f)
            rt.sizeDelta = new Vector2(labelSize.x, rt.sizeDelta.y);
    }

    private RectTransform InstantiateRect(RectTransform parent, RectTransform prefab, string fallbackName)
    {
        RectTransform rt;

        if (prefab != null)
            rt = Instantiate(prefab, parent);
        else
        {
            var go = new GameObject(fallbackName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rt = (RectTransform)go.transform;

            var img = go.GetComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;

            rt.sizeDelta = new Vector2(0.02f, 0.2f);
        }

        rt.localScale = Vector3.one;

        if (disableLayoutComponentsOnSpawnedObjects)
            DisableLayoutComponents(rt);

        return rt;
    }

    private TMP_Text InstantiateLabel(RectTransform parent, TMP_Text prefab, string fallbackName)
    {
        TMP_Text t;

        if (prefab != null)
            t = Instantiate(prefab, parent);
        else
        {
            var go = new GameObject(fallbackName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            t = go.GetComponent<TextMeshProUGUI>();
            t.text = fallbackName;
            t.fontSize = 18f;
            t.color = Color.white;
        }

        t.rectTransform.localScale = Vector3.one;

        if (disableLayoutComponentsOnSpawnedObjects)
            DisableLayoutComponents(t.rectTransform);

        return t;
    }

    private void DisableLayoutComponents(RectTransform root)
    {
        foreach (var lg in root.GetComponentsInChildren<LayoutGroup>(true))
            lg.enabled = false;

        foreach (var csf in root.GetComponentsInChildren<ContentSizeFitter>(true))
            csf.enabled = false;

        foreach (var le in root.GetComponentsInChildren<LayoutElement>(true))
            le.enabled = false;
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private int ChooseMajorStep(int rangeYears)
    {
        if (rangeYears <= step1YearMaxRange) return 1;
        if (rangeYears <= step5YearMaxRange) return 5;
        if (rangeYears <= step10YearMaxRange) return 10;
        if (rangeYears <= step25YearMaxRange) return 25;
        return 50;
    }

    private int FloorToStep(int year, int step)
    {
        if (step <= 0) return year;
        int rem = year % step;
        return year - rem;
    }

    private int CeilToStep(int year, int step)
    {
        if (step <= 0) return year;
        int rem = year % step;
        return rem == 0 ? year : (year + (step - rem));
    }
}
