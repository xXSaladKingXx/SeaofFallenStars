using System;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Zana.WorldAuthoring
{
    [DisallowMultipleComponent]
    public sealed class TimelineCatalogAuthoringSession : MonoBehaviour
    {
        [Tooltip("Keep this pointed at the one permanent timeline catalog file, for example Assets/SaveData/Timeline/main.timeline.json.")]
        public string loadedFilePath = "Assets/SaveData/Timeline/main.timeline.json";

        public TimelineCatalogData data = TimelineCatalogData.CreateMain();

        private void Reset()
        {
            data ??= TimelineCatalogData.CreateMain();
            data.EnsureMainCatalog();
        }

        public void EnsureInitialized()
        {
            data ??= TimelineCatalogData.CreateMain();
            data.EnsureMainCatalog();
            data.SortInPlace();

            for (var i = 0; i < data.entries.Count; i++)
            {
                var entry = data.entries[i];
                if (entry == null)
                {
                    data.entries[i] = CreateBlankEntry();
                    continue;
                }

                entry.date ??= new TimelineEventDate();
                entry.setting ??= new TimelineEventSetting();
                entry.participants ??= new System.Collections.Generic.List<TimelineEventParticipant>();

                if (string.IsNullOrWhiteSpace(entry.id))
                {
                    entry.id = $"timeline_{Guid.NewGuid():N}";
                }

                if (string.IsNullOrWhiteSpace(entry.eventTypeId))
                {
                    entry.eventTypeId = TimelineBuiltInEventTypes.Other;
                }
            }
        }

        public void SortEntries()
        {
            EnsureInitialized();
            data.SortInPlace();
        }

        public TimelineEventEntry InsertBetween(TimelineEventEntry previous, TimelineEventEntry next)
        {
            EnsureInitialized();

            var blank = CreateBlankEntry();
            blank.date = BuildDefaultDateBetween(previous?.date, next?.date);

            data.entries.Add(blank);
            data.SortInPlace();
            return blank;
        }

        public TimelineEventEntry AddBeforeFirst()
        {
            EnsureInitialized();
            var first = data.entries.Count > 0 ? data.entries[0] : null;
            return InsertBetween(null, first);
        }

        public TimelineEventEntry AddAfterLast()
        {
            EnsureInitialized();
            var last = data.entries.Count > 0 ? data.entries[data.entries.Count - 1] : null;
            return InsertBetween(last, null);
        }

        public static TimelineEventEntry CreateBlankEntry()
        {
            return new TimelineEventEntry
            {
                id = $"timeline_{Guid.NewGuid():N}",
                eventTypeId = TimelineBuiltInEventTypes.Other,
                eventName = string.Empty,
                description = string.Empty,
                significance = TimelineEventSignificance.Info,
                directionality = TimelineEventDirectionality.None,
                uniqueIconPathOrGuid = string.Empty,
                date = new TimelineEventDate
                {
                    year = "0",
                    month = 1,
                    day = 1
                },
                setting = new TimelineEventSetting
                {
                    kind = TimelineSettingKind.None,
                    id = string.Empty
                },
                participants = new System.Collections.Generic.List<TimelineEventParticipant>()
            };
        }

        public static TimelineEventDate BuildDefaultDateBetween(TimelineEventDate previous, TimelineEventDate next)
        {
            if (previous == null && next == null)
            {
                return new TimelineEventDate
                {
                    year = "0",
                    month = 1,
                    day = 1
                };
            }

            if (previous == null)
            {
                var nextOrdinal = next.ToOrdinal360();
                return TimelineEventDate.FromOrdinal360(nextOrdinal - 1);
            }

            if (next == null)
            {
                var previousOrdinal = previous.ToOrdinal360();
                return TimelineEventDate.FromOrdinal360(previousOrdinal + 1);
            }

            var low = previous.ToOrdinal360();
            var high = next.ToOrdinal360();

            if (high <= low)
            {
                return previous.Clone();
            }

            var gap = high - low;
            if (gap <= 1)
            {
                return previous.Clone();
            }

            var midpoint = low + (gap / 2);
            return TimelineEventDate.FromOrdinal360(midpoint);
        }

#if UNITY_EDITOR
        public void LoadFromFile()
        {
            if (string.IsNullOrWhiteSpace(loadedFilePath))
            {
                Debug.LogWarning("[Timeline] Loaded file path is blank. Nothing was loaded.");
                data = TimelineCatalogData.CreateMain();
                EnsureInitialized();
                return;
            }

            if (!File.Exists(loadedFilePath))
            {
                Debug.Log($"[Timeline] No timeline file exists at '{loadedFilePath}'. Starting with a blank main timeline.");
                data = TimelineCatalogData.CreateMain();
                EnsureInitialized();
                EditorUtility.SetDirty(this);
                return;
            }

            try
            {
                var json = File.ReadAllText(loadedFilePath);
                var loaded = JsonUtility.FromJson<TimelineCatalogData>(json);
                data = loaded ?? TimelineCatalogData.CreateMain();
                EnsureInitialized();
                EditorUtility.SetDirty(this);
                Debug.Log($"[Timeline] Loaded timeline catalog from '{loadedFilePath}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] Failed to load timeline catalog from '{loadedFilePath}'. Exception: {ex}");
                data = TimelineCatalogData.CreateMain();
                EnsureInitialized();
            }
        }

        public void SaveToFile(bool syncParticipantEventIds)
        {
            EnsureInitialized();
            data.SortInPlace();

            if (string.IsNullOrWhiteSpace(loadedFilePath))
            {
                Debug.LogWarning("[Timeline] Loaded file path is blank. Nothing was saved.");
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(loadedFilePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(loadedFilePath, json);

                if (syncParticipantEventIds)
                {
                    TimelineParticipantEventIdSynchronizer.AuditAndSync(data);
                }

                AssetDatabase.Refresh();
                EditorUtility.SetDirty(this);

                Debug.Log(syncParticipantEventIds
                    ? $"[Timeline] Saved timeline catalog to '{loadedFilePath}' and synchronized participant event references."
                    : $"[Timeline] Saved timeline catalog to '{loadedFilePath}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] Failed to save timeline catalog to '{loadedFilePath}'. Exception: {ex}");
            }
        }
#endif
    }
}
