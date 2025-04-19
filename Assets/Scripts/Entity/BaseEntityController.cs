using UnityEngine;

namespace Blue.Entity
{
    public abstract class BaseEntityController<TModel, TView> : MonoBehaviour
        where TModel : BaseEntityModel
        where TView : BaseEntityView
    {
        [SerializeField] protected TView view;
        protected TModel model;

        protected virtual void Awake()
        {
            if (view == null && !TryGetComponent(out view))
            {
                Debug.LogError($"{name} : Viewが設定されていません");
            }
        }
    }
}