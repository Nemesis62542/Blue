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

        private CraftTableModel model;
        private RecipeData currentRecipe;

        public Action<RecipeData> OnConfirmCraftItem;

        public void Initialize(List<RecipeData> recipes, CraftTableModel craft_model, Action<RecipeData> craft_callback)
        {
            model = craft_model;
            OnConfirmCraftItem = craft_callback;

            foreach(RecipeData recipe in recipes)
            {
                RecipePanel panel = Instantiate(panelPrefab, panelParent);
                panel.Initialize(recipe);
                panel.OnPointerEnter += SetItemInfomation;
                panel.OnConfirmCraftItem += OnConfirmCraftItem;
            }
        }

        public void RefreshDisplay()
        {
            if (currentRecipe != null)
            {
                SetItemInfomation(currentRecipe);
            }
        }

        private void SetItemInfomation(RecipeData recipe)
        {
            currentRecipe = recipe;
            description.text = recipe.ResultItem.Description;
            requireItems.text = GenerateRequireItemText(recipe.RequireResources);
        }

        private string GenerateRequireItemText(List<RequireItemData> requires)
        {
            string result = "";

            foreach(RequireItemData require in requires)
            {
                bool hasEnough = model.CheckEnoughResource(require.Item, require.Count);
                string color = hasEnough ? "white" : "red";
                result += $"<color={color}>{require.Item.Name} x {require.Count}</color>\n";
            }

            return result;
        }
    }
}