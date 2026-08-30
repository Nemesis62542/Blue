using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 全てのAquariumPieceDataへの参照を保持するレジストリ
    /// ビルド版でセーブデータのGUIDから設置物を復元するために使用
    /// </summary>
    [CreateAssetMenu(fileName = "AquariumPieceRegistry", menuName = "Blue/ScriptableObject/Aquarium/PieceRegistry")]
    public class AquariumPieceRegistry : ScriptableObject
    {
        private static AquariumPieceRegistry instance;

        [SerializeField] private List<AquariumPieceData> pieces = new List<AquariumPieceData>();

        /// <summary>
        /// シングルトンインスタンスを取得
        /// </summary>
        public static AquariumPieceRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<AquariumPieceRegistry>("AquariumPieceRegistry");
                    if (instance == null)
                    {
                        Debug.LogError("AquariumPieceRegistry not found in Resources folder! Please create one at Assets/Resources/AquariumPieceRegistry.asset");
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// 登録されている全ての設置物を取得
        /// </summary>
        public IReadOnlyList<AquariumPieceData> Pieces => pieces;
    }
}
