using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Back-compat converter for CharacterSheetData.skills.
    ///
    /// Current schema: SkillEntry[] (JSON array)
    /// Legacy schema (accepted): JSON object keyed by skill id.
    /// </summary>
    public sealed class SkillEntryArrayOrObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(SkillEntry[]);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JToken token;
            try
            {
                token = JToken.Load(reader);
            }
            catch
            {
                return existingValue;
            }

            if (token == null)
                return existingValue;

            if (token.Type == JTokenType.Array)
            {
                try { return token.ToObject<SkillEntry[]>(serializer); }
                catch { return existingValue ?? Array.Empty<SkillEntry>(); }
            }

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                var list = new List<SkillEntry>();

                foreach (var prop in obj.Properties())
                {
                    if (prop == null) continue;

                    var entry = new SkillEntry { skillId = prop.Name };
                    ApplyLegacyValue(entry, prop.Value);
                    list.Add(entry);
                }

                return list.ToArray();
            }

            // Unexpected token type; attempt default conversion.
            try { return token.ToObject<SkillEntry[]>(serializer); }
            catch { return existingValue ?? Array.Empty<SkillEntry>(); }
        }

        private static void ApplyLegacyValue(SkillEntry entry, JToken value)
        {
            if (entry == null || value == null)
                return;

            if (value.Type == JTokenType.Object)
            {
                var o = (JObject)value;

                // If legacy stored skillId inside the object, prefer it.
                var sidTok = o["skillId"];
                if (sidTok != null && sidTok.Type == JTokenType.String)
                {
                    var sid = sidTok.Value<string>();
                    if (!string.IsNullOrWhiteSpace(sid))
                        entry.skillId = sid;
                }

                var pTok = o["proficiency"] ?? o["prof"] ?? o["level"];
                if (pTok != null && TryParseProficiency(pTok, out var p))
                    entry.proficiency = p;

                var mbTok = o["miscBonus"] ?? o["misc"] ?? o["bonus"];
                if (mbTok != null)
                {
                    if (mbTok.Type == JTokenType.Integer)
                        entry.miscBonus = mbTok.Value<int>();
                    else if (mbTok.Type == JTokenType.Float)
                        entry.miscBonus = (int)Math.Round(mbTok.Value<double>());
                }

                return;
            }

            if (value.Type == JTokenType.Integer)
            {
                // Ambiguous legacy representation; treat as misc bonus.
                entry.miscBonus = value.Value<int>();
                return;
            }

            if (value.Type == JTokenType.String)
            {
                // If a legacy string looks like proficiency, parse.
                if (TryParseProficiency(value, out var p))
                    entry.proficiency = p;
            }
        }

        private static bool TryParseProficiency(JToken tok, out SkillProficiencyLevel prof)
        {
            prof = SkillProficiencyLevel.None;
            if (tok == null) return false;

            if (tok.Type == JTokenType.Integer)
            {
                int v = tok.Value<int>();
                if (Enum.IsDefined(typeof(SkillProficiencyLevel), v))
                {
                    prof = (SkillProficiencyLevel)v;
                    return true;
                }
                return false;
            }

            if (tok.Type == JTokenType.String)
            {
                var s = tok.Value<string>();
                if (string.IsNullOrWhiteSpace(s))
                    return false;

                if (int.TryParse(s, out int vi) && Enum.IsDefined(typeof(SkillProficiencyLevel), vi))
                {
                    prof = (SkillProficiencyLevel)vi;
                    return true;
                }

                if (Enum.TryParse(s, true, out SkillProficiencyLevel parsed))
                {
                    prof = parsed;
                    return true;
                }
            }

            return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Preserve current schema (array) when writing.
            serializer.Serialize(writer, value);
        }
    }
}
