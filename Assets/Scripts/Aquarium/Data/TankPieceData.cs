using System;
using Blue.Entity;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 生物を展示する水槽の定義
    /// </summary>
    [CreateAssetMenu(fileName = "TankPiece", menuName = "Blue/ScriptableObject/Aquarium/TankPiece")]
    public class TankPieceData : GridPieceData
    {
        [Header("収容条件")]
        [SerializeField] private HabitationArea[] supportedHabitations = { HabitationArea.Shallow };
        [SerializeField] private float maxDisplaySize = 1.0f; // 1体あたりの大きさの上限
        [SerializeField] private float capacity = 3.0f;       // 収容コストの合計上限
        [SerializeField] private bool allowsSchool;           // 群れ型の生物を入れられるか
        [SerializeField] private int schoolDisplayCount = 20; // 群れを入れたときに表示する匹数

        [Header("遊泳ボリューム")]
        // 展示した生物が泳ぎ回る範囲。水槽の原点からの相対で、size は全体の辺の長さ。
        // BaseSwimmer.SetRoamArea は中心からの半径を取るので、渡すのは半分の値になる
        [SerializeField] private Vector3 swimAreaCenter = Vector3.zero;
        [SerializeField] private Vector3 swimAreaSize = new Vector3(2f, 1.5f, 2f);

        public float MaxDisplaySize => maxDisplaySize;
        public float Capacity => capacity;
        public bool AllowsSchool => allowsSchool;
        public int SchoolDisplayCount => schoolDisplayCount;
        public Vector3 SwimAreaCenter => swimAreaCenter;
        public Vector3 SwimAreaSize => swimAreaSize;

        /// <summary>
        /// 遊泳範囲の中心からの半径
        /// </summary>
        public Vector3 SwimAreaExtents => swimAreaSize * 0.5f;

        /// <summary>
        /// 指定された生息域に対応しているか
        /// </summary>
        public bool SupportsHabitation(HabitationArea habitation)
        {
            if (supportedHabitations == null) return false;

            return Array.IndexOf(supportedHabitations, habitation) >= 0;
        }
    }
}
