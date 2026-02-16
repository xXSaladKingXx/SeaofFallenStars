using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Computes RegionInfoData.derived values from child MapPoints (settlements/unpopulated),
    /// based on RegionInfoData.vassals and/or MapPoint hierarchy.
    /// </summary>
    public static class RegionDerivedCalculator
    {
        public static void RecalculateFromChildren(RegionInfoData region)
        {
            if (region == null)
                return;

            if (region.main == null) region.main = new RegionMainTabData();
            if (region.geography == null) region.geography = new RegionGeographyTabData();
            if (region.vassals == null) region.vassals = new List<string>();
            if (region.derived == null) region.derived = new RegionDerivedInfo();

            // Build MapPoint lookup by stable key / pointId.
            MapPoint[] allPoints;
            try
            {
                allPoints = UnityEngine.Object.FindObjectsOfType<MapPoint>(true);
            }
            catch
            {
                allPoints = Array.Empty<MapPoint>();
            }

            var byId = new Dictionary<string, MapPoint>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allPoints.Length; i++)
            {
                var mp = allPoints[i];
                if (mp == null) continue;

                string id = mp.GetStableKey();
                if (string.IsNullOrWhiteSpace(id)) continue;

                if (!byId.ContainsKey(id))
                    byId.Add(id, mp);
            }

            // Optional: build culture->primary language mapping from culture catalog JSON.
            // If unavailable, we can fall back to character language aggregation.
            var cultureToPrimaryLanguage = TryBuildCultureToPrimaryLanguageMap();

            long totalPopulation = 0;

            var raceWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var cultureWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var languageWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var terrainWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            // Traverse using vassal IDs + scene hierarchy (children lists), and recursively
            // follow nested region vassals to reach settlements/unpopulated.
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>();

            for (int i = 0; i < region.vassals.Count; i++)
            {
                string v = region.vassals[i];
                if (!string.IsNullOrWhiteSpace(v))
                    stack.Push(v.Trim());
            }

            while (stack.Count > 0)
            {
                string id = stack.Pop();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!visited.Add(id)) continue;

                MapPoint mp;
                if (!byId.TryGetValue(id, out mp) || mp == null)
                    continue;

                // Always include scene children as additional graph edges.
                var children = mp.GetChildren();
                for (int c = 0; c < children.Count; c++)
                {
                    var child = children[c];
                    if (child == null) continue;

                    string cid = child.GetStableKey();
                    if (!string.IsNullOrWhiteSpace(cid))
                        stack.Push(cid.Trim());
                }

                switch (mp.infoKind)
                {
                    case MapPoint.InfoKind.Region:
                    {
                        // Recursively include vassals of nested region map points, if present.
                        var childRegion = mp.GetRegionInfoData();
                        if (childRegion != null && childRegion.vassals != null)
                        {
                            for (int vi = 0; vi < childRegion.vassals.Count; vi++)
                            {
                                string vid = childRegion.vassals[vi];
                                if (!string.IsNullOrWhiteSpace(vid))
                                    stack.Push(vid.Trim());
                            }
                        }
                        break;
                    }

                    case MapPoint.InfoKind.Settlement:
                    case MapPoint.InfoKind.PointOfInterest:
                    {
                        var st = mp.GetSettlementInfoData();
                        if (st == null)
                            break;

                        int pop = 0;
                        try { pop = st.main != null ? st.main.population : 0; } catch { pop = 0; }
                        if (pop < 0) pop = 0;

                        totalPopulation += pop;

                        // Race distribution
                        AddWeightedPercentEntries(raceWeights, st.cultural != null ? st.cultural.raceDistribution : null, pop);

                        // Culture distribution (legacy-friendly):
                        // - Prefer explicit cultureDistribution list.
                        // - If missing/empty, fall back to CulturalTab.culture (100%).
                        List<PercentEntry> cultureDist = st.cultural != null ? st.cultural.cultureDistribution : null;
                        bool hasCultureDist = cultureDist != null && cultureDist.Count > 0;

                        if (hasCultureDist)
                        {
                            AddWeightedPercentEntries(cultureWeights, cultureDist, pop);
                        }
                        else if (st.cultural != null && !string.IsNullOrWhiteSpace(st.cultural.culture) && pop > 0)
                        {
                            AddWeight(cultureWeights, st.cultural.culture.Trim(), pop);
                        }

                        // Language distribution:
                        // 1) Prefer culture->primary language mapping (from CultureCatalog JSON)
                        // 2) Otherwise, fall back to character languages (best-effort)
                        bool addedViaCultureMap = false;

                        if (cultureToPrimaryLanguage != null && cultureToPrimaryLanguage.Count > 0 && pop > 0)
                        {
                            if (hasCultureDist)
                            {
                                addedViaCultureMap = AddWeightedLanguagesFromCulture(languageWeights, cultureDist, pop, cultureToPrimaryLanguage);
                            }
                            else if (st.cultural != null && !string.IsNullOrWhiteSpace(st.cultural.culture))
                            {
                                string cId = st.cultural.culture.Trim();
                                if (cultureToPrimaryLanguage.TryGetValue(cId, out string langId) && !string.IsNullOrWhiteSpace(langId))
                                {
                                    AddWeight(languageWeights, langId.Trim(), pop);
                                    addedViaCultureMap = true;
                                }
                            }
                        }

                        if (!addedViaCultureMap)
                        {
                            AddWeightedLanguagesFromCharacters(languageWeights, st, pop);
                        }

                        break;
                    }

                    case MapPoint.InfoKind.Unpopulated:
                    {
                        var up = mp.GetUnpopulatedInfoData();
                        if (up == null || up.geography == null)
                            break;

                        double areaWeight = 1.0;
                        try
                        {
                            // areaSqMi is a non-nullable float in UnpopulatedGeographyTab.
                            // Treat 0 as "unknown" and fall back to equal weighting.
                            float area = up.geography.areaSqMi;
                            if (area > 0f)
                                areaWeight = area;
                        }
                        catch
                        {
                            // ignore
                        }

                        AddWeightedTerrain(terrainWeights, up.geography, areaWeight);
                        break;
                    }

                    default:
                        break;
                }
            }

            region.derived.totalPopulation = (totalPopulation > int.MaxValue) ? int.MaxValue : (int)Math.Max(0, totalPopulation);

            region.derived.raceDistribution = BuildNormalizedPercentEntries(raceWeights);
            region.derived.cultureDistribution = BuildNormalizedPercentEntries(cultureWeights);
            region.derived.languageDistribution = BuildNormalizedPercentEntries(languageWeights);
            region.derived.terrainBreakdown = BuildNormalizedPercentEntries(terrainWeights);

            // Geography dominant terrain derives from terrainBreakdown keys (ordered).
            region.geography.dominantTerrain = new List<string>();
            for (int i = 0; i < region.derived.terrainBreakdown.Count; i++)
            {
                var e = region.derived.terrainBreakdown[i];
                if (e == null) continue;
                if (string.IsNullOrWhiteSpace(e.key)) continue;
                region.geography.dominantTerrain.Add(e.key);
            }
        }

        private static void AddWeightedPercentEntries(Dictionary<string, double> acc, List<PercentEntry> entries, int populationWeight)
        {
            if (acc == null || entries == null || entries.Count == 0)
                return;

            if (populationWeight <= 0)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;

                string key = e.key;
                if (string.IsNullOrWhiteSpace(key)) continue;

                double frac = NormalizePercentToFraction(e.percent);
                if (frac <= 0) continue;

                AddWeight(acc, key.Trim(), populationWeight * frac);
            }
        }

        private static bool AddWeightedLanguagesFromCulture(
            Dictionary<string, double> acc,
            List<PercentEntry> cultureDistribution,
            int populationWeight,
            Dictionary<string, string> cultureToPrimaryLanguage)
        {
            if (acc == null || cultureDistribution == null || cultureDistribution.Count == 0)
                return false;

            if (populationWeight <= 0)
                return false;

            if (cultureToPrimaryLanguage == null || cultureToPrimaryLanguage.Count == 0)
                return false;

            bool addedAny = false;

            for (int i = 0; i < cultureDistribution.Count; i++)
            {
                var c = cultureDistribution[i];
                if (c == null) continue;

                string cultureId = c.key;
                if (string.IsNullOrWhiteSpace(cultureId)) continue;

                string langId;
                if (!cultureToPrimaryLanguage.TryGetValue(cultureId.Trim(), out langId) || string.IsNullOrWhiteSpace(langId))
                    continue;

                double frac = NormalizePercentToFraction(c.percent);
                if (frac <= 0) continue;

                AddWeight(acc, langId.Trim(), populationWeight * frac);
                addedAny = true;
            }

            return addedAny;
        }

        private static void AddWeightedLanguagesFromCharacters(Dictionary<string, double> acc, SettlementInfoData settlement, int populationWeight)
        {
            if (acc == null || settlement == null || settlement.characterIds == null || settlement.characterIds.Length == 0)
                return;

            if (populationWeight <= 0)
                return;

            int charCount = settlement.characterIds.Length;
            if (charCount <= 0) return;

            double perCharWeight = (double)populationWeight / charCount;

            // Attempt to load characters via generic loader (editor/runtime dirs).
            string rtChars = null;
            string edChars = null;

            try { rtChars = WorldDataDirectoryResolver.GetRuntimeCharactersDir(); } catch { /* ignore */ }
            try { edChars = WorldDataDirectoryResolver.GetEditorCharactersDir(); } catch { /* ignore */ }

            for (int i = 0; i < settlement.characterIds.Length; i++)
            {
                string charId = settlement.characterIds[i];
                if (string.IsNullOrWhiteSpace(charId)) continue;

                CharacterSheetData sheet = null;
                try
                {
                    // JsonDataLoader is used elsewhere (MapPoint). This is a best-effort load.
                    sheet = JsonDataLoader.TryLoadFromEitherPath<CharacterSheetData>(rtChars, edChars, charId.Trim());
                }
                catch
                {
                    sheet = null;
                }

                if (sheet == null || sheet.proficiencies == null || sheet.proficiencies.languages == null)
                    continue;

                var langs = sheet.proficiencies.languages;
                if (langs.Length == 0) continue;

                for (int li = 0; li < langs.Length; li++)
                {
                    string lang = langs[li];
                    if (string.IsNullOrWhiteSpace(lang)) continue;

                    AddWeight(acc, lang.Trim(), perCharWeight);
                }
            }
        }

        private static void AddWeightedTerrain(Dictionary<string, double> acc, UnpopulatedGeographyTab geo, double areaWeight)
        {
            if (acc == null || geo == null)
                return;

            if (areaWeight <= 0)
                areaWeight = 1.0;

            if (geo.terrainBreakdown != null && geo.terrainBreakdown.Count > 0)
            {
                for (int i = 0; i < geo.terrainBreakdown.Count; i++)
                {
                    var t = geo.terrainBreakdown[i];
                    if (t == null) continue;

                    string terrain = t.terrainType;
                    if (string.IsNullOrWhiteSpace(terrain)) continue;

                    double frac = NormalizePercentToFraction(t.percent);
                    if (frac <= 0) continue;

                    AddWeight(acc, terrain.Trim(), areaWeight * frac);
                }
            }
            else if (!string.IsNullOrWhiteSpace(geo.terrainType))
            {
                AddWeight(acc, geo.terrainType.Trim(), areaWeight);
            }
        }

        private static void AddWeight(Dictionary<string, double> acc, string key, double weight)
        {
            if (acc == null) return;
            if (string.IsNullOrWhiteSpace(key)) return;
            if (weight <= 0) return;

            double existing;
            if (acc.TryGetValue(key, out existing))
                acc[key] = existing + weight;
            else
                acc[key] = weight;
        }

        private static double NormalizePercentToFraction(float p)
        {
            if (p <= 0f) return 0.0;

            // Heuristic:
            // - If > 1, treat as 0..100 and convert to 0..1.
            // - Else treat as already fractional.
            if (p > 1.0f) return p / 100.0;
            return p;
        }

        private static List<PercentEntry> BuildNormalizedPercentEntries(Dictionary<string, double> weights)
        {
            var list = new List<PercentEntry>();
            if (weights == null || weights.Count == 0)
                return list;

            double sum = 0.0;
            foreach (var kv in weights)
            {
                if (kv.Value > 0)
                    sum += kv.Value;
            }

            if (sum <= 0.0)
                return list;

            foreach (var kv in weights)
            {
                if (kv.Value <= 0) continue;
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;

                list.Add(new PercentEntry
                {
                    key = kv.Key,
                    percent = (float)(kv.Value / sum)
                });
            }

            list.Sort((a, b) => (b != null ? b.percent : 0f).CompareTo(a != null ? a.percent : 0f));
            return list;
        }

        /// <summary>
        /// Tries to read culture entries from Culture Catalog JSON and map cultureId -> primaryLanguageId.
        /// Best-effort; returns an empty dictionary if not available.
        /// </summary>
        private static Dictionary<string, string> TryBuildCultureToPrimaryLanguageMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var dirs = new List<string>(2);

            try
            {
                var ed = WorldDataDirectoryResolver.GetEditorDirForCategory(WorldDataCategory.CultureCatalog);
                if (!string.IsNullOrWhiteSpace(ed))
                    dirs.Add(ed);
            }
            catch { /* ignore */ }

            try
            {
                var rt = WorldDataDirectoryResolver.GetRuntimeDirForCategory(WorldDataCategory.CultureCatalog);
                if (!string.IsNullOrWhiteSpace(rt) && !dirs.Contains(rt))
                    dirs.Add(rt);
            }
            catch { /* ignore */ }

            for (int d = 0; d < dirs.Count; d++)
            {
                string dir = dirs[d];
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                    continue;

                string[] files;
                try
                {
                    files = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    string json;
                    try { json = File.ReadAllText(file); }
                    catch { continue; }

                    JObject jo;
                    try { jo = JObject.Parse(json); }
                    catch { continue; }

                    var cultures = jo["cultures"] as JArray;
                    if (cultures == null || cultures.Count == 0)
                        continue;

                    for (int ci = 0; ci < cultures.Count; ci++)
                    {
                        var c = cultures[ci] as JObject;
                        if (c == null)
                            continue;

                        string cultureId = (string)c["id"] ?? (string)c["cultureId"];
                        if (string.IsNullOrWhiteSpace(cultureId))
                            continue;

                        // Prefer explicit primary language fields if present.
                        string primary = (string)c["primaryLanguageId"] ?? (string)c["primaryLanguage"];
                        if (string.IsNullOrWhiteSpace(primary))
                        {
                            // Otherwise, take the first language in the culture's language list (if any).
                            var langs = c["languages"] as JArray;
                            if (langs != null && langs.Count > 0)
                                primary = (string)langs[0];
                        }

                        if (string.IsNullOrWhiteSpace(primary))
                            continue;

                        if (!map.ContainsKey(cultureId))
                            map.Add(cultureId, primary);
                    }
                }
            }

            return map;
        }
    }
}
