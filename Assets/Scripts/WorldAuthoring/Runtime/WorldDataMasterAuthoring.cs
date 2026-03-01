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
                // When clearing the session, remove only the component.  Do not
                // destroy the GameObject itself because it may be the root
                // authoring object.  DestroyImmediate will immediately remove
                // the component in edit mode.
                if (!Application.isPlaying)
                {
                    DestroyImmediate(activeSession);
                }
                else
                {
                    Destroy(activeSession);
                }
#else
                Destroy(activeSession);
#endif
            }
            activeSession = null;
            // Destroy any leftover child authoring session objects (named
            // __AuthoringSession_*).  These are remnants from older
            // implementations where sessions were created on children.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null && child.name.StartsWith("__AuthoringSession_", StringComparison.Ordinal))
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(child.gameObject);
                    else
                        Destroy(child.gameObject);
#else
                    Destroy(child.gameObject);
#endif
                }
            }
        }

        public WorldDataAuthoringSessionBase CreateOrReplaceSession(WorldDataCategory category)
        {
            // Clear any existing session
            ClearActiveSession();
            activeCategory = category;
            // Attach the new session to this GameObject directly rather than
            // creating a child.  This allows the session data to appear on
            // the root object in the inspector.
            activeSession = AddSessionComponent(gameObject, category);
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
                    // Use reflection to add the timeline catalog session to avoid compile‑time
                    // conversion errors if the generic type cannot be implicitly cast.  We
                    // add the component by type name and then cast it to the base class at runtime.
                    {
                        var component = go.AddComponent(typeof(TimelineCatalogAuthoringSession)) as WorldDataAuthoringSessionBase;
                        return component;
                    }

                case WorldDataCategory.RelationshipCatalog:
                    // Relationship catalog has its own authoring session.  Add it directly.
                    return go.AddComponent<RelationshipCatalogAuthoringSession>();
                case WorldDataCategory.BuildingCatalog:
                    // Building catalog authoring session.
                    return go.AddComponent<BuildingCatalogAuthoringSession>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}