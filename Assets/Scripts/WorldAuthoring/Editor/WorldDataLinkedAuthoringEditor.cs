#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Zana.WorldAuthoring;

[CustomEditor(typeof(WorldDataLinkedAuthoring))]
public sealed class WorldDataLinkedAuthoringEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var linked = (WorldDataLinkedAuthoring)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Helper (Dropdown)", EditorStyles.boldLabel);

        var entries = WorldDataChoicesCache.Get(linked.Category);

        if (entries != null && entries.Count > 0)
        {
            var ids = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                ids[i] = entries[i].id;

            var labels = WorldDataChoicesCache.ToDisplayArray(entries, includeId: true);

            int current = Array.IndexOf(ids, linked.Id);
            int next = EditorGUILayout.Popup("Linked ID", Mathf.Max(0, current), labels);

            if (next >= 0 && next < ids.Length)
            {
                var chosen = ids[next];
                if (!string.Equals(chosen, linked.Id, StringComparison.Ordinal))
                {
                    Undo.RecordObject(linked, "Set Linked ID");
                    linked.SetLink(linked.Category, chosen);
                    EditorUtility.SetDirty(linked);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No JSON files were found for this category. Ensure your SaveData folders contain JSON files.",
                MessageType.Info);
        }

        var path = linked.ResolveEditorFilePath();
        EditorGUILayout.LabelField("Resolved File", string.IsNullOrWhiteSpace(path) ? "(none)" : path);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Open In Master"))
        {
            OpenInMaster(linked);
        }
        if (GUILayout.Button("Save Now"))
        {
            SaveNow(linked);
        }
        EditorGUILayout.EndHorizontal();
    }

    private static WorldDataMasterAuthoring FindMaster()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<WorldDataMasterAuthoring>();
#else
        return UnityEngine.Object.FindObjectOfType<WorldDataMasterAuthoring>();
#endif
    }

    private static void OpenInMaster(WorldDataLinkedAuthoring linked)
    {
        var master = FindMaster();
        if (master == null)
        {
            Debug.LogWarning("WorldDataMasterAuthoring not found in scene.");
            return;
        }

        var cat = linked.Category;
        var filePath = linked.ResolveEditorFilePath();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogWarning("Linked authoring has no resolved file path (missing id).");
            return;
        }

        master.CreateOrReplaceSession(cat);
        var session = master.ActiveSession;
        if (session == null) return;

        if (File.Exists(filePath))
        {
            session.TryLoadFromFile(filePath);
        }
        else
        {
            if (!linked.CreateIfMissing)
            {
                Debug.LogWarning("File does not exist and Create If Missing is disabled.");
                return;
            }

            session.SetLoadedFilePath(filePath);
            session.SaveNow();
            session.TryLoadFromFile(filePath);
            WorldDataChoicesCache.Invalidate();
        }

        Selection.activeObject = master;
        EditorGUIUtility.PingObject(master);
    }

    private static void SaveNow(WorldDataLinkedAuthoring linked)
    {
        var master = FindMaster();
        if (master == null)
        {
            Debug.LogWarning("WorldDataMasterAuthoring not found in scene.");
            return;
        }

        var cat = linked.Category;
        var filePath = linked.ResolveEditorFilePath();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogWarning("Linked authoring has no resolved file path (missing id).");
            return;
        }

        master.CreateOrReplaceSession(cat);
        var session = master.ActiveSession;
        if (session == null) return;

        if (File.Exists(filePath))
        {
            session.TryLoadFromFile(filePath);
        }
        else
        {
            if (!linked.CreateIfMissing)
            {
                Debug.LogWarning("File does not exist and Create If Missing is disabled.");
                return;
            }

            session.SetLoadedFilePath(filePath);
        }

        session.SaveNow();
        WorldDataChoicesCache.Invalidate();
    }
}
#endif
