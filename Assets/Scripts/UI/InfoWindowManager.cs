using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InfoWindowManager : MonoBehaviour
{
    public enum InfoPanelType
    {
        Main,
        Army,
        Economy,
        Cultural,
        History,
        Vassals,
        Geography,
        RealmManagement
    }

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject armyPanel;
    [SerializeField] private GameObject economyPanel;
    [SerializeField] private GameObject culturalPanel;
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private GameObject vassalsPanel;

    [SerializeField] private GameObject geographyPanel;         // NEW
    [SerializeField] private GameObject realmManagementPanel;   // NEW

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button mainButton;
    [SerializeField] private Button armyButton;
    [SerializeField] private Button economyButton;
    [SerializeField] private Button culturalButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button vassalsButton;

    [SerializeField] private Button geographyButton;            // NEW
    [SerializeField] private Button realmManagementButton;      // NEW (only shown when applicable)

    [SerializeField] private Button mapButton;

    // =========================================================
    // Main Tab UI
    // =========================================================
    [Header("Main Tab UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Main Tab UI (NEW - Ruler + Characters)")]
    [SerializeField] private TMP_Text mainRulerNameText;
    [SerializeField] private Button mainRulerButton;

    [Tooltip("Optional panel GameObject that contains the characters list UI (can be inside Main panel).")]
    [SerializeField] private GameObject charactersPanel;

    [Tooltip("Container where character rows will be instantiated.")]
    [SerializeField] private Transform charactersListContainer;

    [Tooltip("Prefab for a single character row (should include a Button and a TMP_Text somewhere).")]
    [SerializeField] private GameObject characterRowPrefab;

    [Tooltip("Optional: shown when there are no characters.")]
    [SerializeField] private TMP_Text charactersEmptyText;
    // New: dropdown for characters list. Assign a TMP_Dropdown in Inspector to replace the button list.
    [Header("Main Tab UI (Dropdown)")]
    [SerializeField] private TMP_Dropdown charactersDropdown;

    // =========================================================
    // Character Stat Panel spawning
    // =========================================================
    [Header("Character Stat Panel (Prefab)")]
    [SerializeField] private GameObject characterStatPanelPrefab;

    [Tooltip("Where to spawn the CharacterStatPanel. If null, spawns under the nearest parent Canvas.")]
    [SerializeField] private Transform characterPanelSpawnParent;

    // ===========================
    // Army Tab UI
    // ===========================
    [Header("Army Tab UI (NEW)")]
    [Tooltip("ArmyPanel/TitleText (optional; used if you want Army tab to have its own title)")]
    [SerializeField] private TMP_Text armyTitleText;

    [Tooltip("ArmyPanel/Body Text (optional; used for army description/notes)")]
    [SerializeField] private TMP_Text armyBodyText;

    [Tooltip("ArmyStatsPanel/TotalArmyCounter")]
    [SerializeField] private TMP_Text armyTotalArmyCounterText;

    [Tooltip("ArmyStatsPanel/CommanderNameText")]
    [SerializeField] private TMP_Text armyCommanderNameText;

    [Tooltip("ArmyStatsPanel/MenAtArmsList (container)")]
    [SerializeField] private Transform armyMenAtArmsListContainer;

    [Tooltip("Optional: if you prefer a single text block list instead of instantiating rows.")]
    [SerializeField] private TMP_Text armyMenAtArmsListText;

    [Tooltip("ArmyPanel/ArmyPopOutButton (optional; wired to no-op stub)")]
    [SerializeField] private Button armyPopOutButton;

    [Tooltip("ArmyStatsPanel/CommanderButton (optional; wired to no-op stub)")]
    [SerializeField] private Button armyCommanderButton;

    // ===========================
    // Economy Tab UI
    // ===========================
    [Header("Economy Tab UI (NEW)")]
    [SerializeField] private TMP_Text economyTitleText;
    [SerializeField] private TMP_Text economyBodyText;

    [Tooltip("Legacy/Optional fields (kept): previously used as generic economy text slots.")]
    [SerializeField] private TMP_Text economyIncomeText;
    [SerializeField] private TMP_Text economyExpensesText;
    [SerializeField] private TMP_Text economyNetText;
    [SerializeField] private TMP_Text economyTaxRateText;

    [Tooltip("EconomyPanel/EconomyPopOutButton (optional; wired to no-op stub)")]
    [SerializeField] private Button economyPopOutButton;

    [Header("Economy Tab UI (Matches JSON)")]
    [SerializeField] private TMP_Text economyTotalIncomePerMonthText;   // totalIncomePerMonth
    [SerializeField] private TMP_Text economyTotalTreasuryText;         // totalTreasury
    [SerializeField] private TMP_Text economyCourtExpensesText;         // courtExpenses (string)
    [SerializeField] private TMP_Text economyArmyExpensesText;          // armyExpenses (string)
    [SerializeField] private TMP_Text economyCurrentlyConstructingText; // currentlyConstructing list

    // ===========================
    // Cultural Tab UI
    // ===========================
    [Header("Cultural Tab UI (NEW)")]
    [SerializeField] private TMP_Text culturalTitleText;
    [SerializeField] private TMP_Text culturalBodyText;

    [Tooltip("Legacy/Optional detail fields (kept): these were placeholders in earlier drafts.")]
    [SerializeField] private TMP_Text culturalReligionText;
    [SerializeField] private TMP_Text culturalLanguageText;
    [SerializeField] private TMP_Text culturalTraditionsText;

    [Tooltip("CulturalPanel/CulturalPopOutButton (optional; wired to no-op stub)")]
    [SerializeField] private Button culturalPopOutButton;

    [Header("Cultural Tab UI (Matches JSON)")]
    [SerializeField] private TMP_Text culturalCultureText;                    // cultural.culture
    [SerializeField] private TMP_Text culturalPopulationDistributionText;      // cultural.populationDistribution
    [SerializeField] private TMP_Text culturalPrimaryTraitsText;               // cultural.primaryTraits list
    [SerializeField] private TMP_Text culturalRaceDistributionText;            // cultural.raceDistribution list
    [SerializeField] private TMP_Text culturalCultureDistributionText;         // cultural.cultureDistribution list

    // ===========================
    // History Tab UI
    // ===========================
    [Header("History Tab UI (NEW)")]
    [SerializeField] private TMP_Text historyTitleText;

    [Tooltip("HistoryPanel/Body Text (or similar)")]
    [SerializeField] private TMP_Text historyBodyText;

    [Tooltip("HistoryPanel/TimelineButtonLabel (optional)")]
    [SerializeField] private TMP_Text historyTimelineButtonLabelText;

    [Tooltip("HistoryPanel/FamilyButtonLabel (optional)")]
    [SerializeField] private TMP_Text historyFamilyButtonLabelText;

    [Tooltip("HistoryPanel/TimelineButton (optional; wired to no-op stub)")]
    [SerializeField] private Button historyTimelineButton;

    [Tooltip("HistoryPanel/RulingFamilyTreeButton (optional; wired to no-op stub)")]
    [SerializeField] private Button historyFamilyTreeButton;

    [Tooltip("HistoryPanel/HistoryPopOutButton (optional; wired to no-op stub)")]
    [SerializeField] private Button historyPopOutButton;

    [Header("History Tab UI (Matches JSON)")]
    [SerializeField] private TMP_Text historyTimelineEntriesText;       // history.timelineEntries list
    [SerializeField] private TMP_Text historyRulingFamilyMembersText;   // history.rulingFamilyMembers list

    // ===========================
    // Geography Tab UI
    // ===========================
    [Header("Geography Tab UI (NEW)")]
    [SerializeField] private float unityUnitsToMiles = 10f;
    [SerializeField] private TMP_Text geoTotalAreaText;
    [SerializeField] private TMP_Dropdown geoWildernessDropdown;
    [SerializeField] private TMP_Dropdown geoNaturalFormationsDropdown;
    [SerializeField] private TMP_Dropdown geoRuinsDropdown;
    [SerializeField] private TMP_Text geoWildernessTerrainPercentText;

    // ===========================
    // Realm Management Tab UI
    // ===========================
    [Header("Realm Management Tab UI (NEW)")]
    [SerializeField] private TMP_InputField realmLawsField;
    [SerializeField] private TMP_Dropdown realmVassalsDropdown;
    [SerializeField] private TMP_Text realmVassalSettlementNameText;
    [SerializeField] private TMP_Text realmVassalRulerNameText;
    [SerializeField] private TMP_Text realmVassalContractTermsText;
    [SerializeField] private TMP_Text realmVassalLevyContributionText;
    [SerializeField] private TMP_Text realmVassalGoldContributionText;

    [Header("Vassals Tab UI (existing)")]
    [SerializeField] private Transform vassalListContainer;
    [SerializeField] private GameObject vassalRowPrefab;
    // New: Dropdown for vassals list; optional. If assigned, vassals will be listed in this dropdown instead of instantiating rows.
    [Header("Vassals Tab UI (Dropdown)")]
    [SerializeField] private TMP_Dropdown vassalsDropdown;

    // =========================================================
    // Tooltips (STATIC TEXT MODE)
    // =========================================================
    [Header("Tooltips (STATIC TEXT)")]
    [SerializeField] private bool enableTooltips = true;

    [Tooltip("Seconds the pointer must remain over a UI element before showing the tooltip text.")]
    [SerializeField] private float tooltipHoverDelay = 1.0f;

    [Tooltip("Assign a TMP_Text somewhere in your UI. This will be updated after 1s hover.")]
    [SerializeField] private TMP_Text tooltipStaticText;

    // (Kept from earlier version so existing assignments don't break; no longer used in static mode)
    [Header("Tooltip (Legacy Bubble - kept for existing assignments, not used if tooltipStaticText is set)")]
    [SerializeField] private float tooltipMaxWidth = 360f;
    [SerializeField] private Vector2 tooltipPadding = new Vector2(12f, 8f);
    [SerializeField] private Vector2 tooltipScreenOffset = new Vector2(12f, -12f);
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;

    // =========================================================
    // Tooltip Text Overrides — edit freely in Inspector
    // =========================================================
    [Header("Tooltip Text Overrides — Top Buttons / Tabs")]
    [SerializeField] private string ttCloseButton = "Close this window.";
    [SerializeField] private string ttMainButton = "Open the Main tab.";
    [SerializeField] private string ttArmyButton = "Open the Army tab.";
    [SerializeField] private string ttEconomyButton = "Open the Economy tab.";
    [SerializeField] private string ttCulturalButton = "Open the Culture tab.";
    [SerializeField] private string ttHistoryButton = "Open the History tab.";
    [SerializeField] private string ttVassalsButton = "Open the Vassals tab.";
    [SerializeField] private string ttGeographyButton = "Open the Geography tab.";
    [SerializeField] private string ttRealmManagementButton = "Open the Realm Management tab (only appears when you have direct non-capital vassals).";
    [SerializeField] private string ttMapButton = "Open the map panel (if implemented).";

    [Header("Tooltip Text Overrides — Main Tab")]
    [SerializeField] private string ttTitleText = "Settlement / location name.";
    [SerializeField] private string ttDescriptionText = "Description of this location.";
    [SerializeField] private string ttMainRulerButton = "Open the ruler's character sheet.";
    [SerializeField] private string ttCharactersPanel = "Characters who belong to / reside in this settlement.";
    [SerializeField] private string ttCharactersRow = "Open character sheet.";

    [Header("Tooltip Text Overrides — Army Tab")]
    [SerializeField] private string ttArmyTotalArmyCounterText = "Total army size (army.totalArmy).";
    [SerializeField] private string ttArmyCommanderNameText = "Primary commander name (army.primaryCommanderDisplayName).";
    [SerializeField] private string ttArmyMenAtArmsListText = "Men-at-arms list (army.menAtArms).";

    [Header("Tooltip Text Overrides — Economy Tab")]
    [SerializeField] private string ttEconomyTotalIncomePerMonthText = "Total income per month (economy.totalIncomePerMonth).";
    [SerializeField] private string ttEconomyTotalTreasuryText = "Total treasury (economy.totalTreasury).";
    [SerializeField] private string ttEconomyCourtExpensesText = "Court expenses (economy.courtExpenses).";
    [SerializeField] private string ttEconomyArmyExpensesText = "Army expenses (economy.armyExpenses).";
    [SerializeField] private string ttEconomyCurrentlyConstructingText = "Currently constructing projects (economy.currentlyConstructing).";

    [Header("Tooltip Text Overrides — Cultural Tab")]
    [SerializeField] private string ttCulturalCultureText = "Culture name (cultural.culture).";
    [SerializeField] private string ttCulturalPopulationDistributionText = "Population distribution (cultural.populationDistribution).";
    [SerializeField] private string ttCulturalPrimaryTraitsText = "Primary traits (cultural.primaryTraits).";
    [SerializeField] private string ttCulturalRaceDistributionText = "Race distribution (cultural.raceDistribution).";
    [SerializeField] private string ttCulturalCultureDistributionText = "Culture distribution (cultural.cultureDistribution).";

    [Header("Tooltip Text Overrides — History Tab")]
    [SerializeField] private string ttHistoryTimelineEntriesText = "Timeline entries (history.timelineEntries).";
    [SerializeField] private string ttHistoryRulingFamilyMembersText = "Ruling family members (history.rulingFamilyMembers).";

    [Header("Tooltip Text Overrides — Geography Tab")]
    [SerializeField] private string ttGeoTotalAreaText = "Total area (computed from collider).";

    [Header("Tooltip Text Overrides — Realm Management Tab")]
    [SerializeField] private string ttRealmVassalRulerNameText = "Ruler Name.";

    private SettlementInfoData _data;
    private MapPoint _point;
    private string _settlementId;

    // Dropdown backing lists
    private readonly List<MapPoint> _geoWilderness = new List<MapPoint>();
    private readonly List<MapPoint> _geoNatural = new List<MapPoint>();
    private readonly List<MapPoint> _geoRuins = new List<MapPoint>();
    private readonly List<string> _realmVassalIds = new List<string>();
    // New backing lists for dropdowns
    private readonly List<string> _characterIds = new List<string>();
    private readonly List<string> _vassalIds = new List<string>();

    // Tooltip runtime (static text)
    private Coroutine _tooltipDelayRoutine;
    private TooltipTarget _currentHoverTarget;
    private string _currentHoverText;

    private Canvas _rootCanvas;

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>(true);

        if (enableTooltips && tooltipStaticText != null)
            tooltipStaticText.text = "";

        if (enableTooltips)
            RegisterAllTooltips();
    }

    // Called by MapManager via reflection if present
    public void Initialize(MapPoint point)
    {
        _point = point;
        _data = _point != null ? _point.GetSettlementInfoData() : null;
        Initialize(_data);
    }

    // Back-compat: if something else initializes by data directly
    public void Initialize(SettlementInfoData data)
    {
        _data = data;

        _settlementId =
            !string.IsNullOrWhiteSpace(_data?.settlementId) ? _data.settlementId :
            !string.IsNullOrWhiteSpace(_point?.pointId) ? _point.pointId :
            "";

        WireButtons();

        RefreshAll();
        ShowPanel(InfoPanelType.Main);
    }

    private void WireButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }

        WireTab(mainButton, InfoPanelType.Main);
        WireTab(armyButton, InfoPanelType.Army);
        WireTab(economyButton, InfoPanelType.Economy);
        WireTab(culturalButton, InfoPanelType.Cultural);
        WireTab(historyButton, InfoPanelType.History);
        WireTab(vassalsButton, InfoPanelType.Vassals);
        WireTab(geographyButton, InfoPanelType.Geography);

        if (realmManagementButton != null)
        {
            realmManagementButton.onClick.RemoveAllListeners();
            realmManagementButton.onClick.AddListener(() => ShowPanel(InfoPanelType.RealmManagement));
        }

        if (mapButton != null)
        {
            mapButton.onClick.RemoveAllListeners();
            mapButton.onClick.AddListener(OpenMapPanelIfYouHaveOne);
        }

        // Main: ruler button opens character stat panel
        if (mainRulerButton != null)
        {
            mainRulerButton.onClick.RemoveAllListeners();
            mainRulerButton.onClick.AddListener(OpenRulerCharacterSheet);
        }

        // Optional / stub wiring (safe no-ops)
        WireNoOp(armyPopOutButton);
        WireNoOp(economyPopOutButton);
        WireNoOp(culturalPopOutButton);
        WireNoOp(historyPopOutButton);
        WireNoOp(historyTimelineButton);
        WireNoOp(historyFamilyTreeButton);
        WireNoOp(armyCommanderButton);
    }

    private void WireNoOp(Button btn)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => { /* intentionally blank */ });
    }

    private void WireTab(Button btn, InfoPanelType type)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ShowPanel(type));
    }

    private void RefreshAll()
    {
        // Friendly titles:
        // - Main: Display Name only
        // - Other tabs: Tab name only
        if (titleText != null)
            titleText.text = _data?.displayName ?? (_point != null ? _point.displayName : "Unknown");

        if (descriptionText != null)
            descriptionText.text = _data?.main?.description ?? "";

        RefreshMainTabExtras();

        RefreshArmyTab();
        RefreshEconomyTab();
        RefreshCulturalTab();
        RefreshHistoryTab();

        RefreshVassalsTab();
        RefreshGeographyTab();

        bool showRealmManagement = HasDirectNonCapitalVassals();
        if (realmManagementButton != null)
            realmManagementButton.gameObject.SetActive(showRealmManagement);

        if (!showRealmManagement && realmManagementPanel != null)
            realmManagementPanel.SetActive(false);

        if (showRealmManagement)
            RefreshRealmManagementTab();
    }

    private void RefreshMainTabExtras()
    {
        // Ruler name text
        if (mainRulerNameText != null)
            mainRulerNameText.text = _data?.main?.rulerDisplayName ?? "";

        // Characters list: if a dropdown is assigned, populate it; otherwise fall back to the old list panel.
        if (charactersDropdown != null)
            RefreshCharactersDropdown();
        else
            RefreshCharactersPanel();
    }

    private void RefreshCharactersPanel()
    {
        if (charactersPanel != null)
            charactersPanel.SetActive(true);

        if (charactersListContainer == null || characterRowPrefab == null)
        {
            if (charactersEmptyText != null)
                charactersEmptyText.text = ""; // can't build list
            return;
        }

        // Clear old rows
        for (int i = charactersListContainer.childCount - 1; i >= 0; i--)
            Destroy(charactersListContainer.GetChild(i).gameObject);

        // Best “home settlement” source you already have in SettlementInfoData:
        // rulerCharacterId + characterIds
        var ids = new List<string>();

        if (!string.IsNullOrWhiteSpace(_data?.rulerCharacterId))
            ids.Add(_data.rulerCharacterId);

        if (_data?.characterIds != null && _data.characterIds.Length > 0)
            ids.AddRange(_data.characterIds.Where(x => !string.IsNullOrWhiteSpace(x)));

        ids = ids
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ids.Count == 0)
        {
            if (charactersEmptyText != null)
                charactersEmptyText.text = "No characters found.";
            return;
        }

        if (charactersEmptyText != null)
            charactersEmptyText.text = "";

        foreach (var cid in ids)
        {
            var rowGo = Instantiate(characterRowPrefab, charactersListContainer);

            string label = ResolveCharacterDisplayName(cid);
            if (string.IsNullOrWhiteSpace(label)) label = cid;

            ConfigureCharacterRow(rowGo, label, () => OpenCharacterStatPanel(cid));

            if (enableTooltips)
                RegisterTooltip(rowGo, $"{ttCharactersRow} ({label})");
        }
    }

    /// <summary>
    /// Populate the charactersDropdown with all character IDs associated with this settlement.
    /// When an option is selected, opens the corresponding character stat panel.
    /// </summary>
    private void RefreshCharactersDropdown()
    {
        if (charactersDropdown == null)
        {
            // Fallback: build list panel if dropdown is not assigned
            RefreshCharactersPanel();
            return;
        }

        // Ensure panel is visible if assigned
        if (charactersPanel != null)
            charactersPanel.SetActive(true);

        charactersDropdown.onValueChanged.RemoveAllListeners();
        charactersDropdown.options.Clear();
        _characterIds.Clear();

        // Gather character IDs: ruler + additional characters
        if (!string.IsNullOrWhiteSpace(_data?.rulerCharacterId))
            _characterIds.Add(_data.rulerCharacterId);
        if (_data?.characterIds != null && _data.characterIds.Length > 0)
        {
            foreach (var id in _data.characterIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                string trimmed = id.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && !_characterIds.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
                    _characterIds.Add(trimmed);
            }
        }

        // Build options
        if (_characterIds.Count == 0)
        {
            charactersDropdown.options.Add(new TMP_Dropdown.OptionData("No characters found."));
            charactersDropdown.value = 0;
            charactersDropdown.interactable = false;
            charactersDropdown.RefreshShownValue();
            return;
        }

        charactersDropdown.options.Add(new TMP_Dropdown.OptionData("Select a character..."));
        foreach (var cid in _characterIds)
        {
            string label = ResolveCharacterDisplayName(cid);
            if (string.IsNullOrWhiteSpace(label)) label = cid;
            charactersDropdown.options.Add(new TMP_Dropdown.OptionData(label));
        }

        charactersDropdown.value = 0;
        charactersDropdown.interactable = true;
        charactersDropdown.RefreshShownValue();

        charactersDropdown.onValueChanged.AddListener(idx =>
        {
            if (idx <= 0) return;
            int listIndex = idx - 1;
            if (listIndex < 0 || listIndex >= _characterIds.Count) return;
            string selectedId = _characterIds[listIndex];
            // Open character sheet
            OpenCharacterStatPanel(selectedId);
            // Reset dropdown to placeholder
            charactersDropdown.value = 0;
            charactersDropdown.RefreshShownValue();
        });
    }

    private void ConfigureCharacterRow(GameObject rowGo, string label, Action onClick)
    {
        if (rowGo == null) return;

        // If they have a custom component with Set(...) use it (reflection-safe)
        // Try common patterns:
        //   Set(string id, string displayName, Action<string> onClick)
        //   Set(string displayName, Action onClick)
        //   Initialize(...)
        var monos = rowGo.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in monos)
        {
            if (mb == null) continue;

            var t = mb.GetType();
            var m1 = t.GetMethod("Set", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m1 != null)
            {
                var ps = m1.GetParameters();
                try
                {
                    if (ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(Action))
                    {
                        m1.Invoke(mb, new object[] { label, onClick });
                        return;
                    }
                }
                catch { /* ignore */ }
            }
        }

        // Generic fallback: set TMP_Text + wire Button
        var tmp = rowGo.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = label;

        var btn = rowGo.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    private void OpenRulerCharacterSheet()
    {
        string cid = _data?.rulerCharacterId;

        if (string.IsNullOrWhiteSpace(cid))
        {
            // fallback: try army commander id
            cid = _data?.army != null ? _data.army.primaryCommanderCharacterId : null;
            if (string.IsNullOrWhiteSpace(cid) && _data?.army?.armyIds != null)
            {
                foreach (var armyId in _data.army.armyIds)
                {
                    if (string.IsNullOrWhiteSpace(armyId)) continue;
                    var army = ArmyDataLoader.TryLoad(armyId);
                    if (!string.IsNullOrWhiteSpace(army?.primaryCommanderCharacterId))
                    {
                        cid = army.primaryCommanderCharacterId;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(cid))
        {
            // fallback: first character in list
            cid = (_data?.characterIds != null && _data.characterIds.Length > 0) ? _data.characterIds[0] : null;
        }

        if (string.IsNullOrWhiteSpace(cid))
            return;

        OpenCharacterStatPanel(cid);
    }

    private void OpenCharacterStatPanel(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return;
        if (characterStatPanelPrefab == null) return;

        Transform parent = characterPanelSpawnParent;

        if (parent == null && _rootCanvas != null)
            parent = _rootCanvas.transform;

        if (parent == null)
            parent = transform.root;

        var go = Instantiate(characterStatPanelPrefab, parent);

        // Best-effort: call Initialize/Init/SetCharacter with (string characterId)
        TryInvokeCharacterPanelInit(go, characterId);
    }

    private void TryInvokeCharacterPanelInit(GameObject panelInstance, string characterId)
    {
        if (panelInstance == null) return;

        var monos = panelInstance.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in monos)
        {
            if (mb == null) continue;
            var t = mb.GetType();

            // Try methods in priority order
            if (TryInvokeStringMethod(mb, t, "Initialize", characterId)) return;
            if (TryInvokeStringMethod(mb, t, "Init", characterId)) return;
            if (TryInvokeStringMethod(mb, t, "SetCharacter", characterId)) return;
            if (TryInvokeStringMethod(mb, t, "Show", characterId)) return;
        }
    }

    private static bool TryInvokeStringMethod(object target, Type targetType, string methodName, string arg)
    {
        try
        {
            var m = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) return false;

            var ps = m.GetParameters();
            if (ps.Length != 1) return false;
            if (ps[0].ParameterType != typeof(string)) return false;

            m.Invoke(target, new object[] { arg });
            return true;
        }
        catch { return false; }
    }

    private string ResolveCharacterDisplayName(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return "";

        // If you have a resolver in your project, we try to use it without hard dependency.
        // Common patterns: CharacterNameResolver.Resolve(string), CharacterNameResolver.GetName(string)
        string name = TryCallStaticStringFunc("CharacterNameResolver", "Resolve", characterId);
        if (!string.IsNullOrWhiteSpace(name)) return name;

        name = TryCallStaticStringFunc("CharacterNameResolver", "GetName", characterId);
        if (!string.IsNullOrWhiteSpace(name)) return name;

        name = TryCallStaticStringFunc("CharacterStatsCache", "GetDisplayName", characterId);
        if (!string.IsNullOrWhiteSpace(name)) return name;

        return characterId;
    }

    private static string TryCallStaticStringFunc(string typeName, string methodName, string arg)
    {
        var t = FindTypeInLoadedAssemblies(typeName);
        if (t == null) return null;

        try
        {
            var m = t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) return null;

            var ps = m.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) return null;
            if (m.ReturnType != typeof(string)) return null;

            return (string)m.Invoke(null, new object[] { arg });
        }
        catch { return null; }
    }

    private static Type FindTypeInLoadedAssemblies(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        // try fast paths
        var t = Type.GetType(typeName);
        if (t != null) return t;

        // Unity usually has these in Assembly-CSharp, but safest is scan
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length; i++)
        {
            try
            {
                t = asms[i].GetType(typeName);
                if (t != null) return t;
            }
            catch { /* ignore */ }
        }

        return null;
    }

    private bool HasDirectNonCapitalVassals()
    {
        if (string.IsNullOrWhiteSpace(_settlementId)) return false;

        var stats = SettlementStatsCache.GetStatsOrNull(_settlementId);
        if (stats == null) return false;

        return stats.directVassals.Any(v => v != null && !v.isCapital);
    }

    private void ShowPanel(InfoPanelType type)
    {
        if (mainPanel != null) mainPanel.SetActive(type == InfoPanelType.Main);
        if (armyPanel != null) armyPanel.SetActive(type == InfoPanelType.Army);
        if (economyPanel != null) economyPanel.SetActive(type == InfoPanelType.Economy);
        if (culturalPanel != null) culturalPanel.SetActive(type == InfoPanelType.Cultural);
        if (historyPanel != null) historyPanel.SetActive(type == InfoPanelType.History);
        if (vassalsPanel != null) vassalsPanel.SetActive(type == InfoPanelType.Vassals);

        if (geographyPanel != null) geographyPanel.SetActive(type == InfoPanelType.Geography);
        if (realmManagementPanel != null) realmManagementPanel.SetActive(type == InfoPanelType.RealmManagement);

        if (type == InfoPanelType.Main) RefreshMainTabExtras();
        if (type == InfoPanelType.Army) RefreshArmyTab();
        if (type == InfoPanelType.Economy) RefreshEconomyTab();
        if (type == InfoPanelType.Cultural) RefreshCulturalTab();
        if (type == InfoPanelType.History) RefreshHistoryTab();
        if (type == InfoPanelType.Geography) RefreshGeographyTab();
        if (type == InfoPanelType.RealmManagement) RefreshRealmManagementTab();

        transform.SetAsLastSibling();
    }

    private static void SetSimpleTabTitle(TMP_Text title, string tabName)
    {
        if (title == null) return;
        title.text = tabName ?? "";
    }

    // ---------------------------
    // Army Tab
    // ---------------------------
    private void RefreshArmyTab()
    {
        SetSimpleTabTitle(armyTitleText, "Army");

        if (armyBodyText != null)
        {
            armyBodyText.text = TryGetStringByPaths(
                _data,
                "army.description",
                "army.notes"
            );
        }

        var armySummary = SettlementArmyResolver.Resolve(_data);
        int total = armySummary.totalTroops;
        if (armyTotalArmyCounterText != null)
            armyTotalArmyCounterText.text = total > 0 ? total.ToString("N0") : "";

        string commander = armySummary.commanderDisplayName ?? "";
        if (string.IsNullOrWhiteSpace(commander))
            commander = _data?.main != null ? _data.main.rulerDisplayName : "";

        if (armyCommanderNameText != null)
            armyCommanderNameText.text = commander ?? "";

        if (armyMenAtArmsListText != null)
        {
            var list = armySummary.menAtArmsCounts;
            armyMenAtArmsListText.text = (list != null && list.Count > 0)
                ? string.Join("\n", list.Select(x =>
                    x.count > 0 ? $"• {x.menAtArmsId} x{x.count}" : $"• {x.menAtArmsId}"))
                : "";
        }
    }

    // ---------------------------
    // Economy Tab
    // ---------------------------
    private void RefreshEconomyTab()
    {
        SetSimpleTabTitle(economyTitleText, "Economy");

        var econ = _data?.economy;
        if (econ == null)
        {
            SetEconomyFieldsEmpty();
            return;
        }

        if (economyTotalIncomePerMonthText != null)
            economyTotalIncomePerMonthText.text = $"{econ.totalIncomePerMonth:0.##}";

        if (economyTotalTreasuryText != null)
            economyTotalTreasuryText.text = $"{econ.totalTreasury:0.##}";

        if (economyCourtExpensesText != null)
            economyCourtExpensesText.text = econ.courtExpenses ?? "";

        if (economyArmyExpensesText != null)
            economyArmyExpensesText.text = econ.armyExpenses ?? "";

        if (economyCurrentlyConstructingText != null)
        {
            var cc = econ.currentlyConstructing;
            economyCurrentlyConstructingText.text = (cc != null && cc.Length > 0)
                ? string.Join("\n", cc.Select(x => $"• {x}"))
                : "";
        }

        // Keep legacy slots populated too
        if (economyIncomeText != null)
            economyIncomeText.text = $"{econ.totalIncomePerMonth:0.##}";

        if (economyNetText != null)
            economyNetText.text = $"{econ.totalTreasury:0.##}";

        if (economyExpensesText != null)
        {
            string court = string.IsNullOrWhiteSpace(econ.courtExpenses) ? "" : $"Court: {econ.courtExpenses}";
            string army = string.IsNullOrWhiteSpace(econ.armyExpenses) ? "" : $"Army: {econ.armyExpenses}";
            economyExpensesText.text = string.Join("\n", new[] { court, army }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        if (economyTaxRateText != null)
            economyTaxRateText.text = ""; // not in your current EconomyTab

        if (economyBodyText != null)
        {
            var lines = new List<string>
            {
                $"Income / Month: {econ.totalIncomePerMonth:0.##}",
                $"Treasury: {econ.totalTreasury:0.##}",
            };

            if (!string.IsNullOrWhiteSpace(econ.courtExpenses)) lines.Add($"Court Expenses: {econ.courtExpenses}");
            if (!string.IsNullOrWhiteSpace(econ.armyExpenses)) lines.Add($"Army Expenses: {econ.armyExpenses}");

            if (econ.currentlyConstructing != null && econ.currentlyConstructing.Length > 0)
            {
                lines.Add("Currently Constructing:");
                lines.AddRange(econ.currentlyConstructing.Select(x => $"• {x}"));
            }

            economyBodyText.text = string.Join("\n", lines);
        }
    }

    private void SetEconomyFieldsEmpty()
    {
        if (economyIncomeText != null) economyIncomeText.text = "";
        if (economyExpensesText != null) economyExpensesText.text = "";
        if (economyNetText != null) economyNetText.text = "";
        if (economyTaxRateText != null) economyTaxRateText.text = "";

        if (economyTotalIncomePerMonthText != null) economyTotalIncomePerMonthText.text = "";
        if (economyTotalTreasuryText != null) economyTotalTreasuryText.text = "";
        if (economyCourtExpensesText != null) economyCourtExpensesText.text = "";
        if (economyArmyExpensesText != null) economyArmyExpensesText.text = "";
        if (economyCurrentlyConstructingText != null) economyCurrentlyConstructingText.text = "";

        if (economyBodyText != null) economyBodyText.text = "";
    }

    // ---------------------------
    // Cultural Tab
    // ---------------------------
    private void RefreshCulturalTab()
    {
        SetSimpleTabTitle(culturalTitleText, "Culture");

        var cul = _data?.cultural;
        if (cul == null)
        {
            SetCulturalFieldsEmpty();
            return;
        }

        if (culturalCultureText != null) culturalCultureText.text = cul.culture ?? "";
        if (culturalPopulationDistributionText != null) culturalPopulationDistributionText.text = cul.populationDistribution ?? "";

        if (culturalPrimaryTraitsText != null)
        {
            var traits = cul.primaryTraits;
            culturalPrimaryTraitsText.text = (traits != null && traits.Length > 0)
                ? string.Join("\n", traits.Select(x => $"• {x}"))
                : "";
        }

        if (culturalRaceDistributionText != null)
            culturalRaceDistributionText.text = FormatPercentEntries(cul.raceDistribution);

        if (culturalCultureDistributionText != null)
            culturalCultureDistributionText.text = FormatPercentEntries(cul.cultureDistribution);

        // Legacy placeholders kept
        if (culturalReligionText != null && string.IsNullOrWhiteSpace(culturalReligionText.text))
            culturalReligionText.text = cul.culture ?? "";

        if (culturalLanguageText != null && string.IsNullOrWhiteSpace(culturalLanguageText.text))
            culturalLanguageText.text = cul.populationDistribution ?? "";

        if (culturalTraditionsText != null && string.IsNullOrWhiteSpace(culturalTraditionsText.text))
        {
            var traits = cul.primaryTraits;
            culturalTraditionsText.text = (traits != null && traits.Length > 0)
                ? string.Join("\n", traits.Select(x => $"• {x}"))
                : "";
        }

        if (culturalBodyText != null)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(cul.culture)) lines.Add($"Culture: {cul.culture}");
            if (!string.IsNullOrWhiteSpace(cul.populationDistribution)) lines.Add(cul.populationDistribution);

            if (cul.primaryTraits != null && cul.primaryTraits.Length > 0)
            {
                lines.Add("Primary Traits:");
                lines.AddRange(cul.primaryTraits.Select(x => $"• {x}"));
            }

            culturalBodyText.text = string.Join("\n", lines);
        }
    }

    private void SetCulturalFieldsEmpty()
    {
        if (culturalCultureText != null) culturalCultureText.text = "";
        if (culturalPopulationDistributionText != null) culturalPopulationDistributionText.text = "";
        if (culturalPrimaryTraitsText != null) culturalPrimaryTraitsText.text = "";
        if (culturalRaceDistributionText != null) culturalRaceDistributionText.text = "";
        if (culturalCultureDistributionText != null) culturalCultureDistributionText.text = "";

        if (culturalReligionText != null) culturalReligionText.text = "";
        if (culturalLanguageText != null) culturalLanguageText.text = "";
        if (culturalTraditionsText != null) culturalTraditionsText.text = "";

        if (culturalBodyText != null) culturalBodyText.text = "";
    }

    private static string FormatPercentEntries(List<PercentEntry> entries)
    {
        if (entries == null || entries.Count == 0) return "";

        var lines = new List<string>();
        foreach (var e in entries)
        {
            if (e == null) continue;
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            lines.Add($"• {e.key}: {e.percent:0.#}%");
        }
        return string.Join("\n", lines);
    }

    // ---------------------------
    // History Tab
    // ---------------------------
    private void RefreshHistoryTab()
    {
        SetSimpleTabTitle(historyTitleText, "History");

        var hist = _data?.history;
        if (hist == null)
        {
            SetHistoryFieldsEmpty();
            return;
        }

        string timeline = (hist.timelineEntries != null && hist.timelineEntries.Length > 0)
            ? string.Join("\n", hist.timelineEntries.Select(x => $"• {x}"))
            : "";

        string family = (hist.rulingFamilyMembers != null && hist.rulingFamilyMembers.Length > 0)
            ? string.Join("\n", hist.rulingFamilyMembers.Select(x => $"• {x}"))
            : "";

        if (historyTimelineEntriesText != null) historyTimelineEntriesText.text = timeline;
        if (historyRulingFamilyMembersText != null) historyRulingFamilyMembersText.text = family;

        if (historyBodyText != null)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(timeline))
            {
                parts.Add("Timeline:");
                parts.Add(timeline);
            }

            if (!string.IsNullOrWhiteSpace(family))
            {
                parts.Add("Ruling Family:");
                parts.Add(family);
            }

            historyBodyText.text = string.Join("\n", parts);
        }
    }

    private void SetHistoryFieldsEmpty()
    {
        if (historyTimelineEntriesText != null) historyTimelineEntriesText.text = "";
        if (historyRulingFamilyMembersText != null) historyRulingFamilyMembersText.text = "";
        if (historyBodyText != null) historyBodyText.text = "";
    }

    // ---------------------------
    // Geography Tab
    // ---------------------------
    private void RefreshGeographyTab()
    {
        if (_point == null) return;

        float parentAreaSqMi = MapPointGeographyUtility.ComputeMapPointAreaSqMi(_point, unityUnitsToMiles);
        if (geoTotalAreaText != null)
            geoTotalAreaText.text = $"Total Area: {parentAreaSqMi:0.#} sq mi";

        var buckets = MapPointGeographyUtility.GetUnpopulatedChildrenBuckets(_point);

        _geoWilderness.Clear();
        _geoNatural.Clear();
        _geoRuins.Clear();

        _geoWilderness.AddRange(buckets.wilderness);
        _geoNatural.AddRange(buckets.naturalFormations);
        _geoRuins.AddRange(buckets.ruins);

        SetupChildDropdown(geoWildernessDropdown, _geoWilderness);
        SetupChildDropdown(geoNaturalFormationsDropdown, _geoNatural);
        SetupChildDropdown(geoRuinsDropdown, _geoRuins);

        if (geoWildernessTerrainPercentText != null)
        {
            if (parentAreaSqMi <= 0.0001f)
            {
                geoWildernessTerrainPercentText.text = "Terrain breakdown: parent area is 0 (no collider / invalid scale).";
            }
            else if (_geoWilderness.Count == 0)
            {
                geoWildernessTerrainPercentText.text = "No Wilderness sub-regions found.";
            }
            else
            {
                var areaByTerrain = MapPointGeographyUtility.ComputeWildernessAreaByTerrainSqMi(_geoWilderness, unityUnitsToMiles);

                var lines = new List<string>();
                foreach (var kv in areaByTerrain.OrderByDescending(k => k.Value))
                {
                    float pct = (kv.Value / parentAreaSqMi) * 100f;
                    lines.Add($"{kv.Key}: {pct:0.#}% ({kv.Value:0.#} sq mi)");
                }

                geoWildernessTerrainPercentText.text = string.Join("\n", lines);
            }
        }
    }

    private void SetupChildDropdown(TMP_Dropdown dropdown, List<MapPoint> points)
    {
        if (dropdown == null) return;

        dropdown.onValueChanged.RemoveAllListeners();

        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Select..."));

        if (points != null)
        {
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                string label = p != null && !string.IsNullOrWhiteSpace(p.displayName) ? p.displayName : (p != null ? p.pointId : "Unknown");
                dropdown.options.Add(new TMP_Dropdown.OptionData(label));
            }
        }

        dropdown.value = 0;
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(idx =>
        {
            if (idx <= 0) return;
            int pIdx = idx - 1;
            if (points == null || pIdx < 0 || pIdx >= points.Count) return;

            MapPointGeographyUtility.SimulateMapPointClick(points[pIdx]);

            dropdown.value = 0;
            dropdown.RefreshShownValue();
        });
    }

    // ---------------------------
    // Realm Management Tab
    // ---------------------------
    private void RefreshRealmManagementTab()
    {
        if (!HasDirectNonCapitalVassals())
            return;

        if (realmLawsField != null)
            realmLawsField.text = _data != null && _data.feudal != null ? (_data.feudal.laws ?? "") : "";

        _realmVassalIds.Clear();

        var stats = SettlementStatsCache.GetStatsOrNull(_settlementId);
        if (stats == null)
        {
            SetupRealmDropdownEmpty("No realm stats.");
            return;
        }

        var vassals = stats.directVassals
            .Where(v => v != null && !v.isCapital)
            .OrderBy(v => v.vassalDisplayName ?? v.vassalSettlementId)
            .ToList();

        if (vassals.Count == 0)
        {
            SetupRealmDropdownEmpty("No direct vassals.");
            return;
        }

        if (realmVassalsDropdown != null)
        {
            realmVassalsDropdown.onValueChanged.RemoveAllListeners();
            realmVassalsDropdown.options.Clear();
            realmVassalsDropdown.options.Add(new TMP_Dropdown.OptionData("Select a vassal..."));

            for (int i = 0; i < vassals.Count; i++)
            {
                var v = vassals[i];
                _realmVassalIds.Add(v.vassalSettlementId);

                string label = !string.IsNullOrWhiteSpace(v.vassalDisplayName) ? v.vassalDisplayName : v.vassalSettlementId;
                realmVassalsDropdown.options.Add(new TMP_Dropdown.OptionData(label));
            }

            realmVassalsDropdown.value = 0;
            realmVassalsDropdown.RefreshShownValue();

            realmVassalsDropdown.onValueChanged.AddListener(idx =>
            {
                if (idx <= 0) return;
                int vIdx = idx - 1;
                if (vIdx < 0 || vIdx >= _realmVassalIds.Count) return;

                string vid = _realmVassalIds[vIdx];
                RefreshRealmVassalDetails(vid, stats);
            });
        }

        SetRealmDetailFields("", "", "", "", "");
    }

    private void SetupRealmDropdownEmpty(string reason)
    {
        if (realmVassalsDropdown != null)
        {
            realmVassalsDropdown.onValueChanged.RemoveAllListeners();
            realmVassalsDropdown.options.Clear();
            realmVassalsDropdown.options.Add(new TMP_Dropdown.OptionData(reason));
            realmVassalsDropdown.value = 0;
            realmVassalsDropdown.RefreshShownValue();
        }

        SetRealmDetailFields("", "", "", "", "");
    }

    private void RefreshRealmVassalDetails(string vassalSettlementId, SettlementStatsCache.SettlementComputedStats stats)
    {
        if (string.IsNullOrWhiteSpace(vassalSettlementId) || stats == null) return;

        var v = stats.directVassals.FirstOrDefault(x =>
            x != null &&
            !x.isCapital &&
            string.Equals(x.vassalSettlementId, vassalSettlementId, StringComparison.OrdinalIgnoreCase));

        var vData = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
            DataPaths.Runtime_MapDataPath,
            DataPaths.Editor_MapDataPath,
            vassalSettlementId
        );

        string settlementName = vData != null && !string.IsNullOrWhiteSpace(vData.displayName)
            ? vData.displayName
            : SettlementNameResolver.Resolve(vassalSettlementId);

        string rulerName = vData != null && vData.main != null && !string.IsNullOrWhiteSpace(vData.main.rulerDisplayName)
            ? vData.main.rulerDisplayName
            : "Unknown";

        // Determine contract terms and rates. Use default contract (25% income, 45% levy) if none are defined or the entry is missing.
        string terms;
        float incomeRate;
        float troopRate;
        if (v != null)
        {
            if (v.isCapital)
            {
                // Capitals pay no taxes
                terms = "Capital holding (no tax)";
                incomeRate = 0f;
                troopRate = 0f;
            }
            else
            {
                incomeRate = (v.incomeTaxRate > 0f) ? v.incomeTaxRate : 0.25f;
                troopRate = (v.troopTaxRate > 0f) ? v.troopTaxRate : 0.45f;
                if (string.IsNullOrWhiteSpace(v.terms))
                    terms = "Default contract";
                else
                    terms = v.terms;
            }
        }
        else
        {
            // Unknown vassal: default contract
            terms = "Default contract";
            incomeRate = 0.25f;
            troopRate = 0.45f;
        }

        string levy = $"{Mathf.Clamp01(troopRate) * 100f:0.#}%";
        string gold = $"{Mathf.Clamp01(incomeRate) * 100f:0.#}%";

        SetRealmDetailFields(settlementName, rulerName, terms, levy, gold);
    }

    /// <summary>
    /// Populate the vassals dropdown with direct vassals of the current settlement. Selecting an entry will open that settlement's info window.
    /// </summary>
    private void RefreshVassalsDropdown()
    {
        if (vassalsDropdown == null) return;

        vassalsDropdown.onValueChanged.RemoveAllListeners();
        vassalsDropdown.options.Clear();
        _vassalIds.Clear();

        // Hide row container if we have one
        if (vassalListContainer != null)
        {
            for (int i = vassalListContainer.childCount - 1; i >= 0; i--)
                Destroy(vassalListContainer.GetChild(i).gameObject);
        }

        // Determine direct vassals
        if (string.IsNullOrWhiteSpace(_settlementId))
        {
            vassalsDropdown.options.Add(new TMP_Dropdown.OptionData("No vassals."));
            vassalsDropdown.value = 0;
            vassalsDropdown.interactable = false;
            vassalsDropdown.RefreshShownValue();
            return;
        }

        var stats = SettlementStatsCache.GetStatsOrNull(_settlementId);
        if (stats == null || stats.directVassals == null || stats.directVassals.Count == 0)
        {
            vassalsDropdown.options.Add(new TMP_Dropdown.OptionData("No vassals."));
            vassalsDropdown.value = 0;
            vassalsDropdown.interactable = false;
            vassalsDropdown.RefreshShownValue();
            return;
        }

        // Add placeholder
        vassalsDropdown.options.Add(new TMP_Dropdown.OptionData("Select a vassal..."));
        foreach (var v in stats.directVassals.OrderBy(x => x.isCapital ? 0 : 1).ThenBy(x => x.vassalDisplayName))
        {
            if (v == null) continue;
            _vassalIds.Add(v.vassalSettlementId);
            string label = !string.IsNullOrWhiteSpace(v.vassalDisplayName) ? v.vassalDisplayName : SettlementNameResolver.Resolve(v.vassalSettlementId);
            vassalsDropdown.options.Add(new TMP_Dropdown.OptionData(label));
        }

        vassalsDropdown.value = 0;
        vassalsDropdown.interactable = true;
        vassalsDropdown.RefreshShownValue();

        vassalsDropdown.onValueChanged.AddListener(idx =>
        {
            if (idx <= 0) return;
            int index = idx - 1;
            if (index < 0 || index >= _vassalIds.Count) return;
            string sid = _vassalIds[index];
            MapNavigationUtil.OpenSettlementById(sid);
            // Reset selection
            vassalsDropdown.value = 0;
            vassalsDropdown.RefreshShownValue();
        });
    }

    private void SetRealmDetailFields(string settlementName, string rulerName, string terms, string levyRate, string goldRate)
    {
        if (realmVassalSettlementNameText != null) realmVassalSettlementNameText.text = settlementName ?? "";
        if (realmVassalRulerNameText != null) realmVassalRulerNameText.text = rulerName ?? "";
        if (realmVassalContractTermsText != null) realmVassalContractTermsText.text = terms ?? "";
        if (realmVassalLevyContributionText != null) realmVassalLevyContributionText.text = levyRate ?? "";
        if (realmVassalGoldContributionText != null) realmVassalGoldContributionText.text = goldRate ?? "";
    }

    // ---------------------------
    // Vassals Tab
    // ---------------------------
    private void RefreshVassalsTab()
    {
        // If we have a dropdown assigned for the vassals tab, use it instead of spawning row prefabs.
        if (vassalsDropdown != null)
        {
            RefreshVassalsDropdown();
            return;
        }

        if (vassalListContainer == null || vassalRowPrefab == null) return;
        if (string.IsNullOrWhiteSpace(_settlementId)) return;

        for (int i = vassalListContainer.childCount - 1; i >= 0; i--)
            Destroy(vassalListContainer.GetChild(i).gameObject);

        var stats = SettlementStatsCache.GetStatsOrNull(_settlementId);
        if (stats == null || stats.directVassals == null || stats.directVassals.Count == 0)
            return;

        foreach (var v in stats.directVassals.OrderBy(x => x.isCapital ? 0 : 1).ThenBy(x => x.vassalDisplayName))
        {
            if (v == null) continue;

            var vData = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
                DataPaths.Runtime_MapDataPath,
                DataPaths.Editor_MapDataPath,
                v.vassalSettlementId
            );

            string name = !string.IsNullOrWhiteSpace(v.vassalDisplayName) ? v.vassalDisplayName : SettlementNameResolver.Resolve(v.vassalSettlementId);
            string ruler = vData != null && vData.main != null && !string.IsNullOrWhiteSpace(vData.main.rulerDisplayName)
                ? vData.main.rulerDisplayName
                : "Unknown";

            string terms = v.isCapital ? "Capital holding (no tax)" : (v.terms ?? "");
            string levyLine = v.isCapital ? "Levy: 0%" : $"Levy: {Mathf.Clamp01(v.troopTaxRate) * 100f:0.#}%";
            string goldLine = v.isCapital ? "Gold: 0%" : $"Gold: {Mathf.Clamp01(v.incomeTaxRate) * 100f:0.#}%";

            var rowGo = Instantiate(vassalRowPrefab, vassalListContainer);
            var row = rowGo.GetComponent<VassalRowUI>();
            if (row != null)
            {
                row.Set(
                    v.vassalSettlementId,
                    name,
                    ruler,
                    terms,
                    goldLine,
                    levyLine,
                    (sid) => MapNavigationUtil.OpenSettlementById(sid)
                );
            }

            if (enableTooltips)
                RegisterTooltip(rowGo, $"Open vassal: {name}");
        }
    }

    private void OpenMapPanelIfYouHaveOne()
    {
        // Intentionally left blank.
    }

    // =========================================================
    // Tooltip system (STATIC TEXT)
    // =========================================================
    private void RegisterAllTooltips()
    {
        if (!enableTooltips) return;

        RegisterTooltip(closeButton, ttCloseButton);
        RegisterTooltip(mainButton, ttMainButton);
        RegisterTooltip(armyButton, ttArmyButton);
        RegisterTooltip(economyButton, ttEconomyButton);
        RegisterTooltip(culturalButton, ttCulturalButton);
        RegisterTooltip(historyButton, ttHistoryButton);
        RegisterTooltip(vassalsButton, ttVassalsButton);
        RegisterTooltip(geographyButton, ttGeographyButton);
        RegisterTooltip(realmManagementButton, ttRealmManagementButton);
        RegisterTooltip(mapButton, ttMapButton);

        RegisterTooltip(titleText, ttTitleText);
        RegisterTooltip(descriptionText, ttDescriptionText);

        RegisterTooltip(mainRulerButton, ttMainRulerButton);
        RegisterTooltip(mainRulerNameText, "Ruler Name.");
        RegisterTooltip(charactersPanel, ttCharactersPanel);

        RegisterTooltip(armyTotalArmyCounterText, ttArmyTotalArmyCounterText);
        RegisterTooltip(armyCommanderNameText, ttArmyCommanderNameText);
        RegisterTooltip(armyMenAtArmsListText, ttArmyMenAtArmsListText);

        RegisterTooltip(economyTotalIncomePerMonthText, ttEconomyTotalIncomePerMonthText);
        RegisterTooltip(economyTotalTreasuryText, ttEconomyTotalTreasuryText);
        RegisterTooltip(economyCourtExpensesText, ttEconomyCourtExpensesText);
        RegisterTooltip(economyArmyExpensesText, ttEconomyArmyExpensesText);
        RegisterTooltip(economyCurrentlyConstructingText, ttEconomyCurrentlyConstructingText);

        RegisterTooltip(culturalCultureText, ttCulturalCultureText);
        RegisterTooltip(culturalPopulationDistributionText, ttCulturalPopulationDistributionText);
        RegisterTooltip(culturalPrimaryTraitsText, ttCulturalPrimaryTraitsText);
        RegisterTooltip(culturalRaceDistributionText, ttCulturalRaceDistributionText);
        RegisterTooltip(culturalCultureDistributionText, ttCulturalCultureDistributionText);

        RegisterTooltip(historyTimelineEntriesText, ttHistoryTimelineEntriesText);
        RegisterTooltip(historyRulingFamilyMembersText, ttHistoryRulingFamilyMembersText);

        // Register tooltips for dropdowns if present
        if (charactersDropdown != null)
            RegisterTooltip(charactersDropdown, ttCharactersPanel);
        if (vassalsDropdown != null)
            RegisterTooltip(vassalsDropdown, "Select a vassal to view details.");

        RegisterTooltip(geoTotalAreaText, ttGeoTotalAreaText);
        RegisterTooltip(realmVassalRulerNameText, ttRealmVassalRulerNameText);

        AutoRegisterTooltipsForAllSerializedUIFields();
    }

    private void AutoRegisterTooltipsForAllSerializedUIFields()
    {
        var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var f in fields)
        {
            if (!Attribute.IsDefined(f, typeof(SerializeField))) continue;

            object v = f.GetValue(this);
            if (v == null) continue;

            if (v is GameObject go) RegisterTooltip(go, NicifyName(f.Name));
            else if (v is Component c) RegisterTooltip(c.gameObject, NicifyName(f.Name));
        }
    }

    private static string NicifyName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "UI Element";
        raw = raw.Replace("_", "").Replace("Text", "").Replace("Button", "").Replace("Panel", "");
        return SplitCamel(raw).Trim();
    }

    private static string SplitCamel(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var chars = new List<char>(s.Length + 8);
        chars.Add(char.ToUpperInvariant(s[0]));
        for (int i = 1; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsUpper(c) && !char.IsWhiteSpace(chars.Last()))
                chars.Add(' ');
            chars.Add(c);
        }
        return new string(chars.ToArray());
    }

    private void RegisterTooltip(Component c, string text)
    {
        if (!enableTooltips) return;
        if (c == null) return;
        RegisterTooltip(c.gameObject, text);
    }

    private void RegisterTooltip(GameObject go, string text)
    {
        if (!enableTooltips) return;
        if (go == null) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;

        // Ensure something raycastable exists
        var selectable = go.GetComponent<Selectable>();
        if (selectable == null)
        {
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) tmp.raycastTarget = true;

            var graphic = go.GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true;
        }

        var tt = go.GetComponent<TooltipTarget>();
        if (tt == null) tt = go.AddComponent<TooltipTarget>();
        tt.Bind(this, text);
    }

    private void BeginHover(TooltipTarget target, string text)
    {
        if (!enableTooltips) return;
        if (target == null) return;

        _currentHoverTarget = target;
        _currentHoverText = text ?? "";

        if (_tooltipDelayRoutine != null)
            StopCoroutine(_tooltipDelayRoutine);

        _tooltipDelayRoutine = StartCoroutine(ShowTooltipAfterDelay(target));
    }

    private void EndHover(TooltipTarget target)
    {
        if (!enableTooltips) return;

        if (_currentHoverTarget == target)
        {
            _currentHoverTarget = null;
            _currentHoverText = null;
        }

        if (_tooltipDelayRoutine != null)
        {
            StopCoroutine(_tooltipDelayRoutine);
            _tooltipDelayRoutine = null;
        }

        // If nothing hovered, clear text
        if (_currentHoverTarget == null && tooltipStaticText != null)
            tooltipStaticText.text = "";
    }

    private IEnumerator ShowTooltipAfterDelay(TooltipTarget expectedTarget)
    {
        float t = 0f;
        while (t < tooltipHoverDelay)
        {
            if (_currentHoverTarget != expectedTarget)
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_currentHoverTarget != expectedTarget)
            yield break;

        if (tooltipStaticText != null)
            tooltipStaticText.text = _currentHoverText ?? "";
    }

    private sealed class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private InfoWindowManager _owner;
        private string _text;

        public void Bind(InfoWindowManager owner, string text)
        {
            _owner = owner;
            _text = text ?? "";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_owner == null) return;
            _owner.BeginHover(this, _text);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_owner == null) return;
            _owner.EndHover(this);
        }
    }

    // =========================================================
    // Reflection helpers (kept for back-compat / flexibility)
    // =========================================================
    private static string TryGetStringByPaths(object root, params string[] paths)
    {
        if (root == null || paths == null) return "";

        for (int i = 0; i < paths.Length; i++)
        {
            if (TryGetValueByPath(root, paths[i], out object v) && v != null)
            {
                if (v is string s) return s;
                return v.ToString();
            }
        }

        return "";
    }

    private static bool TryGetValueByPath(object root, string path, out object value)
    {
        value = null;
        if (root == null) return false;
        if (string.IsNullOrWhiteSpace(path)) return false;

        object current = root;
        string[] parts = path.Split('.');

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(part)) return false;

            if (!TryGetMemberValue(current, part, out current))
                return false;

            if (current == null)
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

        var prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (prop != null)
        {
            try { value = prop.GetValue(obj, null); return true; }
            catch { return false; }
        }

        var field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (field != null)
        {
            try { value = field.GetValue(obj); return true; }
            catch { return false; }
        }

        return false;
    }
}
