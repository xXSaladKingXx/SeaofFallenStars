using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharactersPanelManager : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private TMP_Text titleText;

    [Header("List")]
    [SerializeField] private Transform listParent;
    [SerializeField] private Button listItemButtonPrefab;

    [Header("Dropdown List (Optional)")]
    [Tooltip("If assigned, this dropdown will be used instead of spawning button rows.")]
    [SerializeField] private TMP_Dropdown listDropdown;

    // Backing list for dropdown selections
    private readonly List<string> _characterIds = new List<string>();

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private Action<string> _onCharacterSelected;

    private void Awake()
    {
        if (GetComponent<MapInputBlocker>() == null)
            gameObject.AddComponent<MapInputBlocker>();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }
    }

    public void Initialize(string locationDisplayName, string[] characterIds, Action<string> onCharacterSelected)
    {
        _onCharacterSelected = onCharacterSelected;

        if (titleText != null)
            titleText.text = $"{locationDisplayName} — Characters";

        Rebuild(characterIds);
    }

    private void Rebuild(string[] characterIds)
    {
        Clear();

        // If a dropdown is provided, use it instead of spawning buttons
        if (listDropdown != null)
        {
            listDropdown.onValueChanged.RemoveAllListeners();
            listDropdown.options.Clear();
            _characterIds.Clear();

            if (characterIds == null || characterIds.Length == 0)
            {
                listDropdown.options.Add(new TMP_Dropdown.OptionData("No characters found."));
                listDropdown.value = 0;
                listDropdown.interactable = false;
                listDropdown.RefreshShownValue();
                return;
            }

            listDropdown.options.Add(new TMP_Dropdown.OptionData("Select a character..."));
            foreach (var id in characterIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                string trimmed = id.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                _characterIds.Add(trimmed);
                string displayName = CharacterNameResolver.Resolve(trimmed);
                if (string.IsNullOrWhiteSpace(displayName)) displayName = trimmed;
                listDropdown.options.Add(new TMP_Dropdown.OptionData(displayName));
            }

            listDropdown.value = 0;
            listDropdown.interactable = true;
            listDropdown.RefreshShownValue();

            listDropdown.onValueChanged.AddListener(idx =>
            {
                if (idx <= 0) return;
                int listIndex = idx - 1;
                if (listIndex < 0 || listIndex >= _characterIds.Count) return;
                string cid = _characterIds[listIndex];
                _onCharacterSelected?.Invoke(cid);
                // Reset to placeholder
                listDropdown.value = 0;
                listDropdown.RefreshShownValue();
            });

            return;
        }

        // Fallback to old list panel
        if (listParent == null || listItemButtonPrefab == null)
        {
            Debug.LogWarning("[CharactersPanelManager] listParent or listItemButtonPrefab is not assigned.");
            return;
        }

        if (characterIds == null || characterIds.Length == 0)
            return;

        foreach (var id in characterIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var go = Instantiate(listItemButtonPrefab, listParent).gameObject;
            _spawned.Add(go);

            var btn = go.GetComponent<Button>();
            var label = go.GetComponentInChildren<TMP_Text>(true);

            string displayName = CharacterNameResolver.Resolve(id);

            if (label != null)
                label.text = displayName;

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                string capturedId = id;
                btn.onClick.AddListener(() => _onCharacterSelected?.Invoke(capturedId));
            }
        }
    }

    private void Clear()
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null)
                Destroy(_spawned[i]);

        _spawned.Clear();
    }
}
