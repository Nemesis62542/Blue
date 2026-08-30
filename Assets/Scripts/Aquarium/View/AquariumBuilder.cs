using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// AquariumModel をシーンに写す。生成と破棄はここに閉じ、モデルからシーンへの一方向にする
    /// </summary>
    public class AquariumBuilder : MonoBehaviour
    {
        [SerializeField] private Transform root; // 生成した設置物の親。未設定なら自分の下に置く

        private AquariumModel model;
        private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, AquariumPieceView> views = new Dictionary<string, AquariumPieceView>();

        private Transform Root => root != null ? root : transform;

        /// <summary>
        /// モデルと結びつけ、現在の内容をシーンに起こす
        /// </summary>
        public void Bind(AquariumModel target)
        {
            Unbind();

            model = target;
            if (model == null) return;

            Subscribe();
            BuildAll();
        }

        /// <summary>
        /// モデルとの結びつきを解き、生成したものを全て破棄する
        /// </summary>
        public void Unbind()
        {
            if (model != null)
            {
                Unsubscribe();
                model = null;
            }

            DestroyAll();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Subscribe()
        {
            model.Layout.OnPiecePlaced += HandlePiecePlaced;
            model.Layout.OnPieceRemoved += HandlePieceRemoved;
            model.Layout.OnPieceMoved += HandlePieceMoved;
            model.Layout.OnDecorPlaced += HandleDecorPlaced;
            model.Layout.OnDecorRemoved += HandleDecorRemoved;
            model.Exhibits.OnContentsChanged += RefreshExhibit;
        }

        private void Unsubscribe()
        {
            model.Layout.OnPiecePlaced -= HandlePiecePlaced;
            model.Layout.OnPieceRemoved -= HandlePieceRemoved;
            model.Layout.OnPieceMoved -= HandlePieceMoved;
            model.Layout.OnDecorPlaced -= HandleDecorPlaced;
            model.Layout.OnDecorRemoved -= HandleDecorRemoved;
            model.Exhibits.OnContentsChanged -= RefreshExhibit;
        }

        private void BuildAll()
        {
            // 装飾は設置物を親に取れるので、設置物を先に全て起こしておく
            foreach (PlacedPiece placed in model.Layout.Pieces)
            {
                CreatePiece(placed);
            }

            foreach (PlacedDecor decor in model.Layout.Decors)
            {
                CreateDecor(decor);
            }
        }

        private void DestroyAll()
        {
            foreach (KeyValuePair<string, GameObject> pair in instances)
            {
                if (pair.Value != null) Destroy(pair.Value);
            }

            instances.Clear();
            views.Clear();
        }

        // ---------------- 設置物 ----------------

        private void HandlePiecePlaced(PlacedPiece placed)
        {
            CreatePiece(placed);
        }

        private void HandlePieceRemoved(PlacedPiece placed)
        {
            DestroyInstance(placed.InstanceID);
        }

        private void HandlePieceMoved(PlacedPiece placed)
        {
            if (!instances.TryGetValue(placed.InstanceID, out GameObject instance) || instance == null) return;

            instance.transform.SetPositionAndRotation(placed.GetWorldPosition(), placed.GetWorldRotation());
        }

        private void CreatePiece(PlacedPiece placed)
        {
            GameObject prefab = placed.Piece.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"設置物のプレハブが設定されていません: {placed.Piece.Name}", this);
                return;
            }

            GameObject instance = Instantiate(prefab, placed.GetWorldPosition(), placed.GetWorldRotation(), Root);
            instances[placed.InstanceID] = instance;

            AquariumPieceView view = instance.GetComponent<AquariumPieceView>();
            if (view != null)
            {
                view.Bind(placed);
                views[placed.InstanceID] = view;
            }

            // 復元時は展示内容が先に入っていることがあるので、生成した時点で一度反映する
            RefreshExhibit(placed.InstanceID);
        }

        // ---------------- 装飾 ----------------

        private void HandleDecorPlaced(PlacedDecor decor)
        {
            CreateDecor(decor);
        }

        private void HandleDecorRemoved(PlacedDecor decor)
        {
            DestroyInstance(decor.InstanceID);
        }

        private void CreateDecor(PlacedDecor decor)
        {
            GameObject prefab = decor.Piece.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"装飾のプレハブが設定されていません: {decor.Piece.Name}", this);
                return;
            }

            Transform parent = Root;
            if (decor.HasParent && instances.TryGetValue(decor.ParentInstanceID, out GameObject parent_instance) && parent_instance != null)
            {
                parent = parent_instance.transform;
            }

            GameObject instance = Instantiate(prefab, parent);

            // 親に載せた装飾は相対位置、単独で置いた装飾はワールド位置として扱う
            if (parent == Root)
            {
                instance.transform.SetPositionAndRotation(decor.Position, Quaternion.Euler(0f, decor.Yaw, 0f));
            }
            else
            {
                instance.transform.SetLocalPositionAndRotation(decor.Position, Quaternion.Euler(0f, decor.Yaw, 0f));
            }

            instances[decor.InstanceID] = instance;
        }

        // ---------------- 展示内容 ----------------

        private void RefreshExhibit(string instance_id)
        {
            if (model == null) return;
            if (!views.TryGetValue(instance_id, out AquariumPieceView view) || view == null) return;

            switch (view)
            {
                case TankView tank:
                    tank.RefreshContents(model.Exhibits.GetEntities(instance_id));
                    break;

                case PedestalView pedestal:
                    pedestal.RefreshContents(model.Exhibits.GetItems(instance_id));
                    break;
            }
        }

        private void DestroyInstance(string instance_id)
        {
            if (views.TryGetValue(instance_id, out AquariumPieceView view))
            {
                if (view != null) view.ClearContents();
                views.Remove(instance_id);
            }

            if (!instances.TryGetValue(instance_id, out GameObject instance)) return;

            if (instance != null) Destroy(instance);
            instances.Remove(instance_id);
        }
    }
}
