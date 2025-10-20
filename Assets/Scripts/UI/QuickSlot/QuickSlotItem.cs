using Blue.Item;

namespace Blue.UI.QuickSlot
{
    /// <summary>
    /// クイックスロットに登録されるアイテム情報
    /// アイテムデータと個数をセットで保持
    /// </summary>
    public class QuickSlotItem
    {
        public ItemData ItemData { get; private set; }
        public int Quantity { get; private set; }

        public QuickSlotItem(ItemData item_data, int quantity)
        {
            ItemData = item_data;
            Quantity = quantity;
        }

        /// <summary>
        /// 個数を変更
        /// </summary>
        public void ModifyQuantity(int amount)
        {
            Quantity += amount;
            if (Quantity < 0) Quantity = 0;
        }

        /// <summary>
        /// 個数を設定
        /// </summary>
        public void SetQuantity(int quantity)
        {
            Quantity = quantity;
            if (Quantity < 0) Quantity = 0;
        }
    }
}
