using System;
using System.Collections.Generic;
using Blue.Recipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Garage.CraftTable
{
    public class CraftTableView : MonoBehaviour
    {
        [SerializeField] private RecipePanel panelPrefab;
        [SerializeField] private Transform panelParent;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text requireItems;
        [SerializeField] private Slider progressGauge;

        private CraftTableModel model;
        private RecipeData currentRecipe;
        private bool isPointerPressed;

        public Action<RecipeData> OnConfirmCraftItem;

        public void Initialize(List<RecipeData> recipes, CraftTableModel craft_model, Action<RecipeData> craft_callback)
        {
            model = craft_model;
            OnConfirmCraftItem = craft_callback;
            foreach (Transform child in panelParent)
            {
                Destroy(child.gameObject);
            }

            foreach(RecipeData recipe in recipes)
            {
                RecipePanel panel = Instantiate(panelPrefab, panelParent);
                panel.Initialize(recipe);
                panel.OnPointerEnter += SetItemInfomation;
                panel.OnPointerDown += OnPanelPointerDown;
                panel.OnPointerUp += OnPanelPointerUp;
            }

            if (progressGauge != null)
            {
                progressGauge.value = 0f;
            }
        }

        private void Update()
        {
            if (progressGauge == null) return;

            if (isPointerPressed && currentRecipe != null)
            {
                progressGauge.value += Time.deltaTime;
                if (progressGauge.value >= 1f)
                {
                    progressGauge.value = 0f;
                    OnConfirmCraftItem?.Invoke(currentRecipe);
                }
            }
            else
            {
                progressGauge.value = 0f;
            }
        }

        private void OnPanelPointerDown(RecipeData recipe)
        {
            SetItemInfomation(recipe);
            isPointerPressed = true;
        }

        private void OnPanelPointerUp(RecipeData recipe)
        {
            isPointerPressed = false;
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
            itemName.text = recipe.ResultItem.Name;
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