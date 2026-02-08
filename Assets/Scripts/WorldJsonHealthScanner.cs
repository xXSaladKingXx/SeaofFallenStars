#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A strict JSON health scanner that validates each JSON file against the
/// current data model definitions. Unlike the loose scanner that only
/// performs heuristic checks, this implementation enumerates the allowed
/// and required keys for each record type and flags any unexpected keys
/// or missing required fields as errors. It also takes the "subtype"
/// field into account when validating unpopulated areas so that water
/// bodies, ruins and wilderness areas each obey their own schema.
/// </summary>
public sealed class WorldJsonHealthScanner : EditorWindow
{
    private enum Severity { Error, Warning, Legacy }

    [Serializable]
    private sealed class Finding
    {
        public string assetPath;
        public string category;
        public Severity severity;
        public string message;
    }

    private Vector2 _scroll;
    private readonly List<Finding> _findings = new List<Finding>(2048);

    private bool _scanAssetsSaveData = true;
    private bool _scanPersistentData = false;

    private bool _showErrors = true;
    private bool _showWarnings = true;
    private bool _showLegacy = true;

    private string _textFilter = "";

    /// <summary>
    /// A lookup of allowed top-level keys for each record category. Any
    /// property not listed here will be flagged as an error. The keys
    /// reflect the fields defined in the runtime data models (e.g.
    /// CharacterSheetData, SettlementInfoData, etc.).
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> AllowedRootKeys = new Dictionary<string, HashSet<string>>
    {
        {
            "character",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "characterId","displayName","playerName","className","subclassName","background","race","subrace",
                "level","alignment","experiencePoints",
                // Life and appearance
                "life","appearance",
                // Stats and skills
                "abilities","abilityScores","savingThrows","skills",
                "combat","proficiencies","attacks","equipment",
                // Extra details and narrative
                "featuresAndTraits","personality","backstory","relationships",
                // Feudal relations for characters like monarchs
                "feudal",
                // Spellcasting data
                "spellcasting",
                // Additional metadata
                "notes","homeSettlementId","schema","notesAndMetadata","statusTracking",
                "socialAndDomain","progression","identity"
            }
        },
        {
            "settlement",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "displayName","mapUrlOrPath",
                // Character associations (legacy)
                "rulerCharacterId","characterIds",
                // Primary tabs/sections
                "main","army","economy","cultural","history","feudal",
                // Identity fields
                "settlementId","layer","isPopulated","capitalSettlementId","liegeSettlementId",
                // Contracts and optional extension object
                "vassalContracts","ext"
            }
        },
        {
            "region",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "regionId","displayName","mapUrlOrPath","layer",
                "main","geography","culture","vassals",
                // optional extension
                "ext"
            }
        },
        { "unpopulated", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "areaId","displayName","mapUrlOrPath","layer","isPopulated","subtype","main","geography","nature",
                "history","culture","water"
            }
        },
        { "army", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Base identity
                "armyId","displayName",
                // Command fields.  Allow both single and multi‑commander patterns.
                "primaryCommanderCharacterId","primaryCommanderDisplayName",
                "commanderCharacterIds","knightCharacterIds",
                // Additional information
                "description","notes",
                // Summary statistics
                "totalArmy","attack","defense","speed",
                // Men‑at‑arms representation.  Either legacy or new stacks.
                "menAtArms","menAtArmsStacks",
                // Optional map placement
                "mapPosition"
            }
        },
        { "culture", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "cultureId","displayName","description","tags","traits","languages","factions","notes"
            }
        },
        { "culture_catalog", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "catalogId","displayName","cultures","traits","languages","religions","notes"
            }
        },
        { "men_at_arms_catalog", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "catalogId","displayName","entries","notes"
            }
        }
    };

    /// <summary>
    /// A lookup of required top-level keys for each record category. These
    /// keys must be present and non-empty on the root object.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> RequiredRootKeys = new Dictionary<string, HashSet<string>>
    {
        { "character", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "characterId", "displayName" } },
        { "settlement", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "displayName", "main", "army", "economy", "cultural", "history", "feudal" } },
        { "region", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "regionId", "displayName", "main", "geography", "culture", "vassals" } },
        { "unpopulated", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "areaId", "displayName", "subtype", "main", "geography", "history" } },
        {
            "army",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // At minimum, an army needs an ID
                "armyId",
                // At least one definition of men‑at‑arms must exist.  The scanner
                // will check that either menAtArms or menAtArmsStacks is present
                // separately (see ValidateArmy).  We still list one here to
                // ensure the key exists in some form.
                "menAtArms"
            }
        },
        { "culture", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cultureId", "displayName" } },
        { "culture_catalog", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "catalogId", "cultures", "traits", "languages", "religions" } },
        { "men_at_arms_catalog", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "catalogId", "entries" } }
    };

    /// <summary>
    /// Valid subtypes for unpopulated areas. The subtype drives which
    /// sub-objects are required.
    /// </summary>
    private static readonly HashSet<string> UnpopulatedSubtypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Wilderness", "Water", "Ruins"
    };

    [MenuItem("Tools/World Data/JSON Health Scanner (Strict)")]
    public static void Open()
    {
        var w = GetWindow<WorldJsonHealthScanner>();
        w.titleContent = new GUIContent("JSON Health Scanner");
        w.minSize = new Vector2(980, 520);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Scan Locations", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            _scanAssetsSaveData = EditorGUILayout.ToggleLeft("Assets/SaveData", _scanAssetsSaveData, GUILayout.Width(140));
            _scanPersistentData = EditorGUILayout.ToggleLeft("Application.persistentDataPath", _scanPersistentData, GUILayout.Width(220));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Scan Now", GUILayout.Width(140), GUILayout.Height(24)))
                Scan();
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            _showErrors = EditorGUILayout.ToggleLeft("Errors", _showErrors, GUILayout.Width(80));
            _showWarnings = EditorGUILayout.ToggleLeft("Warnings", _showWarnings, GUILayout.Width(95));
            _showLegacy = EditorGUILayout.ToggleLeft("Legacy", _showLegacy, GUILayout.Width(80));
            GUILayout.Space(18);
            EditorGUILayout.LabelField("Text:", GUILayout.Width(35));
            _textFilter = EditorGUILayout.TextField(_textFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(70)))
                _textFilter = "";
        }
        EditorGUILayout.Space(8);
        int err = _findings.Count(f => f.severity == Severity.Error);
        int warn = _findings.Count(f => f.severity == Severity.Warning);
        int leg = _findings.Count(f => f.severity == Severity.Legacy);
        EditorGUILayout.HelpBox(
            $"Findings: {_findings.Count}   |   Errors: {err}   Warnings: {warn}   Legacy: {leg}\n" +
            "Tip: Legacy means old/mixed format detected so you can remove or migrate it.",
            MessageType.Info);
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Severity", GUILayout.Width(80));
            GUILayout.Label("Category", GUILayout.Width(140));
            GUILayout.Label("File", GUILayout.Width(520));
            GUILayout.Label("Message", GUILayout.ExpandWidth(true));
        }
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var f in GetFiltered())
            DrawRow(f);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Copy Report to Clipboard", GUILayout.Width(220)))
                CopyReportToClipboard();
            if (GUILayout.Button("Export Report CSV...", GUILayout.Width(180)))
                ExportCsv();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping First Error", GUILayout.Width(140)))
                PingFirst(Severity.Error);
            if (GUILayout.Button("Ping First Legacy", GUILayout.Width(140)))
                PingFirst(Severity.Legacy);
        }
    }

    private IEnumerable<Finding> GetFiltered()
    {
        for (int i = 0; i < _findings.Count; i++)
        {
            var f = _findings[i];
            if (f.severity == Severity.Error && !_showErrors) continue;
            if (f.severity == Severity.Warning && !_showWarnings) continue;
            if (f.severity == Severity.Legacy && !_showLegacy) continue;
            if (!string.IsNullOrWhiteSpace(_textFilter))
            {
                string t = _textFilter.Trim();
                if (!(f.assetPath?.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                      f.category?.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                      f.message?.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;
            }
            yield return f;
        }
    }

    private void DrawRow(Finding f)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            Color prev = GUI.color;
            GUI.color = SeverityColor(f.severity);
            GUILayout.Label(f.severity.ToString(), GUILayout.Width(80));
            GUI.color = prev;
            GUILayout.Label(f.category ?? "(unknown)", GUILayout.Width(140));
            if (GUILayout.Button(f.assetPath, EditorStyles.linkLabel, GUILayout.Width(520)))
                PingAsset(f.assetPath);
            GUILayout.Label(f.message ?? "", GUILayout.ExpandWidth(true));
        }
    }

    private static Color SeverityColor(Severity s)
    {
        switch (s)
        {
            case Severity.Error: return new Color(1f, 0.45f, 0.45f);
            case Severity.Warning: return new Color(1f, 0.8f, 0.35f);
            case Severity.Legacy: return new Color(0.6f, 0.85f, 1f);
            default: return Color.white;
        }
    }

    private void Scan()
    {
        _findings.Clear();
        var files = new List<string>(4096);
        if (_scanAssetsSaveData)
        {
            string saveDataDisk = Path.Combine(Application.dataPath, "SaveData");
            if (Directory.Exists(saveDataDisk))
                files.AddRange(Directory.GetFiles(saveDataDisk, "*.json", SearchOption.AllDirectories));
            else
                AddFinding(null, "scanner", Severity.Warning, $"Assets/SaveData not found at: {saveDataDisk}");
        }
        if (_scanPersistentData)
        {
            string p = Application.persistentDataPath;
            if (Directory.Exists(p))
                files.AddRange(Directory.GetFiles(p, "*.json", SearchOption.AllDirectories));
            else
                AddFinding(null, "scanner", Severity.Warning, $"persistentDataPath folder not found: {p}");
        }
        var idToFiles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (string diskPath in files)
        {
            string assetPath = DiskToAssetPath(diskPath);
            string text;
            try
            {
                text = File.ReadAllText(diskPath);
            }
            catch (Exception ex)
            {
                AddFinding(assetPath, "io", Severity.Error, $"Failed to read: {ex.Message}");
                continue;
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                AddFinding(assetPath, "json", Severity.Error, "File is empty.");
                continue;
            }
            JObject jo;
            try
            {
                jo = JObject.Parse(text);
            }
            catch (Exception ex)
            {
                AddFinding(assetPath, "json", Severity.Error, $"JSON parse error: {ex.Message}");
                continue;
            }
            string category = GuessCategory(jo);
            string id = ExtractPrimaryId(jo, category);
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (!idToFiles.TryGetValue(id, out var list))
                {
                    list = new List<string>();
                    idToFiles[id] = list;
                }
                list.Add(assetPath ?? diskPath);
            }
            ValidateJson(assetPath, category, jo);
        }
        foreach (var kv in idToFiles)
        {
            if (kv.Value.Count > 1)
            {
                AddFinding(kv.Value[0], "id", Severity.Warning,
                    $"Duplicate ID '{kv.Key}' appears in {kv.Value.Count} files:\n - " + string.Join("\n - ", kv.Value));
            }
        }
        _findings.Sort((a, b) =>
        {
            int sa = (a.severity == Severity.Error) ? 0 : (a.severity == Severity.Legacy) ? 1 : 2;
            int sb = (b.severity == Severity.Error) ? 0 : (b.severity == Severity.Legacy) ? 1 : 2;
            int c = sa.CompareTo(sb);
            if (c != 0) return c;
            c = string.Compare(a.category, b.category, StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
            return string.Compare(a.assetPath, b.assetPath, StringComparison.OrdinalIgnoreCase);
        });
        Repaint();
        Debug.Log($"[JSON Health Scanner] Scan complete. Findings: {_findings.Count}");
    }

    private void ValidateJson(string assetPath, string category, JObject jo)
    {
        ValidateAllowedAndRequired(assetPath, category, jo);
        switch (category)
        {
            case "culture_catalog":
                ValidateCultureCatalog(assetPath, jo);
                break;
            case "culture":
                ValidateCulture(assetPath, jo);
                break;
            case "men_at_arms_catalog":
                ValidateMenAtArmsCatalog(assetPath, jo);
                break;
            case "army":
                ValidateArmy(assetPath, jo);
                break;
            case "settlement":
                ValidateSettlement(assetPath, jo);
                break;
            case "region":
                ValidateRegion(assetPath, jo);
                break;
            case "unpopulated":
                ValidateUnpopulated(assetPath, jo);
                break;
            case "character":
                ValidateCharacter(assetPath, jo);
                break;
            default:
                DetectLegacyShapes(assetPath, category, jo);
                break;
        }
    }

    private void ValidateAllowedAndRequired(string assetPath, string category, JObject jo)
    {
        if (AllowedRootKeys.TryGetValue(category, out var allowed))
        {
            foreach (var prop in jo.Properties())
            {
                if (!allowed.Contains(prop.Name))
                {
                    AddFinding(assetPath, category, Severity.Error,
                        $"Unexpected property '{prop.Name}' in {category} record.");
                }
            }
        }
        if (RequiredRootKeys.TryGetValue(category, out var required))
        {
            foreach (var key in required)
            {
                if (!jo.TryGetValue(key, out var token) || IsMissingOrEmpty(token))
                {
                    AddFinding(assetPath, category, Severity.Error,
                        $"Missing or empty required key '{key}' in {category} record.");
                }
            }
        }
        if (category == "unpopulated")
        {
            string subtype = (string)(jo["subtype"] ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(subtype) && !UnpopulatedSubtypes.Contains(subtype))
            {
                AddFinding(assetPath, category, Severity.Error,
                    $"Invalid subtype '{subtype}' on unpopulated record. Allowed: " + string.Join(", ", UnpopulatedSubtypes));
            }
            if (string.Equals(subtype, "Water", StringComparison.OrdinalIgnoreCase))
            {
                if (!(jo["water"] is JObject waterObj))
                {
                    AddFinding(assetPath, category, Severity.Error,
                        "Water subtype requires a 'water' object with waterBodyType, waterType and depth.");
                }
                else
                {
                    var reqWater = new[] { "waterBodyType", "waterType", "depth" };
                    foreach (var wkey in reqWater)
                    {
                        if (!waterObj.TryGetValue(wkey, out var wtoken) || IsMissingOrEmpty(wtoken))
                        {
                            AddFinding(assetPath, category, Severity.Error,
                                $"Water subtype requires non-empty '{wkey}' on 'water'.");
                        }
                    }
                }
            }
            else if (string.Equals(subtype, "Ruins", StringComparison.OrdinalIgnoreCase))
            {
                if (!(jo["culture"] is JObject))
                {
                    AddFinding(assetPath, category, Severity.Error,
                        "Ruins subtype requires a 'culture' object with descriptive fields.");
                }
            }
        }
    }

    private void ValidateCultureCatalog(string assetPath, JObject jo)
    {
        RequireNonEmpty(assetPath, "culture_catalog", jo, "catalogId", Severity.Error);
        RequireArray(assetPath, "culture_catalog", jo, "cultures", Severity.Error);
        RequireArray(assetPath, "culture_catalog", jo, "traits", Severity.Error);
        RequireArray(assetPath, "culture_catalog", jo, "languages", Severity.Error);
        RequireArray(assetPath, "culture_catalog", jo, "religions", Severity.Error);
        if (jo["traits"] is JArray traits)
        {
            for (int i = 0; i < traits.Count; i++)
            {
                if (!(traits[i] is JObject t))
                {
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"traits[{i}] is not an object.");
                    continue;
                }
                if (IsMissingOrEmpty(t["id"])) AddFinding(assetPath, "culture_catalog", Severity.Error, $"traits[{i}].id missing/empty.");
                if (IsMissingOrEmpty(t["name"])) AddFinding(assetPath, "culture_catalog", Severity.Warning, $"traits[{i}].name missing/empty.");
                if (IsMissingOrEmpty(t["effect"])) AddFinding(assetPath, "culture_catalog", Severity.Warning, $"traits[{i}].effect missing/empty.");
            }
        }
        if (jo["languages"] is JArray langs)
        {
            for (int i = 0; i < langs.Count; i++)
            {
                if (!(langs[i] is JObject l))
                {
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"languages[{i}] is not an object.");
                    continue;
                }
                if (IsMissingOrEmpty(l["id"])) AddFinding(assetPath, "culture_catalog", Severity.Error, $"languages[{i}].id missing/empty.");
                if (IsMissingOrEmpty(l["name"])) AddFinding(assetPath, "culture_catalog", Severity.Error, $"languages[{i}].name missing/empty.");
                if (IsMissingOrEmpty(l["nativeRegionId"])) AddFinding(assetPath, "culture_catalog", Severity.Warning, $"languages[{i}].nativeRegionId missing/empty.");
            }
        }
        if (jo["religions"] is JArray rels)
        {
            for (int i = 0; i < rels.Count; i++)
            {
                if (!(rels[i] is JObject r))
                {
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"religions[{i}] is not an object.");
                    continue;
                }
                if (IsMissingOrEmpty(r["id"])) AddFinding(assetPath, "culture_catalog", Severity.Error, $"religions[{i}].id missing/empty.");
                if (IsMissingOrEmpty(r["name"])) AddFinding(assetPath, "culture_catalog", Severity.Warning, $"religions[{i}].name missing/empty.");
                if (r["traditions"] != null && !(r["traditions"] is JArray))
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"religions[{i}].traditions must be an array.");
                if (r["traits"] != null && !(r["traits"] is JArray))
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"religions[{i}].traits must be an array.");
                if (IsMissingOrEmpty(r["religiousLeaderCharacterId"]))
                    AddFinding(assetPath, "culture_catalog", Severity.Warning, $"religions[{i}].religiousLeaderCharacterId missing/empty.");
            }
        }
        if (jo["cultures"] is JArray cultures)
        {
            for (int i = 0; i < cultures.Count; i++)
            {
                if (!(cultures[i] is JObject c))
                {
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"cultures[{i}] is not an object.");
                    continue;
                }
                bool cid = !IsMissingOrEmpty(c["cultureId"]) || !IsMissingOrEmpty(c["id"]);
                if (!cid)
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"cultures[{i}] missing cultureId/id.");
                bool name = !IsMissingOrEmpty(c["displayName"]) || !IsMissingOrEmpty(c["name"]);
                if (!name)
                    AddFinding(assetPath, "culture_catalog", Severity.Warning, $"cultures[{i}] missing displayName/name.");
                if (c["traits"] != null && !(c["traits"] is JArray))
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"cultures[{i}].traits must be an array.");
                if (c["languages"] != null && !(c["languages"] is JArray))
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"cultures[{i}].languages must be an array.");
                if (c["religions"] != null && !(c["religions"] is JArray))
                    AddFinding(assetPath, "culture_catalog", Severity.Error, $"cultures[{i}].religions must be an array.");
            }
        }
    }

    private void ValidateCulture(string assetPath, JObject jo)
    {
        bool hasCultureId = !IsMissingOrEmpty(jo["cultureId"]);
        if (!hasCultureId)
            AddFinding(assetPath, "culture", Severity.Error, "Missing or empty 'cultureId'.");
        bool hasTags = jo["tags"] is JArray;
        bool hasTraits = jo["traits"] is JArray;
        bool hasLanguages = jo["languages"] is JArray;
        if (hasTags && !hasTraits && !hasLanguages)
            AddFinding(assetPath, "culture", Severity.Legacy, "Likely legacy culture format (has 'tags' but missing 'traits'/'languages').");
        if (jo["traits"] != null && !(jo["traits"] is JArray))
            AddFinding(assetPath, "culture", Severity.Error, "'traits' must be an array of trait IDs.");
        if (jo["languages"] != null && !(jo["languages"] is JArray))
            AddFinding(assetPath, "culture", Severity.Error, "'languages' must be an array of language IDs.");
    }

    private void ValidateMenAtArmsCatalog(string assetPath, JObject jo)
    {
        RequireNonEmpty(assetPath, "men_at_arms_catalog", jo, "catalogId", Severity.Error);
        if (jo["entries"] == null)
        {
            AddFinding(assetPath, "men_at_arms_catalog", Severity.Error, "Missing 'entries'.");
            return;
        }
        if (!(jo["entries"] is JArray arr))
        {
            AddFinding(assetPath, "men_at_arms_catalog", Severity.Error, "'entries' must be an array.");
            return;
        }
        if (arr.Count > 0 && arr[0] is JValue)
        {
            AddFinding(assetPath, "men_at_arms_catalog", Severity.Legacy, "Legacy men-at-arms catalog format: 'entries' is an array of strings (expected objects). ");
            return;
        }
        for (int i = 0; i < arr.Count; i++)
        {
            if (!(arr[i] is JObject e))
            {
                AddFinding(assetPath, "men_at_arms_catalog", Severity.Error, $"entries[{i}] is not an object.");
                continue;
            }
            if (IsMissingOrEmpty(e["id"])) AddFinding(assetPath, "men_at_arms_catalog", Severity.Error, $"entries[{i}].id missing/empty.");
            if (IsMissingOrEmpty(e["displayName"])) AddFinding(assetPath, "men_at_arms_catalog", Severity.Warning, $"entries[{i}].displayName missing/empty.");
            if (e["attack"] == null) AddFinding(assetPath, "men_at_arms_catalog", Severity.Warning, $"entries[{i}].attack missing.");
            if (e["defense"] == null) AddFinding(assetPath, "men_at_arms_catalog", Severity.Warning, $"entries[{i}].defense missing.");
        }
    }

    private void ValidateArmy(string assetPath, JObject jo)
    {
        if (IsMissingOrEmpty(jo["armyId"]) && IsMissingOrEmpty(jo["id"]))
            AddFinding(assetPath, "army", Severity.Error, "Missing 'armyId'/'id'.");
        bool hasStacks = jo["menAtArmsStacks"] is JArray;
        bool hasOld = jo["menAtArms"] is JArray;
        if (hasOld && !hasStacks)
            AddFinding(assetPath, "army", Severity.Legacy, "Legacy army format: 'menAtArms' array present (expected 'menAtArmsStacks').");
        // At least one representation of men‑at‑arms must exist
        if (!hasOld && !hasStacks)
            AddFinding(assetPath, "army", Severity.Error, "Missing men‑at‑arms definition: either 'menAtArms' or 'menAtArmsStacks' must be present.");
        if (jo["menAtArmsStacks"] != null && !(jo["menAtArmsStacks"] is JArray))
            AddFinding(assetPath, "army", Severity.Error, "'menAtArmsStacks' must be an array.");
    }

    private void ValidateSettlement(string assetPath, JObject jo)
    {
        string sid = (string)jo.SelectToken("feudal.settlementId") ?? (string)jo["settlementId"];
        if (string.IsNullOrWhiteSpace(sid))
            AddFinding(assetPath, "settlement", Severity.Error, "Missing settlement ID (expected 'feudal.settlementId' or 'settlementId').");
        if (jo["feudal"] == null || !(jo["feudal"] is JObject))
            AddFinding(assetPath, "settlement", Severity.Warning, "Missing 'feudal' object (expected in new format).");
        if (jo["rulerCharacterId"] != null && jo.SelectToken("feudal.rulerCharacterId") == null)
            AddFinding(assetPath, "settlement", Severity.Legacy, "Legacy settlement: has top-level 'rulerCharacterId' (expected under feudal). ");
    }

    private void ValidateRegion(string assetPath, JObject jo)
    {
        if (IsMissingOrEmpty(jo["regionId"]) && IsMissingOrEmpty(jo["id"]))
            AddFinding(assetPath, "region", Severity.Error, "Missing 'regionId'/'id'.");
    }

    private void ValidateUnpopulated(string assetPath, JObject jo)
    {
        if (IsMissingOrEmpty(jo["areaId"]) && IsMissingOrEmpty(jo["id"]))
            AddFinding(assetPath, "unpopulated", Severity.Error, "Missing 'areaId'/'id'.");
        if (IsMissingOrEmpty(jo["subtype"]))
            AddFinding(assetPath, "unpopulated", Severity.Error, "Missing 'subtype' (e.g. Wilderness/Water/Ruins).");
    }

    private void ValidateCharacter(string assetPath, JObject jo)
    {
        if (IsMissingOrEmpty(jo["characterId"]) && IsMissingOrEmpty(jo["id"]))
            AddFinding(assetPath, "character", Severity.Error, "Missing 'characterId'/'id'.");
        if (jo["name"] == null && jo["displayName"] == null)
            AddFinding(assetPath, "character", Severity.Warning, "Missing 'name'/'displayName'.");
    }

    private void DetectLegacyShapes(string assetPath, string category, JObject jo)
    {
        if (jo["entries"] is JArray arr && arr.Count > 0 && arr[0] is JValue)
            AddFinding(assetPath, category, Severity.Legacy, "Array-of-strings detected where objects are expected (likely legacy). ");
    }

    private static string GuessCategory(JObject jo)
    {
        if (jo["catalogId"] != null && jo["cultures"] is JArray && (jo["traits"] != null || jo["religions"] != null))
            return "culture_catalog";
        if (jo["catalogId"] != null && jo["entries"] is JArray && (jo["displayName"] != null))
            return "men_at_arms_catalog";
        if (jo["cultureId"] != null || (jo["traits"] != null && jo["languages"] != null))
            return "culture";
        // Character should take precedence: some characters (e.g. monarchs) include a 'feudal' section.
        if (jo["characterId"] != null || jo["id"] != null && (jo["classLevels"] != null || jo["abilityScores"] != null))
            return "character";
        // Settlement detection
        if (jo["feudal"] != null || jo.SelectToken("feudal.settlementId") != null || jo["settlementId"] != null)
            return "settlement";
        if (jo["regionId"] != null)
            return "region";
        if (jo["areaId"] != null || jo["subtype"] != null || jo["terrainType"] != null)
            return "unpopulated";
        if (jo["armyId"] != null || jo["menAtArmsStacks"] != null || jo["primaryCommanderCharacterId"] != null)
            return "army";
        if (jo["characterId"] != null || jo["classLevels"] != null || jo["abilityScores"] != null)
            return "character";
        return "unknown";
    }

    private static string ExtractPrimaryId(JObject jo, string category)
    {
        switch (category)
        {
            case "settlement": return (string)jo.SelectToken("feudal.settlementId") ?? (string)jo["settlementId"];
            case "region": return (string)jo["regionId"] ?? (string)jo["id"];
            case "unpopulated": return (string)jo["areaId"] ?? (string)jo["id"];
            case "army": return (string)jo["armyId"] ?? (string)jo["id"];
            case "character": return (string)jo["characterId"] ?? (string)jo["id"];
            case "culture": return (string)jo["cultureId"] ?? (string)jo["id"];
            case "culture_catalog": return (string)jo["catalogId"];
            case "men_at_arms_catalog": return (string)jo["catalogId"];
            default: return (string)jo["id"];
        }
    }

    private void RequireNonEmpty(string assetPath, string cat, JObject jo, string key, Severity sev)
    {
        if (jo[key] == null || string.IsNullOrWhiteSpace((string)jo[key]))
            AddFinding(assetPath, cat, sev, $"Missing or empty '{key}'.");
    }

    private void RequireArray(string assetPath, string cat, JObject jo, string key, Severity sev)
    {
        if (jo[key] == null)
        {
            AddFinding(assetPath, cat, sev, $"Missing '{key}' array.");
            return;
        }
        if (!(jo[key] is JArray))
            AddFinding(assetPath, cat, Severity.Error, $"'{key}' must be an array.");
    }

    private static bool IsMissingOrEmpty(JToken t)
    {
        if (t == null) return true;
        if (t.Type == JTokenType.Null) return true;
        if (t.Type == JTokenType.String) return string.IsNullOrWhiteSpace((string)t);
        return false;
    }

    private void AddFinding(string assetPath, string category, Severity severity, string message)
    {
        _findings.Add(new Finding
        {
            assetPath = assetPath ?? "(non-asset path)",
            category = category ?? "unknown",
            severity = severity,
            message = message ?? ""
        });
    }

    private static string DiskToAssetPath(string diskPath)
    {
        if (string.IsNullOrWhiteSpace(diskPath)) return null;
        string dataPath = Application.dataPath.Replace('\\', '/');
        string p = diskPath.Replace('\\', '/');
        if (p.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
        {
            return "Assets" + p.Substring(dataPath.Length);
        }
        return p;
    }

    private static void PingAsset(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath)) return;
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (obj == null)
        {
            Debug.LogWarning($"Could not load asset at path: {assetPath}");
            return;
        }
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
    }

    private void PingFirst(Severity sev)
    {
        var first = _findings.FirstOrDefault(f => f.severity == sev);
        if (first == null) return;
        PingAsset(first.assetPath);
    }

    private void CopyReportToClipboard()
    {
        var lines = new List<string>(_findings.Count + 4);
        lines.Add($"JSON Health Scanner Report - {DateTime.Now}");
        lines.Add($"Total: {_findings.Count}");
        lines.Add("");
        foreach (var f in _findings)
        {
            lines.Add($"[{f.severity}] [{f.category}] {f.assetPath}");
            lines.Add($"  {f.message}");
        }
        EditorGUIUtility.systemCopyBuffer = string.Join("\n", lines);
        Debug.Log("[JSON Health Scanner] Report copied to clipboard.");
    }

    private void ExportCsv()
    {
        string path = EditorUtility.SaveFilePanel("Export JSON Scan CSV", Application.dataPath, "json_scan_report", "csv");
        if (string.IsNullOrWhiteSpace(path)) return;
        static string Esc(string s)
        {
            s = s ?? "";
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }
        using (var sw = new StreamWriter(path))
        {
            sw.WriteLine("severity,category,file,message");
            foreach (var f in _findings)
            {
                sw.WriteLine($"{Esc(f.severity.ToString())},{Esc(f.category)},{Esc(f.assetPath)},{Esc(f.message)}");
            }
        }
        Debug.Log($"[JSON Health Scanner] CSV exported: {path}");
        EditorUtility.RevealInFinder(path);
    }
}
#endif
