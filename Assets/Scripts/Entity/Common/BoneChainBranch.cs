using UnityEngine;
using System;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 分岐ボーン（ヒレなど）を制御するクラス
    /// メインチェーンの特定セグメントに追従する
    /// </summary>
    // メインチェーンと違い、こちらは Animator の姿勢ではなく初期姿勢を基準にする。
    // ヒレのようにアニメーションを持たないボーンでも、慣性で倒れた分が
    // 初期姿勢へ戻ってくるようにするため。
    [Serializable]
    public class BoneChainBranch
    {
        [Header("Branch Settings")]
        [SerializeField] private string branchName;

        [Tooltip("根元から末端への順序で設定")]
        [SerializeField] private Transform[] branchBones;

        [Header("Follow Settings")]
        [Tooltip("追従する親セグメントのインデックス（0始まり）")]
        [SerializeField] private int parentSegmentIndex;

        [Tooltip("親の移動速度に対して倒れる量")]
        [SerializeField, Range(0f, 1f)] private float followStrength = 0.5f;

        [Tooltip("追従の遅れ（0=即座に追従、1に近いほど遅い）")]
        [SerializeField, Range(0f, 0.99f)] private float damping = 0.2f;

        [SerializeField] private bool enabled = true;

        private const float MaxDamping = 0.999f;
        private const float MinDirectionSqr = 0.0001f;

        // ランタイムキャッシュ
        private Quaternion[] initialLocalRotations;
        private Vector3[] childLocalDirections;
        private Quaternion[] currentRotations;
        private bool isInitialized;

        // プロパティ
        public string BranchName => branchName;
        public Transform[] BranchBones => branchBones;
        public int ParentSegmentIndex => parentSegmentIndex;
        public float FollowStrength => followStrength;
        public float Damping => damping;
        public bool Enabled { get => enabled; set => enabled = value; }
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 分岐の初期化
        /// </summary>
        public void Initialize()
        {
            if (branchBones == null || branchBones.Length == 0)
            {
                Debug.LogWarning($"[BoneChainBranch] {branchName}: No branch bones defined.");
                return;
            }

            initialLocalRotations = new Quaternion[branchBones.Length];
            childLocalDirections = new Vector3[branchBones.Length];
            currentRotations = new Quaternion[branchBones.Length];

            for (int i = 0; i < branchBones.Length; i++)
            {
                if (branchBones[i] == null) continue;

                initialLocalRotations[i] = branchBones[i].localRotation;
                currentRotations[i] = branchBones[i].rotation;

                // 次のボーンへ向かう方向を、自分のローカル空間で保持する
                if (i < branchBones.Length - 1 && branchBones[i + 1] != null)
                {
                    Vector3 toChild = branchBones[i + 1].position - branchBones[i].position;

                    childLocalDirections[i] = toChild.sqrMagnitude > MinDirectionSqr
                        ? branchBones[i].InverseTransformDirection(toChild.normalized)
                        : Vector3.forward;
                }
            }

            isInitialized = true;
        }

        /// <summary>
        /// 分岐ボーンの更新
        /// </summary>
        /// <param name="parentVelocity">親セグメントの速度</param>
        /// <param name="deltaTime">デルタタイム</param>
        /// <param name="followSpeed">追従速度（メインチェーンと共通）</param>
        public void UpdateBranch(Vector3 parentVelocity, float deltaTime, float followSpeed)
        {
            if (!enabled || !isInitialized || branchBones == null) return;
            if (deltaTime <= 0f) return;

            // 親の移動と逆向きに倒れる
            Vector3 inertia = -parentVelocity * followStrength;

            float t = 1f - Mathf.Pow(Mathf.Clamp(damping, 0f, MaxDamping), deltaTime * followSpeed);
            t = Mathf.Clamp01(t);

            // 末端ボーンは子を持たないので回転させても形状が変わらない
            for (int i = 0; i < branchBones.Length - 1; i++)
            {
                Transform bone = branchBones[i];
                if (bone == null || branchBones[i + 1] == null) continue;

                // 親の現在姿勢のうえで初期姿勢を取ったときの回転・方向
                Quaternion parentRotation = bone.parent != null ? bone.parent.rotation : Quaternion.identity;
                Quaternion restRotation = parentRotation * initialLocalRotations[i];
                Vector3 restDirection = restRotation * childLocalDirections[i];

                // 慣性で倒した先へ、ボーンの軸のとり方に依存しない形で振る
                Vector3 targetDirection = restDirection + inertia;

                Quaternion targetRotation = targetDirection.sqrMagnitude > MinDirectionSqr
                    ? Quaternion.FromToRotation(restDirection, targetDirection.normalized) * restRotation
                    : restRotation;

                bone.rotation = Quaternion.Slerp(currentRotations[i], targetRotation, t);
                currentRotations[i] = bone.rotation;
            }
        }

        /// <summary>
        /// 分岐を初期姿勢にリセット
        /// </summary>
        public void ResetToInitialPose()
        {
            if (!isInitialized || branchBones == null) return;

            for (int i = 0; i < branchBones.Length; i++)
            {
                if (branchBones[i] == null) continue;

                branchBones[i].localRotation = initialLocalRotations[i];
                currentRotations[i] = branchBones[i].rotation;
            }
        }
    }
}
