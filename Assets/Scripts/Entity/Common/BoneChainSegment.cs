using UnityEngine;
using System;

namespace Blue.Entity.Common
{
    /// <summary>
    /// 個々のボーンセグメントのデータと計算を担当
    /// ProceduralBoneChainから使用される
    /// </summary>
    [Serializable]
    public class BoneChainSegment
    {
        [Header("Bone Reference")]
        [SerializeField] private Transform bone;

        [Header("Settings")]
        [Tooltip("追従の遅れ（0=即座に追従、1=ほぼ追従しない）")]
        [SerializeField, Range(0f, 0.99f)] private float damping = 0.1f;

        // ランタイム用キャッシュ（非シリアライズ）
        private float cachedBoneLength;
        private Quaternion initialLocalRotation;
        private Vector3 previousPosition;
        private Quaternion currentRotation;
        private bool isInitialized;

        // プロパティ
        public Transform Bone => bone;
        public float Damping => damping;
        public float BoneLength => cachedBoneLength;
        public Quaternion InitialLocalRotation => initialLocalRotation;
        public Vector3 PreviousPosition { get => previousPosition; set => previousPosition = value; }
        public Quaternion CurrentRotation { get => currentRotation; set => currentRotation = value; }
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 初期化：ボーン長と初期回転をキャッシュ
        /// </summary>
        /// <param name="nextBone">次のボーン（末端の場合はnull）</param>
        public void Initialize(Transform nextBone)
        {
            if (bone == null)
            {
                Debug.LogWarning("[BoneChainSegment] Bone reference is null.");
                return;
            }

            initialLocalRotation = bone.localRotation;
            previousPosition = bone.position;
            currentRotation = bone.rotation;

            // ボーン長を計算（次のボーンがある場合）
            if (nextBone != null)
            {
                cachedBoneLength = Vector3.Distance(bone.position, nextBone.position);
            }
            else
            {
                cachedBoneLength = 0f;
            }

            isInitialized = true;
        }

        /// <summary>
        /// ボーンをリセット（初期姿勢に戻す）
        /// </summary>
        public void ResetToInitialPose()
        {
            if (bone != null && isInitialized)
            {
                bone.localRotation = initialLocalRotation;
                currentRotation = bone.rotation;
            }
        }
    }
}
