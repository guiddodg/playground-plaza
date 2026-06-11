using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A collectable world object. Implements <see cref="IInteractable"/>, so the
/// player picks it up by aiming the reticle at it (within reach) and pressing
/// the interact key — all driven by <see cref="PlayerInteractor"/>. Needs a
/// <see cref="Collider"/> for the aim ray to hit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private ItemData item;
    [Tooltip("How many units this pickup grants.")]
    [SerializeField, Min(1)] private int amount = 1;

    [Header("Events")]
    [Tooltip("Fired after the item is successfully collected.")]
    public UnityEvent onCollected = new UnityEvent();

    public ItemData Item => item;
    public int Amount => amount;

    // --- IInteractable ------------------------------------------------------

    public bool CanInteract => item != null;

    public string GetPrompt() => item != null ? $"Recoger {item.itemName}" : "Recoger";

    public void Interact(GameObject interactor)
    {
        var inv = interactor != null ? interactor.GetComponentInParent<PlayerInventory>() : null;
        if (inv == null) inv = PlayerInventory.Instance;
        Collect(inv);
    }

    // --- Collection ---------------------------------------------------------

    /// <summary>
    /// Adds the item to the given inventory and removes this pickup from the
    /// scene. Returns false (and stays in the world) if there's nothing to give.
    /// Public so it can be driven by tests or other systems.
    /// </summary>
    public bool Collect(PlayerInventory inventory)
    {
        if (inventory == null || item == null || amount <= 0)
            return false;

        inventory.AddItem(item, amount);
        onCollected?.Invoke();

        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
