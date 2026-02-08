using System.Collections.Generic;
using UnityEngine;

public class MapPointIndex : MonoBehaviour
{
    public static MapPointIndex Instance { get; private set; }

    private readonly Dictionary<string, MapPoint> _byId = new Dictionary<string, MapPoint>();

    private void Awake()
    {
        Instance = this;
        Rebuild();
    }

    public void Rebuild()
    {
        _byId.Clear();
        var points = FindObjectsByType<MapPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in points)
        {
            if (p == null) continue;
            string id = p.GetStableKey();
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (!_byId.ContainsKey(id))
                _byId.Add(id, p);
        }
    }

    public MapPoint Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        _byId.TryGetValue(id.Trim(), out var p);
        return p;
    }
}
