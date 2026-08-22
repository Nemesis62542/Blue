using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// ボーンごとに分けたコライダーから、所有者と部位を引くためのマーカー
    /// </summary>
    // 当たり判定を複数に分けると、当たったコライダー自身に GetComponent しても
    // 所有者が取れない。所有者への参照をここに持たせて EntityHit から引く。
    // 付与は EntityHitboxBuilder が行うので、手で設定する必要はない。
    [DisallowMultipleComponent]
    public class EntityPart : MonoBehaviour
    {
        [Tooltip("このコライダーが属するエンティティ。通常はルートの Controller")]
        [SerializeField] private MonoBehaviour owner;

        [Tooltip("このコライダーがどの部位か")]
        [SerializeField] private BodyPart part = BodyPart.Body;

        /// <summary>このコライダーが属するエンティティ</summary>
        public MonoBehaviour Owner => owner;

        /// <summary>このコライダーがどの部位か</summary>
        public BodyPart Part => part;

        /// <summary>
        /// 所有者と部位を設定する
        /// </summary>
        public void Setup(MonoBehaviour owner, BodyPart part)
        {
            this.owner = owner;
            this.part = part;
        }
    }
}
