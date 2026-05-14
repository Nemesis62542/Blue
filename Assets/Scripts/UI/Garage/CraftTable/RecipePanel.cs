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

        private RecipeData recipe;

        public event Action<RecipeData> OnPointerEnter;
        public event Action<RecipeData> OnPointerDown;
        public event Action<RecipeData> OnPointerUp;

        public RecipeData Recipe => recipe;

        public void Initialize(RecipeData recipe)
        {
            this.recipe = recipe;
            icon.sprite = recipe.ResultItem.Icon;
        }

        public void OnPointerEnterEvent()
        {
            OnPointerEnter?.Invoke(recipe);
        }

        public void OnPointerDownEvent()
        {
            OnPointerDown?.Invoke(recipe);
        }

        public void OnPointerUpEvent()
        {
            OnPointerUp?.Invoke(recipe);
        }
    }
}