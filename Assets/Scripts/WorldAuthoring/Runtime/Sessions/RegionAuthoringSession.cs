using System;
using System.Collections.Generic;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Authoring session for RegionInfoData (Regions/Countries/Duchies/Lordships).
    ///
    /// NOTES
    /// - Vassals are stored as MapPoint IDs only (RegionInfoData.vassals).
    /// - Derived fields (population, distributions, terrain breakdown) are recalculated from child MapPoints on save.
    /// - The inspector UI for this session is customized (see RegionAuthoringSessionEditor) so derived fields are not shown.
    /// </summary>
    public sealed class RegionAuthoringSession : WorldDataAuthoringSessionBase
    {
        [Header("Region Data")]
        public RegionInfoData data = new RegionInfoData();

        public override WorldDataCategory Category => WorldDataCategory.Region;

        public override string GetDefaultFileBaseName()
        {
            // Prefer explicit regionId, else derive from displayName.
            if (data == null) data = new RegionInfoData();

            if (string.IsNullOrWhiteSpace(data.regionId))
            {
                if (!string.IsNullOrWhiteSpace(data.displayName))
                    data.regionId = Slugify(data.displayName);
            }

            return string.IsNullOrWhiteSpace(data.regionId) ? "new_region" : data.regionId.Trim();
        }

        public override string BuildJson()
        {
            if (data == null) data = new RegionInfoData();

            // Ensure required sub-objects exist to avoid nulls.
            if (data.main == null) data.main = new RegionMainTabData();
            if (data.geography == null) data.geography = new RegionGeographyTabData();
            if (data.vassals == null) data.vassals = new List<string>();
            if (data.derived == null) data.derived = new RegionDerivedInfo();

            // Ensure regionId.
            if (string.IsNullOrWhiteSpace(data.regionId))
                data.regionId = Slugify(data.displayName);

            // Recompute derived fields from child map points.
            RegionDerivedCalculator.RecalculateFromChildren(data);

            return ToJson(data);
        }

        public override void ApplyJson(string json)
        {
            data = string.IsNullOrWhiteSpace(json)
                ? new RegionInfoData()
                : (FromJson<RegionInfoData>(json) ?? new RegionInfoData());

            // Repair null sub-objects for safety in inspector/editor.
            if (data.main == null) data.main = new RegionMainTabData();
            if (data.geography == null) data.geography = new RegionGeographyTabData();
            if (data.vassals == null) data.vassals = new List<string>();
            if (data.derived == null) data.derived = new RegionDerivedInfo();
        }

        private static string Slugify(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            s = s.Trim().ToLowerInvariant();

            var chars = new List<char>(s.Length);
            bool prevDash = false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (char.IsLetterOrDigit(c))
                {
                    chars.Add(c);
                    prevDash = false;
                    continue;
                }

                // Convert whitespace and punctuation to single dashes.
                if (!prevDash)
                {
                    chars.Add('-');
                    prevDash = true;
                }
            }

            string result = new string(chars.ToArray()).Trim('-');

            // Collapse any accidental multiple dashes.
            while (result.Contains("--"))
                result = result.Replace("--", "-");

            return result;
        }
    }
}
