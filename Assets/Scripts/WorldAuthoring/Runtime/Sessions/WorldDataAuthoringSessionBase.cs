using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// A per-type authoring session component. Intended to be created/destroyed by WorldDataMasterAuthoring.
    /// </summary>
    public abstract class WorldDataAuthoringSessionBase : MonoBehaviour
    {
        [Header("Authoring Session")]
        [SerializeField] private string loadedFilePath;

        [NonSerialized] private string lastLoadError;

        /// <summary>
        /// If the last load failed (eg, attempting to open an older or differently-structured JSON),
        /// the exception message is stored here so Inspector UIs can show it without breaking the editor.
        /// </summary>
        public string LastLoadError => lastLoadError;

        /// <summary>Full path to the JSON file currently loaded (editor path).</summary>
        public string LoadedFilePath => loadedFilePath;

        public bool HasLoadedFile => !string.IsNullOrWhiteSpace(loadedFilePath);

        public abstract WorldDataCategory Category { get; }

        /// <summary>Returns a safe base filename (without .json), usually derived from the id or displayName.</summary>
        public abstract string GetDefaultFileBaseName();

        /// <summary>Builds JSON from the current serialized data state.</summary>
        public abstract string BuildJson();

        /// <summary>Applies a JSON blob onto the current serialized data state.</summary>
        public abstract void ApplyJson(string json);

        public void ClearLoadedFile() => loadedFilePath = null;

        public void SetLoadedFilePath(string path) => loadedFilePath = path;

        public string GetDirectoryEnsured()
        {
            return WorldDataDirectoryResolver.EnsureEditorDirectory(Category);
        }

        public string SaveNow()
        {
#if UNITY_EDITOR
            lastLoadError = null;
            string dir = GetDirectoryEnsured();
            string file = loadedFilePath;

            if (string.IsNullOrWhiteSpace(file))
            {
                string baseName = GetDefaultFileBaseName();
                if (string.IsNullOrWhiteSpace(baseName)) baseName = gameObject.name;
                file = Path.Combine(dir, SanitizeFileBaseName(baseName) + ".json");
            }

            string json = BuildJson();
            File.WriteAllText(file, json);
            loadedFilePath = file;
            return file;
#else
            Debug.LogWarning("[WorldDataAuthoring] SaveNow() is editor-only.");
            return null;
#endif
        }

        public bool TryLoadFromFile(string filePath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                string json = File.ReadAllText(filePath);
                ApplyJson(json);
                loadedFilePath = filePath;
                lastLoadError = null;
                return true;
            }
            catch (Exception ex)
            {
                lastLoadError = ex.Message;
                Debug.LogWarning($"[WorldDataAuthoring] Failed to load '{filePath}'. {ex.Message}");
                return false;
            }
#else
            Debug.LogWarning("[WorldDataAuthoring] TryLoadFromFile() is editor-only.");
            return false;
#endif
        }

        protected static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
        };

        protected static string ToJson(object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented, JsonSettings);
        }

        protected static T FromJson<T>(string json) where T : class
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSettings);
        }

        protected static string SanitizeFileBaseName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "data";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }
    }
}
