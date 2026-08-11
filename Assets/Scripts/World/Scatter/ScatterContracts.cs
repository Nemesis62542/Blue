using System;
using UnityEngine;

namespace Blue.World.Scatter
{
    /// <summary>
    /// 散布物1個体の配置情報。
    /// </summary>
    // Matrix4x4(64byte) ではなく position/rotation/scale(32byte) で持つ。
    // タイルあたり数万個体になるため、常駐サイズが倍以上変わる。
    // スケールは一様スケールのみ。非一様スケールが必要な散布物は今のところ想定しない。
    // 将来さらに削るなら、回転を smallest-three 圧縮、位置をタイルローカルの16bit量子化にできる。
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
    /// </summary>
    // 散布ベイクはシード固定で決定的なので、再ベイクしない限りこのIDは不変。
    // アイテムの「回収済み」判定はこのIDの集合をセーブデータに持つだけで済み、
    // 個体ごとの状態を保存する必要がない。
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
    /// </summary>
    // 描画は群単位で DrawMeshInstanced に流す。
    [Serializable]
    public class ScatterGroup
    {
        [Tooltip("ScatterPrototypeRegistry 上のプロトタイプID")]
        public int prototypeId;

        [Tooltip("GameObject として実体化するか。false ならインスタンシング描画のみ")]
        public bool instantiate;

        public ScatterInstance[] instances = Array.Empty<ScatterInstance>();
    }

}
