using UnityEngine;
using System.Collections.Generic;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 魚の胴体のようなチェーンボーンのプロシージャルIK
    /// 頭が先導し、胴体・尻尾が追従する
    /// ボーン長を保持しながら滑らかに曲がる
    /// </summary>
    public class ProceduralBoneChain : MonoBehaviour
    {
        [Header("Chain Settings")]
        [Tooltip("先頭ボーン（頭）から末端ボーン（尻尾）への順序で設定")]
        [SerializeField] private List<BoneChainSegment> segments = new List<BoneChainSegment>();

        [Header("Follow Settings")]
        [Tooltip("回転の追従速度（大きいほど速く追従）")]
        [SerializeField, Range(1f, 50f)] private float rotationSpeed = 15f;

        [Tooltip("末端に向かうほど遅延を増加させる係数")]
        [SerializeField, Range(0f, 2f)] private float dampingFalloff = 0.5f;

        [Tooltip("Animatorアニメーションを使用するか")]
        [SerializeField] private bool useAnimatorRotation = true;

        [Header("Constraints")]
        [SerializeField] private bool constrainBoneLength = true;
        [SerializeField, Range(0f, 180f)] private float maxBendAngle = 60f;

        [Header("Branch Settings")]
        [SerializeField] private List<BoneChainBranch> branches = new List<BoneChainBranch>();

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;
        [SerializeField] private Color debugBoneColor = Color.cyan;
        [SerializeField] private Color debugConstraintColor = Color.yellow;

        // ランタイム
        private bool isInitialized = false;
        private Transform headBone;
        private Vector3 previousHeadPosition;

        // プロパティ
        public List<BoneChainSegment> Segments => segments;
        public List<BoneChainBranch> Branches => branches;
        public bool IsInitialized => isInitialized;
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = Mathf.Max(1f, value); }

        #region Unity Lifecycle

        /// <summary>
        /// LateUpdateでAnimator更新後にボーンを制御
        /// </summary>
        private void LateUpdate()
        {
            // 最初のLateUpdateで初期化（Animatorがボーンを更新した後）
            if (!isInitialized)
            {
                Initialize();
                return; // 初期化フレームは更新しない
            }

            if (segments.Count == 0) return;

            UpdateBoneChain();
            UpdateBranches();

            // 状態を保存
            if (headBone != null)
            {
                previousHeadPosition = headBone.position;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ボーンチェーンの初期化
        /// </summary>
        public void Initialize()
        {
            if (segments.Count == 0)
            {
                Debug.LogWarning($"[ProceduralBoneChain] {name}: No segments defined.");
                return;
            }

            // 各セグメントを初期化
            for (int i = 0; i < segments.Count; i++)
            {
                Transform nextBone = (i < segments.Count - 1)
                    ? segments[i + 1].Bone
                    : null;
                segments[i].Initialize(nextBone);
            }

            // 先頭ボーンをキャッシュ
            headBone = segments[0].Bone;
            if (headBone != null)
            {
                previousHeadPosition = headBone.position;
            }

            // 分岐の初期化
            foreach (BoneChainBranch branch in branches)
            {
                branch.Initialize();
            }

            isInitialized = true;
        }

        #endregion

        #region Bone Chain Update

        /// <summary>
        /// ボーンチェーン全体の更新
        /// </summary>
        private void UpdateBoneChain()
        {
            if (headBone == null) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            // 先頭ボーンの回転を保存（先頭は制御しない）
            segments[0].CurrentRotation = headBone.rotation;
            segments[0].PreviousPosition = headBone.position;

            // 後続のボーンを順番に更新（前から後ろへ）
            for (int i = 1; i < segments.Count; i++)
            {
                BoneChainSegment currentSegment = segments[i];
                BoneChainSegment parentSegment = segments[i - 1];

                if (currentSegment.Bone == null || parentSegment.Bone == null) continue;

                // 末端に向かうほどダンピングを増加
                float chainProgress = (float)i / (segments.Count - 1);
                float additionalDamping = chainProgress * dampingFalloff;

                UpdateSegment(currentSegment, parentSegment, deltaTime, additionalDamping);
            }
        }

        /// <summary>
        /// 個別セグメントの更新
        /// </summary>
        private void UpdateSegment(BoneChainSegment segment, BoneChainSegment parent, float deltaTime, float additionalDamping)
        {
            Transform bone = segment.Bone;
            Transform parentBone = parent.Bone;

            // === Step 1: 位置の制約（ボーン長を保持） ===
            if (constrainBoneLength && segment.BoneLength > 0f)
            {
                // 現在の位置から親への方向を取得
                Vector3 dirFromParent = (bone.position - parentBone.position);

                if (dirFromParent.sqrMagnitude < 0.0001f)
                {
                    // ほぼ同じ位置の場合、親の後方を使用
                    dirFromParent = -parentBone.forward;
                }
                else
                {
                    dirFromParent.Normalize();
                }

                // 親から固定距離の位置に配置
                bone.position = parentBone.position + dirFromParent * segment.BoneLength;
            }

            // === Step 2: 回転の追従（親の回転に滑らかに追従） ===

            // 目標回転を計算
            // useAnimatorRotation: Animatorが設定した現在のローカル回転を使用（くねくね等が反映）
            // false: 初期ローカル回転を使用（Animatorを無視）
            Quaternion localRotation = useAnimatorRotation ? bone.localRotation : segment.InitialLocalRotation;
            Quaternion targetRotation = parentBone.rotation * localRotation;

            // 角度制限の適用
            if (maxBendAngle < 180f)
            {
                targetRotation = ApplyAngleConstraint(targetRotation, parent.CurrentRotation, localRotation);
            }

            // ダンピング計算（シンプルな指数減衰）
            float effectiveDamping = segment.Damping + additionalDamping;
            effectiveDamping = Mathf.Clamp01(effectiveDamping);

            // t = 1 - damping^(deltaTime * speed) の近似
            float t = 1f - Mathf.Pow(effectiveDamping, deltaTime * rotationSpeed);
            t = Mathf.Clamp01(t);

            // 現在の回転から目標回転へ補間
            Quaternion newRotation = Quaternion.Slerp(segment.CurrentRotation, targetRotation, t);

            bone.rotation = newRotation;
            segment.CurrentRotation = newRotation;
            segment.PreviousPosition = bone.position;
        }

        /// <summary>
        /// 曲がり角度の制限を適用
        /// </summary>
        private Quaternion ApplyAngleConstraint(Quaternion targetRotation, Quaternion parentRotation, Quaternion initialLocalRotation)
        {
            // 親からの相対回転を計算
            Quaternion relativeRotation = Quaternion.Inverse(parentRotation) * targetRotation;

            // 初期ローカル回転からの差分を取得
            Quaternion deltaFromInitial = Quaternion.Inverse(initialLocalRotation) * relativeRotation;

            // 角度を取得
            deltaFromInitial.ToAngleAxis(out float angle, out Vector3 axis);

            // 角度が180度を超える場合は反転（Quaternionの特性上）
            if (angle > 180f)
            {
                angle = 360f - angle;
                axis = -axis;
            }

            // 角度を制限
            if (angle > maxBendAngle)
            {
                Quaternion limitedDelta = Quaternion.AngleAxis(maxBendAngle, axis);
                return parentRotation * initialLocalRotation * limitedDelta;
            }

            return targetRotation;
        }

        #endregion

        #region Branch Update

        /// <summary>
        /// 分岐ボーンの更新
        /// </summary>
        private void UpdateBranches()
        {
            if (branches.Count == 0) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            foreach (BoneChainBranch branch in branches)
            {
                if (!branch.Enabled) continue;
                if (branch.ParentSegmentIndex < 0 || branch.ParentSegmentIndex >= segments.Count) continue;

                BoneChainSegment parentSegment = segments[branch.ParentSegmentIndex];
                if (parentSegment.Bone == null) continue;

                // 親セグメントの速度を計算
                Vector3 parentVelocity = (parentSegment.Bone.position - parentSegment.PreviousPosition) / deltaTime;

                branch.UpdateBranch(parentVelocity, deltaTime);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 分岐の有効/無効を切り替え（名前指定）
        /// </summary>
        public void SetBranchEnabled(string branchName, bool enabled)
        {
            BoneChainBranch branch = branches.Find(b => b.BranchName == branchName);
            if (branch != null)
            {
                branch.Enabled = enabled;
            }
        }

        /// <summary>
        /// 分岐の有効/無効を切り替え（インデックス指定）
        /// </summary>
        public void SetBranchEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < branches.Count)
            {
                branches[index].Enabled = enabled;
            }
        }

        /// <summary>
        /// 即座に全ボーンをリセット
        /// </summary>
        public void ResetToInitialPose()
        {
            foreach (BoneChainSegment segment in segments)
            {
                segment.ResetToInitialPose();
            }

            foreach (BoneChainBranch branch in branches)
            {
                branch.ResetToInitialPose();
            }
        }

        #endregion

        #region Debug Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || segments.Count == 0) return;

            // ボーンチェーンの描画
            Gizmos.color = debugBoneColor;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                BoneChainSegment current = segments[i];
                BoneChainSegment next = segments[i + 1];

                if (current.Bone != null && next.Bone != null)
                {
                    Gizmos.DrawLine(current.Bone.position, next.Bone.position);
                    Gizmos.DrawWireSphere(current.Bone.position, 0.02f);
                }
            }

            // 最後のボーン
            if (segments.Count > 0 && segments[segments.Count - 1].Bone != null)
            {
                Gizmos.DrawWireSphere(segments[segments.Count - 1].Bone.position, 0.02f);
            }

            // ボーン長制約の表示
            if (constrainBoneLength)
            {
                Gizmos.color = debugConstraintColor;
                for (int i = 1; i < segments.Count; i++)
                {
                    BoneChainSegment segment = segments[i];
                    BoneChainSegment parent = segments[i - 1];

                    if (parent.Bone != null && segment.BoneLength > 0f)
                    {
                        Gizmos.DrawWireSphere(parent.Bone.position, segment.BoneLength);
                    }
                }
            }

            // 分岐の描画
            Gizmos.color = Color.green;
            foreach (BoneChainBranch branch in branches)
            {
                if (branch.BranchBones == null) continue;

                for (int i = 0; i < branch.BranchBones.Length - 1; i++)
                {
                    if (branch.BranchBones[i] != null && branch.BranchBones[i + 1] != null)
                    {
                        Gizmos.DrawLine(branch.BranchBones[i].position, branch.BranchBones[i + 1].position);
                    }
                }
            }
        }
#endif

        #endregion
    }
}
