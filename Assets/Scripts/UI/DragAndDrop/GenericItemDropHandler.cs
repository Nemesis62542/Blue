using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Blue.Item;
using Blue.UI.Inventory;

namespace Blue.UI.DragAndDrop
{
    /// <summary>
    /// 汎用的なアイテムドロップハンドラ
    /// インベントリ間のアイテム移動に使用
    /// </summary>
    public class GenericItemDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IItemDropTarget
    {
        [SerializeField] private Image highlightImage;

        private IItemContainer targetContainer;
        private Color defaultColor;

        public void Setup(IItemContainer container)
        {
            targetContainer = container;
            if (highlightImage != null)
            {
                defaultColor = highlightImage.color;
            }
        }

        public void OnDrop(PointerEventData event_data)
        {
            if (event_data.pointerDrag != null &&
                event_data.pointerDrag.TryGetComponent(out IDraggableItemSlot draggable))
            {
                ItemData item_data = draggable.GetItemData();
                int quantity = draggable.GetItemQuantity();
                IItemContainer source_container = draggable.GetSourceContainer();

                if (CanAcceptItem(item_data, quantity))
                {
                    if (event_data.pointerDrag.TryGetComponent(out ItemSlotDragHandler drag_handler))
                    {
                        drag_handler.ForceEndDrag();
                    }

                    OnItemDropped(item_data, quantity, source_container);
                }
            }

            if (highlightImage != null)
            {
                highlightImage.color = defaultColor;
            }
        }

        public void OnPointerEnter(PointerEventData event_data)
        {
            if (highlightImage != null && event_data.pointerDrag != null)
            {
                highlightImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        public void OnPointerExit(PointerEventData event_data)
        {
            if (highlightImage != null)
            {
                highlightImage.color = defaultColor;
            }
        }

        public IItemContainer GetTargetContainer()
        {
            return targetContainer;
        }

        public bool CanAcceptItem(ItemData item_data, int quantity)
        {
            return targetContainer != null && item_data != null && quantity > 0;
        }

        public void OnItemDropped(ItemData item_data, int quantity, IItemContainer source_container)
        {
            if (source_container == null || targetContainer == null)
            {
                return;
            }

            if (source_container == targetContainer)
            {
                return;
            }

            if (source_container.RemoveItem(item_data, quantity))
            {
                if (targetContainer.AddItem(item_data, quantity))
                {
                    source_container.UpdateView();
                    targetContainer.UpdateView();
                }
                else
                {
                    source_container.AddItem(item_data, quantity);
                    source_container.UpdateView();
                }
            }
        }
    }
}
