#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RemoveMissingScriptsFromSelection
{
    [MenuItem("Tools/Prefabs/Remove Missing Scripts From Selection")]
    public static void RemoveMissing()
    {
        var objs = Selection.gameObjects;
        if (objs == null || objs.Length == 0)
        {
            Debug.LogWarning("[RemoveMissingScripts] Select a prefab instance in the scene OR open the prefab and select its root.");
            return;
        }

        int removedTotal = 0;

        foreach (var root in objs)
        {
            if (root == null) continue;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t == null) continue;

                // RemoveMissingMonoBehaviours returns how many it removed on that object.
                removedTotal += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }

        Debug.Log($"[RemoveMissingScripts] Removed missing scripts: {removedTotal}");
    }
}
#endif
