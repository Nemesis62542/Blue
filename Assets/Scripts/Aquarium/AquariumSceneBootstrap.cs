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

        // 未設定なら捕獲記録をそのまま所持数として使う。動作確認用に差し替えられる
        [SerializeField] private EntityStockProvider stockProvider;

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

            // 展示しても所持数は減らないが、持っている数までしか同時に展示できない。
            // 復元もこの上限で判定されるので、所持数は読み込みより前に決めておく
            IEntityStock stock = stockProvider != null ? stockProvider.CreateStock() : new CapturedEntityStock();

            Model = AquariumSaveConverter.LoadAquarium(floor, stock);

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
