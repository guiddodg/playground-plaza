using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single inventory slot in the sticker/candy UI. Shows an item icon and a
/// stack count badge. Populated by <see cref="InventoryUI"/>.
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countLabel;

    public ItemData Item { get; private set; }

    public void SetItem(ItemData item, int count)
    {
        Item = item;

        if (icon != null)
        {
            icon.sprite = item != null ? item.icon : null;
            icon.enabled = item != null && item.icon != null;
        }

        if (countLabel != null)
        {
            bool showCount = item != null && item.isStackable && count > 1;
            countLabel.text = showCount ? count.ToString() : string.Empty;
            countLabel.enabled = showCount;
        }
    }

    public void Clear()
    {
        SetItem(null, 0);
    }
}
