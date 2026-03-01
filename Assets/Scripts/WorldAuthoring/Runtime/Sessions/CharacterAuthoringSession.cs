using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for character JSON files.  This version mirrors the
    /// original CharacterAuthoringSession but adds support for timeline entries on
    /// characters.  Characters now store an array of timeline event identifiers
    /// (see CharacterSheetData.timelineEntries) instead of embedding full
    /// timeline entries.  When building JSON, this session ensures the array is
    /// initialised so that empty or null lists do not cause errors.
    /// </summary>
    public sealed class CharacterAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public CharacterSheetData data = new CharacterSheetData();

        [Header("Travel / Map Coordinates (Non-invasive)\nSaved into JSON as: mapPosition { x, y }")]
        [Tooltip("If enabled, this Character JSON will include a world-map coordinate for placement in Travel Mode.")]
        public bool hasMapPosition = false;
        public Vector2 mapPosition = Vector2.zero;

        [Header("Home Settlement (Non-invasive)\nSaved into JSON as: homeSettlementId")]
        [Tooltip("Optional link to a Settlement where this character resides. This is stored as a top-level JSON field and does not require modifying CharacterSheetData.")]
        public bool hasHomeSettlementId = false;
        public string homeSettlementId;

        public override WorldDataCategory Category => WorldDataCategory.Character;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.characterId : null;
            if (!string.IsNullOrWhiteSpace(id)) return id;
            string dn = data != null ? data.displayName : null;
            return string.IsNullOrWhiteSpace(dn) ? "character" : dn;
        }

        public override string BuildJson()
        {
            // Ensure characterId and displayName are populated. If the authoring user has not
            // explicitly set them, derive sensible defaults so downstream tools (e.g. JSON health
            // scanners) do not flag missing IDs or names. We attempt to use the displayName
            // first (sanitized and lower‑cased) as the ID; if that is also blank we fall back
            // to the default file base name.
            if (data != null)
            {
                // CharacterId
                if (string.IsNullOrWhiteSpace(data.characterId))
                {
                    string source = data.displayName;
                    if (string.IsNullOrWhiteSpace(source))
                        source = GetDefaultFileBaseName();
                    if (!string.IsNullOrWhiteSpace(source))
                    {
                        // Slugify: remove whitespace and punctuation, lower-case
                        string id = source.Trim();
                        id = System.Text.RegularExpressions.Regex.Replace(id, "[^A-Za-z0-9_]+", "_");
                        id = id.Trim('_').ToLowerInvariant();
                        data.characterId = id;
                    }
                }

                // DisplayName
                if (string.IsNullOrWhiteSpace(data.displayName) && !string.IsNullOrWhiteSpace(data.characterId))
                {
                    // Humanize the ID: replace underscores with spaces and capitalize
                    string name = data.characterId.Replace('_', ' ');
                    data.displayName = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
                }

                // Ensure timelineEntries array is not null.  Characters now store
                // references to timeline events rather than embedding them.  If a
                // character does not yet participate in any events, initialize
                // this property to an empty array to avoid null reference errors.
                if (data.timelineEntries == null)
                {
                    data.timelineEntries = Array.Empty<string>();
                }
            }

            // Non-invasive: keep using your existing CharacterSheetData schema,
            // but optionally persist coordinates in a separate field.
            var j = JObject.FromObject(data, Newtonsoft.Json.JsonSerializer.Create(JsonSettings));
            if (hasHomeSettlementId && !string.IsNullOrWhiteSpace(homeSettlementId))
            {
                j["homeSettlementId"] = homeSettlementId;
            }
            if (hasMapPosition)
            {
                j["mapPosition"] = new JObject
                {
                    ["x"] = mapPosition.x,
                    ["y"] = mapPosition.y
                };
            }
            return j.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        public override void ApplyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            var loaded = FromJson<CharacterSheetData>(json);
            if (loaded != null) data = loaded;

            // Non-invasive optional home settlement id.
            try
            {
                var j2 = JObject.Parse(json);
                string hs = j2.Value<string>("homeSettlementId");
                if (!string.IsNullOrWhiteSpace(hs))
                {
                    homeSettlementId = hs;
                    hasHomeSettlementId = true;
                }
            }
            catch { /* ignore */ }

            // Non-invasive optional coordinate read.
            try
            {
                var j = JObject.Parse(json);
                var mp = j["mapPosition"] ?? j["coordinates"];
                if (mp is JObject o)
                {
                    float x = o.Value<float?>("x") ?? 0f;
                    float y = o.Value<float?>("y") ?? 0f;
                    mapPosition = new Vector2(x, y);
                    hasMapPosition = true;
                }
            }
            catch
            {
                // ignore parse errors
            }

            // Ensure timelineEntries is initialized on load to avoid null ref when
            // editing newly loaded characters that have not yet been updated.
            if (data != null && data.timelineEntries == null)
            {
                data.timelineEntries = Array.Empty<string>();
            }
        }
    }
}