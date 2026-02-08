using UnityEngine;

namespace Zana.WorldAuthoring
{
    public sealed class CultureAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public CultureInfoDataModel data = new CultureInfoDataModel();

        public override WorldDataCategory Category => WorldDataCategory.Culture;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.cultureId : null;
            if (!string.IsNullOrWhiteSpace(id)) return id;
            string dn = data != null ? data.displayName : null;
            return string.IsNullOrWhiteSpace(dn) ? "culture" : dn;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<CultureInfoDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}
