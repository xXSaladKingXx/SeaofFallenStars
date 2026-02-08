// A Unity Editor tool to detect and resolve duplicate class definitions.
//
// Place this script in an `Editor` folder within your Assets directory. It
// provides a menu item under `Tools > Duplicate Class Resolver` that opens
// a window where duplicate class definitions (same namespace and class
// name appearing in multiple files) are listed. For each conflict, you
// can choose which file to keep and optionally delete the others.
//
// WARNING: Deleting files through this tool is irreversible. Make sure
// you have backups or are using version control before removing files.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class DuplicateClassResolver : EditorWindow
{
    private Dictionary<(string ns, string name), List<string>> _duplicates;
    private Vector2 _scroll;
    private bool _scanInProgress;

    [MenuItem("Tools/Duplicate Class Resolver")]
    public static void ShowWindow()
    {
        var window = GetWindow<DuplicateClassResolver>();
        window.titleContent = new GUIContent("Duplicate Class Resolver");
        window.Refresh();
    }

    private void OnGUI()
    {
        GUILayout.Label("Duplicate Class Resolver", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh"))
        {
            Refresh();
        }
        if (_scanInProgress)
        {
            GUILayout.Label("Scanning project... please wait.");
            return;
        }
        if (_duplicates == null)
        {
            GUILayout.Label("Click 'Refresh' to scan for duplicates.");
            return;
        }
        if (_duplicates.Count == 0)
        {
            GUILayout.Label("No duplicate class definitions found.");
            return;
        }
        GUILayout.Label($"Found {_duplicates.Count} conflicting class definitions:");
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var kvp in _duplicates)
        {
            var key = kvp.Key;
            var files = kvp.Value;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Class: {key.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Namespace: {key.ns ?? "(global)"}");
            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i + 1}] {path}", GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Keep", GUILayout.MaxWidth(60)))
                {
                    OnKeepFileSelected(key, i);
                    // Break to prevent modifying collection during enumeration
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    private void Refresh()
    {
        _scanInProgress = true;
        _duplicates = null;
        // Delay scanning until next frame to keep UI responsive.
        EditorApplication.delayCall += () =>
        {
            try
            {
                _duplicates = FindDuplicateClasses("Assets/Scripts");
            }
            finally
            {
                _scanInProgress = false;
                Repaint();
            }
        };
    }

    private void OnKeepFileSelected((string ns, string name) key, int indexToKeep)
    {
        var files = _duplicates[key];
        string fileToKeep = files[indexToKeep];
        bool confirm = EditorUtility.DisplayDialog(
            "Resolve Duplicate",
            $"You chose to keep:\n{fileToKeep}\n\nThe other {files.Count - 1} file(s) will be deleted.\nThis action cannot be undone. Are you sure?",
            "Yes, delete others",
            "Cancel");
        if (!confirm)
        {
            return;
        }
        for (int i = 0; i < files.Count; i++)
        {
            if (i != indexToKeep)
            {
                string assetPath = files[i];
                if (AssetDatabase.DeleteAsset(assetPath))
                {
                    Debug.Log($"Deleted duplicate asset: {assetPath}");
                }
                else
                {
                    Debug.LogError($"Failed to delete asset: {assetPath}");
                }
            }
        }
        // Re-scan after deletion
        Refresh();
    }

    private static Dictionary<(string ns, string name), List<string>> FindDuplicateClasses(string rootDir)
    {
        var index = new Dictionary<(string, string), List<string>>();
        if (!Directory.Exists(rootDir))
        {
            Debug.LogWarning($"Directory not found: {rootDir}");
            return new Dictionary<(string, string), List<string>>();
        }
        string[] files = Directory.GetFiles(rootDir, "*.cs", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            try
            {
                string content = File.ReadAllText(file);
                // Extract namespace (first occurrence)
                Match nsMatch = Regex.Match(content, @"namespace\s+([\w\.]+)");
                string ns = nsMatch.Success ? nsMatch.Groups[1].Value : null;
                // Extract class names (simple regex)
                MatchCollection classMatches = Regex.Matches(content, @"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)");
                foreach (Match m in classMatches)
                {
                    string className = m.Groups[1].Value;
                    var key = (ns, className);
                    if (!index.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        index[key] = list;
                    }
                    list.Add(file);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to read {file}: {ex.Message}");
            }
        }
        // Filter to duplicates only
        var duplicates = new Dictionary<(string, string), List<string>>();
        foreach (var kvp in index)
        {
            if (kvp.Value.Count > 1)
            {
                duplicates[kvp.Key] = kvp.Value;
            }
        }
        return duplicates;
    }
}