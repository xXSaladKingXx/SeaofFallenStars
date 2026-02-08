using TMPro;
using UnityEngine;

public class AttackRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text attackBonusText;
    [SerializeField] private TMP_Text damageText;

    public void Set(AttackEntry a)
    {
        if (a == null) return;

        if (nameText != null) nameText.text = a.name ?? "";
        if (attackBonusText != null) attackBonusText.text = (a.attackBonus >= 0) ? $"+{a.attackBonus}" : a.attackBonus.ToString();
        if (damageText != null)
        {
            string dmg = a.damage ?? "";
            string type = a.damageType ?? "";
            damageText.text = string.IsNullOrWhiteSpace(type) ? dmg : $"{dmg} ({type})";
        }
    }
}
