using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ZANA_WORLD_AUTHORING || UNITY_EDITOR
using Zana.WorldAuthoring;
#endif

public class TravelGroupWindowManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;

    [Header("Members Dropdown")]
    [SerializeField] private TMP_Dropdown membersDropdown;

    [Header("Travel Stats")]
    [SerializeField] private TMP_InputField speedInputField;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private Button confirmMoveButton;
    [SerializeField] private Button closeButton;

    [Header("Detail Panel Prefabs")]
    [SerializeField] private GameObject characterStatPanelPrefab;
    [SerializeField] private GameObject armyInfoWindowPrefab;

    [Header("Travel Group Prefab (for nested groups)")]
    [Tooltip("If null, this script will instantiate the current window GameObject to open nested groups.")]
    [SerializeField] private GameObject travelGroupWindowPrefab;

    private MapPoint _point;  // the associated TravelGroup MapPoint
    private readonly List<HexAxial> _path = new List<HexAxial>();

    // For dropdown index -> action mapping
    private readonly List<Action> _dropdownActions = new List<Action>();

    public event Action PathChanged;
    public IReadOnlyList<HexAxial> CurrentPath => _path;

    private float _hexSizeUnits;
    private float _milesPerHex;

    public void Initialize(MapPoint point)
    {
        _point = point;

        // Get map's hex scaling values
        MapManager manager = FindObjectOfType<MapManager>();
        if (manager != null)
        {
            _hexSizeUnits = manager.HexSizeUnits;
            _milesPerHex = manager.MilesPerHex;
        }
        else
        {
            _hexSizeUnits = 30f;
            _milesPerHex = 25f;
        }

        // Set window title
        if (titleText != null)
            titleText.text = (!string.IsNullOrEmpty(point.displayName) ? point.displayName : "Travel Group");

        // Populate members dropdown (groups + characters + armies)
        RefreshMembersList();

        // Init speed input
        if (speedInputField != null)
        {
            speedInputField.onValueChanged.RemoveAllListeners();
            speedInputField.onValueChanged.AddListener(_ => UpdateDistanceAndDuration());

            int defaultSpeed = 0;

            // If group has any armies, use the slowest army's speed as default
            if (_point.armyIds != null && _point.armyIds.Length > 0)
            {
                foreach (string armyId in _point.armyIds)
                {
                    var armyData = ArmyDataLoader.TryLoad(armyId);
                    if (armyData != null)
                    {
                        if (defaultSpeed == 0 || armyData.speed < defaultSpeed)
                            defaultSpeed = armyData.speed;
                    }
                }
            }

            if (defaultSpeed <= 0)
                defaultSpeed = 24;

            speedInputField.text = defaultSpeed.ToString();
        }

        // Buttons
        if (confirmMoveButton != null)
        {
            confirmMoveButton.onClick.RemoveAllListeners();
            confirmMoveButton.onClick.AddListener(ConfirmMove);
            confirmMoveButton.interactable = false;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                _path.Clear();
                PathChanged?.Invoke();
                Destroy(gameObject);
            });
        }

        UpdateDistanceAndDuration();
    }

    private void RefreshMembersList()
    {
        if (membersDropdown == null)
            return;

        membersDropdown.onValueChanged.RemoveAllListeners();
        membersDropdown.ClearOptions();
        _dropdownActions.Clear();

        var options = new List<TMP_Dropdown.OptionData>();
        bool hasEntries = false;

        // 1) Child travel groups (nested)
        if (_point != null)
        {
            for (int i = 0; i < _point.transform.childCount; i++)
            {
                var child = _point.transform.GetChild(i);
                if (child == null) continue;

                var mp = child.GetComponent<MapPoint>();
                if (mp == null) continue;
                if (mp.infoKind != MapPoint.InfoKind.TravelGroup) continue;

                hasEntries = true;

                string label = $"Group: {(string.IsNullOrWhiteSpace(mp.displayName) ? mp.pointId : mp.displayName)}";
                options.Add(new TMP_Dropdown.OptionData(label));
                _dropdownActions.Add(() => OpenTravelGroup(mp));
            }
        }

        // 2) Characters
        if (_point.characterIds != null)
        {
            foreach (string cid in _point.characterIds.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                string charId = cid.Trim();
                if (charId == "") continue;

                hasEntries = true;

                string charName = CharacterNameResolver.Resolve(charId);
                if (string.IsNullOrEmpty(charName)) charName = charId;

                options.Add(new TMP_Dropdown.OptionData($"Character: {charName}"));
                _dropdownActions.Add(() => OpenCharacterSheet(charId));
            }
        }

        // 3) Armies
        if (_point.armyIds != null)
        {
            foreach (string aid in _point.armyIds.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                string armyId = aid.Trim();
                if (armyId == "") continue;

                hasEntries = true;

                string label;
                ArmyInfoData armyData = ArmyDataLoader.TryLoad(armyId);
                if (armyData != null)
                {
                    string commName = armyData.primaryCommanderDisplayName;
                    if (string.IsNullOrWhiteSpace(commName) &&
                        !string.IsNullOrWhiteSpace(armyData.primaryCommanderCharacterId))
                    {
                        commName = CharacterNameResolver.Resolve(armyData.primaryCommanderCharacterId);
                    }

                    if (string.IsNullOrWhiteSpace(commName))
                        commName = "Unknown Commander";

                    label = $"Army: {armyData.GetBestDisplayName()} ({armyData.totalArmy:N0} troops)";
                }
                else
                {
                    label = $"Army (ID: {armyId})";
                }

                options.Add(new TMP_Dropdown.OptionData(label));
                _dropdownActions.Add(() => OpenArmyWindow(armyId));
            }
        }

        if (!hasEntries)
        {
            options.Add(new TMP_Dropdown.OptionData("No members"));
            _dropdownActions.Add(null);
        }

        membersDropdown.AddOptions(options);
        membersDropdown.value = 0;
        membersDropdown.RefreshShownValue();

        membersDropdown.onValueChanged.AddListener(OnMemberDropdownSelected);
    }

    private void OnMemberDropdownSelected(int index)
    {
        if (index < 0 || index >= _dropdownActions.Count)
            return;

        var action = _dropdownActions[index];
        action?.Invoke();
    }

    private void OpenTravelGroup(MapPoint travelGroupPoint)
    {
        if (travelGroupPoint == null) return;

        Transform parent = transform.parent != null ? transform.parent : transform;

        GameObject prefab = travelGroupWindowPrefab != null ? travelGroupWindowPrefab : gameObject;
        GameObject window = Instantiate(prefab, parent);

        // Offset slightly
        RectTransform rt = window.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition += new Vector2(60f, -20f);

        var mgr = window.GetComponent<TravelGroupWindowManager>();
        if (mgr != null)
            mgr.Initialize(travelGroupPoint);
    }

    public void AddHexToPath(HexAxial hex)
    {
        _path.Add(hex);
        PathChanged?.Invoke();
        UpdateDistanceAndDuration();
    }

    private void UpdateDistanceAndDuration()
    {
        float totalMiles = 0f;
        int totalSteps = 0;

        if (_path.Count > 0)
        {
            Vector2 currentPos = new Vector2(_point.transform.position.x, _point.transform.position.y);
            HexAxial startHex = HexGrid.LocalXYToAxial(currentPos, _hexSizeUnits);

            HexAxial prevHex = startHex;
            foreach (HexAxial hx in _path)
            {
                int stepDist = HexGrid.Distance(prevHex, hx);
                totalSteps += stepDist;
                prevHex = hx;
            }

            totalMiles = totalSteps * _milesPerHex;
        }

        float speed = 0f;
        if (speedInputField != null && !string.IsNullOrWhiteSpace(speedInputField.text))
            float.TryParse(speedInputField.text.Trim(), out speed);

        if (speed <= 0) speed = 1;
        float days = totalMiles / speed;

        if (distanceText != null) distanceText.text = $"{totalMiles:0.#} miles";

        if (durationText != null)
        {
            if (totalMiles <= 0) durationText.text = "0 days";
            else if (days >= 1) durationText.text = $"{days:0.#} days";
            else durationText.text = $"{days * 24:0.#} hours";
        }

        if (confirmMoveButton != null)
            confirmMoveButton.interactable = (_path.Count > 0);
    }

    private void ConfirmMove()
    {
        if (_path.Count == 0) return;

        HexAxial finalHex = _path[_path.Count - 1];
        Vector2 targetPos = HexGrid.AxialToLocalXY(finalHex, _hexSizeUnits);

        Vector3 newWorldPos = new Vector3(targetPos.x, targetPos.y, _point.transform.position.z);
        _point.transform.position = newWorldPos;

        // Persist coordinates for all travelable members (characters/armies) in this group and nested groups
        SaveMembersCoordinatesRecursive(_point, new Vector2(newWorldPos.x, newWorldPos.y));

        _path.Clear();
        PathChanged?.Invoke();
        UpdateDistanceAndDuration();
    }

    private static void SaveMembersCoordinatesRecursive(MapPoint group, Vector2 pos)
    {
        if (group == null) return;

#if ZANA_WORLD_AUTHORING || UNITY_EDITOR
        // 1) Save this group's character/army members
        if (group.characterIds != null)
        {
            foreach (var cid in group.characterIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                WorldDataTravelCoordinatesService.SaveCharacterMapPosition(cid.Trim(), pos, preferRuntimePath: true);
        }

        if (group.armyIds != null)
        {
            foreach (var aid in group.armyIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                WorldDataTravelCoordinatesService.SaveArmyMapPosition(aid.Trim(), pos, preferRuntimePath: true);
        }
#endif

        // 2) Recurse into child travel groups (if you nest them)
        for (int i = 0; i < group.transform.childCount; i++)
        {
            var child = group.transform.GetChild(i);
            if (child == null) continue;

            var mp = child.GetComponent<MapPoint>();
            if (mp == null) continue;
            if (mp.infoKind != MapPoint.InfoKind.TravelGroup) continue;

            SaveMembersCoordinatesRecursive(mp, pos);
        }
    }

    private void OpenCharacterSheet(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || characterStatPanelPrefab == null) return;

        Transform parent = transform.parent != null ? transform.parent : transform;
        GameObject panel = Instantiate(characterStatPanelPrefab, parent);

        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition += new Vector2(50f, 0f);

        CharacterStatsPanelManager cs = panel.GetComponentInChildren<CharacterStatsPanelManager>(true);
        if (cs != null) cs.Initialize(characterId);
    }

    private void OpenArmyWindow(string armyId)
    {
        if (string.IsNullOrEmpty(armyId) || armyInfoWindowPrefab == null) return;

        Transform parent = transform.parent != null ? transform.parent : transform;
        GameObject panel = Instantiate(armyInfoWindowPrefab, parent);

        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition += new Vector2(50f, 0f);

        ArmyInfoWindowManager armyWindow = panel.GetComponentInChildren<ArmyInfoWindowManager>(true);
        if (armyWindow != null) armyWindow.Initialize(armyId);
    }
}
