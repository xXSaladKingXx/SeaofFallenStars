using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    public enum TimelineEventSignificance
    {
        Major,
        Minor,
        Info
    }

    public enum TimelineEventDirectionality
    {
        None,
        Forward,
        Mutual
    }

    public enum TimelineParticipantSide
    {
        Neutral,
        Source,
        Target
    }

    public enum TimelineParticipantKind
    {
        Settlement,
        Character,
        Unpopulated,
        Army,
        TravelGroup,
        Other
    }

    public enum TimelineSettingKind
    {
        None,
        Settlement,
        Unpopulated,
        Other
    }

    [Serializable]
    public sealed class TimelineEventSetting
    {
        public TimelineSettingKind kind = TimelineSettingKind.None;
        public string id = string.Empty;
    }

    [Serializable]
    public sealed class TimelineEventParticipant
    {
        public string role = string.Empty;
        public TimelineParticipantKind kind = TimelineParticipantKind.Character;
        public string id = string.Empty;
        public TimelineParticipantSide side = TimelineParticipantSide.Neutral;

        public TimelineEventParticipant Clone(bool clearId)
        {
            return new TimelineEventParticipant
            {
                role = role,
                kind = kind,
                id = clearId ? string.Empty : id,
                side = side
            };
        }
    }

    [Serializable]
    public sealed class TimelineEventTypeDefinition
    {
        public string id = TimelineBuiltInEventTypes.Other;
        public string displayName = TimelineBuiltInEventTypes.Other;
        public bool isBuiltIn = false;
        public TimelineEventDirectionality defaultDirectionality = TimelineEventDirectionality.None;
        public string defaultIconPathOrGuid = string.Empty;
        public List<TimelineEventParticipant> defaultParticipants = new List<TimelineEventParticipant>();
        public List<TimelineSettingKind> allowedSettingKinds = new List<TimelineSettingKind>();

        public void EnsureInitialized()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = string.IsNullOrWhiteSpace(displayName) ? TimelineBuiltInEventTypes.Other : displayName.Trim();
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = id;
            }

            defaultParticipants ??= new List<TimelineEventParticipant>();
            allowedSettingKinds ??= new List<TimelineSettingKind>();
        }
    }

    [Serializable]
    public sealed class TimelineEventEntry
    {
        public string id = string.Empty;
        public TimelineEventDate date = new TimelineEventDate();
        public string eventTypeId = TimelineBuiltInEventTypes.Other;
        public string eventName = string.Empty;
        public TimelineEventSignificance significance = TimelineEventSignificance.Info;

        [TextArea(2, 5)]
        public string description = string.Empty;

        public TimelineEventDirectionality directionality = TimelineEventDirectionality.None;

        [Tooltip("Optional per-entry icon path/GUID. Leave blank to fall back to the event type default icon.")]
        public string uniqueIconPathOrGuid = string.Empty;

        public TimelineEventSetting setting = new TimelineEventSetting();
        public List<TimelineEventParticipant> participants = new List<TimelineEventParticipant>();

        public string GetStableSortKey()
        {
            return $"{date?.ToString() ?? "0000"}::{eventTypeId}::{eventName}::{id}";
        }
    }

    [Serializable]
    public sealed class TimelineCatalogData
    {
        public string catalogId = "main";
        public List<TimelineEventTypeDefinition> customEventTypes = new List<TimelineEventTypeDefinition>();
        public List<TimelineEventEntry> entries = new List<TimelineEventEntry>();

        public static TimelineCatalogData CreateMain()
        {
            return new TimelineCatalogData
            {
                catalogId = "main",
                customEventTypes = new List<TimelineEventTypeDefinition>(),
                entries = new List<TimelineEventEntry>()
            };
        }

        public void EnsureMainCatalog()
        {
            catalogId = "main";
            customEventTypes ??= new List<TimelineEventTypeDefinition>();
            entries ??= new List<TimelineEventEntry>();

            for (var i = customEventTypes.Count - 1; i >= 0; i--)
            {
                if (customEventTypes[i] == null)
                {
                    customEventTypes.RemoveAt(i);
                    continue;
                }

                customEventTypes[i].EnsureInitialized();
            }

            customEventTypes.Sort(CompareTypeDefinitions);
        }

        public bool TryGetCustomType(string typeId, out TimelineEventTypeDefinition definition)
        {
            EnsureMainCatalog();

            if (!string.IsNullOrWhiteSpace(typeId))
            {
                for (var i = 0; i < customEventTypes.Count; i++)
                {
                    var candidate = customEventTypes[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (string.Equals(candidate.id, typeId, StringComparison.OrdinalIgnoreCase))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public void UpsertCustomType(TimelineEventTypeDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            EnsureMainCatalog();
            definition.EnsureInitialized();

            for (var i = 0; i < customEventTypes.Count; i++)
            {
                var existing = customEventTypes[i];
                if (existing == null)
                {
                    continue;
                }

                if (string.Equals(existing.id, definition.id, StringComparison.OrdinalIgnoreCase))
                {
                    customEventTypes[i] = definition;
                    customEventTypes.Sort(CompareTypeDefinitions);
                    return;
                }
            }

            customEventTypes.Add(definition);
            customEventTypes.Sort(CompareTypeDefinitions);
        }

        public void SortInPlace()
        {
            EnsureMainCatalog();
            entries.Sort(CompareEntries);
        }

        private static int CompareEntries(TimelineEventEntry left, TimelineEventEntry right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            var dateComparison = (left.date ?? new TimelineEventDate()).CompareTo(right.date ?? new TimelineEventDate());
            if (dateComparison != 0)
            {
                return dateComparison;
            }

            return string.CompareOrdinal(left.GetStableSortKey(), right.GetStableSortKey());
        }

        private static int CompareTypeDefinitions(TimelineEventTypeDefinition left, TimelineEventTypeDefinition right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return string.Compare(left.displayName, right.displayName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class TimelineBuiltInEventTypes
    {
        public const string Birth = "Birth";
        public const string Death = "Death";
        public const string Coronation = "Coronation";
        public const string DeclareWar = "Declare War";
        public const string Betrothal = "Betrothal";
        public const string Marriage = "Marriage";
        public const string Kill = "Kill";
        public const string Coitus = "Coitus";
        public const string MakeAlliance = "Make Alliance";
        public const string MakePeace = "Make Peace";
        public const string Battle = "Battle";
        public const string BeganTraveling = "Began Traveling";
        public const string Visited = "Visited";
        public const string EndTraveling = "End Traveling";
        public const string StrippedOfTitle = "Stripped of Title";
        public const string Knighted = "Knighted";
        public const string AwardedLand = "Awarded Land";
        public const string LevelUp = "Level Up";
        public const string Befriend = "Befriend";
        public const string StartRivalry = "Start Rivalry";
        public const string Other = "Other";
    }

    public static class TimelineEventTypeRegistry
    {
        private static readonly List<TimelineEventTypeDefinition> BuiltInDefinitions = BuildDefinitions();
        private static readonly Dictionary<string, TimelineEventTypeDefinition> BuiltInLookup = BuildLookup(BuiltInDefinitions);

        public static List<TimelineEventTypeDefinition> GetAllDefinitions(TimelineCatalogData data)
        {
            var results = new List<TimelineEventTypeDefinition>(BuiltInDefinitions.Count + (data?.customEventTypes?.Count ?? 0));

            for (var i = 0; i < BuiltInDefinitions.Count; i++)
            {
                results.Add(BuiltInDefinitions[i]);
            }

            if (data != null)
            {
                data.EnsureMainCatalog();

                for (var i = 0; i < data.customEventTypes.Count; i++)
                {
                    var custom = data.customEventTypes[i];
                    if (custom == null)
                    {
                        continue;
                    }

                    if (BuiltInLookup.ContainsKey(custom.id))
                    {
                        continue;
                    }

                    results.Add(custom);
                }
            }

            return results;
        }

        public static TimelineEventTypeDefinition GetDefinition(TimelineCatalogData data, string typeId)
        {
            if (!string.IsNullOrWhiteSpace(typeId))
            {
                if (BuiltInLookup.TryGetValue(typeId, out var builtIn))
                {
                    return builtIn;
                }

                if (data != null && data.TryGetCustomType(typeId, out var custom))
                {
                    return custom;
                }
            }

            return BuiltInLookup[TimelineBuiltInEventTypes.Other];
        }

        public static bool IsBuiltIn(string typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId))
            {
                return false;
            }

            return BuiltInLookup.ContainsKey(typeId);
        }

        public static void ApplyDefaults(
            TimelineCatalogData data,
            TimelineEventEntry entry,
            bool overwriteExistingParticipants,
            bool setEventNameWhenEmpty)
        {
            if (entry == null)
            {
                return;
            }

            var definition = GetDefinition(data, entry.eventTypeId);
            entry.directionality = definition.defaultDirectionality;

            if (setEventNameWhenEmpty && string.IsNullOrWhiteSpace(entry.eventName))
            {
                entry.eventName = definition.displayName;
            }

            if (!overwriteExistingParticipants && entry.participants != null && entry.participants.Count > 0)
            {
                return;
            }

            entry.participants ??= new List<TimelineEventParticipant>();
            entry.participants.Clear();

            for (var i = 0; i < definition.defaultParticipants.Count; i++)
            {
                var template = definition.defaultParticipants[i];
                if (template == null)
                {
                    continue;
                }

                entry.participants.Add(template.Clone(clearId: true));
            }
        }

        public static bool TryCreateOrUpdateCustomTypeFromOtherEntry(
            TimelineCatalogData data,
            TimelineEventEntry entry,
            out string message)
        {
            if (data == null)
            {
                message = "Timeline catalog data is null.";
                return false;
            }

            if (entry == null)
            {
                message = "Timeline entry is null.";
                return false;
            }

            if (!string.Equals(entry.eventTypeId, TimelineBuiltInEventTypes.Other, StringComparison.OrdinalIgnoreCase))
            {
                message = "Only entries currently typed as Other can be saved as a new custom event type.";
                return false;
            }

            var proposedTypeName = (entry.eventName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(proposedTypeName))
            {
                message = "Custom event type name cannot be blank. Use the entry's Event Name field as the custom type name.";
                return false;
            }

            if (IsBuiltIn(proposedTypeName))
            {
                message = $"'{proposedTypeName}' is already a built-in event type.";
                return false;
            }

            data.EnsureMainCatalog();

            var customDefinition = new TimelineEventTypeDefinition
            {
                id = proposedTypeName,
                displayName = proposedTypeName,
                isBuiltIn = false,
                defaultDirectionality = entry.directionality,
                defaultIconPathOrGuid = string.IsNullOrWhiteSpace(entry.uniqueIconPathOrGuid)
                    ? $"timeline/custom/{MakeIconSafeKey(proposedTypeName)}"
                    : entry.uniqueIconPathOrGuid,
                defaultParticipants = BuildCustomParticipantTemplates(entry.participants),
                allowedSettingKinds = BuildAllSettingKinds()
            };

            data.UpsertCustomType(customDefinition);
            entry.eventTypeId = customDefinition.id;

            message = $"Saved custom timeline event type '{customDefinition.displayName}'.";
            return true;
        }

        private static List<TimelineEventParticipant> BuildCustomParticipantTemplates(List<TimelineEventParticipant> participants)
        {
            var templates = new List<TimelineEventParticipant>();

            if (participants == null)
            {
                return templates;
            }

            var sourceCount = 0;
            var targetCount = 0;
            var neutralCount = 0;

            for (var i = 0; i < participants.Count; i++)
            {
                var participant = participants[i];
                if (participant == null)
                {
                    continue;
                }

                var role = (participant.role ?? string.Empty).Trim();
                switch (participant.side)
                {
                    case TimelineParticipantSide.Source:
                        sourceCount++;
                        if (string.IsNullOrWhiteSpace(role))
                        {
                            role = sourceCount == 1 ? "Initiator" : $"Initiator {sourceCount}";
                        }

                        break;

                    case TimelineParticipantSide.Target:
                        targetCount++;
                        if (string.IsNullOrWhiteSpace(role))
                        {
                            role = targetCount == 1 ? "Target" : $"Target {targetCount}";
                        }

                        break;

                    default:
                        neutralCount++;
                        if (string.IsNullOrWhiteSpace(role))
                        {
                            role = neutralCount == 1 ? "Participant" : $"Participant {neutralCount}";
                        }

                        break;
                }

                templates.Add(new TimelineEventParticipant
                {
                    role = role,
                    kind = participant.kind,
                    id = string.Empty,
                    side = participant.side
                });
            }

            return templates;
        }

        private static List<TimelineSettingKind> BuildAllSettingKinds()
        {
            return new List<TimelineSettingKind>
            {
                TimelineSettingKind.None,
                TimelineSettingKind.Settlement,
                TimelineSettingKind.Unpopulated,
                TimelineSettingKind.Other
            };
        }

        private static string MakeIconSafeKey(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "custom";
            }

            var chars = displayName.Trim().ToLowerInvariant().ToCharArray();
            var buffer = new List<char>(chars.Length);

            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (char.IsLetterOrDigit(c))
                {
                    buffer.Add(c);
                    continue;
                }

                if (buffer.Count == 0 || buffer[buffer.Count - 1] == '-')
                {
                    continue;
                }

                buffer.Add('-');
            }

            if (buffer.Count == 0)
            {
                return "custom";
            }

            if (buffer[buffer.Count - 1] == '-')
            {
                buffer.RemoveAt(buffer.Count - 1);
            }

            return new string(buffer.ToArray());
        }

        private static Dictionary<string, TimelineEventTypeDefinition> BuildLookup(List<TimelineEventTypeDefinition> definitions)
        {
            var result = new Dictionary<string, TimelineEventTypeDefinition>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                result[definition.id] = definition;
            }

            return result;
        }

        private static List<TimelineEventTypeDefinition> BuildDefinitions()
        {
            return new List<TimelineEventTypeDefinition>
            {
                BuiltIn(
                    TimelineBuiltInEventTypes.Birth,
                    TimelineEventDirectionality.Forward,
                    "timeline/birth",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Father", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Mother", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Child", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Death,
                    TimelineEventDirectionality.None,
                    "timeline/death",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Deceased", TimelineParticipantKind.Character, TimelineParticipantSide.Neutral)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Coronation,
                    TimelineEventDirectionality.Forward,
                    "timeline/coronation",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Crowned Character", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Realm / Seat", TimelineParticipantKind.Settlement, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.DeclareWar,
                    TimelineEventDirectionality.Mutual,
                    "timeline/declare-war",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Belligerent A", TimelineParticipantKind.Settlement, TimelineParticipantSide.Source),
                        Participant("Belligerent B", TimelineParticipantKind.Settlement, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Betrothal,
                    TimelineEventDirectionality.Mutual,
                    "timeline/betrothal",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Betrothed A", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Betrothed B", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Marriage,
                    TimelineEventDirectionality.Mutual,
                    "timeline/marriage",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Spouse A", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Spouse B", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Kill,
                    TimelineEventDirectionality.Forward,
                    "timeline/kill",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Killer", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Victim", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Coitus,
                    TimelineEventDirectionality.Mutual,
                    "timeline/coitus",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Participant A", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Participant B", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.MakeAlliance,
                    TimelineEventDirectionality.Mutual,
                    "timeline/alliance",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Ally A", TimelineParticipantKind.Settlement, TimelineParticipantSide.Source),
                        Participant("Ally B", TimelineParticipantKind.Settlement, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.MakePeace,
                    TimelineEventDirectionality.Mutual,
                    "timeline/peace",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Belligerent A", TimelineParticipantKind.Settlement, TimelineParticipantSide.Source),
                        Participant("Belligerent B", TimelineParticipantKind.Settlement, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Battle,
                    TimelineEventDirectionality.Mutual,
                    "timeline/battle",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Side A", TimelineParticipantKind.Army, TimelineParticipantSide.Source),
                        Participant("Side B", TimelineParticipantKind.Army, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.BeganTraveling,
                    TimelineEventDirectionality.None,
                    "timeline/began-traveling",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Traveler", TimelineParticipantKind.TravelGroup, TimelineParticipantSide.Neutral)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Visited,
                    TimelineEventDirectionality.Forward,
                    "timeline/visited",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Visitor", TimelineParticipantKind.Character, TimelineParticipantSide.Source)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated),

                BuiltIn(
                    TimelineBuiltInEventTypes.EndTraveling,
                    TimelineEventDirectionality.None,
                    "timeline/end-traveling",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Traveler", TimelineParticipantKind.TravelGroup, TimelineParticipantSide.Neutral)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.StrippedOfTitle,
                    TimelineEventDirectionality.Forward,
                    "timeline/stripped-of-title",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Character", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Seat / Title", TimelineParticipantKind.Settlement, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Knighted,
                    TimelineEventDirectionality.Forward,
                    "timeline/knighted",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Knight", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Squire", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.AwardedLand,
                    TimelineEventDirectionality.Forward,
                    "timeline/awarded-land",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Grantor", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Awardee", TimelineParticipantKind.Character, TimelineParticipantSide.Target),
                        Participant("Granted Holding", TimelineParticipantKind.Settlement, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.LevelUp,
                    TimelineEventDirectionality.Forward,
                    "timeline/level-up",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Character", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("New Level", TimelineParticipantKind.Other, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Befriend,
                    TimelineEventDirectionality.Mutual,
                    "timeline/befriend",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Friend A", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Friend B", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.StartRivalry,
                    TimelineEventDirectionality.Mutual,
                    "timeline/start-rivalry",
                    new List<TimelineEventParticipant>
                    {
                        Participant("Rival A", TimelineParticipantKind.Character, TimelineParticipantSide.Source),
                        Participant("Rival B", TimelineParticipantKind.Character, TimelineParticipantSide.Target)
                    },
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other),

                BuiltIn(
                    TimelineBuiltInEventTypes.Other,
                    TimelineEventDirectionality.None,
                    "timeline/other",
                    new List<TimelineEventParticipant>(),
                    TimelineSettingKind.None,
                    TimelineSettingKind.Settlement,
                    TimelineSettingKind.Unpopulated,
                    TimelineSettingKind.Other)
            };
        }

        private static TimelineEventTypeDefinition BuiltIn(
            string id,
            TimelineEventDirectionality directionality,
            string iconKey,
            List<TimelineEventParticipant> participants,
            params TimelineSettingKind[] settingKinds)
        {
            return new TimelineEventTypeDefinition
            {
                id = id,
                displayName = id,
                isBuiltIn = true,
                defaultDirectionality = directionality,
                defaultIconPathOrGuid = iconKey,
                defaultParticipants = participants ?? new List<TimelineEventParticipant>(),
                allowedSettingKinds = new List<TimelineSettingKind>(settingKinds ?? Array.Empty<TimelineSettingKind>())
            };
        }

        private static TimelineEventParticipant Participant(string role, TimelineParticipantKind kind, TimelineParticipantSide side)
        {
            return new TimelineEventParticipant
            {
                role = role,
                kind = kind,
                id = string.Empty,
                side = side
            };
        }
    }
}
