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

internal static class WorldDataChoicesCache
{
    private static double _lastRefreshTime;

    private static readonly Dictionary<WorldDataCategory, List<WorldDataIndexEntry>> _byCat =
        new Dictionary<WorldDataCategory, List<WorldDataIndexEntry>>();

    // Dedupe key: "<cat>|<id>". We prefer Editor-path entries; runtime entries are added only when a key is absent.
    private static readonly HashSet<string> _dedupe = new HashSet<string>();

    // Men-at-arms IDs are stored inside MenAtArmsCatalog JSON files. We keep a flattened index for dropdowns.
    private static readonly List<WorldDataIndexEntry> _menAtArmsEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _menAtArmsDedupe = new HashSet<string>();

    // Culture IDs can be stored inside CultureCatalog JSON files. We keep a flattened index for dropdowns.
    private static readonly List<WorldDataIndexEntry> _cultureEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _cultureDedupe = new HashSet<string>();

    // Additional flattened lists for catalog sub-entities (useful for dropdowns)
    private static readonly List<WorldDataIndexEntry> _cultureTraitEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _cultureTraitDedupe = new HashSet<string>();
    private static readonly List<WorldDataIndexEntry> _cultureLanguageEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _cultureLanguageDedupe = new HashSet<string>();
    private static readonly List<WorldDataIndexEntry> _cultureReligionEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _cultureReligionDedupe = new HashSet<string>();

    // Flattened lists for trait and language definitions from their dedicated catalogs. These
    // lists enable dropdowns in editors to reference existing definitions by ID. The
    // dedupe sets ensure we only add each trait/language ID once across all catalogs.
    private static readonly List<WorldDataIndexEntry> _traitEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _traitDedupe = new HashSet<string>();
    private static readonly List<WorldDataIndexEntry> _languageEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _languageDedupe = new HashSet<string>();

    // Flattened lists for religion and race definitions from their dedicated catalogs. These
    // lists enable dropdowns in editors to reference existing definitions by ID. The
    // dedupe sets ensure we only add each religion/race ID once across all catalogs.
    private static readonly List<WorldDataIndexEntry> _religionEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _religionDedupe = new HashSet<string>();
    private static readonly List<WorldDataIndexEntry> _raceEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _raceDedupe = new HashSet<string>();

    private static readonly List<WorldDataIndexEntry> _floraEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _floraDedupe = new HashSet<string>();
    private static readonly List<WorldDataIndexEntry> _faunaEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _faunaDedupe = new HashSet<string>();
    private static readonly List<WorldDataIndexEntry> _itemEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _itemDedupe = new HashSet<string>();
    private static readonly List<WorldDataIndexEntry> _terrainEntries = new List<WorldDataIndexEntry>(512);
    private static readonly HashSet<string> _terrainDedupe = new HashSet<string>();

    public static void RefreshAll(bool force = false)
    {
        double now = EditorApplication.timeSinceStartup;
        if (!force && (now - _lastRefreshTime) < 1.0) return;
        _lastRefreshTime = now;

        _byCat.Clear();
        _dedupe.Clear();
        _menAtArmsEntries.Clear();
        _menAtArmsDedupe.Clear();
        _cultureEntries.Clear();
        _cultureDedupe.Clear();
        _cultureTraitEntries.Clear();
        _cultureTraitDedupe.Clear();
        _cultureLanguageEntries.Clear();
        _cultureLanguageDedupe.Clear();
        _cultureReligionEntries.Clear();
        _cultureReligionDedupe.Clear();

        _traitEntries.Clear();
        _traitDedupe.Clear();
        _languageEntries.Clear();
        _languageDedupe.Clear();

        _religionEntries.Clear();
        _religionDedupe.Clear();
        _raceEntries.Clear();
        _raceDedupe.Clear();

        _floraEntries.Clear();
        _floraDedupe.Clear();
        _faunaEntries.Clear();
        _faunaDedupe.Clear();
        _itemEntries.Clear();
        _itemDedupe.Clear();
        _terrainEntries.Clear();
        _terrainDedupe.Clear();

        foreach (WorldDataCategory c in Enum.GetValues(typeof(WorldDataCategory)))
            _byCat[c] = new List<WorldDataIndexEntry>(256);

        ScanCharacters();
        ScanArmies();
        ScanMapData();
        // Legacy standalone culture files (optional)
        ScanCultures();
        // Preferred: culture catalog files, which also populate the culture dropdown list.
        ScanCultureCatalogs();
        ScanMenAtArmsCatalogs();

        // Scan dedicated catalogs for traits and languages to enable dropdowns in editors.
        ScanTraitCatalogs();
        ScanLanguageCatalogs();

        // Scan religion and race catalogs so they appear in load/create menus
        ScanReligionCatalogs();
        ScanRaceCatalogs();

        ScanFloraCatalogs();
        ScanFaunaCatalogs();
        ScanItemCatalogs();
        ScanTerrainCatalogs();

        foreach (var kv in _byCat)
            kv.Value.Sort((a, b) => string.Compare(a.displayName ?? a.id, b.displayName ?? b.id, StringComparison.OrdinalIgnoreCase));
    }

    public static void Invalidate()
    {
        _lastRefreshTime = 0;
        _byCat.Clear();
        _dedupe.Clear();
        _menAtArmsEntries.Clear();
        _menAtArmsDedupe.Clear();
        _cultureEntries.Clear();
        _cultureDedupe.Clear();
        _cultureTraitEntries.Clear();
        _cultureTraitDedupe.Clear();
        _cultureLanguageEntries.Clear();
        _cultureLanguageDedupe.Clear();
        _cultureReligionEntries.Clear();
        _cultureReligionDedupe.Clear();

        _traitEntries.Clear();
        _traitDedupe.Clear();
        _languageEntries.Clear();
        _languageDedupe.Clear();

        _religionEntries.Clear();
        _religionDedupe.Clear();
        _raceEntries.Clear();
        _raceDedupe.Clear();

        _floraEntries.Clear();
        _floraDedupe.Clear();
        _faunaEntries.Clear();
        _faunaDedupe.Clear();
        _itemEntries.Clear();
        _itemDedupe.Clear();
        _terrainEntries.Clear();
        _terrainDedupe.Clear();
    }

    public static IReadOnlyList<WorldDataIndexEntry> Get(WorldDataCategory category)
    {
        RefreshAll(false);
        return _byCat.TryGetValue(category, out var l) ? l : Array.Empty<WorldDataIndexEntry>();
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetSettlements() => Get(WorldDataCategory.Settlement);
    public static IReadOnlyList<WorldDataIndexEntry> GetCharacters() => Get(WorldDataCategory.Character);
    public static IReadOnlyList<WorldDataIndexEntry> GetCultures() => Get(WorldDataCategory.Culture);
    public static IReadOnlyList<WorldDataIndexEntry> GetArmies() => Get(WorldDataCategory.Army);

    public static IReadOnlyList<WorldDataIndexEntry> GetMenAtArmsFlat()
    {
        RefreshAll(false);
        return _byCat.TryGetValue(WorldDataCategory.MenAtArmsCatalog, out var l) ? l : Array.Empty<WorldDataIndexEntry>();
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetMenAtArmsEntries()
    {
        RefreshAll(false);
        return _menAtArmsEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetCultureEntries()
    {
        RefreshAll(false);
        return _cultureEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetCultureTraits()
    {
        RefreshAll(false);
        return _cultureTraitEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetCultureLanguages()
    {
        RefreshAll(false);
        return _cultureLanguageEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetCultureReligions()
    {
        RefreshAll(false);
        return _cultureReligionEntries;
    }

    /// <summary>
    /// Returns a flattened list of trait definitions from all trait catalogs. Each
    /// entry contains the trait ID and display name. Call RefreshAll() if you
    /// need to ensure the cache is up-to-date.
    /// </summary>
    public static IReadOnlyList<WorldDataIndexEntry> GetTraitDefinitions()
    {
        RefreshAll(false);
        return _traitEntries;
    }

    /// <summary>
    /// Returns a flattened list of language definitions from all language catalogs.
    /// Each entry contains the language ID and display name. Call RefreshAll() if
    /// you need to ensure the cache is up-to-date.
    /// </summary>
    public static IReadOnlyList<WorldDataIndexEntry> GetLanguageDefinitions()
    {
        RefreshAll(false);
        return _languageEntries;
    }

    /// <summary>
    /// Returns a flattened list of religion definitions from all religion catalogs. Each
    /// entry contains the religion ID and display name. Call RefreshAll() if you
    /// need to ensure the cache is up-to-date.
    /// </summary>
    public static IReadOnlyList<WorldDataIndexEntry> GetReligionDefinitions()
    {
        RefreshAll(false);
        return _religionEntries;
    }

    /// <summary>
    /// Returns a flattened list of race definitions from all race catalogs. Each entry
    /// contains the race ID and display name. Call RefreshAll() if you need to
    /// ensure the cache is up-to-date.
    /// </summary>
    public static IReadOnlyList<WorldDataIndexEntry> GetRaceDefinitions()
    {
        RefreshAll(false);
        return _raceEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetFloraDefinitions()
    {
        RefreshAll(false);
        return _floraEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetFaunaDefinitions()
    {
        RefreshAll(false);
        return _faunaEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetItemDefinitions()
    {
        RefreshAll(false);
        return _itemEntries;
    }

    public static IReadOnlyList<WorldDataIndexEntry> GetTerrainDefinitions()
    {
        RefreshAll(false);
        return _terrainEntries;
    }

    public static string[] ToDisplayArray(IReadOnlyList<WorldDataIndexEntry> entries, bool includeId = true)
    {
        if (entries == null || entries.Count == 0) return Array.Empty<string>();

        var arr = new string[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null)
            {
                arr[i] = "(null)";
                continue;
            }

            string dn = string.IsNullOrWhiteSpace(e.displayName) ? e.id : e.displayName;
            if (!includeId || string.IsNullOrWhiteSpace(e.id) || string.Equals(dn, e.id, StringComparison.OrdinalIgnoreCase))
                arr[i] = dn;
            else
                arr[i] = $"{dn} ({e.id})";
        }
        return arr;
    }

    // -----------------------------
    // Scanners
    // -----------------------------
    private static void ScanCharacters()
    {
        ScanDirectoryForSimple(GetEditorDir(WorldDataCategory.Character), WorldDataCategory.Character,
            idKeys: new[] { "characterId", "id" },
            displayNameKeys: new[] { "displayName", "name" });

        ScanDirectoryForSimple(GetRuntimeDir(WorldDataCategory.Character), WorldDataCategory.Character,
            idKeys: new[] { "characterId", "id" },
            displayNameKeys: new[] { "displayName", "name" });
    }

    private static void ScanArmies()
    {
        ScanDirectoryForSimple(GetEditorDir(WorldDataCategory.Army), WorldDataCategory.Army,
            idKeys: new[] { "armyId", "id" },
            displayNameKeys: new[] { "displayName", "primaryCommanderDisplayName", "name" });

        ScanDirectoryForSimple(GetRuntimeDir(WorldDataCategory.Army), WorldDataCategory.Army,
            idKeys: new[] { "armyId", "id" },
            displayNameKeys: new[] { "displayName", "primaryCommanderDisplayName", "name" });
    }

    private static void ScanCultures()
    {
        ScanDirectoryForSimple(GetEditorDir(WorldDataCategory.Culture), WorldDataCategory.Culture,
            idKeys: new[] { "cultureId", "id" },
            displayNameKeys: new[] { "displayName", "name" });

        ScanDirectoryForSimple(GetRuntimeDir(WorldDataCategory.Culture), WorldDataCategory.Culture,
            idKeys: new[] { "cultureId", "id" },
            displayNameKeys: new[] { "displayName", "name" });
    }

    /// <summary>
    /// Scans CultureCatalog JSON files. Adds the catalog file itself under
    /// WorldDataCategory.CultureCatalog, and flattens all contained cultures,
    /// traits, languages and religions into separate in-memory lists for dropdowns.
    /// Cultures are also added into WorldDataCategory.Culture so legacy code paths
    /// (e.g. GetCultures()) keep working.
    /// </summary>
    private static void ScanCultureCatalogs()
    {
        ScanCultureCatalogDir(GetEditorDir(WorldDataCategory.CultureCatalog), preferEditor: true);
        ScanCultureCatalogDir(GetRuntimeDir(WorldDataCategory.CultureCatalog), preferEditor: false);
    }

    private static void ScanCultureCatalogDir(string dir, bool preferEditor)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;

        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;

            // Only treat files as a culture catalog if they contain catalogId or obvious catalog keys.
            bool looksLikeCatalog = jo["catalogId"] != null || jo["traits"] != null || jo["languages"] != null || jo["religions"] != null;
            if (!looksLikeCatalog) continue;

            string catalogId = (string)jo["catalogId"] ?? Path.GetFileNameWithoutExtension(file);
            string catalogDn = (string)jo["displayName"] ?? catalogId;

            AddEntry(WorldDataCategory.CultureCatalog, file, catalogId, catalogDn);

            // Flatten cultures
            try
            {
                var cultures = jo["cultures"] as JArray;
                if (cultures != null)
                {
                    for (int i = 0; i < cultures.Count; i++)
                    {
                        var c = cultures[i] as JObject;
                        if (c == null) continue;

                        string id = ((string)c["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;

                        string dn = (string)c["displayName"]; // culture entries use displayName
                        if (string.IsNullOrWhiteSpace(dn)) dn = id;

                        if (_cultureDedupe.Add(id))
                        {
                            _cultureEntries.Add(new WorldDataIndexEntry { category = WorldDataCategory.Culture, id = id, displayName = dn, filePath = file });
                        }

                        // Also add into the legacy category list so existing dropdown calls work.
                        AddEntry(WorldDataCategory.Culture, file, id, dn);
                    }
                }
            }
            catch { /* ignore malformed catalogs */ }

            // Flatten traits (id + name)
            try
            {
                var traits = jo["traits"] as JArray;
                if (traits != null)
                {
                    for (int i = 0; i < traits.Count; i++)
                    {
                        var t = traits[i] as JObject;
                        if (t == null) continue;

                        string id = ((string)t["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string name = (string)t["name"]; // trait entries use name
                        if (string.IsNullOrWhiteSpace(name)) name = id;

                        if (_cultureTraitDedupe.Add(id))
                            _cultureTraitEntries.Add(new WorldDataIndexEntry { category = WorldDataCategory.CultureCatalog, id = id, displayName = name, filePath = file });
                    }
                }
            }
            catch { }

            // Flatten languages (id + name)
            try
            {
                var langs = jo["languages"] as JArray;
                if (langs != null)
                {
                    for (int i = 0; i < langs.Count; i++)
                    {
                        var l = langs[i] as JObject;
                        if (l == null) continue;

                        string id = ((string)l["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string name = (string)l["name"]; // language entries use name
                        if (string.IsNullOrWhiteSpace(name)) name = id;

                        if (_cultureLanguageDedupe.Add(id))
                            _cultureLanguageEntries.Add(new WorldDataIndexEntry { category = WorldDataCategory.CultureCatalog, id = id, displayName = name, filePath = file });
                    }
                }
            }
            catch { }

            // Flatten religions (id + name)
            try
            {
                var rel = jo["religions"] as JArray;
                if (rel != null)
                {
                    for (int i = 0; i < rel.Count; i++)
                    {
                        var r = rel[i] as JObject;
                        if (r == null) continue;

                        string id = ((string)r["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string name = (string)r["name"]; // religion entries use name
                        if (string.IsNullOrWhiteSpace(name)) name = id;

                        if (_cultureReligionDedupe.Add(id))
                            _cultureReligionEntries.Add(new WorldDataIndexEntry { category = WorldDataCategory.CultureCatalog, id = id, displayName = name, filePath = file });
                    }
                }
            }
            catch { }
        }
    }

    private static void ScanMapData()
    {
        // Your system stores Region/Unpopulated/Settlement JSONs under MapData.
        ScanMapDataDir(GetEditorDir(WorldDataCategory.Settlement));
        ScanMapDataDir(GetRuntimeDir(WorldDataCategory.Settlement));
    }

    private static void ScanMapDataDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;

        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;

            if (jo["regionId"] != null)
            {
                AddEntry(WorldDataCategory.Region, file,
                    id: (string)jo["regionId"] ?? Path.GetFileNameWithoutExtension(file),
                    displayName: (string)jo["displayName"]);
                continue;
            }

            if (jo["areaId"] != null || jo["subtype"] != null || jo["terrainType"] != null)
            {
                AddEntry(WorldDataCategory.Unpopulated, file,
                    id: (string)jo["areaId"] ?? Path.GetFileNameWithoutExtension(file),
                    displayName: (string)jo["displayName"]);
                continue;
            }

            var feudal = jo["feudal"] as JObject;
            string sid = (string)(feudal?["settlementId"]) ?? (string)jo["settlementId"] ?? Path.GetFileNameWithoutExtension(file);
            if (!string.IsNullOrWhiteSpace(sid) || jo["rulerCharacterId"] != null)
            {
                AddEntry(WorldDataCategory.Settlement, file,
                    id: sid,
                    displayName: (string)jo["displayName"]);
            }
        }
    }

    private static void ScanMenAtArmsCatalogs()
    {
        ScanMenAtArmsDir(GetEditorDir(WorldDataCategory.MenAtArmsCatalog));
        ScanMenAtArmsDir(GetRuntimeDir(WorldDataCategory.MenAtArmsCatalog));
    }

    /// <summary>
    /// Scans trait catalog JSON files. Adds the catalog file itself under
    /// WorldDataCategory.TraitCatalog and flattens contained trait definitions
    /// into an in-memory list for dropdowns. Trait definitions are added to
    /// _traitEntries and deduped across catalogs. The catalog file is added
    /// to _byCat[WorldDataCategory.TraitCatalog] for listing in the Load/Create menus.
    /// </summary>
    private static void ScanTraitCatalogs()
    {
        ScanTraitCatalogDir(GetEditorDir(WorldDataCategory.TraitCatalog));
        ScanTraitCatalogDir(GetRuntimeDir(WorldDataCategory.TraitCatalog));
    }

    private static void ScanTraitCatalogDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;
            // Recognize trait catalogs by presence of catalogId or traits array
            bool looksLikeCatalog = jo["catalogId"] != null || jo["traits"] != null;
            if (!looksLikeCatalog) continue;
            string catalogId = (string)jo["catalogId"] ?? Path.GetFileNameWithoutExtension(file);
            string catalogDn = (string)jo["displayName"] ?? catalogId;
            AddEntry(WorldDataCategory.TraitCatalog, file, catalogId, catalogDn);
            try
            {
                var traits = jo["traits"] as JArray;
                if (traits != null)
                {
                    for (int i = 0; i < traits.Count; i++)
                    {
                        var t = traits[i] as JObject;
                        if (t == null) continue;
                        string id = ((string)t["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string name = (string)t["name"];
                        if (string.IsNullOrWhiteSpace(name)) name = id;
                        if (_traitDedupe.Add(id))
                        {
                            _traitEntries.Add(new WorldDataIndexEntry
                            {
                                category = WorldDataCategory.TraitCatalog,
                                id = id,
                                displayName = name,
                                filePath = file
                            });
                        }
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Scans language catalog JSON files. Adds the catalog file itself under
    /// WorldDataCategory.LanguageCatalog and flattens contained language
    /// definitions into an in-memory list for dropdowns. Language definitions
    /// are added to _languageEntries and deduped across catalogs. The catalog
    /// file is added to _byCat[WorldDataCategory.LanguageCatalog] for listing.
    /// </summary>
    private static void ScanLanguageCatalogs()
    {
        ScanLanguageCatalogDir(GetEditorDir(WorldDataCategory.LanguageCatalog));
        ScanLanguageCatalogDir(GetRuntimeDir(WorldDataCategory.LanguageCatalog));
    }

    private static void ScanLanguageCatalogDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;
            bool looksLikeCatalog = jo["catalogId"] != null || jo["languages"] != null;
            if (!looksLikeCatalog) continue;
            string catalogId = (string)jo["catalogId"] ?? Path.GetFileNameWithoutExtension(file);
            string catalogDn = (string)jo["displayName"] ?? catalogId;
            AddEntry(WorldDataCategory.LanguageCatalog, file, catalogId, catalogDn);
            try
            {
                var langs = jo["languages"] as JArray;
                if (langs != null)
                {
                    for (int i = 0; i < langs.Count; i++)
                    {
                        var l = langs[i] as JObject;
                        if (l == null) continue;
                        string id = ((string)l["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string name = (string)l["name"];
                        if (string.IsNullOrWhiteSpace(name)) name = id;
                        if (_languageDedupe.Add(id))
                        {
                            _languageEntries.Add(new WorldDataIndexEntry
                            {
                                category = WorldDataCategory.LanguageCatalog,
                                id = id,
                                displayName = name,
                                filePath = file
                            });
                        }
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Scans religion catalog JSON files. Adds the catalog file itself under
    /// WorldDataCategory.ReligionCatalog for listing in the UI. Does not
    /// flatten religion definitions because those are not referenced by other
    /// editors directly.
    /// </summary>
    private static void ScanReligionCatalogs()
    {
        // Religion catalogs may reside in both the Editor and Runtime directories. Scan each
        // directory, adding the catalog itself to the index and flattening any religion
        // definitions into the _religionEntries list for dropdowns.
        ScanReligionCatalogDir(GetEditorDir(WorldDataCategory.ReligionCatalog));
        ScanReligionCatalogDir(GetRuntimeDir(WorldDataCategory.ReligionCatalog));
    }

    private static void ScanReligionCatalogDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;
            bool looksLikeCatalog = jo["catalogId"] != null || jo["religions"] != null;
            if (!looksLikeCatalog) continue;
            string catalogId = (string)jo["catalogId"] ?? Path.GetFileNameWithoutExtension(file);
            string catalogDn = (string)jo["displayName"] ?? catalogId;
            // Add the catalog itself as an index entry so it can be loaded/created in the UI.
            AddEntry(WorldDataCategory.ReligionCatalog, file, catalogId, catalogDn);
            // Flatten religion definitions for dropdowns.
            try
            {
                var rels = jo["religions"] as JArray;
                if (rels != null)
                {
                    foreach (var item in rels)
                    {
                        if (item is not JObject rel) continue;
                        string id = ((string)rel["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string dn = (string)rel["name"] ?? (string)rel["displayName"];
                        if (string.IsNullOrWhiteSpace(dn)) dn = id;
                        if (_religionDedupe.Add(id))
                        {
                            _religionEntries.Add(new WorldDataIndexEntry
                            {
                                category = WorldDataCategory.ReligionCatalog,
                                id = id,
                                displayName = dn,
                                filePath = file,
                            });
                        }
                    }
                }
            }
            catch
            {
                // ignore parse errors; definitions may not be available for dropdowns
            }
        }
    }

    /// <summary>
    /// Scans race catalog JSON files. Adds the catalog file itself under
    /// WorldDataCategory.RaceCatalog for listing in the UI. Does not flatten
    /// race definitions.
    /// </summary>
    private static void ScanRaceCatalogs()
    {
        // Race catalogs may reside in both Editor and Runtime directories. Scan each
        // directory, adding the catalog itself to the index and flattening race
        // definitions into the _raceEntries list for dropdowns.
        ScanRaceCatalogDir(GetEditorDir(WorldDataCategory.RaceCatalog));
        ScanRaceCatalogDir(GetRuntimeDir(WorldDataCategory.RaceCatalog));
    }

    private static void ScanRaceCatalogDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;
            bool looksLikeCatalog = jo["catalogId"] != null || jo["races"] != null;
            if (!looksLikeCatalog) continue;
            string catalogId = (string)jo["catalogId"] ?? Path.GetFileNameWithoutExtension(file);
            string catalogDn = (string)jo["displayName"] ?? catalogId;
            // Add the catalog itself as an index entry
            AddEntry(WorldDataCategory.RaceCatalog, file, catalogId, catalogDn);
            // Flatten race definitions for dropdowns
            try
            {
                var races = jo["races"] as JArray;
                if (races != null)
                {
                    foreach (var item in races)
                    {
                        if (item is not JObject race) continue;
                        string id = ((string)race["id"])?.Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        string dn = (string)race["displayName"] ?? (string)race["name"];
                        if (string.IsNullOrWhiteSpace(dn)) dn = id;
                        if (_raceDedupe.Add(id))
                        {
                            _raceEntries.Add(new WorldDataIndexEntry
                            {
                                category = WorldDataCategory.RaceCatalog,
                                id = id,
                                displayName = dn,
                                filePath = file,
                            });
                        }
                    }
                }
            }
            catch
            {
                // ignore parse errors; definitions may not be available for dropdowns
            }
        }
    }

    private static void ScanFloraCatalogs()
    {
        ScanSimpleEntriesCatalogDir(WorldDataCategory.FloraCatalog, GetEditorDir(WorldDataCategory.FloraCatalog), _floraEntries, _floraDedupe);
        ScanSimpleEntriesCatalogDir(WorldDataCategory.FloraCatalog, GetRuntimeDir(WorldDataCategory.FloraCatalog), _floraEntries, _floraDedupe);
    }

    private static void ScanFaunaCatalogs()
    {
        ScanSimpleEntriesCatalogDir(WorldDataCategory.FaunaCatalog, GetEditorDir(WorldDataCategory.FaunaCatalog), _faunaEntries, _faunaDedupe);
        ScanSimpleEntriesCatalogDir(WorldDataCategory.FaunaCatalog, GetRuntimeDir(WorldDataCategory.FaunaCatalog), _faunaEntries, _faunaDedupe);
    }

    private static void ScanItemCatalogs()
    {
        ScanSimpleEntriesCatalogDir(WorldDataCategory.ItemCatalog, GetEditorDir(WorldDataCategory.ItemCatalog), _itemEntries, _itemDedupe);
        ScanSimpleEntriesCatalogDir(WorldDataCategory.ItemCatalog, GetRuntimeDir(WorldDataCategory.ItemCatalog), _itemEntries, _itemDedupe);
    }

    private static void ScanTerrainCatalogs()
    {
        ScanSimpleEntriesCatalogDir(WorldDataCategory.TerrainCatalog, GetEditorDir(WorldDataCategory.TerrainCatalog), _terrainEntries, _terrainDedupe);
        ScanSimpleEntriesCatalogDir(WorldDataCategory.TerrainCatalog, GetRuntimeDir(WorldDataCategory.TerrainCatalog), _terrainEntries, _terrainDedupe);
    }

    private static void ScanSimpleEntriesCatalogDir(WorldDataCategory category, string dir, List<WorldDataIndexEntry> flatList, HashSet<string> dedupe)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;

        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;

            bool looksLikeCatalog = jo["catalogId"] != null || jo["entries"] != null;
            if (!looksLikeCatalog) continue;

            string catalogId = (string)jo["catalogId"] ?? Path.GetFileNameWithoutExtension(file);
            string catalogDn = (string)jo["displayName"] ?? catalogId;
            AddEntry(category, file, catalogId, catalogDn);

            try
            {
                var arr = jo["entries"] as JArray;
                if (arr == null) continue;

                for (int i = 0; i < arr.Count; i++)
                {
                    var entry = arr[i] as JObject;
                    if (entry == null) continue;

                    string eid = ((string)entry["id"])?.Trim();
                    if (string.IsNullOrWhiteSpace(eid)) continue;

                    string edn = (string)entry["displayName"] ?? (string)entry["name"];
                    if (string.IsNullOrWhiteSpace(edn)) edn = eid;

                    if (!dedupe.Add(eid)) continue;

                    flatList.Add(new WorldDataIndexEntry
                    {
                        category = category,
                        id = eid,
                        displayName = edn,
                        filePath = file,
                    });
                }
            }
            catch
            {
                // ignore malformed catalogs
            }
        }
    }

    private static void ScanMenAtArmsDir(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;

        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;

            string id = (string)jo["catalogId"] ?? Path.GetFileNameWithoutExtension(file);
            string dn = (string)jo["displayName"];
            AddEntry(WorldDataCategory.MenAtArmsCatalog, file, id, dn);

            try
            {
                var arr = jo["entries"] as JArray;
                if (arr == null) continue;

                for (int i = 0; i < arr.Count; i++)
                {
                    var entry = arr[i] as JObject;
                    if (entry == null) continue;

                    string eid = (string)entry["id"];
                    if (string.IsNullOrWhiteSpace(eid)) continue;
                    eid = eid.Trim();

                    string edn = (string)entry["displayName"];
                    if (string.IsNullOrWhiteSpace(edn)) edn = eid;

                    if (_menAtArmsDedupe.Contains(eid)) continue;
                    _menAtArmsDedupe.Add(eid);

                    _menAtArmsEntries.Add(new WorldDataIndexEntry
                    {
                        category = WorldDataCategory.MenAtArmsCatalog,
                        id = eid,
                        displayName = edn,
                        filePath = file,
                    });
                }
            }
            catch
            {
                // ignore malformed catalogs
            }
        }
    }

    private static void ScanDirectoryForSimple(string dir, WorldDataCategory cat, string[] idKeys, string[] displayNameKeys)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;

        foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (!TryParseJObject(file, out var jo)) continue;

            // Skip culture catalog files when scanning for individual cultures. A culture catalog
            // contains a "catalogId" or other catalog keys (cultures, traits, languages, religions)
            // and should not be surfaced as a single culture entry in dropdowns. Cultures from
            // catalogs are flattened and handled separately in ScanCultureCatalogs().
            if (cat == WorldDataCategory.Culture)
            {
                bool looksLikeCatalog = jo["catalogId"] != null || jo["cultures"] != null || jo["traits"] != null || jo["languages"] != null || jo["religions"] != null;
                if (looksLikeCatalog) continue;
            }

            string id = FirstString(jo, idKeys) ?? Path.GetFileNameWithoutExtension(file);
            string dn = FirstString(jo, displayNameKeys);

            AddEntry(cat, file, id, dn);
        }
    }

    // -----------------------------
    // Directory resolution
    // -----------------------------
    private static string GetEditorDir(WorldDataCategory cat)
    {
        // Prefer your existing resolver if present, but don't hard-depend on its exact API name.
        var resolved = TryCallDirResolver(cat, editor: true);
        return !string.IsNullOrWhiteSpace(resolved) ? resolved : DefaultEditorDir(cat);
    }

    private static string GetRuntimeDir(WorldDataCategory cat)
    {
        var resolved = TryCallDirResolver(cat, editor: false);
        return !string.IsNullOrWhiteSpace(resolved) ? resolved : DefaultRuntimeDir(cat);
    }

    private static string TryCallDirResolver(WorldDataCategory cat, bool editor)
    {
        // Try common type names (global + previous namespace)
        var t =
            FindType("WorldDataDirectoryResolver") ??
            FindType("Zana.WorldAuthoring.WorldDataDirectoryResolver");

        if (t == null) return null;

        string[] methodNames = editor
            ? new[] { "GetEditorDir", "GetEditorDirectory", "GetEditorPath" }
            : new[] { "GetRuntimeDir", "GetRuntimeDirectory", "GetRuntimePath" };

        foreach (var mn in methodNames)
        {
            var m = t.GetMethod(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(WorldDataCategory) }, null);
            if (m == null) continue;

            try { return m.Invoke(null, new object[] { cat }) as string; }
            catch { /* ignore */ }
        }

        return null;
    }

    private static Type FindType(string fullName)
    {
        var t = Type.GetType(fullName);
        if (t != null) return t;

        var asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < asms.Length; i++)
        {
            try
            {
                t = asms[i].GetType(fullName, throwOnError: false, ignoreCase: false);
                if (t != null) return t;
            }
            catch { /* ignore */ }
        }
        return null;
    }

    private static string DefaultEditorDir(WorldDataCategory cat)
    {
        string baseDir = Path.Combine(Application.dataPath, "SaveData");
        switch (cat)
        {
            case WorldDataCategory.Character: return Path.Combine(baseDir, "Characters");
            case WorldDataCategory.Army: return Path.Combine(baseDir, "Armies");
            case WorldDataCategory.Culture:
            case WorldDataCategory.CultureCatalog:
                return Path.Combine(baseDir, "cultures");
            case WorldDataCategory.MenAtArmsCatalog:
                return Path.Combine(baseDir, "menatarms");
            case WorldDataCategory.TraitCatalog:
                return Path.Combine(baseDir, "traits");
            case WorldDataCategory.LanguageCatalog:
                return Path.Combine(baseDir, "languages");
            case WorldDataCategory.ReligionCatalog:
                return Path.Combine(baseDir, "religions");
            case WorldDataCategory.RaceCatalog:
                return Path.Combine(baseDir, "races");
            case WorldDataCategory.FloraCatalog:
                return Path.Combine(baseDir, "flora");
            case WorldDataCategory.FaunaCatalog:
                return Path.Combine(baseDir, "fauna");
            case WorldDataCategory.ItemCatalog:
                return Path.Combine(baseDir, "items");
            case WorldDataCategory.TerrainCatalog:
                return Path.Combine(baseDir, "terrain");
            case WorldDataCategory.Region:
            case WorldDataCategory.Unpopulated:
            case WorldDataCategory.Settlement:
            default:
                return Path.Combine(baseDir, "MapData");
        }
    }

    private static string DefaultRuntimeDir(WorldDataCategory cat)
    {
        string baseDir = Application.persistentDataPath;
        switch (cat)
        {
            case WorldDataCategory.Character: return Path.Combine(baseDir, "Characters");
            case WorldDataCategory.Army: return Path.Combine(baseDir, "Armies");
            case WorldDataCategory.Culture:
            case WorldDataCategory.CultureCatalog:
                return Path.Combine(baseDir, "cultures");
            case WorldDataCategory.MenAtArmsCatalog:
                return Path.Combine(baseDir, "menatarms");
            case WorldDataCategory.TraitCatalog:
                return Path.Combine(baseDir, "traits");
            case WorldDataCategory.LanguageCatalog:
                return Path.Combine(baseDir, "languages");
            case WorldDataCategory.ReligionCatalog:
                return Path.Combine(baseDir, "religions");
            case WorldDataCategory.RaceCatalog:
                return Path.Combine(baseDir, "races");
            case WorldDataCategory.FloraCatalog:
                return Path.Combine(baseDir, "flora");
            case WorldDataCategory.FaunaCatalog:
                return Path.Combine(baseDir, "fauna");
            case WorldDataCategory.ItemCatalog:
                return Path.Combine(baseDir, "items");
            case WorldDataCategory.TerrainCatalog:
                return Path.Combine(baseDir, "terrain");
            case WorldDataCategory.Region:
            case WorldDataCategory.Unpopulated:
            case WorldDataCategory.Settlement:
            default:
                return Path.Combine(baseDir, "MapData");
        }
    }

    // -----------------------------
    // JSON helpers + indexing
    // -----------------------------
    private static string FirstString(JObject jo, string[] keys)
    {
        if (jo == null || keys == null) return null;
        for (int i = 0; i < keys.Length; i++)
        {
            string k = keys[i];
            if (string.IsNullOrWhiteSpace(k)) continue;
            var t = jo[k];
            if (t == null) continue;
            string s = (string)t;
            if (!string.IsNullOrWhiteSpace(s)) return s;
        }
        return null;
    }

    private static bool TryParseJObject(string filePath, out JObject jo)
    {
        jo = null;
        try
        {
            string txt = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(txt)) return false;
            jo = JObject.Parse(txt);
            return jo != null;
        }
        catch
        {
            return false;
        }
    }

    private static void AddEntry(WorldDataCategory cat, string filePath, string id, string displayName)
    {
        if (!_byCat.TryGetValue(cat, out var list))
        {
            list = new List<WorldDataIndexEntry>(128);
            _byCat[cat] = list;
        }

        var e = new WorldDataIndexEntry
        {
            category = cat,
            id = id,
            displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName,
            filePath = filePath,
        };

        string key = $"{cat}|{(string.IsNullOrWhiteSpace(id) ? filePath : id)}";
        if (_dedupe.Contains(key))
            return;

        _dedupe.Add(key);
        list.Add(e);
    }
}
#endif
