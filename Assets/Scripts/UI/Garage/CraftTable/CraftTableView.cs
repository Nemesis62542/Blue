using System;
using System.Collections.Generic;
using Blue.Audio;
using Blue.Item;
using Blue.Recipe;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Garage.CraftTable
{
    [Serializable]
    public class CategoryParent
    {
        public ItemType category;
        public Transform parent;
    }
    public class CraftTableView : MonoBehaviour
    {
        [SerializeField] private RecipePanel panelPrefab;
        [SerializeField] private List<CategoryParent> categoryParents;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text requireItems;
        [SerializeField] private TMP_Text ownedCount;
        [SerializeField] private Slider progressGauge;
        [SerializeField] private MeshFilter itemModelDisplay;
        [SerializeField] private MeshRenderer itemModelRenderer;
        [SerializeField] private float modelRotationSpeed = 30f;
        [SerializeField] private CanvasGroup craftCompleteNotification;
        [SerializeField] private float notificationDisplayDuration = 2f;
        [SerializeField] private int blinkCount = 2;
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("Screen Transition")]
        [SerializeField] private CanvasGroup screenTransitionPanel;
        [SerializeField] private int screenBlinkCount = 3;
        [SerializeField] private float screenBlinkInterval = 0.08f;

        private CraftTableModel model;
        private Tween notificationTween;
        private Tween screenTransitionTween;
        private RecipeData currentRecipe;
        private bool isPointerPressed;
        private Dictionary<ItemType, Transform> categoryParentDict;

        public Action<RecipeData> OnConfirmCraftItem;

        public void Initialize(List<RecipeData> recipes, CraftTableModel craft_model, Action<RecipeData> craft_callback)
        {
            model = craft_model;
            OnConfirmCraftItem = craft_callback;

            // カテゴリ別の親をDictionaryに変換
            categoryParentDict = new Dictionary<ItemType, Transform>();
            foreach (CategoryParent categoryParent in categoryParents)
            {
                categoryParentDict[categoryParent.category] = categoryParent.parent;

                // 既存の子オブジェクトを削除
                foreach (Transform child in categoryParent.parent)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach(RecipeData recipe in recipes)
            {
                Transform parent = GetParentForCategory(recipe.ResultItem.Type);
                RecipePanel panel = Instantiate(panelPrefab, parent);
                panel.Initialize(recipe);
                panel.OnPointerEnter += recipe => SetItemInfomation(recipe);
                panel.OnPointerDown += OnPanelPointerDown;
                panel.OnPointerUp += OnPanelPointerUp;
            }

            ClearDisplay();
            PlayScreenOnAnimation();
        }

        private Transform GetParentForCategory(ItemType type)
        {
            if (categoryParentDict.TryGetValue(type, out Transform parent))
            {
                return parent;
            }

            // カテゴリが見つからない場合は最初の親を使用
            if (categoryParents.Count > 0)
            {
                return categoryParents[0].parent;
            }

            return null;
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
                    SoundController.Instance.PlaySE(SEType.CraftSuccess);
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

            if (model.HasAllRequiredResources(recipe))
            {
                SoundController.Instance.PlayLoopSE(SEType.CraftCharge);
            }
            else
            {
                SoundController.Instance.PlaySE(SEType.CraftFailed);
            }
        }

        private void OnPanelPointerUp(RecipeData recipe)
        {
            isPointerPressed = false;
            SoundController.Instance.StopLoopSE();
        }

        public void RefreshDisplay()
        {
            if (currentRecipe != null)
            {
                SetItemInfomation(currentRecipe, forceRefresh: true);
            }
        }

        private void ClearDisplay()
        {
            currentRecipe = null;

            itemName.text = "";
            description.text = "";
            requireItems.text = "";
            ownedCount.text = "";

            progressGauge.value = 0f;

            itemModelDisplay.mesh = null;
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

            if (itemModelDisplay != null && itemModelRenderer != null && isNewRecipe)
            {
                ApplyModelToDisplay(recipe.ResultItem.Model);
                itemModelDisplay.transform.localRotation = Quaternion.identity;
            }
        }

        private void ApplyModelToDisplay(GameObject model)
        {
            if (model == null) return;

            // モデルからMeshFilterとMeshRendererを取得
            MeshFilter modelMeshFilter = model.GetComponentInChildren<MeshFilter>();
            MeshRenderer modelMeshRenderer = model.GetComponentInChildren<MeshRenderer>();

            if (modelMeshFilter == null || modelMeshRenderer == null) return;

            // メッシュを設定
            itemModelDisplay.mesh = modelMeshFilter.sharedMesh;

            // マテリアルをインスタンス化してテクスチャをコピー
            Material instanceMaterial = new Material(itemModelRenderer.sharedMaterial);
            Texture sourceTexture = GetMainTexture(modelMeshRenderer.sharedMaterial);
            if (sourceTexture != null)
            {
                SetMainTexture(instanceMaterial, sourceTexture);
            }
            itemModelRenderer.material = instanceMaterial;
        }

        private Texture GetMainTexture(Material material)
        {
            return material.GetTexture("_BaseMap");
        }

        private void SetMainTexture(Material material, Texture texture)
        {
           material.SetTexture("_BaseEmissionTexture", texture);
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

        private void PlayScreenOnAnimation()
        {
            if (screenTransitionPanel == null) return;

            screenTransitionTween?.Kill();

            screenTransitionPanel.gameObject.SetActive(true);
            screenTransitionPanel.alpha = 1f;

            screenTransitionTween = DOTween.Sequence()
                .Append(screenTransitionPanel.DOFade(0f, screenBlinkInterval).SetLoops(screenBlinkCount * 2, LoopType.Yoyo))
                .Append(screenTransitionPanel.DOFade(0f, screenBlinkInterval))
                .OnComplete(() => screenTransitionPanel.gameObject.SetActive(false));
        }

        public void ShowScreenOffPanel()
        {
            if (screenTransitionPanel == null) return;

            screenTransitionTween?.Kill();
            screenTransitionPanel.gameObject.SetActive(true);
            screenTransitionPanel.alpha = 1f;
        }

        private void OnDestroy()
        {
            notificationTween?.Kill();
            screenTransitionTween?.Kill();
        }
    }
}