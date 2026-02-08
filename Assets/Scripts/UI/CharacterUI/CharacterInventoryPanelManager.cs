using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInventoryPanelManager : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Button closeButton;

    [Header("Grid")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private InventoryItemSlotUI itemSlotPrefab;

    private readonly List<GameObject> _spawned = new List<GameObject>();

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

    public void Initialize(InventoryItem[] items)
    {
        Clear();

        if (gridParent == null || itemSlotPrefab == null)
            return;

        if (items == null || items.Length == 0)
            return;

        foreach (var it in items)
        {
            if (it == null) continue;

            var slot = Instantiate(itemSlotPrefab, gridParent);
            slot.Set(it);
            _spawned.Add(slot.gameObject);
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
