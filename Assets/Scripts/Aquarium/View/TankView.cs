using System.Collections.Generic;
using Blue.Entity;
using Blue.Entity.Common;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 水槽1つ分。展示中の生物を生成し、遊泳範囲を水槽の内寸に合わせる
    /// </summary>
    public class TankView : AquariumPieceView
    {
        // 群れの中心が動ける範囲が内寸に占める割合。残りが個体1匹ずつの遊泳範囲になる。
        // 合計が内寸を超えると、群れがガラスを突き抜けて見える
        private const float SCHOOL_CENTRE_RANGE_RATIO = 0.4f;

        private readonly Dictionary<EntityData, List<GameObject>> spawned = new Dictionary<EntityData, List<GameObject>>();
        private readonly List<EntityData> removalBuffer = new List<EntityData>();

        private TankPieceData Tank => Placed?.Piece as TankPieceData;

        /// <summary>
        /// 展示内容をモデルに合わせる
        /// </summary>
        public void RefreshContents(IReadOnlyList<EntityData> entities)
        {
            if (Tank == null)
            {
                Debug.LogError($"TankPieceData ではない設置物に TankView が付いています: {name}", this);
                return;
            }

            Dictionary<EntityData, int> required = CountRequired(entities);

            // 差分だけを生成・破棄する。毎回作り直すと、1体足しただけで全部が消えて湧き直る
            RemoveSurplus(required);
            SpawnMissing(required);
        }

        public override void ClearContents()
        {
            foreach (KeyValuePair<EntityData, List<GameObject>> pair in spawned)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (pair.Value[i] != null) Destroy(pair.Value[i]);
                }
            }

            spawned.Clear();
        }

        private Dictionary<EntityData, int> CountRequired(IReadOnlyList<EntityData> entities)
        {
            Dictionary<EntityData, int> required = new Dictionary<EntityData, int>();

            if (entities == null) return required;

            for (int i = 0; i < entities.Count; i++)
            {
                EntityData entity = entities[i];
                if (entity == null) continue;

                required.TryGetValue(entity, out int count);
                required[entity] = count + 1;
            }

            return required;
        }

        private void RemoveSurplus(Dictionary<EntityData, int> required)
        {
            removalBuffer.Clear();

            foreach (KeyValuePair<EntityData, List<GameObject>> pair in spawned)
            {
                required.TryGetValue(pair.Key, out int needed);
                List<GameObject> instances = pair.Value;

                for (int i = instances.Count - 1; i >= needed; i--)
                {
                    if (instances[i] != null) Destroy(instances[i]);
                    instances.RemoveAt(i);
                }

                if (instances.Count == 0) removalBuffer.Add(pair.Key);
            }

            for (int i = 0; i < removalBuffer.Count; i++)
            {
                spawned.Remove(removalBuffer[i]);
            }

            removalBuffer.Clear();
        }

        private void SpawnMissing(Dictionary<EntityData, int> required)
        {
            foreach (KeyValuePair<EntityData, int> pair in required)
            {
                if (!spawned.TryGetValue(pair.Key, out List<GameObject> instances))
                {
                    instances = new List<GameObject>();
                    spawned[pair.Key] = instances;
                }

                for (int i = instances.Count; i < pair.Value; i++)
                {
                    GameObject instance = SpawnEntity(pair.Key);
                    if (instance != null) instances.Add(instance);
                }
            }
        }

        private GameObject SpawnEntity(EntityData entity)
        {
            GameObject prefab = entity.School != null ? entity.School.gameObject : entity.Object;
            if (prefab == null)
            {
                Debug.LogWarning($"展示に使うプレハブが設定されていません: {entity.Name}", this);
                return null;
            }

            Vector3 center = GetSwimCenter();
            Vector3 extents = GetSwimExtents();

            // 群れは SchoolController の Start が個体を撒くので、生成した時点ではまだ中身が無い。
            // 撒かれる前に設定を渡す必要があるため、群れと単体で入口を分けている
            if (entity.School != null)
            {
                GameObject school_instance = Instantiate(prefab, center, transform.rotation, transform);
                ConfigureSchool(school_instance, extents);
                return school_instance;
            }

            GameObject instance = Instantiate(prefab, RandomPointInside(center, extents), Random.rotation, transform);
            ConfigureSwimmer(instance, center, extents);
            return instance;
        }

        private void ConfigureSwimmer(GameObject instance, Vector3 center, Vector3 extents)
        {
            BaseSwimmer swimmer = instance.GetComponentInChildren<BaseSwimmer>();
            if (swimmer == null) return;

            swimmer.SetRoamCenter(center);
            swimmer.SetRoamArea(extents);

            // 回遊は縄張りの中心ごと動かすので、水槽では必ず切る
            swimmer.SetMigrationEnabled(false);
        }

        private void ConfigureSchool(GameObject instance, Vector3 extents)
        {
            SchoolController school = instance.GetComponent<SchoolController>();
            if (school == null)
            {
                Debug.LogWarning($"群れのプレハブに SchoolController がありません: {instance.name}", this);
                return;
            }

            // 水槽ごと破棄したときに個体が取り残されないよう、必ず配下へ入れる
            school._groupChildToSchool = true;
            school._groupChildToNewTransform = false;
            school._posOffset = Vector3.zero;
            school._childAmount = Tank.SchoolDisplayCount;

            // 群れの中心が動く範囲と、個体が中心のまわりを泳ぐ範囲の合計が内寸に収まるようにする
            Vector3 centre_range = extents * SCHOOL_CENTRE_RANGE_RATIO;
            Vector3 member_range = extents - centre_range;

            school._positionSphere = centre_range.x;
            school._positionSphereHeight = centre_range.y;
            school._positionSphereDepth = centre_range.z;

            school._spawnSphere = member_range.x;
            school._spawnSphereHeight = member_range.y;
            school._spawnSphereDepth = member_range.z;
        }

        private Vector3 GetSwimCenter()
        {
            return transform.TransformPoint(Tank.SwimAreaCenter);
        }

        private Vector3 GetSwimExtents()
        {
            // 水槽は90度刻みでしか回らないので、軸ごとの絶対値を取れば回転後の内寸になる
            Vector3 world = transform.TransformVector(Tank.SwimAreaExtents);

            return new Vector3(Mathf.Abs(world.x), Mathf.Abs(world.y), Mathf.Abs(world.z));
        }

        private static Vector3 RandomPointInside(Vector3 center, Vector3 extents)
        {
            return center + new Vector3(
                Random.Range(-extents.x, extents.x),
                Random.Range(-extents.y, extents.y),
                Random.Range(-extents.z, extents.z)
            );
        }

        private void OnDrawGizmosSelected()
        {
            TankPieceData tank = Tank;
            if (tank == null) return;

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(tank.SwimAreaCenter, tank.SwimAreaSize);
        }
    }
}
