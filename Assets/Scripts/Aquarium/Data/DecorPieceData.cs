using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 位置と向きを自由に決められる装飾の定義
    /// </summary>
    [CreateAssetMenu(fileName = "DecorPiece", menuName = "Blue/ScriptableObject/Aquarium/DecorPiece")]
    public class DecorPieceData : AquariumPieceData
    {
        // 水槽の中に沈める岩や海藻など、他の設置物に載せる装飾を区別する
        [SerializeField] private bool placeableInsideTank;

        public bool PlaceableInsideTank => placeableInsideTank;

        public override PiecePlacement Placement => PiecePlacement.Free;
    }
}
