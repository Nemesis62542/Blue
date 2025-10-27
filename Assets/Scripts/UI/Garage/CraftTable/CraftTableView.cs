using System;
using System.Collections.Generic;
using Blue.Recipe;
using TMPro;
using UnityEngine;

namespace Blue.UI.Garage.CraftTable
{
    public class CraftTableView : MonoBehaviour
    {
        [SerializeField] private RecipePanel panelPrefab;
        [SerializeField] private Transform panelParent;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text requireItems;

        public Action<RecipeData> OnConfirmCraftItem;

        public void Initialize(List<RecipeData> recipes, Action<RecipeData> craft_callback)
        {
            OnConfirmCraftItem = craft_callback;

            foreach(RecipeData recipe in recipes)
            {
                RecipePanel panel = Instantiate(panelPrefab, panelParent);
                panel.Initialize(recipe);
                panel.OnPointerEnter += SetItemInfomation;
                panel.OnConfirmCraftItem += OnConfirmCraftItem;
            }
        }

        private void SetItemInfomation(RecipeData recipe)
        {
            description.text = recipe.ResultItem.Description;
            requireItems.text = GenerateRequireItemText(recipe.RequireResources);
        }

        private string GenerateRequireItemText(List<RequireItemData> requires)
        {
            string result = "";

            foreach(RequireItemData require in requires)
            {
                result += $"{require.Item.Name} x {require.Count}\n";
            }

            return result;
        }
    }
}