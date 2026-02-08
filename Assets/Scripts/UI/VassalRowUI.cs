using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VassalRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rulerText;
    [SerializeField] private TMP_Text termsText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text troopsText;
    [SerializeField] private Button openVassalButton;

    private string _settlementId;
    private System.Action<string> _onOpen;

    public void Set(
        string settlementId,
        string settlementDisplayName,
        string rulerDisplayName,
        string terms,
        string incomeLine,
        string troopsLine,
        System.Action<string> onOpen
    )
    {
        _settlementId = settlementId;
        _onOpen = onOpen;

        if (nameText != null) nameText.text = settlementDisplayName ?? "";
        if (rulerText != null) rulerText.text = rulerDisplayName ?? "";
        if (termsText != null) termsText.text = terms ?? "";
        if (incomeText != null) incomeText.text = incomeLine ?? "";
        if (troopsText != null) troopsText.text = troopsLine ?? "";

        if (openVassalButton != null)
        {
            openVassalButton.onClick.RemoveAllListeners();
            openVassalButton.onClick.AddListener(() => _onOpen?.Invoke(_settlementId));
        }
    }
}
