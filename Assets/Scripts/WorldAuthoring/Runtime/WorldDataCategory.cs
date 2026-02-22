// Global (no-namespace) enum so legacy authoring scripts that reference
// WorldDataCategory without a using/namespace do not break.
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

    // Catalog of cultures and their trait/language/religion definitions.
    // See CultureCatalogAuthoringSession for editing. Cultures are referenced
    // by settlements or characters by ID.
    CultureCatalog = 7,

    // Catalog of traits. Definitions of trait entries are stored here.
    TraitCatalog = 8,

    // Catalog of languages.
    LanguageCatalog = 9,

    // Catalog of religions.
    ReligionCatalog = 10,

    // Catalog of races.
    RaceCatalog = 11,

    // Catalog of flora.
    FloraCatalog = 12,

    // Catalog of fauna.
    FaunaCatalog = 13,

    // Catalog of items.
    ItemCatalog = 14,

    // Catalog of statistics (existing in repo; used by traits/items/etc).
    StatCatalog = 15,

    // Catalog of terrain types (added).
    TerrainCatalog = 16,
}