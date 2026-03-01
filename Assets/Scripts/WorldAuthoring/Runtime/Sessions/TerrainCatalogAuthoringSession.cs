using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session component for editing terrain catalogs.  This
    /// component holds the current state of a terrain catalog and
    /// implements the necessary serialization hooks so it can be saved
    /// to and loaded from JSON files.  When attached to a GameObject in
    /// the editor it will be rendered by <see
    /// cref="TerrainCatalogAuthoringSessionEditor"/>.
    /// </summary>
    public sealed class TerrainCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public TerrainCatalogDataModel data = new TerrainCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.TerrainCatalog;

        /// <summary>
        /// Returns a suitable default file base name for this catalog.  If
        /// a catalogId has been supplied it is used; otherwise the
        /// displayName is used.  Fallback is "terrain".
        /// </summary>
        public override string GetDefaultFileBaseName()
        {
            string id = data?.catalogId;
            if (!string.IsNullOrWhiteSpace(id)) return id;
            string dn = data?.displayName;
            return string.IsNullOrWhiteSpace(dn) ? "terrain" : dn;
        }

        /// <summary>
        /// Serializes the current data into a JSON string using the
        /// configured JsonSerializer settings.  Additional top level
        /// properties can be added here if needed.
        /// </summary>
        public override string BuildJson()
        {
            var j = Newtonsoft.Json.Linq.JObject.FromObject(data, Newtonsoft.Json.JsonSerializer.Create(JsonSettings));
            return j.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Deserializes the supplied JSON string into the catalog data
        /// model.  Unknown or missing fields are ignored.
        /// </summary>
        public override void ApplyJson(string json)
        {
            var loaded = FromJson<TerrainCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}