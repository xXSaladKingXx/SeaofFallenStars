using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Back-compat converter for CharacterSheetData.savingThrows.
    ///
    /// Current schema: SavingThrowEntry[] (JSON array)
    /// Legacy schema (accepted): JSON object keyed by ability id (str/dex/etc).
    /// </summary>
    public sealed class SavingThrowEntryArrayOrObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(SavingThrowEntry[]);

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
                try { return token.ToObject<SavingThrowEntry[]>(serializer); }
                catch { return existingValue ?? Array.Empty<SavingThrowEntry>(); }
            }

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                var list = new List<SavingThrowEntry>();

                foreach (var prop in obj.Properties())
                {
                    if (prop == null) continue;

                    var entry = new SavingThrowEntry { ability = prop.Name };
                    ApplyLegacyValue(entry, prop.Value);
                    list.Add(entry);
                }

                return list.ToArray();
            }

            try { return token.ToObject<SavingThrowEntry[]>(serializer); }
            catch { return existingValue ?? Array.Empty<SavingThrowEntry>(); }
        }

        private static void ApplyLegacyValue(SavingThrowEntry entry, JToken value)
        {
            if (entry == null || value == null) return;

            if (value.Type == JTokenType.Object)
            {
                var o = (JObject)value;

                var aTok = o["ability"];
                if (aTok != null && aTok.Type == JTokenType.String)
                {
                    var a = aTok.Value<string>();
                    if (!string.IsNullOrWhiteSpace(a)) entry.ability = a;
                }

                var pTok = o["proficient"] ?? o["prof"];
                if (pTok != null && pTok.Type == JTokenType.Boolean)
                    entry.proficient = pTok.Value<bool>();

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

            if (value.Type == JTokenType.Boolean)
            {
                entry.proficient = value.Value<bool>();
                return;
            }

            if (value.Type == JTokenType.Integer)
            {
                // Ambiguous legacy representation; treat as misc bonus.
                entry.miscBonus = value.Value<int>();
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
