using UnityEngine;

namespace VIPProject.Items
{
    /// <summary>
    /// Static definition of an item (config only, never mutated at runtime).
    /// Create via Assets > Create > VIP > Item Data. A future inventory can key off ItemId.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "VIP/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Unique, stable id for saves and inventory lookups. Falls back to the asset name.")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName = "Item";
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        public string ItemId => string.IsNullOrEmpty(itemId) ? name : itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
