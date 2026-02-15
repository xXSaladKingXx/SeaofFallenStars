using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a fauna catalog. This session allows editing and
    /// saving creature species definitions in a centralized catalog. Entries defined
    /// here can be referenced by unpopulated areas for fauna distributions. Editing
    /// creatures should be done exclusively via this catalog session.
    /// </summary>
    public sealed class FaunaCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public FaunaCatalogDataModel data = new FaunaCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.FaunaCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "fauna_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<FaunaCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}