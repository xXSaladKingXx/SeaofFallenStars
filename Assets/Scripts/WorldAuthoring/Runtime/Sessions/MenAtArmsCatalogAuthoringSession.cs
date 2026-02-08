using UnityEngine;

namespace Zana.WorldAuthoring
{
    public sealed class MenAtArmsCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public MenAtArmsCatalogDataModel data = new MenAtArmsCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.MenAtArmsCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "men_at_arms_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<MenAtArmsCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}
