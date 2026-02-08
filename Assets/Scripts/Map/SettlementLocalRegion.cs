using UnityEngine;

public class SettlementLocalRegion : MonoBehaviour
{
    [Header("Identity")]
    public string regionId;
    public string displayName;

    [Header("Optional: clicking this loads a sub-map inside the same panel")]
    public string subMapUrlOrPath;
}
