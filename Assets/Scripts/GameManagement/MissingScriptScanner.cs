#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MissingScriptScanner
{
    [MenuItem("Tools/Missing Scripts/Scan Project")]
    public static void ScanProject()
    {
        int count = 0;

        // Scan all prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var all = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t == null) continue;
                var components = t.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        Debug.LogError($"[MissingScript] Prefab: {path}  Object: {GetTransformPath(t)}", prefab);
                        count++;
                    }
                }
            }
        }

        Debug.Log($"[MissingScriptScanner] Done. Missing components found: {count}");
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "<null>";
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }
}
#endif
