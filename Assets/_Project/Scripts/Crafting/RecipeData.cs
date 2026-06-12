using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A crafting recipe: a set of ingredient stacks consumed to produce a result
/// stack. Authored as an asset so designers can add recipes without code.
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Playground Plaza/Recipe")]
public class RecipeData : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public ItemData item;
        [Min(1)] public int count;
    }

    [Header("Identity")]
    public string id;
    public string recipeName;
    [Tooltip("Optional icon for the crafting UI.")]
    public Sprite icon;

    [Header("Ingredients (all required)")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("Result")]
    public ItemData result;
    [Min(1)] public int resultCount = 1;
}
