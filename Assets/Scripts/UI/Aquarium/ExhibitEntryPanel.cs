using System;
using Blue.Aquarium;
using Blue.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Exhibit
{
    /// <summary>
    /// 展示画面の1行。生物ひとつ分の状態と出し入れのボタン
    /// </summary>
    public class ExhibitEntryPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text entityName;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private TMP_Text reasonLabel;
        [SerializeField] private Button addButton;
        [SerializeField] private Button removeButton;

        private EntityData entity;

        public event Action<EntityData> OnAdd;
        public event Action<EntityData> OnRemove;

        private void Awake()
        {
            if (addButton != null) addButton.onClick.AddListener(() => OnAdd?.Invoke(entity));
            if (removeButton != null) removeButton.onClick.AddListener(() => OnRemove?.Invoke(entity));
        }

        /// <summary>
        /// 表示を1行ぶんの状態に合わせる
        /// </summary>
        public void Bind(ExhibitCandidate candidate)
        {
            entity = candidate.Entity;

            if (entityName != null) entityName.text = candidate.Entity.Name;

            if (countLabel != null)
            {
                // 所持数のうち何匹をこの水槽に入れているか。館全体の展示数も出さないと
                // 「持っているのに入れられない」理由が読み取れない
                countLabel.text = candidate.Owned == int.MaxValue
                    ? $"水槽 {candidate.ExhibitedHere}"
                    : $"水槽 {candidate.ExhibitedHere} ／ 展示 {candidate.ExhibitedTotal} ／ 所持 {candidate.Owned}";
            }

            if (reasonLabel != null)
            {
                reasonLabel.text = candidate.CanAdd ? string.Empty : ExhibitRule.Describe(candidate.Rejection);
            }

            if (addButton != null) addButton.interactable = candidate.CanAdd;
            if (removeButton != null) removeButton.interactable = candidate.CanRemove;
        }
    }
}
