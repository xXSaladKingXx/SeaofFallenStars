using UnityEngine;
using SeaOfFallenStars.WorldData;

namespace Zana.WorldAuthoring
{
    public sealed class RegionAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public RegionInfoData data = new RegionInfoData();

        public override WorldDataCategory Category => WorldDataCategory.Region;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.regionId : null;
            if (!string.IsNullOrWhiteSpace(id)) return id;
            string dn = data != null ? data.displayName : null;
            return string.IsNullOrWhiteSpace(dn) ? "region" : dn;
        }

        /// <summary>
        /// Breakdown of the region's population by culture. Entries specify a culture
        /// identifier and the fraction of the population belonging to that culture.
        /// Serialized into the top‑level JSON property "culturalComposition".
        /// </summary>
        [Header("Cultural Composition")]
        [HideInInspector]
        public System.Collections.Generic.List<CultureCompositionEntry> culturalComposition = new System.Collections.Generic.List<CultureCompositionEntry>();

        public override string BuildJson()
        {
            var j = Newtonsoft.Json.Linq.JObject.FromObject(data, Newtonsoft.Json.JsonSerializer.Create(JsonSettings));
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
            var loaded = FromJson<RegionInfoData>(json);
            if (loaded != null) data = loaded;

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
                                culturalComposition.Add(new CultureCompositionEntry
                                {
                                    cultureId = cid,
                                    percentage = pct.Value
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore parse errors
            }
        }
    }
}
