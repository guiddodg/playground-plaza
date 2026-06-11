using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game event feed shown top-left. Any system posts a one-line message via
/// the static <see cref="Post"/> and a row appears, stacks (newest on top) and
/// fades out on its own. A stand-in for a future async notification service, so
/// callers depend only on a tiny static API — not on this UI.
/// </summary>
public class NotificationFeed : MonoBehaviour
{
    public static NotificationFeed Instance { get; private set; }

    [Header("Container")]
    [Tooltip("Parent (top-aligned VerticalLayoutGroup) where rows are added.")]
    [SerializeField] private RectTransform container;

    [Header("Behaviour")]
    [SerializeField, Min(1)] private int maxVisible = 5;
    [SerializeField, Min(0.5f)] private float lifetime = 3.5f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.5f;

    [Header("Row style")]
    [SerializeField] private Color background = new Color(0.12f, 0.10f, 0.18f, 0.82f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(8)] private int fontSize = 26;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Post a message to the feed from anywhere. No-ops if no feed is present.</summary>
    public static void Post(string message)
    {
        if (Instance != null) Instance.Show(message);
    }

    public void Show(string message)
    {
        if (container == null || string.IsNullOrEmpty(message))
            return;

        var go = new GameObject("Notification", typeof(RectTransform));
        go.transform.SetParent(container, false);

        var bg = go.AddComponent<Image>();
        bg.color = background;

        var cg = go.AddComponent<CanvasGroup>();

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = fontSize + 16;

        var txtGo = new GameObject("Label", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.color = textColor;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        var trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14f, 4f);
        trt.offsetMax = new Vector2(-14f, -4f);

        go.transform.SetAsFirstSibling(); // newest on top

        var row = go.AddComponent<NotificationRow>();
        row.Play(cg, lifetime, fadeDuration);

        // Drop the oldest rows beyond the cap.
        while (container.childCount > maxVisible)
            Destroy(container.GetChild(container.childCount - 1).gameObject);
    }
}
