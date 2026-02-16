#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(RegionAuthoringSession))]
    public sealed class RegionAuthoringSessionEditor : Editor
    {
        private int _addVassalIndex;

        public override void OnInspectorGUI()
        {
            var session = (RegionAuthoringSession)target;
            if (session == null)
                return;

            // Base session may surface load errors.
            if (!string.IsNullOrWhiteSpace(session.LastLoadError))
                EditorGUILayout.HelpBox(session.LastLoadError, MessageType.Warning);

            if (session.data == null) session.data = new RegionInfoData();
            if (session.data.main == null) session.data.main = new RegionMainTabData();
            if (session.data.main.notableFacts == null) session.data.main.notableFacts = new List<string>();
            if (session.data.geography == null) session.data.geography = new RegionGeographyTabData();
            if (session.data.vassals == null) session.data.vassals = new List<string>();
            if (session.data.derived == null) session.data.derived = new RegionDerivedInfo();

            Undo.RecordObject(session, "Edit Region Data");

            DrawIdentity(session);
            EditorGUILayout.Space(6);

            DrawMain(session);
            EditorGUILayout.Space(6);

            DrawGeography(session);
            EditorGUILayout.Space(6);

            DrawVassals(session);

            // Intentionally NOT drawing derived fields. They are recomputed from child map points on save.
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Derived fields (population, distributions, terrain breakdown) are recalculated from child map points when saving.",
                MessageType.Info);

            if (GUI.changed)
                EditorUtility.SetDirty(session);
        }

        private static void DrawIdentity(RegionAuthoringSession session)
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);

            session.data.regionId = EditorGUILayout.TextField("Region Id", session.data.regionId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            session.data.mapUrlOrPath = EditorGUILayout.TextField("Map Url/Path", session.data.mapUrlOrPath);

            // Stored as string in RegionInfoData; use enum dropdown in the inspector for convenience.
            MapLayer layerEnum = MapLayer.Regional;
            if (!string.IsNullOrWhiteSpace(session.data.layer))
                Enum.TryParse(session.data.layer, true, out layerEnum);
            layerEnum = (MapLayer)EditorGUILayout.EnumPopup("Layer", layerEnum);
            session.data.layer = layerEnum.ToString();
        }

        private static void DrawMain(RegionAuthoringSession session)
        {
            EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Description");
            session.data.main.description = EditorGUILayout.TextArea(session.data.main.description, GUILayout.MinHeight(70));

            DrawStringList("Notable Facts", session.data.main.notableFacts);
        }

        private static void DrawGeography(RegionAuthoringSession session)
        {
            EditorGUILayout.LabelField("Geography", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Overview");
            session.data.geography.overview = EditorGUILayout.TextArea(session.data.geography.overview, GUILayout.MinHeight(55));

            EditorGUILayout.LabelField("Climate Notes");
            session.data.geography.climateNotes = EditorGUILayout.TextArea(session.data.geography.climateNotes, GUILayout.MinHeight(45));
        }

        private void DrawVassals(RegionAuthoringSession session)
        {
            EditorGUILayout.LabelField("Vassals", EditorStyles.boldLabel);

            session.data.vassals ??= new List<string>();

            DrawExistingVassals(session.data.vassals);

            // Build candidate list: child-layer map points not already assigned to another parent of this layer.
            MapLayer parentLayer = MapLayer.Regional;
            if (!string.IsNullOrWhiteSpace(session.data.layer))
                Enum.TryParse(session.data.layer, true, out parentLayer);

            MapLayer? childLayer = GetChildLayer(parentLayer);
            if (childLayer == null)
            {
                EditorGUILayout.HelpBox("This layer has no defined child layer for vassals.", MessageType.Info);
                return;
            }

            string currentRegionId = string.IsNullOrWhiteSpace(session.data.regionId) ? null : session.data.regionId.Trim();
            string currentLayer = session.data.layer;

            HashSet<string> assignedElsewhere = GetAssignedVassalIdsFromOtherParents(currentRegionId, currentLayer);
            HashSet<string> currentSet = new HashSet<string>(session.data.vassals, StringComparer.OrdinalIgnoreCase);

            List<MapPoint> candidates = GetCandidateMapPoints(childLayer.Value, assignedElsewhere, currentSet);
            if (candidates.Count == 0)
            {
                EditorGUILayout.HelpBox("No available vassals at this layer that aren't already assigned.", MessageType.Info);
                return;
            }

            string[] options = new string[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                var mp = candidates[i];
                if (mp == null)
                {
                    options[i] = "<missing>";
                    continue;
                }

                string id = mp.GetStableKey();
                string name = !string.IsNullOrWhiteSpace(mp.displayName) ? mp.displayName : id;
                options[i] = string.Format("{0} ({1})", name, id);
            }

            _addVassalIndex = Mathf.Clamp(_addVassalIndex, 0, options.Length - 1);
            _addVassalIndex = EditorGUILayout.Popup("Add Vassal", _addVassalIndex, options);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", GUILayout.Width(90)))
                {
                    var chosen = candidates[_addVassalIndex];
                    if (chosen != null)
                    {
                        string id = chosen.GetStableKey();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            id = id.Trim();
                            if (!currentSet.Contains(id))
                                session.data.vassals.Add(id);
                        }
                    }
                }
            }
        }

        private static void DrawExistingVassals(List<string> vassals)
        {
            if (vassals == null)
                return;

            var mapIndex = BuildMapPointIndex();

            int removeAt = -1;
            for (int i = 0; i < vassals.Count; i++)
            {
                string id = vassals[i] ?? string.Empty;
                string trimmed = id.Trim();

                string label = string.IsNullOrWhiteSpace(trimmed) ? "<empty>" : trimmed;

                if (!string.IsNullOrWhiteSpace(trimmed) && mapIndex.TryGetValue(trimmed, out var mp) && mp != null)
                {
                    string name = !string.IsNullOrWhiteSpace(mp.displayName) ? mp.displayName : mp.GetStableKey();
                    label = string.Format("{0} ({1})", name, mp.GetStableKey());
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label);
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        removeAt = i;
                }
            }

            if (removeAt >= 0 && removeAt < vassals.Count)
                vassals.RemoveAt(removeAt);
        }

        private static void DrawStringList(string label, List<string> list)
        {
            if (list == null)
                return;

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            int removeIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    list[i] = EditorGUILayout.TextField(list[i] ?? string.Empty);
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0 && removeIndex < list.Count)
                list.RemoveAt(removeIndex);

            if (GUILayout.Button("Add", GUILayout.Width(80)))
                list.Add(string.Empty);
        }

        private static MapLayer? GetChildLayer(MapLayer parent)
        {
            switch (parent)
            {
                case MapLayer.Regional:
                    return MapLayer.Country;
                case MapLayer.Country:
                    return MapLayer.Duchy;
                case MapLayer.Duchy:
                    return MapLayer.Lordship;
                case MapLayer.Lordship:
                    return MapLayer.Point;
                default:
                    return null;
            }
        }

        private static HashSet<string> GetAssignedVassalIdsFromOtherParents(string currentRegionId, string currentLayer)
        {
            var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Ensure the cache is up-to-date (best-effort).
            try { WorldDataChoicesCache.RefreshAll(false); } catch { /* ignore */ }

            IReadOnlyList<WorldDataIndexEntry> entries;
            try { entries = WorldDataChoicesCache.Get(WorldDataCategory.Region); }
            catch { entries = null; }

            if (entries == null)
                return assigned;

            string currentIdTrim = !string.IsNullOrWhiteSpace(currentRegionId) ? currentRegionId.Trim() : null;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;

                // Skip the current region entry (if it exists on disk).
                if (!string.IsNullOrWhiteSpace(currentIdTrim) &&
                    string.Equals(entry.id, currentIdTrim, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(entry.filePath) || !File.Exists(entry.filePath))
                    continue;

                JObject jo;
                try { jo = JObject.Parse(File.ReadAllText(entry.filePath)); }
                catch { continue; }

                // Only consider other parents at the same layer.
                string otherLayer = (string)jo["layer"];
                if (!string.IsNullOrWhiteSpace(currentLayer) &&
                    !string.Equals(otherLayer, currentLayer, StringComparison.OrdinalIgnoreCase))
                    continue;

                var vassals = jo["vassals"] as JArray;
                if (vassals == null)
                    continue;

                for (int v = 0; v < vassals.Count; v++)
                {
                    string id = (string)vassals[v];
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    assigned.Add(id.Trim());
                }
            }

            return assigned;
        }

        private static List<MapPoint> GetCandidateMapPoints(MapLayer childLayer, HashSet<string> assigned, HashSet<string> currentSet)
        {
            var list = new List<MapPoint>();

            MapPoint[] all = Resources.FindObjectsOfTypeAll<MapPoint>();
            if (all == null)
                return list;

            for (int i = 0; i < all.Length; i++)
            {
                var mp = all[i];
                if (mp == null)
                    continue;

                if (mp.gameObject == null || !mp.gameObject.scene.IsValid())
                    continue;

                // Vassals are "administrative" MapPoints at the next layer down.
                // In this project, a Country/Duchy/etc may be represented either as:
                // - InfoKind.Region (an administrative region node), OR
                // - a populated Settlement/POI MapPoint that lives on that administrative layer.
                // So we must NOT restrict candidates to InfoKind.Region.
                // Exclude only kinds that are never valid administrative vassals.
                if (mp.infoKind == MapPoint.InfoKind.TravelGroup)
                    continue;
                if (mp.infoKind == MapPoint.InfoKind.Unpopulated)
                    continue;

                if (mp.layer != childLayer)
                    continue;

                string id = mp.GetStableKey();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();

                bool alreadyAssignedElsewhere = assigned != null && assigned.Contains(id) && (currentSet == null || !currentSet.Contains(id));
                if (alreadyAssignedElsewhere)
                    continue;

                if (currentSet != null && currentSet.Contains(id))
                    continue;

                list.Add(mp);
            }

            list.Sort((a, b) =>
            {
                string an = a != null && !string.IsNullOrWhiteSpace(a.displayName) ? a.displayName : a != null ? a.GetStableKey() : "";
                string bn = b != null && !string.IsNullOrWhiteSpace(b.displayName) ? b.displayName : b != null ? b.GetStableKey() : "";
                return string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        private static Dictionary<string, MapPoint> BuildMapPointIndex()
        {
            var index = new Dictionary<string, MapPoint>(StringComparer.OrdinalIgnoreCase);

            MapPoint[] all = Resources.FindObjectsOfTypeAll<MapPoint>();
            if (all == null)
                return index;

            for (int i = 0; i < all.Length; i++)
            {
                var mp = all[i];
                if (mp == null)
                    continue;

                if (mp.gameObject == null || !mp.gameObject.scene.IsValid())
                    continue;

                string id = mp.GetStableKey();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();

                if (!index.ContainsKey(id))
                    index.Add(id, mp);
            }

            return index;
        }
    }
}
#endif
