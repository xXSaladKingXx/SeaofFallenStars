// LayerMenu.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
public class LayerMenu : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MapManager mapManager;

    [Header("Layer Buttons")]
    [SerializeField] private Button regionalButton;
    [SerializeField] private Button countryButton;
    [SerializeField] private Button duchyButton;
    [SerializeField] private Button lordshipButton;
    [SerializeField] private Button pointButton;

    [Header("Geography Mode Button")]
    [SerializeField] private Button geographyModeButton;
    [SerializeField] private Image geographyModeButtonImage;
    [SerializeField] private Color geoOffColor = Color.white;
    [SerializeField] private Color geoOnColor = new Color(0.6f, 0.9f, 1f, 1f);

    [Header("Optional: Highlight Active Layer")]
    [SerializeField] private bool highlightActiveLayer = true;
    [SerializeField] private Color layerOffColor = Color.white;
    [SerializeField] private Color layerOnColor = new Color(0.85f, 1f, 0.75f, 1f);

    [SerializeField] private Image regionalButtonImage;
    [SerializeField] private Image countryButtonImage;
    [SerializeField] private Image duchyButtonImage;
    [SerializeField] private Image lordshipButtonImage;
    [SerializeField] private Image pointButtonImage;

    [Header("Debug")]
    [SerializeField] private bool logButtonClicks = true;
    [SerializeField] private bool autoEnsureEventSystem = true;
    [SerializeField] private bool autoAddMapInputBlocker = true;

    private bool _wired;

    private void Awake()
    {
        if (autoEnsureEventSystem)
            EnsureEventSystemForUI();

        if (autoAddMapInputBlocker)
            EnsureMapInputBlocker();

        if (mapManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            mapManager = FindFirstObjectByType<MapManager>();
#else
            mapManager = FindObjectOfType<MapManager>();
#endif
        }

        // Fallback images if you didn't assign them
        if (regionalButtonImage == null && regionalButton != null) regionalButtonImage = regionalButton.GetComponent<Image>();
        if (countryButtonImage == null && countryButton != null) countryButtonImage = countryButton.GetComponent<Image>();
        if (duchyButtonImage == null && duchyButton != null) duchyButtonImage = duchyButton.GetComponent<Image>();
        if (lordshipButtonImage == null && lordshipButton != null) lordshipButtonImage = lordshipButton.GetComponent<Image>();
        if (pointButtonImage == null && pointButton != null) pointButtonImage = pointButton.GetComponent<Image>();

        WireButtonsOnce();

        if (mapManager != null)
            mapManager.LayerModeChanged += OnLayerModeChanged;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (mapManager != null)
            mapManager.LayerModeChanged -= OnLayerModeChanged;
    }

    private void OnLayerModeChanged(MapLayer layer, bool geoMode)
    {
        RefreshUI();
    }

    private void WireButtonsOnce()
    {
        if (_wired) return;
        _wired = true;

        WireLayerButton(regionalButton, MapLayer.Regional, "Regional");
        WireLayerButton(countryButton, MapLayer.Country, "Country");
        WireLayerButton(duchyButton, MapLayer.Duchy, "Duchy");
        WireLayerButton(lordshipButton, MapLayer.Lordship, "Lordship");
        WireLayerButton(pointButton, MapLayer.Point, "Point");

        if (geographyModeButton != null)
        {
            geographyModeButton.onClick.RemoveAllListeners();
            geographyModeButton.onClick.AddListener(() =>
            {
                if (logButtonClicks) Debug.Log("[LayerMenu] Click: Geography Mode Toggle");
                mapManager?.ToggleGeographyMode();
                RefreshUI();
            });
        }

        if (logButtonClicks)
            Debug.Log("[LayerMenu] Buttons wired.");
    }

    private void WireLayerButton(Button btn, MapLayer layer, string label)
    {
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (logButtonClicks) Debug.Log($"[LayerMenu] Click: {label} => SetActiveLayer({layer})");
            mapManager?.SetActiveLayer(layer);
            RefreshUI();
        });
    }

    private void RefreshUI()
    {
        if (mapManager == null) return;

        // Geography button tint
        if (geographyModeButtonImage != null)
            geographyModeButtonImage.color = mapManager.GeographyMode ? geoOnColor : geoOffColor;

        if (!highlightActiveLayer) return;

        SetLayerImage(regionalButtonImage, mapManager.ActiveLayer == MapLayer.Regional);
        SetLayerImage(countryButtonImage, mapManager.ActiveLayer == MapLayer.Country);
        SetLayerImage(duchyButtonImage, mapManager.ActiveLayer == MapLayer.Duchy);
        SetLayerImage(lordshipButtonImage, mapManager.ActiveLayer == MapLayer.Lordship);
        SetLayerImage(pointButtonImage, mapManager.ActiveLayer == MapLayer.Point);
    }

    private void SetLayerImage(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? layerOnColor : layerOffColor;
    }

    private void EnsureMapInputBlocker()
    {
        // Ensures UI blocks map clicks when pointer is over it.
        if (GetComponent<MapInputBlocker>() == null)
            gameObject.AddComponent<MapInputBlocker>();
    }

    private void EnsureEventSystemForUI()
    {
        // If there is no EventSystem, UI will not receive clicks and map input cannot detect UI blocking.
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif

            if (logButtonClicks)
                Debug.Log("[LayerMenu] Created EventSystem (was missing).");
        }
        else
        {
#if ENABLE_INPUT_SYSTEM
            // Ensure correct input module for the New Input System
            if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
                if (logButtonClicks)
                    Debug.Log("[LayerMenu] Added InputSystemUIInputModule to existing EventSystem.");
            }
#endif
        }

        // Also make sure the Canvas has a GraphicRaycaster (required for UI raycasts)
        var canvas = GetComponentInParent<Canvas>(true);
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (logButtonClicks)
                Debug.Log("[LayerMenu] Added GraphicRaycaster to Canvas.");
        }
    }
}
