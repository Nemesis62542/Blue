using System.Collections.Generic;
using Blue.Entity;
using Blue.Save;

namespace Blue.Aquarium
{
    /// <summary>
    /// 生物を何匹持っているかを答える
    /// </summary>
    // 展示は所持数を減らさない。出し入れは自由にしたいので、消費ではなく
    // 「同時に展示できる数の上限」として使う。水槽から出せばすぐ別の水槽へ入れられる
    public interface IEntityStock
    {
        int GetOwnedCount(EntityData entity);

        /// <summary>
        /// 1匹でも持っている生物を挙げる。展示UIの候補一覧に使う
        /// </summary>
        IEnumerable<EntityData> EnumerateOwned();
    }

    /// <summary>
    /// 捕獲済みの生物を所持数とみなす
    /// </summary>
    public class CapturedEntityStock : IEntityStock
    {
        private readonly Dictionary<EntityData, int> captured;

        public CapturedEntityStock()
            : this(SaveDataConverter.LoadCapturedEntities())
        {
        }

        public CapturedEntityStock(Dictionary<EntityData, int> captured_entities)
        {
            captured = captured_entities ?? new Dictionary<EntityData, int>();
        }

        public int GetOwnedCount(EntityData entity)
        {
            if (entity == null) return 0;

            return captured.TryGetValue(entity, out int count) ? count : 0;
        }

        public IEnumerable<EntityData> EnumerateOwned()
        {
            return captured.Keys;
        }
    }
}
