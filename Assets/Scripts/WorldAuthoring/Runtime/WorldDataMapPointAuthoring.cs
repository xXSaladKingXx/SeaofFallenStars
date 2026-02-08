using System;
using System.IO;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Optional helper component you can attach to a MapPoint GameObject.
    ///
    /// It does not replace WorldDataMasterAuthoring; rather, it integrates with it:
    /// - Determines which JSON category (Settlement/Region/Unpopulated) applies to the MapPoint.
    /// - Resolves the expected file path (based on MapPoint.stable key).
    /// - The accompanying editor can auto-load/create that file in the master author.
    ///
    /// This keeps authoring centralized while allowing point-local “edit this thing” workflows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldDataMapPointAuthoring : MonoBehaviour
    {
        [Header("Master Authoring")]
        [Tooltip("Optional explicit reference. If null, the editor will auto-find the first WorldDataMasterAuthoring in the scene.")]
        public WorldDataMasterAuthoring masterOverride;

        [Tooltip("If true, selecting this MapPoint will attempt to load/create its data file in the master author.")]
        public bool autoSyncToMaster = true;

        [Tooltip("If true, the MapPoint inspector will render the active session editor inline (through the master author).")]
        public bool showInlineEditor = true;

        [Tooltip("If the resolved data file does not exist, create a new one when syncing.")]
        public bool autoCreateIfMissing = true;

        [Header("Resolution")]
        [Tooltip("If true, uses MapPoint.GetStableKey(). If false, uses MapPoint.pointId.")]
        public bool useStableKey = true;

        [NonSerialized] public WorldDataCategory resolvedCategory;
        [NonSerialized] public string resolvedFileBaseName;
        [NonSerialized] public string resolvedFilePath;

        public bool TryResolve(out WorldDataCategory category, out string fileBaseName)
        {
            category = default;
            fileBaseName = null;

            var mp = GetComponent<MapPoint>();
            if (mp == null) return false;

            // Determine category by InfoKind.
            switch (mp.infoKind)
            {
                case MapPoint.InfoKind.Region:
                    category = WorldDataCategory.Region;
                    break;
                case MapPoint.InfoKind.Settlement:
                case MapPoint.InfoKind.PointOfInterest:
                    category = WorldDataCategory.Settlement;
                    break;
                case MapPoint.InfoKind.Unpopulated:
                    category = WorldDataCategory.Unpopulated;
                    break;
                default:
                    return false; // TravelGroup (etc.) not authored here yet
            }

            fileBaseName = useStableKey ? mp.GetStableKey() : mp.pointId;
            if (string.IsNullOrWhiteSpace(fileBaseName)) return false;

            resolvedCategory = category;
            resolvedFileBaseName = fileBaseName;

            return true;
        }

        public bool TryResolveFilePath(out string filePath)
        {
            filePath = null;
            if (!TryResolve(out WorldDataCategory cat, out string baseName)) return false;

            string dir = WorldDataDirectoryResolver.GetEditorDirectory(cat);
            if (string.IsNullOrWhiteSpace(dir)) return false;
            filePath = Path.Combine(dir, baseName + ".json");

            resolvedFilePath = filePath;
            return true;
        }
    }
}
