using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SeaOfFallenStars.WorldData;
// This authoring session mirrors the original ArmyAuthoringSession from the
// project and has been retained to ensure compatibility. It derives from
// WorldDataAuthoringSessionBase and populates commander/knight lists and
// optional map position when serializing. The core ArmyInfoData schema has
// been extended to include new fields, but the session API remains the same.
namespace Zana.WorldAuthoring
{
    [DisallowMultipleComponent]
    public sealed class ArmyAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Army Data")]
        public ArmyInfoData data = new ArmyInfoData();

        [Header("Travel / Map Coordinates (Non-invasive)\nSaved into JSON as: mapPosition { x, y }")]
        [Tooltip("If enabled, this Army JSON will include a world-map coordinate for placement in Travel Mode.")]
        public bool hasMapPosition = false;
        public Vector2 mapPosition = Vector2.zero;

        [Header("Extra (Non-invasive)\nStored into JSON on save as: commanderCharacterIds, knightCharacterIds")]
        public List<string> commanderCharacterIds = new List<string>();
        public List<string> knightCharacterIds = new List<string>();

        public override WorldDataCategory Category => WorldDataCategory.Army;

        public override string GetDefaultFileBaseName()
        {
            if (data != null)
            {
                if (!string.IsNullOrWhiteSpace(data.armyId)) return data.armyId;
                if (!string.IsNullOrWhiteSpace(data.primaryCommanderCharacterId)) return $"army_{data.primaryCommanderCharacterId}";
                // ArmyInfoData does not guarantee a displayName field; prefer commander display-name if present.
                if (!string.IsNullOrWhiteSpace(data.primaryCommanderDisplayName)) return data.primaryCommanderDisplayName;
            }
            return "army";
        }

        public override string BuildJson()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            // Serialize existing schema.
            var j = JObject.FromObject(data, JsonSerializer.Create(settings));

            // Inject extra lists if any.
            var cleanCommanders = CleanList(commanderCharacterIds);
            if (cleanCommanders.Count > 0)
                j["commanderCharacterIds"] = new JArray(cleanCommanders);

            var cleanKnights = CleanList(knightCharacterIds);
            if (cleanKnights.Count > 0)
                j["knightCharacterIds"] = new JArray(cleanKnights);

            if (hasMapPosition)
            {
                j["mapPosition"] = new JObject
                {
                    ["x"] = mapPosition.x,
                    ["y"] = mapPosition.y
                };
            }

            return j.ToString(Formatting.Indented);
        }

        public override void ApplyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            // Populate known fields.
            data = JsonConvert.DeserializeObject<ArmyInfoData>(json) ?? new ArmyInfoData();

            // Populate extras if present.
            try
            {
                var j = JObject.Parse(json);
                commanderCharacterIds = ReadStringArray(j["commanderCharacterIds"]);
                knightCharacterIds = ReadStringArray(j["knightCharacterIds"]);

                // Optional travel coords (non-invasive)
                var mp = j["mapPosition"];
                if (mp is JObject mpObj)
                {
                    hasMapPosition = true;
                    mapPosition = new Vector2(
                        (float)(mpObj.Value<double?>("x") ?? 0.0),
                        (float)(mpObj.Value<double?>("y") ?? 0.0));
                }
                else
                {
                    hasMapPosition = false;
                }
            }
            catch
            {
                commanderCharacterIds = commanderCharacterIds ?? new List<string>();
                knightCharacterIds = knightCharacterIds ?? new List<string>();
                hasMapPosition = false;
            }
        }

        private static List<string> ReadStringArray(JToken token)
        {
            var list = new List<string>();
            if (token == null) return list;

            if (token.Type == JTokenType.Array)
            {
                foreach (var c in token)
                {
                    if (c == null) continue;
                    var s = c.ToString();
                    if (!string.IsNullOrWhiteSpace(s)) list.Add(s.Trim());
                }
            }

            return list;
        }

        private static List<string> CleanList(List<string> src)
        {
            var list = new List<string>();
            if (src == null) return list;

            for (int i = 0; i < src.Count; i++)
            {
                var s = src[i];
                if (string.IsNullOrWhiteSpace(s)) continue;
                s = s.Trim();
                if (!list.Contains(s)) list.Add(s);
            }

            return list;
        }
    }
}