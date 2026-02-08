using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Runtime helper that:
    /// - When Travel Mode becomes enabled (MapManager.TravelModeChanged), applies stored coordinates to all TravelableMarker components.
    /// - Exposes helper methods so your Confirm Move button can persist the new coordinates back to JSON.
    ///
    /// This is intentionally decoupled from your TravelMenu implementation: you call SaveMarker/SaveAllMarkers at the moment you confirm a move.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TravelablePlacementSystem : MonoBehaviour
    {
        [Header("Scene Refs")]
        [SerializeField] private MapManager mapManager;
        [SerializeField] private Canvas infoLayerCanvas;
        [SerializeField] private Camera worldCamera;
        [Tooltip("Z plane used when converting to/from screen space. For an orthographic map camera, this mostly affects Z only.")]
        [SerializeField] private float mapPlaneZ = 0f;

        [Header("Auto-Apply")]
        [Tooltip("If true, whenever Travel Mode is enabled, all TravelableMarker objects will be positioned from their saved mapPosition.")]
        [SerializeField] private bool applyOnTravelModeEnabled = true;

        [Tooltip("Canvas name used for auto-discovery when InfoLayer is not assigned.")]
        [SerializeField] private string infoLayerCanvasName = "InfoLayer";

        private RectTransform _infoLayerRect;

        private void Awake()
        {
            ResolveRefs();
        }

        private void OnEnable()
        {
            ResolveRefs();
            Subscribe(true);
        }

        private void OnDisable()
        {
            Subscribe(false);
        }

        private void ResolveRefs()
        {
            if (mapManager == null)
                mapManager = FindObjectOfType<MapManager>(true);

            if (infoLayerCanvas == null)
            {
                var canvases = FindObjectsOfType<Canvas>(true);
                for (int i = 0; i < canvases.Length; i++)
                {
                    var c = canvases[i];
                    if (c == null) continue;
                    if (string.Equals(c.gameObject.name, infoLayerCanvasName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        infoLayerCanvas = c;
                        break;
                    }
                }
            }

            if (infoLayerCanvas != null)
                _infoLayerRect = infoLayerCanvas.GetComponent<RectTransform>();

            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void Subscribe(bool on)
        {
            if (mapManager == null)
                return;

            if (on)
                mapManager.TravelModeChanged += HandleTravelModeChanged;
            else
                mapManager.TravelModeChanged -= HandleTravelModeChanged;
        }

        private void HandleTravelModeChanged(bool enabled)
        {
            if (!enabled) return;
            if (!applyOnTravelModeEnabled) return;

            ApplyAllMarkersFromJson();
        }

        public void ApplyAllMarkersFromJson()
        {
            var markers = FindObjectsOfType<TravelableMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                var m = markers[i];
                if (m == null) continue;
                m.TryApplyStoredMapPosition(this);
            }
        }

        public void SaveAllMarkersToJson()
        {
            var markers = FindObjectsOfType<TravelableMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                var m = markers[i];
                if (m == null) continue;
                m.TrySaveCurrentMapPosition(this);
            }
        }

        public void SaveMarkerToJson(TravelableMarker marker)
        {
            if (marker == null) return;
            marker.TrySaveCurrentMapPosition(this);
        }

        public void SetMarkerWorldPosition(TravelableMarker marker, Vector2 worldXY)
        {
            if (marker == null) return;
            var rt = marker.TargetRect;
            if (rt == null) return;

            // If InfoLayer is World Space, simply place by world position.
            if (infoLayerCanvas != null && infoLayerCanvas.renderMode == RenderMode.WorldSpace)
            {
                Vector3 p = rt.position;
                rt.position = new Vector3(worldXY.x, worldXY.y, p.z);
                return;
            }

            // Screen Space: world -> screen -> anchored.
            if (worldCamera == null || _infoLayerRect == null)
                return;

            Vector3 screen = worldCamera.WorldToScreenPoint(new Vector3(worldXY.x, worldXY.y, mapPlaneZ));
            Camera eventCam = ResolveEventCamera();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_infoLayerRect, screen, eventCam, out Vector2 local))
            {
                rt.anchoredPosition = local;
            }
        }

        public bool TryGetMarkerWorldPosition(TravelableMarker marker, out Vector2 worldXY)
        {
            worldXY = Vector2.zero;
            if (marker == null) return false;

            var rt = marker.TargetRect;
            if (rt == null) return false;

            if (infoLayerCanvas != null && infoLayerCanvas.renderMode == RenderMode.WorldSpace)
            {
                Vector3 p = rt.position;
                worldXY = new Vector2(p.x, p.y);
                return true;
            }

            if (worldCamera == null)
                return false;

            Camera eventCam = ResolveEventCamera();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(eventCam, rt.position);
            float zDist = mapPlaneZ - worldCamera.transform.position.z;
            Vector3 wp = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, zDist));
            worldXY = new Vector2(wp.x, wp.y);
            return true;
        }

        private Camera ResolveEventCamera()
        {
            if (infoLayerCanvas == null) return null;

            if (infoLayerCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            if (infoLayerCanvas.worldCamera != null)
                return infoLayerCanvas.worldCamera;

            return worldCamera != null ? worldCamera : Camera.main;
        }
    }
}
