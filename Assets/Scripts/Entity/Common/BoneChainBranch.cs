using UnityEngine;
using System;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 分岐ボーン（ヒレなど）を制御するクラス
    /// メインチェーンの特定セグメントに追従する
    /// </summary>
    [Serializable]
    public class BoneChainBranch
    {
        [Header("Branch Settings")]
        [SerializeField] private string branchName;
        [SerializeField] private Transform branchRoot;
        [SerializeField] private Transform[] branchBones;

        [Header("Follow Settings")]
        [Tooltip("追従する親セグメントのインデックス（0始まり）")]
        [SerializeField] private int parentSegmentIndex;
        [SerializeField, Range(0f, 1f)] private float followStrength = 0.5f;
        [SerializeField, Range(0f, 1f)] private float damping = 0.2f;
        [SerializeField] private bool enabled = true;

        // ランタイムキャッシュ
        private float[] boneLengths;
        private Quaternion[] initialLocalRotations;
        private Vector3[] previousPositions;
        private bool isInitialized;

        // プロパティ
        public string BranchName => branchName;
        public Transform BranchRoot => branchRoot;
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

            boneLengths = new float[branchBones.Length];
            initialLocalRotations = new Quaternion[branchBones.Length];
            previousPositions = new Vector3[branchBones.Length];

            for (int i = 0; i < branchBones.Length; i++)
            {
                if (branchBones[i] == null) continue;

                initialLocalRotations[i] = branchBones[i].localRotation;
                previousPositions[i] = branchBones[i].position;

                // ボーン長を計算
                if (i < branchBones.Length - 1 && branchBones[i + 1] != null)
                {
                    boneLengths[i] = Vector3.Distance(
                        branchBones[i].position,
                        branchBones[i + 1].position
                    );
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

            for (int i = 0; i < branchBones.Length; i++)
            {
                if (branchBones[i] == null) continue;

                // 親の動きに対する遅延追従（速度の逆方向に少し遅れる）
                Vector3 inertiaOffset = -parentVelocity * followStrength * deltaTime;

                // 目標位置を計算
                Vector3 targetPos = branchBones[i].position + inertiaOffset;

                // 回転の計算（前のボーンを向く）
                if (i > 0 && branchBones[i - 1] != null)
                {
                    Vector3 dirToParent = (branchBones[i - 1].position - branchBones[i].position).normalized;
                    if (dirToParent.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dirToParent);
                        branchBones[i].rotation = Quaternion.Slerp(
                            branchBones[i].rotation,
                            targetRot,
                            damping
                        );
                    }
                }

                // ボーン長の制約を適用
                if (i > 0 && branchBones[i - 1] != null && boneLengths[i - 1] > 0f)
                {
                    Vector3 parentBonePos = branchBones[i - 1].position;
                    Vector3 dir = (branchBones[i].position - parentBonePos).normalized;

                    if (dir.sqrMagnitude < 0.001f)
                    {
                        dir = -branchBones[i - 1].forward;
                    }

                    branchBones[i].position = parentBonePos + dir * boneLengths[i - 1];
                }

                previousPositions[i] = branchBones[i].position;
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
                if (branchBones[i] != null)
                {
                    branchBones[i].localRotation = initialLocalRotations[i];
                }
            }
        }
    }
}
