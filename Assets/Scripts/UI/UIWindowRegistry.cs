using System;
using System.Collections.Generic;
using UnityEngine;

public static class UIWindowRegistry
{
    private static readonly Dictionary<string, GameObject> _open = new Dictionary<string, GameObject>();

    public static void Register(string key, GameObject go)
    {
        if (string.IsNullOrWhiteSpace(key) || go == null)
            return;

        _open[key] = go;
    }

    public static bool TryFocus(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_open.TryGetValue(key, out var go) && go != null)
        {
            go.transform.SetAsLastSibling();
            return true;
        }

        if (_open.ContainsKey(key))
            _open.Remove(key);

        return false;
    }

    public static GameObject Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (_open.TryGetValue(key, out var go) && go != null)
            return go;

        if (_open.ContainsKey(key))
            _open.Remove(key);

        return null;
    }

    public static GameObject OpenOrFocus(string key, Func<GameObject> spawn)
    {
        if (TryFocus(key))
            return Get(key);

        if (spawn == null)
            return null;

        var go = spawn();
        if (go == null)
            return null;

        Register(key, go);

        var hook = go.GetComponent<UIWindowRegistryHook>();
        if (hook == null) hook = go.AddComponent<UIWindowRegistryHook>();
        hook.Initialize(key);

        return go;
    }

    public static bool Close(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_open.TryGetValue(key, out var go) && go != null)
        {
            UnityEngine.Object.Destroy(go);
            _open.Remove(key);
            return true;
        }

        if (_open.ContainsKey(key))
            _open.Remove(key);

        return false;
    }

    public static void Unregister(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (_open.ContainsKey(key))
            _open.Remove(key);
    }

    private class UIWindowRegistryHook : MonoBehaviour
    {
        private string _key;

        public void Initialize(string key) => _key = key;

        private void OnDestroy()
        {
            if (!string.IsNullOrWhiteSpace(_key))
                UIWindowRegistry.Unregister(_key);
        }
    }
}
