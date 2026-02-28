using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// InfoWindowManager controls the UI window for settlement map points.
/// Updated to support new settlement data model, including levy tax, profit,
/// resources, and aggregated army stats with army selection dropdown.
/// </summary>
public class InfoWindowManager : MonoBehaviour
{
    public enum InfoPanelType
    {
        Main,
        Army,
        Economy,
        Cultural,
        History,
        Geography,
        RealmManagement
    }

    // === Panels ===
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject armyPanel;
    [SerializeField] private GameObject economyPanel;
    [SerializeField] private GameObject culturalPanel;
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private GameObject geographyPanel;
    [SerializeField] private GameObject realmManagementPanel;

    // === Buttons ===
    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button mainButton;
    [SerializeField] private Button armyButton;
    [SerializeField] private Button economyButton;
    [SerializeField] private Button culturalButton;
    [SerializeField] private Button historyButton;
    [SerializeField] private Button geographyButton;
    [SerializeField] private Button realmManagementButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button capitalButton;

    // === Main Tab ===
    [Header("Main Tab UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Main Tab (Ruler + Characters)")]
    [SerializeField] private TMP_Text mainRulerNameText;
    [SerializeField] private Button mainRulerButton;
    [SerializeField] private GameObject charactersPanel;
    [SerializeField] private Transform charactersListContainer;
    [SerializeField] private GameObject characterRowPrefab;
    [SerializeField] private TMP_Text charactersEmptyText;
    [SerializeField] private TMP_Dropdown charactersDropdown;

    // === Character Stat Panel ===
    [Header("Character Stat Panel")]
    [SerializeField] private GameObject characterStatPanelPrefab;
    [SerializeField] private Transform characterPanelSpawnParent;

    // === Army Tab ===
    [Header("Army Tab UI")]
    [SerializeField] private TMP_Text armyTitleText;
    [SerializeField] private TMP_Text armyBodyText;
    [SerializeField] private TMP_Text armyTotalArmyCounterText;
    [SerializeField] private TMP_Text armyCommanderNameText;
    [SerializeField] private Transform armyMenAtArmsListContainer;
    [SerializeField] private TMP_Text armyMenAtArmsListText;
    [SerializeField] private Button armyPopOutButton;
    [SerializeField] private Button armyCommanderButton;
    // New UI elements for army dropdown and stats
    [SerializeField] private TMP_Dropdown armyDropdown;
    [SerializeField] private Transform armyStatsListContainer;
    [SerializeField] private GameObject armyStatRowPrefab;
    [SerializeField] private GameObject armyInfoWindowPrefab;

    // === Economy Tab ===
    [Header("Economy Tab UI")]
    [SerializeField] private TMP_Text economyTitleText;
    [SerializeField] private TMP_Text economyBodyText;
    [SerializeField] private TMP_Text economyIncomeText;
    [SerializeField] private TMP_Text economyExpensesText;
    [SerializeField] private TMP_Text economyNetText;
    [SerializeField] private TMP_Text economyTaxRateText;
    [SerializeField] private Button economyPopOutButton;
    [SerializeField] private TMP_Text economyTotalIncomePerMonthText;
    [SerializeField] private TMP_Text economyTotalProfitPerMonthText;
    [SerializeField] private TMP_Text economyTotalTreasuryText;
    [SerializeField] private TMP_Text economyCourtExpensesText;
    [SerializeField] private TMP_Text economyArmyExpensesText;
    [SerializeField] private TMP_Text economyCurrentlyConstructingText;

    // === Cultural Tab ===
    [Header("Cultural Tab UI")]
    [SerializeField] private TMP_Text culturalTitleText;
    [SerializeField] private TMP_Text culturalBodyText;
    [SerializeField] private TMP_Text culturalReligionText;
    [SerializeField] private TMP_Text culturalLanguageText;
    [SerializeField] private TMP_Text culturalTraditionsText;
    [SerializeField] private Button culturalPopOutButton;
    [SerializeField] private TMP_Text culturalCultureText;
    [SerializeField] private TMP_Text culturalPopulationDistributionText;
    [SerializeField] private TMP_Text culturalPrimaryTraitsText;
    [SerializeField] private TMP_Text culturalRaceDistributionText;
    [SerializeField] private TMP_Text culturalCultureDistributionText;

    [Header("Cultural Tab Dropdowns")]
    [SerializeField] private TMP_Dropdown cultureDropdown;
    [SerializeField] private TMP_Dropdown raceDropdown;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private TMP_Dropdown religionDropdown;
    [SerializeField] private GameObject subInfoPrefab;

    // === History Tab ===
    [Header("History Tab UI")]
    [SerializeField] private TMP_Text historyTitleText;
    [SerializeField] private TMP_Text historyBodyText;
    [SerializeField] private TMP_Text historyTimelineButtonLabelText;
    [SerializeField] private TMP_Text historyFamilyButtonLabelText;
    [SerializeField] private Button historyTimelineButton;
    [SerializeField] private Button historyFamilyTreeButton;
    [SerializeField] private Button historyPopOutButton;
    [SerializeField] private TMP_Text historyTimelineEntriesText;
    [SerializeField] private TMP_Text historyRulingFamilyMembersText;

    // === Geography Tab ===
    [Header("Geography Tab UI")]
    [SerializeField] private float unityUnitsToMiles = 10f;
    [SerializeField] private TMP_Text geoTotalAreaText;
    [SerializeField] private TMP_Dropdown geoWildernessDropdown;
    [SerializeField] private TMP_Dropdown geoNaturalFormationsDropdown;
    [SerializeField] private TMP_Dropdown geoRuinsDropdown;
    [SerializeField] private TMP_Text geoWildernessTerrainPercentText;

    // === Realm Management Tab ===
    [Header("Realm Management Tab UI")]
    [SerializeField] private TMP_InputField realmLawsField;
    [SerializeField] private TMP_Dropdown realmVassalsDropdown;
    [SerializeField] private TMP_Text realmVassalSettlementNameText;
    [SerializeField] private TMP_Text realmVassalRulerNameText;
    [SerializeField] private TMP_Text realmVassalContractTermsText;
    [SerializeField] private TMP_Text realmVassalLevyContributionText;
    [SerializeField] private TMP_Text realmVassalGoldContributionText;
    [SerializeField] private TMP_Text realmSelectedVassalNameText;

    // === Council and Liege UI ===
    [Header("Council Members")]
    [SerializeField] private TMP_Text castellanNameText;
    [SerializeField] private TMP_Text marshallNameText;
    [SerializeField] private TMP_Text stewardNameText;
    [SerializeField] private TMP_Text diplomatNameText;
    [SerializeField] private TMP_Text spymasterNameText;
    [SerializeField] private TMP_Text headPriestNameText;

    [Header("Liege Information")]
    [SerializeField] private TMP_Text liegeSettlementNameText;
    [SerializeField] private TMP_Text liegeRulerNameText;
    [SerializeField] private TMP_Text liegeContractTermsText;
    [SerializeField] private TMP_Text liegeLevyContributionText;
    [SerializeField] private TMP_Text liegeGoldContributionText;

    // === Tooltips ===
    [Header("Tooltips")]
    [SerializeField] private bool enableTooltips = true;
    [SerializeField] private float tooltipHoverDelay = 1.0f;
    [SerializeField] private TMP_Text tooltipStaticText;

    // Private state
    private SettlementInfoData _data;
    private MapPoint _point;
    private string _settlementId;

    // Backing lists
    private readonly List<MapPoint> _geoWilderness = new();
    private readonly List<MapPoint> _geoNatural = new();
    private readonly List<MapPoint> _geoRuins = new();
    private readonly List<string> _realmVassalIds = new();
    private readonly List<string> _realmVassalDisplayNames = new();
    private readonly List<string> _characterIds = new();
    private readonly List<Action> _armyDropdownActions = new();

    // Root canvas reference
    private Canvas _rootCanvas;

    // Tooltip tracking
    private Coroutine _tooltipDelayRoutine;
    private TooltipTarget _currentHoverTarget;
    private string _currentHoverText;

    private void Awake()
    {
        try
        {
            _rootCanvas = GetComponentInParent<Canvas>(true);

            if (enableTooltips && tooltipStaticText != null)
                tooltipStaticText.text = "";

            if (enableTooltips)
                RegisterAllTooltips();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[InfoWindowManager] Awake threw: {ex}", this);
        }
    }

    // Initialization overloads
    public void Initialize(MapPoint point)
    {
        Safe("Initialize(MapPoint)", () =>
        {
            _point = point;
            _data = _point != null ? _point.GetSettlementInfoData() : null;
            Initialize(_data);
        });
    }
    public void Initialize(SettlementInfoData data)
    {
        Safe("Initialize(SettlementInfoData)", () =>
        {
            _data = data;
            _settlementId = _data?.settlementId ?? _point?.pointId ?? "";
            WireButtons();
            RefreshAll();
            ShowPanel(InfoPanelType.Main);
        });
    }

    #region Wiring
    private void WireButtons()
    {
        Safe("WireButtons", () =>
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
            WireTab(geographyButton, InfoPanelType.Geography);
            WireTab(realmManagementButton, InfoPanelType.RealmManagement);

            if (mapButton != null)
            {
                mapButton.onClick.RemoveAllListeners();
                mapButton.onClick.AddListener(OpenMapPanelIfYouHaveOne);
            }

            if (capitalButton != null)
            {
                capitalButton.onClick.RemoveAllListeners();
                capitalButton.onClick.AddListener(OpenCapitalSettlement);
            }

            if (mainRulerButton != null)
            {
                mainRulerButton.onClick.RemoveAllListeners();
                mainRulerButton.onClick.AddListener(OpenRulerCharacterSheet);
            }

            WireNoOp(armyPopOutButton);
            WireNoOp(economyPopOutButton);
            WireNoOp(culturalPopOutButton);
            WireNoOp(historyPopOutButton);
            WireNoOp(historyTimelineButton);
            WireNoOp(historyFamilyTreeButton);
            WireNoOp(armyCommanderButton);
        });
    }
    private void WireTab(Button btn, InfoPanelType type)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ShowPanel(type));
    }
    private void WireNoOp(Button btn)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => { });
    }
    #endregion

    #region Refresh Methods
    private void RefreshAll()
    {
        Safe("RefreshAll", () =>
        {
            if (titleText != null)
            {
                string val = _data?.displayName ?? _point?.displayName ?? "Unknown";
                LogValue("displayName", val);
                titleText.text = val;
            }

            if (descriptionText != null)
            {
                string val = _data?.main?.description ?? "";
                LogValue("main.description", val);
                descriptionText.text = val;
            }

            RefreshMainTabExtras();
            RefreshArmyTab();
            RefreshEconomyTab();
            RefreshCulturalTab();
            RefreshHistoryTab();
            RefreshGeographyTab();

            // Show realm management only if there are direct vassals or no liege
            bool showRealm = HasDirectNonCapitalVassals() || string.IsNullOrWhiteSpace(_data?.liegeSettlementId);
            LogValue("realm.show", showRealm);
            if (realmManagementButton != null)
                realmManagementButton.gameObject.SetActive(showRealm);
            if (!showRealm && realmManagementPanel != null)
                realmManagementPanel.SetActive(false);
            if (showRealm)
                RefreshRealmManagementTab();
        });
    }
    private void RefreshMainTabExtras()
    {
        Safe("RefreshMainTabExtras", () =>
        {
            if (mainRulerNameText != null)
            {
                string val = _data?.main?.rulerDisplayName ?? "";
                LogValue("main.rulerDisplayName", val);
                mainRulerNameText.text = val;
            }

            if (charactersDropdown != null)
                RefreshCharactersDropdown();
            else
                RefreshCharactersPanel();
        });
    }
    private void RefreshCharactersPanel()
    {
        Safe("RefreshCharactersPanel", () =>
        {
            if (charactersPanel != null)
                charactersPanel.SetActive(true);

            if (charactersListContainer == null || characterRowPrefab == null)
            {
                if (charactersEmptyText != null)
                    charactersEmptyText.text = "";
                return;
            }

            foreach (Transform child in charactersListContainer)
                Destroy(child.gameObject);

            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(_data?.rulerCharacterId))
                ids.Add(_data.rulerCharacterId);
            if (_data?.characterIds != null)
                ids.AddRange(_data.characterIds.Where(x => !string.IsNullOrWhiteSpace(x)));

            ids = ids.Select(x => x.Trim())
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

            foreach (string cid in ids)
            {
                GameObject rowGo = Instantiate(characterRowPrefab, charactersListContainer);
                string label = ResolveCharacterDisplayName(cid) ?? cid;
                LogValue($"character.displayName[{cid}]", label);
                ConfigureCharacterRow(rowGo, label, () => OpenCharacterStatPanel(cid));
            }
        });
    }
    private void RefreshCharactersDropdown()
    {
        Safe("RefreshCharactersDropdown", () =>
        {
            if (charactersDropdown == null)
            {
                RefreshCharactersPanel();
                return;
            }

            if (charactersPanel != null)
                charactersPanel.SetActive(true);

            charactersDropdown.onValueChanged.RemoveAllListeners();
            charactersDropdown.options.Clear();
            _characterIds.Clear();

            if (!string.IsNullOrWhiteSpace(_data?.rulerCharacterId))
                _characterIds.Add(_data.rulerCharacterId);

            if (_data?.characterIds != null)
            {
                foreach (string id in _data.characterIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    string trimmed = id.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) &&
                        !_characterIds.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
                        _characterIds.Add(trimmed);
                }
            }

            if (_characterIds.Count == 0)
            {
                charactersDropdown.options.Add(new TMP_Dropdown.OptionData("No characters found."));
                charactersDropdown.value = 0;
                charactersDropdown.interactable = false;
                charactersDropdown.RefreshShownValue();
                return;
            }

            charactersDropdown.options.Add(new TMP_Dropdown.OptionData("Select a character..."));
            foreach (string cid in _characterIds)
            {
                string label = ResolveCharacterDisplayName(cid) ?? cid;
                LogValue($"character.dropdown[{cid}]", label);
                charactersDropdown.options.Add(new TMP_Dropdown.OptionData(label));
            }

            charactersDropdown.value = 0;
            charactersDropdown.interactable = true;
            charactersDropdown.RefreshShownValue();

            charactersDropdown.onValueChanged.AddListener(idx =>
            {
                Safe("charactersDropdown.onValueChanged", () =>
                {
                    if (idx <= 0) return;
                    int listIndex = idx - 1;
                    if (listIndex < 0 || listIndex >= _characterIds.Count) return;
                    string selectedId = _characterIds[listIndex];
                    OpenCharacterStatPanel(selectedId);
                    charactersDropdown.value = 0;
                    charactersDropdown.RefreshShownValue();
                });
            });
        });
    }
    private void ConfigureCharacterRow(GameObject rowGo, string label, Action onClick)
    {
        if (rowGo == null) return;
        foreach (MonoBehaviour mb in rowGo.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            MethodInfo m1 = mb.GetType().GetMethod("Set", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m1 != null)
            {
                var ps = m1.GetParameters();
                if (ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(Action))
                {
                    try
                    {
                        m1.Invoke(mb, new object[] { label, onClick });
                        return;
                    }
                    catch { /* ignore */ }
                }
            }
        }

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
        Safe("OpenRulerCharacterSheet", () =>
        {
            string cid = _data?.rulerCharacterId ?? _data?.army?.primaryCommanderCharacterId;
            if (string.IsNullOrWhiteSpace(cid) && _data?.characterIds != null && _data.characterIds.Length > 0)
                cid = _data.characterIds[0];

            if (!string.IsNullOrWhiteSpace(cid))
                OpenCharacterStatPanel(cid);
        });
    }
    private void OpenCharacterStatPanel(string characterId)
    {
        Safe("OpenCharacterStatPanel", () =>
        {
            if (string.IsNullOrWhiteSpace(characterId)) return;
            if (characterStatPanelPrefab == null) return;

            Transform parent = characterPanelSpawnParent ?? _rootCanvas?.transform ?? transform.root;
            GameObject go = Instantiate(characterStatPanelPrefab, parent);
            TryInvokeCharacterPanelInit(go, characterId);
        });
    }
    private void TryInvokeCharacterPanelInit(GameObject panelInstance, string characterId)
    {
        if (panelInstance == null) return;
        foreach (MonoBehaviour mb in panelInstance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            Type t = mb.GetType();
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
            MethodInfo m = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) return false;
            var ps = m.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) return false;
            m.Invoke(target, new object[] { arg });
            return true;
        }
        catch { return false; }
    }
    private string ResolveCharacterDisplayName(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return "";

        string name = TryCallStaticStringFunc("CharacterNameResolver", "Resolve", characterId);
        if (!string.IsNullOrWhiteSpace(name)) return name;

        name = TryCallStaticStringFunc("CharacterNameResolver", "GetName", characterId);
        if (!string.IsNullOrWhiteSpace(name)) return name;

        name = TryCallStaticStringFunc("CharacterStatsCache", "GetDisplayName", characterId);
        return !string.IsNullOrWhiteSpace(name) ? name : characterId;
    }
    private static string TryCallStaticStringFunc(string typeName, string methodName, string arg)
    {
        Type t = FindTypeInLoadedAssemblies(typeName);
        if (t == null) return null;

        try
        {
            MethodInfo m = t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
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

        Type t = Type.GetType(typeName);
        if (t != null) return t;

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }
            catch { }
        }
        return null;
    }

    private void RefreshArmyTab()
    {
        Safe("RefreshArmyTab", () =>
        {
            SetSimpleTabTitle(armyTitleText, "Army");

            // Show description/notes
            if (armyBodyText != null)
            {
                string val = TryGetStringByPaths(_data, "army.description", "army.notes");
                LogValue("army.description", val);
                armyBodyText.text = val;
            }

            // Total army size
            int total = _data?.army?.totalArmy ?? 0;
            LogValue("army.totalArmy", total);
            if (armyTotalArmyCounterText != null)
                armyTotalArmyCounterText.text = total > 0 ? total.ToString("N0") : "";

            // Commander
            string commander = _data?.army?.primaryCommanderDisplayName;
            if (string.IsNullOrWhiteSpace(commander))
                commander = _data?.main?.rulerDisplayName;
            LogValue("army.primaryCommanderDisplayName", commander);
            if (armyCommanderNameText != null)
                armyCommanderNameText.text = commander ?? "";

            // Men-at-arms and knights list
            if (armyMenAtArmsListText != null)
            {
                var menList = _data?.army?.menAtArms;
                var knightIds = _data?.army?.knightCharacterIds;
                List<string> lines = new();
                if (menList != null && menList.Length > 0)
                {
                    lines.Add("Men-at-Arms:");
                    lines.AddRange(menList.Select(x => $"• {x}"));
                }
                if (knightIds != null && knightIds.Length > 0)
                {
                    lines.Add("Knights:");
                    foreach (string kid in knightIds)
                    {
                        string name = ResolveCharacterDisplayName(kid);
                        lines.Add($"• {name}");
                    }
                }
                armyMenAtArmsListText.text = lines.Count > 0 ? string.Join("\n", lines) : "";
            }

            // Populate army dropdown with each assigned army
            RefreshArmyDropdown();

            // Display aggregated stats using stat rows
            RefreshArmyStatsRows();
        });
    }

    private void RefreshArmyDropdown()
    {
        if (armyDropdown == null) return;
        armyDropdown.onValueChanged.RemoveAllListeners();
        armyDropdown.options.Clear();
        _armyDropdownActions.Clear();

        // Add placeholder option
        armyDropdown.options.Add(new TMP_Dropdown.OptionData("Select an army..."));
        // Populate if there are armies
        var armyIds = _data?.army?.armyIds;
        if (armyIds != null)
        {
            foreach (string rawId in armyIds)
            {
                if (string.IsNullOrWhiteSpace(rawId)) continue;
                string id = rawId.Trim();
                // Attempt to resolve army display name and size via ArmyDataLoader
                string displayName = id;
                int size = 0;
                try
                {
                    // Use reflection to call ArmyDataLoader.TryLoad(string, out var data)
                    Type loaderType = FindTypeInLoadedAssemblies("ArmyDataLoader");
                    if (loaderType != null)
                    {
                        MethodInfo tryLoad = loaderType.GetMethod("TryLoad", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (tryLoad != null)
                        {
                            var parameters = tryLoad.GetParameters();
                            if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string))
                            {
                                object[] args = new object[] { id, null };
                                bool ok = (bool)tryLoad.Invoke(null, args);
                                if (ok && args[1] != null)
                                {
                                    var armyData = args[1];
                                    // Try to get displayName and totalArmy from armyData
                                    object dispObj;
                                    if (TryGetMemberValue(armyData, "displayName", out dispObj) && dispObj != null)
                                        displayName = dispObj.ToString();
                                    object sizeObj;
                                    if (TryGetMemberValue(armyData, "totalArmy", out sizeObj) && sizeObj != null)
                                        size = Convert.ToInt32(sizeObj);
                                }
                            }
                        }
                    }
                }
                catch { }
                string optionLabel = size > 0 ? $"{displayName} ({size:N0})" : displayName;
                armyDropdown.options.Add(new TMP_Dropdown.OptionData(optionLabel));
                // Add action
                _armyDropdownActions.Add(() => OpenArmyWindow(id));
            }
        }
        armyDropdown.value = 0;
        armyDropdown.interactable = armyDropdown.options.Count > 1;
        armyDropdown.RefreshShownValue();
        armyDropdown.onValueChanged.AddListener(idx =>
        {
            Safe("armyDropdown.onValueChanged", () =>
            {
                // idx 0 is placeholder
                if (idx <= 0) return;
                int index = idx - 1;
                if (index < 0 || index >= _armyDropdownActions.Count) return;
                var action = _armyDropdownActions[index];
                action?.Invoke();
                // Reset dropdown
                armyDropdown.value = 0;
                armyDropdown.RefreshShownValue();
            });
        });
    }

    private void RefreshArmyStatsRows()
    {
        if (armyStatsListContainer == null || armyStatRowPrefab == null) return;
        // Clear existing rows
        foreach (Transform child in armyStatsListContainer)
            Destroy(child.gameObject);
        // Build list of stat name/value pairs
        var stats = new List<(string, string)>();
        int totalLevies = _data?.army?.totalLevies ?? 0;
        int totalArmy = _data?.army?.totalArmy ?? 0;
        int totalMenAtArms = _data?.army?.menAtArms?.Length ?? 0;
        float raisedCost = _data?.army?.raisedMaintenanceCosts ?? 0f;
        float unraisedCost = _data?.army?.unraisedMaintenanceCosts ?? 0f;
        float attack = _data?.army?.attack ?? 0f;
        float defense = _data?.army?.defense ?? 0f;
        float speed = _data?.army?.speed ?? 0f;
        // Add stats
        stats.Add(("Total Army", totalArmy.ToString("N0")));
        stats.Add(("Total Levies", totalLevies.ToString("N0")));
        stats.Add(("Total Men At Arms", totalMenAtArms.ToString("N0")));
        stats.Add(("Raised Maintenance", raisedCost.ToString("0.##")));
        stats.Add(("Unraised Maintenance", unraisedCost.ToString("0.##")));
        stats.Add(("Attack", attack.ToString("0.##")));
        stats.Add(("Defense", defense.ToString("0.##")));
        stats.Add(("Speed", speed.ToString("0.##")));
        foreach (var (name, value) in stats)
        {
            GameObject row = Instantiate(armyStatRowPrefab, armyStatsListContainer);
            // Expect row prefab has two TMP_Text components: label and value
            var texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = name;
                texts[1].text = value;
            }
        }
    }

    private void OpenArmyWindow(string armyId)
    {
        Safe("OpenArmyWindow", () =>
        {
            if (string.IsNullOrWhiteSpace(armyId)) return;
            if (armyInfoWindowPrefab == null) return;
            Transform parent = _rootCanvas != null ? _rootCanvas.transform : transform.root;
            GameObject go = Instantiate(armyInfoWindowPrefab, parent);
            // Try to initialize the window
            foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                Type t = mb.GetType();
                if (TryInvokeStringMethod(mb, t, "Initialize", armyId)) return;
                if (TryInvokeStringMethod(mb, t, "Init", armyId)) return;
                if (TryInvokeStringMethod(mb, t, "SetArmy", armyId)) return;
                if (TryInvokeStringMethod(mb, t, "Show", armyId)) return;
            }
        });
    }

    private void RefreshEconomyTab()
    {
        Safe("RefreshEconomyTab", () =>
        {
            SetSimpleTabTitle(economyTitleText, "Economy");
            var econ = _data?.economy;
            LogValue("economy.exists", econ != null);
            if (econ == null)
            {
                SetEconomyFieldsEmpty();
                return;
            }

            // Income, profit and treasury
            economyTotalIncomePerMonthText?.SetText($"{econ.totalIncomePerMonth:0.##}");
            LogValue("economy.totalIncomePerMonth", econ.totalIncomePerMonth);
            economyTotalProfitPerMonthText?.SetText($"{econ.totalProfitPerMonth:0.##}");
            LogValue("economy.totalProfitPerMonth", econ.totalProfitPerMonth);
            economyTotalTreasuryText?.SetText($"{econ.totalTreasury:0.##}");
            LogValue("economy.totalTreasury", econ.totalTreasury);

            // Expenses (court and army) as floats
            if (economyCourtExpensesText != null)
            {
                economyCourtExpensesText.text = econ.courtExpenses >= 0f ? econ.courtExpenses.ToString("0.##") : "";
                LogValue("economy.courtExpenses", econ.courtExpenses);
            }
            if (economyArmyExpensesText != null)
            {
                economyArmyExpensesText.text = econ.armyExpenses >= 0f ? econ.armyExpenses.ToString("0.##") : "";
                LogValue("economy.armyExpenses", econ.armyExpenses);
            }

            // Currently constructing
            if (economyCurrentlyConstructingText != null)
            {
                var cc = econ.currentlyConstructing;
                LogValue("economy.currentlyConstructing", cc == null ? "null" : string.Join(",", cc));
                economyCurrentlyConstructingText.text = (cc != null && cc.Length > 0)
                    ? string.Join("\n", cc.Select(x => $"• {x}"))
                    : "";
            }

            // Summary texts
            economyIncomeText?.SetText($"{econ.totalIncomePerMonth:0.##}");
            economyNetText?.SetText($"{econ.totalProfitPerMonth:0.##}");
            if (economyExpensesText != null)
            {
                string court = econ.courtExpenses > 0f ? $"Court: {econ.courtExpenses:0.##}" : "";
                string army = econ.armyExpenses > 0f ? $"Army: {econ.armyExpenses:0.##}" : "";
                economyExpensesText.text = string.Join("\n", new[] { court, army }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            economyTaxRateText?.SetText("");

            // Build body text with income, profit, treasury, expenses, resources, constructing
            if (economyBodyText != null)
            {
                List<string> lines = new();
                lines.Add($"Income / Month: {econ.totalIncomePerMonth:0.##}");
                lines.Add($"Profit / Month: {econ.totalProfitPerMonth:0.##}");
                lines.Add($"Treasury: {econ.totalTreasury:0.##}");
                if (econ.courtExpenses > 0f) lines.Add($"Court Expenses: {econ.courtExpenses:0.##}");
                if (econ.armyExpenses > 0f) lines.Add($"Army Expenses: {econ.armyExpenses:0.##}");
                // Resources
                if (econ.wheat > 0f) lines.Add($"Wheat: {econ.wheat:0.##}");
                if (econ.bread > 0f) lines.Add($"Bread: {econ.bread:0.##}");
                if (econ.meat > 0f) lines.Add($"Meat: {econ.meat:0.##}");
                if (econ.wood > 0f) lines.Add($"Wood: {econ.wood:0.##}");
                if (econ.stone > 0f) lines.Add($"Stone: {econ.stone:0.##}");
                if (econ.iron > 0f) lines.Add($"Iron: {econ.iron:0.##}");
                if (econ.steel > 0f) lines.Add($"Steel: {econ.steel:0.##}");
                if (econ.currentlyConstructing != null && econ.currentlyConstructing.Length > 0)
                {
                    lines.Add("Currently Constructing:");
                    lines.AddRange(econ.currentlyConstructing.Select(x => $"• {x}"));
                }
                economyBodyText.text = string.Join("\n", lines);
            }
        });
    }
    private void SetEconomyFieldsEmpty()
    {
        Safe("SetEconomyFieldsEmpty", () =>
        {
            economyIncomeText?.SetText("");
            economyExpensesText?.SetText("");
            economyNetText?.SetText("");
            economyTaxRateText?.SetText("");
            economyTotalIncomePerMonthText?.SetText("");
            economyTotalProfitPerMonthText?.SetText("");
            economyTotalTreasuryText?.SetText("");
            economyCourtExpensesText?.SetText("");
            economyArmyExpensesText?.SetText("");
            economyCurrentlyConstructingText?.SetText("");
            economyBodyText?.SetText("");
        });
    }
    private void RefreshCulturalTab()
    {
        Safe("RefreshCulturalTab", () =>
        {
            SetSimpleTabTitle(culturalTitleText, "Culture");

            var cul = _data?.cultural;
            LogValue("cultural.exists", cul != null);
            if (cul == null)
            {
                SetCulturalFieldsEmpty();
                return;
            }

            culturalCultureText?.SetText(cul.culture ?? "");
            LogValue("cultural.culture", cul.culture);
            culturalPopulationDistributionText?.SetText(cul.populationDistribution ?? "");
            LogValue("cultural.populationDistribution", cul.populationDistribution);

            if (culturalPrimaryTraitsText != null)
            {
                var traits = cul.primaryTraits;
                LogValue("cultural.primaryTraits", traits == null ? "null" : string.Join(",", traits));
                culturalPrimaryTraitsText.text = (traits != null && traits.Length > 0)
                    ? string.Join("\n", traits.Select(x => $"• {x}"))
                    : "";
            }

            culturalRaceDistributionText?.SetText(FormatPercentEntries(cul.raceDistribution));
            LogValue("cultural.raceDistribution", cul.raceDistribution == null ? "null" : FormatPercentEntries(cul.raceDistribution));
            culturalCultureDistributionText?.SetText(FormatPercentEntries(cul.cultureDistribution));
            LogValue("cultural.cultureDistribution", cul.cultureDistribution == null ? "null" : FormatPercentEntries(cul.cultureDistribution));

            PopulateCultureDropdown(cultureDropdown, cul.cultureDistribution);
            PopulateRaceDropdown(raceDropdown, cul.raceDistribution);

            List<string> languages = TryGetValueByPath(_data, "cultural.languages", out var lvalue) && lvalue is IEnumerable<string> llist
                ? llist.ToList()
                : null;
            List<string> religions = TryGetValueByPath(_data, "cultural.religions", out var rvalue) && rvalue is IEnumerable<string> rlist
                ? rlist.ToList()
                : null;
            LogValue("cultural.languages", languages == null ? "null" : string.Join(",", languages));
            LogValue("cultural.religions", religions == null ? "null" : string.Join(",", religions));

            PopulateLanguagesDropdown(languageDropdown, languages);
            PopulateReligionDropdown(religionDropdown, religions);
        });
    }
    private void SetCulturalFieldsEmpty()
    {
        Safe("SetCulturalFieldsEmpty", () =>
        {
            culturalCultureText?.SetText("");
            culturalPopulationDistributionText?.SetText("");
            culturalPrimaryTraitsText?.SetText("");
            culturalRaceDistributionText?.SetText("");
            culturalCultureDistributionText?.SetText("");
            culturalReligionText?.SetText("");
            culturalLanguageText?.SetText("");
            culturalTraditionsText?.SetText("");
            culturalBodyText?.SetText("");

            if (cultureDropdown != null)
            {
                cultureDropdown.options.Clear();
                cultureDropdown.options.Add(new TMP_Dropdown.OptionData("No cultures"));
                cultureDropdown.value = 0;
                cultureDropdown.interactable = false;
                cultureDropdown.RefreshShownValue();
            }
            if (raceDropdown != null)
            {
                raceDropdown.options.Clear();
                raceDropdown.options.Add(new TMP_Dropdown.OptionData("No races"));
                raceDropdown.value = 0;
                raceDropdown.interactable = false;
                raceDropdown.RefreshShownValue();
            }
            if (languageDropdown != null)
            {
                languageDropdown.options.Clear();
                languageDropdown.options.Add(new TMP_Dropdown.OptionData("No languages"));
                languageDropdown.value = 0;
                languageDropdown.interactable = false;
                languageDropdown.RefreshShownValue();
            }
            if (religionDropdown != null)
            {
                religionDropdown.options.Clear();
                religionDropdown.options.Add(new TMP_Dropdown.OptionData("No religions"));
                religionDropdown.value = 0;
                religionDropdown.interactable = false;
                religionDropdown.RefreshShownValue();
            }
        });
    }
    private void PopulateCultureDropdown(TMP_Dropdown dropdown, List<PercentEntry> entries)
    {
        Safe("PopulateCultureDropdown", () =>
        {
            if (dropdown == null) return;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.options.Clear();

            if (entries == null || entries.Count == 0)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData("No cultures"));
                dropdown.value = 0;
                dropdown.interactable = false;
                dropdown.RefreshShownValue();
                return;
            }

            dropdown.options.Add(new TMP_Dropdown.OptionData("Select a culture..."));
            foreach (PercentEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                float pct = entry.percent;
                dropdown.options.Add(new TMP_Dropdown.OptionData($"{entry.key} ({pct:0.#}%)"));
            }

            dropdown.value = 0;
            dropdown.interactable = true;
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(idx =>
            {
                if (idx <= 0) return;
                int index = idx - 1;
                if (entries != null && index < entries.Count)
                {
                    ShowSubInfo(entries[index].key, entries[index]);
                    dropdown.value = 0;
                    dropdown.RefreshShownValue();
                }
            });
        });
    }
    private void PopulateRaceDropdown(TMP_Dropdown dropdown, List<PercentEntry> entries)
    {
        Safe("PopulateRaceDropdown", () =>
        {
            if (dropdown == null) return;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.options.Clear();

            if (entries == null || entries.Count == 0)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData("No races"));
                dropdown.value = 0;
                dropdown.interactable = false;
                dropdown.RefreshShownValue();
                return;
            }

            dropdown.options.Add(new TMP_Dropdown.OptionData("Select a race..."));
            foreach (PercentEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                dropdown.options.Add(new TMP_Dropdown.OptionData($"{entry.key} ({entry.percent:0.#}%)"));
            }

            dropdown.value = 0;
            dropdown.interactable = true;
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(idx =>
            {
                if (idx <= 0) return;
                int index = idx - 1;
                if (entries != null && index < entries.Count)
                {
                    ShowSubInfo(entries[index].key, entries[index]);
                    dropdown.value = 0;
                    dropdown.RefreshShownValue();
                }
            });
        });
    }
    private void PopulateLanguagesDropdown(TMP_Dropdown dropdown, List<string> languages)
    {
        Safe("PopulateLanguagesDropdown", () =>
        {
            if (dropdown == null) return;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.options.Clear();

            if (languages == null || languages.Count == 0)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData("No languages"));
                dropdown.value = 0;
                dropdown.interactable = false;
                dropdown.RefreshShownValue();
                return;
            }

            dropdown.options.Add(new TMP_Dropdown.OptionData("Select a language..."));
            foreach (string lang in languages.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(lang)) continue;
                dropdown.options.Add(new TMP_Dropdown.OptionData(lang));
            }

            dropdown.value = 0;
            dropdown.interactable = true;
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(idx =>
            {
                if (idx <= 0) return;
                string lang = dropdown.options[idx].text;
                ShowSubInfo(lang, null);
                dropdown.value = 0;
                dropdown.RefreshShownValue();
            });
        });
    }
    private void PopulateReligionDropdown(TMP_Dropdown dropdown, List<string> religions)
    {
        Safe("PopulateReligionDropdown", () =>
        {
            if (dropdown == null) return;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.options.Clear();

            if (religions == null || religions.Count == 0)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData("No religions"));
                dropdown.value = 0;
                dropdown.interactable = false;
                dropdown.RefreshShownValue();
                return;
            }

            dropdown.options.Add(new TMP_Dropdown.OptionData("Select a religion..."));
            foreach (string rel in religions.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(rel)) continue;
                dropdown.options.Add(new TMP_Dropdown.OptionData(rel));
            }

            dropdown.value = 0;
            dropdown.interactable = true;
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(idx =>
            {
                if (idx <= 0) return;
                string rel = dropdown.options[idx].text;
                ShowSubInfo(rel, null);
                dropdown.value = 0;
                dropdown.RefreshShownValue();
            });
        });
    }
    private void ShowSubInfo(string key, object subInfo)
    {
        Safe("ShowSubInfo", () =>
        {
            if (string.IsNullOrWhiteSpace(key) || subInfoPrefab == null) return;

            Transform parent = _rootCanvas != null ? _rootCanvas.transform : transform.root;
            GameObject panel = Instantiate(subInfoPrefab, parent);

            foreach (MonoBehaviour mb in panel.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                Type t = mb.GetType();
                if (TryInvokeInitializeSubInfo(mb, t, "Initialize", key, subInfo)) return;
                if (TryInvokeInitializeSubInfo(mb, t, "Init", key, subInfo)) return;
                if (TryInvokeInitializeSubInfo(mb, t, "Set", key, subInfo)) return;
            }
        });
    }
    private bool TryInvokeInitializeSubInfo(object target, Type targetType, string methodName, string key, object subInfo)
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
                if (ps.Length == 2 && ps[0].ParameterType == typeof(string))
                {
                    m.Invoke(target, new object[] { key, subInfo });
                    return true;
                }
                if (ps.Length == 1 && subInfo != null && ps[0].ParameterType == subInfo.GetType())
                {
                    m.Invoke(target, new[] { subInfo });
                    return true;
                }
            }
        }
        catch { }
        return false;
    }
    private void RefreshHistoryTab()
    {
        Safe("RefreshHistoryTab", () =>
        {
            SetSimpleTabTitle(historyTitleText, "History");
            var hist = _data?.history;
            LogValue("history.exists", hist != null);
            if (hist == null)
            {
                SetHistoryFieldsEmpty();
                return;
            }

            if (historyTimelineEntriesText != null)
            {
                string val = (hist.timelineEntries != null && hist.timelineEntries.Length > 0)
                    ? string.Join("\n", hist.timelineEntries.Select(x => $"• {x}"))
                    : "";
                LogValue("history.timelineEntries", val);
                historyTimelineEntriesText.text = val;
            }

            if (historyRulingFamilyMembersText != null)
            {
                string val = (hist.rulingFamilyMembers != null && hist.rulingFamilyMembers.Length > 0)
                    ? string.Join("\n", hist.rulingFamilyMembers.Select(x => $"• {x}"))
                    : "";
                LogValue("history.rulingFamilyMembers", val);
                historyRulingFamilyMembersText.text = val;
            }

            if (historyBodyText != null)
            {
                List<string> parts = new();
                if (hist.timelineEntries != null && hist.timelineEntries.Length > 0)
                {
                    parts.Add("Timeline:");
                    parts.Add(string.Join("\n", hist.timelineEntries.Select(x => $"• {x}")));
                }
                if (hist.rulingFamilyMembers != null && hist.rulingFamilyMembers.Length > 0)
                {
                    parts.Add("Ruling Family:");
                    parts.Add(string.Join("\n", hist.rulingFamilyMembers.Select(x => $"• {x}")));
                }
                string val = string.Join("\n", parts);
                LogValue("history.body", val);
                historyBodyText.text = val;
            }
        });
    }
    private void SetHistoryFieldsEmpty()
    {
        historyTimelineEntriesText?.SetText("");
        historyRulingFamilyMembersText?.SetText("");
        historyBodyText?.SetText("");
    }
    private void RefreshGeographyTab()
    {
        Safe("RefreshGeographyTab", () =>
        {
            if (_point == null) return;

            float parentAreaSqMi = MapPointGeographyUtility.ComputeMapPointAreaSqMi(_point, unityUnitsToMiles);
            geoTotalAreaText?.SetText($"Total Area: {parentAreaSqMi:0.#} sq mi");
            LogValue("geography.totalAreaSqMi", parentAreaSqMi);

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
                    geoWildernessTerrainPercentText.text = "Terrain breakdown: parent area is 0.";
                }
                else if (_geoWilderness.Count == 0)
                {
                    geoWildernessTerrainPercentText.text = "No Wilderness sub-regions found.";
                }
                else
                {
                    var areaByTerrain = MapPointGeographyUtility.ComputeWildernessAreaByTerrainSqMi(_geoWilderness, unityUnitsToMiles);
                    var lines = areaByTerrain.OrderByDescending(kv => kv.Value)
                        .Select(kv => $"{kv.Key}: {(kv.Value / parentAreaSqMi * 100f):0.#}% ({kv.Value:0.#} sq mi)")
                        .ToList();
                    geoWildernessTerrainPercentText.text = string.Join("\n", lines);
                }
            }
        });
    }
    private void SetupChildDropdown(TMP_Dropdown dropdown, List<MapPoint> points)
    {
        Safe("SetupChildDropdown", () =>
        {
            if (dropdown == null) return;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("Select..."));

            if (points != null)
            {
                foreach (MapPoint p in points)
                {
                    string label = (p != null && !string.IsNullOrWhiteSpace(p.displayName))
                       ? p.displayName
                       : (p?.pointId ?? "Unknown");
                    dropdown.options.Add(new TMP_Dropdown.OptionData(label));
                }
            }

            dropdown.value = 0;
            dropdown.interactable = true;
            dropdown.RefreshShownValue();

            dropdown.onValueChanged.AddListener(idx =>
            {
                Safe("SetupChildDropdown.onValueChanged", () =>
                {
                    if (idx <= 0) return;
                    int realIndex = idx - 1;
                    if (points == null || realIndex < 0 || realIndex >= points.Count) return;

                    MapPointGeographyUtility.SimulateMapPointClick(points[realIndex]);

                    dropdown.value = 0;
                    dropdown.RefreshShownValue();
                });
            });
        });
    }

    private void RefreshRealmManagementTab()
    {
        Safe("RefreshRealmManagementTab", () =>
        {
            // Clear vassal lists
            _realmVassalIds.Clear();
            _realmVassalDisplayNames.Clear();
            realmVassalsDropdown?.onValueChanged.RemoveAllListeners();
            realmVassalsDropdown?.options.Clear();

            var stats = SettlementStatsCache.GetStatsOrNull(_settlementId);
            var vassals = stats?.directVassals?.Where(v => v != null && !v.isCapital)
                .OrderBy(v => v.vassalDisplayName ?? v.vassalSettlementId)
                .ToList() ?? new List<SettlementStatsCache.VassalComputedSummary>();

            // Set laws field
            if (realmLawsField != null)
            {
                string val = _data?.feudal?.laws ?? "";
                LogValue("feudal.laws", val);
                realmLawsField.text = val;
            }

            // Populate vassal dropdown
            if (vassals.Count == 0 || realmVassalsDropdown == null)
            {
                if (realmVassalsDropdown != null)
                {
                    realmVassalsDropdown.options.Add(new TMP_Dropdown.OptionData("No vassals."));
                    realmVassalsDropdown.value = 0;
                    realmVassalsDropdown.interactable = false;
                    realmVassalsDropdown.RefreshShownValue();
                }
                // Clear display fields
                SetRealmDetailFields("", "", "", "", "");
                if (realmSelectedVassalNameText != null) realmSelectedVassalNameText.text = "";
            }
            else
            {
                realmVassalsDropdown.options.Add(new TMP_Dropdown.OptionData("Select a vassal..."));
                foreach (var v in vassals)
                {
                    if (v == null) continue;
                    _realmVassalIds.Add(v.vassalSettlementId);
                    string label = !string.IsNullOrWhiteSpace(v.vassalDisplayName)
                        ? v.vassalDisplayName
                        : SettlementNameResolver.Resolve(v.vassalSettlementId);
                    _realmVassalDisplayNames.Add(label);
                    realmVassalsDropdown.options.Add(new TMP_Dropdown.OptionData(label));
                }
                realmVassalsDropdown.value = 0;
                realmVassalsDropdown.interactable = true;
                realmVassalsDropdown.RefreshShownValue();

                // Clear detail fields and selected name
                SetRealmDetailFields("", "", "", "", "");
                if (realmSelectedVassalNameText != null) realmSelectedVassalNameText.text = "";

                // Selection handler
                realmVassalsDropdown.onValueChanged.AddListener(idx =>
                {
                    Safe("realmVassalsDropdown.onValueChanged", () =>
                    {
                        if (idx <= 0) return;
                        int vIdx = idx - 1;
                        if (vIdx < 0 || vIdx >= _realmVassalIds.Count) return;
                        string vid = _realmVassalIds[vIdx];

                        // Update the selected vassal name
                        if (realmSelectedVassalNameText != null && vIdx < _realmVassalDisplayNames.Count)
                            realmSelectedVassalNameText.text = _realmVassalDisplayNames[vIdx];

                        // Clear existing contract details
                        SetRealmDetailFields("", "", "", "", "");

                        // Load the contract
                        LoadVassalContract(vid);

                        realmVassalsDropdown.value = 0;
                        realmVassalsDropdown.RefreshShownValue();
                    });
                });
            }

            // Display council members
            RefreshCouncilMembers();

            // Display liege contract
            RefreshLiegeSection();
        });
    }
    private void RefreshCouncilMembers()
    {
        Safe("RefreshCouncilMembers", () =>
        {
            if (castellanNameText != null)
            {
                string id = GetCouncilMemberId("castellan");
                string name = !string.IsNullOrWhiteSpace(id) ? ResolveCharacterDisplayName(id) : "Unassigned";
                LogValue("council.castellanCharacterId", id);
                castellanNameText.text = name;
            }
            if (marshallNameText != null)
            {
                string id = GetCouncilMemberId("marshall");
                string name = !string.IsNullOrWhiteSpace(id) ? ResolveCharacterDisplayName(id) : "Unassigned";
                LogValue("council.marshallCharacterId", id);
                marshallNameText.text = name;
            }
            if (stewardNameText != null)
            {
                string id = GetCouncilMemberId("steward");
                string name = !string.IsNullOrWhiteSpace(id) ? ResolveCharacterDisplayName(id) : "Unassigned";
                LogValue("council.stewardCharacterId", id);
                stewardNameText.text = name;
            }
            if (diplomatNameText != null)
            {
                string id = GetCouncilMemberId("diplomat");
                string name = !string.IsNullOrWhiteSpace(id) ? ResolveCharacterDisplayName(id) : "Unassigned";
                LogValue("council.diplomatCharacterId", id);
                diplomatNameText.text = name;
            }
            if (spymasterNameText != null)
            {
                string id = GetCouncilMemberId("spymaster");
                string name = !string.IsNullOrWhiteSpace(id) ? ResolveCharacterDisplayName(id) : "Unassigned";
                LogValue("council.spymasterCharacterId", id);
                spymasterNameText.text = name;
            }
            if (headPriestNameText != null)
            {
                string id = GetCouncilMemberId("headPriest");
                string name = !string.IsNullOrWhiteSpace(id) ? ResolveCharacterDisplayName(id) : "Unassigned";
                LogValue("council.headPriestCharacterId", id);
                headPriestNameText.text = name;
            }
        });
    }
    private string GetCouncilMemberId(string role)
    {
        if (TryGetValueByPath(_data, $"feudal.{role}CharacterId", out object value) && value != null)
            return value.ToString();
        return null;
    }
    private void RefreshLiegeSection()
    {
        Safe("RefreshLiegeSection", () =>
        {
            string liegeId = _data?.liegeSettlementId;
            LogValue("feudal.liegeSettlementId", liegeId);
            if (string.IsNullOrWhiteSpace(liegeId))
            {
                if (liegeSettlementNameText != null) liegeSettlementNameText.text = "";
                if (liegeRulerNameText != null) liegeRulerNameText.text = "";
                if (liegeContractTermsText != null) liegeContractTermsText.text = "";
                if (liegeLevyContributionText != null) liegeLevyContributionText.text = "";
                if (liegeGoldContributionText != null) liegeGoldContributionText.text = "";
                return;
            }
            var liegeData = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
                DataPaths.Runtime_MapDataPath,
                DataPaths.Editor_MapDataPath,
                liegeId
            );
            string settlementName = liegeData?.displayName ?? SettlementNameResolver.Resolve(liegeId);
            string rulerName = liegeData?.main?.rulerDisplayName ?? "Unknown";

            float incomeRate = 0f;
            float levyRate = 0f;
            string terms = "Default";
            bool found = false;
            if (liegeData?.feudal?.vassalContracts != null)
            {
                foreach (var c in liegeData.feudal.vassalContracts)
                {
                    if (c == null) continue;
                    if (string.Equals(c.vassalSettlementId, _settlementId, StringComparison.OrdinalIgnoreCase))
                    {
                        incomeRate = Mathf.Clamp01(c.incomeTaxRate);
                        levyRate = Mathf.Clamp01(c.levyTaxRate);
                        terms = string.IsNullOrWhiteSpace(c.terms) ? "Default" : c.terms;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    foreach (var c in liegeData.feudal.vassalContracts)
                    {
                        if (c == null) continue;
                        if (string.Equals(c.vassalSettlementId, "main", StringComparison.OrdinalIgnoreCase))
                        {
                            incomeRate = Mathf.Clamp01(c.incomeTaxRate);
                            levyRate = Mathf.Clamp01(c.levyTaxRate);
                            terms = string.IsNullOrWhiteSpace(c.terms) ? "Default" : c.terms;
                            found = true;
                            break;
                        }
                    }
                }
            }
            string levyPct = $"{levyRate * 100f:0.#}%";
            string goldPct = $"{incomeRate * 100f:0.#}%";

            if (liegeSettlementNameText != null) liegeSettlementNameText.text = settlementName ?? "";
            if (liegeRulerNameText != null) liegeRulerNameText.text = rulerName ?? "";
            if (liegeContractTermsText != null) liegeContractTermsText.text = terms ?? "";
            if (liegeLevyContributionText != null) liegeLevyContributionText.text = levyPct;
            if (liegeGoldContributionText != null) liegeGoldContributionText.text = goldPct;

            LogValue("liege.settlementName", settlementName);
            LogValue("liege.rulerName", rulerName);
            LogValue("liege.terms", terms);
            LogValue("liege.levyRate", levyRate);
            LogValue("liege.incomeRate", incomeRate);
        });
    }
    private void LoadVassalContract(string vassalSettlementId)
    {
        Safe("LoadVassalContract", () =>
        {
            if (string.IsNullOrWhiteSpace(vassalSettlementId)) return;

            var vData = JsonDataLoader.TryLoadFromEitherPath<SettlementInfoData>(
                DataPaths.Runtime_MapDataPath,
                DataPaths.Editor_MapDataPath,
                vassalSettlementId
            );

            string settlementName = vData?.displayName ?? SettlementNameResolver.Resolve(vassalSettlementId);
            string rulerName = vData?.main?.rulerDisplayName ?? "Unknown";

            float incomeRate = 0.125f;
            float levyRate = 0.35f;
            string terms = "Default";
            bool found = false;

            var contracts = _data?.feudal?.vassalContracts;
            if (contracts != null)
            {
                foreach (var c in contracts)
                {
                    if (c == null) continue;
                    if (string.Equals(c.vassalSettlementId, vassalSettlementId, StringComparison.OrdinalIgnoreCase))
                    {
                        incomeRate = Mathf.Clamp01(c.incomeTaxRate);
                        levyRate = Mathf.Clamp01(c.levyTaxRate);
                        terms = string.IsNullOrWhiteSpace(c.terms) ? "Default" : c.terms;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    foreach (var c in contracts)
                    {
                        if (c == null) continue;
                        if (string.Equals(c.vassalSettlementId, "main", StringComparison.OrdinalIgnoreCase))
                        {
                            incomeRate = Mathf.Clamp01(c.incomeTaxRate);
                            levyRate = Mathf.Clamp01(c.levyTaxRate);
                            terms = string.IsNullOrWhiteSpace(c.terms) ? "Default" : c.terms;
                            found = true;
                            break;
                        }
                    }
                }
            }

            string levyPct = $"{levyRate * 100f:0.#}%";
            string goldPct = $"{incomeRate * 100f:0.#}%";

            SetRealmDetailFields(settlementName, rulerName, terms, levyPct, goldPct);

            LogValue("vassal.settlementName", settlementName);
            LogValue("vassal.rulerName", rulerName);
            LogValue("vassal.terms", terms);
            LogValue("vassal.incomeRate", incomeRate);
            LogValue("vassal.levyRate", levyRate);
        });
    }
    #endregion

    #region Helpers
    private void ShowPanel(InfoPanelType type)
    {
        Safe($"ShowPanel({type})", () =>
        {
            mainPanel?.SetActive(type == InfoPanelType.Main);
            armyPanel?.SetActive(type == InfoPanelType.Army);
            economyPanel?.SetActive(type == InfoPanelType.Economy);
            culturalPanel?.SetActive(type == InfoPanelType.Cultural);
            historyPanel?.SetActive(type == InfoPanelType.History);
            geographyPanel?.SetActive(type == InfoPanelType.Geography);
            realmManagementPanel?.SetActive(type == InfoPanelType.RealmManagement);

            if (type == InfoPanelType.Main) RefreshMainTabExtras();
            if (type == InfoPanelType.Army) RefreshArmyTab();
            if (type == InfoPanelType.Economy) RefreshEconomyTab();
            if (type == InfoPanelType.Cultural) RefreshCulturalTab();
            if (type == InfoPanelType.History) RefreshHistoryTab();
            if (type == InfoPanelType.Geography) RefreshGeographyTab();
            if (type == InfoPanelType.RealmManagement) RefreshRealmManagementTab();

            transform.SetAsLastSibling();
        });
    }
    private static void SetSimpleTabTitle(TMP_Text title, string tabName)
    {
        if (title != null) title.text = tabName ?? "";
    }
    private static string FormatPercentEntries(List<PercentEntry> entries)
    {
        if (entries == null || entries.Count == 0) return "";
        List<string> lines = new();
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.key)) continue;
            lines.Add($"• {e.key}: {e.percent:0.#}%");
        }
        return string.Join("\n", lines);
    }
    private void SetRealmDetailFields(string settlementName, string rulerName, string terms, string levyRate, string incomeRate)
    {
        Safe("SetRealmDetailFields", () =>
        {
            if (realmVassalSettlementNameText != null) realmVassalSettlementNameText.text = settlementName ?? "";
            if (realmVassalRulerNameText != null) realmVassalRulerNameText.text = rulerName ?? "";
            if (realmVassalContractTermsText != null) realmVassalContractTermsText.text = terms ?? "";
            if (realmVassalLevyContributionText != null) realmVassalLevyContributionText.text = levyRate ?? "";
            if (realmVassalGoldContributionText != null) realmVassalGoldContributionText.text = incomeRate ?? "";
        });
    }
    private bool HasDirectNonCapitalVassals()
    {
        if (string.IsNullOrWhiteSpace(_settlementId)) return false;
        var stats = SettlementStatsCache.GetStatsOrNull(_settlementId);
        return stats?.directVassals?.Any(v => v != null && !v.isCapital) ?? false;
    }
    private void OpenCapitalSettlement()
    {
        Safe("OpenCapitalSettlement", () =>
        {
            string capId = _data?.feudal?.capitalSettlementId ?? _data?.capitalSettlementId ?? "";
            if (string.IsNullOrWhiteSpace(capId))
            {
                Debug.LogWarning("[InfoWindowManager] No capitalSettlementId defined.");
                return;
            }
            MapNavigationUtil.OpenSettlementById(capId);
        });
    }
    private void OpenMapPanelIfYouHaveOne()
    {
        // stub
    }
    private void Safe(string context, Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            Debug.LogWarning($"[InfoWindowManager] {context} threw: {ex}", this);
        }
    }
    private void RegisterAllTooltips()
    {
        // optional
    }
    private static string TryGetStringByPaths(object root, params string[] paths)
    {
        if (root == null || paths == null) return "";
        foreach (string path in paths)
        {
            if (TryGetValueByPath(root, path, out object v) && v != null)
                return v.ToString();
        }
        return "";
    }
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
        PropertyInfo prop = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (prop != null)
        {
            try { value = prop.GetValue(obj); return true; } catch { return false; }
        }
        FieldInfo field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (field != null)
        {
            try { value = field.GetValue(obj); return true; } catch { return false; }
        }
        return false;
    }
    private void LogValue(string propertyPath, object value)
    {
        Debug.Log($"[InfoWindowManager] Loaded {propertyPath}: {value}");
    }

    private sealed class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void Bind(InfoWindowManager owner, string text) { }
        public void OnPointerEnter(PointerEventData eventData) { }
        public void OnPointerExit(PointerEventData eventData) { }
    }

    // Close the Helpers region
    #endregion
}