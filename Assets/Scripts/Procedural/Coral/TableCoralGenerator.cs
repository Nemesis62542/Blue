using UnityEngine;

namespace Blue.Procedural.Coral
{
    /// <summary>
    /// テーブル状のサンゴを生成
    /// 中央の幹 + 放射状に枝分かれする平板状の枝
    /// </summary>
    public class TableCoralGenerator : ProceduralCoralGenerator
    {
        #region Serialized Fields

        [Header("Trunk Settings")]
        [Tooltip("幹の高さ")]
        [SerializeField, Range(0.1f, 2f)] private float trunkHeight = 0.5f;

        [Header("Branching Settings")]
        [Tooltip("放射状の主枝の数")]
        [SerializeField, Range(3, 12)] private int mainBranches = 6;

        [Tooltip("枝分かれの深さ")]
        [SerializeField, Range(1, 4)] private int branchDepth = 3;

        [Tooltip("各深さでの分岐数")]
        [SerializeField, Range(2, 4)] private int subBranchCount = 2;

        [Tooltip("枝の長さ")]
        [SerializeField, Range(0.1f, 1f)] private float branchLength = 0.4f;

        [Tooltip("深さごとの長さ減衰")]
        [SerializeField, Range(0.5f, 1f)] private float lengthDecay = 0.8f;

        [Header("Flat Box Settings")]
        [Tooltip("枝の幅（横方向）")]
        [SerializeField, Range(0.02f, 0.3f)] private float branchWidth = 0.08f;

        [Tooltip("枝の厚さ（上下方向）")]
        [SerializeField, Range(0.01f, 0.1f)] private float branchThickness = 0.02f;

        [Tooltip("深さごとの幅減衰")]
        [SerializeField, Range(0.5f, 1f)] private float widthDecay = 0.85f;

        [Header("Shape Control")]
        [Tooltip("水平方向への広がり角度")]
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 15f;

        [Tooltip("分岐角度")]
        [SerializeField, Range(10f, 60f)] private float forkAngle = 30f;

        [Tooltip("角度のランダム変動")]
        [SerializeField, Range(0f, 20f)] private float angleVariance = 10f;

        [Tooltip("長さのランダム変動")]
        [SerializeField, Range(0f, 0.3f)] private float lengthVariance = 0.15f;

        [Header("Material Blend")]
        [Tooltip("シェーダーブレンド用の頂点カラーを設定")]
        [SerializeField] private bool useShaderBlend = true;

        [Tooltip("ブレンドのカーブ（0=幹、1=先端）")]
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        #endregion

        #region Properties

        public override string CoralTypeName => "Table";

        #endregion

        #region Public Methods

        public override void Generate()
        {
            InitializeRandom();
            InitializeMeshData();

            // 幹を生成
            GenerateTrunk();

            // テーブル部分（放射状に枝分かれ）を生成
            GenerateTableBranches();

            ApplyMeshData(recalculateNormals: true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 中央の幹を生成
        /// </summary>
        private void GenerateTrunk()
        {
            Vector3 start = Vector3.zero;
            Vector3 end = Vector3.up * trunkHeight;

            // 幹はブレンド値0（根本）
            float blendFactor = useShaderBlend ? blendCurve.Evaluate(0f) : 0f;
            Color color = new Color(blendFactor, blendFactor, blendFactor, 1f);

            meshData.AddBox(start, end, baseRadius, Vector3.up, color);
        }

        /// <summary>
        /// テーブル状の枝分かれ構造を生成
        /// </summary>
        private void GenerateTableBranches()
        {
            Vector3 branchOrigin = Vector3.up * trunkHeight;
            float angleStep = 360f / mainBranches;
            float startAngle = RandomRange(0f, angleStep);

            for (int i = 0; i < mainBranches; i++)
            {
                float angle = startAngle + angleStep * i + RandomRange(-angleVariance * 0.5f, angleVariance * 0.5f);
                float angleRad = angle * Mathf.Deg2Rad;

                // 水平方向 + 少し上向き
                Vector3 direction = new Vector3(
                    Mathf.Cos(angleRad),
                    Mathf.Tan(spreadAngle * Mathf.Deg2Rad),
                    Mathf.Sin(angleRad)
                ).normalized;

                GenerateBranch(branchOrigin, direction, 0, branchWidth, branchLength);
            }
        }

        /// <summary>
        /// 再帰的に枝を生成
        /// </summary>
        private void GenerateBranch(Vector3 origin, Vector3 direction, int depth, float currentWidth, float currentLength)
        {
            if (depth >= branchDepth) return;

            // 長さにランダム変動を加える
            float actualLength = currentLength * (1f + RandomRange(-lengthVariance, lengthVariance));

            Vector3 end = origin + direction * actualLength;

            // ブレンド係数を深さに基づいて計算
            float depthRatio = (float)(depth + 1) / branchDepth;
            float blendFactor = useShaderBlend ? blendCurve.Evaluate(depthRatio) : 0f;
            Color color = new Color(blendFactor, blendFactor, blendFactor, 1f);

            // 平たいボックスを追加
            meshData.AddFlatBox(origin, end, currentWidth, branchThickness, Vector3.up, color);

            // 次の深さのパラメータ
            float nextWidth = currentWidth * widthDecay;
            float nextLength = currentLength * lengthDecay;

            // 最終深さでなければ枝分かれ
            if (depth < branchDepth - 1)
            {
                // 分岐方向を計算
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                if (right.sqrMagnitude < 0.001f)
                {
                    right = Vector3.Cross(Vector3.forward, direction).normalized;
                }

                for (int i = 0; i < subBranchCount; i++)
                {
                    // 分岐角度を計算（左右に広がる）
                    float spreadT = (subBranchCount == 1) ? 0f : (float)i / (subBranchCount - 1) - 0.5f;
                    float horizontalAngle = spreadT * forkAngle * 2f + RandomRange(-angleVariance, angleVariance);
                    float verticalAngle = RandomRange(-angleVariance * 0.5f, angleVariance * 0.5f);

                    // 新しい方向を計算
                    Quaternion horizontalRot = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
                    Quaternion verticalRot = Quaternion.AngleAxis(verticalAngle, right);
                    Vector3 newDirection = (verticalRot * horizontalRot * direction).normalized;

                    GenerateBranch(end, newDirection, depth + 1, nextWidth, nextLength);
                }
            }
        }

        #endregion

        #region Debug Gizmos

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (!showDebugGizmos) return;

            Gizmos.color = new Color(0f, 0.8f, 0.8f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;

            // 幹を表示
            Gizmos.DrawLine(Vector3.zero, Vector3.up * trunkHeight);

            // 放射状の主枝を表示
            Vector3 branchOrigin = Vector3.up * trunkHeight;
            float angleStep = 360f / mainBranches;

            for (int i = 0; i < mainBranches; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(
                    Mathf.Cos(angle),
                    Mathf.Tan(spreadAngle * Mathf.Deg2Rad),
                    Mathf.Sin(angle)
                ).normalized;

                DrawGizmoBranch(branchOrigin, direction, 0, branchLength);
            }
        }

        private void DrawGizmoBranch(Vector3 origin, Vector3 direction, int depth, float length)
        {
            if (depth >= branchDepth) return;

            Vector3 end = origin + direction * length;

            float alpha = 1f - (float)depth / branchDepth * 0.5f;
            Gizmos.color = new Color(0f, 0.8f, 0.8f, alpha);
            Gizmos.DrawLine(origin, end);

            float nextLength = length * lengthDecay;

            if (depth < branchDepth - 1)
            {
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

                for (int i = 0; i < subBranchCount; i++)
                {
                    float spreadT = (subBranchCount == 1) ? 0f : (float)i / (subBranchCount - 1) - 0.5f;
                    float horizontalAngle = spreadT * forkAngle * 2f;

                    Quaternion rot = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
                    Vector3 newDirection = (rot * direction).normalized;

                    DrawGizmoBranch(end, newDirection, depth + 1, nextLength);
                }
            }
        }
#endif

        #endregion
    }
}
