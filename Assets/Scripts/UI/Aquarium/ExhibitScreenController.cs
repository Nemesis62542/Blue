using Blue.Aquarium;
using Blue.Entity;
using UnityEngine;

namespace Blue.UI.Exhibit
{
    /// <summary>
    /// 水槽の中身を入れ替える画面の制御
    /// </summary>
    public class ExhibitScreenController : MonoBehaviour
    {
        [SerializeField] private AquariumSceneBootstrap bootstrap;
        [SerializeField] private ExhibitScreenView view;
        [SerializeField] private AquariumEditController editController;
        [SerializeField] private AquariumCameraDirector cameraDirector;
        [SerializeField] private AquariumModeController modeController;

        private ExhibitScreenModel model;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (view == null)
            {
                Debug.LogError("ExhibitScreenView が設定されていません", this);
                return;
            }

            view.OnAdd += HandleAdd;
            view.OnRemove += HandleRemove;
            view.OnClose += Close;
        }

        private void OnEnable()
        {
            if (editController != null) editController.OnPieceInspected += HandlePieceInspected;
            if (modeController != null) modeController.OnModeChanged += HandleModeChanged;
        }

        private void OnDisable()
        {
            if (editController != null) editController.OnPieceInspected -= HandlePieceInspected;
            if (modeController != null) modeController.OnModeChanged -= HandleModeChanged;
        }

        // この画面は編集モードのオブジェクトの外にあるので、editRig を無効にしても閉じない。
        // 開いたまま見学へ抜けると、カーソルが消えた状態でパネルだけが残る
        private void HandleModeChanged(AquariumMode mode)
        {
            if (mode != AquariumMode.Edit && IsOpen) Close();
        }

        private void HandlePieceInspected(PlacedPiece piece)
        {
            // 生物を入れられる設置物だけが対象。展示台は別の画面で扱う
            if (piece == null || piece.Piece is not TankPieceData) return;

            Open(piece);
        }

        public void Open(PlacedPiece tank)
        {
            if (bootstrap == null || bootstrap.Model == null) return;

            model ??= CreateModel();
            model.SetTank(tank);

            IsOpen = true;
            view.SetVisible(true);
            view.Refresh(model);

            // 画面を開いているあいだの左クリックは行の操作。
            // 止めないと、UIを押すたびに背後へ水槽が設置される
            if (editController != null) editController.IsSuspended = true;

            // 入れ替えた結果が見えないと選びようがないので、中が見える位置へ寄る
            if (cameraDirector != null) cameraDirector.FocusTank(tank);
        }

        public void Close()
        {
            IsOpen = false;
            view.SetVisible(false);

            if (editController != null) editController.IsSuspended = false;
            if (cameraDirector != null) cameraDirector.ReturnToOverview();
        }

        private ExhibitScreenModel CreateModel()
        {
            ExhibitScreenModel created = new ExhibitScreenModel(bootstrap.Model);
            created.OnChanged += () => view.Refresh(created);

            return created;
        }

        private void HandleAdd(EntityData entity)
        {
            if (!IsOpen || model == null) return;

            model.TryAdd(entity);
        }

        private void HandleRemove(EntityData entity)
        {
            if (!IsOpen || model == null) return;

            model.Remove(entity);
        }
    }
}
