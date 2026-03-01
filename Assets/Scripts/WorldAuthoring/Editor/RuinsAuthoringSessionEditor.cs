#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Custom inspector for <see cref="RuinsAuthoringSession"/>.  Allows editing
    /// of ruin identifiers, display names, descriptions and related
    /// timeline events.  Timeline events are selected by identifier from
    /// the existing TimelineCatalog via dropdown; when no timeline catalog
    /// is available free text editing is used instead.  This implementation
    /// relies on the <see cref="TimelineCatalogDataModel"/> and
    /// <see cref="TimelineEventModel"/> types introduced in the custom timeline
    /// system.  It queries loaded timeline sessions for available event ids
    /// and labels via the <see cref="TimelineCatalogAuthoringSession"/>.
    /// </summary>
    [CustomEditor(typeof(RuinsAuthoringSession))]
    internal sealed class RuinsAuthoringSessionEditor : Editor
    {
        private int _addEventSelection;

        public override void OnInspectorGUI()
        {
            var session = (RuinsAuthoringSession)target;
            if (session == null || session.data == null)
            {
                base.OnInspectorGUI();
                return;
            }
            serializedObject.Update();

            EditorGUILayout.LabelField("Ruin", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            session.data.ruinId = EditorGUILayout.TextField("Ruin ID", session.data.ruinId);
            session.data.displayName = EditorGUILayout.TextField("Display Name", session.data.displayName);
            EditorGUILayout.LabelField("Description");
            session.data.description = EditorGUILayout.TextArea(session.data.description ?? string.Empty, GUILayout.MinHeight(50));
            EditorGUI.indentLevel--;

            // Fetch timeline event definitions.  We scan loaded timeline
            // authoring sessions and flatten out their event identifiers using reflection.
            string[] eventIds = System.Array.Empty<string>();
            string[] eventLabels = System.Array.Empty<string>();
            try
            {
                // Use non‑generic FindObjectsOfType to avoid generic type constraints on TimelineCatalogAuthoringSession
                var timelineObjects = UnityEngine.Object.FindObjectsOfType(typeof(TimelineCatalogAuthoringSession));
                if (timelineObjects != null && timelineObjects.Length > 0)
                {
                    // Use the first loaded timeline session found in the scene
                    UnityEngine.Object sessionObj = timelineObjects[0];
                    if (sessionObj != null)
                    {
                        var sessionType = sessionObj.GetType();
                        // Attempt to get the 'data' field or property via reflection
                        object dataObj = null;
                        var dataField = sessionType.GetField("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (dataField != null)
                        {
                            dataObj = dataField.GetValue(sessionObj);
                        }
                        else
                        {
                            var dataProp = sessionType.GetProperty("data", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (dataProp != null)
                            {
                                dataObj = dataProp.GetValue(sessionObj);
                            }
                        }
                        if (dataObj != null)
                        {
                            // Try to get 'events' or 'entries' list via reflection
                            System.Collections.IList eventsList = null;
                            var eventsField = dataObj.GetType().GetField("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (eventsField != null)
                            {
                                var listObj = eventsField.GetValue(dataObj);
                                eventsList = listObj as System.Collections.IList;
                            }
                            if (eventsList == null)
                            {
                                var eventsProp = dataObj.GetType().GetProperty("events", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (eventsProp != null)
                                {
                                    var listObj = eventsProp.GetValue(dataObj);
                                    eventsList = listObj as System.Collections.IList;
                                }
                            }
                            if (eventsList == null)
                            {
                                var entriesField = dataObj.GetType().GetField("entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (entriesField != null)
                                {
                                    var listObj = entriesField.GetValue(dataObj);
                                    eventsList = listObj as System.Collections.IList;
                                }
                                else
                                {
                                    var entriesProp = dataObj.GetType().GetProperty("entries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (entriesProp != null)
                                    {
                                        var listObj = entriesProp.GetValue(dataObj);
                                        eventsList = listObj as System.Collections.IList;
                                    }
                                }
                            }
                            if (eventsList != null)
                            {
                                // Build arrays of event ids and labels
                                var ids = new System.Collections.Generic.List<string>();
                                var labels = new System.Collections.Generic.List<string>();
                                foreach (var ev in eventsList)
                                {
                                    if (ev == null) continue;
                                    string id = null;
                                    // Try to get eventId alias or id
                                    var idProp = ev.GetType().GetProperty("eventId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    if (idProp != null)
                                    {
                                        id = idProp.GetValue(ev) as string;
                                    }
                                    if (string.IsNullOrWhiteSpace(id))
                                    {
                                        var idField = ev.GetType().GetField("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                        if (idField != null)
                                        {
                                            id = idField.GetValue(ev) as string;
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(id))
                                    {
                                        ids.Add(id);
                                        // Try to get event name
                                        string name = null;
                                        var nameProp = ev.GetType().GetProperty("eventName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                        if (nameProp != null)
                                        {
                                            name = nameProp.GetValue(ev) as string;
                                        }
                                        if (string.IsNullOrWhiteSpace(name))
                                        {
                                            name = id;
                                        }
                                        labels.Add(name);
                                    }
                                }
                                eventIds = ids.ToArray();
                                eventLabels = labels.ToArray();
                            }
                        }
                    }
                }
            }
            catch { /* ignore errors */ }

            // Timeline events list for this ruin
            session.data.timelineEventIds ??= new System.Collections.Generic.List<string>();
            EditorGUILayout.LabelField("Timeline Events", EditorStyles.miniBoldLabel);
            if (eventIds.Length > 0)
            {
                for (int i = 0; i < session.data.timelineEventIds.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string current = session.data.timelineEventIds[i] ?? string.Empty;
                        int curIndex = System.Array.IndexOf(eventIds, current);
                        if (curIndex < 0) curIndex = 0;
                        int next = EditorGUILayout.Popup(curIndex, eventLabels);
                        if (next >= 0 && next < eventIds.Length)
                            session.data.timelineEventIds[i] = eventIds[next];
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            session.data.timelineEventIds.RemoveAt(i);
                            i--;
                        }
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _addEventSelection = EditorGUILayout.Popup("Add", Mathf.Clamp(_addEventSelection, 0, eventLabels.Length - 1), eventLabels);
                    if (GUILayout.Button("Add", GUILayout.Width(70)))
                    {
                        string toAdd = eventIds[Mathf.Clamp(_addEventSelection, 0, eventIds.Length - 1)];
                        if (!string.IsNullOrEmpty(toAdd) && !session.data.timelineEventIds.Contains(toAdd))
                            session.data.timelineEventIds.Add(toAdd);
                    }
                }
            }
            else
            {
                // Fallback free‑text editing if no timeline definitions are loaded
                for (int i = 0; i < session.data.timelineEventIds.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        session.data.timelineEventIds[i] = EditorGUILayout.TextField(session.data.timelineEventIds[i] ?? string.Empty);
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            session.data.timelineEventIds.RemoveAt(i);
                            i--;
                        }
                    }
                }
                if (GUILayout.Button("Add Event", GUILayout.Width(120)))
                    session.data.timelineEventIds.Add(string.Empty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif