using System;

/// <summary>
/// Represents a single entry in a settlement's timeline or history.
/// Holds arbitrary descriptive text and returns it when converted to string.
/// </summary>
[Serializable]
public class TimelineEntry
{
    /// <summary>
    /// Textual description of this timeline entry.
    /// </summary>
    public string entry;

    /// <summary>
    /// Returns the underlying entry text so that lists of timeline entries
    /// can be joined or printed without additional formatting.
    /// </summary>
    public override string ToString()
    {
        return entry;
    }

    /// <summary>
    /// Allows implicit conversion of a timeline entry to a string.  This is useful for
    /// array and list join operations in UI code where TimelineEntry objects are
    /// expected to behave like strings.  If the entry itself is null, this returns
    /// an empty string rather than throwing.
    /// </summary>
    public static implicit operator string(TimelineEntry t) => t != null ? t.entry : "";
}
