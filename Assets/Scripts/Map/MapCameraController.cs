using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapBounds mapBounds;

    [Header("Zoom (Defaults; can be overridden by SettingsMenuController)")]
    [SerializeField] private float minZoom = 15f;
    [SerializeField] private float maxZoom = 450f;
    [SerializeField] private float zoomSpeed = 1000f;

    [Header("Pan (Defaults; can be overridden by SettingsMenuController)")]
    [SerializeField] private float panSpeed = 30f;

    [Header("Snap (Defaults; can be overridden by SettingsMenuController)")]
    [SerializeField] private float snapDuration = 0.5f;

    [Header("Reset (Defaults; can be overridden by SettingsMenuController)")]
    [SerializeField] private Vector3 resetPosition = new Vector3(0f, 0f, -10f);
    [SerializeField] private float resetZoom = 500f;


    [Header("Settings Integration")]
    [Tooltip("If enabled, pulls pan/zoom/snap/reset + bindings from SettingsMenuController.Instance when available.")]
    [SerializeField] private bool useSettingsMenuController = true;

    [Header("Pan Cursor Behavior")]
    [Tooltip("If true, during pan the cursor is warped to stay on the same MAP point (world point) the user started panning on.")]
    [SerializeField] private bool lockCursorToMapPointDuringPan = true;


    [Header("UI Input Blocking")]
    [Tooltip("If true, ANY UI under the pointer blocks zoom/pan (uses EventSystem.IsPointerOverGameObject). If false, only UI with MapInputBlocker (via MapUIRaycastUtil) blocks map input.")]
    [SerializeField] private bool blockZoomOverAnyUI = false;
    public bool IsSnapping => _isSnapping;
    public event Action OnSnapCompleted;

    private Camera _cam;

    // Settings source
    private SettingsMenuController _settings;

    // Runtime-tunable values (defaults come from serialized fields, can be overridden)
    private float _zoomSpeedRuntime;
    private float _panSpeedRuntime;
    private float _snapDurationRuntime;
    private Vector3 _resetPositionRuntime;
    private float _resetZoomRuntime;
    private float _modifierZoomMultiplierRuntime;

    private SettingsMenuController.MapModifierKey _modifierKeyRuntime;
    private SettingsMenuController.MapPanMouseButton _panButtonRuntime;

    // Snap state
    private bool _isSnapping;
    private Vector3 _snapStartPos;
    private float _snapStartZoom;
    private Vector3 _snapTargetPos;
    private float _snapTargetZoom;
    private float _snapElapsed;

    // Pan state
    private bool _isPanning;
    private Vector2 _panAnchorScreenPos;
    private Vector3 _panAnchorWorldPos; // NEW: world/map point under cursor when pan starts

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam.orthographic)
            _cam.orthographic = true;

        ApplyDefaultsToRuntime();
    }

    private void OnEnable()
    {
        TryBindSettingsController();
        PullFromSettingsIfAvailable();
        SubscribeToSettings();
    }

    private void OnDisable()
    {
        UnsubscribeFromSettings();
        if (_isPanning)
            EndPan();
    }

    private void Update()
    {
        if (useSettingsMenuController && _settings == null)
        {
            TryBindSettingsController();
            PullFromSettingsIfAvailable();
            SubscribeToSettings();
        }

        if (_isSnapping)
            UpdateSnap();
        else
            HandlePanAndZoom();
    }

    private void ApplyDefaultsToRuntime()
    {
        _zoomSpeedRuntime = zoomSpeed;
        _panSpeedRuntime = panSpeed;
        _snapDurationRuntime = snapDuration;
        _resetPositionRuntime = resetPosition;
        _resetZoomRuntime = resetZoom;

        _modifierZoomMultiplierRuntime = 2f;
        _modifierKeyRuntime = SettingsMenuController.MapModifierKey.Ctrl;
        _panButtonRuntime = SettingsMenuController.MapPanMouseButton.Right;
    }

    private void TryBindSettingsController()
    {
        if (!useSettingsMenuController)
            return;

        _settings = SettingsMenuController.Instance;
        if (_settings == null)
            _settings = FindFirstObjectByType<SettingsMenuController>();
    }

    private void SubscribeToSettings()
    {
        if (_settings == null)
            return;

        _settings.MapSettingsChanged -= OnMapSettingsChanged;
        _settings.MapSettingsChanged += OnMapSettingsChanged;
    }

    private void UnsubscribeFromSettings()
    {
        if (_settings == null)
            return;

        _settings.MapSettingsChanged -= OnMapSettingsChanged;
    }

    private void OnMapSettingsChanged()
    {
        PullFromSettingsIfAvailable();
    }

    private void PullFromSettingsIfAvailable()
    {
        if (_settings == null)
            return;

        _zoomSpeedRuntime = _settings.MapZoomSpeed;
        _panSpeedRuntime = _settings.MapPanSpeed;
        _snapDurationRuntime = _settings.MapSnapDuration;

        _resetPositionRuntime = _settings.MapResetPosition;
        _resetZoomRuntime = _settings.MapResetZoom;

        _modifierZoomMultiplierRuntime = Mathf.Max(1f, _settings.MapModifierZoomMultiplier);
        _modifierKeyRuntime = _settings.MapModifier;
        _panButtonRuntime = _settings.MapPanButton;
    }

    private void HandlePanAndZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null)
            return;

        float scrollY = mouse.scroll.ReadValue().y;

        // Combine detection of UI panels with MapInputBlocker and generic UI under cursor. The
        // EventSystem check will return true for any UI element (buttons, dropdown lists, etc.),
        // while MapUIRaycastUtil specifically tests for MapInputBlocker markers. Merging these
        // ensures that scroll wheel input over dropdown lists or other UI created at runtime
        // (e.g. TMP_Dropdown lists) still counts as blocking and prevents the map from zooming.
        bool pointerOverBlockingUI = MapUIRaycastUtil.IsPointerOverBlockingUI();
        if (blockZoomOverAnyUI && !pointerOverBlockingUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            pointerOverBlockingUI = true;

        var kb = Keyboard.current;
        bool modifierPressed = IsModifierPressed(kb, _modifierKeyRuntime);

        // Modifier + wheel down => reset (even if pointer is over UI)
        if (modifierPressed && scrollY < -0.01f)
        {
            ResetZoomKeyPress();
        }

        // Over blocking UI: block zoom input in both directions
        bool allowZoomThisFrame = true;
        // When the pointer is over a UI element marked with MapInputBlocker
        // we should prevent any scroll wheel zoom from affecting the map. This
        // ensures dropdowns, scrollbars and other interactive UI elements consume
        // the scroll input entirely instead of triggering a zoom.  Previously the
        // map allowed zoom in while over UI and blocked only zoom out, which
        // resulted in asymmetric behaviour (users could scroll in one direction
        // but not the other).  By unconditionally blocking zoom whenever
        // pointerOverBlockingUI is true, we treat UI overlays consistently.
        if (pointerOverBlockingUI)
            allowZoomThisFrame = false;

        // Zoom behavior
        if (allowZoomThisFrame && Mathf.Abs(scrollY) > 0.01f)
        {
            float effectiveZoomSpeed = _zoomSpeedRuntime;

            // If NOT over UI panel and modifier is held while zooming in => multiply zoom speed
            if (!pointerOverBlockingUI && modifierPressed && scrollY > 0.01f)
                effectiveZoomSpeed *= _modifierZoomMultiplierRuntime;

            float zoomDelta = -scrollY * effectiveZoomSpeed * Time.unscaledDeltaTime;

            // If over a UI panel and modifier is held while zooming in => snap + zoom to that panel's position
            if (pointerOverBlockingUI && modifierPressed && scrollY > 0.01f && TryGetHoveredPanelScreenCenter(out Vector2 panelCenterScreen))
            {
                SnapZoomToScreenPoint(panelCenterScreen, zoomDelta);
            }
            else
            {
                ZoomTowardsMouse(mouse.position.ReadValue(), zoomDelta);
            }
        }

        // Pan: disallow panning while pointer is over blocking UI
        if (pointerOverBlockingUI)
        {
            if (_isPanning)
                EndPan();

            ClampToBounds();
            return;
        }

        bool panPressed = IsPanButtonPressed(mouse, _panButtonRuntime);

        if (panPressed)
        {
            if (!_isPanning)
                BeginPan(mouse);

            Vector2 delta = mouse.delta.ReadValue();
            if (delta.sqrMagnitude > 0.0f)
            {
                float scale = _cam.orthographicSize * _panSpeedRuntime * Time.unscaledDeltaTime;
                Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * scale * 0.01f;

                _cam.transform.position += move;
                ClampToBounds();
            }

            // NEW: keep cursor glued to the SAME MAP point (world point) we started panning on
            if (lockCursorToMapPointDuringPan)
                WarpCursorToMapAnchor(mouse);
        }
        else
        {
            if (_isPanning)
                EndPan();

            ClampToBounds();
        }
    }

    private void BeginPan(Mouse mouse)
    {
        _isPanning = true;
        _panAnchorScreenPos = mouse.position.ReadValue();

        // NEW: record the map/world point under the cursor at pan start
        Vector3 world = _cam.ScreenToWorldPoint(new Vector3(_panAnchorScreenPos.x, _panAnchorScreenPos.y, _cam.nearClipPlane));
        _panAnchorWorldPos = new Vector3(world.x, world.y, 0f);
    }

    private void WarpCursorToMapAnchor(Mouse mouse)
    {
        // Warp cursor so it stays on the same world/map point as the camera moves.
        Vector3 screen3 = _cam.WorldToScreenPoint(new Vector3(_panAnchorWorldPos.x, _panAnchorWorldPos.y, 0f));
        Vector2 screen = new Vector2(screen3.x, screen3.y);

        // Clamp to screen bounds to avoid weirdness if the anchor goes off-screen.
        screen.x = Mathf.Clamp(screen.x, 0f, Screen.width - 1f);
        screen.y = Mathf.Clamp(screen.y, 0f, Screen.height - 1f);

        mouse.WarpCursorPosition(screen);
    }

    private void EndPan()
    {
        _isPanning = false;
    }

    public void ResetZoomKeyPress()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (mouse == null)
            return;
        bool modifierPressed = IsModifierPressed(kb, _modifierKeyRuntime);
        float scrollY = mouse.scroll.ReadValue().y;
        if (modifierPressed && scrollY < -0.01f)
        {
            SnapTo(_resetPositionRuntime, _resetZoomRuntime);
            return;
        }

    }


    public void ResetZoom()
    {
        SnapTo(_resetPositionRuntime, _resetZoomRuntime);
    }

    private bool IsPanButtonPressed(Mouse mouse, SettingsMenuController.MapPanMouseButton panButton)
    {
        return panButton switch
        {
            SettingsMenuController.MapPanMouseButton.Left => mouse.leftButton.isPressed,
            SettingsMenuController.MapPanMouseButton.Middle => mouse.middleButton.isPressed,
            _ => mouse.rightButton.isPressed,
        };
    }

    private bool IsModifierPressed(Keyboard kb, SettingsMenuController.MapModifierKey modifier)
    {
        if (kb == null)
            return false;

        return modifier switch
        {
            SettingsMenuController.MapModifierKey.Ctrl => kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed,
            SettingsMenuController.MapModifierKey.Alt => kb.leftAltKey.isPressed || kb.rightAltKey.isPressed,
            SettingsMenuController.MapModifierKey.Shift => kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed,
            _ => false,
        };
    }

    private void SnapZoomToScreenPoint(Vector2 screenPos, float zoomDelta)
    {
        float oldSize = _cam.orthographicSize;
        float newSize = Mathf.Clamp(oldSize + zoomDelta, minZoom, maxZoom);
        if (Mathf.Approximately(oldSize, newSize))
            return;

        Vector3 worldPoint = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, _cam.nearClipPlane));
        Vector3 targetPos = new Vector3(worldPoint.x, worldPoint.y, _cam.transform.position.z);
        SnapTo(targetPos, newSize);
    }

    private bool TryGetHoveredPanelScreenCenter(out Vector2 screenCenter)
    {
        screenCenter = default;

        if (EventSystem.current == null)
            return false;

        var mouse = Mouse.current;
        if (mouse == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = mouse.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results == null || results.Count == 0)
            return false;

        GameObject hit = results[0].gameObject;
        if (hit == null)
            return false;

        RectTransform rt = hit.GetComponentInParent<RectTransform>();
        if (rt == null)
            return false;

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        RectTransform panelRoot = rt;

        if (canvas != null)
        {
            Transform t = panelRoot.transform;
            while (t.parent != null && t.parent != canvas.transform)
            {
                if (t.parent.GetComponent<Canvas>() != null)
                    break;

                t = t.parent;
            }

            panelRoot = t as RectTransform ?? panelRoot;
        }

        Vector3 worldCenter = panelRoot.TransformPoint(panelRoot.rect.center);
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        screenCenter = RectTransformUtility.WorldToScreenPoint(uiCam, worldCenter);
        return true;
    }

    private void ZoomTowardsMouse(Vector2 mouseScreenPos, float zoomDelta)
    {
        float oldSize = _cam.orthographicSize;
        float newSize = Mathf.Clamp(oldSize + zoomDelta, minZoom, maxZoom);

        if (Mathf.Approximately(oldSize, newSize))
            return;

        Vector3 before = _cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, _cam.nearClipPlane));

        _cam.orthographicSize = newSize;

        Vector3 after = _cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, _cam.nearClipPlane));

        Vector3 diff = before - after;
        _cam.transform.position += new Vector3(diff.x, diff.y, 0f);

        ClampToBounds();
    }

    private void ClampToBounds()
    {
        if (mapBounds != null)
            _cam.transform.position = mapBounds.ClampPosition(_cam.transform.position);
    }

    private void UpdateSnap()
    {
        _snapElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_snapElapsed / Mathf.Max(0.0001f, _snapDurationRuntime));
        t = Mathf.SmoothStep(0f, 1f, t);

        Vector3 pos = Vector3.Lerp(_snapStartPos, _snapTargetPos, t);
        float size = Mathf.Lerp(_snapStartZoom, _snapTargetZoom, t);

        _cam.transform.position = pos;
        _cam.orthographicSize = Mathf.Clamp(size, minZoom, maxZoom);

        ClampToBounds();

        if (t >= 1f)
        {
            _isSnapping = false;
            OnSnapCompleted?.Invoke();
        }
    }

    public void SnapTo(Vector3 targetPosition, float targetZoom)
    {
        _isSnapping = true;
        _snapElapsed = 0f;

        _snapStartPos = _cam.transform.position;
        _snapStartZoom = _cam.orthographicSize;

        targetPosition.z = _snapStartPos.z;

        _snapTargetPos = targetPosition;
        _snapTargetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    public float CurrentZoom => _cam.orthographicSize;
    public Vector3 CurrentPosition => _cam.transform.position;

    public float MinZoom => minZoom;
    public float MaxZoom => maxZoom;

    public float GetZoomPercent()
    {
        if (Mathf.Approximately(minZoom, maxZoom))
            return 0f;

        return Mathf.InverseLerp(maxZoom, minZoom, _cam.orthographicSize);
    }
}
