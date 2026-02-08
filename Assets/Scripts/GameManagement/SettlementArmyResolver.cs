using System;
using System.Collections.Generic;
using System.Linq;

public static class SettlementArmyResolver
{
    public sealed class SettlementArmySummary
    {
        public int totalTroops;
        public string commanderDisplayName;
        public List<MenAtArmsCount> menAtArmsCounts = new List<MenAtArmsCount>();
        public List<string> armyIds = new List<string>();
    }

    public sealed class MenAtArmsCount
    {
        public string menAtArmsId;
        public int count;
    }

    public static SettlementArmySummary Resolve(SettlementInfoData data)
    {
        var summary = new SettlementArmySummary();
        if (data == null)
            return summary;

        var linkedIds = data.army?.armyIds ?? Array.Empty<string>();
        if (linkedIds.Length == 0)
        {
            summary.totalTroops = Math.Max(0, data.army != null ? data.army.totalArmy : 0);
            summary.commanderDisplayName = data.army?.primaryCommanderDisplayName;

            if (data.army?.menAtArms != null)
            {
                foreach (var id in data.army.menAtArms.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    summary.menAtArmsCounts.Add(new MenAtArmsCount
                    {
                        menAtArmsId = id,
                        count = 0
                    });
                }
            }

            return summary;
        }

        var uniqueArmyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var menAtArmsTotals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var commanderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawId in linkedIds)
        {
            if (string.IsNullOrWhiteSpace(rawId)) continue;
            if (!uniqueArmyIds.Add(rawId)) continue;

            summary.armyIds.Add(rawId);
            var army = ArmyDataLoader.TryLoad(rawId);
            if (army == null) continue;

            summary.totalTroops += Math.Max(0, army.totalArmy);

            string commanderName = !string.IsNullOrWhiteSpace(army.primaryCommanderDisplayName)
                ? army.primaryCommanderDisplayName
                : army.primaryCommanderCharacterId;
            if (!string.IsNullOrWhiteSpace(commanderName))
                commanderNames.Add(commanderName);

            if (army.menAtArms == null) continue;
            foreach (var stack in army.menAtArms)
            {
                if (stack == null || string.IsNullOrWhiteSpace(stack.menAtArmsId)) continue;
                int count = Math.Max(0, stack.count);
                if (menAtArmsTotals.TryGetValue(stack.menAtArmsId, out var existing))
                    menAtArmsTotals[stack.menAtArmsId] = existing + count;
                else
                    menAtArmsTotals[stack.menAtArmsId] = count;
            }
        }

        if (commanderNames.Count == 1)
            summary.commanderDisplayName = commanderNames.First();
        else if (commanderNames.Count > 1)
            summary.commanderDisplayName = string.Join(", ", commanderNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

        foreach (var kvp in menAtArmsTotals.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            summary.menAtArmsCounts.Add(new MenAtArmsCount
            {
                menAtArmsId = kvp.Key,
                count = kvp.Value
            });
        }

        return summary;
    }
}
