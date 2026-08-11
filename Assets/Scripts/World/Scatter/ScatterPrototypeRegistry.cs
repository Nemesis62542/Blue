using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blue.World.Scatter
{
    /// <summary>
    /// 散布物の1種類ぶんの定義。
    /// サンゴ・海藻・岩・落ちているアイテムのいずれもこの型で表す。
    /// </summary>
    [Serializable]
    public class ScatterPrototype
    {
        [Tooltip("ベイク結果から参照される安定ID。一度振ったら変更しないこと（永続IDが壊れる）")]
        public int id;

        public string displayName;

        [Tooltip("インスタンシング描画に使うメッシュ。LODごとに要素を並べる（要素0が最も高精細）")]
        public Mesh[] lodMeshes = Array.Empty<Mesh>();

        public Material material;

        [Tooltip("GameObject として実体化する散布物のプレハブ。アイテムなど拾える物に使う")]
        public GameObject prefab;

        [Tooltip("true なら GameObject を実体化する。false ならインスタンシング描画のみ")]
        public bool instantiate;

        [Tooltip("影を落とすか。海中では影のコストが割に合わないことが多い")]
        public bool castShadows;
    }

    /// <summary>
    /// 散布物プロトタイプの一覧。ベイク結果は prototypeId でここを引く。
    ///
    /// 【重要】id は永続IDの一部になっている（ScatterInstanceId 参照）。
    /// 既存の id を振り直すと、セーブデータ中の「回収済みアイテム」が別の物を指してしまう。
    /// </summary>
    [CreateAssetMenu(fileName = "ScatterPrototypeRegistry", menuName = "Blue/ScriptableObject/ScatterPrototypeRegistry")]
    public class ScatterPrototypeRegistry : ScriptableObject
    {
        [SerializeField] private ScatterPrototype[] prototypes = Array.Empty<ScatterPrototype>();

        private Dictionary<int, ScatterPrototype> cache;

        public ScatterPrototype[] Prototypes => prototypes;

        public ScatterPrototype Find(int id)
        {
            if (cache == null)
            {
                cache = new Dictionary<int, ScatterPrototype>(prototypes.Length);
                foreach (ScatterPrototype prototype in prototypes)
                {
                    if (prototype != null && !cache.ContainsKey(prototype.id))
                    {
                        cache.Add(prototype.id, prototype);
                    }
                }
            }

            return cache.TryGetValue(id, out ScatterPrototype found) ? found : null;
        }

        /// <summary>
        /// id の重複を検出する。重複すると永続IDが一意でなくなるため、ベイク前に必ず検証する。
        /// </summary>
        public bool ValidateIds(out string error)
        {
            HashSet<int> seen = new HashSet<int>();
            foreach (ScatterPrototype prototype in prototypes)
            {
                if (prototype == null)
                {
                    continue;
                }

                if (!seen.Add(prototype.id))
                {
                    error = $"ScatterPrototype の id {prototype.id} が重複しています。";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
