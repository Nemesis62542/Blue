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

        public event Action<RecipeData> OnPointerEnter;

        public RecipeData Recipe => recipe;

        public void Initialize(RecipeData recipe)
        {
            this.recipe = recipe;
            icon.sprite = recipe.ResultItem.Icon;
            nameText.text = recipe.ResultItem.Name;
            progressGauge.value = 0f;
        }

        public void OnPointerEnterEvent()
        {
            OnPointerEnter?.Invoke(recipe);
        }
    }
}