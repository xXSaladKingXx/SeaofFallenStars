#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Zana.WorldAuthoring
{
    internal static class TimelineReferenceLookup
    {
        internal readonly struct Option
        {
            public Option(string id, string label)
            {
                Id = id;
                Label = label;
            }

            public string Id { get; }
            public string Label { get; }
        }

        private static readonly string[] SettlementFolders =
        {
            "Assets/SaveData/MapData",
            "Assets/SaveData/SettlementData"
        };

        private static readonly string[] UnpopulatedFolders =
        {
            "Assets/SaveData/UnpopulatedData"
        };

        private static readonly string[] CharacterFolders =
        {
            "Assets/SaveData/CharacterData",
            "Assets/SaveData/Characters"
        };

        private static readonly string[] ArmyFolders =
        {
            "Assets/SaveData/ArmyData",
            "Assets/SaveData/Armies"
        };

        private static readonly string[] TravelGroupFolders =
        {
            "Assets/SaveData/TravelGroupData",
            "Assets/SaveData/TravelGroups"
        };

        internal static Option[] GetSettingOptions(TimelineSettingKind kind)
        {
            return kind switch
            {
                TimelineSettingKind.Settlement => GetOptions(TimelineParticipantKind.Settlement),
                TimelineSettingKind.Unpopulated => GetOptions(TimelineParticipantKind.Unpopulated),
                TimelineSettingKind.Other => Array.Empty<Option>(),
                _ => Array.Empty<Option>()
            };
        }

        internal static Option[] GetOptions(TimelineParticipantKind kind)
        {
            return kind switch
            {
                TimelineParticipantKind.Settlement => FindFromFolders(SettlementFolders, "Settlement"),
                TimelineParticipantKind.Unpopulated => FindFromFolders(UnpopulatedFolders, "Unpopulated"),
                TimelineParticipantKind.Character => FindFromFolders(CharacterFolders, "Character"),
                TimelineParticipantKind.Army => FindFromFolders(ArmyFolders, "Army"),
                TimelineParticipantKind.TravelGroup => FindFromFolders(TravelGroupFolders, "Travel Group"),
                _ => Array.Empty<Option>()
            };
        }

        internal static bool TryFindJsonFile(TimelineParticipantKind kind, string id, out string filePath)
        {
            filePath = string.Empty;

            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            foreach (var folder in GetFolders(kind))
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
                {
                    var extension = Path.GetExtension(file);
                    if (!string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var candidateId = Path.GetFileNameWithoutExtension(file);
                    if (string.Equals(candidateId, id, StringComparison.OrdinalIgnoreCase))
                    {
                        filePath = file;
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<string> GetFolders(TimelineParticipantKind kind)
        {
            return kind switch
            {
                TimelineParticipantKind.Settlement => SettlementFolders,
                TimelineParticipantKind.Unpopulated => UnpopulatedFolders,
                TimelineParticipantKind.Character => CharacterFolders,
                TimelineParticipantKind.Army => ArmyFolders,
                TimelineParticipantKind.TravelGroup => TravelGroupFolders,
                _ => Array.Empty<string>()
            };
        }

        private static Option[] FindFromFolders(IEnumerable<string> folders, string prefix)
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (var file in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
                {
                    var extension = Path.GetExtension(file);
                    if (!string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var id = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    results[id] = $"{prefix}: {id}";
                }
            }

            return results
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => new Option(pair.Key, pair.Value))
                .ToArray();
        }
    }
}
#endif
