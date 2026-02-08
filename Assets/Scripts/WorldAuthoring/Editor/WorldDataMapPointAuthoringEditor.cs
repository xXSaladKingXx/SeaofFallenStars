using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(WorldDataMapPointAuthoring))]
    public sealed class WorldDataMapPointAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var authoring = (WorldDataMapPointAuthoring)target;
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("MapPoint-Linked Authoring", EditorStyles.boldLabel);

            var mp = authoring.GetComponent<MapPoint>();
            if (mp == null)
            {
                EditorGUILayout.HelpBox(
                    "No MapPoint component found on this GameObject. This authoring component is intended to live on the same GameObject as MapPoint.",
                    MessageType.Warning);
                return;
            }

            var category = InferCategory(mp);
            var stableId = mp.GetStableKey();
            var editorDir = WorldDataDirectoryResolver.GetEditorDir(category);
            var expectedPath = string.IsNullOrWhiteSpace(editorDir) ? null : Path.Combine(editorDir, stableId + ".json");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Category", category);
                EditorGUILayout.TextField("Stable ID", stableId ?? "(null)");
                EditorGUILayout.TextField("Expected JSON", expectedPath ?? "(no editor dir)");
            }

            var master = UnityEngine.Object.FindFirstObjectByType<WorldDataMasterAuthoring>();
            if (master == null)
            {
                EditorGUILayout.HelpBox(
                    "No WorldDataMasterAuthoring found in the scene. Add it to a manager GameObject to enable one-click load/edit from MapPoints.",
                    MessageType.Info);

                if (GUILayout.Button("Create WorldDataMasterAuthoring in Scene"))
                {
                    var go = new GameObject("WorldDataMasterAuthoring");
                    go.AddComponent<WorldDataMasterAuthoring>();
                    Selection.activeGameObject = go;
                }
                return;
            }

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Load/Focus In Master Authoring"))
            {
                LoadOrCreateIntoMaster(master, category, expectedPath, stableId, mp);
            }

            if (GUILayout.Button("Create/Overwrite JSON From MapPoint Defaults"))
            {
                CreateOrOverwrite(master, category, expectedPath, stableId, mp);
            }
        }

        private static WorldDataCategory InferCategory(MapPoint mp)
        {
            if (mp == null) return WorldDataCategory.Settlement;

            switch (mp.infoKind)
            {
                case MapPoint.InfoKind.Region:
                    return WorldDataCategory.Region;
                case MapPoint.InfoKind.Unpopulated:
                    return WorldDataCategory.Unpopulated;
                case MapPoint.InfoKind.Settlement:
                case MapPoint.InfoKind.PointOfInterest:
                default:
                    return WorldDataCategory.Settlement;
            }
        }

        private static void LoadOrCreateIntoMaster(WorldDataMasterAuthoring master, WorldDataCategory category, string expectedPath, string stableId, MapPoint mp)
        {
            if (master == null) return;

            var session = master.CreateOrReplaceSession(category);
            if (session == null)
            {
                Debug.LogError("Failed to create authoring session for " + category);
                return;
            }

            if (!string.IsNullOrWhiteSpace(expectedPath) && File.Exists(expectedPath))
            {
                session.TryLoadFromFile(expectedPath);
            }
            else
            {
                SeedFromMapPoint(session, stableId, mp);
            }

            WorldDataChoicesCache.Invalidate();

            Selection.activeObject = master;
            EditorGUIUtility.PingObject(master);
        }

        private static void CreateOrOverwrite(WorldDataMasterAuthoring master, WorldDataCategory category, string expectedPath, string stableId, MapPoint mp)
        {
            if (master == null) return;

            var session = master.CreateOrReplaceSession(category);
            if (session == null)
            {
                Debug.LogError("Failed to create authoring session for " + category);
                return;
            }

            SeedFromMapPoint(session, stableId, mp);

            if (string.IsNullOrWhiteSpace(expectedPath))
            {
                Debug.LogError("No editor directory resolved for category " + category);
                return;
            }

            var dir = Path.GetDirectoryName(expectedPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            // IMPORTANT: SaveNow() takes no args
            session.SetLoadedFilePath(expectedPath);
            session.SaveNow();

            WorldDataChoicesCache.Invalidate();

            Selection.activeObject = master;
            EditorGUIUtility.PingObject(master);
        }

        private static void SeedFromMapPoint(WorldDataAuthoringSessionBase session, string stableId, MapPoint mp)
        {
            if (session == null || mp == null) return;

            switch (session)
            {
                case SettlementAuthoringSession s:
                    {
                        if (s.data == null) s.data = new SettlementInfoData();
                        s.data.displayName = mp.displayName;

                        s.data.feudal ??= new SettlementFeudalData();
                        s.data.feudal.settlementId = stableId;
                        s.data.feudal.layer = mp.layer;
                        s.data.feudal.isPopulated = true;

                        s.data.main ??= new MainTab();
                        s.data.main.rulerDisplayName ??= "";
                        break;
                    }

                case RegionAuthoringSession r:
                    {
                        if (r.data == null) r.data = new RegionInfoData();
                        r.data.displayName = mp.displayName;
                        r.data.regionId = stableId;
                        r.data.layer = MapLayer.Regional; // Regions always on Regional layer
                        break;
                    }

                case UnpopulatedAuthoringSession u:
                    {
                        if (u.data == null) u.data = new UnpopulatedInfoData();
                        u.data.displayName = mp.displayName;
                        u.data.areaId = stableId;
                        u.data.layer = mp.layer;

                        // UnpopulatedInfoData.subtype is a STRING in your schema
                        u.data.subtype =
                            mp.unpopulatedSubtype == MapPoint.UnpopulatedSubtype.Water ? "Water" :
                            mp.unpopulatedSubtype == MapPoint.UnpopulatedSubtype.Ruins ? "Ruins" :
                            "Wilderness";

                        break;
                    }

                default:
                    break;
            }
        }
    }
}
