using System;
using UnityEngine;

/// <summary>
/// One entry in the <see cref="PlayerInventory"/>: an item plus how many of it
/// are held in this stack. Non-stackable items always live in their own slot
/// with a count of 1. Serializable so the inventory contents are visible (and
/// hand-editable) in the Inspector for debugging without any UI.
/// </summary>
[Serializable]
public class ItemSlot
{
    public ItemData item;
    [Min(1)] public int count = 1;

    public ItemSlot(ItemData item, int count)
    {
        this.item = item;
        this.count = count;
    }

    /// <summary>Spare room left in this stack before hitting the item's max.</summary>
    public int FreeSpace => item != null && item.isStackable
        ? Mathf.Max(0, item.maxStack - count)
        : 0;
}
