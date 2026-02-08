using UnityEngine;
using UnityEngine.EventSystems;

public class SettlementMapClickCatcher : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SettlementMapManager manager;

    private void Awake()
    {
        if (manager == null)
            manager = GetComponentInParent<SettlementMapManager>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null)
            return;

        manager.HandleMapClick(eventData.position, eventData.pressEventCamera);
    }
}
