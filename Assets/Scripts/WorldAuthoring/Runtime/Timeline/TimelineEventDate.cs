using System;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    [Serializable]
    public sealed class TimelineEventDate : IComparable<TimelineEventDate>
    {
        [Tooltip("Free-text year field. Supports negatives if you want BCE-style dates.")]
        public string year = string.Empty;

        [Tooltip("0 means 'unspecified'. When unspecified, sorting defaults to month 1.")]
        [Range(0, 12)]
        public int month = 0;

        [Tooltip("0 means 'unspecified'. When unspecified, sorting defaults to day 1.")]
        [Range(0, 30)]
        public int day = 0;

        public int ParsedYear => int.TryParse(year, out var parsed) ? parsed : 0;

        public int SortMonth => month <= 0 ? 1 : Mathf.Clamp(month, 1, 12);

        public int SortDay => day <= 0 ? 1 : Mathf.Clamp(day, 1, 30);

        public bool HasYear => !string.IsNullOrWhiteSpace(year);

        public bool HasMonth => month > 0;

        public bool HasDay => day > 0;

        public void Normalize()
        {
            if (month < 0) month = 0;
            if (month > 12) month = 12;

            if (month == 0)
            {
                day = 0;
                return;
            }

            if (day < 0) day = 0;
            if (day > 30) day = 30;
        }

        public int CompareTo(TimelineEventDate other)
        {
            if (other == null)
            {
                return 1;
            }

            var yearComparison = ParsedYear.CompareTo(other.ParsedYear);
            if (yearComparison != 0)
            {
                return yearComparison;
            }

            var monthComparison = SortMonth.CompareTo(other.SortMonth);
            if (monthComparison != 0)
            {
                return monthComparison;
            }

            return SortDay.CompareTo(other.SortDay);
        }

        public TimelineEventDate Clone()
        {
            return new TimelineEventDate
            {
                year = year,
                month = month,
                day = day
            };
        }

        public int ToOrdinal360()
        {
            var sortMonth = SortMonth;
            var sortDay = SortDay;
            return (ParsedYear * 360) + ((sortMonth - 1) * 30) + (sortDay - 1);
        }

        public static TimelineEventDate FromOrdinal360(int ordinal)
        {
            var year = Mathf.FloorToInt(ordinal / 360f);
            var remaining = ordinal - (year * 360);
            if (remaining < 0)
            {
                remaining += 360;
                year--;
            }

            var month = Mathf.Clamp((remaining / 30) + 1, 1, 12);
            var day = Mathf.Clamp((remaining % 30) + 1, 1, 30);

            return new TimelineEventDate
            {
                year = year.ToString(),
                month = month,
                day = day
            };
        }

        public override string ToString()
        {
            if (!HasYear)
            {
                return "Unspecified date";
            }

            if (!HasMonth)
            {
                return year;
            }

            if (!HasDay)
            {
                return $"{year}-{month:00}";
            }

            return $"{year}-{month:00}-{day:00}";
        }
    }
}
