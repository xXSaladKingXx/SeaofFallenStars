using System;
using System.Collections.Generic;

// This file defines the data model for a character sheet.  It mirrors the
// original CharacterSheetData from the upstream repository but adds support
// for timeline entries.  Each character now stores a list of timeline
// event identifiers rather than embedding event data directly.  When a
// character participates in a timeline event, its id is added to the
// timelineEntries array.

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

    /// <summary>
    /// The character type determines which sections of data are visible in the
    /// authoring UI.  Defaults to <see cref="CharacterType.Default"/>.  See
    /// <see cref="CharacterType"/> for definitions.
    /// </summary>
    public CharacterType characterType = CharacterType.Default;

    /// <summary>
    /// Relationships defined on this character.  Each entry references a
    /// relationship type from the global <see cref="RelationshipCatalogDataModel"/>
    /// and the target entities involved in the relationship.  This new
    /// collection supersedes the legacy <see cref="relationships"/> object,
    /// although the legacy fields are retained for backward compatibility.
    /// </summary>
    public List<RelationshipEntry> relationshipEntries = new List<RelationshipEntry>();

    // Relationships + Feudal
    public Relationships relationships = new Relationships();
    public FeudalInfo feudal = new FeudalInfo();

    // Spells (page 3)
    public SpellcastingInfo spellcasting = new SpellcastingInfo();

    public string notes;

    /// <summary>
    /// List of timeline event identifiers associated with this character.  Each entry
    /// references an event defined in the global TimelineCatalog.  Characters
    /// should not embed full timeline entries in their JSON; instead they track
    /// participation by storing the event id here.  This array is initialised
    /// to an empty array to avoid null reference errors when saving or editing.
    /// </summary>
    public string[] timelineEntries = Array.Empty<string>();

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

/// <summary>
/// Represents a reference to a relationship defined in the global
/// RelationshipCatalog.  Each entry stores the identifier of the
/// relationship type along with a list of participant entity IDs.
/// The order of participants corresponds to their role in the
/// relationship; for two‑way relationships the order is arbitrary, for
/// one‑way relationships the first segment (up to <see cref="participantSplitIndex"/>)
/// represents side A and the remainder represents side B.
/// </summary>
[Serializable]
public class RelationshipEntry
{
    /// <summary>
    /// The identifier of the relationship type, referencing
    /// <see cref="RelationshipCatalogDataModel"/>.
    /// </summary>
    public string relationshipTypeId;

    /// <summary>
    /// The identifiers of the participants involved in this relationship.
    /// These may be character IDs, settlement IDs or other entity IDs.
    /// </summary>
    public List<string> participantIds = new List<string>();

    /// <summary>
    /// For one‑way or directional relationships, specifies how many
    /// participants belong to the first side (side A).  Participants
    /// with index less than <c>participantSplitIndex</c> are considered
    /// side A, and the rest are side B.  For two‑way relationships this
    /// value is ignored.
    /// </summary>
    public int participantSplitIndex = 1;
}

/// <summary>
/// Enumeration of character types used to determine which sections of the
/// character sheet are shown in the authoring UI.  Increasing values
/// correspond to characters with more detailed data.
/// </summary>
public enum CharacterType
{
    /// <summary>
    /// Minimal character information: ID, name, race, birth/death, relationships
    /// and optional timeline entries.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Non‑player character.  Includes appearance, voice sample, proficiencies,
    /// skills, personality, relationships and feudal information.
    /// </summary>
    NPC = 1,
    /// <summary>
    /// Combat‑oriented NPC.  Includes the minimal sections plus appearance,
    /// proficiencies, skills and optionally personality.  Provides a toggle
    /// to enable spellcasting.
    /// </summary>
    CombatNPC = 2,
    /// <summary>
    /// Player character.  Includes all available sections (full detail).
    /// </summary>
    PC = 3
}

[Serializable]
public class FeudalInfo
{
    public bool isFeudal = false;
    /// <summary>
    /// Titles held by this character.  Each entry should be the identifier
    /// of a settlement or holding where the character rules.  This replaces
    /// the previous rank/rulesSettlementId fields.
    /// </summary>
    public string[] titles;

    /// <summary>
    /// Identifiers of settlements for which this character is the liege.
    /// </summary>
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