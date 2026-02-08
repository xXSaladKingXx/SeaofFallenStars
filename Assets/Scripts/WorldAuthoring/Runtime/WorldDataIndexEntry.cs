using System;

namespace Zana.WorldAuthoring
{
    [Serializable]
    public sealed class WorldDataIndexEntry
    {
        public WorldDataCategory category;
        public string id;
        public string displayName;
        public string filePath;

        public override string ToString()
        {
            string n = string.IsNullOrWhiteSpace(displayName) ? "(no name)" : displayName;
            string i = string.IsNullOrWhiteSpace(id) ? "(no id)" : id;
            return $"{n} [{i}]";
        }
    }
}
