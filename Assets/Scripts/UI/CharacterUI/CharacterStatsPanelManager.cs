using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatsPanelManager : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Button closeButton;

    [Header("Top Nav")]
    [SerializeField] private Button page1Button;
    [SerializeField] private Button page2Button;
    [SerializeField] private Button page3Button;

    [Header("Pages")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;
    [SerializeField] private GameObject page3;

    [Header("Spawn Prefabs")]
    [SerializeField] private CharacterInventoryPanelManager inventoryPanelPrefab;
    [SerializeField] private CharacterStatsPanelManager characterStatsPanelPrefab; // used when opening relationships

    [Header("Spawn Parents")]
    [SerializeField] private Transform spawnedPanelsParent; // usually the same Canvas parent as this panel

    [Header("Spawn Offsets")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(50f, 0f);

    // -------------------------
    // PAGE 1 (sheet front)
    // -------------------------
    [Header("Page 1 - Header")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text classLevelText;
    [SerializeField] private TMP_Text backgroundText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text raceText;
    [SerializeField] private TMP_Text alignmentText;
    [SerializeField] private TMP_Text xpText;

    [SerializeField] private Image portraitImage;

    [Header("Page 1 - Proficiency/Combat")]
    [SerializeField] private TMP_Text proficiencyBonusText;
    [SerializeField] private TMP_Text armorClassText;
    [SerializeField] private TMP_Text initiativeText;
    [SerializeField] private TMP_Text speedText;

    [SerializeField] private TMP_Text hpMaxText;
    [SerializeField] private TMP_Text hpCurrentText;
    [SerializeField] private TMP_Text hpTempText;

    [SerializeField] private TMP_Text hitDiceText;

    [Header("Page 1 - Ability Scores (score + mod)")]
    [SerializeField] private TMP_Text strScoreText; [SerializeField] private TMP_Text strModText;
    [SerializeField] private TMP_Text dexScoreText; [SerializeField] private TMP_Text dexModText;
    [SerializeField] private TMP_Text conScoreText; [SerializeField] private TMP_Text conModText;
    [SerializeField] private TMP_Text intScoreText; [SerializeField] private TMP_Text intModText;
    [SerializeField] private TMP_Text wisScoreText; [SerializeField] private TMP_Text wisModText;
    [SerializeField] private TMP_Text chaScoreText; [SerializeField] private TMP_Text chaModText;

    [Header("Page 1 - Saving Throws + Skills (dynamic rows)")]
    [SerializeField] private Transform savingThrowsListParent;
    [SerializeField] private Transform skillsListParent;
    [SerializeField] private ProficiencyRowUI proficiencyRowPrefab;

    [Header("Page 1 - Proficiencies text blocks")]
    [SerializeField] private TMP_Text armorProfsText;
    [SerializeField] private TMP_Text weaponProfsText;
    [SerializeField] private TMP_Text toolProfsText;
    [SerializeField] private TMP_Text languagesText;

    [Header("Page 1 - Attacks (dynamic rows)")]
    [SerializeField] private Transform attacksListParent;
    [SerializeField] private AttackRowUI attackRowPrefab;

    [Header("Page 1 - Features")]
    [SerializeField] private TMP_Text featuresText;

    [Header("Page 1 - Inventory")]
    [SerializeField] private Button inventoryButton;

    // -------------------------
    // PAGE 2 (appearance/backstory/relationships)
    // -------------------------
    [Header("Page 2 - Life / Appearance")]
    [SerializeField] private TMP_Text birthDateText;
    [SerializeField] private TMP_Text aliveText;
    [SerializeField] private TMP_Text deathDateText;

    [SerializeField] private TMP_Text ageText;
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text eyesText;
    [SerializeField] private TMP_Text skinText;
    [SerializeField] private TMP_Text hairText;

    [Header("Page 2 - Personality + Backstory")]
    [SerializeField] private TMP_Text personalityTraitsText;
    [SerializeField] private TMP_Text idealsText;
    [SerializeField] private TMP_Text bondsText;
    [SerializeField] private TMP_Text flawsText;
    [SerializeField] private TMP_Text backstoryText;
    [SerializeField] private TMP_Text notesText;

    [Header("Page 2 - Relationships (dynamic rows)")]
    [SerializeField] private Transform relationshipsListParent;
    [SerializeField] private RelationshipRowUI relationshipRowPrefab;

    [Header("Page 2 - Feudal")]
    [SerializeField] private GameObject feudalSectionRoot;
    [SerializeField] private TMP_Text feudalRankText;
    [SerializeField] private Button rulesSettlementButton;
    [SerializeField] private TMP_Text rulesSettlementLabelText;
    [SerializeField] private TMP_Text feudalVassalsText;

    // -------------------------
    // PAGE 3 (spells)
    // -------------------------
    [Header("Page 3 - Spellcasting Header")]
    [SerializeField] private GameObject spellcastingRoot;
    [SerializeField] private TMP_Text spellcastingClassText;
    [SerializeField] private TMP_Text spellcastingAbilityText;
    [SerializeField] private TMP_Text spellSaveDcText;
    [SerializeField] private TMP_Text spellAttackBonusText;

    [Header("Page 3 - Spell Levels Container")]
    [SerializeField] private Transform spellLevelsParent;
    [SerializeField] private SpellLevelSectionUI spellLevelSectionPrefab;

    // -------------------------
    // Internal
    // -------------------------
    private readonly List<GameObject> _spawnedRows = new List<GameObject>();
    private CharacterSheetData _data;

    private void Awake()
    {
        if (GetComponent<MapInputBlocker>() == null)
            gameObject.AddComponent<MapInputBlocker>();

        if (spawnedPanelsParent == null)
            spawnedPanelsParent = transform.parent;

        if (characterStatsPanelPrefab == null)
            characterStatsPanelPrefab = this;

        WireCoreButtons();
        ShowPage(1);
    }

    private void WireCoreButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }

        if (page1Button != null)
        {
            page1Button.onClick.RemoveAllListeners();
            page1Button.onClick.AddListener(() => ShowPage(1));
        }

        if (page2Button != null)
        {
            page2Button.onClick.RemoveAllListeners();
            page2Button.onClick.AddListener(() => ShowPage(2));
        }

        if (page3Button != null)
        {
            page3Button.onClick.RemoveAllListeners();
            page3Button.onClick.AddListener(() => ShowPage(3));
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(OpenInventoryPanel);
        }
    }

    public void Initialize(string characterId)
    {
        _data = CharacterDataLoader.TryLoad(characterId);

        if (_data == null)
        {
            Debug.LogWarning($"[CharacterStatsPanelManager] No character JSON found for '{characterId}'.");
            if (nameText != null) nameText.text = characterId;
            return;
        }

        BringToFront();
        RebuildAll();
    }

    private void RebuildAll()
    {
        ClearSpawnedRows();

        int profBonus = Dnd5eRules.ProficiencyBonus(_data.level);

        // --- Header
        SetText(nameText, _data.GetBestDisplayName());
        SetText(playerNameText, _data.playerName);
        SetText(classLevelText, BuildClassLevel());
        SetText(backgroundText, _data.background);
        SetText(raceText, BuildRace());
        SetText(alignmentText, _data.alignment);
        SetText(xpText, (_data.experiencePoints > 0) ? _data.experiencePoints.ToString("N0") : "");

        // Portrait
        ApplyPortrait();

        // --- Proficiency / Combat
        SetText(proficiencyBonusText, $"+{profBonus}");
        SetText(armorClassText, _data.combat != null ? _data.combat.armorClass.ToString() : "");
        SetText(speedText, _data.combat != null ? _data.combat.speedFeet.ToString() : "");

        int dexMod = GetAbilityMod("dex");
        int init = dexMod + (_data.combat != null ? _data.combat.initiativeMisc : 0);
        SetText(initiativeText, FormatSigned(init));

        if (_data.combat != null)
        {
            SetText(hpMaxText, _data.combat.maxHp.ToString());
            SetText(hpCurrentText, _data.combat.currentHp.ToString());
            SetText(hpTempText, (_data.combat.tempHp > 0) ? _data.combat.tempHp.ToString() : "");
            SetText(hitDiceText, BuildHitDice());
        }

        // --- Abilities
        ApplyAbility("str", _data.abilities.str, strScoreText, strModText);
        ApplyAbility("dex", _data.abilities.dex, dexScoreText, dexModText);
        ApplyAbility("con", _data.abilities.con, conScoreText, conModText);
        ApplyAbility("int", _data.abilities.intel, intScoreText, intModText);
        ApplyAbility("wis", _data.abilities.wis, wisScoreText, wisModText);
        ApplyAbility("cha", _data.abilities.cha, chaScoreText, chaModText);

        // --- Saving Throws (rows)
        BuildSavingThrows(profBonus);

        // --- Skills (rows)
        BuildSkills(profBonus);

        // --- Proficiencies text blocks
        if (_data.proficiencies != null)
        {
            SetText(armorProfsText, JoinLines(_data.proficiencies.armor));
            SetText(weaponProfsText, JoinLines(_data.proficiencies.weapons));
            SetText(toolProfsText, JoinLines(_data.proficiencies.tools));
            SetText(languagesText, JoinLines(_data.proficiencies.languages));
        }

        // --- Attacks (rows)
        BuildAttacks();

        // --- Features
        SetText(featuresText, JoinLines(_data.featuresAndTraits));

        // PAGE 2 (life/appearance/personality/backstory/relationships/feudal)
        ApplyPage2();

        // PAGE 3 (spells)
        ApplyPage3();
    }

    private void ApplyPage2()
    {
        // Life
        if (_data.life != null)
        {
            SetText(birthDateText, _data.life.birthDate);
            SetText(aliveText, _data.life.isAlive ? "Alive" : "Deceased");
            SetText(deathDateText, _data.life.isAlive ? "" : _data.life.deathDate);
        }

        // Appearance
        if (_data.appearance != null)
        {
            SetText(ageText, _data.appearance.age);
            SetText(heightText, _data.appearance.height);
            SetText(weightText, _data.appearance.weight);
            SetText(eyesText, _data.appearance.eyes);
            SetText(skinText, _data.appearance.skin);
            SetText(hairText, _data.appearance.hair);
        }

        // Personality + Backstory + Notes
        if (_data.personality != null)
        {
            SetText(personalityTraitsText, _data.personality.traits);
            SetText(idealsText, _data.personality.ideals);
            SetText(bondsText, _data.personality.bonds);
            SetText(flawsText, _data.personality.flaws);
        }

        SetText(backstoryText, _data.backstory);
        SetText(notesText, _data.notes);

        // Relationships
        BuildRelationships();

        // Feudal
        ApplyFeudal();
    }

    private void ApplyPage3()
    {
        bool hasSpellcasting = _data.spellcasting != null && _data.spellcasting.isCaster;

        if (spellcastingRoot != null) spellcastingRoot.SetActive(hasSpellcasting);
        if (page3Button != null) page3Button.gameObject.SetActive(true); // keep visible as "page 3", but you can hide if you want

        if (!hasSpellcasting)
        {
            // If you prefer to hide page 3 entirely when no spells:
            // if (page3Button != null) page3Button.gameObject.SetActive(false);
            // if (page3 != null) page3.SetActive(false);
            return;
        }

        SetText(spellcastingClassText, _data.spellcasting.spellcastingClass);
        SetText(spellcastingAbilityText, Dnd5eRules.PrettyAbility(_data.spellcasting.spellcastingAbility));
        SetText(spellSaveDcText, _data.spellcasting.spellSaveDc > 0 ? _data.spellcasting.spellSaveDc.ToString() : "");
        SetText(spellAttackBonusText, _data.spellcasting.spellAttackBonus != 0 ? FormatSigned(_data.spellcasting.spellAttackBonus) : "");

        if (spellLevelsParent == null || spellLevelSectionPrefab == null)
            return;

        // Spawn one section per level present in JSON
        if (_data.spellcasting.spellLevels == null) return;

        foreach (var lvl in _data.spellcasting.spellLevels)
        {
            if (lvl == null) continue;

            var section = Instantiate(spellLevelSectionPrefab, spellLevelsParent);
            section.Set(lvl);
            _spawnedRows.Add(section.gameObject);
        }
    }

    private void ApplyFeudal()
    {
        bool isFeudal = _data.feudal != null && _data.feudal.isFeudal;

        if (feudalSectionRoot != null)
            feudalSectionRoot.SetActive(isFeudal);

        if (!isFeudal)
            return;

        SetText(feudalRankText, _data.feudal.rank);

        string settlementId = _data.feudal.rulesSettlementId;
        string settlementName = SettlementNameResolver.Resolve(settlementId);

        if (rulesSettlementLabelText != null)
            rulesSettlementLabelText.text = settlementName;

        if (rulesSettlementButton != null)
        {
            rulesSettlementButton.onClick.RemoveAllListeners();
            rulesSettlementButton.onClick.AddListener(() =>
            {
                BringToFront();
                MapNavigationUtil.OpenSettlementById(settlementId);
            });
        }

        SetText(feudalVassalsText, JoinLines(_data.feudal.vassalSettlementIds));
    }

    // -------------------------
    // Builders
    // -------------------------

    private void BuildSavingThrows(int profBonus)
    {
        if (savingThrowsListParent == null || proficiencyRowPrefab == null)
            return;

        // Build lookup from JSON
        var map = new Dictionary<string, SavingThrowEntry>();
        if (_data.savingThrows != null)
        {
            foreach (var st in _data.savingThrows)
            {
                if (st == null || string.IsNullOrWhiteSpace(st.ability)) continue;
                map[st.ability.Trim().ToLowerInvariant()] = st;
            }
        }

        foreach (var a in Dnd5eRules.AllAbilities)
        {
            map.TryGetValue(a, out var entry);

            bool prof = entry != null && entry.proficient;
            int misc = entry != null ? entry.miscBonus : 0;

            int val = GetAbilityMod(a) + (prof ? profBonus : 0) + misc;

            var row = Instantiate(proficiencyRowPrefab, savingThrowsListParent);
            row.SetSavingThrow($"{Dnd5eRules.PrettyAbility(a)} Save", val, prof);
            _spawnedRows.Add(row.gameObject);
        }
    }

    private void BuildSkills(int profBonus)
    {
        if (skillsListParent == null || proficiencyRowPrefab == null)
            return;

        // Build lookup from JSON
        var map = new Dictionary<string, SkillEntry>();
        if (_data.skills != null)
        {
            foreach (var sk in _data.skills)
            {
                if (sk == null || string.IsNullOrWhiteSpace(sk.skillId)) continue;
                map[NormalizeSkillId(sk.skillId)] = sk;
            }
        }

        foreach (var skillId in Dnd5eRules.AllSkills)
        {
            map.TryGetValue(skillId, out var entry);

            SkillProficiencyLevel prof = entry != null ? entry.proficiency : SkillProficiencyLevel.None;
            int misc = entry != null ? entry.miscBonus : 0;

            string ability = Dnd5eRules.SkillToAbility.TryGetValue(skillId, out var a) ? a : "int";
            int mod = GetAbilityMod(ability);

            int mult = (prof == SkillProficiencyLevel.Expertise) ? 2 :
                       (prof == SkillProficiencyLevel.Proficient) ? 1 : 0;

            int val = mod + (mult * profBonus) + misc;

            var row = Instantiate(proficiencyRowPrefab, skillsListParent);
            row.Set(Dnd5eRules.PrettySkill(skillId), val, prof);
            _spawnedRows.Add(row.gameObject);
        }
    }

    private void BuildAttacks()
    {
        if (attacksListParent == null || attackRowPrefab == null)
            return;

        if (_data.attacks == null) return;

        foreach (var atk in _data.attacks)
        {
            if (atk == null) continue;
            var row = Instantiate(attackRowPrefab, attacksListParent);
            row.Set(atk);
            _spawnedRows.Add(row.gameObject);
        }
    }

    private void BuildRelationships()
    {
        if (relationshipsListParent == null || relationshipRowPrefab == null)
            return;

        if (_data.relationships == null) return;

        // helper spawner
        void AddGroup(string label, string[] ids)
        {
            if (ids == null) return;
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                SpawnRelationshipRow(label, id);
            }
        }

        AddGroup("Parent", _data.relationships.parents);
        AddGroup("Sibling", _data.relationships.siblings);
        AddGroup("Child", _data.relationships.children);

        if (!string.IsNullOrWhiteSpace(_data.relationships.spouse))
            SpawnRelationshipRow("Spouse", _data.relationships.spouse);

        if (_data.relationships.other != null)
        {
            foreach (var o in _data.relationships.other)
            {
                if (o == null || string.IsNullOrWhiteSpace(o.characterId)) continue;
                string label = string.IsNullOrWhiteSpace(o.relation) ? "Other" : o.relation;
                SpawnRelationshipRow(label, o.characterId);
            }
        }
    }

    private void SpawnRelationshipRow(string relationLabel, string characterId)
    {
        string display = CharacterNameResolver.Resolve(characterId);

        var row = Instantiate(relationshipRowPrefab, relationshipsListParent);
        row.Set(relationLabel, display, characterId, OpenCharacterFromRelationship);
        _spawnedRows.Add(row.gameObject);
    }

    private void OpenCharacterFromRelationship(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (characterStatsPanelPrefab == null)
        {
            Debug.LogWarning("[CharacterStatsPanelManager] characterStatsPanelPrefab is not assigned.");
            return;
        }

        Transform parent = spawnedPanelsParent != null ? spawnedPanelsParent : transform.parent;
        var clone = Instantiate(characterStatsPanelPrefab, parent);

        // Offset to the right
        var rt = clone.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition += spawnOffset;

        clone.BringToFront();
        clone.Initialize(characterId);
    }

    private void OpenInventoryPanel()
    {
        if (inventoryPanelPrefab == null)
        {
            Debug.LogWarning("[CharacterStatsPanelManager] inventoryPanelPrefab is not assigned.");
            return;
        }

        Transform parent = spawnedPanelsParent != null ? spawnedPanelsParent : transform.parent;
        var inv = Instantiate(inventoryPanelPrefab, parent);

        var rt = inv.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition += spawnOffset;

        inv.transform.SetAsLastSibling();
        inv.Initialize(_data != null ? _data.equipment : null);
    }

    // -------------------------
    // Utilities
    // -------------------------

    private void ShowPage(int pageIndex)
    {
        if (page1 != null) page1.SetActive(pageIndex == 1);
        if (page2 != null) page2.SetActive(pageIndex == 2);
        if (page3 != null) page3.SetActive(pageIndex == 3);

        BringToFront();
    }

    public void BringToFront()
    {
        transform.SetAsLastSibling();
    }

    private void ApplyPortrait()
    {
        if (portraitImage == null)
            return;

        portraitImage.sprite = null;
        portraitImage.enabled = false;

        string key = _data != null && _data.appearance != null ? _data.appearance.portraitSprite : null;
        if (string.IsNullOrWhiteSpace(key))
            return;

        // Resources/Pictures/Portraits/<portraitSprite>.png
        var sp = Resources.Load<Sprite>($"Pictures/Portraits/{key}");
        if (sp != null)
        {
            portraitImage.sprite = sp;
            portraitImage.enabled = true;
        }
    }

    private string BuildClassLevel()
    {
        string cls = _data.className ?? "";
        string sub = _data.subclassName ?? "";
        string lvl = _data.level > 0 ? _data.level.ToString() : "";

        if (!string.IsNullOrWhiteSpace(sub))
            return $"{cls} {lvl} ({sub})";

        return $"{cls} {lvl}".Trim();
    }

    private string BuildRace()
    {
        string r = _data.race ?? "";
        string sr = _data.subrace ?? "";
        if (!string.IsNullOrWhiteSpace(sr))
            return $"{r} ({sr})";
        return r;
    }

    private string BuildHitDice()
    {
        if (_data.combat == null) return "";
        if (string.IsNullOrWhiteSpace(_data.combat.hitDice)) return "";

        if (_data.combat.hitDiceUsed > 0)
            return $"{_data.combat.hitDice} (used {_data.combat.hitDiceUsed})";

        return _data.combat.hitDice;
    }

    private void ApplyAbility(string abilityId, int score, TMP_Text scoreText, TMP_Text modText)
    {
        SetText(scoreText, score.ToString());
        SetText(modText, FormatSigned(Dnd5eRules.AbilityMod(score)));
    }

    private int GetAbilityScore(string abilityId)
    {
        abilityId = (abilityId ?? "").ToLowerInvariant();
        switch (abilityId)
        {
            case "str": return _data.abilities.str;
            case "dex": return _data.abilities.dex;
            case "con": return _data.abilities.con;
            case "int": return _data.abilities.intel;
            case "wis": return _data.abilities.wis;
            case "cha": return _data.abilities.cha;
            default: return 10;
        }
    }

    private int GetAbilityMod(string abilityId) => Dnd5eRules.AbilityMod(GetAbilityScore(abilityId));

    private static void SetText(TMP_Text t, string v)
    {
        if (t != null) t.text = v ?? "";
    }

    private static string JoinLines(string[] arr)
    {
        if (arr == null || arr.Length == 0) return "";
        return string.Join("\n", arr);
    }

    private static string FormatSigned(int v) => (v >= 0) ? $"+{v}" : v.ToString();

    private static string NormalizeSkillId(string s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        s = s.Replace(" ", "_");
        return s;
    }

    private void ClearSpawnedRows()
    {
        for (int i = 0; i < _spawnedRows.Count; i++)
            if (_spawnedRows[i] != null)
                Destroy(_spawnedRows[i]);

        _spawnedRows.Clear();
    }
}
