#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [CustomEditor(typeof(TimelineCatalogAuthoringSession))]
    public sealed class TimelineCatalogAuthoringSessionEditor : Editor
    {
        private static readonly string[] MonthOptions =
        {
            "(unspecified)",
            "01", "02", "03", "04", "05", "06",
            "07", "08", "09", "10", "11", "12"
        };

        private static readonly string[] DayOptions =
        {
            "(unspecified)",
            "01", "02", "03", "04", "05", "06", "07", "08", "09", "10",
            "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
            "21", "22", "23", "24", "25", "26", "27", "28", "29", "30"
        };

        private TimelineCatalogAuthoringSession Session => (TimelineCatalogAuthoringSession)target;

        public override void OnInspectorGUI()
        {
            DrawSessionInspector(Session);
        }

        internal static void DrawSessionInspector(TimelineCatalogAuthoringSession session)
        {
            if (session == null)
            {
                EditorGUILayout.HelpBox("Timeline session is null.", MessageType.Error);
                return;
            }

            session.EnsureInitialized();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Timeline Catalog", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Catalog ID", session.data.catalogId);
            }

            session.loadedFilePath = EditorGUILayout.TextField("Loaded File Path", session.loadedFilePath);

            EditorGUILayout.HelpBox(
                "This is the single permanent main timeline catalog. The package does not migrate any legacy history entries. " +
                "Entries auto-sort by normalized date. Year-only dates sort as YYYY-01-01. Year+month dates sort as YYYY-MM-01.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load From File"))
            {
                session.LoadFromFile();
            }

            if (GUILayout.Button("Save Timeline"))
            {
                session.SaveToFile(syncParticipantEventIds: false);
            }

            if (GUILayout.Button("Save + Sync Participants"))
            {
                session.SaveToFile(syncParticipantEventIds: true);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Entry At Start"))
            {
                RecordMutation(session, "Add Timeline Entry At Start");
                session.AddBeforeFirst();
            }

            if (GUILayout.Button("Audit + Sync Participant Event IDs"))
            {
                TimelineParticipantEventIdSynchronizer.AuditAndSync(session.data);
            }

            EditorGUILayout.EndHorizontal();

            session.SortEntries();

            for (var i = 0; i < session.data.entries.Count; i++)
            {
                if (i == 0)
                {
                    DrawInsertButton(session, previous: null, next: session.data.entries[i], label: "Insert Before First");
                }

                var entry = session.data.entries[i];
                DrawEntry(session, entry, i);

                if (i < session.data.entries.Count - 1)
                {
                    DrawInsertButton(session, session.data.entries[i], session.data.entries[i + 1], "Insert Between");
                }
                else
                {
                    DrawInsertButton(session, session.data.entries[i], next: null, label: "Insert After Last");
                }
            }

            if (session.data.entries.Count == 0)
            {
                EditorGUILayout.HelpBox("The timeline is empty.", MessageType.None);

                if (GUILayout.Button("Create First Entry"))
                {
                    RecordMutation(session, "Create First Timeline Entry");
                    session.AddAfterLast();
                }
            }
        }

        private static void DrawInsertButton(TimelineCatalogAuthoringSession session, TimelineEventEntry previous, TimelineEventEntry next, string label)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(label, GUILayout.Width(180)))
            {
                RecordMutation(session, label);
                session.InsertBetween(previous, next);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private static void DrawEntry(TimelineCatalogAuthoringSession session, TimelineEventEntry entry, int index)
        {
            entry.date ??= new TimelineEventDate();
            entry.setting ??= new TimelineEventSetting();
            entry.participants ??= new List<TimelineEventParticipant>();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Entry {index + 1}: {GetHeaderLabel(session.data, entry)}", EditorStyles.boldLabel);

                if (GUILayout.Button("Apply Type Defaults", GUILayout.Width(140)))
                {
                    RecordMutation(session, "Apply Timeline Type Defaults");
                    TimelineEventTypeRegistry.ApplyDefaults(session.data, entry, overwriteExistingParticipants: true, setEventNameWhenEmpty: false);
                }

                if (string.Equals(entry.eventTypeId, TimelineBuiltInEventTypes.Other, StringComparison.OrdinalIgnoreCase))
                {
                    if (GUILayout.Button("Save Other As Type", GUILayout.Width(150)))
                    {
                        RecordMutation(session, "Save Other Timeline Type");
                        if (TimelineEventTypeRegistry.TryCreateOrUpdateCustomTypeFromOtherEntry(session.data, entry, out var message))
                        {
                            Debug.Log($"[Timeline] {message}");
                        }
                        else
                        {
                            Debug.LogWarning($"[Timeline] {message}");
                        }
                    }
                }

                if (GUILayout.Button("Delete", GUILayout.Width(70)))
                {
                    RecordMutation(session, "Delete Timeline Entry");
                    session.data.entries.RemoveAt(index);
                    session.SortEntries();
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                DrawDate(session, entry);
                DrawEventType(session, entry);

                entry.directionality = (TimelineEventDirectionality)EditorGUILayout.EnumPopup("Directionality", entry.directionality);
                entry.significance = (TimelineEventSignificance)EditorGUILayout.EnumPopup("Significance", entry.significance);
                entry.eventName = EditorGUILayout.TextField("Event Name", entry.eventName);

                EditorGUILayout.LabelField("Event Description");
                entry.description = EditorGUILayout.TextArea(entry.description, GUILayout.MinHeight(50f));

                entry.uniqueIconPathOrGuid = EditorGUILayout.TextField("Unique Icon Path / GUID", entry.uniqueIconPathOrGuid);

                DrawSetting(session, entry);
                DrawParticipants(session, entry);

                if (string.Equals(entry.eventTypeId, TimelineBuiltInEventTypes.Other, StringComparison.OrdinalIgnoreCase))
                {
                    EditorGUILayout.HelpBox(
                        "For Other entries, 'Save Other As Type' uses the current Event Name as the new custom type name and stores the " +
                        "current participant kinds, participant sides, and directionality as the reusable template. Selected participant IDs are not copied into the template.",
                        MessageType.None);
                }

                var definition = TimelineEventTypeRegistry.GetDefinition(session.data, entry.eventTypeId);
                if (string.IsNullOrWhiteSpace(entry.uniqueIconPathOrGuid))
                {
                    EditorGUILayout.HelpBox(
                        $"No unique icon set. This entry will fall back to the event type icon/default: {definition.defaultIconPathOrGuid}",
                        MessageType.None);
                }

                if (definition.allowedSettingKinds != null &&
                    definition.allowedSettingKinds.Count > 0 &&
                    !definition.allowedSettingKinds.Contains(entry.setting.kind))
                {
                    EditorGUILayout.HelpBox(
                        $"The current setting kind '{entry.setting.kind}' is outside the default allowed setting kinds for event type '{definition.displayName}'. " +
                        "This is not blocked, but it is outside the template defaults.",
                        MessageType.Warning);
                }

                if (!string.IsNullOrWhiteSpace(entry.id))
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField("Entry ID", entry.id);
                    }
                }
            }

            EditorGUILayout.Space(6f);
        }

        private static void DrawDate(TimelineCatalogAuthoringSession session, TimelineEventEntry entry)
        {
            EditorGUILayout.LabelField("Date", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            entry.date.year = EditorGUILayout.TextField("Year", entry.date.year);

            var month = Mathf.Clamp(entry.date.month, 0, 12);
            var monthChosen = EditorGUILayout.Popup("Month", month, MonthOptions);
            if (monthChosen != month)
            {
                RecordMutation(session, "Change Timeline Month");
                entry.date.month = monthChosen;
                if (entry.date.month == 0)
                {
                    entry.date.day = 0;
                }
            }

            using (new EditorGUI.DisabledScope(entry.date.month == 0))
            {
                var day = Mathf.Clamp(entry.date.day, 0, 30);
                var dayChosen = EditorGUILayout.Popup("Day", day, DayOptions);
                if (dayChosen != day)
                {
                    RecordMutation(session, "Change Timeline Day");
                    entry.date.day = dayChosen;
                }
            }

            EditorGUILayout.EndHorizontal();

            entry.date.Normalize();

            if (!int.TryParse(entry.date.year, out _))
            {
                EditorGUILayout.HelpBox("Year is free-text, but sorting only works numerically when it parses as an integer.", MessageType.Warning);
            }

            EditorGUILayout.LabelField("Normalized Sort Date", entry.date.ToString());
        }

        private static void DrawEventType(TimelineCatalogAuthoringSession session, TimelineEventEntry entry)
        {
            var definitions = TimelineEventTypeRegistry.GetAllDefinitions(session.data);
            var labels = new string[definitions.Count];
            var currentIndex = 0;

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                labels[i] = definition.displayName;

                if (string.Equals(definition.id, entry.eventTypeId, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                }
            }

            var chosen = EditorGUILayout.Popup("Event Type", currentIndex, labels);
            if (chosen < 0 || chosen >= definitions.Count)
            {
                return;
            }

            var selectedDefinition = definitions[chosen];
            if (string.Equals(selectedDefinition.id, entry.eventTypeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RecordMutation(session, "Change Timeline Event Type");
            entry.eventTypeId = selectedDefinition.id;
            TimelineEventTypeRegistry.ApplyDefaults(session.data, entry, overwriteExistingParticipants: entry.participants.Count == 0, setEventNameWhenEmpty: true);
        }

        private static void DrawSetting(TimelineCatalogAuthoringSession session, TimelineEventEntry entry)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Setting", EditorStyles.boldLabel);

            entry.setting.kind = (TimelineSettingKind)EditorGUILayout.EnumPopup("Setting Kind", entry.setting.kind);

            var options = TimelineReferenceLookup.GetSettingOptions(entry.setting.kind);
            if (options.Length == 0)
            {
                if (entry.setting.kind == TimelineSettingKind.Other)
                {
                    entry.setting.id = EditorGUILayout.TextField("Setting ID", entry.setting.id);
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Popup("Setting", 0, new[] { "(no entries available)" });
                    }

                    entry.setting.id = string.Empty;
                }

                return;
            }

            var labels = BuildLabelsWithNone(options);
            var ids = BuildIdsWithNone(options);
            var currentIndex = Array.IndexOf(ids, entry.setting.id);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            var chosen = EditorGUILayout.Popup("Setting", currentIndex, labels);
            if (chosen >= 0 && chosen < ids.Length)
            {
                entry.setting.id = ids[chosen];
            }
        }

        private static void DrawParticipants(TimelineCatalogAuthoringSession session, TimelineEventEntry entry)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Participants", EditorStyles.boldLabel);

            for (var i = 0; i < entry.participants.Count; i++)
            {
                var participant = entry.participants[i] ??= new TimelineEventParticipant();

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    participant.role = EditorGUILayout.TextField("Role", participant.role);
                    participant.side = (TimelineParticipantSide)EditorGUILayout.EnumPopup("Direction Side", participant.side);
                    participant.kind = (TimelineParticipantKind)EditorGUILayout.EnumPopup("Kind", participant.kind);

                    var options = TimelineReferenceLookup.GetOptions(participant.kind);
                    if (options.Length == 0 || participant.kind == TimelineParticipantKind.Other)
                    {
                        participant.id = EditorGUILayout.TextField("ID", participant.id);
                    }
                    else
                    {
                        var labels = BuildLabelsWithNone(options);
                        var ids = BuildIdsWithNone(options);
                        var currentIndex = Array.IndexOf(ids, participant.id);
                        if (currentIndex < 0)
                        {
                            currentIndex = 0;
                        }

                        var chosen = EditorGUILayout.Popup("Reference", currentIndex, labels);
                        if (chosen >= 0 && chosen < ids.Length)
                        {
                            participant.id = ids[chosen];
                        }
                    }

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Remove Participant", GUILayout.Width(150)))
                    {
                        RecordMutation(session, "Remove Timeline Participant");
                        entry.participants.RemoveAt(i);
                        i--;
                    }

                    if (GUILayout.Button("Duplicate", GUILayout.Width(90)))
                    {
                        RecordMutation(session, "Duplicate Timeline Participant");
                        entry.participants.Insert(i + 1, new TimelineEventParticipant
                        {
                            role = participant.role,
                            kind = participant.kind,
                            id = participant.id,
                            side = participant.side
                        });
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("Add Participant"))
            {
                RecordMutation(session, "Add Timeline Participant");
                entry.participants.Add(new TimelineEventParticipant());
            }
        }

        private static string[] BuildLabelsWithNone(TimelineReferenceLookup.Option[] options)
        {
            var labels = new string[options.Length + 1];
            labels[0] = "(None)";

            for (var i = 0; i < options.Length; i++)
            {
                labels[i + 1] = options[i].Label;
            }

            return labels;
        }

        private static string[] BuildIdsWithNone(TimelineReferenceLookup.Option[] options)
        {
            var ids = new string[options.Length + 1];
            ids[0] = string.Empty;

            for (var i = 0; i < options.Length; i++)
            {
                ids[i + 1] = options[i].Id;
            }

            return ids;
        }

        private static string GetHeaderLabel(TimelineCatalogData data, TimelineEventEntry entry)
        {
            var date = entry.date?.ToString() ?? "Unspecified date";
            var definition = TimelineEventTypeRegistry.GetDefinition(data, entry.eventTypeId);
            var name = string.IsNullOrWhiteSpace(entry.eventName) ? $"({definition.displayName})" : entry.eventName;
            return $"{date} — {name}";
        }

        private static void RecordMutation(TimelineCatalogAuthoringSession session, string label)
        {
            Undo.RecordObject(session, label);
            EditorUtility.SetDirty(session);
        }
    }
}
#endif
