using UnityEngine;

namespace Zana.WorldAuthoring
{
    [DisallowMultipleComponent]
    public sealed class TravelableMarker : MonoBehaviour
    {
        [Header("Identity")]
        public WorldDataCategory category = WorldDataCategory.Character;
        public string id;

        [Header("Runtime Behavior")]
        [Tooltip("If true, the marker will auto-apply stored coordinates when Travel Mode is enabled.")]
        public bool applyOnTravelModeEnable = true;

        [Tooltip("If true, coordinate writes are allowed at runtime.")]
        public bool allowRuntimeSave = true;

        [Header("UI Reference")]
        [SerializeField] private RectTransform targetRect;

        public RectTransform TargetRect
        {
            get
            {
                if (targetRect != null) return targetRect;
                targetRect = GetComponent<RectTransform>();
                return targetRect;
            }
        }

        private void Reset()
        {
            targetRect = GetComponent<RectTransform>();
        }

        public bool TryApplyStoredMapPosition(TravelablePlacementSystem system)
        {
            if (system == null) return false;
            if (!applyOnTravelModeEnable) return false;
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (!WorldDataMapPositionUtil.TryLoadMapPosition(category, id, out Vector2 worldPos))
                return false;

            system.SetMarkerWorldPosition(this, worldPos);
            return true;
        }

        public bool TrySaveCurrentMapPosition(TravelablePlacementSystem system)
        {
            if (system == null) return false;
            if (!allowRuntimeSave) return false;
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (!system.TryGetMarkerWorldPosition(this, out Vector2 worldPos))
                return false;

            return WorldDataMapPositionUtil.TrySaveMapPosition(category, id, worldPos);
        }
    }
}
