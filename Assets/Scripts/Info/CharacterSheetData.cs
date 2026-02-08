using System;
using System.Collections.Generic;

[Serializable]
public class CharacterSheetData
{
    // Identity
    public string characterId;
    public string displayName;
    public string playerName;

    // Basics
    public string className;
    public string subclassName;
    public string background;
    public string race;
    public string subrace;
    public int level;
    public string alignment;
    public int experiencePoints;

    public LifeInfo life = new LifeInfo();
    public AppearanceInfo appearance = new AppearanceInfo();

    public AbilityScores abilities = new AbilityScores();

    // Saves/Skills
    public SavingThrowEntry[] savingThrows;
    public SkillEntry[] skills;

    // Combat + HP
    public CombatInfo combat = new CombatInfo();

    // Proficiencies
    public ProficiencyInfo proficiencies = new ProficiencyInfo();

    // Attacks / Equipment / Features
    public AttackEntry[] attacks;
    public InventoryItem[] equipment;
    public string[] featuresAndTraits;

    // Personality / Backstory
    public PersonalityInfo personality = new PersonalityInfo();
    public string backstory;

    // Relationships + Feudal
    public Relationships relationships = new Relationships();
    public FeudalInfo feudal = new FeudalInfo();

    // Spells (page 3)
    public SpellcastingInfo spellcasting = new SpellcastingInfo();

    public string notes;

    public string GetBestDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
        if (!string.IsNullOrWhiteSpace(characterId)) return characterId;
        return "Unknown";
    }
}

[Serializable]
public class LifeInfo
{
    public string birthDate;
    public bool isAlive = true;
    public string deathDate;
}

[Serializable]
public class AppearanceInfo
{
    public string age;
    public string height;
    public string weight;
    public string eyes;
    public string skin;
    public string hair;

    // Resources path name (no extension), e.g. "azoun_matarys_iv"
    public string portraitSprite;
}

[Serializable]
public class AbilityScores
{
    public int str = 10;
    public int dex = 10;
    public int con = 10;
    public int intel = 10;
    public int wis = 10;
    public int cha = 10;
}

[Serializable]
public class CombatInfo
{
    public int armorClass;
    public int initiativeMisc;
    public int speedFeet;

    public string hitDice;      // e.g. "5d10"
    public int hitDiceUsed;

    public int maxHp;
    public int currentHp;
    public int tempHp;
}

[Serializable]
public class SavingThrowEntry
{
    // "str","dex","con","int","wis","cha"
    public string ability;
    public bool proficient;
    public int miscBonus;
}

public enum SkillProficiencyLevel
{
    None = 0,
    Proficient = 1,
    Expertise = 2
}

[Serializable]
public class SkillEntry
{
    // e.g. "athletics","perception","stealth" (lowercase recommended)
    public string skillId;
    public SkillProficiencyLevel proficiency = SkillProficiencyLevel.None;
    public int miscBonus;
}

[Serializable]
public class ProficiencyInfo
{
    public string[] armor;
    public string[] weapons;
    public string[] tools;
    public string[] languages;
}

[Serializable]
public class AttackEntry
{
    public string name;
    public int attackBonus;
    public string damage;      // "1d8+3"
    public string damageType;  // "Slashing"
    public string notes;
}

[Serializable]
public class InventoryItem
{
    public string itemId;
    public string displayName;
    public int quantity = 1;

    // Resources icon name (no extension)
    public string iconSprite;

    public string description;
}

[Serializable]
public class PersonalityInfo
{
    public string traits;
    public string ideals;
    public string bonds;
    public string flaws;
}

[Serializable]
public class Relationships
{
    public string[] parents;
    public string[] siblings;
    public string[] children;
    public string spouse;

    public RelationshipOther[] other;
}

[Serializable]
public class RelationshipOther
{
    public string relation;     // e.g. "Liege", "Mentor"
    public string characterId;
}

[Serializable]
public class FeudalInfo
{
    public bool isFeudal = false;
    public string rank;
    public string rulesSettlementId;
    public string[] vassalSettlementIds;
}

[Serializable]
public class SpellcastingInfo
{
    public bool isCaster = false;

    public string spellcastingClass;   // e.g. "Wizard"
    public string spellcastingAbility; // "int","wis","cha"
    public int spellSaveDc;
    public int spellAttackBonus;

    public SpellLevelBlock[] spellLevels; // includes cantrips as level 0
}

[Serializable]
public class SpellLevelBlock
{
    public int level;           // 0..9
    public int slotsMax;
    public int slotsUsed;
    public SpellEntry[] spells;
}

[Serializable]
public class SpellEntry
{
    public string name;
    public bool prepared;
    public string notes;
}
