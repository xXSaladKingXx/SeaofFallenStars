using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for editing individual ruin data.  This session
    /// exposes a <see cref="RuinsInfoData"/> object to the inspector and
    /// implements JSON serialization hooks.  Ruins are stored under the
    /// <see cref="WorldDataCategory.Ruins"/> category.
    /// </summary>
    public sealed class RuinsAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public RuinsInfoData data = new RuinsInfoData();

        // Ruins are stored under the Unpopulated category.  This avoids
        // adding a new WorldDataCategory enum entry and fits the existing
        // model of unpopulated areas.  Editors or loaders can still
        // distinguish ruin files by their data structure.
        public override WorldDataCategory Category => WorldDataCategory.Unpopulated;

        /// <summary>
        /// Derive a filename base from the ruin identifier or display name.
        /// Falls back to "ruin" if neither field is specified.
        /// </summary>
        public override string GetDefaultFileBaseName()
        {
            string id = data?.ruinId;
            if (!string.IsNullOrWhiteSpace(id)) return id;
            string dn = data?.displayName;
            return string.IsNullOrWhiteSpace(dn) ? "ruin" : dn;
        }

        /// <summary>
        /// Serialize the ruin data to JSON.  Unknown fields are omitted.
        /// </summary>
        public override string BuildJson()
        {
            var j = Newtonsoft.Json.Linq.JObject.FromObject(data, Newtonsoft.Json.JsonSerializer.Create(JsonSettings));
            return j.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Load ruin data from a JSON string.  Missing or unknown fields are
        /// ignored.  If deserialization fails the existing data object is
        /// retained.
        /// </summary>
        public override void ApplyJson(string json)
        {
            var loaded = FromJson<RuinsInfoData>(json);
            if (loaded != null) data = loaded;
        }
    }
}