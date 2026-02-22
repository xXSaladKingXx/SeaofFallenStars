using System;

/// <summary>
/// Simple index entry used by editor tooling to list and reference JSON files.
/// Each entry captures the category, ID, display name and file path of a single
/// world-data object. See WorldDataChoicesCache and WorldDataSessionEditors for usage.
/// </summary>
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