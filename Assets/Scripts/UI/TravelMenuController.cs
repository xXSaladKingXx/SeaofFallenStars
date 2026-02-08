using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TravelMenuController : MonoBehaviour
{
    public enum Mode
    {
        None = 0,
        LineCalculation = 1,
        AreaCalculation = 2,
        PlanTrip = 3
    }

    public enum InputStyle
    {
        Drag = 0,
        ClickPoints = 1
    }

    [Header("Wiring")]
    [SerializeField] private DistanceCalculator distanceCalculator;

    [Tooltip("Key used by UIWindowRegistry to close this window.")]
    [SerializeField] private string registryKey = "TravelMenu";

    [Header("Mode Toggles")]
    [SerializeField] private Toggle lineModeToggle;
    [SerializeField] private Toggle areaModeToggle;
    [SerializeField] private Toggle tripModeToggle;

    [Header("Line Options")]
    [SerializeField] private Toggle lineDragToggle;
    [SerializeField] private Toggle lineClickToggle;

    [Header("Area Options")]
    [SerializeField] private Toggle areaDragToggle;
    [SerializeField] private Toggle areaClickToggle;

    [Header("Actions")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button finishButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;

    [Header("Output")]
    [SerializeField] private TMP_Text distanceOutputText;
    [SerializeField] private TMP_Text areaOutputText;
    [SerializeField] private TMP_Text timeOutputText;

    [Header("Trip Planning UI")]
    [SerializeField] private TMP_InputField globalMilesPerDayInput;
    [SerializeField] private Toggle perSegmentMilesPerDayToggle;

    [Tooltip("Parent container for per-segment rows (VerticalLayoutGroup recommended).")]
    [SerializeField] private Transform segmentRowsParent;

    [Tooltip("Prefab with TravelTripSegmentRow component (label + TMP_InputField).")]
    [SerializeField] private GameObject segmentRowPrefab;

    [Header("Visualization (optional)")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Input Rules")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;
    [SerializeField] private float minPointSpacingUnits = 0.25f;
    [SerializeField] private bool clampToMapBounds = true;

    private Mode _mode = Mode.None;
    private InputStyle _inputStyle = InputStyle.ClickPoints;

    private bool _captureEnabled;
    private bool _isDragging;

    private readonly List<Vector2> _points = new List<Vector2>(2048);
    private readonly List<TravelTripSegmentRow> _segmentRows = new List<TravelTripSegmentRow>(256);

    private void Awake()
    {
        // Always resolve on load (do not rely on inspector reference)
        ResolveDistanceCalculator(logWarning: false);

        WireUI();
        ApplyUIToState();
        ClearAll();
    }

    private void OnEnable()
    {
        // Resolve again each time the menu is shown (e.g., reopened after scene load)
        ResolveDistanceCalculator(logWarning: true);

        // Default mode if none selected
        if (_mode == Mode.None)
        {
            if (lineModeToggle != null) lineModeToggle.isOn = true;
            SetMode(Mode.LineCalculation);
        }
    }

    private void Update()
    {
        if (!_captureEnabled) return;

        // If DistanceCalculator wasn't available at enable time (or scene changed), keep retrying.
        if (distanceCalculator == null)
        {
            ResolveDistanceCalculator(logWarning: false);
            if (distanceCalculator == null) return;
        }

        if (ignoreWhenPointerOverUI && IsPointerOverUI())
            return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        switch (_mode)
        {
            case Mode.LineCalculation:
                HandleLineInput(mouse);
                break;

            case Mode.AreaCalculation:
                HandleAreaInput(mouse);
                break;

            case Mode.PlanTrip:
                HandleTripInput(mouse);
                break;
        }
    }

    // -----------------------------
    // DistanceCalculator resolution
    // -----------------------------

    private void ResolveDistanceCalculator(bool logWarning)
    {
        // Prefer finding even if inactive; supports cases where DistanceCalculator lives on a disabled GO at load.
#if UNITY_2023_1_OR_NEWER
        distanceCalculator = FindFirstObjectByType<DistanceCalculator>(FindObjectsInactive.Include);
#else
        distanceCalculator = FindObjectOfType<DistanceCalculator>(true);
#endif

        if (distanceCalculator == null && logWarning)
        {
            Debug.LogWarning($"{nameof(TravelMenuController)}: No DistanceCalculator found in the active scene. " +
                             $"Travel calculations will be unavailable until one exists.");
        }
    }

    // -----------------------------
    // UI wiring / state
    // -----------------------------

    public void SetRegistryKey(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            registryKey = key;
    }

    private void WireUI()
    {
        if (lineModeToggle != null) lineModeToggle.onValueChanged.AddListener(on => { if (on) SetMode(Mode.LineCalculation); });
        if (areaModeToggle != null) areaModeToggle.onValueChanged.AddListener(on => { if (on) SetMode(Mode.AreaCalculation); });
        if (tripModeToggle != null) tripModeToggle.onValueChanged.AddListener(on => { if (on) SetMode(Mode.PlanTrip); });

        if (lineDragToggle != null) lineDragToggle.onValueChanged.AddListener(_ => ApplyUIToState());
        if (lineClickToggle != null) lineClickToggle.onValueChanged.AddListener(_ => ApplyUIToState());

        if (areaDragToggle != null) areaDragToggle.onValueChanged.AddListener(_ => ApplyUIToState());
        if (areaClickToggle != null) areaClickToggle.onValueChanged.AddListener(_ => ApplyUIToState());

        if (startButton != null) startButton.onClick.AddListener(() =>
        {
            // Ensure we can actually calculate once user starts
            if (distanceCalculator == null)
                ResolveDistanceCalculator(logWarning: true);

            _captureEnabled = (distanceCalculator != null);
            _isDragging = false;
        });

        if (finishButton != null) finishButton.onClick.AddListener(FinishAndReport);
        if (clearButton != null) clearButton.onClick.AddListener(ClearAll);
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);

        if (perSegmentMilesPerDayToggle != null) perSegmentMilesPerDayToggle.onValueChanged.AddListener(_ => RefreshTripRows());
        if (globalMilesPerDayInput != null) globalMilesPerDayInput.onEndEdit.AddListener(_ => FinishAndReport());
    }

    private void SetMode(Mode mode)
    {
        _mode = mode;

        // Trip is point-based only
        if (_mode == Mode.PlanTrip)
            _inputStyle = InputStyle.ClickPoints;

        ApplyUIToState();
        ClearAll();
    }

    private void ApplyUIToState()
    {
        if (_mode == Mode.LineCalculation)
        {
            _inputStyle = (lineDragToggle != null && lineDragToggle.isOn) ? InputStyle.Drag : InputStyle.ClickPoints;
        }
        else if (_mode == Mode.AreaCalculation)
        {
            _inputStyle = (areaDragToggle != null && areaDragToggle.isOn) ? InputStyle.Drag : InputStyle.ClickPoints;
        }
        else if (_mode == Mode.PlanTrip)
        {
            _inputStyle = InputStyle.ClickPoints;
        }
    }

    // -----------------------------
    // Input handling
    // -----------------------------

    private void HandleLineInput(Mouse mouse)
    {
        if (_inputStyle == InputStyle.Drag)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                BeginDragCapture();
            }
            else if (_isDragging && mouse.leftButton.isPressed)
            {
                DragCaptureStep();
            }
            else if (_isDragging && mouse.leftButton.wasReleasedThisFrame)
            {
                EndDragCapture();
                FinishAndReport();
            }
        }
        else // ClickPoints
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                AddClickedPoint();
                ReportLineOnly();
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                FinishAndReport();
            }
        }
    }

    private void HandleAreaInput(Mouse mouse)
    {
        if (_inputStyle == InputStyle.Drag)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                BeginDragCapture();
            }
            else if (_isDragging && mouse.leftButton.isPressed)
            {
                DragCaptureStep();
            }
            else if (_isDragging && mouse.leftButton.wasReleasedThisFrame)
            {
                EndDragCapture();
                FinishAndReport();
            }
        }
        else // ClickPoints
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                AddClickedPoint();
                ReportAreaOnly();
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                FinishAndReport();
            }
        }
    }

    private void HandleTripInput(Mouse mouse)
    {
        // Trip planning is always ClickPoints:
        if (mouse.leftButton.wasPressedThisFrame)
        {
            AddClickedPoint();
            RefreshTripRows();
            ReportTripOnly();
        }
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            FinishAndReport();
        }
    }

    private void BeginDragCapture()
    {
        _points.Clear();
        _isDragging = true;

        if (TryGetMapPoint(out Vector2 p, out _))
        {
            _points.Add(p);
            UpdateLineRenderer();
        }
    }

    private void DragCaptureStep()
    {
        if (_points.Count == 0)
        {
            BeginDragCapture();
            return;
        }

        if (!TryGetMapPoint(out Vector2 p, out _))
            return;

        Vector2 prev = _points[_points.Count - 1];
        if (Vector2.Distance(prev, p) < Mathf.Max(0.0001f, minPointSpacingUnits))
            return;

        _points.Add(p);
        UpdateLineRenderer();
    }

    private void EndDragCapture()
    {
        _isDragging = false;
    }

    private void AddClickedPoint()
    {
        if (!TryGetMapPoint(out Vector2 p, out _))
            return;

        _points.Add(p);
        UpdateLineRenderer();
    }

    private bool TryGetMapPoint(out Vector2 mapPt, out Vector3 worldPt)
    {
        mapPt = default;
        worldPt = default;

        if (distanceCalculator == null)
            ResolveDistanceCalculator(logWarning: false);

        if (distanceCalculator == null) return false;

        if (!distanceCalculator.TryGetMouseMapPoint(out mapPt, out worldPt))
            return false;

        if (clampToMapBounds || distanceCalculator.ClampToMapBoundsByDefault)
            mapPt = distanceCalculator.ClampToMapBounds(mapPt);

        return true;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    // -----------------------------
    // Reporting / calculations
    // -----------------------------

    private void FinishAndReport()
    {
        switch (_mode)
        {
            case Mode.LineCalculation:
                ReportLineOnly();
                break;

            case Mode.AreaCalculation:
                ReportAreaOnly();
                break;

            case Mode.PlanTrip:
                ReportTripOnly();
                break;
        }
    }

    private void ReportLineOnly()
    {
        ClearOutputs();

        if (distanceCalculator == null || _points.Count < 2)
        {
            SetDistanceText("Distance: —");
            return;
        }

        float miles = distanceCalculator.CalculatePolylineLengthMiles(_points, clampToMapBounds);
        SetDistanceText($"Distance: {miles:N2} mi");
    }

    private void ReportAreaOnly()
    {
        ClearOutputs();

        if (distanceCalculator == null || _points.Count < 3)
        {
            SetAreaText("Area: —");
            return;
        }

        float areaSqMi = distanceCalculator.CalculatePolygonAreaSqMi(_points, clampToMapBounds);
        SetAreaText($"Area: {areaSqMi:N2} sq mi");
    }

    private void ReportTripOnly()
    {
        ClearOutputs();

        if (distanceCalculator == null || _points.Count < 2)
        {
            SetDistanceText("Trip Distance: —");
            SetTimeText("Travel Time: —");
            return;
        }

        float totalMiles = distanceCalculator.CalculatePolylineLengthMiles(_points, clampToMapBounds);
        float days = ComputeTripDays(totalMiles);

        SetDistanceText($"Trip Distance: {totalMiles:N2} mi");
        SetTimeText($"Travel Time: {days:N2} days");
    }

    private float ComputeTripDays(float totalMiles)
    {
        float globalMpd = GetGlobalMilesPerDay(24f);

        bool perSeg = (perSegmentMilesPerDayToggle != null && perSegmentMilesPerDayToggle.isOn);
        if (!perSeg || _points.Count < 2 || _segmentRows.Count == 0)
        {
            return (globalMpd > 0f) ? (totalMiles / globalMpd) : 0f;
        }

        // Per segment: days = sum(segmentMiles / segmentMpd)
        float days = 0f;
        double mpu = distanceCalculator.GetMilesPerMapUnit();

        int segCount = _points.Count - 1;
        for (int i = 0; i < segCount; i++)
        {
            float segUnits = Vector2.Distance(_points[i], _points[i + 1]);
            float segMiles = (float)(segUnits * mpu);

            float mpd = (i < _segmentRows.Count) ? _segmentRows[i].GetMilesPerDay(globalMpd) : globalMpd;
            if (mpd > 0f)
                days += segMiles / mpd;
        }

        return days;
    }

    private float GetGlobalMilesPerDay(float fallback)
    {
        if (globalMilesPerDayInput == null) return fallback;

        if (float.TryParse(globalMilesPerDayInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > 0f)
            return v;

        return fallback;
    }

    // -----------------------------
    // Trip segment rows
    // -----------------------------

    private void RefreshTripRows()
    {
        if (_mode != Mode.PlanTrip)
            return;

        bool perSeg = (perSegmentMilesPerDayToggle != null && perSegmentMilesPerDayToggle.isOn);
        if (!perSeg)
        {
            ClearTripRows();
            return;
        }

        if (segmentRowsParent == null || segmentRowPrefab == null)
        {
            // UI not wired; silently fall back to global miles/day.
            ClearTripRows();
            return;
        }

        int needed = Mathf.Max(0, _points.Count - 1);

        // Rebuild (simple + reliable; trip sizes are small)
        ClearTripRows();

        float globalMpd = GetGlobalMilesPerDay(24f);

        for (int i = 0; i < needed; i++)
        {
            var go = Instantiate(segmentRowPrefab, segmentRowsParent);
            var row = go.GetComponent<TravelTripSegmentRow>();
            if (row == null) continue;

            row.SetLabel($"Segment {i + 1} mi/day");
            row.SetMilesPerDay(globalMpd);
            _segmentRows.Add(row);
        }
    }

    private void ClearTripRows()
    {
        for (int i = 0; i < _segmentRows.Count; i++)
        {
            if (_segmentRows[i] != null)
                Destroy(_segmentRows[i].gameObject);
        }
        _segmentRows.Clear();
    }

    // -----------------------------
    // Visuals / outputs
    // -----------------------------

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || distanceCalculator == null)
            return;

        lineRenderer.positionCount = _points.Count;

        for (int i = 0; i < _points.Count; i++)
        {
            Vector3 w = distanceCalculator.MapPointToWorld(_points[i]);
            lineRenderer.SetPosition(i, w);
        }
    }

    private void ClearAll()
    {
        _captureEnabled = false;
        _isDragging = false;

        _points.Clear();
        ClearTripRows();
        ClearOutputs();

        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
    }

    private void ClearOutputs()
    {
        SetDistanceText(string.Empty);
        SetAreaText(string.Empty);
        SetTimeText(string.Empty);
    }

    private void SetDistanceText(string text)
    {
        if (distanceOutputText != null) distanceOutputText.text = text;
    }

    private void SetAreaText(string text)
    {
        if (areaOutputText != null) areaOutputText.text = text;
    }

    private void SetTimeText(string text)
    {
        if (timeOutputText != null) timeOutputText.text = text;
    }

    private void CloseWindow()
    {
        UIWindowRegistry.Close(registryKey);
    }
}
