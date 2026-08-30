using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 水族館シーンの入口。セーブデータからモデルを起こし、シーンへの反映を AquariumBuilder に任せる
    /// </summary>
    public class AquariumSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private AquariumFloorData floor;
        [SerializeField] private AquariumBuilder builder;

        /// <summary>
        /// このシーンが扱う水族館。編集UIや展示UIはここから受け取る
        /// </summary>
        public AquariumModel Model { get; private set; }

        private void Awake()
        {
            if (floor == null)
            {
                Debug.LogError("AquariumFloorData が設定されていません", this);
                return;
            }

            // 展示しても所持数は減らないが、持っている数までしか同時に展示できない
            Model = AquariumSaveConverter.LoadAquarium(floor, new CapturedEntityStock());

            if (builder == null)
            {
                Debug.LogError("AquariumBuilder が設定されていません", this);
                return;
            }

            builder.Bind(Model);
        }

        /// <summary>
        /// 現在の水族館を保存する
        /// </summary>
        // 編集を抜けたときなど、区切りで明示的に呼ぶ。毎回の設置で書き出すと重い
        public void Save()
        {
            if (Model == null) return;

            AquariumSaveConverter.SaveAquarium(Model);
        }
    }
}
