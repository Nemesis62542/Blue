using Blue.Item;

namespace Blue.Interface
{
    public interface ICapturable
    {
        /// <summary>
        /// 捕獲処理を試みる
        /// </summary>
        /// <param name="item">使用された捕獲アイテム</param>
        /// <returns>捕獲に成功したかどうか</returns>
        bool TryCapture(ItemData item);

        /// <summary>
        /// 捕獲成功時にインベントリへ追加するItemDataを返す
        /// </summary>
        ItemData GetCapturedItem();

        /// <summary>
        /// 捕獲対象の表示名
        /// </summary>
        string EntityName { get; }
    }
}