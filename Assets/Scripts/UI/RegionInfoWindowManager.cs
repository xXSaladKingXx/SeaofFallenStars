using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionInfoWindowManager : MonoBehaviour
{
    private enum PanelTab { Main, Geography, Culture }

    [Header("Header")]
    [SerializeField] private TMP_Text headerTitleText;
    [SerializeField] private Button closeButton;

    [Header("Tab Buttons")]
    [SerializeField] private Button mainTabButton;
    [SerializeField] private Button geographyTabButton;
    [SerializeField] private Button cultureTabButton;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject geographyPanel;
    [SerializeField] private GameObject culturePanel;

    [Header("Main Tab UI")]
    [SerializeField] private TMP_Text regionNameText;
    [SerializeField] private TMP_Text regionDescriptionText;

    [Header("Geography Tab UI")]
    [SerializeField] private Button geoMapButton;
    [SerializeField] private TMP_Text geoCountrySizesText;

    [SerializeField] private TMP_Text geoRegionTotalAreaText;
    [SerializeField] private TMP_Dropdown geoWildernessDropdown;
    [SerializeField] private TMP_Dropdown geoNaturalFormationsDropdown;
    [SerializeField] private TMP_Dropdown geoRuinsDropdown;
    [SerializeField] private TMP_Text geoWildernessTerrainPercentText;

    [Header("Culture Tab UI")]
    [SerializeField] private TMP_Dropdown cultureDropdown;
    [SerializeField] private TMP_Text cultureDescriptionText; // optional: assign if you have it
    // Added: dropdowns and description text for races, languages, and religions.
    [SerializeField] private TMP_Dropdown raceDropdown;
    [SerializeField] private TMP_Text raceDescriptionText;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private TMP_Text languageDescriptionText;
    [SerializeField] private TMP_Dropdown religionDropdown;
    [SerializeField] private TMP_Text religionDescriptionText;

    // === SubInfo ===
    [Header("Sub Info Panel")]
    [SerializeField] private GameObject subInfoPrefab;

    // Root canvas reference for subinfo spawning
    private Canvas _rootCanvas;

    [Header("Map Panel Prefab (Optional)")]
    [SerializeField] private GameObject mapPanelPrefab;

    [Header("Area Conversion")]
    [Tooltip("Miles per Unity unit (used to convert collider world-units into sq miles).")]
    [SerializeField] private float unityUnitsToMiles = 1f;

    private MapPoint _regionPoint;
    private Collider2D _regionBoundary;
    private RegionInfoData _data;

    private readonly List<MapPoint> _countries = new List<MapPoint>();
    private readonly List<MapPoint> _wilderness = new List<MapPoint>();
    private readonly List<MapPoint> _naturalFormations = new List<MapPoint>();
    private readonly List<MapPoint> _ruins = new List<MapPoint>();

    private bool _wired;

    // Default placeholder for dropdowns. Use simple ASCII characters to avoid font issues.
    private const string DROPDOWN_DEFAULT = "- Select -";

    private static readonly BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public void Initialize(MapPoint regionPoint)
    {
        _regionPoint = regionPoint;
        _regionBoundary = _regionPoint != null ? _regionPoint.regionBoundaryCollider : null;

        _data = _regionPoint != null ? _regionPoint.GetRegionInfoData() : null;

        // Find root canvas for spawning subinfo panels
        if (_rootCanvas == null)
        {
            _rootCanvas = GetComponentInParent<Canvas>(true);
        }

        WireUIOnce();
        RefreshAll();
        ShowTab(PanelTab.Main);
    }

    private void WireUIOnce()
    {
        if (_wired) return;
        _wired = true;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }

        if (mainTabButton != null)
        {
            mainTabButton.onClick.RemoveAllListeners();
            mainTabButton.onClick.AddListener(() => ShowTab(PanelTab.Main));
        }

        if (geographyTabButton != null)
        {
            geographyTabButton.onClick.RemoveAllListeners();
            geographyTabButton.onClick.AddListener(() =>
            {
                ShowTab(PanelTab.Geography);
                RefreshGeographyTab();
            });
        }

        if (cultureTabButton != null)
        {
            cultureTabButton.onClick.RemoveAllListeners();
            cultureTabButton.onClick.AddListener(() =>
            {
                ShowTab(PanelTab.Culture);
                RefreshCultureTab();
            });
        }

        if (geoMapButton != null)
        {
            geoMapButton.onClick.RemoveAllListeners();
            geoMapButton.onClick.AddListener(OpenRegionMapPanel);
        }

        if (geoWildernessDropdown != null)
        {
            geoWildernessDropdown.onValueChanged.RemoveAllListeners();
            geoWildernessDropdown.onValueChanged.AddListener(i => OnGeoDropdownSelected(_wilderness, geoWildernessDropdown, i));
        }

        if (geoNaturalFormationsDropdown != null)
        {
            geoNaturalFormationsDropdown.onValueChanged.RemoveAllListeners();
            geoNaturalFormationsDropdown.onValueChanged.AddListener(i => OnGeoDropdownSelected(_naturalFormations, geoNaturalFormationsDropdown, i));
        }

        if (geoRuinsDropdown != null)
        {
            geoRuinsDropdown.onValueChanged.RemoveAllListeners();
            geoRuinsDropdown.onValueChanged.AddListener(i => OnGeoDropdownSelected(_ruins, geoRuinsDropdown, i));
        }
    }

    private void ShowTab(PanelTab tab)
    {
        if (mainPanel != null) mainPanel.SetActive(tab == PanelTab.Main);
        if (geographyPanel != null) geographyPanel.SetActive(tab == PanelTab.Geography);
        if (culturePanel != null) culturePanel.SetActive(tab == PanelTab.Culture);
    }

    private void RefreshAll()
    {
        RefreshHeader();
        RefreshMainTab();
        RefreshGeographyTab();
        RefreshCultureTab();
    }

    private void RefreshHeader()
    {
        if (headerTitleText == null) return;

        string title =
            !string.IsNullOrWhiteSpace(_data?.displayName) ? _data.displayName :
            _regionPoint != null && !string.IsNullOrWhiteSpace(_regionPoint.displayName) ? _regionPoint.displayName :
            "Region";

        headerTitleText.text = title;
    }

    private void RefreshMainTab()
    {
        if (_regionPoint == null) return;

        if (regionNameText != null)
        {
            regionNameText.text =
                !string.IsNullOrWhiteSpace(_data?.displayName) ? _data.displayName :
                _regionPoint.displayName ?? _regionPoint.pointId ?? _regionPoint.name;
        }

        if (regionDescriptionText != null)
        {
            regionDescriptionText.text =
                !string.IsNullOrWhiteSpace(_data?.main?.description) ? _data.main.description :
                _regionPoint.mainDescription ?? "";
        }
    }

    private void RefreshCultureTab()
    {
        // Build dynamic culture/race/language/religion distribution from child populated settlements.
        // If any of the dropdowns are not assigned, skip those categories.
        // Compute aggregated distributions across all child populated MapPoints.
        var cultureCounts = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var raceCounts = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var languageCounts = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var religionCounts = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        int settlementCount = 0; // number of populated settlements considered
        float totalPopulation = 0f; // sum of populations across settlements

        // Traverse all descendants of the region to find populated settlements.
        var stack = new Stack<MapPoint>();
        if (_regionPoint != null)
        {
            var firstChildren = _regionPoint.GetChildren();
            if (firstChildren != null)
            {
                foreach (var c in firstChildren)
                    if (c != null) stack.Push(c);
            }
        }
        var visited = new HashSet<MapPoint>();
        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (p == null || !visited.Add(p)) continue;

            var kids = p.GetChildren();
            if (kids != null)
            {
                foreach (var k in kids)
                    if (k != null) stack.Push(k);
            }

            // Only consider populated points
            try
            {
                // Use Settlement info kind instead of Populated, as per user instructions
                if (p.infoKind == MapPoint.InfoKind.Settlement)
                {
                    // Attempt to get settlement info
                    var sInfo = p.GetSettlementInfoData();
                    if (sInfo != null)
                    {
                        settlementCount++;
                        // Use population to weight contributions; default to 1 if not specified
                        int pop = 0;
                        try
                        {
                            pop = sInfo?.main?.population ?? 0;
                        }
                        catch
                        {
                            pop = 0;
                        }
                        float fpop = pop > 0 ? (float)pop : 0f;
                        totalPopulation += fpop;
                        // Culture distribution: use distribution if available, else fallback to single culture
                        var cul = sInfo.cultural;
                        if (cul != null)
                        {
                            var cd = cul.cultureDistribution;
                            if (cd != null && cd.Count > 0)
                            {
                                foreach (var pe in cd)
                                {
                                    if (pe == null || string.IsNullOrWhiteSpace(pe.key)) continue;
                                    float existing = 0f;
                                    cultureCounts.TryGetValue(pe.key, out existing);
                                    // weight by population and percent
                                    cultureCounts[pe.key] = existing + (pe.percent / 100f) * fpop;
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(cul.culture))
                            {
                                float existing = 0f;
                                cultureCounts.TryGetValue(cul.culture, out existing);
                                cultureCounts[cul.culture] = existing + fpop;
                            }
                            // Race distribution
                            var rd = cul.raceDistribution;
                            if (rd != null && rd.Count > 0)
                            {
                                foreach (var pe in rd)
                                {
                                    if (pe == null || string.IsNullOrWhiteSpace(pe.key)) continue;
                                    float existing = 0f;
                                    raceCounts.TryGetValue(pe.key, out existing);
                                    raceCounts[pe.key] = existing + (pe.percent / 100f) * fpop;
                                }
                            }
                            // Languages: get list via reflection
                            List<string> langList = null;
                            object langObj;
                            if (TryGetValueByPath(sInfo, "cultural.languages", out langObj) && langObj is IEnumerable<string> langEnum)
                                langList = new List<string>(langEnum);
                            if (langList != null)
                            {
                                foreach (var lang in langList)
                                {
                                    if (string.IsNullOrWhiteSpace(lang)) continue;
                                    float existing = 0f;
                                    languageCounts.TryGetValue(lang, out existing);
                                    // treat each language as full population
                                    languageCounts[lang] = existing + fpop;
                                }
                            }
                            // Religions: get list via reflection
                            List<string> relList = null;
                            object relObj;
                            if (TryGetValueByPath(sInfo, "cultural.religions", out relObj) && relObj is IEnumerable<string> relEnum)
                                relList = new List<string>(relEnum);
                            if (relList != null)
                            {
                                foreach (var rel in relList)
                                {
                                    if (string.IsNullOrWhiteSpace(rel)) continue;
                                    float existing = 0f;
                                    religionCounts.TryGetValue(rel, out existing);
                                    religionCounts[rel] = existing + fpop;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore exceptions from MapPoint methods
            }
        }

        // Helper to populate dropdowns with aggregated distribution
        void PopulateDistributionDropdown(TMP_Dropdown dd, TMP_Text descText, Dictionary<string, float> counts)
        {
            if (dd == null) return;
            dd.onValueChanged.RemoveAllListeners();
            dd.ClearOptions();
            var opts = new List<string> { DROPDOWN_DEFAULT };
            var keysList = new List<string>();
            foreach (var kv in counts)
            {
                // Use weighted total population to compute percentages (kv.Value stores weighted population contribution)
                float pct = (totalPopulation > 0f) ? (kv.Value / totalPopulation * 100f) : 0f;
                keysList.Add($"{kv.Key} ({pct:0.#}%)");
            }
            // sort by descending percent
            keysList.Sort((a, b) =>
            {
                float pa = 0f;
                float pb = 0f;
                int idxA = a.LastIndexOf('(');
                int idxAPer = a.LastIndexOf('%');
                if (idxA >= 0 && idxAPer > idxA && float.TryParse(a.Substring(idxA + 1, idxAPer - idxA - 1), out float tmpA))
                    pa = tmpA;
                int idxB = b.LastIndexOf('(');
                int idxBPer = b.LastIndexOf('%');
                if (idxB >= 0 && idxBPer > idxB && float.TryParse(b.Substring(idxB + 1, idxBPer - idxB - 1), out float tmpB))
                    pb = tmpB;
                return pb.CompareTo(pa);
            });
            opts.AddRange(keysList);
            dd.AddOptions(opts);
            dd.value = 0;
            dd.RefreshShownValue();
            if (descText != null)
                descText.text = "";
            dd.onValueChanged.AddListener(idx =>
            {
                if (idx <= 0) return;
                int listIndex = idx - 1;
                if (listIndex < 0 || listIndex >= keysList.Count) return;
                string entry = keysList[listIndex];
                // Clear description text since sub-info panel will handle details
                if (descText != null)
                    descText.text = "";
                // Extract the key name before the percentage and parentheses
                string keyName = entry;
                int parenIndex = entry.LastIndexOf('(');
                if (parenIndex > 0)
                    keyName = entry.Substring(0, parenIndex).Trim();
                // Show sub-info panel for the selected item
                ShowSubInfo(keyName);
            });
        }

        // Populate each dropdown
        PopulateDistributionDropdown(cultureDropdown, cultureDescriptionText, cultureCounts);
        PopulateDistributionDropdown(raceDropdown, raceDescriptionText, raceCounts);
        PopulateDistributionDropdown(languageDropdown, languageDescriptionText, languageCounts);
        PopulateDistributionDropdown(religionDropdown, religionDescriptionText, religionCounts);
    }

    private void RefreshGeographyTab()
    {
        if (_regionPoint == null) return;

        float regionAreaSqMi = ComputeAreaSqMiles(_regionBoundary);

        if (geoRegionTotalAreaText != null)
        {
            geoRegionTotalAreaText.text = regionAreaSqMi > 0.0001f
                ? $"Region Total Area: {regionAreaSqMi:0.##} sq mi"
                : "Region Total Area: (no boundary collider)";
        }

        RebuildCountries();
        if (geoCountrySizesText != null)
            geoCountrySizesText.text = BuildCountrySizesText();

        RebuildUnpopulatedSubregions();

        PopulateDropdown(geoWildernessDropdown, _wilderness);
        PopulateDropdown(geoNaturalFormationsDropdown, _naturalFormations);
        PopulateDropdown(geoRuinsDropdown, _ruins);

        if (geoWildernessTerrainPercentText != null)
            geoWildernessTerrainPercentText.text = BuildWildernessTerrainPercentText(regionAreaSqMi);
    }

    private void RebuildCountries()
    {
        _countries.Clear();

        var children = _regionPoint.GetChildren();
        if (children != null)
        {
            foreach (var c in children)
            {
                if (c == null) continue;
                if (c.layer == MapLayer.Country)
                    _countries.Add(c);
            }
        }

        if (_countries.Count == 0 && _regionBoundary != null)
        {
            foreach (var p in FindAllMapPoints(includeInactive: true))
            {
                if (p == null) continue;
                if (p.layer != MapLayer.Country) continue;

                var col = p.regionBoundaryCollider != null ? p.regionBoundaryCollider : p.GetComponent<Collider2D>();
                if (col == null) continue;

                Vector2 center = col.bounds.center;
                if (_regionBoundary.OverlapPoint(center))
                    _countries.Add(p);
            }
        }
    }

    private string BuildCountrySizesText()
    {
        if (_countries.Count == 0)
            return "No countries found under this region.";

        float regionAreaSqMi = ComputeAreaSqMiles(_regionBoundary);

        var lines = new List<string>();
        lines.Add("Country Sizes:");
        lines.Add("");

        foreach (var c in _countries)
        {
            float area = ComputeAreaSqMiles(c != null ? (c.regionBoundaryCollider ?? c.GetComponent<Collider2D>()) : null);
            if (area <= 0.0001f) continue;

            string name = !string.IsNullOrWhiteSpace(c.displayName) ? c.displayName : (c.pointId ?? c.name);
            if (regionAreaSqMi > 0.0001f)
                lines.Add($" {name}: {area:0.##} sq mi ({(area / regionAreaSqMi * 100f):0.#}%)");
            else
                lines.Add($" {name}: {area:0.##} sq mi");
        }

        return string.Join("\n", lines);
    }

    private void RebuildUnpopulatedSubregions()
    {
        _wilderness.Clear();
        _naturalFormations.Clear();
        _ruins.Clear();

        var stack = new Stack<MapPoint>();
        var visited = new HashSet<MapPoint>();

        var firstChildren = _regionPoint.GetChildren();
        if (firstChildren != null)
        {
            foreach (var c in firstChildren)
                if (c != null) stack.Push(c);
        }

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (p == null) continue;
            if (!visited.Add(p)) continue;

            var kids = p.GetChildren();
            if (kids != null)
            {
                foreach (var k in kids)
                    if (k != null) stack.Push(k);
            }

            if (p.infoKind != MapPoint.InfoKind.Unpopulated)
                continue;

            // classify based on MapPoint subtype (now your source of truth)
            switch (p.unpopulatedSubtype)
            {
                case MapPoint.UnpopulatedSubtype.Water:
                    _naturalFormations.Add(p);
                    break;
                case MapPoint.UnpopulatedSubtype.Ruins:
                    _ruins.Add(p);
                    break;
                default:
                    _wilderness.Add(p);
                    break;
            }
        }

        _wilderness.Sort(CompareByDisplayName);
        _naturalFormations.Sort(CompareByDisplayName);
        _ruins.Sort(CompareByDisplayName);
    }

    private void PopulateDropdown(TMP_Dropdown dd, List<MapPoint> points)
    {
        if (dd == null) return;

        dd.ClearOptions();

        var options = new List<string> { DROPDOWN_DEFAULT };
        if (points != null)
        {
            foreach (var p in points)
            {
                if (p == null) continue;
                options.Add(!string.IsNullOrWhiteSpace(p.displayName) ? p.displayName : (p.pointId ?? p.name));
            }
        }

        dd.AddOptions(options);
        dd.value = 0;
        dd.RefreshShownValue();
    }

    private void OnGeoDropdownSelected(List<MapPoint> list, TMP_Dropdown dd, int index)
    {
        if (index <= 0) return;

        int listIndex = index - 1;
        if (list == null || listIndex < 0 || listIndex >= list.Count) return;

        var target = list[listIndex];
        if (target == null) return;

        if (dd != null)
        {
            dd.SetValueWithoutNotify(0);
            dd.RefreshShownValue();
        }

        SimulateMapPointClick(target);
    }

    private void SimulateMapPointClick(MapPoint target)
    {
        if (target == null) return;

        var mapManager = FindObjectOfType<MapManager>();
        if (mapManager == null) return;

        var mmType = mapManager.GetType();
        var mi = mmType.GetMethod("OnPointClicked", AnyInstance, null, new[] { typeof(MapPoint) }, null);

        if (mi != null)
        {
            mi.Invoke(mapManager, new object[] { target });
            return;
        }

        mi = mmType.GetMethod("OpenOrFocusWindow", AnyInstance, null, new[] { typeof(MapPoint) }, null);
        if (mi != null)
            mi.Invoke(mapManager, new object[] { target });
    }

    private string BuildWildernessTerrainPercentText(float regionAreaSqMi)
    {
        if (_wilderness.Count == 0)
            return "No wilderness subregions found.";

        if (regionAreaSqMi <= 0.0001f)
            return "Terrain breakdown unavailable (region area is 0).";

        var areaByTerrain = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in _wilderness)
        {
            if (p == null) continue;

            var data = p.GetUnpopulatedInfoData();
            string terrainType = data != null && data.geography != null ? (data.geography.terrainType ?? "") : "";
            string key = string.IsNullOrWhiteSpace(terrainType) ? "Unknown" : terrainType.Trim();

            float area = ComputeAreaSqMiles(p.regionBoundaryCollider ?? p.GetComponent<Collider2D>());
            if (area <= 0.0001f) continue;

            if (!areaByTerrain.ContainsKey(key))
                areaByTerrain[key] = 0f;

            areaByTerrain[key] += area;
        }

        if (areaByTerrain.Count == 0)
            return "No wilderness terrain areas could be computed (missing polygon colliders?).";

        var entries = new List<KeyValuePair<string, float>>(areaByTerrain);
        entries.Sort((a, b) => b.Value.CompareTo(a.Value));

        var lines = new List<string>();
        lines.Add("Wilderness Terrain Coverage (of Region):");

        foreach (var kv in entries)
        {
            float pct = Mathf.Clamp01(kv.Value / regionAreaSqMi) * 100f;
            lines.Add($" {kv.Key}: {pct:0.#}% ({kv.Value:0.##} sq mi)");
        }

        return string.Join("\n", lines);
    }

    private float ComputeAreaSqMiles(Collider2D col)
    {
        if (col == null) return 0f;

        float worldArea = 0f;

        var poly = col as PolygonCollider2D;
        if (poly == null)
            poly = col.GetComponent<PolygonCollider2D>();

        if (poly != null) worldArea = ComputePolygonAreaWorldUnits(poly);
        else
        {
            var b = col.bounds;
            worldArea = Mathf.Abs(b.size.x * b.size.y);
        }

        float milesPerUnit = Mathf.Max(0.000001f, unityUnitsToMiles);
        return worldArea * milesPerUnit * milesPerUnit;
    }

    private float ComputePolygonAreaWorldUnits(PolygonCollider2D poly)
    {
        if (poly == null) return 0f;

        float total = 0f;

        for (int path = 0; path < poly.pathCount; path++)
        {
            var pts = poly.GetPath(path);
            if (pts == null || pts.Length < 3) continue;

            float sum = 0f;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 a = poly.transform.TransformPoint(pts[i]);
                Vector2 b = poly.transform.TransformPoint(pts[(i + 1) % pts.Length]);
                sum += (a.x * b.y - b.x * a.y);
            }

            total += sum;
        }

        return Mathf.Abs(total) * 0.5f;
    }

    private void OpenRegionMapPanel()
    {
        if (mapPanelPrefab == null || _regionPoint == null) return;

        var mapPanel = Instantiate(mapPanelPrefab, transform.parent);

        // Try to initialize the map panel in a flexible way:
        // Initialize(RegionInfoData) -> Initialize(MapPoint) -> Initialize(string mapUrlOrPath)
        bool initialized = TryInvokeInitialize(mapPanel, _data)
                        || TryInvokeInitialize(mapPanel, _regionPoint)
                        || TryInvokeInitialize(mapPanel, _data != null ? _data.mapUrlOrPath : null);

        if (!initialized)
            Debug.LogWarning("[RegionInfoWindowManager] Map panel spawned, but no compatible Initialize(...) was found.");
    }

    private static bool TryInvokeInitialize(GameObject root, object arg)
    {
        if (root == null) return false;

        var comps = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var mb = comps[i];
            if (mb == null) continue;

            var m = mb.GetType().GetMethod(
                "Initialize",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { arg != null ? arg.GetType() : typeof(string) },
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

    private static int CompareByDisplayName(MapPoint a, MapPoint b)
    {
        string an = a != null ? (a.displayName ?? a.pointId ?? a.name) : "";
        string bn = b != null ? (b.displayName ?? b.pointId ?? b.name) : "";
        return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
    }

    private static List<MapPoint> FindAllMapPoints(bool includeInactive)
    {
#if UNITY_2022_2_OR_NEWER
        var inactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
        return new List<MapPoint>(UnityEngine.Object.FindObjectsByType<MapPoint>(inactive, FindObjectsSortMode.None));
#else
        return new List<MapPoint>(GameObject.FindObjectsOfType<MapPoint>(includeInactive));
#endif
    }

    // Helpers copied from InfoWindowManager: reflection-based retrieval of values by path.
    private static bool TryGetValueByPath(object root, string path, out object value)
    {
        value = null;
        if (root == null || string.IsNullOrWhiteSpace(path)) return false;
        object current = root;
        foreach (string part in path.Split('.'))
        {
            if (string.IsNullOrWhiteSpace(part)) return false;
            if (!TryGetMemberValue(current, part, out current) || current == null)
                return false;
        }
        value = current;
        return true;
    }

    private static bool TryGetMemberValue(object obj, string memberName, out object value)
    {
        value = null;
        if (obj == null) return false;
        Type t = obj.GetType();
        // Try property first
        PropertyInfo prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (prop != null)
        {
            try { value = prop.GetValue(obj); return true; } catch { return false; }
        }
        // Try field next
        FieldInfo field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (field != null)
        {
            try { value = field.GetValue(obj); return true; } catch { return false; }
        }
        return false;
    }

    // === SubInfo Methods (copied/adapted from InfoWindowManager) ===
    /// <summary>
    /// Spawn a subinfo panel for the given key. Uses reflection to call Initialize/Init/Set methods on the prefab's components.
    /// </summary>
    /// <param name="key">The identifier of the culture/race/language/religion to show.</param>
    private void ShowSubInfo(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || subInfoPrefab == null) return;
        // Use root canvas or fallback to this object's root
        Transform parent = (_rootCanvas != null ? _rootCanvas.transform : transform.root);
        GameObject panel = Instantiate(subInfoPrefab, parent);

        // Attempt to call Initialize/Init/Set on components
        foreach (MonoBehaviour mb in panel.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            Type t = mb.GetType();
            // Try Initialize(string)
            if (TryInvokeInitializeSubInfo(mb, t, "Initialize", key)) return;
            // Try Init(string)
            if (TryInvokeInitializeSubInfo(mb, t, "Init", key)) return;
            // Try Set(string)
            if (TryInvokeInitializeSubInfo(mb, t, "Set", key)) return;
        }
    }

    /// <summary>
    /// Try invoking a method with a single string parameter on the target object.
    /// </summary>
    private bool TryInvokeInitializeSubInfo(object target, Type targetType, string methodName, string key)
    {
        try
        {
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (MethodInfo m in methods)
            {
                if (m.Name != methodName) continue;
                var ps = m.GetParameters();
                if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
                {
                    m.Invoke(target, new object[] { key });
                    return true;
                }
            }
        }
        catch
        {
            // ignore
        }
        return false;
    }
}
