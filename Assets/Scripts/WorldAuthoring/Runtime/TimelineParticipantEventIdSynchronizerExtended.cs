using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Utility class that ensures timeline event identifiers are synchronised
    /// across the various participant data models.  When saving the timeline
    /// catalogue, call <see cref="Synchronize"/> to iterate through every
    /// event and append its ID to the appropriate participant JSON files.
    /// Characters store their event references in the top‑level
    /// <c>timelineEntries</c> array, while settlements and unpopulated map
    /// points store references under <c>history.timelineEntries</c>.
    /// This extended version is used instead of the built‑in synchronizer to
    /// avoid duplicate class definitions across different runtime contexts.
    /// </summary>
    public static class TimelineParticipantEventIdSynchronizerExtended
    {
        /// <summary>
        /// Audits all events in the provided catalogue and ensures the
        /// participants' timeline entries are up to date.  This method is
        /// provided for backwards compatibility with authoring scripts that
        /// call AuditAndSync on the synchronizer.  Internally it
        /// delegates to <see cref="Synchronize(TimelineCatalogDataModel)"/>.
        /// </summary>
        /// <param name="catalog">The timeline catalogue containing events and participants.</param>
        public static void AuditAndSync(TimelineCatalogDataModel catalog)
        {
            Synchronize(catalog);
        }

        /// <summary>
        /// Audits and synchronises a list of events rather than an entire
        /// catalogue.  Wraps the provided events into a temporary catalogue
        /// instance and synchronises participants.  Maintained for
        /// backwards compatibility with legacy calls.
        /// </summary>
        /// <param name="events">A list of timeline events to synchronise.</param>
        public static void AuditAndSync(List<TimelineEventModel> events)
        {
            if (events == null)
                return;
            var temp = new TimelineCatalogDataModel();
            temp.events = events;
            Synchronize(temp);
        }

        /// <summary>
        /// Audits all events in the provided runtime timeline catalogue and ensures
        /// the participants' timeline entries are up to date.  This overload
        /// accepts the legacy <see cref="TimelineCatalogData"/> type used by
        /// the built‑in timeline authoring session.  It iterates through
        /// each <see cref="TimelineEventEntry"/> in the catalogue and adds
        /// the entry's identifier to the appropriate participant JSON files.
        /// Characters store their event references in the top‑level
        /// <c>timelineEntries</c> array, while settlements and unpopulated map
        /// points store references under <c>history.timelineEntries</c>.
        /// Participants whose kind is Army, TravelGroup or Other cannot
        /// currently record timeline entries and will be logged for
        /// completeness.  Missing participant JSON files will generate
        /// warnings but do not interrupt the audit process.
        /// </summary>
        /// <param name="catalog">The runtime timeline catalogue containing entries and participants.</param>
        public static void AuditAndSync(TimelineCatalogData catalog)
        {
            if (catalog == null || catalog.entries == null)
                return;
            foreach (var entry in catalog.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;
                // Skip entries with no participants
                if (entry.participants == null)
                    continue;
                foreach (var participant in entry.participants)
                {
                    if (participant == null || string.IsNullOrWhiteSpace(participant.id))
                        continue;
                    try
                    {
                        var category = GetParticipantCategory(participant.id);
                        switch (category)
                        {
                            case ParticipantCategory.Character:
                                UpdateCharacterTimeline(participant.id, entry.id);
                                break;
                            case ParticipantCategory.Settlement:
                            case ParticipantCategory.Unpopulated:
                                UpdateMapTimeline(participant.id, entry.id);
                                break;
                            default:
                                // Currently, armies, travel groups and other types do not support timeline
                                // participation tracking.  Log for completeness.
                                Debug.Log($"[TimelineSynchronizer] Participant '{participant.id}' of category '{category}' cannot record timeline entries.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[TimelineSynchronizer] Failed to synchronise event '{entry.id}' for participant '{participant.id}'. {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Legacy entry point maintained for backwards compatibility.  Redirects
        /// to <see cref="Synchronize(TimelineCatalogDataModel)"/> when called with
        /// a catalogue.  If a list of events is provided instead, the
        /// catalogue reference may be null and the method will operate on the
        /// provided events list.
        /// </summary>
        /// <param name="catalog">Optional timeline catalogue containing events.</param>
        /// <param name="events">Optional list of events to synchronise.  Used when the caller
        /// passes just the event list rather than the full catalogue.</param>
        public static void SynchronizeParticipantEventIds(TimelineCatalogDataModel catalog = null, List<TimelineEventModel> events = null)
        {
            // Prefer the passed catalogue if available; otherwise build a temporary
            // catalogue wrapper around the provided events list.
            if (catalog != null)
            {
                Synchronize(catalog);
            }
            else if (events != null)
            {
                var temp = new TimelineCatalogDataModel();
                temp.events = events;
                Synchronize(temp);
            }
        }

        /// <summary>
        /// Iterate over all events in the provided catalogue and add each
        /// event's identifier to the timeline entries of every listed
        /// participant.  Missing participants will result in a warning
        /// message but do not stop execution.
        /// </summary>
        /// <param name="catalog">The timeline catalogue containing events and participants.</param>
        public static void Synchronize(TimelineCatalogDataModel catalog)
        {
            if (catalog?.events == null)
                return;
            foreach (var evt in catalog.events)
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
                                // Currently, armies and travel groups do not support timeline
                                // participation tracking.  Log for completeness.
                                Debug.Log($"[TimelineSynchronizer] Participant '{pid}' of category '{category}' cannot record timeline entries.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[TimelineSynchronizer] Failed to synchronise event '{evt.id}' for participant '{pid}'. {ex.Message}");
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
        private static ParticipantCategory GetParticipantCategory(string participantId)
        {
            try
            {
                // Check characters directory
                string charDir = WorldDataDirectoryResolver.GetEditorCharactersDir();
                if (!string.IsNullOrEmpty(charDir))
                {
                    string charPath = Path.Combine(charDir, participantId + ".json");
                    if (File.Exists(charPath))
                    {
                        return ParticipantCategory.Character;
                    }
                }

                // Check map data directory (settlements and unpopulated)
                string mapDir = WorldDataDirectoryResolver.GetEditorMapDataDir();
                if (!string.IsNullOrEmpty(mapDir))
                {
                    string mapPath = Path.Combine(mapDir, participantId + ".json");
                    if (File.Exists(mapPath))
                    {
                        // Inspect root JSON to decide settlement vs unpopulated
                        var text = File.ReadAllText(mapPath);
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
        private static void UpdateCharacterTimeline(string characterId, string eventId)
        {
            string dir = WorldDataDirectoryResolver.GetEditorCharactersDir();
            if (string.IsNullOrEmpty(dir)) return;
            string filePath = Path.Combine(dir, characterId + ".json");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[TimelineSynchronizer] Character file '{characterId}.json' not found when adding event '{eventId}'.");
                return;
            }
            string json = File.ReadAllText(filePath);
            var root = JObject.Parse(json);
            JArray arr = root["timelineEntries"] as JArray;
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
                root["timelineEntries"] = arr;
                File.WriteAllText(filePath, root.ToString());
                Debug.Log($"[TimelineSynchronizer] Added event '{eventId}' to character '{characterId}'.");
            }
            else
            {
                Debug.Log($"[TimelineSynchronizer] Character '{characterId}' already contains event '{eventId}'.");
            }
        }

        /// <summary>
        /// Add the given event id to a map data (settlement or unpopulated) JSON file.
        /// The timeline entries are stored under history.timelineEntries.  If the
        /// file or history section is missing, it will be created.  Logs
        /// descriptive messages on update or if the event is already present.
        /// </summary>
        private static void UpdateMapTimeline(string mapId, string eventId)
        {
            string dir = WorldDataDirectoryResolver.GetEditorMapDataDir();
            if (string.IsNullOrEmpty(dir)) return;
            string filePath = Path.Combine(dir, mapId + ".json");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[TimelineSynchronizer] Map data file '{mapId}.json' not found when adding event '{eventId}'.");
                return;
            }
            string json = File.ReadAllText(filePath);
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
                File.WriteAllText(filePath, root.ToString());
                Debug.Log($"[TimelineSynchronizer] Added event '{eventId}' to map data '{mapId}'.");
            }
            else
            {
                Debug.Log($"[TimelineSynchronizer] Map data '{mapId}' already contains event '{eventId}'.");
            }
        }
    }
}