using UnityEngine;

public enum ItemType
{
    Equipable,
    Consumible,
    Coleccionable
}

public enum EquipSlot
{
    None,
    Head,
    Back,
    Hand
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Playground Plaza/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public GameObject prefab;

    [Header("Classification")]
    public ItemType itemType;
    [Tooltip("Only relevant if itemType is Equipable")]
    public EquipSlot equipSlot;

    [Header("Stack")]
    public bool isStackable;
    [Min(1)] public int maxStack = 1;

    [Header("Consumable")]
    [Tooltip("Energy restored when consumed. Only relevant if itemType is Consumible")]
    [Min(0)] public float restoreAmount;

    [Header("Crafting")]
    [Tooltip("Can be used as an ingredient in crafting recipes")]
    public bool isRawMaterial;
}
