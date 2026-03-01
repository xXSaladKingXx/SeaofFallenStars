using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TMPro;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

/// <summary>
/// SubInfoPanelWindowManager is a flexible inspector for minor world data types.
/// It adapts to a variety of data models (language, religion, race, culture,
/// men‑at‑arms, trait, etc.) by displaying a title, subtitle, description,
/// arbitrary stats and percentage entries. It serves as a fallback panel for
/// data types that do not have dedicated UI panels.  This script relies on
/// WorldDataChoicesCache to resolve data definitions by id and uses simple
/// reflection to enumerate property values on the underlying entry.  Percent
/// entries are displayed using a prefab row with a name and percent text.
/// </summary>
public class SubInfoPanelWindowManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statRowPrefab;
    [SerializeField] private Transform percentContainer;
    [SerializeField] private GameObject percentRowPrefab;

    // Optional UI elements for icons and audio playback.  Assign these
    // references in the inspector if you wish to display an icon or
    // provide a button to play an associated audio clip for the current
    // entry.  If no icon or audio is resolved, these elements will be
    // automatically hidden.
    [Header("Optional Icon & Audio")]
    [SerializeField] private UnityEngine.UI.Image iconImage;
    [SerializeField] private UnityEngine.UI.Button audioPlayButton;
    [SerializeField] private AudioSource audioSource;

    // Optional dropdown support.  Assign a dropdown prefab and a
    // container to hold it.  Currently unused, reserved for future
    // extensions where a definition may provide a list of selectable
    // options.
    [Header("Optional Dropdown")]
    [SerializeField] private Transform dropdownContainer;
    [SerializeField] private GameObject dropdownPrefab;

    // This panel does not depend on WorldDataChoicesCache at runtime.  We avoid
    // static caches because WorldDataChoicesCache is only defined in the editor
    // assembly.  Names will be resolved on demand by scanning SaveData
    // directories via DataPaths.

    /// <summary>
    /// Initialize the panel for a given key and optional subInfo.  The key is
    /// typically the identifier of a world data definition (e.g. a language,
    /// culture or men‑at‑arms id).  If subInfo is provided and is a
    /// PercentEntry or list of PercentEntry, the panel will display a
    /// distribution of percentages.  Otherwise, the panel attempts to resolve
    /// the key against known data categories and reflectively display fields
    /// from the resolved entry.  Any description field found will be used as
    /// the body text.  All stats are listed in the stats container via the
    /// provided statRowPrefab.  Percent entries are sorted descending and
    /// displayed via the percentRowPrefab.
    /// </summary>
    /// <param name="key">The identifier of the data to display.</param>
    /// <param name="subInfo">Optional sub info (e.g. PercentEntry or list).</param>
    public void Initialize(string key, object subInfo = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            SetTexts("Unknown", "", "");
            ClearContainers();
            HideOptionalUI();
            return;
        }

        // If subInfo is a single PercentEntry, treat as a distribution of one.
        if (subInfo is PercentEntry singlePercent)
        {
            HandlePercentEntries(key, new List<PercentEntry> { singlePercent });
            HideOptionalUI();
            return;
        }
        // If subInfo is an enumerable of PercentEntry, treat as distribution.
        if (subInfo is IEnumerable<PercentEntry> percentList)
        {
            HandlePercentEntries(key, percentList.ToList());
            HideOptionalUI();
            return;
        }

        // Prepare optional audio clip or path from subInfo.  If subInfo is
        // an AudioClip or a string path to an audio file, capture it here.
        AudioClip providedClip = null;
        string providedAudioPath = null;
        if (subInfo is AudioClip clip)
        {
            providedClip = clip;
        }
        else if (subInfo is string s && (s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)))
        {
            providedAudioPath = s;
        }

        // Attempt to load JSON for the key.  We will derive type and fields
        // from the JSON object.  If no JSON is found, we fall back to showing
        // the key as unknown.
        JObject jObj = TryLoadJsonFromDataPaths(key);
        string title = key;
        string resolvedType = null;
        string description = string.Empty;
        var stats = new List<KeyValuePair<string, string>>();

        // Hold icon and audio hints extracted from the JSON definition.
        string iconPath = null;
        string audioPath = null;
        if (jObj != null)
        {
            title = jObj.Value<string>("displayName") ??
                    jObj.Value<string>("name") ??
                    jObj.Value<string>("id") ?? key;
            description = jObj.Value<string>("description") ??
                          jObj.Value<string>("notes") ?? string.Empty;
            foreach (var prop in jObj.Properties())
            {
                string nameField = prop.Name;
                if (string.Equals(nameField, "id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nameField, "displayName", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nameField, "name", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nameField, "description", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nameField, "notes", StringComparison.OrdinalIgnoreCase))
                    continue;
                string strVal = ConvertJTokenToString(prop.Value);
                if (string.IsNullOrWhiteSpace(strVal)) continue;
                string label = ToTitleCase(nameField);
                stats.Add(new KeyValuePair<string, string>(label, strVal));
            }
            // Determine type heuristically
            if (jObj.ContainsKey("attack") && jObj.ContainsKey("defense")) resolvedType = "Men at Arms";
            else if (jObj.ContainsKey("primaryCultureId")) resolvedType = "Language";
            else if (jObj.ContainsKey("traditions")) resolvedType = "Religion";
            else if (jObj.ContainsKey("traits") && jObj.ContainsKey("languages")) resolvedType = "Culture";
            else if (jObj.ContainsKey("traits")) resolvedType = "Race";
            else if (jObj.ContainsKey("effect") || jObj.ContainsKey("effects")) resolvedType = "Trait";

            // Extract optional icon and audio fields from the definition.  These
            // fields are not standardised across all definitions, so we try
            // multiple common names.  If present, they will be used to
            // load and display an icon or audio clip.
            iconPath = jObj.Value<string>("icon") ?? jObj.Value<string>("iconPath") ?? jObj.Value<string>("iconFile");
            audioPath = jObj.Value<string>("audio") ?? jObj.Value<string>("audioPath") ?? jObj.Value<string>("audioFile");
        }
        else
        {
            // Unknown type; show key only
            title = key;
            resolvedType = "Unknown";
            description = string.Empty;
        }
        SetTexts(title, resolvedType ?? "Unknown", description);
        ClearContainers();
        foreach (var kv in stats)
        {
            CreateStatRow(kv.Key, kv.Value);
        }

        // Configure optional UI elements after stats have been created.  This
        // includes showing an icon if available and setting up the audio
        // button and source.  The provided subInfo values take lower
        // precedence than JSON-defined fields.
        SetupIcon(iconPath);
        SetupAudio(providedClip, providedAudioPath, audioPath);

        // Hide body text if there is no description.  This allows the
        // layout group to collapse unused space.
        if (bodyText != null)
        {
            bool hasBody = !string.IsNullOrWhiteSpace(description);
            bodyText.gameObject.SetActive(hasBody);
        }

        // Force a layout rebuild to ensure proper sizing after dynamic
        // content additions or removals.
        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }

    #region Internal Helpers
    private void SetTexts(string title, string subtitle, string body)
    {
        if (titleText != null) titleText.text = title ?? string.Empty;
        if (subtitleText != null) subtitleText.text = subtitle ?? string.Empty;
        if (bodyText != null) bodyText.text = body ?? string.Empty;
    }

    private void ClearContainers()
    {
        if (statsContainer != null)
        {
            foreach (Transform child in statsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        if (percentContainer != null)
        {
            foreach (Transform child in percentContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Also hide optional UI elements whenever we clear containers.
        HideOptionalUI();
    }

    /// <summary>
    /// Hide optional UI elements (icon, audio button) and reset the audio
    /// source.  Called when the panel is cleared or when an entry has no
    /// optional media.
    /// </summary>
    private void HideOptionalUI()
    {
        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
            iconImage.sprite = null;
        }
        if (audioPlayButton != null)
        {
            audioPlayButton.gameObject.SetActive(false);
            audioPlayButton.onClick.RemoveAllListeners();
        }
        if (audioSource != null)
        {
            audioSource.clip = null;
            if (audioSource.isPlaying) audioSource.Stop();
        }
        // Dropdown remains unused until needed; no action here.
    }

    /// <summary>
    /// Attempt to load an icon from the specified relative path and assign
    /// it to the iconImage.  The path is searched under both the runtime
    /// persistent data directory and the editor SaveData directory.  If no
    /// path is provided or the file cannot be loaded, the icon image is
    /// hidden.
    /// </summary>
    private void SetupIcon(string iconPath)
    {
        if (iconImage == null)
            return;
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            iconImage.gameObject.SetActive(false);
            iconImage.sprite = null;
            return;
        }
        try
        {
            // Determine candidate roots.
            List<string> roots = new List<string>();
            string runtimeRoot = Application.persistentDataPath;
            string editorRoot = System.IO.Path.Combine(Application.dataPath, "SaveData");
            if (!string.IsNullOrWhiteSpace(runtimeRoot)) roots.Add(runtimeRoot);
            if (!string.IsNullOrWhiteSpace(editorRoot)) roots.Add(editorRoot);
            foreach (string root in roots)
            {
                string full = System.IO.Path.Combine(root, iconPath);
                if (System.IO.File.Exists(full))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(full);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        Rect rect = new Rect(0, 0, tex.width, tex.height);
                        Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f));
                        iconImage.sprite = sprite;
                        iconImage.gameObject.SetActive(true);
                        return;
                    }
                }
            }
        }
        catch { }
        iconImage.gameObject.SetActive(false);
        iconImage.sprite = null;
    }

    /// <summary>
    /// Configure audio playback.  Use a provided AudioClip if available,
    /// otherwise attempt to load a clip from the specified JSON or subInfo
    /// path.  Supported formats: WAV, OGG, MP3.  If no clip is resolved,
    /// the play button is hidden.
    /// </summary>
    private void SetupAudio(AudioClip providedClip, string providedPath, string jsonPath)
    {
        if (audioPlayButton == null || audioSource == null)
        {
            return;
        }
        // Determine which path or clip to use: JSON-defined path takes
        // precedence, then provided path, then provided clip.
        AudioClip clip = null;
        string audioPath = null;
        if (!string.IsNullOrWhiteSpace(jsonPath))
        {
            audioPath = jsonPath;
        }
        else if (!string.IsNullOrWhiteSpace(providedPath))
        {
            audioPath = providedPath;
        }
        else if (providedClip != null)
        {
            clip = providedClip;
        }
        // Load from path if necessary.
        if (clip == null && !string.IsNullOrWhiteSpace(audioPath))
        {
            try
            {
                // Candidate roots
                List<string> roots = new List<string>();
                string runtimeRoot = Application.persistentDataPath;
                string editorRoot = System.IO.Path.Combine(Application.dataPath, "SaveData");
                if (!string.IsNullOrWhiteSpace(runtimeRoot)) roots.Add(runtimeRoot);
                if (!string.IsNullOrWhiteSpace(editorRoot)) roots.Add(editorRoot);
                foreach (string root in roots)
                {
                    string fullPath = System.IO.Path.Combine(root, audioPath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        var request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, GetAudioTypeFromExtension(fullPath));
                        var op = request.SendWebRequest();
                        while (!op.isDone) { }
                        if (!request.isNetworkError && !request.isHttpError)
                        {
                            clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
                        }
                        break;
                    }
                }
            }
            catch { }
        }
        if (clip != null)
        {
            audioSource.clip = clip;
            audioPlayButton.gameObject.SetActive(true);
            audioPlayButton.onClick.RemoveAllListeners();
            audioPlayButton.onClick.AddListener(() => { audioSource.Play(); });
        }
        else
        {
            audioSource.clip = null;
            audioPlayButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Determine the appropriate AudioType based on the file extension.  If
    /// the extension is not recognized, default to WAV.
    /// </summary>
    private static UnityEngine.AudioType GetAudioTypeFromExtension(string path)
    {
        string ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
        switch (ext)
        {
            case ".ogg": return UnityEngine.AudioType.OGGVORBIS;
            case ".mp3": return UnityEngine.AudioType.MPEG;
            case ".wav": return UnityEngine.AudioType.WAV;
            default: return UnityEngine.AudioType.WAV;
        }
    }

    private void CreateStatRow(string label, string value)
    {
        if (statRowPrefab == null || statsContainer == null) return;
        GameObject row = Instantiate(statRowPrefab, statsContainer);
        var texts = row.GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length >= 2)
        {
            texts[0].text = label;
            texts[1].text = value;
        }
    }

    private void HandlePercentEntries(string key, List<PercentEntry> entries)
    {
        PopulateCaches();
        string displayName = ResolveName(key) ?? key;
        string type = "Distribution";
        SetTexts(displayName, type, string.Empty);
        ClearContainers();
        var ordered = entries.Where(x => x != null).OrderByDescending(x => x.percent).ToList();
        foreach (var pe in ordered)
        {
            if (percentRowPrefab == null || percentContainer == null) continue;
            GameObject row = Instantiate(percentRowPrefab, percentContainer);
            var texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length >= 2)
            {
                string entryName = ResolveName(pe.key) ?? pe.key;
                texts[0].text = entryName;
                texts[1].text = string.Format("{0:0.#}%", pe.percent);
            }
        }
    }

    private static T GetPropertyValue<T>(object obj, string propName)
    {
        if (obj == null) return default;
        var type = obj.GetType();
        var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop != null)
        {
            try
            {
                var val = prop.GetValue(obj);
                if (val is T tval) return tval;
                if (val != null)
                {
                    try
                    {
                        return (T)Convert.ChangeType(val, typeof(T));
                    }
                    catch
                    {
                        return default;
                    }
                }
            }
            catch { }
        }
        return default;
    }

    private static string ConvertValueToString(object value)
    {
        if (value == null) return null;
        if (value is string s) return s;
        if (value is float f) return f.ToString("0.##");
        if (value is double d) return d.ToString("0.##");
        if (value is decimal m) return m.ToString("0.##");
        if (value is int i) return i.ToString();
        if (value is long l) return l.ToString();
        if (value is bool b) return b ? "Yes" : "No";
        if (value is IEnumerable<string> sList)
            return string.Join(", ", sList);
        if (value is IEnumerable<int> iList)
            return string.Join(", ", iList);
        if (value is IEnumerable<object> list)
        {
            var sb = new List<string>();
            foreach (var item in list)
            {
                string str = ConvertValueToString(item);
                if (!string.IsNullOrWhiteSpace(str)) sb.Add(str);
            }
            return string.Join(", ", sb);
        }
        var type = value.GetType();
        var dnProp = type.GetProperty("displayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        var idProp = type.GetProperty("id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (dnProp != null)
        {
            var dn = dnProp.GetValue(value) as string;
            if (!string.IsNullOrWhiteSpace(dn)) return dn;
        }
        if (idProp != null)
        {
            var idVal = idProp.GetValue(value) as string;
            if (!string.IsNullOrWhiteSpace(idVal)) return idVal;
        }
        return value.ToString();
    }

    private static string ConvertJTokenToString(JToken token)
    {
        if (token == null) return null;
        switch (token.Type)
        {
            case JTokenType.String:
                return token.ToString();
            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.Boolean:
                return token.ToString();
            case JTokenType.Array:
                var list = new List<string>();
                foreach (var item in token)
                {
                    string str = ConvertJTokenToString(item);
                    if (!string.IsNullOrWhiteSpace(str)) list.Add(str);
                }
                return string.Join(", ", list);
            case JTokenType.Object:
                var obj = (JObject)token;
                var parts = new List<string>();
                foreach (var prop in obj.Properties())
                {
                    var subVal = ConvertJTokenToString(prop.Value);
                    if (!string.IsNullOrWhiteSpace(subVal))
                        parts.Add(string.Format("{0}: {1}", prop.Name, subVal));
                }
                return string.Join(", ", parts);
        }
        return token.ToString();
    }

    private static string ToTitleCase(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        var parts = str.Trim('_').Split(new char[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var words = new List<string>();
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            words.Add(char.ToUpper(part[0]) + part.Substring(1));
        }
        return string.Join(" ", words);
    }


    // PopulateCaches is no longer used.  All data is resolved on demand via JSON files.
    private static void PopulateCaches() { }

    private static JObject TryLoadJsonFromDataPaths(string id)
    {
        // Attempt to find and load a JSON definition for the given id by
        // searching known SaveData subdirectories (menatarms, cultures,
        // languages, religions, races, traits) in both runtime and editor
        // environments.  This implementation avoids referencing DataPaths
        // constants directly, because those definitions vary across game
        // versions.  Instead, we compute the root directories from
        // Application.persistentDataPath and Application.dataPath.
        try
        {
            // Build a list of directories to search.  We include both
            // runtime and editor SaveData roots with their respective
            // subdirectories.  Case-insensitive names are handled by
            // scanning in lowercase.
            List<string> dirs = new List<string>();
            string runtimeRoot = Application.persistentDataPath;
            string editorRoot = System.IO.Path.Combine(Application.dataPath, "SaveData");
            string[] suffixes = new[] { "menatarms", "cultures", "languages", "religions", "races", "traits" };
            foreach (string suffix in suffixes)
            {
                if (!string.IsNullOrWhiteSpace(runtimeRoot))
                {
                    string dir = System.IO.Path.Combine(runtimeRoot, suffix);
                    dirs.Add(dir);
                }
                if (!string.IsNullOrWhiteSpace(editorRoot))
                {
                    string dir = System.IO.Path.Combine(editorRoot, suffix);
                    dirs.Add(dir);
                }
            }
            string fileName = id;
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                fileName += ".json";
            foreach (string dir in dirs)
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    if (!System.IO.Directory.Exists(dir)) continue;
                    string fullPath = System.IO.Path.Combine(dir, fileName);
                    if (System.IO.File.Exists(fullPath))
                    {
                        string json = System.IO.File.ReadAllText(fullPath);
                        return JObject.Parse(json);
                    }
                }
                catch { }
            }
        }
        catch { }
        return null;
    }

    private static string ResolveName(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        // Attempt to load the JSON definition for the id and return its display name or name.
        JObject jo = TryLoadJsonFromDataPaths(id);
        if (jo != null)
        {
            string dn = jo.Value<string>("displayName") ?? jo.Value<string>("name") ?? jo.Value<string>("id");
            return string.IsNullOrWhiteSpace(dn) ? id : dn;
        }
        return id;
    }
    #endregion
}