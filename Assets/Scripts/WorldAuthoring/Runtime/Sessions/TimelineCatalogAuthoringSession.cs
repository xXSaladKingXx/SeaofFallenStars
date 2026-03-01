using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for the timeline catalog.  This session exposes the
    /// TimelineCatalogDataModel to the Unity inspector, allowing users to add,
    /// edit and remove timeline events.  The authoring UI should provide
    /// auto‑sorting of events by date and controls for inserting new entries
    /// between existing ones.
    /// </summary>
    // Mark the class as partial so it can be split across runtime and editor
    // assemblies.  This helps avoid duplicate type definition errors when a
    // stub exists in the editor folder.  The sealed modifier is preserved.
    public sealed partial class TimelineCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Data")]
        public TimelineCatalogDataModel data = new TimelineCatalogDataModel();

        /// <summary>
        /// Path to the JSON file backing this timeline catalog.  When editing
        /// in the Unity inspector, this should point at the permanent main
        /// timeline file.  Defaults to a sensible location under
        /// Assets/SaveData/Timeline.
        /// </summary>
        [Tooltip("Keep this pointed at the one permanent timeline catalog file, for example Assets/SaveData/Timeline/main.timeline.json.")]
        public string loadedFilePath = "Assets/SaveData/Timeline/main.timeline.json";

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

        /// <summary>
        /// Ensure that the timeline data is initialised and that each event
        /// contains a valid identifier and default values.  This mirrors the
        /// behaviour of the original <c>EnsureInitialized</c> method from the
        /// upstream timeline authoring session.  It can safely be called
        /// repeatedly; redundant initialisation is avoided.
        /// </summary>
        public void EnsureInitialized()
        {
            if (data == null)
            {
                data = new TimelineCatalogDataModel();
            }
            if (data.events == null)
            {
                data.events = new List<TimelineEventModel>();
            }
            // Assign identifiers and defaults for each event
            for (int i = 0; i < data.events.Count; i++)
            {
                var evt = data.events[i];
                if (evt == null)
                {
                    data.events[i] = CreateBlankEntry();
                    continue;
                }
                // Ensure id
                if (string.IsNullOrWhiteSpace(evt.id))
                {
                    evt.id = $"timeline_{Guid.NewGuid():N}";
                }
                // Default month/day to 1 when unspecified (null)
                if (!evt.month.HasValue)
                {
                    evt.month = 1;
                }
                if (!evt.day.HasValue)
                {
                    evt.day = 1;
                }
                // Ensure participant list is not null
                if (evt.participantIds == null)
                {
                    evt.participantIds = new List<string>();
                }
            }
        }

        /// <summary>
        /// Sort the timeline entries chronologically.  Events with missing
        /// month or day values are treated as occurring on the first month
        /// or day for sorting purposes.  Null entries are ignored.
        /// </summary>
        public void SortEntries()
        {
            EnsureInitialized();
            data.events.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return -1;
                if (b == null) return 1;
                long ordA = ToOrdinal(a);
                long ordB = ToOrdinal(b);
                return ordA.CompareTo(ordB);
            });
        }

        /// <summary>
        /// Insert a blank timeline entry between two existing entries.  If
        /// either neighbour is null it will be treated as unbounded on that
        /// side and the default date will be adjusted accordingly.  The new
        /// entry is added to the catalog and returned.
        /// </summary>
        public TimelineEventModel InsertBetween(TimelineEventModel previous, TimelineEventModel next)
        {
            EnsureInitialized();
            var blank = CreateBlankEntry();
            var date = BuildDefaultDateBetween(previous, next);
            blank.year = date.year;
            blank.month = date.month;
            blank.day = date.day;
            data.events.Add(blank);
            SortEntries();
            return blank;
        }

        /// <summary>
        /// Add a new blank entry at the beginning of the timeline.  The
        /// resulting event date will precede the current first entry or
        /// default to year zero if the timeline is empty.
        /// </summary>
        public TimelineEventModel AddBeforeFirst()
        {
            EnsureInitialized();
            TimelineEventModel first = data.events.Count > 0 ? data.events[0] : null;
            return InsertBetween(null, first);
        }

        /// <summary>
        /// Add a new blank entry at the end of the timeline.  The
        /// resulting event date will follow the current last entry or
        /// default to year zero if the timeline is empty.
        /// </summary>
        public TimelineEventModel AddAfterLast()
        {
            EnsureInitialized();
            TimelineEventModel last = data.events.Count > 0 ? data.events[data.events.Count - 1] : null;
            return InsertBetween(last, null);
        }

        /// <summary>
        /// Create a new blank timeline event with sensible defaults.  All
        /// fields are initialised so the event can be safely edited in the
        /// inspector.  The new event id is unique.
        /// </summary>
        public static TimelineEventModel CreateBlankEntry()
        {
            return new TimelineEventModel
            {
                id = $"timeline_{Guid.NewGuid():N}",
                eventName = string.Empty,
                description = string.Empty,
                eventSignificance = EventSignificance.Info,
                eventType = EventType.Other,
                year = 0,
                month = 1,
                day = 1,
                settingId = null,
                participantIds = new List<string>(),
                customTypeName = null,
                icon = null
            };
        }

        /// <summary>
        /// Determine a default date between two existing events.  Dates are
        /// converted to ordinal values on a 360‑day year (12 months × 30 days).
        /// When one neighbour is null, the date will be one day before or
        /// after the other neighbour.  When both neighbours are null, the
        /// default date is year zero, month one, day one.  When the gap is
        /// too small to compute a midpoint, the earlier date is returned.
        /// </summary>
        public static TimelineEventModel BuildDefaultDateBetween(TimelineEventModel previous, TimelineEventModel next)
        {
            long prevOrdinal;
            long nextOrdinal;
            if (previous == null)
            {
                prevOrdinal = long.MinValue / 2;
            }
            else
            {
                prevOrdinal = ToOrdinal(previous);
            }
            if (next == null)
            {
                nextOrdinal = long.MaxValue / 2;
            }
            else
            {
                nextOrdinal = ToOrdinal(next);
            }
            long resultOrdinal;
            if (previous == null && next == null)
            {
                resultOrdinal = 0;
            }
            else if (previous == null)
            {
                resultOrdinal = nextOrdinal - 1;
            }
            else if (next == null)
            {
                resultOrdinal = prevOrdinal + 1;
            }
            else
            {
                if (nextOrdinal <= prevOrdinal + 1)
                {
                    resultOrdinal = prevOrdinal;
                }
                else
                {
                    resultOrdinal = prevOrdinal + ((nextOrdinal - prevOrdinal) / 2);
                }
            }
            var result = new TimelineEventModel();
            SetDateFromOrdinal(result, resultOrdinal);
            return result;
        }

        // Helper to convert a timeline event date into an ordinal number.  Month
        // and day default to 1 when unspecified.  Negative ordinals are allowed
        // if dates precede year zero.
        private static long ToOrdinal(TimelineEventModel evt)
        {
            if (evt == null) return 0;
            int y = evt.year;
            int m = evt.month.HasValue ? evt.month.Value : 1;
            int d = evt.day.HasValue ? evt.day.Value : 1;
            long ordinal = ((long)y * 360L) + ((long)(m - 1) * 30L) + (d - 1);
            return ordinal;
        }

        // Helper to assign year, month and day on a timeline event from an ordinal
        // number.  Negative ordinals will produce negative years.  Months and
        // days are guaranteed to be within their valid ranges.
        private static void SetDateFromOrdinal(TimelineEventModel evt, long ordinal)
        {
            int y = (int)(ordinal / 360L);
            int rem = (int)(ordinal % 360L);
            int m = (rem / 30) + 1;
            int d = (rem % 30) + 1;
            evt.year = y;
            evt.month = m;
            evt.day = d;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Load the timeline catalogue from the file specified by
        /// <see cref="loadedFilePath"/>.  If the file does not exist or the
        /// path is empty, a new default catalog is created instead.  After
        /// loading, the data is initialised and the editor object is marked
        /// dirty so that changes are reflected in the inspector.
        /// </summary>
        public void LoadFromFile()
        {
            if (string.IsNullOrWhiteSpace(loadedFilePath))
            {
                Debug.LogWarning("[Timeline] Loaded file path is blank. Nothing was loaded.");
                data = new TimelineCatalogDataModel();
                EnsureInitialized();
                return;
            }
            if (!System.IO.File.Exists(loadedFilePath))
            {
                Debug.Log($"[Timeline] No timeline file exists at '{loadedFilePath}'. Starting with a blank timeline.");
                data = new TimelineCatalogDataModel();
                EnsureInitialized();
                UnityEditor.EditorUtility.SetDirty(this);
                return;
            }
            try
            {
                var json = System.IO.File.ReadAllText(loadedFilePath);
                // Use Newtonsoft.Json to deserialize the data model
                var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<TimelineCatalogDataModel>(json);
                data = loaded ?? new TimelineCatalogDataModel();
                EnsureInitialized();
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"[Timeline] Loaded timeline catalog from '{loadedFilePath}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] Failed to load timeline catalog from '{loadedFilePath}'. Exception: {ex}");
                data = new TimelineCatalogDataModel();
                EnsureInitialized();
            }
        }

        /// <summary>
        /// Save the timeline catalogue to the file specified by
        /// <see cref="loadedFilePath"/>.  If <paramref name="syncParticipantEventIds"/>
        /// is true, participant event identifiers will be synchronised via
        /// <see cref="SynchronizeParticipantTimelineIds"/> after writing the file.
        /// When the directory does not exist it will be created.  The editor
        /// asset database is refreshed and the session marked dirty upon
        /// completion.
        /// </summary>
        /// <param name="syncParticipantEventIds">Whether to synchronise
        /// participant event references after saving.</param>
        public void SaveToFile(bool syncParticipantEventIds)
        {
            EnsureInitialized();
            // Always sort before saving
            SortEntries();
            if (string.IsNullOrWhiteSpace(loadedFilePath))
            {
                Debug.LogWarning("[Timeline] Loaded file path is blank. Nothing was saved.");
                return;
            }
            try
            {
                var directory = System.IO.Path.GetDirectoryName(loadedFilePath);
                if (!string.IsNullOrWhiteSpace(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                // Build JSON (this will promote custom types and synchronise participants if needed)
                if (syncParticipantEventIds)
                {
                    // When synchronisation is requested, run both the custom type conversion and participant sync
                    SaveCustomTypesAndConvertEvents();
                    SynchronizeParticipantTimelineIds();
                }
                else
                {
                    // Only promote custom types, do not synchronise participants
                    SaveCustomTypesAndConvertEvents();
                }
                // Serialize the data model to pretty JSON using Newtonsoft.Json
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(loadedFilePath, json);
                // Refresh asset database and mark dirty
                UnityEditor.AssetDatabase.Refresh();
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log(syncParticipantEventIds
                    ? $"[Timeline] Saved timeline catalog to '{loadedFilePath}' and synchronised participant event references."
                    : $"[Timeline] Saved timeline catalog to '{loadedFilePath}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] Failed to save timeline catalog to '{loadedFilePath}'. Exception: {ex}");
            }
        }
#endif
    }
}