using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a stat catalog. This session allows editing and
    /// saving stat definitions in a centralized catalog. Stat entries defined here
    /// can be referenced by traits, items and other systems when applying
    /// modifiers. Editing statistics should be done exclusively via this catalog
    /// session.
    /// </summary>
    public sealed class StatCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public StatCatalogDataModel data = new StatCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.StatCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "stat_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<StatCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}