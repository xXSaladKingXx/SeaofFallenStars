using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for the building catalog.  Worlds should contain a single
    /// instance of this catalog which defines all available buildings.  This
    /// authoring session allows loading from and saving to JSON files and
    /// exposes the catalog data via the inspector.
    /// </summary>
    public sealed class BuildingCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        public BuildingCatalogDataModel data = new BuildingCatalogDataModel();

        public override WorldDataCategory Category => WorldDataCategory.BuildingCatalog;

        public override string GetDefaultFileBaseName() => "main";

        public override string BuildJson()
        {
            EnsureDataShape();
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public override void ApplyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                data = new BuildingCatalogDataModel();
                EnsureDataShape();
                return;
            }
            try
            {
                data = JsonConvert.DeserializeObject<BuildingCatalogDataModel>(json)
                       ?? new BuildingCatalogDataModel();
            }
            catch (Exception)
            {
                data = new BuildingCatalogDataModel();
            }
            EnsureDataShape();
        }

        private void EnsureDataShape()
        {
            if (data == null)
                data = new BuildingCatalogDataModel();
            data.entries ??= new System.Collections.Generic.List<BuildingEntryModel>();
            // Seed with some basic settlement tier entries if empty.
            if (data.entries.Count == 0)
            {
                data.entries.AddRange(new[]
                {
                    new BuildingEntryModel
                    {
                        id = "village",
                        displayName = "Village",
                        category = BuildingCategory.SettlementTier,
                        cost = 2500,
                        income = 0,
                        levies = 1,
                        defense = 1,
                        stability = 0,
                        prestige = 0,
                        happiness = 0,
                        population = 50,
                        buildingSlots = 1,
                        tradeCapacity = 0,
                        prerequisites = "None",
                        buildTime = "6 months"
                    },
                    new BuildingEntryModel
                    {
                        id = "large_village",
                        displayName = "Large Village",
                        category = BuildingCategory.SettlementTier,
                        cost = 5000,
                        income = 1,
                        levies = 0,
                        defense = 0,
                        stability = 1,
                        prestige = 0,
                        happiness = 0,
                        population = 100,
                        buildingSlots = 2,
                        tradeCapacity = 1,
                        prerequisites = "Residential Area Level 1",
                        buildTime = "8 months"
                    },
                    new BuildingEntryModel
                    {
                        id = "small_town",
                        displayName = "Small Town",
                        category = BuildingCategory.SettlementTier,
                        cost = 10000,
                        income = 2,
                        levies = 0,
                        defense = 0,
                        stability = 1,
                        prestige = 0,
                        happiness = 1,
                        population = 350,
                        buildingSlots = 2,
                        tradeCapacity = 2,
                        prerequisites = "Large Village",
                        buildTime = "18 months"
                    },
                    new BuildingEntryModel
                    {
                        id = "large_town",
                        displayName = "Large Town",
                        category = BuildingCategory.SettlementTier,
                        cost = 25000,
                        income = 4,
                        levies = 0,
                        defense = 0,
                        stability = 1,
                        prestige = 2,
                        happiness = 1,
                        population = 750,
                        buildingSlots = 3,
                        tradeCapacity = 3,
                        prerequisites = "Small Town",
                        buildTime = "2 years"
                    },
                    new BuildingEntryModel
                    {
                        id = "city",
                        displayName = "City",
                        category = BuildingCategory.SettlementTier,
                        cost = 75000,
                        income = 8,
                        levies = 0,
                        defense = 0,
                        stability = 1,
                        prestige = 5,
                        happiness = -1,
                        population = 2000,
                        buildingSlots = 4,
                        tradeCapacity = 4,
                        prerequisites = "Large Town",
                        buildTime = "5 years"
                    },
                    new BuildingEntryModel
                    {
                        id = "metropolis",
                        displayName = "Metropolis",
                        category = BuildingCategory.SettlementTier,
                        cost = 300000,
                        income = 18,
                        levies = 0,
                        defense = 0,
                        stability = -1,
                        prestige = 10,
                        happiness = -2,
                        population = 10000,
                        buildingSlots = 5,
                        tradeCapacity = 10,
                        prerequisites = "City",
                        buildTime = "15 years"
                    }
                });
            }
        }
    }
}