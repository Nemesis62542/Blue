using UnityEngine;

namespace Blue.Procedural.Coral
{
    /// <summary>
    /// 枝分かれするサンゴを生成
    /// 再帰的な分岐アルゴリズムを使用
    /// </summary>
    public class BranchingCoralGenerator : ProceduralCoralGenerator
    {
        #region Serialized Fields

        [Header("Branching Settings")]
        [Tooltip("最大分岐の深さ")]
        [SerializeField, Range(1, 6)] private int maxDepth = 4;

        [Tooltip("各分岐点での枝の数")]
        [SerializeField, Range(2, 5)] private int branchCount = 3;

        [Tooltip("子枝の長さの比率")]
        [SerializeField, Range(0.4f, 0.9f)] private float branchLengthRatio = 0.7f;

        [Tooltip("子枝の半径の比率")]
        [SerializeField, Range(0.4f, 0.9f)] private float branchRadiusRatio = 0.65f;

        [Tooltip("枝の広がり角度")]
        [SerializeField, Range(15f, 60f)] private float branchAngle = 35f;

        [Tooltip("角度のランダム変動")]
        [SerializeField, Range(0f, 30f)] private float branchAngleVariance = 10f;

        [Header("Branch Shape")]
        [Tooltip("枝1本あたりのセグメント数")]
        [SerializeField, Range(1, 8)] private int segmentsPerBranch = 2;

        [Tooltip("枝の曲がり具合")]
        [SerializeField, Range(0f, 0.5f)] private float curvature = 0.1f;

        [Header("Variation")]
        [Tooltip("枝の長さのランダム変動")]
        [SerializeField, Range(0f, 0.3f)] private float lengthVariance = 0.15f;

        [Tooltip("枝の太さのランダム変動")]
        [SerializeField, Range(0f, 0.3f)] private float radiusVariance = 0.1f;

        [Header("Upward Growth")]
        [Tooltip("先端が上方向に向く強さ（0=影響なし、1=完全に上向き）")]
        [SerializeField, Range(0f, 1f)] private float upwardBias = 0f;

        [Tooltip("上向きの影響が始まる深さ（0=最初から、1=先端のみ）")]
        [SerializeField, Range(0f, 1f)] private float upwardBiasStartDepth = 0f;

        [Header("Material Blend")]
        [Tooltip("シェーダーブレンド用の頂点カラーを設定（Blue/CoralBlendシェーダーと併用）")]
        [SerializeField] private bool useShaderBlend = true;

        [Tooltip("ブレンドのカーブ（0=根元のマテリアル、1=先端のマテリアル）")]
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        #endregion

        #region Properties

        public override string CoralTypeName => "Branching";

        #endregion

        #region Public Methods

        public override void Generate()
        {
            InitializeRandom();
            InitializeMeshData();

            // 根元から生成開始
            GenerateBranch(
                origin: Vector3.zero,
                direction: Vector3.up,
                length: baseHeight,
                radius: baseRadius,
                depth: 0
            );

            ApplyMeshData(recalculateNormals: true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 再帰的に枝を生成（閉じたBoxを組み合わせる方式）
        /// </summary>
        private void GenerateBranch(Vector3 origin, Vector3 direction, float length, float radius, int depth)
        {
            // 長さと半径にランダム変動を追加
            float actualLength = length * (1f + RandomRange(-lengthVariance, lengthVariance));
            float actualRadius = radius * (1f + RandomRange(-radiusVariance, radiusVariance));

            // 枝のセグメントを生成
            Vector3 currentPos = origin;
            Vector3 currentDir = direction.normalized;

            float segmentLength = actualLength / segmentsPerBranch;

            // 各セグメントをBoxとして追加
            for (int i = 0; i < segmentsPerBranch; i++)
            {
                Vector3 segmentStart = currentPos;

                // 曲がりを追加
                if (curvature > 0f && i > 0)
                {
                    Vector3 randomBend = GetRandomPerpendicular(currentDir) * curvature * RandomRange(-1f, 1f);
                    currentDir = (currentDir + randomBend).normalized;
                }

                // 上方向への傾きを適用
                if (upwardBias > 0f)
                {
                    float depthRatio = (float)depth / (maxDepth - 1);
                    if (depthRatio >= upwardBiasStartDepth)
                    {
                        float biasStrength = upwardBias * ((depthRatio - upwardBiasStartDepth) / (1f - upwardBiasStartDepth + 0.001f));
                        currentDir = Vector3.Lerp(currentDir, Vector3.up, biasStrength).normalized;
                    }
                }

                Vector3 segmentEnd = currentPos + currentDir * segmentLength;

                // 頂点カラーを計算（R値にブレンド係数を格納）
                Color segmentColor = Color.white;
                if (useShaderBlend)
                {
                    float depthRatio = (float)depth / Mathf.Max(1, maxDepth - 1);
                    float blendFactor = blendCurve.Evaluate(depthRatio);
                    // R値にブレンド係数を格納（シェーダーで使用）
                    segmentColor = new Color(blendFactor, blendFactor, blendFactor, 1f);
                }

                // 閉じたBoxを追加
                meshData.AddBox(segmentStart, segmentEnd, actualRadius, Vector3.up, segmentColor);

                currentPos = segmentEnd;
            }

            // 先端位置
            Vector3 tipPosition = currentPos;

            if (depth < maxDepth - 1)
            {
                // まだ分岐可能：子枝を生成
                GenerateChildBranches(tipPosition, currentDir, actualLength, actualRadius * branchRadiusRatio, depth);
            }
        }

        /// <summary>
        /// 子枝を生成
        /// </summary>
        private void GenerateChildBranches(Vector3 origin, Vector3 parentDirection, float parentLength, float parentRadius, int depth)
        {
            // 枝の数を決定（ランダム変動あり）
            int actualBranchCount = branchCount;
            if (RandomRange(0f, 1f) > 0.7f)
            {
                actualBranchCount += RandomRange(-1, 2);
                actualBranchCount = Mathf.Clamp(actualBranchCount, 2, 5);
            }

            // 基本の回転角度（均等配置）
            float baseAngleStep = 360f / actualBranchCount;
            float startAngle = RandomRange(0f, 360f); // ランダムな開始角度

            for (int i = 0; i < actualBranchCount; i++)
            {
                // 水平方向の角度
                float horizontalAngle = startAngle + baseAngleStep * i + RandomRange(-20f, 20f);

                // 垂直方向の広がり角度
                float verticalAngle = branchAngle + RandomRange(-branchAngleVariance, branchAngleVariance);

                // 子枝の方向を計算
                Vector3 childDirection = CalculateBranchDirection(parentDirection, horizontalAngle, verticalAngle);

                // 子枝のパラメータ
                float childLength = parentLength * branchLengthRatio;
                float childRadius = parentRadius * branchRadiusRatio;

                // 再帰呼び出し
                GenerateBranch(origin, childDirection, childLength, childRadius, depth + 1);
            }
        }

        /// <summary>
        /// 親の方向から子枝の方向を計算
        /// </summary>
        private Vector3 CalculateBranchDirection(Vector3 parentDirection, float horizontalAngle, float verticalAngle)
        {
            // 親の方向に垂直な平面上での回転軸を取得
            Vector3 perpendicular = GetRandomPerpendicular(parentDirection);

            // 垂直角度で傾ける
            Quaternion tiltRotation = Quaternion.AngleAxis(verticalAngle, perpendicular);
            Vector3 tiltedDirection = tiltRotation * parentDirection;

            // 親の方向周りで水平回転
            Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, parentDirection);
            Vector3 finalDirection = horizontalRotation * tiltedDirection;

            return finalDirection.normalized;
        }

        /// <summary>
        /// 指定方向に垂直なランダムなベクトルを取得
        /// </summary>
        private Vector3 GetRandomPerpendicular(Vector3 direction)
        {
            // 適当な軸との外積で垂直ベクトルを得る
            Vector3 arbitrary = Mathf.Abs(direction.y) < 0.9f ? Vector3.up : Vector3.right;
            Vector3 perpendicular = Vector3.Cross(direction, arbitrary).normalized;

            // ランダムに回転
            float angle = RandomRange(0f, 360f);
            return Quaternion.AngleAxis(angle, direction) * perpendicular;
        }

        #endregion

        #region Debug Gizmos

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (!showDebugGizmos) return;

            // 分岐パターンをワイヤーフレームで表示
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;

            // 簡易的な分岐プレビュー
            DrawBranchPreview(Vector3.zero, Vector3.up, baseHeight, 0);
        }

        private void DrawBranchPreview(Vector3 origin, Vector3 direction, float length, int depth)
        {
            if (depth >= maxDepth) return;

            Vector3 end = origin + direction * length;
            Gizmos.DrawLine(origin, end);

            if (depth < maxDepth - 1)
            {
                float childLength = length * branchLengthRatio;
                float angleStep = 360f / branchCount;

                for (int i = 0; i < branchCount; i++)
                {
                    float angle = angleStep * i;
                    Vector3 childDir = CalculateBranchDirectionForGizmo(direction, angle, branchAngle);
                    DrawBranchPreview(end, childDir, childLength, depth + 1);
                }
            }
        }

        private Vector3 CalculateBranchDirectionForGizmo(Vector3 parentDirection, float horizontalAngle, float verticalAngle)
        {
            Vector3 perpendicular = Mathf.Abs(parentDirection.y) < 0.9f
                ? Vector3.Cross(parentDirection, Vector3.up).normalized
                : Vector3.Cross(parentDirection, Vector3.right).normalized;

            Quaternion tiltRotation = Quaternion.AngleAxis(verticalAngle, perpendicular);
            Vector3 tiltedDirection = tiltRotation * parentDirection;

            Quaternion horizontalRotation = Quaternion.AngleAxis(horizontalAngle, parentDirection);
            return (horizontalRotation * tiltedDirection).normalized;
        }
#endif

        #endregion
    }
}
