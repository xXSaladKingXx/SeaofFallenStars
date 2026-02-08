using UnityEngine;
using SeaOfFallenStars.WorldData;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for a culture catalog. Allows you to edit and save a
    /// centralized catalog of cultures along with the trait, language and
    /// religion definitions they reference. This works similarly to the
    /// MenAtArmsCatalogAuthoringSession but manages cultures instead of units.
    /// </summary>
    public sealed class CultureCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public CultureCatalogDataModel data = new CultureCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.CultureCatalog;

        public override string GetDefaultFileBaseName()
        {
            if (data == null) return "culture_catalog";

            // Prefer stable ID, but fall back to displayName so "Name" edits
            // affect the initial save path when an ID hasn't been set yet.
            string id = data.catalogId;
            if (!string.IsNullOrWhiteSpace(id)) return id;

            string dn = data.displayName;
            if (!string.IsNullOrWhiteSpace(dn)) return dn;

            return "culture_catalog";
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<CultureCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}