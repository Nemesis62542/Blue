using Blue.Item;

namespace Blue.UI.DragAndDrop
{
    /// <summary>
    /// ドラッグ可能なアイテムスロットのインターフェース
    /// </summary>
    public interface IDraggableItemSlot
    {
        /// <summary>
        /// ドラッグ中のアイテムデータを取得
        /// </summary>
        /// <returns>アイテムデータ</returns>
        ItemData GetItemData();

        /// <summary>
        /// ドラッグ中のアイテムの個数を取得
        /// </summary>
        /// <returns>個数</returns>
        int GetItemQuantity();

        /// <summary>
        /// このアイテムが属するコンテナを取得
        /// </summary>
        /// <returns>元のコンテナ</returns>
        IItemContainer GetSourceContainer();
    }
}
