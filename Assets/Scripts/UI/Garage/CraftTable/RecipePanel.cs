using System;
using Blue.Recipe;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Garage.CraftTable
{
    public class RecipePanel : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private float pressAnimationDuration = 0.3f;

        private RecipeData recipe;
        private Tweener iconMoveTween;

        public event Action<RecipeData> OnPointerEnter;
        public event Action<RecipeData> OnPointerDown;
        public event Action<RecipeData> OnPointerUp;

        public RecipeData Recipe => recipe;

        public void Initialize(RecipeData recipe)
        {
            this.recipe = recipe;
            icon.sprite = recipe.ResultItem.Icon;
            Vector3 pos = icon.transform.localPosition;
            icon.transform.localPosition = new Vector3(pos.x, pos.y, -30);
        }

        private void OnDestroy()
        {
            iconMoveTween?.Kill();
        }

        public void OnPointerEnterEvent()
        {
            OnPointerEnter?.Invoke(recipe);
        }

        public void OnPointerDownEvent()
        {
            OnPointerDown?.Invoke(recipe);
            PlayPressAnimation();
        }

        public void OnPointerUpEvent()
        {
            OnPointerUp?.Invoke(recipe);
            PlayReleaseAnimation();
        }

        private void PlayPressAnimation()
        {
            iconMoveTween?.Kill();
            iconMoveTween = icon.transform
                .DOLocalMoveZ(0, pressAnimationDuration)
                .SetEase(Ease.OutCubic);
        }

        private void PlayReleaseAnimation()
        {
            iconMoveTween?.Kill();
            iconMoveTween = icon.transform
                .DOLocalMoveZ(-30, pressAnimationDuration)
                .SetEase(Ease.OutCubic);
        }
    }
}