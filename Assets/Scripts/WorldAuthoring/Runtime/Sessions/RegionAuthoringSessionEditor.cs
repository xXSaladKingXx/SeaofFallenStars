#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Zana.WorldAuthoring;

[CustomEditor(typeof(RegionAuthoringSession))]
public class RegionAuthoringSessionEditor : Editor
{
    private struct MapPointOption
    {
        public string id;
        public string label;
    }

    private int _addIndex;

    public override void OnInspectorGUI()
    {
        var session = (RegionAuthoringSession)target;
        if (session == null) return;

        session.EnsureIds();

        EditorGUI.BeginChangeCheck();

        DrawIdentity(session);
        EditorGUILayout.Space(6);

        DrawMain(session);
        EditorGUILayout.Space(6);

        DrawGeography(session);
        EditorGUILayout.Space(6);

        DrawVassals(session);
        EditorGUILayout.Space(6);

        session.overrideJsonPath = EditorGUILayout.TextField("Override JSON Path", session.overrideJsonPath);

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save JSON", GUILayout.Height(28)))
            {
                session.SaveJson();
            }

            if (GUILayout.Button("Recalculate Derived", GUILayout.Height(28)))
            {
                session.RecalculateDerived();
                // Do not display derived values in the session UI.
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(session);
        }
    }

    private static void DrawIdentity(RegionAuthoringSession session)
    {
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);

        session.model.displayName = EditorGUILayout.TextField("Display Name", session.model.displayName);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Region ID");
        EditorGUILayout.SelectableLabel(session.model.regionId ?? "", GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();

        session.model.layer = LayerDropdown("Layer", session.model.layer);
        session.model.mapUrlOrPath = EditorGUILayout.TextField("Map Url/Path", session.model.mapUrlOrPath);
    }

    private static string LayerDropdown(string label, string current)
    {
        // Minimal hardcoded list matching the plan.
        string[] options = new[] { "Region", "Country", "Duchy", "Lordship", "Point" };

        int curIndex = 0;
        if (!string.IsNullOrWhiteSpace(current))
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (string.Equals(options[i], current, StringComparison.OrdinalIgnoreCase))
                {
                    curIndex = i;
                    break;
                }
            }
        }

        int next = EditorGUILayout.Popup(label, curIndex, options);
        return options[Mathf.Clamp(next, 0, options.Length - 1)];
    }

    private static void DrawMain(RegionAuthoringSession session)
    {
        EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Description");
        session.model.main.description = EditorGUILayout.TextArea(session.model.main.description, GUILayout.MinHeight(70));

        DrawStringList("Notable Facts", session.model.main.notableFacts);
    }

    private static void DrawGeography(RegionAuthoringSession session)
    {
        EditorGUILayout.LabelField("Geography", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Overview");
        session.model.geography.overview = EditorGUILayout.TextArea(session.model.geography.overview, GUILayout.MinHeight(70));

        session.model.geography.climateNotes = EditorGUILayout.TextField("Climate Notes", session.model.geography.climateNotes);

        // Note: dominantTerrain is derived and not editable here.
    }

    private void DrawVassals(RegionAuthoringSession session)
    {
        EditorGUILayout.LabelField("Vassals", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Regions store vassals as MapPoint IDs only. For a Region-layer entry, vassals are Country-layer MapPoints not already assigned to another Region.", MessageType.Info);

        // Current vassals
        if (session.model.vassals == null)
            session.model.vassals = new List<string>();

        for (int i = session.model.vassals.Count - 1; i >= 0; i--)
        {
            string id = session.model.vassals[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(id ?? "", GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                session.model.vassals.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        // Add dropdown
        var options = BuildAvailableCountryOptions(session);
        if (options.Count == 0)
        {
            EditorGUILayout.HelpBox("No available Country-layer MapPoints found (or all are assigned to other regions).", MessageType.Warning);
            return;
        }

        string[] labels = options.Select(o => o.label).ToArray();
        _addIndex = Mathf.Clamp(_addIndex, 0, labels.Length - 1);
        _addIndex = EditorGUILayout.Popup("Add Country", _addIndex, labels);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add", GUILayout.Width(90)))
            {
                var picked = options[Mathf.Clamp(_addIndex, 0, options.Count - 1)];
                if (!session.model.vassals.Any(v => string.Equals(v, picked.id, StringComparison.OrdinalIgnoreCase)))
                    session.model.vassals.Add(picked.id);
            }
        }
    }

    private static List<MapPointOption> BuildAvailableCountryOptions(RegionAuthoringSession session)
    {
        string mapDataDir = WorldDataDirectoryResolver.GetEditorMapDataDir();
        var assignedToOtherRegions = GetCountriesAssignedToOtherRegions(mapDataDir, session.model.regionId);

        var allCountryMapPoints = FindMapPointsByLayer("Country");

        var outList = new List<MapPointOption>(allCountryMapPoints.Count);

        for (int i = 0; i < allCountryMapPoints.Count; i++)
        {
            var mp = allCountryMapPoints[i];
            if (string.IsNullOrWhiteSpace(mp.id)) continue;

            // Exclude if another region already claims it.
            if (assignedToOtherRegions.Contains(mp.id))
                continue;

            // Exclude if already in this region's list (no duplicates).
            if (session.model.vassals != null && session.model.vassals.Any(v => string.Equals(v, mp.id, StringComparison.OrdinalIgnoreCase)))
                continue;

            outList.Add(mp);
        }

        // Sort by display
        outList.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));
        return outList;
    }

    private static HashSet<string> GetCountriesAssignedToOtherRegions(string mapDataDir, string currentRegionId)
    {
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(mapDataDir) || !Directory.Exists(mapDataDir))
            return assigned;

        foreach (string file in Directory.GetFiles(mapDataDir, "*.json", SearchOption.AllDirectories))
        {
            JObject jo;
            try
            {
                jo = JObject.Parse(File.ReadAllText(file));
            }
            catch
            {
                continue;
            }

            string rid = ((string)jo["regionId"])?.Trim();
            if (string.IsNullOrWhiteSpace(rid)) continue;

            if (!string.IsNullOrWhiteSpace(currentRegionId) && string.Equals(rid, currentRegionId, StringComparison.OrdinalIgnoreCase))
                continue;

            var arr = jo["vassals"] as JArray;
            if (arr == null) continue;

            foreach (var t in arr)
            {
                string id = ((string)t)?.Trim();
                if (!string.IsNullOrWhiteSpace(id))
                    assigned.Add(id);
            }
        }

        return assigned;
    }

    private static List<MapPointOption> FindMapPointsByLayer(string layerName)
    {
        var results = new List<MapPointOption>();
        if (string.IsNullOrWhiteSpace(layerName)) return results;

        var mapPointType = FindType("MapPoint");
        if (mapPointType == null)
            return results;

        UnityEngine.Object[] objs;
        try
        {
            objs = Resources.FindObjectsOfTypeAll(mapPointType);
        }
        catch
        {
            return results;
        }

        for (int i = 0; i < objs.Length; i++)
        {
            var obj = objs[i];
            if (obj == null) continue;

            // Skip assets/prefabs.
            if (EditorUtility.IsPersistent(obj))
                continue;

            object layerVal = GetMemberValue(obj, "layer");
            if (layerVal == null) continue;

            if (!string.Equals(layerVal.ToString(), layerName, StringComparison.OrdinalIgnoreCase))
                continue;

            string id = (GetMemberValue(obj, "pointId") as string)?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                id = (GetMemberValue(obj, "regionId") as string)?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                id = (GetMemberValue(obj, "settlementId") as string)?.Trim();

            if (string.IsNullOrWhiteSpace(id))
                continue;

            string dn = (GetMemberValue(obj, "displayName") as string)?.Trim();
            if (string.IsNullOrWhiteSpace(dn))
                dn = id;

            results.Add(new MapPointOption
            {
                id = id,
                label = $"{dn} ({id})",
            });
        }

        return results;
    }

    private static Type FindType(string simpleOrFullName)
    {
        if (string.IsNullOrWhiteSpace(simpleOrFullName)) return null;

        var t = Type.GetType(simpleOrFullName);
        if (t != null) return t;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = asm.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    var tt = types[i];
                    if (tt == null) continue;
                    if (string.Equals(tt.Name, simpleOrFullName, StringComparison.Ordinal) ||
                        string.Equals(tt.FullName, simpleOrFullName, StringComparison.Ordinal))
                        return tt;
                }
            }
            catch
            {
                // ignore assemblies that don't allow GetTypes
            }
        }

        return null;
    }

    private static object GetMemberValue(object obj, string memberName)
    {
        if (obj == null || string.IsNullOrWhiteSpace(memberName)) return null;

        var t = obj.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        try
        {
            var p = t.GetProperty(memberName, flags);
            if (p != null && p.GetIndexParameters().Length == 0)
                return p.GetValue(obj, null);

            var f = t.GetField(memberName, flags);
            if (f != null)
                return f.GetValue(obj);
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static void DrawStringList(string label, List<string> list)
    {
        if (list == null) return;

        EditorGUILayout.LabelField(label);

        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            list[i] = EditorGUILayout.TextField(list[i]);
            if (GUILayout.Button("-", GUILayout.Width(22)))
            {
                list.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add", GUILayout.Width(90)))
                list.Add(string.Empty);
        }
    }
}
#endif
