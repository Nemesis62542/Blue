using UnityEngine;
using System;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 分岐ボーン（ヒレなど）を制御するクラス
    /// メインチェーンの特定セグメントに追従する
    /// </summary>
    // メインチェーンと違い、こちらは Animator の姿勢ではなく初期姿勢を基準にする。
    // ヒレのようにアニメーションを持たないボーンでも、倒れた分が初期姿勢へ戻ってくるようにするため。
    [Serializable]
    public class BoneChainBranch
    {
        [Header("Branch Settings")]
        [SerializeField] private string branchName;

        [Tooltip("根元から末端への順序で設定。末端は向きの基準にしか使わないので最低2本必要")]
        [SerializeField] private Transform[] branchBones;

        [Header("Follow Settings")]
        [Tooltip("追従する親セグメントのインデックス（0始まり）")]
        [SerializeField] private int parentSegmentIndex;

        [Tooltip("追従の遅れ時間（秒）")]
        [SerializeField, Range(0.01f, 1f)] private float lagTime = 0.12f;

        [Tooltip("行き過ぎて揺れ戻る量（0=行き過ぎなし、大きいほど揺れが残る）")]
        [SerializeField, Range(0f, 0.9f)] private float overshoot = 0.6f;

        [Tooltip("進行方向と逆に寝かせる量。親の移動速度に比例するので泳ぐほど後ろへ倒れる")]
        [SerializeField, Range(0f, 1f)] private float followStrength = 0.2f;

        [SerializeField] private bool enabled = true;

        private const float MinDirectionSqr = 0.0001f;

        // ランタイムキャッシュ
        private Quaternion[] initialLocalRotations;
        private Vector3[] childLocalDirections;
        private Quaternion[] currentRotations;
        private Vector3[] angularVelocities;
        private bool isInitialized;

        // プロパティ
        public string BranchName => branchName;
        public Transform[] BranchBones => branchBones;
        public int ParentSegmentIndex => parentSegmentIndex;
        public float FollowStrength => followStrength;
        public float LagTime => lagTime;
        public bool Enabled { get => enabled; set => enabled = value; }
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 分岐の初期化
        /// </summary>
        public void Initialize()
        {
            if (branchBones == null || branchBones.Length < 2)
            {
                Debug.LogWarning($"[BoneChainBranch] {branchName}: 分岐には末端を含めて2本以上のボーンが必要です。");
                return;
            }

            initialLocalRotations = new Quaternion[branchBones.Length];
            childLocalDirections = new Vector3[branchBones.Length];
            currentRotations = new Quaternion[branchBones.Length];
            angularVelocities = new Vector3[branchBones.Length];

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
        public void UpdateBranch(Vector3 parentVelocity, float deltaTime)
        {
            if (!enabled || !isInitialized || branchBones == null) return;
            if (deltaTime <= 0f) return;

            // 親の移動と逆向きに寝かせる
            Vector3 inertia = -parentVelocity * followStrength;

            // 末端ボーンは子を持たないので回転させても形状が変わらない
            for (int i = 0; i < branchBones.Length - 1; i++)
            {
                Transform bone = branchBones[i];
                if (bone == null || branchBones[i + 1] == null) continue;

                // 親の現在姿勢のうえで初期姿勢を取ったときの回転と、その向き
                Quaternion parentRotation = bone.parent != null ? bone.parent.rotation : Quaternion.identity;
                Quaternion restRotation = parentRotation * initialLocalRotations[i];
                Vector3 restDirection = restRotation * childLocalDirections[i];

                // 慣性で倒した先へ、ボーンの軸のとり方に依存しない形で振る
                Vector3 targetDirection = restDirection + inertia;

                Quaternion targetRotation = targetDirection.sqrMagnitude > MinDirectionSqr
                    ? Quaternion.FromToRotation(restDirection, targetDirection.normalized) * restRotation
                    : restRotation;

                Vector3 angularVelocity = angularVelocities[i];
                currentRotations[i] = BoneChainSpring.Step(
                    currentRotations[i], targetRotation, ref angularVelocity, lagTime, overshoot, deltaTime);
                angularVelocities[i] = angularVelocity;

                bone.rotation = currentRotations[i];
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
                angularVelocities[i] = Vector3.zero;
            }
        }
    }
}
