using TMPro;
using UnityEngine;

public class SpellRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text preparedText;
    [SerializeField] private TMP_Text notesText;

    public void Set(SpellEntry s)
    {
        if (s == null) return;

        if (nameText != null) nameText.text = s.name ?? "";
        if (preparedText != null) preparedText.text = s.prepared ? "Prepared" : "";
        if (notesText != null) notesText.text = s.notes ?? "";
    }
}
