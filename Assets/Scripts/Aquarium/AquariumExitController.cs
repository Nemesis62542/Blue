using Blue.Audio;
using Blue.Game;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 水族館から出て別のシーンへ戻る
    /// </summary>
    // 水族館はタイトルからも Garage からも入れるが、出る手段はシーン側にしか置けない。
    // 以前は CharacterMovementController が仮に持っていたものを、役割として切り出した
    public class AquariumExitController : MonoBehaviour
    {
        [SerializeField] private string returnSceneName = "Garage";
        [SerializeField] private KeyCode exitKey = KeyCode.Escape;

        // 編集中は別の操作に集中しているので出さない。未設定なら常に出られる
        [SerializeField] private AquariumModeController modeController;

        private void Update()
        {
            if (!UnityEngine.Input.GetKeyDown(exitKey)) return;
            if (modeController != null && modeController.Mode == AquariumMode.Edit) return;

            Exit();
        }

        /// <summary>
        /// 戻り先のシーンへ移動する
        /// </summary>
        public void Exit()
        {
            if (string.IsNullOrEmpty(returnSceneName))
            {
                Debug.LogError("戻り先のシーンが設定されていません", this);
                return;
            }

            if (SoundController.Instance != null) SoundController.Instance.StopBGM(0);

            SceneLoader.LoadScene(returnSceneName);
        }
    }
}
