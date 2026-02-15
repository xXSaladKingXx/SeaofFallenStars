using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a flora catalog. This session allows editing and
    /// saving plant species definitions in a centralized catalog. Entries defined
    /// here can be referenced by unpopulated areas for flora distributions. Editing
    /// plants should be done exclusively via this catalog session.
    /// </summary>
    public sealed class FloraCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public FloraCatalogDataModel data = new FloraCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.FloraCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "flora_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<FloraCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}