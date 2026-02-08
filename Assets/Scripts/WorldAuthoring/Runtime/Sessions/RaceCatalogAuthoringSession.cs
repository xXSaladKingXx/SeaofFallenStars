using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a race catalog. This session allows editing and
    /// saving race definitions in a centralized catalog. Races defined here
    /// can reference trait definitions via their IDs. Editing races should be
    /// done exclusively through this catalog session.
    /// </summary>
    public sealed class RaceCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public RaceCatalogDataModel data = new RaceCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.RaceCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "race_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<RaceCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}