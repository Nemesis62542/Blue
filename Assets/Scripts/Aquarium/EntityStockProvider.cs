using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 所持数の出どころを差し替えるための入口
    /// </summary>
    // 所持数は展示の復元より前に決まっていないといけない。
    // モデルを作ったあとで差し替えると、復元の時点では別の所持数で判定され、
    // 保存されていた展示が丸ごと弾かれる
    public abstract class EntityStockProvider : MonoBehaviour
    {
        public abstract IEntityStock CreateStock();
    }
}
