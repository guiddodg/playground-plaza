using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Crafting screen: one row per known recipe, showing the result and the
/// ingredients you have vs need. A row is clickable (crafts) only when all
/// ingredients are available. Opened by a <see cref="CraftingStation"/>. The
/// panel's <c>InventoryInputGate</c> handles the input lock by visibility.
///
/// Rows are built once from <see cref="CraftingSystem.KnownRecipes"/> and only
/// re-styled on refresh (no per-frame create/destroy), and the grid uses fixed
/// row heights with no content-driven sizing to avoid layout-rebuild loops.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    private class Row
    {
        public RecipeData recipe;
        public Button button;
        public Image background;
        public TMP_Text label;
    }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform rowContainer;
    [Tooltip("Crafting system to read recipes from. If empty, CraftingSystem.Instance is used.")]
    [SerializeField] private CraftingSystem crafting;

    [Header("Row style")]
    [SerializeField] private Color craftableColor = new Color(0.27f, 0.62f, 0.30f, 0.92f);
    [SerializeField] private Color blockedColor = new Color(0.45f, 0.45f, 0.50f, 0.85f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(8)] private int fontSize = 24;

    private readonly List<Row> rows = new List<Row>();
    private CraftingSystem Craft => crafting != null ? crafting : CraftingSystem.Instance;
    private PlayerInventory boundInventory;
    private bool built;

    private void Start() => SetOpen(false);

    private void OnDestroy()
    {
        if (boundInventory != null)
            boundInventory.OnChanged -= Refresh;
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool open)
    {
        if (open && !built)
            Build();

        if (panelRoot != null)
            panelRoot.SetActive(open);

        if (open)
            Refresh();
    }

    private void Build()
    {
        built = true;
        var sys = Craft;
        if (sys == null || rowContainer == null)
            return;

        foreach (var recipe in sys.KnownRecipes)
            if (recipe != null && recipe.result != null)
                rows.Add(CreateRow(recipe));

        boundInventory = PlayerInventory.Instance;
        if (boundInventory != null)
            boundInventory.OnChanged += Refresh;
    }

    private Row CreateRow(RecipeData recipe)
    {
        var go = new GameObject("Recipe", typeof(RectTransform));
        go.transform.SetParent(rowContainer, false);

        var bg = go.AddComponent<Image>();

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = fontSize + 22;
        le.preferredHeight = fontSize + 22;

        var button = go.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(() =>
        {
            if (Craft != null && Craft.Craft(recipe))
                Refresh();
        });

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.color = textColor;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        var trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16f, 2f);
        trt.offsetMax = new Vector2(-16f, -2f);

        return new Row { recipe = recipe, button = button, background = bg, label = tmp };
    }

    public void Refresh()
    {
        var sys = Craft;
        foreach (var row in rows)
        {
            bool can = sys != null && sys.CanCraft(row.recipe);
            if (row.button != null) row.button.interactable = can;
            if (row.background != null) row.background.color = can ? craftableColor : blockedColor;
            if (row.label != null) row.label.text = BuildLabel(row.recipe);
        }
    }

    private string BuildLabel(RecipeData recipe)
    {
        var sb = new StringBuilder();
        sb.Append(recipe.result.itemName);
        if (recipe.resultCount > 1) sb.Append(" x").Append(recipe.resultCount);
        sb.Append("   ←   ");

        bool first = true;
        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            if (!first) sb.Append(",  ");
            first = false;
            int have = boundInventory != null ? boundInventory.CountOf(ing.item) : 0;
            sb.Append(ing.item.itemName).Append(" ").Append(have).Append("/").Append(ing.count);
        }
        return sb.ToString();
    }
}
