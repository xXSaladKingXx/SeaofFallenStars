using System;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Attach this to a persistent editor object in your map systems. It is the ONLY authoring component you need in‑scene.
    /// It spawns temporary session components as children when you create/load individual JSON authoring sessions.
    /// </summary>
    public sealed class WorldDataMasterAuthoring : MonoBehaviour
    {
        [Header("Active Session")]
        [SerializeField] private WorldDataCategory activeCategory = WorldDataCategory.Character;
        [SerializeField] private WorldDataAuthoringSessionBase activeSession;

        public WorldDataCategory ActiveCategory => activeCategory;
        public WorldDataAuthoringSessionBase ActiveSession => activeSession;

        public void ClearActiveSession()
        {
            if (activeSession != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(activeSession.gameObject);
                else
                    Destroy(activeSession.gameObject);
#else
                Destroy(activeSession.gameObject);
#endif
            }

            activeSession = null;
        }

        public WorldDataAuthoringSessionBase CreateOrReplaceSession(WorldDataCategory category)
        {
            ClearActiveSession();

            activeCategory = category;

            var go = new GameObject($"__AuthoringSession_{category}");
            go.transform.SetParent(transform, false);

            activeSession = AddSessionComponent(go, category);
            return activeSession;
        }

        private static WorldDataAuthoringSessionBase AddSessionComponent(GameObject go, WorldDataCategory category)
        {
            switch (category)
            {
                case WorldDataCategory.Character:
                    return go.AddComponent<CharacterAuthoringSession>();
                case WorldDataCategory.Army:
                    return go.AddComponent<ArmyAuthoringSession>();
                case WorldDataCategory.Settlement:
                    return go.AddComponent<SettlementAuthoringSession>();
                case WorldDataCategory.Region:
                    return go.AddComponent<RegionAuthoringSession>();
                case WorldDataCategory.Unpopulated:
                    return go.AddComponent<UnpopulatedAuthoringSession>();
                case WorldDataCategory.Culture:
                    // Cultures are no longer edited via individual CultureAuthoringSession instances.
                    // Redirect requests for a Culture session to the CultureCatalogAuthoringSession so users
                    // cannot create or modify cultures outside the catalog.
                    return go.AddComponent<CultureCatalogAuthoringSession>();
                case WorldDataCategory.MenAtArmsCatalog:
                    return go.AddComponent<MenAtArmsCatalogAuthoringSession>();
                case WorldDataCategory.CultureCatalog:
                    return go.AddComponent<CultureCatalogAuthoringSession>();
                case WorldDataCategory.TraitCatalog:
                    return go.AddComponent<TraitCatalogAuthoringSession>();
                case WorldDataCategory.LanguageCatalog:
                    return go.AddComponent<LanguageCatalogAuthoringSession>();
                case WorldDataCategory.ReligionCatalog:
                    return go.AddComponent<ReligionCatalogAuthoringSession>();
                case WorldDataCategory.RaceCatalog:
                    return go.AddComponent<RaceCatalogAuthoringSession>();
                case WorldDataCategory.FloraCatalog:
                    return go.AddComponent<FloraCatalogAuthoringSession>();
                case WorldDataCategory.FaunaCatalog:
                    return go.AddComponent<FaunaCatalogAuthoringSession>();
                case WorldDataCategory.ItemCatalog:
                    return go.AddComponent<ItemCatalogAuthoringSession>();
                case WorldDataCategory.StatCatalog:
                    return go.AddComponent<StatCatalogAuthoringSession>();
                case WorldDataCategory.TerrainCatalog:
                    return go.AddComponent<TerrainCatalogAuthoringSession>();
                case WorldDataCategory.TimelineCatalog:
                    return go.AddComponent<TimelineCatalogAuthoringSession>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}