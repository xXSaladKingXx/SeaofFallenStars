using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RulingFamilyTreePanelManager : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (GetComponent<MapInputBlocker>() == null)
            gameObject.AddComponent<MapInputBlocker>();

        if (closeButton != null)
            closeButton.onClick.AddListener(() => Destroy(gameObject));
    }

    public void Initialize(SettlementInfoData data)
    {
        if (titleText != null) titleText.text = (data != null ? data.displayName : "Ruling Family");
        if (bodyText != null)
        {
            var lines = (data != null && data.history != null) ? data.history.rulingFamilyMembers : null;
            bodyText.text = (lines != null) ? string.Join("\n", lines) : "No ruling family entries yet.";
        }
    }
}
