using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Makes a world object collectable. Sits on a GameObject with a trigger
/// <see cref="Collider"/>; when a <see cref="PlayerInventory"/> enters the
/// trigger it shows a prompt and, on the pickup key, adds the item to that
/// inventory and removes itself from the scene.
///
/// Detection keys off the <see cref="PlayerInventory"/> component rather than a
/// tag, so anything that can carry items can pick up. Presentation (the on-screen
/// "Press E" prompt and the in-range highlight) is wired through UnityEvents /
/// optional GameObject references, so this stays decoupled from the HUD canvas.
///
/// This is intentionally pickup-specific; if more world interactions appear it
/// can be generalised behind an IInteractable interface that an interaction
/// driver on the player invokes.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData item;
    [Tooltip("How many units this pickup grants.")]
    [SerializeField, Min(1)] private int amount = 1;

    [Header("Input")]
    [SerializeField] private Key pickupKey = Key.E;
    [Tooltip("If true the item is collected automatically on touch, without a key press.")]
    [SerializeField] private bool autoCollect = false;

    [Header("Feedback (optional)")]
    [Tooltip("Object toggled on while the player is in range (e.g. an outline or a world-space prompt).")]
    [SerializeField] private GameObject highlight;

    /// <summary>Concrete UnityEvent so the bool payload serializes / shows in the Inspector.</summary>
    [System.Serializable] public class RangeEvent : UnityEvent<bool> { }

    [Header("Events")]
    [Tooltip("Fired when the player enters (true) or leaves (false) pickup range. Hook the HUD prompt here.")]
    public RangeEvent onRangeChanged = new RangeEvent();
    [Tooltip("Fired after the item is successfully collected.")]
    public UnityEvent onCollected = new UnityEvent();

    public ItemData Item => item;
    public int Amount => amount;
    public bool PlayerInRange => current != null;

    private PlayerInventory current;

    private void Reset()
    {
        // Convenience when adding in the Editor: pickups need a trigger collider.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        SetHighlight(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        var inv = other.GetComponentInParent<PlayerInventory>();
        if (inv == null) return;

        current = inv;
        SetHighlight(true);
        onRangeChanged?.Invoke(true);

        if (autoCollect)
            Collect(current);
    }

    private void OnTriggerExit(Collider other)
    {
        var inv = other.GetComponentInParent<PlayerInventory>();
        if (inv == null || inv != current) return;

        current = null;
        SetHighlight(false);
        onRangeChanged?.Invoke(false);
    }

    private void Update()
    {
        if (current == null || autoCollect) return;

        var kb = Keyboard.current;
        if (kb != null && kb[pickupKey].wasPressedThisFrame)
            Collect(current);
    }

    /// <summary>
    /// Adds the item to the given inventory and removes this pickup from the
    /// scene. Returns false (and stays in the world) if there's nothing to give.
    /// Public so it can be driven by tests or an external interaction system.
    /// </summary>
    public bool Collect(PlayerInventory inventory)
    {
        if (inventory == null || item == null || amount <= 0)
            return false;

        inventory.AddItem(item, amount);
        onCollected?.Invoke();

        // Clear range state before leaving so listeners hide their prompt.
        current = null;
        onRangeChanged?.Invoke(false);

        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }

    private void SetHighlight(bool on)
    {
        if (highlight != null)
            highlight.SetActive(on);
    }
}
