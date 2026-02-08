using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Layer menu controller that drives MapManager.
///
/// - Layer buttons: call MapManager.SetActiveLayer(...)
/// - Geography toggle: MapManager.SetGeographyMode(...)
/// - Travel mode: MapManager.SetTravelMode(...)
///
/// Assign the UI elements in the inspector.
/// </summary>
[DisallowMultipleComponent]
public class MapLayerButtonBinder : MonoBehaviour
{
    [Header("Map Manager")]
    [SerializeField] private MapManager mapManager;
    [SerializeField] private bool autoFindMapManager = true;

    [Header("Layer Buttons")]
    [SerializeField] private Button regionalButton;
    [SerializeField] private Button countryButton;
    [SerializeField] private Button duchyButton;
    [SerializeField] private Button lordshipButton;
    [SerializeField] private Button pointButton;

    [Header("Modes")]
    [Tooltip("Optional: if assigned, toggles Geography Mode.")]
    [SerializeField] private Toggle geographyModeToggle;

    [Tooltip("Use either a Toggle or a Button for Travel Mode. If both are assigned, Toggle wins.")]
    [SerializeField] private Toggle travelModeToggle;

    [Tooltip("If travelModeToggle is not assigned, this button toggles Travel Mode.")]
    [SerializeField] private Button travelModeButton;

    [Tooltip("Optional: an indicator (Image, Text, etc.) that is enabled when Travel Mode is active.")]
    [SerializeField] private Graphic travelModeActiveIndicator;

    private bool _suppressUI;

    private void OnEnable()
    {
        ResolveMapManager();
        HookUI();

        if (mapManager != null)
        {
            mapManager.LayerModeChanged += HandleLayerModeChanged;
            mapManager.TravelModeChanged += HandleTravelModeChanged;
        }

        SyncUIFromManager();
    }

    private void OnDisable()
    {
        if (mapManager != null)
        {
            mapManager.LayerModeChanged -= HandleLayerModeChanged;
            mapManager.TravelModeChanged -= HandleTravelModeChanged;
        }
    }

    private void ResolveMapManager()
    {
        if (mapManager != null || !autoFindMapManager) return;

#if UNITY_2023_1_OR_NEWER
        mapManager = FindFirstObjectByType<MapManager>();
#else
        mapManager = FindObjectOfType<MapManager>();
#endif
    }

    private void HookUI()
    {
        if (regionalButton != null)
        {
            regionalButton.onClick.RemoveAllListeners();
            regionalButton.onClick.AddListener(() => SetLayer(MapLayer.Regional));
        }

        if (countryButton != null)
        {
            countryButton.onClick.RemoveAllListeners();
            countryButton.onClick.AddListener(() => SetLayer(MapLayer.Country));
        }

        if (duchyButton != null)
        {
            duchyButton.onClick.RemoveAllListeners();
            duchyButton.onClick.AddListener(() => SetLayer(MapLayer.Duchy));
        }

        if (lordshipButton != null)
        {
            lordshipButton.onClick.RemoveAllListeners();
            lordshipButton.onClick.AddListener(() => SetLayer(MapLayer.Lordship));
        }

        if (pointButton != null)
        {
            pointButton.onClick.RemoveAllListeners();
            pointButton.onClick.AddListener(() => SetLayer(MapLayer.Point));
        }

        if (geographyModeToggle != null)
        {
            geographyModeToggle.onValueChanged.RemoveAllListeners();
            geographyModeToggle.onValueChanged.AddListener(OnGeographyToggleChanged);
        }

        if (travelModeToggle != null)
        {
            travelModeToggle.onValueChanged.RemoveAllListeners();
            travelModeToggle.onValueChanged.AddListener(OnTravelToggleChanged);
        }

        if (travelModeButton != null)
        {
            travelModeButton.onClick.RemoveAllListeners();
            travelModeButton.onClick.AddListener(() =>
            {
                if (mapManager != null)
                    mapManager.ToggleTravelMode();
            });
        }
    }

    private void SetLayer(MapLayer layer)
    {
        if (mapManager == null) return;
        mapManager.SetActiveLayer(layer);
    }

    private void OnGeographyToggleChanged(bool enabled)
    {
        if (_suppressUI) return;
        if (mapManager == null) return;
        mapManager.SetGeographyMode(enabled);
    }

    private void OnTravelToggleChanged(bool enabled)
    {
        if (_suppressUI) return;
        if (mapManager == null) return;
        mapManager.SetTravelMode(enabled);
    }

    private void HandleLayerModeChanged(MapLayer selectedLayer, bool geoMode)
    {
        SyncUIFromManager();
    }

    private void HandleTravelModeChanged(bool travelMode)
    {
        SyncUIFromManager();
    }

    private void SyncUIFromManager()
    {
        if (mapManager == null) return;

        _suppressUI = true;
        try
        {
            if (geographyModeToggle != null)
                geographyModeToggle.isOn = mapManager.GeographyMode;

            if (travelModeToggle != null)
                travelModeToggle.isOn = mapManager.TravelMode;

            if (travelModeActiveIndicator != null)
                travelModeActiveIndicator.enabled = mapManager.TravelMode;
        }
        finally
        {
            _suppressUI = false;
        }
    }
}
