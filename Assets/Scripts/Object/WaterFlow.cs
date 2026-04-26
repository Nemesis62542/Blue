using System.Collections.Generic;
using UnityEngine;

namespace Blue.Object
{
    /// <summary>
    /// 水流システム
    /// 指定方向にRigidBodyを押し出し、ParticleSystemと連動
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WaterFlow : MonoBehaviour
    {
        [Header("水流設定")]
        [SerializeField] private Vector3 flowDirection = Vector3.forward;
        [SerializeField] private float strength = 10f;
        [SerializeField] private bool isActive = true;

        [Header("パーティクル")]
        [SerializeField] private ParticleSystem flowParticle;

        // 水流エリア内のRigidbody
        private HashSet<Rigidbody> affectedBodies = new HashSet<Rigidbody>();

        // 子パーティクル含むすべてのParticleSystem
        private ParticleSystem[] allParticles;

        // 基準となるStartSpeed（強さ1.0時の値）
        private float[] baseStartSpeeds;

        // 水流方向の長さ（Lifetime計算用）
        private float flowLength;

        private void Awake()
        {
            // コライダーをトリガーに設定
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            // 親子含むすべてのParticleSystemを取得
            if (flowParticle != null)
            {
                allParticles = flowParticle.GetComponentsInChildren<ParticleSystem>();
                baseStartSpeeds = new float[allParticles.Length];

                // 基準StartSpeedを保存
                for (int i = 0; i < allParticles.Length; i++)
                {
                    ParticleSystem.MainModule main = allParticles[i].main;
                    baseStartSpeeds[i] = main.startSpeed.constant;
                }

                // BoxColliderのサイズをParticleのShapeに適用
                ApplyColliderToParticleShape(col);
            }

            // 初期状態を適用
            ApplyActiveState();
        }

        private void FixedUpdate()
        {
            if (!isActive) return;

            // ワールド空間での水流方向
            Vector3 worldDirection = transform.TransformDirection(flowDirection.normalized);
            Vector3 force = worldDirection * strength;

            // 影響下のすべてのRigidbodyに力を加える
            foreach (Rigidbody rb in affectedBodies)
            {
                if (rb != null)
                {
                    rb.AddForce(force, ForceMode.Acceleration);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null)
            {
                affectedBodies.Add(other.attachedRigidbody);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody != null)
            {
                affectedBodies.Remove(other.attachedRigidbody);
            }
        }

        /// <summary>
        /// 水流の有効/無効を切り替える
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            ApplyActiveState();
        }

        /// <summary>
        /// 水流の有効/無効をトグルする
        /// </summary>
        public void ToggleActive()
        {
            SetActive(!isActive);
        }

        /// <summary>
        /// 水流の強さを設定する
        /// </summary>
        /// <param name="newStrength">新しい強さ</param>
        public void SetStrength(float newStrength)
        {
            strength = newStrength;
            UpdateParticleSettings();
        }

        /// <summary>
        /// 水流が有効かどうか
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// 現在の水流の強さ
        /// </summary>
        public float Strength => strength;

        private void ApplyActiveState()
        {
            if (flowParticle == null) return;

            if (isActive)
            {
                UpdateParticleSettings();
                flowParticle.Play(true); // withChildren=true
            }
            else
            {
                flowParticle.Stop(true); // withChildren=true
            }
        }

        private void UpdateParticleSettings()
        {
            if (allParticles == null) return;

            for (int i = 0; i < allParticles.Length; i++)
            {
                float speed = baseStartSpeeds[i] * strength;
                // パーティクルがBoxCollider内に収まるようLifetimeを計算
                float lifetime = speed > 0f ? flowLength / speed : 0f;

                ParticleSystem.MainModule main = allParticles[i].main;
                main.startSpeed = speed;
                main.startLifetime = lifetime;
            }
        }

        private void ApplyColliderToParticleShape(Collider col)
        {
            if (allParticles == null) return;
            if (col is not BoxCollider boxCollider) return;

            Vector3 normalizedDir = flowDirection.normalized;

            // 水流方向に沿ったBoxColliderの長さを計算
            flowLength = Mathf.Abs(normalizedDir.x) * boxCollider.size.x +
                         Mathf.Abs(normalizedDir.y) * boxCollider.size.y +
                         Mathf.Abs(normalizedDir.z) * boxCollider.size.z;

            // パーティクルの発生位置を水流の上流側（端）にオフセット
            Vector3 shapeOffset = boxCollider.center - normalizedDir * (flowLength * 0.5f);

            // 水流方向に垂直な面のサイズを計算（パーティクルの発生範囲）
            Vector3 perpendicularSize = boxCollider.size;
            perpendicularSize.x *= 1f - Mathf.Abs(normalizedDir.x);
            perpendicularSize.y *= 1f - Mathf.Abs(normalizedDir.y);
            perpendicularSize.z *= 1f - Mathf.Abs(normalizedDir.z);

            // 親子すべてのパーティクルに設定を適用
            foreach (ParticleSystem ps in allParticles)
            {
                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.position = shapeOffset;
                shape.scale = perpendicularSize;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 水流方向を可視化
            Gizmos.color = isActive ? Color.cyan : Color.gray;
            Vector3 worldDirection = transform.TransformDirection(flowDirection.normalized);
            Gizmos.DrawRay(transform.position, worldDirection * 2f);

            // 矢印の先端
            Vector3 arrowEnd = transform.position + worldDirection * 2f;
            Vector3 right = Vector3.Cross(Vector3.up, worldDirection).normalized;
            if (right.magnitude < 0.01f)
            {
                right = Vector3.Cross(Vector3.forward, worldDirection).normalized;
            }
            Gizmos.DrawRay(arrowEnd, (-worldDirection + right) * 0.3f);
            Gizmos.DrawRay(arrowEnd, (-worldDirection - right) * 0.3f);
        }
#endif
    }
}
