using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for the global relationship catalog.  This session
    /// wraps a <see cref="RelationshipCatalogDataModel"/> and provides
    /// load/save functionality consistent with other authoring sessions.  The
    /// session is created and managed by <see cref="WorldDataMasterAuthoring"/>.
    /// </summary>
    public sealed class RelationshipCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        /// <summary>
        /// The data being edited in this session.
        /// </summary>
        [SerializeField]
        public RelationshipCatalogDataModel data = new RelationshipCatalogDataModel();

        /// <inheritdoc />
        public override WorldDataCategory Category => WorldDataCategory.RelationshipCatalog;

#if UNITY_EDITOR
        /// <inheritdoc />
        public override string BuildJson()
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        /// <inheritdoc />
        public override void ApplyJson(string json)
        {
            try
            {
                var model = JsonConvert.DeserializeObject<RelationshipCatalogDataModel>(json);
                if (model != null)
                    data = model;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RelationshipCatalogAuthoringSession] Failed to parse JSON: {ex.Message}");
            }
        }
#endif
    }
}