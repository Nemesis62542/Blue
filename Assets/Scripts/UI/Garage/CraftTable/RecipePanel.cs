using System;
using Blue.Recipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Garage.CraftTable
{
    public class RecipePanel : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Slider progressGauge;

        private RecipeData recipe;
        private bool isPointerClick;

        public event Action<RecipeData> OnPointerEnter;
        public event Action<RecipeData> OnConfirmCraftItem;

        public RecipeData Recipe => recipe;

        public void Initialize(RecipeData recipe)
        {
            this.recipe = recipe;
            icon.sprite = recipe.ResultItem.Icon;
            nameText.text = recipe.ResultItem.Name;
            progressGauge.value = 0f;
        }

        public void Update()
        {
            if(isPointerClick)
            {
                progressGauge.value += Time.deltaTime;
                if(progressGauge.value >= 1)
                {
                    progressGauge.value = 0;
                    OnConfirmCraftItem?.Invoke(recipe);
                }
            }
            else
            {
                progressGauge.value = 0;
            }
        }

        public void OnPointerEnterEvent()
        {
            OnPointerEnter?.Invoke(recipe);
        }

        public void OnPointerDownEvent()
        {
            isPointerClick = true;
        }

        public void OnPointerUpEvent()
        {
            isPointerClick = false;
        }
    }
}