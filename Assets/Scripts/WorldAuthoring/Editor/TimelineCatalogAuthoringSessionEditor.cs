#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Custom inspector for <see cref="TimelineCatalogAuthoringSession"/>.  This implementation
    /// is a simplified UI for editing timeline events defined in the
    /// <see cref="TimelineCatalogDataModel"/>.  It replaces the upstream
    /// timeline editor which relies on legacy timeline data classes that no
    /// longer exist in this fork.  The inspector allows basic viewing and
    /// editing of events, insertion of new events before or after existing
    /// ones, and saving/loading of the catalog.  Event ordering is
    /// normalised via <see cref="TimelineCatalogAuthoringSession.SortEntries"/>.
    /// </summary>
    [CustomEditor(typeof(TimelineCatalogAuthoringSession))]
    public sealed class TimelineCatalogAuthoringSessionEditor : Editor
    {
        // Predefined default participant categories, directionality and side split for each built‑in event type.
        // When a new event is created or its type changes, these definitions are used to
        // suggest a sensible number of participants, their ordering into sides and the
        // directional semantics.  The third element of each tuple indicates the number
        // of participants on the first side (side A).  A split of 0 means there is only
        // one group (no sides).  Authors can remove or add participants manually after
        // these defaults are applied.  For events with more participants than the
        // default, the split indicates the minimum initial side size; additional
        // participants will be added to the end of the list and belong to the second side.
        private static readonly Dictionary<EventType, (ParticipantCategory[] categories, EventDirection direction, int split)> DefaultEventDefinitions = new()
        {
            { EventType.Birth, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.OneWay, 2) },
            { EventType.Death, (new[]{ ParticipantCategory.Character }, EventDirection.None, 0) },
            { EventType.Coronation, (new[]{ ParticipantCategory.Character, ParticipantCategory.Settlement }, EventDirection.OneWay, 1) },
            { EventType.DeclareWar, (new[]{ ParticipantCategory.Settlement, ParticipantCategory.Settlement }, EventDirection.TwoWay, 1) },
            { EventType.Betrothal, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.TwoWay, 1) },
            { EventType.Marriage, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.TwoWay, 1) },
            { EventType.Kill, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.OneWay, 1) },
            { EventType.Coitus, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.TwoWay, 1) },
            { EventType.MakeAlliance, (new[]{ ParticipantCategory.Settlement, ParticipantCategory.Settlement }, EventDirection.TwoWay, 1) },
            { EventType.MakePeace, (new[]{ ParticipantCategory.Settlement, ParticipantCategory.Settlement }, EventDirection.TwoWay, 1) },
            // For battle events we cannot predict the number of participants so we default to two settlements.
            // The split indicates one participant per side; authors can add more participants manually.
            { EventType.Battle, (new[]{ ParticipantCategory.Settlement, ParticipantCategory.Settlement }, EventDirection.TwoWay, 1) },
            { EventType.BeganTraveling, (new[]{ ParticipantCategory.Character }, EventDirection.None, 0) },
            { EventType.Visited, (new[]{ ParticipantCategory.Character, ParticipantCategory.Settlement }, EventDirection.OneWay, 1) },
            { EventType.EndTraveling, (new[]{ ParticipantCategory.Character }, EventDirection.None, 0) },
            { EventType.StrippedOfTitle, (new[]{ ParticipantCategory.Character, ParticipantCategory.Settlement }, EventDirection.OneWay, 1) },
            { EventType.Knighted, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.OneWay, 1) },
            { EventType.AwardedLand, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.OneWay, 1) },
            { EventType.LevelUp, (new[]{ ParticipantCategory.Character }, EventDirection.OneWay, 0) },
            { EventType.Befriend, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.TwoWay, 1) },
            { EventType.StartRivalry, (new[]{ ParticipantCategory.Character, ParticipantCategory.Character }, EventDirection.TwoWay, 1) }
        };
        public override void OnInspectorGUI()
        {
            // Treat the target as a UnityEngine.Object to avoid invalid casts when the
            // timeline session type does not derive from MonoBehaviour in the editor assembly.
            UnityEngine.Object sessionObj = target;
            if (sessionObj == null)
            {
                EditorGUILayout.HelpBox("Timeline session is null.", MessageType.Error);
                return;
            }
            var sessionType = sessionObj.GetType();

            // Ensure the session data exists and is initialised.  Invoke EnsureInitialized() via reflection if present.
            var ensureMethod = sessionType.GetMethod("EnsureInitialized", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (ensureMethod != null)
            {
                ensureMethod.Invoke(sessionObj, null);
            }

            EditorGUILayout.LabelField("Timeline Catalog", EditorStyles.boldLabel);
            // Display Catalog ID from the data model via reflection
            using (new EditorGUI.DisabledScope(true))
            {
                string catalogId = string.Empty;
                // Obtain data object for the catalog.  Use a unique name to avoid
                // shadowing the timelineDataObj variable defined later.
                object catalogDataObj = null;
                var dataField = sessionType.GetField("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (dataField != null)
                {
                    catalogDataObj = dataField.GetValue(sessionObj);
                }
                else
                {
                    var dataProp = sessionType.GetProperty("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (dataProp != null)
                    {
                        catalogDataObj = dataProp.GetValue(sessionObj);
                    }
                }
                if (catalogDataObj != null)
                {
                    var catField = catalogDataObj.GetType().GetField("catalogId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (catField != null)
                    {
                        var idVal = catField.GetValue(catalogDataObj) as string;
                        if (!string.IsNullOrEmpty(idVal)) catalogId = idVal;
                    }
                    else
                    {
                        var catProp = catalogDataObj.GetType().GetProperty("catalogId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (catProp != null)
                        {
                            var idVal = catProp.GetValue(catalogDataObj) as string;
                            if (!string.IsNullOrEmpty(idVal)) catalogId = idVal;
                        }
                    }
                }
                EditorGUILayout.TextField("Catalog ID", catalogId ?? string.Empty);
            }

            // File path field.  Use reflection to read and write the timelineFilePath property or field
            string loadedPath = string.Empty;
            // Attempt to get a field called timelineFilePath
            var pathField = sessionType.GetField("timelineFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pathField != null)
            {
                var value = pathField.GetValue(sessionObj);
                if (value is string s) loadedPath = s;
            }
            else
            {
                // Attempt to get a property called timelineFilePath
                var pathProp = sessionType.GetProperty("timelineFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pathProp != null)
                {
                    var value = pathProp.GetValue(sessionObj);
                    if (value is string s) loadedPath = s;
                }
            }
            var newLoadedPath = EditorGUILayout.TextField("Loaded File Path", loadedPath);
            // Write back if changed
            if (newLoadedPath != loadedPath)
            {
                if (pathField != null)
                {
                    pathField.SetValue(sessionObj, newLoadedPath);
                }
                else
                {
                    var pathPropSet = sessionType.GetProperty("timelineFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pathPropSet != null && pathPropSet.CanWrite)
                    {
                        pathPropSet.SetValue(sessionObj, newLoadedPath);
                    }
                }
            }

            EditorGUILayout.Space();

            // Load/save controls using reflection.  Some implementations may not expose
            // LoadFromFile or SaveToFile directly, so we use reflection to invoke
            // these methods if present.
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load From File"))
            {
                Undo.RecordObject(sessionObj, "Load Timeline");
                var method = sessionType.GetMethod("LoadFromFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(sessionObj, null);
                }
                EditorUtility.SetDirty(sessionObj);
            }
            if (GUILayout.Button("Save Timeline"))
            {
                Undo.RecordObject(sessionObj, "Save Timeline");
                var method = sessionType.GetMethod("SaveToFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    // Save without synchronising participants (parameter false)
                    method.Invoke(sessionObj, new object[] { false });
                }
                EditorUtility.SetDirty(sessionObj);
            }
            if (GUILayout.Button("Save + Sync Participants"))
            {
                Undo.RecordObject(sessionObj, "Save and Sync Timeline");
                var method = sessionType.GetMethod("SaveToFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(sessionObj, new object[] { true });
                }
                EditorUtility.SetDirty(sessionObj);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Controls to add entries at start or end
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Entry At Start"))
            {
                Undo.RecordObject(sessionObj, "Add Timeline Entry At Start");
                var method = sessionType.GetMethod("AddBeforeFirst", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(sessionObj, null);
                }
                EditorUtility.SetDirty(sessionObj);
            }
            if (GUILayout.Button("Add Entry At End"))
            {
                Undo.RecordObject(sessionObj, "Add Timeline Entry At End");
                var method = sessionType.GetMethod("AddAfterLast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(sessionObj, null);
                }
                EditorUtility.SetDirty(sessionObj);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Sort events in chronological order via reflection.  Not all
            // implementations of TimelineCatalogAuthoringSession may expose
            // SortEntries() publicly, so call it reflectively if available.
            var sortMethod = sessionType.GetMethod("SortEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (sortMethod != null)
            {
                sortMethod.Invoke(sessionObj, null);
            }

            // Retrieve the events list via reflection to handle multiple data model variations
            System.Collections.IList events = null;
            // Use a unique variable name for the timeline's data to avoid shadowing
            object timelineDataObj = null;
            // Attempt to obtain the 'data' field or property on the session
            var dataFieldInfo = sessionType.GetField("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (dataFieldInfo != null)
            {
                timelineDataObj = dataFieldInfo.GetValue(sessionObj);
            }
            else
            {
                var dataPropInfo = sessionType.GetProperty("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (dataPropInfo != null)
                {
                    timelineDataObj = dataPropInfo.GetValue(sessionObj);
                }
            }
            if (timelineDataObj != null)
            {
                // Try to get 'events' field or property
                var eventsFieldInfo = timelineDataObj.GetType().GetField("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (eventsFieldInfo != null)
                {
                    var listObj = eventsFieldInfo.GetValue(timelineDataObj);
                    if (listObj is System.Collections.IList list)
                    {
                        events = list;
                    }
                }
                else
                {
                    var eventsPropInfo = timelineDataObj.GetType().GetProperty("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (eventsPropInfo != null)
                    {
                        var listObj = eventsPropInfo.GetValue(timelineDataObj);
                        if (listObj is System.Collections.IList list)
                        {
                            events = list;
                        }
                    }
                }
                // Fallback to 'entries' field or property if 'events' not found
                if (events == null)
                {
                    var entriesFieldInfo = timelineDataObj.GetType().GetField("entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (entriesFieldInfo != null)
                    {
                        var listObj = entriesFieldInfo.GetValue(timelineDataObj);
                        if (listObj is System.Collections.IList list)
                        {
                            events = list;
                        }
                    }
                    else
                    {
                        var entriesPropInfo = timelineDataObj.GetType().GetProperty("entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (entriesPropInfo != null)
                        {
                            var listObj = entriesPropInfo.GetValue(timelineDataObj);
                            if (listObj is System.Collections.IList list)
                            {
                                events = list;
                            }
                        }
                    }
                }
            }
            if (events != null)
            {
                // Build options for settings and participants.  We compute these lists
                // once per inspector update to avoid repeated file IO within the event loop.
                var mapIds = new List<string>();
                var mapNames = new List<string>();
                try
                {
                    string mapDir = Zana.WorldAuthoring.WorldDataDirectoryResolver.GetEditorMapDataDir();
                    if (!string.IsNullOrEmpty(mapDir) && Directory.Exists(mapDir))
                    {
                        foreach (var file in Directory.GetFiles(mapDir, "*.json"))
                        {
                            string id = Path.GetFileNameWithoutExtension(file);
                            string name = id;
                            try
                            {
                                var text = File.ReadAllText(file);
                                var j = JObject.Parse(text);
                                // Try displayName, name or settlementName
                                var displayNameToken = j.SelectToken("displayName") ?? j.SelectToken("name") ?? j.SelectToken("settlementName") ?? j.SelectToken("title");
                                if (displayNameToken != null && displayNameToken.Type == JTokenType.String)
                                {
                                    name = displayNameToken.ToString();
                                }
                            }
                            catch { /* ignore parsing errors */ }
                            mapIds.Add(id);
                            // Show both id and name if they differ
                            mapNames.Add(id == name ? id : $"{name} ({id})");
                        }
                    }
                }
                catch { /* ignore errors building map list */ }

                var charIds = new List<string>();
                var charNames = new List<string>();
                try
                {
                    string charDir = Zana.WorldAuthoring.WorldDataDirectoryResolver.GetEditorCharactersDir();
                    if (!string.IsNullOrEmpty(charDir) && Directory.Exists(charDir))
                    {
                        foreach (var file in Directory.GetFiles(charDir, "*.json"))
                        {
                            string id = Path.GetFileNameWithoutExtension(file);
                            string name = id;
                            try
                            {
                                var text = File.ReadAllText(file);
                                var j = JObject.Parse(text);
                                var displayNameToken = j.SelectToken("displayName") ?? j.SelectToken("name") ?? j.SelectToken("characterName") ?? j.SelectToken("title");
                                if (displayNameToken != null && displayNameToken.Type == JTokenType.String)
                                {
                                    name = displayNameToken.ToString();
                                }
                            }
                            catch { /* ignore parsing errors */ }
                            charIds.Add(id);
                            charNames.Add(id == name ? id : $"{name} ({id})");
                        }
                    }
                }
                catch { /* ignore errors building character list */ }

                // Prebuild arrays for category choices
                var categoryLabels = Enum.GetNames(typeof(ParticipantCategory));

                for (int i = 0; i < events.Count; i++)
                {
                    var evtObj = events[i];
                    if (evtObj == null)
                    {
                        continue;
                    }
                    // Cast the event to the strongly typed model.  If the cast fails
                    // skip drawing this entry to avoid reflection overhead.
                    var evt = evtObj as TimelineEventModel;
                    if (evt == null)
                    {
                        continue;
                    }
                    // Button to insert before this event
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Insert Before", GUILayout.Width(120)))
                    {
                        Undo.RecordObject(sessionObj, "Insert Timeline Entry");
                        // Use reflection to call InsertBetween(previous, next) if available
                        var method = sessionType.GetMethod("InsertBetween", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            object prev = (i > 0 ? events[i - 1] : null);
                            // Pass the strongly typed event as the next argument when invoking
                            method.Invoke(sessionObj, new object[] { prev, evt });
                        }
                        EditorUtility.SetDirty(sessionObj);
                        // Early exit so the UI refreshes with new event
                        return;
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"Event {i + 1}", EditorStyles.boldLabel);
                        // Date fields
                        evt.year = EditorGUILayout.IntField("Year", evt.year);
                        int newMonth = EditorGUILayout.IntField("Month", evt.month ?? 1);
                        evt.month = Mathf.Clamp(newMonth, 1, 12);
                        int newDay = EditorGUILayout.IntField("Day", evt.day ?? 1);
                        evt.day = Mathf.Clamp(newDay, 1, 31);
                        // Event metadata
                        evt.eventName = EditorGUILayout.TextField("Event Name", evt.eventName);
                        evt.description = EditorGUILayout.TextField("Description", evt.description);
                        evt.eventSignificance = (EventSignificance)EditorGUILayout.EnumPopup("Significance", evt.eventSignificance);
                        // Event Type and defaults
                        EventType beforeType = evt.eventType;
                        evt.eventType = (EventType)EditorGUILayout.EnumPopup("Event Type", evt.eventType);
                        // When the event type changes or there are no participants, apply defaults.  In addition,
                        // if there are fewer participants than the minimum specified for this type, top up the lists
                        // with blanks and corresponding categories.  This ensures that events always include at
                        // least the recommended number of participants without forcibly removing user‑added entries.
                        if ((evt.participantIds == null || evt.participantIds.Count == 0) || evt.eventType != beforeType)
                        {
                            if (DefaultEventDefinitions.TryGetValue(evt.eventType, out var def))
                            {
                                // Ensure lists exist
                                if (evt.participantIds == null)
                                    evt.participantIds = new List<string>();
                                if (evt.participantCategories == null)
                                    evt.participantCategories = new List<ParticipantCategory>();
                                // When the type changed, discard existing participants exceeding the default length
                                if (evt.eventType != beforeType && evt.participantIds.Count > def.categories.Length)
                                {
                                    evt.participantIds = evt.participantIds.GetRange(0, def.categories.Length);
                                    evt.participantCategories = new List<ParticipantCategory>(def.categories);
                                }
                                // Append blanks until the participant count meets the default minimum
                                for (int p = evt.participantIds.Count; p < def.categories.Length; p++)
                                {
                                    evt.participantIds.Add(string.Empty);
                                    evt.participantCategories.Add(def.categories[p]);
                                }
                                // Update directionality and split
                                evt.eventDirection = def.direction;
                                evt.participantSplitIndex = def.split;
                                // Clamp split to valid range
                                if (evt.participantSplitIndex < 0) evt.participantSplitIndex = 0;
                                if (evt.participantSplitIndex > evt.participantIds.Count) evt.participantSplitIndex = evt.participantIds.Count;
                            }
                            else
                            {
                                // Other or custom events: ensure lists exist but do not prepopulate
                                if (evt.participantIds == null)
                                    evt.participantIds = new List<string>();
                                if (evt.participantCategories == null)
                                    evt.participantCategories = new List<ParticipantCategory>();
                                evt.eventDirection = EventDirection.None;
                                evt.participantSplitIndex = 0;
                            }
                        }
                        evt.customTypeName = EditorGUILayout.TextField("Custom Type Name", evt.customTypeName);
                        // Setting selection
                        int currentSettingIndex = mapIds.IndexOf(evt.settingId);
                        int newSettingIndex = EditorGUILayout.Popup("Setting", currentSettingIndex, mapNames.ToArray());
                        if (newSettingIndex >= 0 && newSettingIndex < mapIds.Count)
                        {
                            evt.settingId = mapIds[newSettingIndex];
                        }
                        // Participants and sides
                        // Ensure participant lists exist
                        if (evt.participantIds == null) evt.participantIds = new List<string>();
                        if (evt.participantCategories == null) evt.participantCategories = new List<ParticipantCategory>();
                        // Clamp split index to valid range
                        if (evt.participantSplitIndex < 0) evt.participantSplitIndex = 0;
                        if (evt.participantSplitIndex > evt.participantIds.Count) evt.participantSplitIndex = evt.participantIds.Count;
                        // Ensure categories list matches ids list
                        while (evt.participantCategories.Count < evt.participantIds.Count)
                        {
                            evt.participantCategories.Add(ParticipantCategory.Character);
                        }
                        while (evt.participantCategories.Count > evt.participantIds.Count)
                        {
                            evt.participantCategories.RemoveAt(evt.participantCategories.Count - 1);
                        }
                        // Determine if we need to display sides.  Only directional events (OneWay or TwoWay)
                        // use sides.  Events with None direction treat all participants as a single group.
                        bool useSides = evt.eventDirection != EventDirection.None;
                        if (useSides)
                        {
                            // Display Side A participants
                            EditorGUILayout.LabelField("Side A Participants", EditorStyles.boldLabel);
                            int index = 0;
                            while (index < evt.participantSplitIndex)
                            {
                                DrawParticipantRow(evt, index, charIds, charNames, mapIds, mapNames, categoryLabels);
                                // If a removal occurred inside DrawParticipantRow, the split index must adjust
                                // accordingly.  DrawParticipantRow will signal removal by setting a flag.
                                if (_participantRemoved)
                                {
                                    _participantRemoved = false;
                                    // Adjust split index if removal was from side A
                                    evt.participantSplitIndex = Math.Max(0, evt.participantSplitIndex - 1);
                                    // Continue loop without incrementing index because the list has shifted
                                    continue;
                                }
                                index++;
                            }
                            // Add button for Side A
                            if (GUILayout.Button("Add to Side A"))
                            {
                                evt.participantIds.Insert(evt.participantSplitIndex, string.Empty);
                                evt.participantCategories.Insert(evt.participantSplitIndex, ParticipantCategory.Character);
                                evt.participantSplitIndex++;
                            }
                            EditorGUILayout.Space();
                            // Display Side B participants
                            EditorGUILayout.LabelField("Side B Participants", EditorStyles.boldLabel);
                            index = evt.participantSplitIndex;
                            while (index < evt.participantIds.Count)
                            {
                                DrawParticipantRow(evt, index, charIds, charNames, mapIds, mapNames, categoryLabels);
                                if (_participantRemoved)
                                {
                                    _participantRemoved = false;
                                    // Removal from side B does not affect split index
                                    continue;
                                }
                                index++;
                            }
                            // Add button for Side B
                            if (GUILayout.Button("Add to Side B"))
                            {
                                evt.participantIds.Add(string.Empty);
                                evt.participantCategories.Add(ParticipantCategory.Character);
                            }
                        }
                        else
                        {
                            // Non‑directional events: single participant list
                            EditorGUILayout.LabelField("Participants", EditorStyles.boldLabel);
                            int index = 0;
                            while (index < evt.participantIds.Count)
                            {
                                DrawParticipantRow(evt, index, charIds, charNames, mapIds, mapNames, categoryLabels);
                                if (_participantRemoved)
                                {
                                    _participantRemoved = false;
                                    // Do not adjust split index for non‑directional events
                                    continue;
                                }
                                index++;
                            }
                            if (GUILayout.Button("Add Participant"))
                            {
                                evt.participantIds.Add(string.Empty);
                                evt.participantCategories.Add(ParticipantCategory.Character);
                            }
                        }
                        // Delete button
                        EditorGUILayout.Space();
                        if (GUILayout.Button("Delete Event"))
                        {
                            Undo.RecordObject(sessionObj, "Delete Timeline Event");
                            events.RemoveAt(i);
                            EditorUtility.SetDirty(sessionObj);
                            return;
                        }
                    }
                    // Button to insert after this event (will appear only for the last event)
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                        if (i == events.Count - 1)
                        {
                            if (GUILayout.Button("Insert After", GUILayout.Width(120)))
                            {
                                Undo.RecordObject(sessionObj, "Insert Timeline Entry");
                                // Use reflection to call InsertBetween(previous, next)
                                var method = sessionType.GetMethod("InsertBetween", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (method != null)
                                {
                                    method.Invoke(sessionObj, new object[] { evt, null });
                                }
                                EditorUtility.SetDirty(sessionObj);
                                return;
                            }
                        }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }

                if (events.Count == 0)
                {
                    EditorGUILayout.HelpBox("The timeline is empty.", MessageType.None);
                    if (GUILayout.Button("Create First Entry"))
                    {
                        Undo.RecordObject(sessionObj, "Create First Timeline Entry");
                        var method = sessionType.GetMethod("AddAfterLast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            method.Invoke(sessionObj, null);
                        }
                        EditorUtility.SetDirty(sessionObj);
                    }
                }
            }
        }
        // Flag used internally to signal when a participant row has been removed during drawing.
        // Because the participants list can be modified while iterating, we cannot simply
        // break out of the loop or modify the index in place without losing sync.  The
        // DrawParticipantRow method sets this flag when it removes a participant so that
        // the calling code can adjust indices accordingly.
        private static bool _participantRemoved;

        /// <summary>
        /// Draw a single participant row with editable category, identifier and removal button.
        /// This helper encapsulates the common UI for participants across both sides and
        /// non‑directional events.  When the remove button is pressed, the participant
        /// is removed from both the ids and categories lists, and the global
        /// <see cref="_participantRemoved"/> flag is set to true.  The caller must
        /// handle index adjustments when this flag is observed.
        /// </summary>
        private static void DrawParticipantRow(TimelineEventModel evt, int index,
            List<string> charIds, List<string> charNames,
            List<string> mapIds, List<string> mapNames,
            string[] categoryLabels)
        {
            _participantRemoved = false;
            if (evt == null) return;
            // Ensure lists are valid
            if (evt.participantIds == null) evt.participantIds = new List<string>();
            if (evt.participantCategories == null) evt.participantCategories = new List<ParticipantCategory>();
            while (evt.participantCategories.Count < evt.participantIds.Count)
            {
                evt.participantCategories.Add(ParticipantCategory.Character);
            }
            while (evt.participantCategories.Count > evt.participantIds.Count)
            {
                evt.participantCategories.RemoveAt(evt.participantCategories.Count - 1);
            }
            if (index < 0 || index >= evt.participantIds.Count) return;
            EditorGUILayout.BeginHorizontal();
            // Unique label for each row to avoid control id conflicts
            int catIndex = (int)evt.participantCategories[index];
            string label = $"Type {index + 1}";
            int newCatIndex = EditorGUILayout.Popup(label, catIndex, categoryLabels);
            if (newCatIndex != catIndex)
            {
                evt.participantCategories[index] = (ParticipantCategory)newCatIndex;
                // Reset id when type changes
                evt.participantIds[index] = string.Empty;
            }
            // Build dropdown options based on category
            List<string> idList = null;
            List<string> nameList = null;
            switch (evt.participantCategories[index])
            {
                case ParticipantCategory.Character:
                    idList = charIds;
                    nameList = charNames;
                    break;
                case ParticipantCategory.Settlement:
                case ParticipantCategory.Unpopulated:
                    idList = mapIds;
                    nameList = mapNames;
                    break;
                case ParticipantCategory.Army:
                case ParticipantCategory.TravelGroup:
                    // Currently unsupported; allow manual id entry
                    idList = null;
                    nameList = null;
                    break;
            }
            if (idList != null && nameList != null && idList.Count > 0)
            {
                int currentIndex = idList.IndexOf(evt.participantIds[index]);
                if (currentIndex < 0) currentIndex = 0;
                int newIndex = EditorGUILayout.Popup(currentIndex, nameList.ToArray());
                if (newIndex >= 0 && newIndex < idList.Count)
                {
                    evt.participantIds[index] = idList[newIndex];
                }
            }
            else
            {
                evt.participantIds[index] = EditorGUILayout.TextField(evt.participantIds[index] ?? string.Empty);
            }
            if (GUILayout.Button("✕", GUILayout.Width(20)))
            {
                // Remove this participant
                evt.participantIds.RemoveAt(index);
                evt.participantCategories.RemoveAt(index);
                _participantRemoved = true;
                EditorGUILayout.EndHorizontal();
                return;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif