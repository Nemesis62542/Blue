using Blue.Item;

namespace Blue.UI.DragAndDrop
{
    /// <summary>
    /// アイテムをドロップできる場所のインターフェース
    /// </summary>
    public interface IItemDropTarget
    {
        /// <summary>
        /// このドロップ先が属するコンテナを取得
        /// </summary>
        /// <returns>ドロップ先のコンテナ</returns>
        IItemContainer GetTargetContainer();

        /// <summary>
        /// 指定されたアイテムを受け入れ可能かチェック
        /// </summary>
        /// <param name="item_data">アイテムデータ</param>
        /// <param name="quantity">個数</param>
        /// <returns>受け入れ可能な場合true</returns>
        bool CanAcceptItem(ItemData item_data, int quantity);

        /// <summary>
        /// アイテムがドロップされた時の処理
        /// </summary>
        /// <param name="item_data">アイテムデータ</param>
        /// <param name="quantity">個数</param>
        /// <param name="source_container">元のコンテナ</param>
        void OnItemDropped(ItemData item_data, int quantity, IItemContainer source_container);
    }
}
