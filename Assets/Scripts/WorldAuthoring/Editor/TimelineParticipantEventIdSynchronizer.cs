#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    internal static class TimelineParticipantEventIdSynchronizer
    {
        private const string PropertyName = "timelineEventIds";

        private static readonly Regex PropertyRegex = new Regex(
            "\"timelineEventIds\"\\s*:\\s*\\[(?<content>[\\s\\S]*?)\\]",
            RegexOptions.Compiled);

        private static readonly Regex JsonStringRegex = new Regex(
            "\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.Compiled);

        internal sealed class SyncSummary
        {
            public int participantsVisited;
            public int idsAlreadyPresent;
            public int idsAddedToExistingProperty;
            public int propertiesCreated;
            public int missingParticipantFiles;
            public int nonFileBackedParticipants;
            public int blankParticipantIds;
            public int invalidJsonFiles;
            public int blankEventIds;

            public override string ToString()
            {
                return $"visited={participantsVisited}, alreadyPresent={idsAlreadyPresent}, addedToExisting={idsAddedToExistingProperty}, " +
                       $"createdProperty={propertiesCreated}, missingFiles={missingParticipantFiles}, nonFileBacked={nonFileBackedParticipants}, " +
                       $"blankParticipantIds={blankParticipantIds}, invalidJson={invalidJsonFiles}, blankEventIds={blankEventIds}";
            }
        }

        private enum UpsertResult
        {
            AlreadyPresent,
            AddedToExistingProperty,
            CreatedPropertyAndAdded,
            InvalidJsonRoot
        }

        internal static SyncSummary AuditAndSync(TimelineCatalogData data)
        {
            var summary = new SyncSummary();

            if (data == null)
            {
                Debug.LogWarning("[Timeline Sync] Timeline catalog data is null. Nothing to synchronize.");
                return summary;
            }

            data.EnsureMainCatalog();

            for (var entryIndex = 0; entryIndex < data.entries.Count; entryIndex++)
            {
                var entry = data.entries[entryIndex];
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.id))
                {
                    summary.blankEventIds++;
                    Debug.LogWarning($"[Timeline Sync] Entry at index {entryIndex} has no event id. Participant JSON references cannot be synchronized for that entry.");
                    continue;
                }

                if (entry.participants == null)
                {
                    continue;
                }

                for (var participantIndex = 0; participantIndex < entry.participants.Count; participantIndex++)
                {
                    summary.participantsVisited++;

                    var participant = entry.participants[participantIndex];
                    if (participant == null)
                    {
                        Debug.LogWarning($"[Timeline Sync] Event '{entry.id}' has a null participant slot at index {participantIndex}.");
                        continue;
                    }

                    if (participant.kind == TimelineParticipantKind.Other)
                    {
                        summary.nonFileBackedParticipants++;
                        Debug.Log($"[Timeline Sync] Event '{entry.id}' participant '{participant.role}' is of kind Other and has no file-backed JSON to inspect.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(participant.id))
                    {
                        summary.blankParticipantIds++;
                        Debug.Log($"[Timeline Sync] Event '{entry.id}' participant '{participant.role}' has no selected participant id. No JSON file was updated.");
                        continue;
                    }

                    if (!TimelineReferenceLookup.TryFindJsonFile(participant.kind, participant.id, out var filePath))
                    {
                        summary.missingParticipantFiles++;
                        Debug.LogWarning($"[Timeline Sync] Event '{entry.id}' participant '{participant.id}' ({participant.kind}) has no matching JSON file in the known save-data folders.");
                        continue;
                    }

                    try
                    {
                        var upsertResult = EnsureEventIdPresent(filePath, entry.id);

                        switch (upsertResult)
                        {
                            case UpsertResult.AlreadyPresent:
                                summary.idsAlreadyPresent++;
                                Debug.Log($"[Timeline Sync] Event '{entry.id}' already exists in {participant.kind} JSON '{participant.id}' at '{filePath}'.");
                                break;

                            case UpsertResult.AddedToExistingProperty:
                                summary.idsAddedToExistingProperty++;
                                Debug.Log($"[Timeline Sync] Added missing event id '{entry.id}' to existing {PropertyName} on {participant.kind} '{participant.id}' at '{filePath}'.");
                                break;

                            case UpsertResult.CreatedPropertyAndAdded:
                                summary.propertiesCreated++;
                                Debug.Log($"[Timeline Sync] {participant.kind} '{participant.id}' at '{filePath}' did not have a {PropertyName} array. Created it and added '{entry.id}'.");
                                break;

                            default:
                                summary.invalidJsonFiles++;
                                Debug.LogWarning($"[Timeline Sync] Could not synchronize {participant.kind} '{participant.id}' at '{filePath}' because the file does not appear to contain a root JSON object.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        summary.invalidJsonFiles++;
                        Debug.LogWarning($"[Timeline Sync] Failed to update {participant.kind} '{participant.id}' for event '{entry.id}'. Exception: {ex}");
                    }
                }
            }

            Debug.Log($"[Timeline Sync] Completed participant event-id synchronization: {summary}");
            return summary;
        }

        private static UpsertResult EnsureEventIdPresent(string filePath, string eventId)
        {
            var text = File.ReadAllText(filePath);
            if (!LooksLikeRootObject(text))
            {
                return UpsertResult.InvalidJsonRoot;
            }

            var propertyMatch = PropertyRegex.Match(text);
            if (propertyMatch.Success)
            {
                var existingIds = ExtractJsonStrings(propertyMatch.Groups["content"].Value);
                for (var i = 0; i < existingIds.Count; i++)
                {
                    if (string.Equals(existingIds[i], eventId, StringComparison.Ordinal))
                    {
                        return UpsertResult.AlreadyPresent;
                    }
                }

                existingIds.Add(eventId);

                var indent = GetLineIndentation(text, propertyMatch.Index);
                var indentUnit = DetectIndentUnit(text);
                var replacement = BuildPropertyValue(indent, indent + indentUnit, existingIds, DetectNewline(text));

                var updatedText = text.Substring(0, propertyMatch.Index) +
                                  replacement +
                                  text.Substring(propertyMatch.Index + propertyMatch.Length);

                File.WriteAllText(filePath, updatedText);
                return UpsertResult.AddedToExistingProperty;
            }

            var newText = InsertNewProperty(text, eventId);
            if (string.IsNullOrEmpty(newText))
            {
                return UpsertResult.InvalidJsonRoot;
            }

            File.WriteAllText(filePath, newText);
            return UpsertResult.CreatedPropertyAndAdded;
        }

        private static List<string> ExtractJsonStrings(string content)
        {
            var values = new List<string>();

            foreach (Match match in JsonStringRegex.Matches(content))
            {
                if (!match.Success)
                {
                    continue;
                }

                var value = match.Groups["value"].Value
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");

                values.Add(value);
            }

            return values;
        }

        private static string BuildPropertyValue(string propertyIndent, string itemIndent, List<string> ids, string newline)
        {
            var builder = new StringBuilder();
            builder.Append('"').Append(PropertyName).Append("\": [");

            if (ids == null || ids.Count == 0)
            {
                builder.Append(']');
                return builder.ToString();
            }

            builder.Append(newline);

            for (var i = 0; i < ids.Count; i++)
            {
                builder.Append(itemIndent)
                    .Append('"')
                    .Append(EscapeJsonString(ids[i]))
                    .Append('"');

                if (i < ids.Count - 1)
                {
                    builder.Append(',');
                }

                builder.Append(newline);
            }

            builder.Append(propertyIndent).Append(']');
            return builder.ToString();
        }

        private static string InsertNewProperty(string text, string eventId)
        {
            var lastNonWhitespaceIndex = FindLastNonWhitespaceIndex(text);
            if (lastNonWhitespaceIndex < 0 || text[lastNonWhitespaceIndex] != '}')
            {
                return null;
            }

            var newline = DetectNewline(text);
            var indentUnit = DetectIndentUnit(text);

            var beforeClose = text.Substring(0, lastNonWhitespaceIndex);
            var trailing = text.Substring(lastNonWhitespaceIndex + 1);

            var trimmedBeforeClose = beforeClose.TrimEnd();
            var hasExistingProperties = trimmedBeforeClose.Length > 0 && trimmedBeforeClose[trimmedBeforeClose.Length - 1] != '{';

            var builder = new StringBuilder();
            builder.Append(trimmedBeforeClose);

            if (hasExistingProperties)
            {
                builder.Append(',');
            }

            builder.Append(newline);
            builder.Append(indentUnit);
            builder.Append(BuildPropertyValue(indentUnit, indentUnit + indentUnit, new List<string> { eventId }, newline));
            builder.Append(newline);
            builder.Append('}');
            builder.Append(trailing);

            return builder.ToString();
        }

        private static bool LooksLikeRootObject(string text)
        {
            var index = FindFirstNonWhitespaceIndex(text);
            if (index < 0 || text[index] != '{')
            {
                return false;
            }

            index = FindLastNonWhitespaceIndex(text);
            return index >= 0 && text[index] == '}';
        }

        private static int FindFirstNonWhitespaceIndex(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return -1;
            }

            for (var i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindLastNonWhitespaceIndex(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return -1;
            }

            for (var i = text.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string DetectNewline(string text)
        {
            return text != null && text.Contains("\r\n") ? "\r\n" : "\n";
        }

        private static string DetectIndentUnit(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "  ";
            }

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '\n')
                {
                    continue;
                }

                var start = i + 1;
                var length = 0;

                while (start + length < text.Length &&
                       (text[start + length] == ' ' || text[start + length] == '\t'))
                {
                    length++;
                }

                if (length > 0)
                {
                    return text.Substring(start, length);
                }
            }

            return "  ";
        }

        private static string GetLineIndentation(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index <= 0)
            {
                return string.Empty;
            }

            var lineStart = text.LastIndexOf('\n', Math.Min(index - 1, text.Length - 1));
            if (lineStart < 0)
            {
                lineStart = 0;
            }
            else
            {
                lineStart++;
            }

            var indentationLength = 0;
            while (lineStart + indentationLength < index &&
                   (text[lineStart + indentationLength] == ' ' || text[lineStart + indentationLength] == '\t'))
            {
                indentationLength++;
            }

            return text.Substring(lineStart, indentationLength);
        }

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
#endif
