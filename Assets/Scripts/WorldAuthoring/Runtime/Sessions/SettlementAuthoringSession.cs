using UnityEngine;
using SeaOfFallenStars.WorldData;

// This authoring session mirrors the original SettlementAuthoringSession from the
// project. It preserves the session API while allowing the SettlementInfoData
// schema to be extended. Cultural composition is serialized into a top‑level
// array named "culturalComposition" if any entries exist.
namespace Zana.WorldAuthoring
{
    public sealed class SettlementAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public SettlementInfoData data = new SettlementInfoData();

        public override WorldDataCategory Category => WorldDataCategory.Settlement;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.settlementId : null;
            if (!string.IsNullOrWhiteSpace(id)) return id;
            string dn = data != null ? data.displayName : null;
            return string.IsNullOrWhiteSpace(dn) ? "settlement" : dn;
        }

        /// <summary>
        /// Breakdown of the settlement's population by culture. Each entry specifies
        /// a culture identifier and the fraction of the population belonging to that
        /// culture. When serialized, this list is written to a top‑level JSON
        /// property named "culturalComposition". Values should sum to 1.0 but this
        /// is not enforced programmatically.
        /// </summary>
        [Header("Cultural Composition")]
        [HideInInspector]
        public System.Collections.Generic.List<CultureCompositionEntry> culturalComposition = new System.Collections.Generic.List<CultureCompositionEntry>();

        /// <summary>
        /// Garrison composition expressed as a list of men-at-arms stacks. Each entry
        /// specifies a men-at-arms unit identifier and the number of units present.
        /// Stored under the settlement's army tab when serialized.
        /// </summary>
        [Header("Garrison Composition")]
        [HideInInspector]
        public System.Collections.Generic.List<MenAtArmsQuantityEntry> menAtArmsStacks = new System.Collections.Generic.List<MenAtArmsQuantityEntry>();

        /// <summary>
        /// Breakdown of the settlement's population by race. Each entry specifies
        /// a race identifier and the fraction of the population belonging to that
        /// race. This list is currently stored in an extension field during
        /// serialization and may be used by the editor for demographic analysis.
        /// </summary>
        [Header("Race Distribution")]
        [HideInInspector]
        public System.Collections.Generic.List<RaceDistributionEntry> raceDistribution = new System.Collections.Generic.List<RaceDistributionEntry>();

        public override string BuildJson()
        {
            // Serialize the settlement data to a JObject so we can inject additional
            // authoring properties without altering the core SettlementInfoData schema.
            var j = Newtonsoft.Json.Linq.JObject.FromObject(data, Newtonsoft.Json.JsonSerializer.Create(JsonSettings));

            // Persist the cultural composition if any entries exist. Each entry is
            // serialized as { "cultureId": "id", "percentage": <float> } and
            // collectively stored in an array on the top‑level property
            // "culturalComposition". If the list is empty, omit the property.
            if (culturalComposition != null && culturalComposition.Count > 0)
            {
                var arr = new Newtonsoft.Json.Linq.JArray();
                foreach (var entry in culturalComposition)
                {
                    if (entry == null) continue;
                    var o = new Newtonsoft.Json.Linq.JObject
                    {
                        ["cultureId"] = entry.cultureId,
                        ["percentage"] = entry.percentage
                    };
                    arr.Add(o);
                }
                if (arr.Count > 0)
                    j["culturalComposition"] = arr;
            }

            return j.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<SettlementInfoData>(json);
            if (loaded != null) data = loaded;

            // Parse the cultural composition from the JSON. If the property is
            // absent or malformed, clear the current composition list.
            culturalComposition.Clear();
            try
            {
                var jo = Newtonsoft.Json.Linq.JObject.Parse(json);
                var arr = jo["culturalComposition"] as Newtonsoft.Json.Linq.JArray;
                if (arr != null)
                {
                    foreach (var token in arr)
                    {
                        if (token is Newtonsoft.Json.Linq.JObject o)
                        {
                            string cid = (string)o["cultureId"];
                            // Parse percentage using ToObject<T>() to avoid CS7036 error with Value<T>()
                            float? pct = o["percentage"]?.ToObject<float?>();
                            if (!string.IsNullOrWhiteSpace(cid) && pct.HasValue)
                            {
                                var entry = new CultureCompositionEntry
                                {
                                    cultureId = cid,
                                    percentage = pct.Value
                                };
                                culturalComposition.Add(entry);
                            }
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, leave the composition list empty.
            }
        }
    }
}