using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SeaOfFallenStars.WorldData;
public class MapViewerPanelManager : MonoBehaviour
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
        if (titleText != null) titleText.text = (data != null ? data.displayName : "Map");
        if (bodyText != null) bodyText.text = "Map panel prefab loaded. Implement actual map viewer later.\n\nmapUrlOrPath:\n" + (data != null ? data.mapUrlOrPath : "");
    }
}
