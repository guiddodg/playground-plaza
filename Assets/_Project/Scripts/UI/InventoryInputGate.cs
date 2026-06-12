using UnityEngine;

/// <summary>
/// Drives the gameplay input lock and cursor from this object's active state.
/// Put it on the inventory panel: while the panel is shown the player/camera
/// freeze and the cursor is freed; when it's hidden — by any means (the toggle
/// key, <see cref="InventoryUI.SetOpen"/>, or a close button calling
/// <c>SetActive(false)</c> directly) — the lock is released. Tying the lock to
/// visibility avoids it getting stuck when a close path forgets to unlock.
/// </summary>
public class InventoryInputGate : MonoBehaviour
{
    private void OnEnable()
    {
        GameplayInputLock.SetLocked(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        GameplayInputLock.SetLocked(false);
    }
}
