using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for an item catalog. This session allows editing and
    /// saving item definitions in a centralized catalog. Items defined here can be
    /// referenced by inventories, resource lists and yield tables. Editing items
    /// should be done exclusively via this catalog session.
    /// </summary>
    public sealed class ItemCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public ItemCatalogDataModel data = new ItemCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.ItemCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "item_catalog" : id;
        }

        public override string BuildJson() => ToJson(data);

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<ItemCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }
    }
}