using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds the inventory grid in the sticker/candy UI style. Instantiates one
/// <see cref="InventorySlotUI"/> per entry inside a GridLayoutGroup, and lets
/// the player toggle the wood-framed panel open/closed. While open it engages
/// <see cref="GameplayInputLock"/> so the player and camera stay frozen.
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

    [Header("Layout")]
    [Tooltip("Total slots to draw. Empty slots beyond the item count render blank.")]
    [SerializeField] private int slotCount = 12;

    [Header("Contents")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    [Header("Input")]
    [Tooltip("Key that toggles the inventory open/closed (new Input System).")]
    [SerializeField] private Key toggleKey = Key.I;
    [SerializeField] private bool startOpen = false;

    private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private bool isOpen;

    private void Start()
    {
        BuildSlots();
        Refresh();
        SetOpen(startOpen);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
            SetOpen(!isOpen);
    }

    public void BuildSlots()
    {
        if (slotContainer == null || slotPrefab == null)
            return;

        foreach (var s in slots)
            if (s != null) Destroy(s.gameObject);
        slots.Clear();

        int total = Mathf.Max(slotCount, entries.Count);
        for (int i = 0; i < total; i++)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.name = $"Slot_{i:00}";
            slots.Add(slot);
        }
    }

    public void Refresh()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < entries.Count && entries[i].item != null)
                slots[i].SetItem(entries[i].item, entries[i].count);
            else
                slots[i].Clear();
        }
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (panelRoot != null)
            panelRoot.SetActive(open);

        // Freeze player + camera while the inventory is open, and free the cursor
        // so the panel can be clicked.
        GameplayInputLock.SetLocked(open);
        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
