using Blue.Item;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 収集アイテムを展示する台の定義
    /// </summary>
    [CreateAssetMenu(fileName = "PedestalPiece", menuName = "Blue/ScriptableObject/Aquarium/PedestalPiece")]
    public class PedestalPieceData : GridPieceData
    {
        [Header("展示条件")]
        [SerializeField] private int slotCount = 1;             // 同時に飾れる点数
        [SerializeField] private ItemType[] acceptedTypes;      // 空なら種類を問わない

        public int SlotCount => slotCount;

        /// <summary>
        /// 指定された種類のアイテムを飾れるか
        /// </summary>
        public bool AcceptsType(ItemType type)
        {
            if (acceptedTypes == null || acceptedTypes.Length == 0) return true;

            return System.Array.IndexOf(acceptedTypes, type) >= 0;
        }
    }
}
