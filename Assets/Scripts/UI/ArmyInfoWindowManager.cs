using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SeaOfFallenStars.WorldData;

public class ArmyInfoWindowManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text commanderText;
    [SerializeField] private TMP_Text totalTroopsText;
    [SerializeField] private TMP_Text menAtArmsText;
    [SerializeField] private Button closeButton;

    private ArmyInfoData _armyData;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }

    /// <summary>
    /// Preferred entrypoint from other scripts: pass an armyId and we load it.
    /// This also fixes your TravelGroupWindowManager "string -> ArmyInfoData" mismatch.
    /// </summary>
    public void Initialize(string armyId)
    {
        if (string.IsNullOrWhiteSpace(armyId))
        {
            Debug.LogWarning("ArmyInfoWindowManager.Initialize called with empty armyId.");
            return;
        }

        ArmyInfoData data = null;
        try
        {
            data = ArmyDataLoader.TryLoad(armyId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"ArmyDataLoader.TryLoad failed for '{armyId}': {ex.Message}");
        }

        Initialize(data);
    }

    public void Initialize(ArmyInfoData armyData)
    {
        _armyData = armyData;

        if (_armyData == null)
        {
            Debug.LogWarning("ArmyInfoWindowManager.Initialize called with null ArmyInfoData");
            return;
        }

        string commanderName = ResolveCommanderName(_armyData);

        // Title
        if (titleText != null)
        {
            // Prefer explicit displayName, else commander-based fallback, else id.
            if (!string.IsNullOrWhiteSpace(_armyData.displayName))
                titleText.text = _armyData.displayName;
            else if (!string.IsNullOrWhiteSpace(commanderName))
                titleText.text = $"Army of {commanderName}";
            else
                titleText.text = !string.IsNullOrWhiteSpace(_armyData.armyId) ? _armyData.armyId : "Army";
        }

        // Commander
        if (commanderText != null)
            commanderText.text = string.IsNullOrWhiteSpace(commanderName) ? "(none)" : commanderName;

        // Total troops
        if (totalTroopsText != null)
            totalTroopsText.text = _armyData.totalArmy.ToString("N0");

        // Men-at-arms breakdown (no string.Join ambiguity)
        if (menAtArmsText != null)
            menAtArmsText.text = BuildMenAtArmsText(_armyData);
    }

    private static string ResolveCommanderName(ArmyInfoData data)
    {
        if (data == null) return null;

        if (!string.IsNullOrWhiteSpace(data.primaryCommanderDisplayName))
            return data.primaryCommanderDisplayName;

        // If you want, we can resolve characterId -> displayName here, but that requires
        // a repository/loader call. Keep it non-invasive for now.
        if (!string.IsNullOrWhiteSpace(data.primaryCommanderCharacterId))
            return data.primaryCommanderCharacterId;

        return null;
    }

    private static string BuildMenAtArmsText(ArmyInfoData data)
    {
        if (data == null || data.menAtArms == null || data.menAtArms.Length == 0)
            return "Men-at-arms: (none)";

        var sb = new StringBuilder(256);

        for (int i = 0; i < data.menAtArms.Length; i++)
        {
            var stack = data.menAtArms[i];
            if (stack == null) continue;

            string id = stack.menAtArmsId;
            int count = stack.count;

            if (string.IsNullOrWhiteSpace(id)) continue;

            if (sb.Length > 0) sb.AppendLine();
            sb.Append(count > 0 ? $"{id} x{count}" : id);
        }

        return sb.Length > 0 ? sb.ToString() : "Men-at-arms: (none)";
    }
}
