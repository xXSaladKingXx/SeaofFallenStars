using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SeaOfFallenStars.WorldData;
public class PointOfInterestInfoWindowManager : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button moreInfoButton;
    [SerializeField] private Button charactersButton;

    private SettlementInfoData _data;

    private void Awake()
    {
        if (GetComponent<MapInputBlocker>() == null)
            gameObject.AddComponent<MapInputBlocker>();

        if (closeButton != null)
            closeButton.onClick.AddListener(() => Destroy(gameObject));

        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(() => Debug.Log("[POI] MoreInfo pressed (hook prefab later)."));

        if (charactersButton != null)
            charactersButton.onClick.AddListener(() => Debug.Log("[POI] Characters pressed (hook later)."));
    }

    public void Initialize(SettlementInfoData data)
    {
        _data = data;

        if (titleText != null) titleText.text = data != null ? data.displayName : "POI";
        if (bodyText != null) bodyText.text = data != null && data.main != null ? data.main.description : "";
    }
}
