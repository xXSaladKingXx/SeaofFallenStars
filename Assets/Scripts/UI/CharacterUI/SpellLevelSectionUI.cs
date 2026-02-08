using TMPro;
using UnityEngine;

public class SpellLevelSectionUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text headerText;      // e.g. "Level 1"
    [SerializeField] private TMP_Text slotsText;       // e.g. "Slots: 4/2"

    [Header("List")]
    [SerializeField] private Transform listParent;
    [SerializeField] private SpellRowUI spellRowPrefab;

    public void Set(SpellLevelBlock level)
    {
        if (level == null) return;

        if (headerText != null)
            headerText.text = (level.level == 0) ? "Cantrips" : $"Level {level.level}";

        if (slotsText != null)
        {
            if (level.level == 0) slotsText.text = "";
            else slotsText.text = $"Slots: {level.slotsMax}/{level.slotsUsed}";
        }

        if (listParent == null || spellRowPrefab == null)
            return;

        // clear existing
        for (int i = listParent.childCount - 1; i >= 0; i--)
            Destroy(listParent.GetChild(i).gameObject);

        if (level.spells == null) return;

        foreach (var s in level.spells)
        {
            if (s == null) continue;
            var row = Instantiate(spellRowPrefab, listParent);
            row.Set(s);
        }
    }
}
