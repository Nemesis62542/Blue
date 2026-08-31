using System.Collections.Generic;
using Blue.Entity;
using Blue.Save;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 動作確認用の所持数。捕獲記録が空のときは、実体を持つ生物を一定数持っていることにする
    /// </summary>
    // 捕獲記録が無いと何も展示できず、水槽の見た目しか確かめられない。
    // 上限を外すのではなく所持していることにするので、展示UIは本番と同じ経路を通る
    public class DebugEntityStockProvider : EntityStockProvider
    {
        [SerializeField] private int ownedCountPerEntity = 3;

        public override IEntityStock CreateStock()
        {
            Dictionary<EntityData, int> captured = SaveDataConverter.LoadCapturedEntities();

            if (captured != null && captured.Count > 0)
            {
                Debug.Log($"[DebugEntityStockProvider] 捕獲記録があるためそのまま使います（{captured.Count} 種）");
                return new CapturedEntityStock(captured);
            }

            Dictionary<EntityData, int> granted = new Dictionary<EntityData, int>();

            foreach (EntityData entity in EntityDataCache.GetAllEntities())
            {
                // プレイヤーのように展示対象でないものが混ざるので、実体を持つものだけ拾う
                if (entity == null || (entity.Object == null && entity.School == null)) continue;

                granted[entity] = ownedCountPerEntity;
            }

            Debug.Log($"[DebugEntityStockProvider] 捕獲記録が無いため {granted.Count} 種を {ownedCountPerEntity} 匹ずつ所持していることにします");

            return new CapturedEntityStock(granted);
        }
    }
}
