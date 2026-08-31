using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// シーンに生成された設置物1つ分。モデルの写像であり、状態は持たない
    /// </summary>
    public abstract class AquariumPieceView : MonoBehaviour
    {
        /// <summary>
        /// 対応するモデル上の設置物
        /// </summary>
        public PlacedPiece Placed { get; private set; }

        public string InstanceID => Placed != null ? Placed.InstanceID : string.Empty;

        /// <summary>
        /// モデルと結びつける。AquariumBuilder が生成直後に呼ぶ
        /// </summary>
        public virtual void Bind(PlacedPiece placed)
        {
            Placed = placed;
        }

        /// <summary>
        /// 展示物を全て取り除く。撤去時に呼ぶ
        /// </summary>
        public virtual void ClearContents() { }
    }
}
