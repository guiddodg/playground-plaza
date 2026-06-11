using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds the inventory grid in the sticker/candy UI style. Instantiates one
/// <see cref="InventorySlotUI"/> per slot inside a GridLayoutGroup, and lets the
/// player toggle the wood-framed panel open/closed. While open it engages
/// <see cref="GameplayInputLock"/> so the player and camera stay frozen.
///
/// The grid is a view over the runtime <see cref="PlayerInventory"/>: it reads
/// from the model and re-renders whenever the model raises
/// <see cref="PlayerInventory.OnChanged"/>. The <see cref="entries"/> list is
/// only a debug seed pushed into the model on start, so the inventory can be
/// pre-populated for testing in the Editor.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public ItemData item;
        [Min(1)] public int count;
    }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private InventorySlotUI slotPrefab;
    [Tooltip("Inventory model to display. If left empty, PlayerInventory.Instance is used.")]
    [SerializeField] private PlayerInventory inventory;

    [Header("Layout")]
    [Tooltip("Minimum slots to draw. The grid grows if the inventory holds more stacks.")]
    [SerializeField] private int slotCount = 12;

    [Header("Debug seed")]
    [Tooltip("Items pushed into the inventory model on Start (editor testing). The model is the source of truth, not this list.")]
    [SerializeField] private List<Entry> entries = new List<Entry>();
    [Tooltip("If true, the entries above are added to the inventory on Start.")]
    [SerializeField] private bool seedEntriesOnStart = true;

    [Header("Input")]
    [Tooltip("Key that toggles the inventory open/closed (new Input System).")]
    [SerializeField] private Key toggleKey = Key.I;
    [SerializeField] private bool startOpen = false;

    private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private bool isOpen;
    private PlayerInventory bound;

    /// <summary>The inventory this UI reflects: the explicit ref, else the singleton.</summary>
    private PlayerInventory Inv => inventory != null ? inventory : PlayerInventory.Instance;

    private void Start()
    {
        BuildSlots();
        Bind();
        Refresh();
        SetOpen(startOpen);
    }

    private void OnDestroy()
    {
        if (bound != null)
            bound.OnChanged -= Refresh;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            // Read the panel's real state so the toggle stays in sync even if it
            // was closed another way (e.g. the on-screen close button).
            bool currentlyOpen = panelRoot != null && panelRoot.activeSelf;
            SetOpen(!currentlyOpen);
        }
    }

    private void Bind()
    {
        bound = Inv;
        if (bound == null)
            return;

        // Push the debug seed into the model, then re-render on every change.
        if (seedEntriesOnStart)
            foreach (var e in entries)
                if (e.item != null) bound.AddItem(e.item, e.count);

        bound.OnChanged += Refresh;
    }

    public void BuildSlots()
    {
        if (slotContainer == null || slotPrefab == null)
            return;

        foreach (var s in slots)
            if (s != null) Destroy(s.gameObject);
        slots.Clear();

        int modelCount = Inv != null ? Inv.Slots.Count : entries.Count;
        int total = Mathf.Max(slotCount, modelCount);
        for (int i = 0; i < total; i++)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.name = $"Slot_{i:00}";
            slots.Add(slot);
        }
    }

    public void Refresh()
    {
        var inv = Inv;

        // Grow the grid if the model now holds more stacks than we have slots for.
        if (inv != null && inv.Slots.Count > slots.Count)
            BuildSlots();

        for (int i = 0; i < slots.Count; i++)
        {
            if (inv != null)
            {
                if (i < inv.Slots.Count && inv.Slots[i].item != null)
                    slots[i].SetItem(inv.Slots[i].item, inv.Slots[i].count);
                else
                    slots[i].Clear();
            }
            else
            {
                // No model in the scene (e.g. previewing the panel alone): fall
                // back to the raw seed list so the grid isn't blank.
                if (i < entries.Count && entries[i].item != null)
                    slots[i].SetItem(entries[i].item, entries[i].count);
                else
                    slots[i].Clear();
            }
        }
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (panelRoot != null)
            panelRoot.SetActive(open);
        // The input lock + cursor are driven by InventoryInputGate on the panel, so
        // they stay correct no matter how the panel is shown/hidden (toggle key,
        // SetOpen, or the on-screen close button calling SetActive directly).
    }
}
