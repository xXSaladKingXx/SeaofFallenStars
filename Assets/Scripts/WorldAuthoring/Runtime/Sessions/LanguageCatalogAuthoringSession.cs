using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a language catalog. Allows editing and saving a
    /// centralized catalog of language definitions. Languages defined here can
    /// be referenced by cultures or characters by ID. Editing of languages
    /// should be performed exclusively via this catalog session.
    /// </summary>
    public sealed class LanguageCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public LanguageCatalogDataModel data = new LanguageCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.LanguageCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "language_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<LanguageCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}