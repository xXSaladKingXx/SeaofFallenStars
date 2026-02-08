using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelationshipRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text relationshipText; // e.g. "Parent"
    [SerializeField] private TMP_Text nameText;         // e.g. "Lady X"
    [SerializeField] private Button openButton;

    private string _characterId;
    private System.Action<string> _onOpen;

    public void Set(string relationLabel, string characterDisplayName, string characterId, System.Action<string> onOpen)
    {
        _characterId = characterId;
        _onOpen = onOpen;

        if (relationshipText != null) relationshipText.text = relationLabel ?? "";
        if (nameText != null) nameText.text = characterDisplayName ?? "";

        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(() => _onOpen?.Invoke(_characterId));
        }
    }
}
