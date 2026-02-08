using UnityEngine;

/// <summary>
/// Attached to spawned info window instances so MapManager can unregister them on Destroy.
/// </summary>
public class MapInfoWindowIdentity : MonoBehaviour
{
    private MapManager _owner;
    private string _key;

    public void Initialize(MapManager owner, string key)
    {
        _owner = owner;
        _key = key;
    }

    private void OnDestroy()
    {
        if (_owner != null && !string.IsNullOrWhiteSpace(_key))
            _owner.UnregisterWindow(_key);
    }
}
