/// <summary>
/// Global (no-namespace) enum so legacy authoring scripts that reference
/// WorldDataCategory without a using/namespace do not break.
/// </summary>
public enum WorldDataCategory
{
    Character = 0,
    Army = 1,
    Settlement = 2,
    Region = 3,
    Unpopulated = 4,
    // Note: The Culture category is deprecated. All culture editing should be done via the CultureCatalog.
    Culture = 5,
    MenAtArmsCatalog = 6,
    /// <summary>
    /// Catalog of cultures and their trait/language/religion definitions. See
    /// CultureCatalogAuthoringSession for editing. Cultures are referenced
    /// by settlements or characters by ID.
    /// </summary>
    CultureCatalog = 7,
    /// <summary>
    /// Catalog of traits. Definitions of trait entries are stored here. Assign traits
    /// to cultures, religions or races by referencing their IDs. Editing traits must
    /// be performed through this catalog.
    /// </summary>
    TraitCatalog = 8,
    /// <summary>
    /// Catalog of languages. Each language definition includes a primary culture and
    /// description. Cultures may reference language IDs from this catalog.
    /// </summary>
    LanguageCatalog = 9,
    /// <summary>
    /// Catalog of religions. Religions contain descriptive fields and references to
    /// trait definitions. A separate session is used to edit religions.
    /// </summary>
    ReligionCatalog = 10,
    /// <summary>
    /// Catalog of races. Races include descriptive information and assigned traits.
    /// Editing of race definitions is performed through this catalog.
    /// </summary>
    RaceCatalog = 11,

    /// <summary>
    /// Catalog of flora definitions (plants/fungi) referenced by world authoring.
    /// </summary>
    FloraCatalog = 12,

    /// <summary>
    /// Catalog of fauna definitions (animals/creatures) referenced by world authoring.
    /// </summary>
    FaunaCatalog = 13,

    /// <summary>
    /// Catalog of item definitions referenced by world authoring.
    /// </summary>
    ItemCatalog = 14,

    /// <summary>
    /// Catalog of terrain/subtype definitions (used for geography and men-at-arms bonuses).
    /// </summary>
    TerrainCatalog = 15,
}
