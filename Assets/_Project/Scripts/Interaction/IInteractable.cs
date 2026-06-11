using UnityEngine;

/// <summary>
/// Anything the player can target with the camera reticle and act on with the
/// interact key. Items implement it to be picked up; playground equipment will
/// implement it to play an animation, etc. The <see cref="PlayerInteractor"/>
/// drives it — implementers don't need to know about input or the camera.
/// </summary>
public interface IInteractable
{
    /// <summary>False to be ignored (e.g. already consumed, on cooldown).</summary>
    bool CanInteract { get; }

    /// <summary>Short text shown next to the reticle while focused (e.g. "Recoger Estrella").</summary>
    string GetPrompt();

    /// <summary>Run the interaction. <paramref name="interactor"/> is the acting GameObject (the player).</summary>
    void Interact(GameObject interactor);
}
