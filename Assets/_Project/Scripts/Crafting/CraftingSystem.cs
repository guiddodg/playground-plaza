using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure crafting logic on top of <see cref="PlayerInventory"/>. Holds the set of
/// known <see cref="RecipeData"/> and turns ingredients into results: checking
/// availability, consuming ingredients and granting the result. No UI — a
/// crafting screen drives it through <see cref="CanCraft"/>/<see cref="Craft"/>.
/// </summary>
public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; private set; }

    [Tooltip("Inventory to craft from. If empty, PlayerInventory.Instance is used.")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private List<RecipeData> knownRecipes = new List<RecipeData>();

    /// <summary>Read-only view of the recipes this system knows about.</summary>
    public IReadOnlyList<RecipeData> KnownRecipes => knownRecipes;

    /// <summary>Fired after a successful craft, with the recipe that was crafted.</summary>
    public event Action<RecipeData> OnCrafted;

    private PlayerInventory Inv => inventory != null ? inventory : PlayerInventory.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>True if the inventory holds every ingredient of the recipe.</summary>
    public bool CanCraft(RecipeData recipe)
    {
        var inv = Inv;
        if (recipe == null || recipe.result == null || inv == null)
            return false;
        if (recipe.ingredients == null || recipe.ingredients.Count == 0)
            return false;

        foreach (var ing in recipe.ingredients)
            if (ing.item == null || !inv.HasItem(ing.item, ing.count))
                return false;

        return true;
    }

    /// <summary>
    /// Consumes the ingredients and grants the result. Returns false (and changes
    /// nothing) if the recipe can't be crafted.
    /// </summary>
    public bool Craft(RecipeData recipe)
    {
        if (!CanCraft(recipe))
            return false;

        var inv = Inv;
        foreach (var ing in recipe.ingredients)
            inv.RemoveItem(ing.item, ing.count);

        inv.AddItem(recipe.result, recipe.resultCount);

        OnCrafted?.Invoke(recipe);
        NotificationFeed.Post($"Crafteaste {recipe.result.itemName}");
        return true;
    }

    /// <summary>The known recipes that can be crafted with the current inventory.</summary>
    public List<RecipeData> GetCraftable()
    {
        var result = new List<RecipeData>();
        foreach (var recipe in knownRecipes)
            if (CanCraft(recipe))
                result.Add(recipe);
        return result;
    }
}
