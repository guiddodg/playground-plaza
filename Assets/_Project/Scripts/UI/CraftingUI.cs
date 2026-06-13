using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen crafting screen in the Animal Crossing DIY style: a grid of recipe
/// cards on the left and a detail panel on the right (result icon + name, the
/// required materials with have/need counts, and a craft button). Opened by the
/// <see cref="CraftingStation"/>; the panel's input gate handles the lock.
///
/// Cards and material rows are built dynamically but with fixed sizes and no
/// content-driven fitters, to avoid layout-rebuild loops. Material rows are
/// rebuilt only when the selection changes; counts/colors refresh in place.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    private class Card { public RecipeData recipe; public Button button; public Image frame; }
    private class MatRow { public ItemData item; public int need; public TMP_Text countText; }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private CraftingSystem crafting;

    [Header("Detail panel")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private RectTransform materialsContainer;
    [SerializeField] private Button craftButton;
    [SerializeField] private TMP_Text craftButtonLabel;

    [Header("Style")]
    [SerializeField] private Sprite roundedSprite;
    [SerializeField] private Color cardColor = new Color(1f, 0.99f, 0.94f);
    [SerializeField] private Color cardSelected = new Color(0.99f, 0.86f, 0.45f);
    [SerializeField] private Color textColor = new Color(0.36f, 0.29f, 0.20f);
    [SerializeField] private Color enoughColor = new Color(0.30f, 0.56f, 0.27f);
    [SerializeField] private Color missingColor = new Color(0.80f, 0.33f, 0.28f);
    [SerializeField] private int matFontSize = 26;

    private readonly List<Card> cards = new List<Card>();
    private readonly List<MatRow> matRows = new List<MatRow>();
    private RecipeData selected;
    private bool built;
    private PlayerInventory boundInventory;

    private CraftingSystem Craft => crafting != null ? crafting : CraftingSystem.Instance;

    private void Start() => SetOpen(false);

    private void OnDestroy()
    {
        if (boundInventory != null) boundInventory.OnChanged -= Refresh;
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool open)
    {
        if (open && !built) Build();
        if (panelRoot != null) panelRoot.SetActive(open);
        if (open)
        {
            if (selected == null && cards.Count > 0) SelectRecipe(cards[0].recipe);
            Refresh();
        }
    }

    private void Build()
    {
        built = true;
        var sys = Craft;
        if (sys == null) return;

        if (gridContainer != null)
            foreach (var recipe in sys.KnownRecipes)
                if (recipe != null && recipe.result != null)
                    cards.Add(CreateCard(recipe));

        if (craftButton != null)
            craftButton.onClick.AddListener(CraftSelected);

        boundInventory = PlayerInventory.Instance;
        if (boundInventory != null) boundInventory.OnChanged += Refresh;
    }

    private Card CreateCard(RecipeData recipe)
    {
        var go = new GameObject("Card", typeof(RectTransform));
        go.transform.SetParent(gridContainer, false);
        var frame = go.AddComponent<Image>();
        frame.sprite = roundedSprite; frame.type = Image.Type.Sliced; frame.color = cardColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = frame;

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(go.transform, false);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = recipe.result.icon;
        icon.preserveAspect = true;
        icon.enabled = recipe.result.icon != null;
        var irt = icon.rectTransform;
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(10f, 10f); irt.offsetMax = new Vector2(-10f, -10f);

        var card = new Card { recipe = recipe, button = btn, frame = frame };
        btn.onClick.AddListener(() => SelectRecipe(card.recipe));
        return card;
    }

    private void SelectRecipe(RecipeData recipe)
    {
        selected = recipe;

        // highlight the selected card
        foreach (var c in cards)
            if (c.frame != null) c.frame.color = (c.recipe == recipe) ? cardSelected : cardColor;

        // detail header
        if (detailName != null) detailName.text = recipe != null ? recipe.result.itemName : "";
        if (detailIcon != null)
        {
            detailIcon.sprite = recipe != null ? recipe.result.icon : null;
            detailIcon.enabled = recipe != null && recipe.result.icon != null;
            detailIcon.preserveAspect = true;
        }

        BuildMaterials(recipe);
        Refresh();
    }

    private void BuildMaterials(RecipeData recipe)
    {
        // clear current rows (fixed-count loop over our list, never a childCount while)
        foreach (var r in matRows)
            if (r.countText != null) Destroy(r.countText.transform.parent.gameObject);
        matRows.Clear();
        if (recipe == null || materialsContainer == null) return;

        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            matRows.Add(CreateMatRow(ing.item, ing.count));
        }
    }

    private MatRow CreateMatRow(ItemData item, int need)
    {
        var go = new GameObject("Material", typeof(RectTransform));
        go.transform.SetParent(materialsContainer, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = matFontSize + 22; le.preferredHeight = matFontSize + 22;

        var icon = new GameObject("Icon", typeof(RectTransform));
        icon.transform.SetParent(go.transform, false);
        var img = icon.AddComponent<Image>();
        img.sprite = item.icon; img.preserveAspect = true; img.enabled = item.icon != null;
        var irt = img.rectTransform;
        irt.anchorMin = new Vector2(0f, 0.5f); irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.sizeDelta = new Vector2(matFontSize + 14, matFontSize + 14);
        irt.anchoredPosition = new Vector2(6f, 0f);

        var label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = matFontSize; tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft; tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        var lrt = tmp.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(matFontSize + 28, 0f); lrt.offsetMax = new Vector2(-8f, 0f);

        return new MatRow { item = item, need = need, countText = tmp };
    }

    public void Refresh()
    {
        var inv = boundInventory != null ? boundInventory : PlayerInventory.Instance;

        foreach (var r in matRows)
        {
            int have = inv != null ? inv.CountOf(r.item) : 0;
            if (r.countText != null)
            {
                r.countText.text = r.item.itemName + "   " + have + " / " + r.need;
                r.countText.color = have >= r.need ? enoughColor : missingColor;
            }
        }

        bool can = selected != null && Craft != null && Craft.CanCraft(selected);
        if (craftButton != null) craftButton.interactable = can;
        if (craftButtonLabel != null) craftButtonLabel.text = can ? "CRAFTEAR" : "FALTAN MATERIALES";
    }

    private void CraftSelected()
    {
        if (selected != null && Craft != null && Craft.Craft(selected))
            Refresh();
    }
}
