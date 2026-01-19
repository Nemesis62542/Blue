using System;
using System.Collections.Generic;
using System.Linq;
using Blue.Entity;
using Blue.Object;
using UnityEngine;

namespace Blue.Game
{
    public class AquariumManager : MonoBehaviour
    {
        [SerializeField] private List<AquariumController> aquariums = new List<AquariumController>();
        [SerializeField] private PlacementMode mode = PlacementMode.Auto;
        [SerializeField] private AutoPlacementRule[] autoRules;

        public enum PlacementMode
        {
            Auto,   // 自動配置（体験版）
            Manual  // 手動配置（製品版）
        }

        /// <summary>
        /// 自動配置モードで生物を配置
        /// </summary>
        public bool AutoPlaceEntity(EntityData entity)
        {
            AquariumController target = FindTargetAquarium(entity);
            if (target == null)
            {
                Debug.LogWarning($"適切な水槽が見つかりません: {entity.Name}");
                return false;
            }

            bool success = target.TryAddEntity(entity);
            if (!success)
            {
                Debug.LogWarning($"水槽への配置に失敗しました: {entity.Name}");
            }
            return success;
        }

        /// <summary>
        /// 手動配置モードで指定水槽に生物を配置
        /// </summary>
        public bool ManualPlaceEntity(EntityData entity, int aquariumIndex)
        {
            if (aquariumIndex < 0 || aquariumIndex >= aquariums.Count)
            {
                Debug.LogError($"無効な水槽インデックス: {aquariumIndex}");
                return false;
            }

            return aquariums[aquariumIndex].TryAddEntity(entity);
        }

        /// <summary>
        /// エンティティに適した水槽を検索
        /// </summary>
        private AquariumController FindTargetAquarium(EntityData entity)
        {
            // 1. AutoPlacementRuleを優先適用
            if (autoRules != null)
            {
                foreach (var rule in autoRules)
                {
                    if (rule.Matches(entity))
                    {
                        if (rule.targetAquariumIndex >= 0 && rule.targetAquariumIndex < aquariums.Count)
                        {
                            var aquarium = aquariums[rule.targetAquariumIndex];
                            if (aquarium.HasAvailableSlot(entity))
                            {
                                return aquarium;
                            }
                        }
                    }
                }
            }

            // 2. Habitationが一致する水槽から空きスロットを持つものを検索
            return aquariums.FirstOrDefault(a =>
                a.Habitation == entity.Habitation && a.HasAvailableSlot(entity));
        }

        /// <summary>
        /// 複数の生物を一括配置
        /// </summary>
        public void PlaceEntities(List<EntityData> entities)
        {
            if (entities == null || entities.Count == 0) return;

            foreach (EntityData entity in entities)
            {
                if (mode == PlacementMode.Auto)
                {
                    AutoPlaceEntity(entity);
                }
                else
                {
                    Debug.LogWarning("手動配置モードでは PlaceEntities は使用できません");
                    break;
                }
            }
        }
    }

    [Serializable]
    public class AutoPlacementRule
    {
        [Tooltip("特定のEntityIDを指定（-1で無視）")]
        public int entityID = -1;

        [Tooltip("生息域による条件（Noneで無視）")]
        public HabitationArea habitation = HabitationArea.None;

        [Tooltip("配置先水槽のインデックス")]
        public int targetAquariumIndex;

        public bool Matches(EntityData entity)
        {
            // EntityID指定がある場合はそれを優先
            if (entityID >= 0 && entity.ID != entityID)
                return false;

            // Habitation条件チェック
            if (habitation != HabitationArea.None && entity.Habitation != habitation)
                return false;

            return true;
        }
    }
}
