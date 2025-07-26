using UnityEngine;

namespace Blue.Game
{
    public class GameEventController : MonoBehaviour
    {
        [SerializeField] private GameObject cinematicObject; // ムービー用オブジェクト（Timeline等）

        public void TriggerEvent()
        {
            Debug.Log("イベント発生！");

            // ムービー開始 or 任意の演出
            if (cinematicObject != null)
            {
                cinematicObject.SetActive(true);
            }

            // 必要に応じて：
            // - プレイヤー操作停止
            // - カメラ切り替え
            // - SE/BGM変更
            // - UIの非表示
        }
    }
}