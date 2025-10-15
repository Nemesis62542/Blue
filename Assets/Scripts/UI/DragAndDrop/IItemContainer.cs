using Blue.Item;

namespace Blue.UI.DragAndDrop
{
    /// <summary>
    /// アイテムを保持・管理するコンテナのインターフェース
    /// プレイヤーインベントリ、倉庫、アイテムボックスなどが実装
    /// </summary>
    public interface IItemContainer
    {
        /// <summary>
        /// コンテナからアイテムを削除
        /// </summary>
        /// <param name="item_data">削除するアイテムのデータ</param>
        /// <param name="quantity">削除する個数</param>
        /// <returns>削除に成功した場合true</returns>
        bool RemoveItem(ItemData item_data, int quantity);

        /// <summary>
        /// コンテナにアイテムを追加
        /// </summary>
        /// <param name="item_data">追加するアイテムのデータ</param>
        /// <param name="quantity">追加する個数</param>
        /// <returns>追加に成功した場合true</returns>
        bool AddItem(ItemData item_data, int quantity);

        /// <summary>
        /// コンテナのビューを更新
        /// </summary>
        void UpdateView();
    }
}
