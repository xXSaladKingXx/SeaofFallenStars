#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Zana.WorldAuthoring;

/// <summary>
/// Authoring wrapper for RegionInfoData.
///
/// Requested behaviors implemented:
/// - Regions track vassals as MapPoint IDs only.
/// - Region derived fields are recomputed from child map points on save.
/// - Biomes/travel-notes are not part of this model.
/// </summary>
[CreateAssetMenu(menuName = "SeaofFallenStars/World Authoring/Region Authoring Session")]
public class RegionAuthoringSession : ScriptableObject
{
    [SerializeField] public RegionInfoData model = new RegionInfoData();

    [Tooltip("Optional: override the JSON file path used for load/save. If empty, the session uses MapData/<regionId>.json")]
    public string overrideJsonPath;

    public void EnsureIds()
    {
        if (model == null) model = new RegionInfoData();
        if (model.main == null) model.main = new RegionMainTabData();
        if (model.geography == null) model.geography = new RegionGeographyTabData();
        if (model.vassals == null) model.vassals = new System.Collections.Generic.List<string>();
        if (model.derived == null) model.derived = new RegionDerivedInfo();

        if (string.IsNullOrWhiteSpace(model.regionId))
            model.regionId = Slugify(model.displayName);
    }

    public void RecalculateDerived()
    {
        EnsureIds();
        string mapDataDir = WorldDataDirectoryResolver.GetEditorMapDataDir();
        RegionDerivedCalculator.Recalculate(model, mapDataDir);
    }

    public string GetDefaultJsonPath()
    {
        EnsureIds();
        string dir = WorldDataDirectoryResolver.EnsureEditorDirectory(WorldDataCategory.Region); // MapData
        return Path.Combine(dir, $"{model.regionId}.json");
    }

    public void SaveJson()
    {
        EnsureIds();
        RecalculateDerived();

        string path = string.IsNullOrWhiteSpace(overrideJsonPath) ? GetDefaultJsonPath() : overrideJsonPath;
        string json = JsonConvert.SerializeObject(model, Formatting.Indented);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
    }

    public void LoadJson(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        try
        {
            string txt = File.ReadAllText(filePath);
            var loaded = JsonConvert.DeserializeObject<RegionInfoData>(txt);
            model = loaded ?? new RegionInfoData();
            overrideJsonPath = filePath;
        }
        catch
        {
            // If parsing fails, keep current model.
        }

        EnsureIds();
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        input = input.Trim().ToLowerInvariant();
        var sb = new StringBuilder(input.Length);
        bool prevDash = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            bool isAlphaNum = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
            if (isAlphaNum)
            {
                sb.Append(c);
                prevDash = false;
                continue;
            }

            // collapse whitespace and punctuation into a single dash
            if (!prevDash)
            {
                sb.Append('-');
                prevDash = true;
            }
        }

        // trim dashes
        string s = sb.ToString().Trim('-');
        return s;
    }
}
#endif
