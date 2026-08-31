using System;
using System.Collections.Generic;
using Blue.Entity;
using Blue.Save;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 編集UIができるまでの動作確認用。設置物を並べ、捕獲済みの生物を入れられる水槽へ配る
    /// </summary>
    // レイアウトが空のときしか動かない。セーブされた配置の上に毎回積み増すのを避けるため
    public class AquariumDebugPlacer : MonoBehaviour
    {
        [SerializeField] private AquariumSceneBootstrap bootstrap;
        [SerializeField] private string[] roomsToUnlock;
        [SerializeField] private DebugPlacement[] placements;
        [SerializeField] private bool exhibitCapturedEntities = true;

        // 所持していることにする生物は DebugEntityStockProvider が決める。
        // ここでは所持しているものを水槽へ配るだけ

        private void Start()
        {
            if (bootstrap == null || bootstrap.Model == null)
            {
                Debug.LogError("AquariumSceneBootstrap が設定されていません", this);
                return;
            }

            AquariumModel model = bootstrap.Model;

            if (model.Layout.Pieces.Count > 0)
            {
                Debug.Log("[AquariumDebugPlacer] 既に配置があるため何もしません");
                return;
            }

            UnlockRooms(model);
            PlacePieces(model);

            if (exhibitCapturedEntities) ExhibitCaptured(model);
        }

        private void UnlockRooms(AquariumModel model)
        {
            if (roomsToUnlock == null) return;

            foreach (string room_id in roomsToUnlock)
            {
                model.Layout.UnlockRoom(room_id);
            }
        }

        private void PlacePieces(AquariumModel model)
        {
            if (placements == null) return;

            foreach (DebugPlacement placement in placements)
            {
                if (placement.piece == null) continue;

                model.Layout.TryPlace(placement.piece, placement.cell, placement.rotationStep, out PlacementRejection rejection);

                if (rejection != PlacementRejection.None)
                {
                    Debug.LogWarning($"[AquariumDebugPlacer] 設置できませんでした: {placement.piece.Name} {placement.cell} ({rejection})", this);
                }
            }
        }

        private void ExhibitCaptured(AquariumModel model)
        {
            List<EntityData> entities = CollectEntities(model);
            if (entities.Count == 0)
            {
                Debug.LogWarning("[AquariumDebugPlacer] 展示できる生物がありません", this);
                return;
            }

            // 常に先頭の水槽へ入れると1台に全部溜まるので、受け入れられる水槽へ順番に配る
            int next_tank = 0;

            foreach (EntityData entity in entities)
            {
                if (entity == null) continue;

                List<PlacedPiece> tanks = model.FindTanksAccepting(entity);
                if (tanks.Count == 0)
                {
                    Debug.LogWarning($"[AquariumDebugPlacer] 入れられる水槽がありません: {entity.Name}", this);
                    continue;
                }

                PlacedPiece target = tanks[next_tank % tanks.Count];
                next_tank++;

                if (!model.TryExhibitEntity(target.InstanceID, entity, out ExhibitRejection rejection))
                {
                    Debug.LogWarning($"[AquariumDebugPlacer] 展示できませんでした: {entity.Name} ({rejection})", this);
                }
            }
        }

        // 展示する候補は所持数の出どころに合わせる。ここで別の一覧を作ると、
        // 「置けたのに一覧に出ない」「一覧に出るのに置けない」というズレが生まれる
        private static List<EntityData> CollectEntities(AquariumModel model)
        {
            if (model.Stock == null) return new List<EntityData>();

            return new List<EntityData>(model.Stock.EnumerateOwned());
        }

        /// <summary>
        /// 動作確認用の設置指定1件
        /// </summary>
        [Serializable]
        public class DebugPlacement
        {
            public GridPieceData piece;
            public Vector2Int cell;
            public int rotationStep;
        }
    }
}
