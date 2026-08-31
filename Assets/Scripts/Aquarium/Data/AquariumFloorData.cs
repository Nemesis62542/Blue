using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 水族館の間取り。設置できるセルの範囲を部屋単位で定義する
    /// </summary>
    [CreateAssetMenu(fileName = "AquariumFloor", menuName = "Blue/ScriptableObject/Aquarium/Floor")]
    public class AquariumFloorData : ScriptableObject
    {
        [SerializeField] private List<AquariumRoomDefinition> rooms = new List<AquariumRoomDefinition>();

        public IReadOnlyList<AquariumRoomDefinition> Rooms => rooms;

        /// <summary>
        /// IDから部屋を取得
        /// </summary>
        public AquariumRoomDefinition FindRoom(string room_id)
        {
            return rooms.Find(r => r.RoomID == room_id);
        }
    }

    /// <summary>
    /// 部屋1つ分の範囲と解放条件
    /// </summary>
    [Serializable]
    public class AquariumRoomDefinition
    {
        [SerializeField] private string roomID;                  // セーブデータから参照する識別子
        [SerializeField] private string name;                    // 画面に出す部屋名
        [SerializeField] private Vector2Int origin;              // 範囲の最小セル
        [SerializeField] private Vector2Int size = new Vector2Int(8, 8); // セル数(X,Z)
        [SerializeField] private bool unlockedFromStart;         // 最初から解放されているか

        // 来館者が入ってくるセル。ここを起点に通路の繋がりを辿る。
        // 起点が無いと、通路をいくら置いても「どこから来るのか」が決まらない
        [SerializeField] private Vector2Int[] entrances = new Vector2Int[0];

        public string RoomID => roomID;
        public string Name => name;
        public Vector2Int Origin => origin;
        public Vector2Int Size => size;
        public bool UnlockedFromStart => unlockedFromStart;
        public IReadOnlyList<Vector2Int> Entrances => entrances;

        /// <summary>
        /// 指定セルがこの部屋に含まれるか
        /// </summary>
        public bool Contains(Vector2Int cell)
        {
            return cell.x >= origin.x && cell.x < origin.x + size.x
                && cell.y >= origin.y && cell.y < origin.y + size.y;
        }
    }
}
