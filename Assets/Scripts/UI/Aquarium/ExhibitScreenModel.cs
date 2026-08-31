using System;
using System.Collections.Generic;
using Blue.Aquarium;
using Blue.Entity;

namespace Blue.UI.Exhibit
{
    /// <summary>
    /// 水槽1台ぶんの展示画面の状態
    /// </summary>
    // 可否の判定は持たない。AquariumModel に尋ねた結果を、画面が並べやすい形に整えるだけ
    public class ExhibitScreenModel
    {
        private readonly AquariumModel aquarium;
        private readonly List<ExhibitCandidate> candidates = new List<ExhibitCandidate>();

        public ExhibitScreenModel(AquariumModel aquarium_model)
        {
            aquarium = aquarium_model;
        }

        /// <summary>
        /// 編集中の水槽
        /// </summary>
        public PlacedPiece Tank { get; private set; }

        public TankPieceData TankData => Tank?.Piece as TankPieceData;

        public IReadOnlyList<EntityData> Contents =>
            Tank != null ? aquarium.Exhibits.GetEntities(Tank.InstanceID) : Array.Empty<EntityData>();

        /// <summary>
        /// 展示できる生物と、その状態
        /// </summary>
        public IReadOnlyList<ExhibitCandidate> Candidates => candidates;

        /// <summary>
        /// 中身が変わったときに通知する
        /// </summary>
        public event Action OnChanged;

        public void SetTank(PlacedPiece tank)
        {
            Tank = tank;
            Rebuild();
        }

        public bool TryAdd(EntityData entity)
        {
            if (Tank == null) return false;
            if (!aquarium.TryExhibitEntity(Tank.InstanceID, entity, out _)) return false;

            Rebuild();
            return true;
        }

        public bool Remove(EntityData entity)
        {
            if (Tank == null) return false;
            if (!aquarium.RemoveExhibitedEntity(Tank.InstanceID, entity)) return false;

            Rebuild();
            return true;
        }

        /// <summary>
        /// この水槽が使っている容量と上限
        /// </summary>
        public void GetCapacity(out float used, out float total)
        {
            used = ExhibitRule.GetUsedCapacity(Contents);
            total = TankData != null ? TankData.Capacity : 0f;
        }

        private void Rebuild()
        {
            candidates.Clear();

            if (Tank == null)
            {
                OnChanged?.Invoke();
                return;
            }

            foreach (EntityData entity in EnumerateSelectable())
            {
                if (entity == null) continue;

                candidates.Add(new ExhibitCandidate
                {
                    Entity = entity,
                    ExhibitedHere = CountIn(Contents, entity),
                    ExhibitedTotal = aquarium.CountExhibited(entity),
                    Owned = aquarium.Stock?.GetOwnedCount(entity) ?? int.MaxValue,
                    Rejection = aquarium.CanExhibitEntity(Tank.InstanceID, entity),
                });
            }

            OnChanged?.Invoke();
        }

        // 候補は必ず所持の記録から引く。登録済みの EntityData を列挙してはいけない。
        // あれはステータス定義の一覧で図鑑ではないため、プレイヤー自身が混ざるうえ、
        // まだ捕まえていない生物まで名前が見えてしまう
        private IEnumerable<EntityData> EnumerateSelectable()
        {
            return aquarium.Stock != null ? aquarium.Stock.EnumerateOwned() : Array.Empty<EntityData>();
        }

        private static int CountIn(IReadOnlyList<EntityData> contents, EntityData entity)
        {
            int count = 0;

            for (int i = 0; i < contents.Count; i++)
            {
                if (contents[i] == entity) count++;
            }

            return count;
        }
    }

    /// <summary>
    /// 展示画面に並べる1行ぶんの情報
    /// </summary>
    public struct ExhibitCandidate
    {
        public EntityData Entity;
        public int ExhibitedHere;   // この水槽に何匹入っているか
        public int ExhibitedTotal;  // 館全体で何匹展示しているか
        public int Owned;           // 所持数
        public ExhibitRejection Rejection;

        public bool CanAdd => Rejection == ExhibitRejection.None;
        public bool CanRemove => ExhibitedHere > 0;
    }
}
