using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a religion catalog. This session allows editing and
    /// saving religion definitions in a centralized catalog. Religions defined
    /// here can be referenced by cultures or races. Editing religions should
    /// be done exclusively through this catalog session.
    /// </summary>
    public sealed class ReligionCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public ReligionCatalogDataModel data = new ReligionCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.ReligionCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "religion_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<ReligionCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}