using System.Collections.Generic;
using Blue.Entity;
using Blue.Item;

namespace Blue.Aquarium
{
    /// <summary>
    /// 展示できるかどうかの判定を一手に引き受ける
    /// </summary>
    public static class ExhibitRule
    {
        // 群れは1匹分より場所を取る。SchoolController の匹数までは見ず、一律の係数で概算する
        public const float SCHOOL_COST_MULTIPLIER = 3.0f;

        /// <summary>
        /// 生物1体が水槽の容量をどれだけ使うか
        /// </summary>
        public static float GetCost(EntityData entity)
        {
            if (entity == null) return 0f;

            float cost = entity.DisplaySize;
            if (entity.School != null)
            {
                cost *= SCHOOL_COST_MULTIPLIER;
            }

            return cost;
        }

        /// <summary>
        /// 現在の展示内容が使っている容量の合計
        /// </summary>
        public static float GetUsedCapacity(IReadOnlyList<EntityData> contents)
        {
            if (contents == null) return 0f;

            float used = 0f;
            for (int i = 0; i < contents.Count; i++)
            {
                used += GetCost(contents[i]);
            }

            return used;
        }

        /// <summary>
        /// 水槽に生物を追加できるかを判定する
        /// </summary>
        public static ExhibitRejection EvaluateEntity(TankPieceData tank, IReadOnlyList<EntityData> contents, EntityData entity)
        {
            if (tank == null) return ExhibitRejection.NotATank;
            if (entity == null) return ExhibitRejection.InvalidExhibit;

            if (!tank.SupportsHabitation(entity.Habitation))
            {
                return ExhibitRejection.HabitationMismatch;
            }

            if (entity.DisplaySize > tank.MaxDisplaySize)
            {
                return ExhibitRejection.TooLarge;
            }

            if (entity.School != null && !tank.AllowsSchool)
            {
                return ExhibitRejection.SchoolNotSupported;
            }

            if (GetUsedCapacity(contents) + GetCost(entity) > tank.Capacity)
            {
                return ExhibitRejection.CapacityFull;
            }

            return ExhibitRejection.None;
        }

        /// <summary>
        /// 展示台にアイテムを追加できるかを判定する
        /// </summary>
        public static ExhibitRejection EvaluateItem(PedestalPieceData pedestal, IReadOnlyList<ItemData> contents, ItemData item)
        {
            if (pedestal == null) return ExhibitRejection.NotAPedestal;
            if (item == null) return ExhibitRejection.InvalidExhibit;

            if (!pedestal.AcceptsType(item.Type))
            {
                return ExhibitRejection.ItemTypeMismatch;
            }

            int current_count = contents?.Count ?? 0;
            if (current_count >= pedestal.SlotCount)
            {
                return ExhibitRejection.CapacityFull;
            }

            return ExhibitRejection.None;
        }

        /// <summary>
        /// 断られた理由を画面に出す文言にする
        /// </summary>
        public static string Describe(ExhibitRejection rejection)
        {
            return rejection switch
            {
                ExhibitRejection.None => string.Empty,
                ExhibitRejection.NotATank => "生物を入れられる設置物ではありません",
                ExhibitRejection.NotAPedestal => "アイテムを飾れる設置物ではありません",
                ExhibitRejection.InvalidExhibit => "展示するものが選ばれていません",
                ExhibitRejection.HabitationMismatch => "この水槽の生息域では飼えません",
                ExhibitRejection.TooLarge => "この水槽には大きすぎます",
                ExhibitRejection.SchoolNotSupported => "この水槽は群れに対応していません",
                ExhibitRejection.ItemTypeMismatch => "この展示台には飾れない種類です",
                ExhibitRejection.CapacityFull => "水槽に空きがありません",
                ExhibitRejection.PieceNotFound => "対象の設置物が見つかりません",
                ExhibitRejection.StockExhausted => "持っている分をすべて展示しています",
                _ => rejection.ToString(),
            };
        }
    }

    /// <summary>
    /// 展示を断った理由。UIの説明文とグレーアウトの根拠に使う
    /// </summary>
    public enum ExhibitRejection
    {
        None,                // 展示できる
        NotATank,            // 生物を入れられる設置物ではない
        NotAPedestal,        // アイテムを飾れる設置物ではない
        InvalidExhibit,      // 展示対象が不正
        HabitationMismatch,  // 生息域が合わない
        TooLarge,            // 生物が水槽に対して大きすぎる
        SchoolNotSupported,  // 群れ型に対応していない
        ItemTypeMismatch,    // アイテムの種類が合わない
        CapacityFull,        // 空きがない
        PieceNotFound,       // 対象の設置物が存在しない
        StockExhausted,      // 持っている数を全て展示済み
    }
}
