using UnityEngine;

/// <summary>
/// A world crafting station (a workbench). Implements <see cref="IInteractable"/>
/// so the player opens the crafting screen by aiming at it and pressing the
/// interact key. Needs a <see cref="Collider"/> on the Interactable layer for the
/// aim ray to hit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CraftingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private string prompt = "Mesa de crafteo";

    public bool CanInteract => craftingUI != null;

    public string GetPrompt() => prompt;

    public void Interact(GameObject interactor)
    {
        if (craftingUI != null)
            craftingUI.Open();
    }
}
