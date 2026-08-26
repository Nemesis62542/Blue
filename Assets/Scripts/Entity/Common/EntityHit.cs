using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 当たった対象から、そのエンティティと部位を解決する
    /// </summary>
    // 単一コライダーのエンティティと、ボーンごとに分けたエンティティが混在するので、
    // 呼び出し側がどちらかを気にしなくて済むようここへ寄せている。
    // 小型の生物までボーン分割する必要はないため、この混在は解消しない前提。
    public static class EntityHit
    {
        /// <summary>
        /// 当たった GameObject から、目的の型を持つ所有者と部位を取り出す
        /// </summary>
        /// <param name="target">当たった対象</param>
        /// <param name="owner">見つかった所有者</param>
        /// <param name="part">当たった部位。分割していないエンティティは Body</param>
        public static bool TryResolve<T>(GameObject target, out T owner, out BodyPart part) where T : class
        {
            owner = null;
            part = BodyPart.Body;

            if (target == null) return false;

            // ボーンごとに分けたコライダー
            if (target.TryGetComponent(out EntityPart entityPart) && entityPart.Owner != null)
            {
                part = entityPart.Part;
                owner = entityPart.Owner as T;

                return owner != null;
            }

            // 単一コライダーのエンティティ
            return target.TryGetComponent(out owner);
        }

        /// <summary>
        /// 当たったコライダーから、目的の型を持つ所有者と部位を取り出す
        /// </summary>
        public static bool TryResolve<T>(Collider collider, out T owner, out BodyPart part) where T : class
        {
            return TryResolve(collider != null ? collider.gameObject : null, out owner, out part);
        }

        /// <summary>
        /// 部位を使わない場合の簡易版
        /// </summary>
        public static bool TryResolve<T>(GameObject target, out T owner) where T : class
        {
            return TryResolve(target, out owner, out _);
        }

        /// <summary>
        /// 部位を使わない場合の簡易版
        /// </summary>
        public static bool TryResolve<T>(Collider collider, out T owner) where T : class
        {
            return TryResolve(collider, out owner, out _);
        }
    }
}
