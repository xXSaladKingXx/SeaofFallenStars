using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SeaOfFallenStars.WorldData;

public class UnpopulatedInfoWindowManager : MonoBehaviour
{
    public enum InfoPanelType
    {
        Main,
        Flora,
        Fauna,
        History,
        Culture,
        Water
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject floraPanel;
    [SerializeField] private GameObject faunaPanel;
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private GameObject culturePanel;
    [SerializeField] private GameObject waterPanel;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [SerializeField] private Button mainTabButton;
    [SerializeField] private Button floraTabButton;
    [SerializeField] private Button faunaTabButton;
    [SerializeField] private Button historyTabButton;
    [SerializeField] private Button cultureTabButton;
    [SerializeField] private Button waterTabButton;

    [SerializeField] private Button mapButton;                  // shown for Ruins + Wilderness
    [SerializeField] private GameObject mapPanelPrefab;         // optional, for Map button instantiation

    [Header("Main UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text subtypeText;
    [SerializeField] private TMP_Text terrainTypeText;
    [SerializeField] private TMP_Text derivedAreaText;
    [SerializeField] private TMP_Text terrainBreakdownText;

    [Header("Tab UI")]
    [SerializeField] private TMP_Text floraText;
    [SerializeField] private TMP_Text faunaText;
    [SerializeField] private TMP_Text historyText;
    [SerializeField] private TMP_Text cultureText;
    [SerializeField] private TMP_Text waterText;

    private MapPoint _point;
    private UnpopulatedInfoData _data;

    private static readonly HashSet<string> AllowedWildernessTerrainTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Fertile Plains",
            "Grasslands / Steppe",
            "Riverlands / Floodplains",
            "Hills",
            "Mountains / Highlands",
            "Forest (Temperate)",
            "Deep Forest",
            "Marsh / Swamp",
            "Coastal",
            "Desert",
            "Tundra / Cold Wastes"
        };

    private static readonly BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public void Initialize(MapPoint point)
    {
        _point = point;
        _data = _point != null ? _point.GetUnpopulatedAreaData() : null;

        WireUI();
        RefreshAll();

        ConfigureTabsForSubtype();
        Show(InfoPanelType.Main);
    }

    private void WireUI()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }

        WireTab(mainTabButton, InfoPanelType.Main);
        WireTab(floraTabButton, InfoPanelType.Flora);
        WireTab(faunaTabButton, InfoPanelType.Fauna);
        WireTab(historyTabButton, InfoPanelType.History);
        WireTab(cultureTabButton, InfoPanelType.Culture);
        WireTab(waterTabButton, InfoPanelType.Water);

        if (mapButton != null)
        {
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(OpenMapPanel);
        }
    }

    private void WireTab(Button btn, InfoPanelType type)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => Show(type));
    }

    private void RefreshAll()
    {
        if (titleText != null)
            titleText.text = _data?.displayName ?? (_point != null ? _point.displayName : "Unknown");

        if (descriptionText != null)
            descriptionText.text = _data?.main?.description ?? "";

        var subtype = _point != null ? _point.unpopulatedSubtype : MapPoint.UnpopulatedSubtype.Wilderness;

        if (subtypeText != null)
            subtypeText.text = $"Subtype: {subtype}";

        string terrain = _data?.geography?.terrainType ?? "";
        if (terrainTypeText != null)
        {
            if (subtype == MapPoint.UnpopulatedSubtype.Wilderness)
            {
                if (!string.IsNullOrWhiteSpace(terrain) && !AllowedWildernessTerrainTypes.Contains(terrain.Trim()))
                {
                    terrainTypeText.text =
                        $"Terrain: {terrain}\n<color=#ff8080>WARNING:</color> Not in allowed list.";
                }
                else
                {
                    terrainTypeText.text = !string.IsNullOrWhiteSpace(terrain)
                        ? $"Terrain: {terrain}"
                        : "Terrain: (not set)";
                }
            }
            else if (subtype == MapPoint.UnpopulatedSubtype.Water)
            {
                // Water might still be Coastal etc, but not required.
                terrainTypeText.text = !string.IsNullOrWhiteSpace(terrain)
                    ? $"Surrounding Terrain: {terrain}"
                    : "";
            }
            else
            {
                // Ruins: optional
                terrainTypeText.text = !string.IsNullOrWhiteSpace(terrain)
                    ? $"Surrounding Terrain: {terrain}"
                    : "";
            }
        }

        // Derived area + terrain breakdown (if your calculator provides it)
        var computed = _point != null ? UnpopulatedAreaCalculator.Compute(_point) : null;
        if (computed != null)
        {
            if (derivedAreaText != null)
                derivedAreaText.text = $"Total Area (recursive): {computed.totalAreaSqMi:0.#} sq mi";

            if (terrainBreakdownText != null)
            {
                if (computed.areaByTerrain != null && computed.areaByTerrain.Count > 0)
                {
                    var lines = new List<string>();
                    foreach (var kv in computed.areaByTerrain)
                        lines.Add($"{kv.Key}: {kv.Value:0.#} sq mi");
                    terrainBreakdownText.text = string.Join("\n", lines);
                }
                else
                {
                    terrainBreakdownText.text = "";
                }
            }
        }
        else
        {
            if (derivedAreaText != null) derivedAreaText.text = "";
            if (terrainBreakdownText != null) terrainBreakdownText.text = "";
        }

        // Tab contents
        if (floraText != null) floraText.text = _data?.nature?.flora ?? "";
        if (faunaText != null) faunaText.text = _data?.nature?.fauna ?? "";

        if (historyText != null)
        {
            string notes = _data?.history?.notes ?? "";
            var entries = _data?.history?.timelineEntries ?? Array.Empty<string>();
            historyText.text = entries.Length > 0
                ? (notes + (string.IsNullOrWhiteSpace(notes) ? "" : "\n\n") + string.Join("\n• ", PrependBullet(entries)))
                : notes;
        }

        if (cultureText != null)
        {
            var c = _data?.culture;
            if (c == null)
            {
                cultureText.text = "";
            }
            else
            {
                cultureText.text =
                    BuildSection("Notes", c.notes) +
                    BuildArraySection("Peoples", c.peoples) +
                    BuildArraySection("Factions", c.factions) +
                    BuildArraySection("Languages", c.languages) +
                    BuildSection("Customs", c.customs) +
                    BuildArraySection("Rumors", c.rumors);
            }
        }

        if (waterText != null)
        {
            var w = _data?.water;
            if (w == null)
            {
                waterText.text = "";
            }
            else
            {
                waterText.text =
                    BuildLine("Water Body Type", w.waterBodyType) +
                    BuildLine("Water Type", w.waterType) +
                    BuildLine("Depth", w.depth) +
                    BuildSection("Currents", w.currents) +
                    BuildSection("Hazards", w.hazards) +
                    BuildArraySection("Notable Features", w.notableFeatures) +
                    BuildSection("Notes", w.notes);
            }
        }
    }

    private void ConfigureTabsForSubtype()
    {
        var subtype = _point != null ? _point.unpopulatedSubtype : MapPoint.UnpopulatedSubtype.Wilderness;

        // Default off
        SetActiveSafe(floraTabButton, false);
        SetActiveSafe(faunaTabButton, false);
        SetActiveSafe(historyTabButton, false);
        SetActiveSafe(cultureTabButton, false);
        SetActiveSafe(waterTabButton, false);

        // Panels will be controlled by Show(), but map button can be gated here.
        bool showMapButton = false;

        switch (subtype)
        {
            case MapPoint.UnpopulatedSubtype.Ruins:
                // Ruins: History + Culture + Map button
                SetActiveSafe(historyTabButton, true);
                SetActiveSafe(cultureTabButton, true);
                showMapButton = true;
                break;

            case MapPoint.UnpopulatedSubtype.Water:
                // Water: Water + Flora + Fauna (no History/Culture requested)
                SetActiveSafe(waterTabButton, true);
                SetActiveSafe(floraTabButton, true);
                SetActiveSafe(faunaTabButton, true);
                showMapButton = false;
                break;

            default:
            case MapPoint.UnpopulatedSubtype.Wilderness:
                // Wilderness: Flora + Fauna + History + Map button
                SetActiveSafe(floraTabButton, true);
                SetActiveSafe(faunaTabButton, true);
                SetActiveSafe(historyTabButton, true);
                showMapButton = true;
                break;
        }

        if (mapButton != null)
        {
            // Only show if subtype wants it AND there's something meaningful to open.
            bool hasSomethingToOpen = mapPanelPrefab != null || !string.IsNullOrWhiteSpace(_data?.mapUrlOrPath);
            mapButton.gameObject.SetActive(showMapButton && hasSomethingToOpen);
        }
    }

    private void Show(InfoPanelType type)
    {
        if (mainPanel != null) mainPanel.SetActive(type == InfoPanelType.Main);
        if (floraPanel != null) floraPanel.SetActive(type == InfoPanelType.Flora);
        if (faunaPanel != null) faunaPanel.SetActive(type == InfoPanelType.Fauna);
        if (historyPanel != null) historyPanel.SetActive(type == InfoPanelType.History);
        if (culturePanel != null) culturePanel.SetActive(type == InfoPanelType.Culture);
        if (waterPanel != null) waterPanel.SetActive(type == InfoPanelType.Water);

        transform.SetAsLastSibling();
    }

    private void OpenMapPanel()
    {
        // You said you don't have the prefab yet. This supports both cases:
        // - If you assign mapPanelPrefab later, it will instantiate it
        // - Otherwise it just logs (and you can decide how to handle mapUrlOrPath later)
        if (mapPanelPrefab == null)
        {
            Debug.LogWarning("[UnpopulatedInfoWindowManager] Map button pressed but mapPanelPrefab is not assigned.");
            return;
        }

        var mapPanel = Instantiate(mapPanelPrefab, transform.parent);

        // Try a few common initialization signatures:
        // Initialize(MapPoint) -> Initialize(UnpopulatedInfoData) -> Initialize(string mapUrlOrPath)
        bool initialized = TryInvokeInitialize(mapPanel, _point)
                        || TryInvokeInitialize(mapPanel, _data)
                        || TryInvokeInitialize(mapPanel, _data != null ? _data.mapUrlOrPath : null);

        if (!initialized)
            Debug.LogWarning("[UnpopulatedInfoWindowManager] Map panel spawned, but no compatible Initialize(...) was found.");
    }

    private static bool TryInvokeInitialize(GameObject root, object arg)
    {
        if (root == null) return false;

        var comps = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var mb = comps[i];
            if (mb == null) continue;

            Type argType = arg != null ? arg.GetType() : typeof(string);

            var m = mb.GetType().GetMethod(
                "Initialize",
                AnyInstance,
                binder: null,
                types: new[] { argType },
                modifiers: null
            );

            if (m != null)
            {
                m.Invoke(mb, new[] { arg });
                return true;
            }
        }

        return false;
    }

    private static void SetActiveSafe(Button btn, bool active)
    {
        if (btn == null) return;
        btn.gameObject.SetActive(active);
    }

    private static string BuildLine(string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return $"{label}: {value}\n";
    }

    private static string BuildSection(string label, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return $"{label}:\n{text}\n\n";
    }

    private static string BuildArraySection(string label, string[] arr)
    {
        if (arr == null || arr.Length == 0) return "";
        return $"{label}:\n• {string.Join("\n• ", arr)}\n\n";
    }

    private static IEnumerable<string> PrependBullet(string[] entries)
    {
        for (int i = 0; i < entries.Length; i++)
            yield return entries[i] ?? "";
    }
}
