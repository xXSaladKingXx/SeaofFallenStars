using UnityEngine;

public static class MapNavigationUtil
{
    public static bool OpenSettlementById(string settlementId)
    {
        if (string.IsNullOrWhiteSpace(settlementId))
            return false;

        var mapManager = Object.FindObjectOfType<MapManager>();
        if (mapManager == null)
        {
            Debug.LogWarning("[MapNavigationUtil] No MapManager found in scene.");
            return false;
        }

        // Find MapPoint by pointId / stable key
        var points = Object.FindObjectsOfType<MapPoint>(true);
        MapPoint target = null;

        foreach (var p in points)
        {
            if (p == null) continue;

            // Prefer direct pointId match
            if (!string.IsNullOrWhiteSpace(p.pointId) && p.pointId == settlementId)
            {
                target = p;
                break;
            }

            // Fall back to stable key
            if (p.GetStableKey() == settlementId)
            {
                target = p;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"[MapNavigationUtil] No MapPoint found for settlementId '{settlementId}'.");
            return false;
        }

        // Let your MapManager handle snapping + opening windows (works even if OnPointClicked is private)
        mapManager.SendMessage("OnPointClicked", target, SendMessageOptions.DontRequireReceiver);
        return true;
    }
}
