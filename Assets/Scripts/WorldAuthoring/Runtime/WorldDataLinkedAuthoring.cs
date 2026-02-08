using System;
using System.IO;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Optional helper: attach to any GameObject to link it to a single JSON file (by category + id).
    /// Useful for non-MapPoint objects (e.g., an Army or Character GameObject in-scene).
    /// </summary>
    public sealed class WorldDataLinkedAuthoring : MonoBehaviour
    {
        [Header("Link")]
        [SerializeField] private WorldDataCategory category = WorldDataCategory.Character;
        [SerializeField] private string id;

        [Header("Behavior")]
        [Tooltip("If enabled, the editor buttons will create the JSON file when it doesn't exist.")]
        [SerializeField] private bool createIfMissing = true;

        public WorldDataCategory Category => category;
        public string Id => id;
        public bool CreateIfMissing => createIfMissing;

        public string ResolveEditorFilePath()
        {
            var dir = WorldDataDirectoryResolver.GetEditorDir(category);
            if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(id)) return null;
            return Path.Combine(dir, id + ".json");
        }

        public void SetLink(WorldDataCategory cat, string newId)
        {
            category = cat;
            id = newId;
        }
    }
}
