using TMPro;
using UnityEngine;

public class CharacterSheetSectionView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    public void Set(string title, string body)
    {
        if (titleText != null) titleText.text = title ?? "";
        if (bodyText != null) bodyText.text = body ?? "";
    }
}
