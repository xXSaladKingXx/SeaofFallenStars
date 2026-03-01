#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Custom inspector for <see cref="RuinsAuthoringSession"/>.  Allows editing
    /// of ruin identifiers, display names, descriptions and related
    /// timeline events.  Timeline events are selected by identifier from
    /// the existing TimelineCatalog via dropdown; when no timeline catalog
    /// is available free text editing is used instead.
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

            // Fetch timeline event definitions.  The WorldDataChoicesCache does not
            // currently expose timeline definitions directly; however, timeline
            // events are stored inside the TimelineCatalog.  We attempt to
            // flatten them by scanning loaded timeline sessions via the
            // TimelineCatalogAuthoringSession.  If nothing is loaded the
            // dropdown will fall back to free text.
            string[] eventIds = System.Array.Empty<string>();
            string[] eventLabels = System.Array.Empty<string>();
            try
            {
                // Attempt to locate an existing timeline catalog authoring session
                var timelineSessions = Object.FindObjectsOfType<TimelineCatalogAuthoringSession>();
                if (timelineSessions != null && timelineSessions.Length > 0)
                {
                    var tl = timelineSessions[0].data;
                    if (tl != null && tl.events != null)
                    {
                        var list = tl.events;
                        eventIds = list.Select(e => e.eventId).Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
                        eventLabels = list.Select(e => !string.IsNullOrWhiteSpace(e.eventName) ? e.eventName : e.eventId).ToArray();
                    }
                }
            }
            catch { /* ignore errors */ }

            // Timeline events list
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