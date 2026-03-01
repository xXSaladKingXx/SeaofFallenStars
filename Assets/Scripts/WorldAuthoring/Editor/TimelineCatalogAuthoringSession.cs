using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for the timeline catalog.  This session exposes the
    /// TimelineCatalogDataModel to the Unity inspector, allowing users to add,
    /// edit and remove timeline events.  The authoring UI should provide
    /// auto‑sorting of events by date and controls for inserting new entries
    /// between existing ones.
    /// </summary>
    public sealed class TimelineCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public TimelineCatalogDataModel data = new TimelineCatalogDataModel();

        /// <summary>
        /// Identifier for this session category.
        /// </summary>
        public override WorldDataCategory Category => WorldDataCategory.TimelineCatalog;

        public override string GetDefaultFileBaseName()
        {
            string id = data != null ? data.catalogId : null;
            return string.IsNullOrWhiteSpace(id) ? "timeline_catalog" : id;
        }

        /// <summary>
        /// Builds the JSON for the timeline catalog.  Before serialization this
        /// method ensures that any "Other" events with a custom type name are
        /// promoted to custom event types and that participant timeline lists
        /// across characters, settlements and unpopulated areas are kept in
        /// sync.  Events with unspecified month or day fields default to
        /// January/1 when computing chronological order; however this is
        /// handled elsewhere in the UI.
        /// </summary>
        public override string BuildJson()
        {
            if (data == null)
                data = new TimelineCatalogDataModel();

            // Promote any "Other" events that specify a customTypeName into
            // formal custom event definitions.  Convert the eventType to
            // Custom so the UI can reflect the new type.  This allows
            // authors to reuse the definition for future events.
            SaveCustomTypesAndConvertEvents();

            // Ensure that participants are aware of their event ids.  For each
            // event, iterate its participants and add the event id to the
            // corresponding timeline list in the participant's JSON.  If the
            // participant JSON cannot be found, a warning is logged instead.
            SynchronizeParticipantTimelineIds();

            // Serialize the updated catalog to JSON.
            return ToJson(data);
        }

        public override void ApplyJson(string json)
        {
            var loaded = FromJson<TimelineCatalogDataModel>(json);
            if (loaded != null) data = loaded;
        }

        /// <summary>
        /// Converts "Other" events with a custom type name into the Custom type and
        /// records a corresponding custom type definition if one does not yet
        /// exist.  Participant categories are inferred heuristically from the
        /// participant identifiers.  Directionality is assumed to be bilateral
        /// because arbitrary other events may involve mutual interaction.  If a
        /// custom type with the same name already exists it is left unchanged.
        /// </summary>
        private void SaveCustomTypesAndConvertEvents()
        {
            if (data?.events == null)
                return;
            foreach (var evt in data.events)
            {
                if (evt == null)
                    continue;

                // Only operate on events typed as Other with a custom name.
                if (evt.eventType != EventType.Other)
                    continue;
                if (string.IsNullOrWhiteSpace(evt.customTypeName))
                    continue;

                // Check if this custom type name already exists.
                bool exists = false;
                if (data.customEventTypes != null)
                {
                    foreach (var def in data.customEventTypes)
                    {
                        if (def != null && string.Equals(def.name, evt.customTypeName, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                }
                if (!exists)
                {
                    // Infer participant categories from participant IDs.
                    var categories = new List<ParticipantCategory>();
                    if (evt.participantIds != null && evt.participantIds.Count > 0)
                    {
                        foreach (var pid in evt.participantIds)
                        {
                            categories.Add(GetParticipantCategory(pid));
                        }
                    }
                    var def = new CustomEventTypeDefinition
                    {
                        name = evt.customTypeName,
                        participantCategories = categories,
                        directionality = EventDirection.TwoWay,
                        icon = evt.icon
                    };
                    if (data.customEventTypes == null)
                        data.customEventTypes = new List<CustomEventTypeDefinition>();
                    data.customEventTypes.Add(def);
                }
                // Convert the event to a custom type.
                evt.eventType = EventType.Custom;
            }
        }

        /// <summary>
        /// Synchronises event identifiers into the timeline lists of each
        /// participant.  For characters the timelineEntries array is stored
        /// top‑level on CharacterSheetData.  For settlements and unpopulated
        /// areas the entry is stored under history.timelineEntries in their
        /// JSON.  This method reads and writes JSON files directly from the
        /// editor directories using WorldDataDirectoryResolver.  Missing
        /// participants will generate warnings but do not interrupt the save.
        /// </summary>
        private void SynchronizeParticipantTimelineIds()
        {
            if (data?.events == null)
                return;
            foreach (var evt in data.events)
            {
                if (evt == null || string.IsNullOrWhiteSpace(evt.id))
                    continue;
                if (evt.participantIds == null)
                    continue;
                foreach (var pid in evt.participantIds)
                {
                    if (string.IsNullOrWhiteSpace(pid))
                        continue;
                    try
                    {
                        var category = GetParticipantCategory(pid);
                        switch (category)
                        {
                            case ParticipantCategory.Character:
                                UpdateCharacterTimeline(pid, evt.id);
                                break;
                            case ParticipantCategory.Settlement:
                            case ParticipantCategory.Unpopulated:
                                UpdateMapTimeline(pid, evt.id);
                                break;
                            default:
                                // Other categories (Army, TravelGroup) do not currently
                                // support timeline participation tracking.  Log for
                                // completeness.
                                Debug.Log($"[TimelineCatalog] Participant '{pid}' of category '{category}' for event '{evt.id}' cannot record timeline entries.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[TimelineCatalog] Failed to synchronise event '{evt.id}' for participant '{pid}'. {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Determine the participant category based on the identifier and the
        /// structure of the corresponding JSON file.  Characters live in the
        /// Characters directory; settlements and unpopulated areas live in the
        /// MapData directory.  Unpopulated areas have isPopulated=false on
        /// their root object.  If the JSON cannot be found, the method
        /// assumes a Character category to minimise unintended updates.
        /// </summary>
        private ParticipantCategory GetParticipantCategory(string participantId)
        {
            try
            {
                // Check characters directory
                string charDir = WorldDataDirectoryResolver.GetEditorCharactersDir();
                if (!string.IsNullOrEmpty(charDir))
                {
                    string charPath = System.IO.Path.Combine(charDir, participantId + ".json");
                    if (System.IO.File.Exists(charPath))
                    {
                        return ParticipantCategory.Character;
                    }
                }

                // Check map data directory (settlements and unpopulated)
                string mapDir = WorldDataDirectoryResolver.GetEditorMapDataDir();
                if (!string.IsNullOrEmpty(mapDir))
                {
                    string mapPath = System.IO.Path.Combine(mapDir, participantId + ".json");
                    if (System.IO.File.Exists(mapPath))
                    {
                        // Inspect root JSON to decide settlement vs unpopulated
                        var text = System.IO.File.ReadAllText(mapPath);
                        var j = JObject.Parse(text);
                        bool? populated = j.Value<bool?>("isPopulated");
                        if (populated.HasValue && !populated.Value)
                            return ParticipantCategory.Unpopulated;
                        return ParticipantCategory.Settlement;
                    }
                }
            }
            catch
            {
                // ignore exceptions and fall through
            }
            // Default to character if unknown
            return ParticipantCategory.Character;
        }

        /// <summary>
        /// Add the given event id to a character's timeline entries.  Reads
        /// Character JSON from the editor directory, updates or adds the
        /// timelineEntries array, and writes back the file.  Logs a message
        /// describing whether the id was added or already present.
        /// </summary>
        private void UpdateCharacterTimeline(string characterId, string eventId)
        {
            string dir = WorldDataDirectoryResolver.GetEditorCharactersDir();
            if (string.IsNullOrEmpty(dir)) return;
            string filePath = System.IO.Path.Combine(dir, characterId + ".json");
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[TimelineCatalog] Character file '{characterId}.json' not found when adding event '{eventId}'.");
                return;
            }
            string json = System.IO.File.ReadAllText(filePath);
            var root = JObject.Parse(json);
            JArray arr = root["timelineEntries"] as JArray;
            if (arr == null)
            {
                arr = new JArray();
            }
            // Ensure event id present
            bool contains = false;
            foreach (var token in arr)
            {
                if (token != null && string.Equals(token.ToString(), eventId, StringComparison.Ordinal))
                {
                    contains = true;
                    break;
                }
            }
            if (!contains)
            {
                arr.Add(eventId);
                root["timelineEntries"] = arr;
                System.IO.File.WriteAllText(filePath, root.ToString());
                Debug.Log($"[TimelineCatalog] Added event '{eventId}' to character '{characterId}'.");
            }
            else
            {
                Debug.Log($"[TimelineCatalog] Character '{characterId}' already contains event '{eventId}'.");
            }
        }

        /// <summary>
        /// Add the given event id to a map data (settlement or unpopulated) JSON file.
        /// The timeline entries are stored under history.timelineEntries.  If the
        /// file or history section is missing, it will be created.  Logs
        /// descriptive messages on update or if the event is already present.
        /// </summary>
        private void UpdateMapTimeline(string mapId, string eventId)
        {
            string dir = WorldDataDirectoryResolver.GetEditorMapDataDir();
            if (string.IsNullOrEmpty(dir)) return;
            string filePath = System.IO.Path.Combine(dir, mapId + ".json");
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[TimelineCatalog] Map data file '{mapId}.json' not found when adding event '{eventId}'.");
                return;
            }
            string json = System.IO.File.ReadAllText(filePath);
            var root = JObject.Parse(json);
            // Ensure history object
            var history = root["history"] as JObject;
            if (history == null)
            {
                history = new JObject();
                root["history"] = history;
            }
            JArray arr = history["timelineEntries"] as JArray;
            if (arr == null)
            {
                arr = new JArray();
            }
            bool contains = false;
            foreach (var token in arr)
            {
                if (token != null && string.Equals(token.ToString(), eventId, StringComparison.Ordinal))
                {
                    contains = true;
                    break;
                }
            }
            if (!contains)
            {
                arr.Add(eventId);
                history["timelineEntries"] = arr;
                System.IO.File.WriteAllText(filePath, root.ToString());
                Debug.Log($"[TimelineCatalog] Added event '{eventId}' to map data '{mapId}'.");
            }
            else
            {
                Debug.Log($"[TimelineCatalog] Map data '{mapId}' already contains event '{eventId}'.");
            }
        }
    }
}