using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text qtyText;

    public void Set(InventoryItem item)
    {
        if (item == null) return;

        if (labelText != null) labelText.text = item.displayName ?? item.itemId ?? "";
        if (qtyText != null) qtyText.text = (item.quantity > 1) ? $"x{item.quantity}" : "";

        if (iconImage != null)
        {
            iconImage.sprite = null;

            if (!string.IsNullOrWhiteSpace(item.iconSprite))
            {
                // Resources/Pictures/ItemIcons/<iconSprite>.png
                var sp = Resources.Load<Sprite>($"Pictures/ItemIcons/{item.iconSprite}");
                if (sp != null) iconImage.sprite = sp;
            }

            iconImage.enabled = (iconImage.sprite != null);
        }
    }
}
