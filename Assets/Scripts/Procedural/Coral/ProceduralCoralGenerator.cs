using UnityEngine;

namespace Blue.Procedural.Coral
{
    /// <summary>
    /// プロシージャルサンゴ生成の基底クラス
    /// エディタ上でMeshを生成し、シード値による再現が可能
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public abstract class ProceduralCoralGenerator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Seed Settings")]
        [Tooltip("乱数シード値（同じ値で同じ形状を再現）")]
        [SerializeField] protected int seed = 12345;

        [Header("Material")]
        [Tooltip("サンゴのマテリアル")]
        [SerializeField] protected Material coralMaterial;

        [Header("Base Shape")]
        [Tooltip("根元の半径")]
        [SerializeField, Range(0.01f, 1f)] protected float baseRadius = 0.1f;

        [Tooltip("基本の高さ")]
        [SerializeField, Range(0.1f, 5f)] protected float baseHeight = 1f;

        [Tooltip("円周方向の分割数")]
        [SerializeField, Range(4, 24)] protected int radialSegments = 8;

        [Header("Debug")]
        [Tooltip("デバッグ用Gizmoを表示")]
        [SerializeField] protected bool showDebugGizmos = false;

        #endregion

        #region Protected Fields

        protected Mesh generatedMesh;
        protected MeshFilter meshFilter;
        protected MeshRenderer meshRenderer;
        protected System.Random random;
        protected CoralMeshData meshData;

        #endregion

        #region Properties

        /// <summary>
        /// シード値
        /// </summary>
        public int Seed
        {
            get => seed;
            set => seed = value;
        }

        /// <summary>
        /// 生成されたMesh
        /// </summary>
        public Mesh GeneratedMesh => generatedMesh;

        /// <summary>
        /// Meshが生成済みかどうか
        /// </summary>
        public bool HasMesh => generatedMesh != null && generatedMesh.vertexCount > 0;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Meshを生成（派生クラスで実装）
        /// </summary>
        public abstract void Generate();

        /// <summary>
        /// サンゴタイプの名前を取得
        /// </summary>
        public abstract string CoralTypeName { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// 生成をクリア
        /// </summary>
        public virtual void Clear()
        {
            EnsureComponents();

            if (generatedMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(generatedMesh);
                }
                else
                {
                    DestroyImmediate(generatedMesh);
                }
                generatedMesh = null;
            }

            meshFilter.sharedMesh = null;
        }

        /// <summary>
        /// シード値をランダム化して再生成
        /// </summary>
        public void RandomizeSeed()
        {
            seed = UnityEngine.Random.Range(0, int.MaxValue);
            Generate();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// シード値を使って乱数を初期化
        /// </summary>
        protected void InitializeRandom()
        {
            random = new System.Random(seed);
        }

        /// <summary>
        /// MeshFilterとMeshRendererを確保
        /// </summary>
        protected void EnsureComponents()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
            }

            // マテリアルを適用
            if (coralMaterial != null && meshRenderer.sharedMaterial != coralMaterial)
            {
                meshRenderer.sharedMaterial = coralMaterial;
            }
        }

        /// <summary>
        /// CoralMeshDataを初期化
        /// </summary>
        protected void InitializeMeshData()
        {
            if (meshData == null)
            {
                meshData = new CoralMeshData();
            }
            else
            {
                meshData.Clear();
            }
        }

        /// <summary>
        /// MeshDataからMeshを作成して適用
        /// </summary>
        protected void ApplyMeshData(bool recalculateNormals = true)
        {
            EnsureComponents();

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh();
                generatedMesh.name = $"ProceduralCoral_{CoralTypeName}_{seed}";
            }

            meshData.ApplyToMesh(generatedMesh, recalculateNormals);
            meshFilter.sharedMesh = generatedMesh;
        }

        /// <summary>
        /// 指定範囲の乱数を取得
        /// </summary>
        protected float RandomRange(float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }

        /// <summary>
        /// 指定範囲の整数乱数を取得
        /// </summary>
        protected int RandomRange(int min, int max)
        {
            return random.Next(min, max);
        }

        /// <summary>
        /// 球内のランダムな点を取得
        /// </summary>
        protected Vector3 RandomInsideSphere(float radius)
        {
            float u = RandomRange(0f, 1f);
            float v = RandomRange(0f, 1f);
            float theta = u * 2f * Mathf.PI;
            float phi = Mathf.Acos(2f * v - 1f);
            float r = Mathf.Pow(RandomRange(0f, 1f), 1f / 3f) * radius;

            float sinPhi = Mathf.Sin(phi);
            return new Vector3(
                r * sinPhi * Mathf.Cos(theta),
                r * sinPhi * Mathf.Sin(theta),
                r * Mathf.Cos(phi)
            );
        }

        /// <summary>
        /// 単位球面上のランダムな点を取得
        /// </summary>
        protected Vector3 RandomOnUnitSphere()
        {
            float u = RandomRange(0f, 1f);
            float v = RandomRange(0f, 1f);
            float theta = u * 2f * Mathf.PI;
            float phi = Mathf.Acos(2f * v - 1f);

            float sinPhi = Mathf.Sin(phi);
            return new Vector3(
                sinPhi * Mathf.Cos(theta),
                sinPhi * Mathf.Sin(theta),
                Mathf.Cos(phi)
            );
        }

        #endregion

        #region Debug Gizmos

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            // 基本の形状範囲を表示
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.up * baseHeight * 0.5f, baseHeight * 0.5f);

            // 根元を表示
            Gizmos.color = Color.cyan;
            DrawGizmoCircle(Vector3.zero, baseRadius, 16);
        }

        protected void DrawGizmoCircle(Vector3 center, float radius, int segments)
        {
            Vector3 prevPoint = center + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
#endif

        #endregion
    }
}
