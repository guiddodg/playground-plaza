using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure inventory logic for the player — no UI. Holds a dynamic list of
/// <see cref="ItemSlot"/> stacks plus a set of equipment slots keyed by
/// <see cref="EquipSlot"/>. Lives as a component on the Player GameObject and
/// exposes a lightweight <see cref="Instance"/> accessor so pickups, crafting
/// and UI can reach it without hard references.
///
/// All mutations raise events so presentation layers (e.g. InventoryUI) can
/// react without polling. Adding obeys each item's stacking rules
/// (<see cref="ItemData.isStackable"/> / <see cref="ItemData.maxStack"/>).
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Contents (runtime — visible for debugging)")]
    [SerializeField] private List<ItemSlot> slots = new List<ItemSlot>();

    // Equipment is runtime-only state; Unity can't serialize a Dictionary, and
    // it's reconstructed from gameplay rather than authored in the Inspector.
    private readonly Dictionary<EquipSlot, ItemData> equipment = new Dictionary<EquipSlot, ItemData>();

    /// <summary>Read-only view of the current stacks, for UI to render.</summary>
    public IReadOnlyList<ItemSlot> Slots => slots;

    // --- Events --------------------------------------------------------------
    /// <summary>Fired when items are added. Args: item, amount actually added.</summary>
    public event Action<ItemData, int> OnItemAdded;
    /// <summary>Fired when items are removed. Args: item, amount removed.</summary>
    public event Action<ItemData, int> OnItemRemoved;
    /// <summary>Fired when an item is equipped. Args: slot, item.</summary>
    public event Action<EquipSlot, ItemData> OnItemEquipped;
    /// <summary>Fired when an item is unequipped. Args: slot, item.</summary>
    public event Action<EquipSlot, ItemData> OnItemUnequipped;
    /// <summary>Catch-all fired after any mutation; handy for a blanket UI refresh.</summary>
    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerInventory] A second instance on '{name}' was disabled; only one is allowed.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // --- Queries -------------------------------------------------------------

    /// <summary>Total count of <paramref name="item"/> across all stacks.</summary>
    public int CountOf(ItemData item)
    {
        if (item == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].item == item) total += slots[i].count;
        return total;
    }

    /// <summary>True if the inventory holds at least <paramref name="amount"/> of the item.</summary>
    public bool HasItem(ItemData item, int amount = 1) => CountOf(item) >= amount;

    /// <summary>Currently equipped item in a slot, or null if empty.</summary>
    public ItemData GetEquipped(EquipSlot slot)
        => equipment.TryGetValue(slot, out var item) ? item : null;

    // --- Mutations -----------------------------------------------------------

    /// <summary>
    /// Adds <paramref name="amount"/> of an item, respecting stacking rules.
    /// Returns the amount actually added (always equal to <paramref name="amount"/>
    /// here since the inventory has no capacity cap yet — kept as the return value
    /// so a future bounded inventory can report partial adds).
    /// </summary>
    public int AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return 0;

        int remaining = amount;

        if (item.isStackable)
        {
            // Top up existing stacks first.
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].item != item) continue;
                int add = Mathf.Min(slots[i].FreeSpace, remaining);
                slots[i].count += add;
                remaining -= add;
            }
            // Spill the rest into new stacks.
            while (remaining > 0)
            {
                int add = Mathf.Min(item.maxStack, remaining);
                slots.Add(new ItemSlot(item, add));
                remaining -= add;
            }
        }
        else
        {
            // Non-stackable: one slot per unit.
            for (int i = 0; i < amount; i++)
                slots.Add(new ItemSlot(item, 1));
            remaining = 0;
        }

        int added = amount - remaining;
        if (added > 0)
        {
            OnItemAdded?.Invoke(item, added);
            OnChanged?.Invoke();
        }
        return added;
    }

    /// <summary>
    /// Removes <paramref name="amount"/> of an item. Returns false (and changes
    /// nothing) if the inventory doesn't hold enough.
    /// </summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        if (CountOf(item) < amount) return false;

        int remaining = amount;
        // Walk backwards so emptied stacks can be removed without skipping entries.
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (slots[i].item != item) continue;
            int take = Mathf.Min(slots[i].count, remaining);
            slots[i].count -= take;
            remaining -= take;
            if (slots[i].count <= 0)
                slots.RemoveAt(i);
        }

        OnItemRemoved?.Invoke(item, amount);
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Equips an Equipable item from the inventory into its target slot. Any item
    /// already in that slot is returned to the inventory. Returns false if the
    /// item isn't equipable, has no target slot, or isn't held.
    /// </summary>
    public bool EquipItem(ItemData item)
    {
        if (item == null) return false;
        if (item.itemType != ItemType.Equipable || item.equipSlot == EquipSlot.None)
        {
            Debug.LogWarning($"[PlayerInventory] '{item.itemName}' is not equipable.", this);
            return false;
        }
        if (!HasItem(item, 1)) return false;

        EquipSlot slot = item.equipSlot;
        // Return whatever currently occupies the slot to the inventory.
        UnequipItem(slot);

        RemoveItem(item, 1);
        equipment[slot] = item;

        OnItemEquipped?.Invoke(slot, item);
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Unequips whatever is in a slot, returning it to the inventory. Returns
    /// false if the slot was empty.
    /// </summary>
    public bool UnequipItem(EquipSlot slot)
    {
        if (slot == EquipSlot.None) return false;
        if (!equipment.TryGetValue(slot, out var item) || item == null) return false;

        equipment.Remove(slot);
        AddItem(item, 1);

        OnItemUnequipped?.Invoke(slot, item);
        OnChanged?.Invoke();
        return true;
    }

#if UNITY_EDITOR
    [Header("Debug (Editor only)")]
    [Tooltip("Item used by the context-menu test actions below.")]
    [SerializeField] private ItemData debugItem;
    [SerializeField, Min(1)] private int debugAmount = 1;

    [ContextMenu("Debug/Add debugItem")]
    private void DebugAdd()
    {
        int added = AddItem(debugItem, debugAmount);
        Debug.Log($"[PlayerInventory] Added {added}x {debugItem?.itemName}. Total now {CountOf(debugItem)}.", this);
    }

    [ContextMenu("Debug/Remove debugItem")]
    private void DebugRemove()
    {
        bool ok = RemoveItem(debugItem, debugAmount);
        Debug.Log($"[PlayerInventory] Remove {debugAmount}x {debugItem?.itemName} -> {ok}. Total now {CountOf(debugItem)}.", this);
    }

    [ContextMenu("Debug/Equip debugItem")]
    private void DebugEquip()
    {
        bool ok = EquipItem(debugItem);
        Debug.Log($"[PlayerInventory] Equip {debugItem?.itemName} -> {ok}.", this);
    }
#endif
}
