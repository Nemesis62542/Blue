using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blue.World.Scatter
{
    /// <summary>
    /// ロード済みタイルの散布物をインスタンシングで描画する。
    /// </summary>
    // 散布物は数万個体になるため GameObject にはしない。ScatterChunk の配列を
    // そのまま RenderMeshInstanced に流す。GameObject が要るもの（拾えるアイテム等）
    // だけを instantiate フラグで実体化する。
    //
    // タイルシーン側の StageTileScatter が OnEnable/OnDisable で自己登録するので、
    // このコンポーネントはタイルのロード状態を知る必要がない。
    // ExecuteAlways にしているのは、散布ルールの調整をエディタ上で確認するため。
    // Play に入らないと見えないと、間隔や傾斜の調整が全く回らない。
    // なお Scene ビューが再描画されないと更新されないので、シーンビューの
    // 「Always Refresh」を有効にするか、ビューを操作すること。
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ScatterRenderer : MonoBehaviour
    {
        #region Static Registry

        private static readonly List<StageTileScatter> pendingTiles = new List<StageTileScatter>();
        private static ScatterRenderer instance;

        /// <summary>タイルシーンの散布データを登録する。</summary>
        public static void Register(StageTileScatter tile)
        {
            if (tile == null || tile.Chunk == null)
            {
                return;
            }

            if (instance != null)
            {
                instance.Add(tile);
                return;
            }

            // レンダラーより先にタイルが載ることがあるので保留しておく
            pendingTiles.Add(tile);
        }

        /// <summary>タイルシーンの散布データを登録解除する。</summary>
        public static void Unregister(StageTileScatter tile)
        {
            pendingTiles.Remove(tile);

            if (instance != null)
            {
                instance.Remove(tile);
            }
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private ScatterPrototypeRegistry registry;

        [Tooltip("この Transform からの距離で描画するタイルを絞る。未設定なら MainCamera を探す")]
        [SerializeField] private Transform viewer;

        [Tooltip("この距離以内のタイルだけ描画する(m)。水中はフォグで視界が短いので絞れる")]
        [SerializeField] private float renderDistance = 200f;

        [Tooltip("描画距離にこれを足した距離を超えたら行列キャッシュを解放する(m)")]
        [SerializeField] private float cacheHysteresis = 64f;

        [Tooltip("実体化する散布物の親。未設定ならこの GameObject の下に作る")]
        [SerializeField] private Transform instantiateRoot;

        [Tooltip("インスタンシング描画を行う。切ると実体化するものだけが残る")]
        [SerializeField] private bool renderInstanced = true;

        #endregion

        #region Fields

        // RenderMeshInstanced は1回の呼び出しで最大1023インスタンスまで
        private const int BATCH_SIZE = 1023;

        private readonly List<TileEntry> tiles = new List<TileEntry>();

        private sealed class TileEntry
        {
            public StageTileScatter source;
            public List<GameObject> spawned;

            // グループごとの行列。毎フレーム TRS を組み直すと 96万インスタンスで 23ms かかる。
            // 描画距離に入ったタイルだけ作り、離れたら解放して常駐量を抑える
            public Matrix4x4[][] matrices;
        }

        #endregion

        #region Diagnostics

        /// <summary>直近フレームで描画したタイル数</summary>
        public int DrawnTileCount { get; private set; }

        /// <summary>直近フレームで描画したインスタンス数</summary>
        public int DrawnInstanceCount { get; private set; }

        /// <summary>行列キャッシュを保持しているタイル数</summary>
        public int CachedTileCount { get; private set; }

        #endregion

        #region Unity

        private void OnEnable()
        {
            instance = this;

            // レンダラー生成前に載っていたタイルを取り込む
            for (int i = pendingTiles.Count - 1; i >= 0; i--)
            {
                Add(pendingTiles[i]);
            }

            pendingTiles.Clear();
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }

            for (int i = tiles.Count - 1; i >= 0; i--)
            {
                DestroySpawned(tiles[i]);
            }

            tiles.Clear();
        }

        private void LateUpdate()
        {
            DrawnTileCount = 0;
            DrawnInstanceCount = 0;
            CachedTileCount = 0;

            if (!renderInstanced || registry == null)
            {
                return;
            }

            Vector3 viewerPosition = ResolveViewerPosition();
            float renderSqr = renderDistance * renderDistance;
            float releaseDistance = renderDistance + cacheHysteresis;
            float releaseSqr = releaseDistance * releaseDistance;

            foreach (TileEntry tile in tiles)
            {
                ScatterChunk chunk = tile.source != null ? tile.source.Chunk : null;
                if (chunk == null)
                {
                    continue;
                }

                float sqrDistance = chunk.Bounds.SqrDistance(viewerPosition);

                if (sqrDistance <= renderSqr)
                {
                    EnsureCache(tile, chunk);
                    DrawTile(tile, chunk);
                    DrawnTileCount++;
                }
                else if (sqrDistance > releaseSqr)
                {
                    ReleaseCache(tile);
                }

                if (tile.matrices != null)
                {
                    CachedTileCount++;
                }
            }
        }

        private Vector3 ResolveViewerPosition()
        {
            if (viewer != null)
            {
                return viewer.position;
            }

            Camera main = Camera.main;
            return main != null ? main.transform.position : transform.position;
        }

        #endregion

        #region Registration

        private void Add(StageTileScatter source)
        {
            if (source == null || source.Chunk == null || Find(source) != null)
            {
                return;
            }

            TileEntry entry = new TileEntry { source = source };
            tiles.Add(entry);

            SpawnInstantiated(entry);
        }

        private void Remove(StageTileScatter source)
        {
            TileEntry entry = Find(source);
            if (entry == null)
            {
                return;
            }

            DestroySpawned(entry);
            tiles.Remove(entry);
        }

        private TileEntry Find(StageTileScatter source)
        {
            foreach (TileEntry entry in tiles)
            {
                if (entry.source == source)
                {
                    return entry;
                }
            }

            return null;
        }

        #endregion

        #region Draw

        /// <summary>
        /// タイルの行列キャッシュを用意する。
        /// </summary>
        // 描画距離に入った時点で1度だけ構築する。全タイル分を常時持つと
        // 96万インスタンス x 64byte で 60MB を超えるため、必要なぶんだけ持つ。
        private void EnsureCache(TileEntry tile, ScatterChunk chunk)
        {
            if (tile.matrices != null)
            {
                return;
            }

            ScatterGroup[] groups = chunk.Groups;
            tile.matrices = new Matrix4x4[groups.Length][];

            for (int g = 0; g < groups.Length; g++)
            {
                ScatterGroup group = groups[g];
                if (group.instantiate || group.instances == null || group.instances.Length == 0)
                {
                    continue;
                }

                Matrix4x4[] matrices = new Matrix4x4[group.instances.Length];
                for (int i = 0; i < matrices.Length; i++)
                {
                    matrices[i] = group.instances[i].ToMatrix();
                }

                tile.matrices[g] = matrices;
            }
        }

        private static void ReleaseCache(TileEntry tile)
        {
            tile.matrices = null;
        }

        private void DrawTile(TileEntry tile, ScatterChunk chunk)
        {
            ScatterGroup[] groups = chunk.Groups;

            for (int g = 0; g < groups.Length; g++)
            {
                Matrix4x4[] matrices = tile.matrices[g];
                if (matrices == null || matrices.Length == 0)
                {
                    continue;
                }

                ScatterPrototype prototype = registry.Find(groups[g].prototypeId);
                if (prototype == null || prototype.material == null ||
                    prototype.lodMeshes == null || prototype.lodMeshes.Length == 0)
                {
                    continue;
                }

                Mesh mesh = prototype.lodMeshes[0];
                if (mesh == null)
                {
                    continue;
                }

                RenderParams parameters = new RenderParams(prototype.material)
                {
                    worldBounds = chunk.Bounds,
                    shadowCastingMode = prototype.castShadows
                        ? ShadowCastingMode.On
                        : ShadowCastingMode.Off,
                    receiveShadows = prototype.castShadows,
                };

                // キャッシュ済み配列をそのまま渡す。startInstance があるので
                // バッチごとに別配列へコピーする必要はない
                for (int start = 0; start < matrices.Length; start += BATCH_SIZE)
                {
                    int count = Mathf.Min(BATCH_SIZE, matrices.Length - start);

                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        Graphics.RenderMeshInstanced(parameters, mesh, submesh, matrices, count, start);
                    }
                }

                DrawnInstanceCount += matrices.Length;
            }
        }

        #endregion

        #region Instantiated Objects

        private void SpawnInstantiated(TileEntry tile)
        {
            // 編集中に GameObject を生やすとシーンが汚れるので、実体化は再生中のみ
            if (!Application.isPlaying)
            {
                return;
            }

            ScatterChunk chunk = tile.source.Chunk;
            Transform parent = instantiateRoot != null ? instantiateRoot : transform;

            foreach (ScatterGroup group in chunk.Groups)
            {
                if (!group.instantiate || group.instances == null)
                {
                    continue;
                }

                ScatterPrototype prototype = registry != null ? registry.Find(group.prototypeId) : null;
                if (prototype == null || prototype.prefab == null)
                {
                    continue;
                }

                tile.spawned ??= new List<GameObject>();

                for (int i = 0; i < group.instances.Length; i++)
                {
                    // TODO: 回収済み判定。ScatterInstanceId(chunk.TileIndex, prototypeId, i) を
                    // セーブデータの集合と照合して、拾い済みならスキップする
                    ScatterInstance data = group.instances[i];

                    GameObject spawned = Instantiate(prototype.prefab, data.position, data.rotation, parent);
                    spawned.transform.localScale = Vector3.one * data.scale;
                    tile.spawned.Add(spawned);
                }
            }
        }

        private void DestroySpawned(TileEntry tile)
        {
            if (tile.spawned == null)
            {
                return;
            }

            foreach (GameObject spawned in tile.spawned)
            {
                if (spawned == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(spawned);
                }
                else
                {
                    DestroyImmediate(spawned);
                }
            }

            tile.spawned.Clear();
        }

        #endregion
    }
}
