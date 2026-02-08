using TMPro;
using UnityEngine;

public class ProficiencyRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    [Header("Markers (optional)")]
    [SerializeField] private GameObject proficientMarker;
    [SerializeField] private GameObject expertiseMarker;

    public void Set(string label, int value, SkillProficiencyLevel profLevel)
    {
        if (nameText != null) nameText.text = label ?? "";
        if (valueText != null) valueText.text = FormatSigned(value);

        if (proficientMarker != null)
            proficientMarker.SetActive(profLevel == SkillProficiencyLevel.Proficient || profLevel == SkillProficiencyLevel.Expertise);

        if (expertiseMarker != null)
            expertiseMarker.SetActive(profLevel == SkillProficiencyLevel.Expertise);
    }

    public void SetSavingThrow(string label, int value, bool proficient)
    {
        if (nameText != null) nameText.text = label ?? "";
        if (valueText != null) valueText.text = FormatSigned(value);

        if (proficientMarker != null)
            proficientMarker.SetActive(proficient);

        if (expertiseMarker != null)
            expertiseMarker.SetActive(false);
    }

    private static string FormatSigned(int v) => (v >= 0) ? $"+{v}" : v.ToString();
}
