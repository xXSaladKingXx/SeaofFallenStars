#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Reflection;
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
        public override void OnInspectorGUI()
        {
            var session = (TimelineCatalogAuthoringSession)target;
            if (session == null)
            {
                EditorGUILayout.HelpBox("Timeline session is null.", MessageType.Error);
                return;
            }

            // Ensure the session data exists and is initialised via reflection.  Some
            // versions of TimelineCatalogAuthoringSession may not expose
            // EnsureInitialized() publicly, so we invoke it reflectively if present.
            var ensureMethod = session.GetType().GetMethod("EnsureInitialized", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (ensureMethod != null)
            {
                ensureMethod.Invoke(session, null);
            }

            EditorGUILayout.LabelField("Timeline Catalog", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Catalog ID", session.data?.catalogId ?? string.Empty);
            }

            // File path field.  Use reflection to read and write the loadedFilePath property or field
            string loadedPath = string.Empty;
            // Attempt to get a field called loadedFilePath
            var pathField = session.GetType().GetField("loadedFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pathField != null)
            {
                var value = pathField.GetValue(session);
                if (value is string s) loadedPath = s;
            }
            else
            {
                // Attempt to get a property called loadedFilePath
                var pathProp = session.GetType().GetProperty("loadedFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pathProp != null)
                {
                    var value = pathProp.GetValue(session);
                    if (value is string s) loadedPath = s;
                }
            }
            var newLoadedPath = EditorGUILayout.TextField("Loaded File Path", loadedPath);
            // Write back if changed
            if (newLoadedPath != loadedPath)
            {
                if (pathField != null)
                {
                    pathField.SetValue(session, newLoadedPath);
                }
                else
                {
                    var pathProp = session.GetType().GetProperty("loadedFilePath", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pathProp != null && pathProp.CanWrite)
                    {
                        pathProp.SetValue(session, newLoadedPath);
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
                Undo.RecordObject(session, "Load Timeline");
                var method = session.GetType().GetMethod("LoadFromFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(session, null);
                }
                EditorUtility.SetDirty(session);
            }
            if (GUILayout.Button("Save Timeline"))
            {
                Undo.RecordObject(session, "Save Timeline");
                var method = session.GetType().GetMethod("SaveToFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    // Save without synchronising participants (parameter false)
                    method.Invoke(session, new object[] { false });
                }
                EditorUtility.SetDirty(session);
            }
            if (GUILayout.Button("Save + Sync Participants"))
            {
                Undo.RecordObject(session, "Save and Sync Timeline");
                var method = session.GetType().GetMethod("SaveToFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(session, new object[] { true });
                }
                EditorUtility.SetDirty(session);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Controls to add entries at start or end
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Entry At Start"))
            {
                Undo.RecordObject(session, "Add Timeline Entry At Start");
                var method = session.GetType().GetMethod("AddBeforeFirst", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(session, null);
                }
                EditorUtility.SetDirty(session);
            }
            if (GUILayout.Button("Add Entry At End"))
            {
                Undo.RecordObject(session, "Add Timeline Entry At End");
                var method = session.GetType().GetMethod("AddAfterLast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(session, null);
                }
                EditorUtility.SetDirty(session);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Sort events in chronological order via reflection.  Not all
            // implementations of TimelineCatalogAuthoringSession may expose
            // SortEntries() publicly, so call it reflectively if available.
            var sortMethod = session.GetType().GetMethod("SortEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (sortMethod != null)
            {
                sortMethod.Invoke(session, null);
            }

            // Retrieve the events list via reflection to handle multiple data model variations
            System.Collections.IList events = null;
            object dataObj = null;
            // Attempt to obtain the 'data' field or property on the session
            var dataFieldInfo = session.GetType().GetField("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (dataFieldInfo != null)
            {
                dataObj = dataFieldInfo.GetValue(session);
            }
            else
            {
                var dataPropInfo = session.GetType().GetProperty("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (dataPropInfo != null)
                {
                    dataObj = dataPropInfo.GetValue(session);
                }
            }
            if (dataObj != null)
            {
                // Try to get 'events' field or property
                var eventsFieldInfo = dataObj.GetType().GetField("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (eventsFieldInfo != null)
                {
                    var listObj = eventsFieldInfo.GetValue(dataObj);
                    if (listObj is System.Collections.IList list)
                    {
                        events = list;
                    }
                }
                else
                {
                    var eventsPropInfo = dataObj.GetType().GetProperty("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (eventsPropInfo != null)
                    {
                        var listObj = eventsPropInfo.GetValue(dataObj);
                        if (listObj is System.Collections.IList list)
                        {
                            events = list;
                        }
                    }
                }
                // Fallback to 'entries' field or property if 'events' not found
                if (events == null)
                {
                    var entriesFieldInfo = dataObj.GetType().GetField("entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (entriesFieldInfo != null)
                    {
                        var listObj = entriesFieldInfo.GetValue(dataObj);
                        if (listObj is System.Collections.IList list)
                        {
                            events = list;
                        }
                    }
                    else
                    {
                        var entriesPropInfo = dataObj.GetType().GetProperty("entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (entriesPropInfo != null)
                        {
                            var listObj = entriesPropInfo.GetValue(dataObj);
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
                for (int i = 0; i < events.Count; i++)
                {
                    var evt = events[i];
                    if (evt == null)
                    {
                        continue;
                    }
                    // Button to insert before this event
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Insert Before", GUILayout.Width(120)))
                    {
                        Undo.RecordObject(session, "Insert Timeline Entry");
                        // Use reflection to call InsertBetween(previous, next) if available
                        var method = session.GetType().GetMethod("InsertBetween", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            object prev = (i > 0 ? events[i - 1] : null);
                            object nextArg = evt;
                            method.Invoke(session, new object[] { prev, nextArg });
                        }
                        EditorUtility.SetDirty(session);
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
                        evt.eventType = (EventType)EditorGUILayout.EnumPopup("Event Type", evt.eventType);
                        evt.customTypeName = EditorGUILayout.TextField("Custom Type Name", evt.customTypeName);
                        evt.settingId = EditorGUILayout.TextField("Setting ID", evt.settingId);
                        // Participants
                        EditorGUILayout.LabelField("Participants", EditorStyles.boldLabel);
                        if (evt.participantIds != null)
                        {
                            for (int j = 0; j < evt.participantIds.Count; j++)
                            {
                                evt.participantIds[j] = EditorGUILayout.TextField($"Participant {j + 1}", evt.participantIds[j]);
                            }
                            if (GUILayout.Button("Add Participant"))
                            {
                                evt.participantIds.Add(string.Empty);
                            }
                        }
                        else
                        {
                            evt.participantIds = new System.Collections.Generic.List<string>();
                            if (GUILayout.Button("Add Participant"))
                            {
                                evt.participantIds.Add(string.Empty);
                            }
                        }
                        // Delete button
                        EditorGUILayout.Space();
                        if (GUILayout.Button("Delete Event"))
                        {
                            Undo.RecordObject(session, "Delete Timeline Event");
                            events.RemoveAt(i);
                            EditorUtility.SetDirty(session);
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
                            Undo.RecordObject(session, "Insert Timeline Entry");
                            // Use reflection to call InsertBetween(previous, next)
                            var method = session.GetType().GetMethod("InsertBetween", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (method != null)
                            {
                                method.Invoke(session, new object[] { evt, null });
                            }
                            EditorUtility.SetDirty(session);
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
                        Undo.RecordObject(session, "Create First Timeline Entry");
                        var method = session.GetType().GetMethod("AddAfterLast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            method.Invoke(session, null);
                        }
                        EditorUtility.SetDirty(session);
                    }
                }
            }
        }
    }
}
#endif