using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a trait catalog. This session allows editing and
    /// saving trait definitions in a centralized catalog. Traits defined here
    /// can be referenced by cultures, religions or races. Editing traits should
    /// be done exclusively via this catalog session.
    /// </summary>
    public sealed class TraitCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public TraitCatalogDataModel data = new TraitCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.TraitCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "trait_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<TraitCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}