using Unity.Cinemachine;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 編集中のカメラの切り替え。俯瞰と、水槽の中を覗く寄りを持つ
    /// </summary>
    // Priority で切り替えるのは GarageCameraController と同じ流儀。
    // 補間は CinemachineBrain に任せる。俯瞰カメラの Transform を自前で動かしているため、
    // 実カメラを直接動かす実装とは併用できない（Brain が毎フレーム上書きする）
    public class AquariumCameraDirector : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera overviewCamera;
        [SerializeField] private CinemachineCamera focusCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 0;

        /// <summary>
        /// 水槽の中が見える位置へ寄る
        /// </summary>
        public void FocusTank(PlacedPiece tank)
        {
            if (focusCamera == null || tank == null) return;
            if (tank.Piece is not TankPieceData data) return;

            Vector3 origin = tank.GetWorldPosition();
            Quaternion rotation = tank.GetWorldRotation();

            Vector3 look_at = origin + rotation * data.SwimAreaCenter;
            Vector3 position = origin + rotation * data.ViewOffset;
            Vector3 forward = look_at - position;

            // 見る位置と見る先が重なっていると向きが決まらない。設定漏れの水槽で落ちないようにする
            if (forward.sqrMagnitude < 0.0001f)
            {
                Debug.LogWarning($"中を見せる位置が遊泳範囲の中心と重なっています: {data.Name}", this);
                return;
            }

            focusCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));

            focusCamera.Priority = activePriority;
            if (overviewCamera != null) overviewCamera.Priority = inactivePriority;
        }

        /// <summary>
        /// 俯瞰へ戻る
        /// </summary>
        public void ReturnToOverview()
        {
            if (focusCamera != null) focusCamera.Priority = inactivePriority;
            if (overviewCamera != null) overviewCamera.Priority = activePriority;
        }
    }
}
