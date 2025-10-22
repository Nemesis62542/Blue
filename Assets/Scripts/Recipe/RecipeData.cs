using System.Collections.Generic;
using Blue.Item;
using UnityEngine;

namespace Blue.Recipe
{
    [CreateAssetMenu(fileName = "RecipeData", menuName = "Blue/ScriptableObject/RecipeData")]
    public class RecipeData : ScriptableObject
    {
        [SerializeField] private List<RequireItemData> requires = new List<RequireItemData>();
        [SerializeField] private ItemData item;
        [SerializeField] private int count;

        public List<RequireItemData> RequireResources => requires;
        public ItemData ResultItem => item;
        public int ResultCount => count;
    }

    [System.Serializable]
    public class RequireItemData
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int count;

        public ItemData RequireItem => item;
        public int RequireCount => count;
    }
}