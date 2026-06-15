using System.Collections.Generic;
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
///
/// Each material row mimics the AC recipe page: a framed icon slot, the item
/// name, a dotted leader, and the have/need count inside a rounded count box.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    private class Card { public RecipeData recipe; public Button button; public Image frame; }
    private class MatRow { public ItemData item; public int need; public TMP_Text countText; public Image box; }

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
    [SerializeField] private Sprite cardSprite;
    [SerializeField] private Sprite cardSelectedSprite;
    [SerializeField] private Color textColor = new Color(0.36f, 0.29f, 0.20f);
    [SerializeField] private Color enoughColor = new Color(0.30f, 0.56f, 0.27f);
    [SerializeField] private Color missingColor = new Color(0.80f, 0.33f, 0.28f);
    [SerializeField] private int matFontSize = 26;

    [Header("Style (AC detail rows)")]
    [SerializeField] private TMP_FontAsset uiFont;
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Sprite countBoxSprite;
    [SerializeField] private Sprite dotsSprite;
    [SerializeField] private Color countBoxEnough = new Color(0.55f, 0.74f, 0.42f);
    [SerializeField] private Color countBoxMissing = new Color(0.96f, 0.60f, 0.25f);

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
        frame.sprite = cardSprite; frame.type = Image.Type.Simple; frame.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = frame;

        // The item icon sits in the upper-middle of the card; the printed acorn
        // pattern and the corner star are baked into the card sprite behind it.
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(go.transform, false);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = recipe.result.icon;
        icon.preserveAspect = true;
        icon.enabled = recipe.result.icon != null;
        var irt = icon.rectTransform;
        irt.anchorMin = new Vector2(0.5f, 0.5f); irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.anchoredPosition = new Vector2(0f, 6f);
        irt.sizeDelta = new Vector2(118f, 118f);

        var card = new Card { recipe = recipe, button = btn, frame = frame };
        btn.onClick.AddListener(() => SelectRecipe(card.recipe));
        return card;
    }

    private void SelectRecipe(RecipeData recipe)
    {
        selected = recipe;

        foreach (var c in cards)
            if (c.frame != null) c.frame.sprite = (c.recipe == recipe) ? cardSelectedSprite : cardSprite;

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
        // destroy the row container objects we created (fixed-count loop over our
        // own list, never a childCount while-loop which can spin on deferred Destroy)
        for (int i = matRows.Count - 1; i >= 0; i--)
        {
            var rootGo = RowRoot(matRows[i]);
            if (rootGo != null) Destroy(rootGo);
        }
        matRows.Clear();
        if (recipe == null || materialsContainer == null) return;

        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            matRows.Add(CreateMatRow(ing.item, ing.count));
        }
    }

    // The row root is the slot's grandparent ("Material" container).
    private GameObject RowRoot(MatRow r)
    {
        if (r.box != null) return r.box.transform.parent.gameObject;
        return null;
    }

    private MatRow CreateMatRow(ItemData item, int need)
    {
        float rowH = 72f;
        var go = new GameObject("Material", typeof(RectTransform));
        go.transform.SetParent(materialsContainer, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = rowH; le.preferredHeight = rowH;

        // dotted leader line behind, vertically centred
        if (dotsSprite != null)
        {
            var dots = new GameObject("Dots", typeof(RectTransform));
            dots.transform.SetParent(go.transform, false);
            var di = dots.AddComponent<Image>();
            di.sprite = dotsSprite; di.type = Image.Type.Tiled; di.color = new Color(0.55f, 0.47f, 0.33f, 0.7f);
            var drt = di.rectTransform;
            drt.anchorMin = new Vector2(0f, 0.5f); drt.anchorMax = new Vector2(1f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            // start past where short item names end, ending before the count box,
            // so it reads as a dotted leader rather than crossing the name.
            drt.offsetMin = new Vector2(250f, -3f); drt.offsetMax = new Vector2(-96f, 3f);
        }

        // framed icon slot (left)
        var slotGo = new GameObject("Slot", typeof(RectTransform));
        slotGo.transform.SetParent(go.transform, false);
        var slotImg = slotGo.AddComponent<Image>();
        slotImg.sprite = slotSprite; slotImg.type = Image.Type.Sliced; slotImg.color = Color.white;
        var srt = slotImg.rectTransform;
        srt.anchorMin = new Vector2(0f, 0.5f); srt.anchorMax = new Vector2(0f, 0.5f);
        srt.pivot = new Vector2(0f, 0.5f);
        srt.sizeDelta = new Vector2(60f, 60f);
        srt.anchoredPosition = new Vector2(6f, 0f);

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(slotGo.transform, false);
        var img = iconGo.AddComponent<Image>();
        img.sprite = item.icon; img.preserveAspect = true; img.enabled = item.icon != null;
        var irt = img.rectTransform;
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(7f, 7f); irt.offsetMax = new Vector2(-7f, -7f);

        // item name (left, after slot)
        var nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(go.transform, false);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) nameTmp.font = uiFont;
        nameTmp.text = item.itemName;
        nameTmp.fontSize = matFontSize; nameTmp.color = textColor;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft; nameTmp.enableWordWrapping = false;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        var nrt = nameTmp.rectTransform;
        nrt.anchorMin = new Vector2(0f, 0f); nrt.anchorMax = new Vector2(1f, 1f);
        nrt.offsetMin = new Vector2(78f, 0f); nrt.offsetMax = new Vector2(-92f, 0f);

        // count box (right)
        var boxGo = new GameObject("Count", typeof(RectTransform));
        boxGo.transform.SetParent(go.transform, false);
        var box = boxGo.AddComponent<Image>();
        box.sprite = countBoxSprite; box.type = Image.Type.Sliced; box.color = countBoxMissing;
        var brt = box.rectTransform;
        brt.anchorMin = new Vector2(1f, 0.5f); brt.anchorMax = new Vector2(1f, 0.5f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.sizeDelta = new Vector2(84f, 46f);
        brt.anchoredPosition = new Vector2(-4f, 0f);

        var countGo = new GameObject("Label", typeof(RectTransform));
        countGo.transform.SetParent(boxGo.transform, false);
        var countTmp = countGo.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) countTmp.font = uiFont;
        countTmp.fontSize = matFontSize; countTmp.color = Color.white;
        countTmp.alignment = TextAlignmentOptions.Center; countTmp.enableWordWrapping = false;
        var crt = countTmp.rectTransform;
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(4f, 0f); crt.offsetMax = new Vector2(-4f, -2f);

        return new MatRow { item = item, need = need, countText = countTmp, box = box };
    }

    public void Refresh()
    {
        var inv = boundInventory != null ? boundInventory : PlayerInventory.Instance;

        foreach (var r in matRows)
        {
            int have = inv != null ? inv.CountOf(r.item) : 0;
            bool ok = have >= r.need;
            if (r.countText != null) r.countText.text = have + " / " + r.need;
            if (r.box != null) r.box.color = ok ? countBoxEnough : countBoxMissing;
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
