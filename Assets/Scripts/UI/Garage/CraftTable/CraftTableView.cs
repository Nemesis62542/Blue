using System;
using System.Collections.Generic;
using Blue.Recipe;
using DG.Tweening;
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
        [SerializeField] private TMP_Text ownedCount;
        [SerializeField] private Slider progressGauge;
        [SerializeField] private MeshFilter itemModelDisplay;
        [SerializeField] private float modelRotationSpeed = 30f;
        [SerializeField] private CanvasGroup craftCompleteNotification;
        [SerializeField] private float notificationDisplayDuration = 2f;
        [SerializeField] private int blinkCount = 2;
        [SerializeField] private float blinkInterval = 0.1f;

        private CraftTableModel model;
        private Tween notificationTween;
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
                panel.OnPointerEnter += recipe => SetItemInfomation(recipe);
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
            UpdateProgressGauge();
            UpdateModelRotation();
        }

        private void UpdateProgressGauge()
        {
            if (progressGauge == null) return;

            if (isPointerPressed && currentRecipe != null && model.HasAllRequiredResources(currentRecipe))
            {
                progressGauge.value += Time.deltaTime;
                if (progressGauge.value >= 1f)
                {
                    progressGauge.value = 0f;
                    OnConfirmCraftItem?.Invoke(currentRecipe);
                    ShowCraftCompleteNotification();
                }
            }
            else
            {
                progressGauge.value = 0f;
            }
        }

        private void UpdateModelRotation()
        {
            if (itemModelDisplay == null || itemModelDisplay.mesh == null) return;

            itemModelDisplay.transform.Rotate(0f, modelRotationSpeed * Time.deltaTime, 0f);
        }

        private void OnPanelPointerDown(RecipeData recipe)
        {
            currentRecipe = recipe;
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
                SetItemInfomation(currentRecipe, forceRefresh: true);
            }
        }

        private void SetItemInfomation(RecipeData recipe, bool forceRefresh = false)
        {
            if (!forceRefresh && currentRecipe == recipe) return;

            bool isNewRecipe = currentRecipe != recipe;
            currentRecipe = recipe;
            itemName.text = recipe.ResultItem.Name;
            description.text = recipe.ResultItem.Description;
            requireItems.text = GenerateRequireItemText(recipe.RequireResources);

            if (ownedCount != null)
            {
                int count = model.GetItemCount(recipe.ResultItem);
                ownedCount.text = $"所持数: {count}";
            }

            if (itemModelDisplay != null && isNewRecipe)
            {
                itemModelDisplay.mesh = recipe.ResultItem.ModelMesh;
                itemModelDisplay.transform.localRotation = Quaternion.identity;
            }
        }

        private string GenerateRequireItemText(List<RequireItemData> requires)
        {
            string result = "";

            foreach(RequireItemData require in requires)
            {
                bool hasEnough = model.CheckEnoughResource(require.Item, require.Count);
                string color = hasEnough ? "#FF9600" : "red";
                result += $"<color={color}>{require.Item.Name} x {require.Count}</color>\n";
            }

            return result;
        }

        private void ShowCraftCompleteNotification()
        {
            if (craftCompleteNotification == null) return;

            notificationTween?.Kill();

            craftCompleteNotification.gameObject.SetActive(true);
            craftCompleteNotification.alpha = 1f;

            notificationTween = DOTween.Sequence()
                .Append(craftCompleteNotification.DOFade(0.2f, blinkInterval).SetLoops(blinkCount * 2, LoopType.Yoyo))
                .AppendInterval(notificationDisplayDuration)
                .Append(craftCompleteNotification.DOFade(0f, 0.05f))
                .OnComplete(() => craftCompleteNotification.gameObject.SetActive(false));
        }

        private void OnDestroy()
        {
            notificationTween?.Kill();
        }
    }
}