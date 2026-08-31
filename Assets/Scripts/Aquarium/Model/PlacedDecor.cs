using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 自由配置された装飾1つ分
    /// </summary>
    public class PlacedDecor
    {
        private readonly DecorPieceData piece;
        private readonly string instanceID;

        public DecorPieceData Piece => piece;
        public string InstanceID => instanceID;

        /// <summary>
        /// 水槽の中など、他の設置物に載せる場合の親。単独で置く場合は空
        /// </summary>
        public string ParentInstanceID { get; private set; }

        /// <summary>
        /// 親がある場合は親からの相対位置、ない場合はワールド位置
        /// </summary>
        public Vector3 Position { get; private set; }

        public float Yaw { get; private set; }

        public PlacedDecor(DecorPieceData piece_data, string instance_id, string parent_instance_id, Vector3 position, float yaw)
        {
            piece = piece_data;
            instanceID = instance_id;
            ParentInstanceID = parent_instance_id ?? string.Empty;
            Position = position;
            Yaw = yaw;
        }

        public bool HasParent => !string.IsNullOrEmpty(ParentInstanceID);

        /// <summary>
        /// 配置を変更する
        /// </summary>
        public void MoveTo(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
        }
    }
}
