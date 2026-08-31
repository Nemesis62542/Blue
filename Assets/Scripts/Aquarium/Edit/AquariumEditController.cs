using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 編集モードの操作。選択と設置を切り替えて扱う
    /// </summary>
    // 設置できるかどうかは AquariumLayoutModel.CanPlace が唯一の判断元。
    // ここでは判定を持たず、返ってきた理由を下見の色と UI へ流すだけにする
    public class AquariumEditController : MonoBehaviour
    {
        [SerializeField] private AquariumSceneBootstrap bootstrap;
        [SerializeField] private AquariumEditCamera editCamera;
        [SerializeField] private Camera view;
        [SerializeField] private PlacementGhost ghost;

        // 設置物を選ぶ UI ができるまでの仮の持ち方
        [SerializeField] private List<GridPieceData> palette = new List<GridPieceData>();

        [SerializeField] private AquariumEditInput input = new AquariumEditInput();

        private int selectedIndex;
        private int rotationStep;
        private Vector2Int cursorCell;
        private Vector2Int pointedCell;
        private bool hasCursor;

        /// <summary>
        /// いま選んでいる設置物
        /// </summary>
        public GridPieceData SelectedPiece =>
            palette.Count > 0 ? palette[Mathf.Clamp(selectedIndex, 0, palette.Count - 1)] : null;

        /// <summary>
        /// 選択と設置のどちらの構えか
        /// </summary>
        // 常に下見が張り付いていると、置いてあるものが隠れて見えない。
        // 設置物を選んだときだけ設置の構えに入り、右クリックで選択へ戻す
        public AquariumEditTool Tool { get; private set; } = AquariumEditTool.Select;

        /// <summary>
        /// カーソル位置に置けるか。置けない場合はその理由
        /// </summary>
        public PlacementRejection CurrentRejection { get; private set; }

        /// <summary>
        /// 撤去する構えかどうか
        /// </summary>
        public bool IsRemoving { get; private set; }

        /// <summary>
        /// 選択中の設置物やカーソルの状態が変わったときに通知する
        /// </summary>
        public event Action OnStateChanged;

        /// <summary>
        /// 設置済みのものが選ばれたときに通知する。展示画面を開く入口
        /// </summary>
        public event Action<PlacedPiece> OnPieceInspected;

        /// <summary>
        /// 入力を受け付けないようにする。画面を重ねているあいだに使う
        /// </summary>
        // 止めないと、UI を押したクリックが背後の設置にも通ってしまう
        public bool IsSuspended { get; set; }

        public AquariumEditInput Input => input;

        private AquariumModel Model => bootstrap != null ? bootstrap.Model : null;

        private void Awake()
        {
            if (view == null && editCamera != null) view = editCamera.GetComponent<Camera>();
        }

        private void OnEnable()
        {
            rotationStep = 0;
            SetTool(AquariumEditTool.Select);
        }

        private void OnDisable()
        {
            if (ghost != null) ghost.SetVisible(false);
        }

        private void Update()
        {
            if (Model == null || view == null) return;

            if (IsSuspended)
            {
                if (ghost != null) ghost.SetVisible(false);
                return;
            }

            HandleCamera();
            HandlePalette();
            HandleToolSwitch();

            if (Tool == AquariumEditTool.Place) HandleRotate();

            IsRemoving = input.RemoveHeld;

            UpdateCursor();
            HandleCommit();
        }

        private void HandleCamera()
        {
            if (editCamera == null) return;

            editCamera.Pan(input.Pan, Time.deltaTime);
            editCamera.Zoom(input.Zoom);
        }

        private void HandlePalette()
        {
            if (palette.Count == 0) return;
            if (!input.NextPiece && !input.PreviousPiece) return;

            if (input.NextPiece) selectedIndex = (selectedIndex + 1) % palette.Count;
            if (input.PreviousPiece) selectedIndex = (selectedIndex - 1 + palette.Count) % palette.Count;

            // 設置物を選んだ操作は「置きたい」という意思表示なので、そのまま設置の構えに入る
            SetTool(AquariumEditTool.Place);
        }

        private void HandleToolSwitch()
        {
            // 右クリックは構えの取り消し。選択へ戻る
            if (input.Cancel && Tool != AquariumEditTool.Select) SetTool(AquariumEditTool.Select);
        }

        private void SetTool(AquariumEditTool tool)
        {
            Tool = tool;

            if (ghost != null)
            {
                ghost.SetPiece(tool == AquariumEditTool.Place ? SelectedPiece : null);
                ghost.SetVisible(false);
            }

            OnStateChanged?.Invoke();
        }

        private void HandleRotate()
        {
            if (!input.Rotate) return;

            rotationStep = AquariumGrid.NormalizeStep(rotationStep + 1);
            OnStateChanged?.Invoke();
        }

        private void UpdateCursor()
        {
            hasCursor = TryGetCursorCell(out pointedCell);

            GridPieceData piece = SelectedPiece;
            bool shows_ghost = hasCursor && piece != null && Tool == AquariumEditTool.Place && !IsRemoving;

            if (!shows_ghost)
            {
                if (ghost != null) ghost.SetVisible(false);
                CurrentRejection = PlacementRejection.InvalidPiece;
                return;
            }

            cursorCell = CenterOnCursor(pointedCell, piece, rotationStep);
            CurrentRejection = Model.Layout.CanPlace(piece, cursorCell, rotationStep);

            if (ghost == null) return;

            ghost.SetPiece(piece);
            ghost.SetVisible(true);
            ghost.UpdatePlacement(
                AquariumGrid.CellToWorld(cursorCell, piece.Footprint, rotationStep),
                AquariumGrid.StepToRotation(rotationStep),
                EvaluateFeedback(piece)
            );
        }

        // 通路に面していなくても設置は断らない。見に行けないことを色で知らせるだけ
        private PlacementFeedback EvaluateFeedback(GridPieceData piece)
        {
            if (CurrentRejection != PlacementRejection.None) return PlacementFeedback.Blocked;

            // 通路そのものや装飾に「通路に面していない」は無意味なので、展示するものだけ見る
            if (!IsExhibitPiece(piece)) return PlacementFeedback.Ready;

            return Model.Layout.IsFacingPath(cursorCell, piece.Footprint, rotationStep)
                ? PlacementFeedback.Ready
                : PlacementFeedback.NotOnPath;
        }

        private static bool IsExhibitPiece(GridPieceData piece)
        {
            return piece is TankPieceData or PedestalPieceData;
        }

        private void HandleCommit()
        {
            if (!input.Commit || !hasCursor) return;

            if (IsRemoving)
            {
                RemoveAtCursor();
                return;
            }

            if (Tool == AquariumEditTool.Select)
            {
                InspectAtCursor();
                return;
            }

            PlaceAtCursor();
        }

        private void InspectAtCursor()
        {
            PlacedPiece target = Model.Layout.GetPieceAt(pointedCell);
            if (target == null) return;

            OnPieceInspected?.Invoke(target);
        }

        private void PlaceAtCursor()
        {
            GridPieceData piece = SelectedPiece;
            if (piece == null) return;

            Model.Layout.TryPlace(piece, cursorCell, rotationStep, out PlacementRejection rejection);

            if (rejection != PlacementRejection.None)
            {
                Debug.Log($"[AquariumEdit] 設置できません: {DescribeRejection(rejection)}");
                return;
            }

            OnStateChanged?.Invoke();
        }

        private void RemoveAtCursor()
        {
            PlacedPiece target = Model.Layout.GetPieceAt(pointedCell);
            if (target == null) return;

            Model.Layout.RemovePiece(target.InstanceID);
            OnStateChanged?.Invoke();
        }

        // 掴んでいる物の中心をカーソルに合わせる。最小セルを合わせると、
        // 大きな設置物ほどカーソルから離れた場所に出て狙いにくい
        private static Vector2Int CenterOnCursor(Vector2Int pointed, GridPieceData piece, int rotation_step)
        {
            Vector2Int rotated = AquariumGrid.RotateFootprint(piece.Footprint, rotation_step);

            return pointed - new Vector2Int(rotated.x / 2, rotated.y / 2);
        }

        private bool TryGetCursorCell(out Vector2Int cell)
        {
            cell = default;

            Ray ray = view.ScreenPointToRay(input.PointerPosition);

            // 設置面は y=0。床のコライダーに当てると、置いた設置物の上でカーソルが
            // 跳ねてセルが飛ぶため、数式上の平面と交差させる
            Plane floor = new Plane(Vector3.up, Vector3.zero);

            if (!floor.Raycast(ray, out float distance)) return false;

            cell = AquariumGrid.WorldToCell(ray.GetPoint(distance));
            return true;
        }

        /// <summary>
        /// 断られた理由を画面に出す文言にする
        /// </summary>
        public static string DescribeRejection(PlacementRejection rejection)
        {
            return rejection switch
            {
                PlacementRejection.None => string.Empty,
                PlacementRejection.InvalidPiece => "設置物が選ばれていません",
                PlacementRejection.OutsideUnlockedArea => "部屋の外には置けません",
                PlacementRejection.CellOccupied => "ほかの設置物と重なっています",
                _ => rejection.ToString(),
            };
        }
    }

    /// <summary>
    /// 編集モードの構え
    /// </summary>
    public enum AquariumEditTool
    {
        Select, // 置いてあるものを選ぶ
        Place,  // 選んだ設置物を置く
    }
}
