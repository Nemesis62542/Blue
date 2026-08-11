using System;
using UnityEngine;

namespace Blue.World.Scatter
{
    /// <summary>
    /// 散布物1個体の配置情報。
    ///
    /// Matrix4x4(64byte) ではなく position/rotation/scale(32byte) で持つ。
    /// タイルあたり数万個体になるため、常駐サイズが倍以上変わる。
    /// スケールは一様スケールのみ。非一様スケールが必要な散布物は今のところ想定しない。
    /// （将来さらに削るなら、回転をsmallest-three圧縮、位置をタイルローカルの16bit量子化にできる）
    /// </summary>
    [Serializable]
    public struct ScatterInstance
    {
        public Vector3 position;
        public Quaternion rotation;
        public float scale;

        public Matrix4x4 ToMatrix() => Matrix4x4.TRS(position, rotation, Vector3.one * scale);
    }

    /// <summary>
    /// 散布物1個体を永続的に指すID。
    ///
    /// 散布ベイクはシード固定で決定的なので、再ベイクしない限りこのIDは不変。
    /// アイテムの「回収済み」判定はこのIDの集合をセーブデータに持つだけで済み、
    /// 個体ごとの状態を保存する必要がない。
    /// </summary>
    [Serializable]
    public struct ScatterInstanceId : IEquatable<ScatterInstanceId>
    {
        public int tileIndex;
        public int prototypeId;
        public int instanceIndex;

        public ScatterInstanceId(int tileIndex, int prototypeId, int instanceIndex)
        {
            this.tileIndex = tileIndex;
            this.prototypeId = prototypeId;
            this.instanceIndex = instanceIndex;
        }

        public bool Equals(ScatterInstanceId other) =>
            tileIndex == other.tileIndex &&
            prototypeId == other.prototypeId &&
            instanceIndex == other.instanceIndex;

        public override bool Equals(object obj) => obj is ScatterInstanceId other && Equals(other);

        public override int GetHashCode() =>
            (tileIndex * 397 ^ prototypeId) * 397 ^ instanceIndex;

        public override string ToString() => $"{tileIndex}:{prototypeId}:{instanceIndex}";
    }

    /// <summary>
    /// 同一プロトタイプの散布物をまとめた群。
    /// 描画は群単位で DrawMeshInstanced に流す。
    /// </summary>
    [Serializable]
    public class ScatterGroup
    {
        [Tooltip("ScatterPrototypeRegistry 上のプロトタイプID")]
        public int prototypeId;

        [Tooltip("GameObject として実体化するか。false ならインスタンシング描画のみ")]
        public bool instantiate;

        public ScatterInstance[] instances = Array.Empty<ScatterInstance>();
    }

    /// <summary>
    /// タイル1枚分の散布データ。散布ベイクの出力物。
    ///
    /// 【所有権のルール】
    /// インスタンスは基点(position)が属するタイルが所有する。描画も所有タイルが丸ごと行うため、
    /// 隣タイルが未ロードでも「大きな岩が半分だけ消える」ことは起きない
    /// （オブジェクト単位で出るか出ないかになる）。
    /// タイルサイズを超える巨大物のみ overlappingTiles に隣タイルを登録して例外扱いする。
    ///
    /// サンゴ・海藻・岩・落ちているアイテムはすべてこの形式に乗る。
    /// 種類が増えてもベイカー側・ランタイム側の変更は不要。
    /// </summary>
    public class ScatterChunk : ScriptableObject
    {
        [SerializeField] private int tileIndex;
        [SerializeField] private ScatterGroup[] groups = Array.Empty<ScatterGroup>();
        [SerializeField] private Bounds bounds;

        [Tooltip("このタイルの散布物がはみ出している隣接タイルのインデックス（巨大物のみ）")]
        [SerializeField] private int[] overlappingTiles = Array.Empty<int>();

        public int TileIndex => tileIndex;
        public ScatterGroup[] Groups => groups;
        public Bounds Bounds => bounds;
        public int[] OverlappingTiles => overlappingTiles;

        /// <summary>ベイカーから内容を設定する。ランタイムからは呼ばない。</summary>
        public void SetContents(int tileIndex, ScatterGroup[] groups, Bounds bounds, int[] overlappingTiles)
        {
            this.tileIndex = tileIndex;
            this.groups = groups ?? Array.Empty<ScatterGroup>();
            this.bounds = bounds;
            this.overlappingTiles = overlappingTiles ?? Array.Empty<int>();
        }
    }
}
