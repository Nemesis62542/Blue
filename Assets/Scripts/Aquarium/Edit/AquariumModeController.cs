using System;
using Blue.Input;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 見学モードと編集モードの切り替え
    /// </summary>
    public class AquariumModeController : MonoBehaviour
    {
        [SerializeField] private AquariumSceneBootstrap bootstrap;

        [Header("見学")]
        [SerializeField] private GameObject viewRig;   // 一人称のプレイヤー一式

        [Header("編集")]
        [SerializeField] private GameObject editRig;   // 俯瞰カメラと編集操作一式
        [SerializeField] private AquariumEditController editController;

        [SerializeField] private AquariumMode startMode = AquariumMode.View;

        public AquariumMode Mode { get; private set; } = AquariumMode.View;

        /// <summary>
        /// モードが変わったときに通知する
        /// </summary>
        // 編集モードの外に置いた画面は editRig を無効にしても閉じない。
        // 自分から閉じてもらうために知らせる
        public event Action<AquariumMode> OnModeChanged;

        private void Start()
        {
            Apply(startMode, save_on_leave: false);
        }

        private void Update()
        {
            if (editController == null) return;

            // 切り替えの入力は編集側の入力定義に相乗りしている。
            // 見学モードの入力は既存の InputActionMap が握っているため、ここでは触らない
            if (editController.Input.ToggleMode) Toggle();
        }

        public void Toggle()
        {
            Apply(Mode == AquariumMode.View ? AquariumMode.Edit : AquariumMode.View, save_on_leave: true);
        }

        public void SetMode(AquariumMode mode)
        {
            Apply(mode, save_on_leave: true);
        }

        private void Apply(AquariumMode mode, bool save_on_leave)
        {
            // 編集を抜けるところが保存の区切り。設置のたびに書き出すと、
            // まだ決めきっていない配置まで残ってしまう
            if (save_on_leave && Mode == AquariumMode.Edit && mode != AquariumMode.Edit)
            {
                if (bootstrap != null) bootstrap.Save();
            }

            Mode = mode;

            bool is_edit = mode == AquariumMode.Edit;

            if (viewRig != null) viewRig.SetActive(!is_edit);
            if (editRig != null) editRig.SetActive(is_edit);

            ApplyCursor(is_edit);
            ApplyInputMap(is_edit);

            OnModeChanged?.Invoke(mode);
        }

        private static void ApplyCursor(bool is_edit)
        {
            // 編集はマウスで位置を決めるので、掴んだままだと操作できない
            Cursor.lockState = is_edit ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = is_edit;
        }

        private static void ApplyInputMap(bool is_edit)
        {
            if (PlayerInputHandler.Instance == null) return;

            // 編集中の入力は AquariumEditInput が旧 Input から直接読む。
            // 見学用のマップを生かしたままだと、移動入力が二重に効いてしまう
            PlayerInputHandler.Instance.SetInputMap(is_edit ? InputMapType.None : InputMapType.Aquarium);
        }
    }

    public enum AquariumMode
    {
        View, // 歩いて見て回る
        Edit, // 俯瞰でレイアウトを決める
    }
}
