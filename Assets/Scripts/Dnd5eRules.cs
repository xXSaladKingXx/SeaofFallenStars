using System.Collections.Generic;
using UnityEngine;

public static class Dnd5eRules
{
    // Proficiency bonus by level: 2 + floor((level-1)/4)
    public static int ProficiencyBonus(int level)
    {
        level = Mathf.Max(1, level);
        return 2 + ((level - 1) / 4);
    }

    public static int AbilityMod(int score)
    {
        // floor((score-10)/2)
        return Mathf.FloorToInt((score - 10) / 2f);
    }

    public static readonly Dictionary<string, string> SkillToAbility = new Dictionary<string, string>()
    {
        // STR
        { "athletics", "str" },

        // DEX
        { "acrobatics", "dex" },
        { "sleight_of_hand", "dex" },
        { "stealth", "dex" },

        // INT
        { "arcana", "int" },
        { "history", "int" },
        { "investigation", "int" },
        { "nature", "int" },
        { "religion", "int" },

        // WIS
        { "animal_handling", "wis" },
        { "insight", "wis" },
        { "medicine", "wis" },
        { "perception", "wis" },
        { "survival", "wis" },

        // CHA
        { "deception", "cha" },
        { "intimidation", "cha" },
        { "performance", "cha" },
        { "persuasion", "cha" }
    };

    public static readonly string[] AllAbilities = { "str", "dex", "con", "int", "wis", "cha" };

    public static readonly string[] AllSkills =
    {
        "acrobatics","animal_handling","arcana","athletics","deception","history","insight","intimidation",
        "investigation","medicine","nature","perception","performance","persuasion","religion",
        "sleight_of_hand","stealth","survival"
    };

    public static string PrettyAbility(string a)
    {
        switch ((a ?? "").ToLowerInvariant())
        {
            case "str": return "Strength";
            case "dex": return "Dexterity";
            case "con": return "Constitution";
            case "int": return "Intelligence";
            case "wis": return "Wisdom";
            case "cha": return "Charisma";
            default: return a ?? "";
        }
    }

    public static string PrettySkill(string s)
    {
        s = (s ?? "").ToLowerInvariant();
        switch (s)
        {
            case "animal_handling": return "Animal Handling";
            case "sleight_of_hand": return "Sleight of Hand";
            default:
                if (string.IsNullOrWhiteSpace(s)) return "";
                // Title-case-ish
                string[] parts = s.Split('_');
                for (int i = 0; i < parts.Length; i++)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                return string.Join(" ", parts);
        }
    }
}
