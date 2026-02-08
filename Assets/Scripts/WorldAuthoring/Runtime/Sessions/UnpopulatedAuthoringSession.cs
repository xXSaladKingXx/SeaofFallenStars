using UnityEngine;

namespace Zana.WorldAuthoring
{
    public sealed class UnpopulatedAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public UnpopulatedInfoData data = new UnpopulatedInfoData();

        public override WorldDataCategory Category => WorldDataCategory.Unpopulated;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.areaId : null;
            if (!string.IsNullOrWhiteSpace(id)) return id;
            string dn = data != null ? data.displayName : null;
            return string.IsNullOrWhiteSpace(dn) ? "unpopulated" : dn;
        }

        /// <summary>
        /// Cultural breakdown for unpopulated areas or ruins. Each entry specifies a
        /// culture identifier and the fraction of the population (or peoples) belonging
        /// to that culture. Stored in the top‑level JSON under "culturalComposition".
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
            var loaded = FromJson<UnpopulatedInfoData>(json);
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
                // parse errors ignored
            }
        }
    }
}
