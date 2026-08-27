using Blue.Attack;
using Blue.Interface;
using UnityEngine;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 群れに所属する 1 匹。移動は BaseSwimmer、群れとしての振る舞いは SchoolBehaviour が持つ
    /// </summary>
    // 旧 SchoolChild は移動・回避・押し戻し・群れ制御を 1 クラスで抱えていたため、
    // BaseSwimmer 側の改善が群れの魚には一切効かなかった。ここでは所属の管理だけを担う。
    [RequireComponent(typeof(BaseSwimmer))]
    public class SchoolMember : MonoBehaviour, ILivingEntity
    {
        private SchoolController school;
        private BaseSwimmer swimmer;

        /// <summary>
        /// 所属する群れ
        /// </summary>
        public SchoolController School => school;

        /// <summary>
        /// 移動制御
        /// </summary>
        public BaseSwimmer Swimmer => swimmer;

        // 群れの魚は個体ごとの体力を持たない。脅威としての大きさは群れ全体の設定に従う
        public Status Status => null;
        public float Size => school != null ? school._schoolThreatSize : 0f;
        public float ThreatSizeThreshold => school != null ? school._schoolThreatThreshold : -1f;

        public void Damage(AttackData attackData) { }

        public void OnDead() { }

        /// <summary>
        /// 生成直後に所属先を渡す
        /// </summary>
        public void Initialize(SchoolController owner)
        {
            school = owner;

            swimmer = GetComponent<BaseSwimmer>();

            // 群れの中心は SchoolController が動かすので、個体側の回遊とは併用しない
            swimmer.SetRoamCenter(school.SchoolCenter);
            swimmer.SetBehaviour(new SchoolBehaviour(this));

            ApplyRandomScale();
        }

        private void ApplyRandomScale()
        {
            float scale = Random.Range(school._minScale, school._maxScale);
            transform.localScale = Vector3.one * scale;
        }

        private void OnEnable()
        {
            if (school != null) school.Register(this);
        }

        private void OnDisable()
        {
            if (school != null) school.Unregister(this);
        }
    }
}
