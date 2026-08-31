using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blue.Aquarium
{
    /// <summary>
    /// 水族館に設置できるもの全ての定義の基底
    /// </summary>
    public abstract class AquariumPieceData : ScriptableObject
    {
        [SerializeField] private new string name;         // 設置物の名前
        [SerializeField, TextArea] private string description; // 説明
        [SerializeField] private Sprite icon;             // 設置UIに出すアイコン
        [SerializeField] private GameObject prefab;       // シーンに生成する実体
        [SerializeField, HideInInspector] private string cachedGUID; // ビルド版用のキャッシュGUID

        public string Name => name;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;

        /// <summary>
        /// この設置物をグリッドに載せるか、自由配置するか
        /// </summary>
        public abstract PiecePlacement Placement { get; }

        /// <summary>
        /// 設置物の一意なID（GUIDベース）
        /// </summary>
        public string PieceID
        {
            get
            {
#if UNITY_EDITOR
                string asset_path = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(asset_path))
                {
                    string guid = AssetDatabase.AssetPathToGUID(asset_path);
                    // エディタではGUIDをキャッシュに保存（ビルド時に使用）
                    if (cachedGUID != guid)
                    {
                        cachedGUID = guid;
                        EditorUtility.SetDirty(this);
                    }
                    return guid;
                }
#else
                // ビルド版ではキャッシュされたGUIDを返す
                return cachedGUID;
#endif
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// 設置方式
    /// </summary>
    public enum PiecePlacement
    {
        Grid, // セルに吸着させる（水槽・通路・展示台）
        Free, // 位置と向きを自由に決める（装飾）
    }
}
