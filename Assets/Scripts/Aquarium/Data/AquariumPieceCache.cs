using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Blue.Aquarium
{
    /// <summary>
    /// ランタイムでAquariumPieceDataのGUID検索を高速化するキャッシュシステム
    /// </summary>
    public static class AquariumPieceCache
    {
        private static Dictionary<string, AquariumPieceData> guidToPieceCache = new Dictionary<string, AquariumPieceData>();
        private static Dictionary<AquariumPieceData, string> pieceToGuidCache = new Dictionary<AquariumPieceData, string>();
        private static AquariumPieceRegistry registryOverride;
        private static bool isInitialized = false;

        public static void SetRegistry(AquariumPieceRegistry registry)
        {
            registryOverride = registry;
            RebuildCache();
        }

        /// <summary>
        /// キャッシュを初期化（ゲーム起動時に一度だけ呼び出す）
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized) return;

#if UNITY_EDITOR
            // エディタではAssetDatabaseから全設置物を取得
            string[] guids = AssetDatabase.FindAssets("t:AquariumPieceData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AquariumPieceData piece = AssetDatabase.LoadAssetAtPath<AquariumPieceData>(path);
                if (piece != null)
                {
                    RegisterPiece(piece, guid);
                }
            }
#else
            // ビルド版ではレジストリから読み込み
            AquariumPieceRegistry registry = registryOverride != null ? registryOverride : AquariumPieceRegistry.Instance;
            if (registry != null)
            {
                foreach (AquariumPieceData piece in registry.Pieces)
                {
                    if (piece != null)
                    {
                        RegisterPiece(piece, piece.PieceID);
                    }
                }
            }
#endif

            isInitialized = true;
            Debug.Log($"AquariumPieceCache initialized with {guidToPieceCache.Count} pieces");
        }

        private static void RegisterPiece(AquariumPieceData piece, string guid)
        {
            if (piece == null || string.IsNullOrEmpty(guid)) return;

            guidToPieceCache[guid] = piece;
            pieceToGuidCache[piece] = guid;
        }

        /// <summary>
        /// GUIDからAquariumPieceDataを取得
        /// </summary>
        public static AquariumPieceData GetPieceByGUID(string guid)
        {
            if (!isInitialized) Initialize();

            if (string.IsNullOrEmpty(guid)) return null;

            return guidToPieceCache.TryGetValue(guid, out AquariumPieceData piece) ? piece : null;
        }

        /// <summary>
        /// GUIDから指定した型の設置物を取得
        /// </summary>
        public static T GetPieceByGUID<T>(string guid) where T : AquariumPieceData
        {
            return GetPieceByGUID(guid) as T;
        }

        /// <summary>
        /// AquariumPieceDataからGUIDを取得
        /// </summary>
        public static string GetGUID(AquariumPieceData piece)
        {
            if (!isInitialized) Initialize();

            if (piece == null) return string.Empty;

            return pieceToGuidCache.TryGetValue(piece, out string guid) ? guid : string.Empty;
        }

        /// <summary>
        /// キャッシュをクリア（テストやデバッグ用）
        /// </summary>
        public static void ClearCache()
        {
            guidToPieceCache.Clear();
            pieceToGuidCache.Clear();
            isInitialized = false;
        }

        /// <summary>
        /// キャッシュを再構築
        /// </summary>
        public static void RebuildCache()
        {
            ClearCache();
            Initialize();
        }

        /// <summary>
        /// 登録されている全ての設置物を取得
        /// </summary>
        public static IEnumerable<AquariumPieceData> GetAllPieces()
        {
            if (!isInitialized) Initialize();
            return guidToPieceCache.Values;
        }
    }
}
