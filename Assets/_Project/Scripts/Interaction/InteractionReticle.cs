using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-centre crosshair plus a prompt label, driven by
/// <see cref="PlayerInteractor"/>. The dot tints to <see cref="focusColor"/> and
/// the prompt appears when something interactable is focused.
/// </summary>
public class InteractionReticle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Graphic dot;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptLabel;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color focusColor = new Color(1f, 0.85f, 0.2f, 1f);

    private void Awake() => SetFocus(null);

    /// <summary>Pass the prompt text to show focus, or null/empty to clear it.</summary>
    public void SetFocus(string prompt)
    {
        bool active = !string.IsNullOrEmpty(prompt);

        if (dot != null)
            dot.color = active ? focusColor : idleColor;

        if (promptRoot != null)
            promptRoot.SetActive(active);

        if (active && promptLabel != null)
            promptLabel.text = prompt;
    }
}
