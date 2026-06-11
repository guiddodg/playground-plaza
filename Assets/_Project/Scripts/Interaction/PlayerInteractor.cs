using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central interaction driver on the player. Each frame it casts a ray from the
/// centre of the screen (the reticle) and, if it hits an <see cref="IInteractable"/>
/// that is within <see cref="reachDistance"/> of the player, marks it as focused.
/// Pressing <see cref="interactKey"/> runs the focused interactable.
///
/// This is the single place that knows about the camera, the ray and the input,
/// so interactables stay dumb. Disabled while the gameplay input is locked
/// (e.g. the inventory is open).
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Aim")]
    [Tooltip("Camera the reticle ray is cast from. Defaults to Camera.main.")]
    [SerializeField] private Camera aimCamera;
    [Tooltip("How close the player must be to the target to interact.")]
    [SerializeField, Min(0.5f)] private float reachDistance = 4f;
    [Tooltip("Max length of the aim ray from the camera (covers the 3rd-person gap).")]
    [SerializeField, Min(1f)] private float rayDistance = 20f;
    [SerializeField] private LayerMask mask = ~0;

    [Header("Input")]
    [SerializeField] private Key interactKey = Key.F;

    [Header("UI")]
    [SerializeField] private InteractionReticle reticle;

    private IInteractable focused;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    /// <summary>The interactable currently under the reticle and in reach, or null.</summary>
    public IInteractable Focused => focused;

    private void Update()
    {
        UpdateFocus();

        if (focused != null && !GameplayInputLock.Locked)
        {
            var kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame)
                focused.Interact(gameObject);
        }
    }

    private void UpdateFocus()
    {
        IInteractable found = null;

        var cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam != null && !GameplayInputLock.Locked)
        {
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int n = Physics.RaycastNonAlloc(ray, hitBuffer, rayDistance, mask, QueryTriggerInteraction.Collide);

            // Walk hits front-to-back. A non-interactable solid collider (a wall)
            // occludes anything behind it; non-interactable triggers (gameplay
            // zones like safety areas) are see-through. The first interactable in
            // reach wins.
            System.Array.Sort(hitBuffer, 0, n, HitDistanceComparer.Instance);
            for (int i = 0; i < n; i++)
            {
                var col = hitBuffer[i].collider;
                if (col.transform.IsChildOf(transform)) continue; // our own body

                var inter = col.GetComponentInParent<IInteractable>();
                if (inter != null && inter.CanInteract)
                {
                    if (Vector3.Distance(transform.position, hitBuffer[i].point) <= reachDistance)
                        found = inter;
                    break; // it's what we're aiming at, in reach or not
                }

                if (!col.isTrigger)
                    break; // a solid wall blocks the view
            }
        }

        SetFocus(found);
    }

    private void SetFocus(IInteractable next)
    {
        if (ReferenceEquals(next, focused))
            return;

        focused = next;
        if (reticle != null)
            reticle.SetFocus(next != null ? next.GetPrompt() : null);
    }

    /// <summary>Sorts raycast hits nearest-first without allocating each frame.</summary>
    private sealed class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }
}
