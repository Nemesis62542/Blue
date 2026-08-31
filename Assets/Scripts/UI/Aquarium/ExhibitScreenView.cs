using System;
using System.Collections.Generic;
using Blue.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Exhibit
{
    /// <summary>
    /// 展示画面の見た目。並べるだけで、可否の判断はしない
    /// </summary>
    public class ExhibitScreenView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private ExhibitEntryPanel entryPrefab;
        [SerializeField] private Transform entryParent;
        [SerializeField] private TMP_Text tankName;
        [SerializeField] private TMP_Text capacityLabel;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private Button closeButton;

        // 開くたびに作り直すと、行が多いときに毎回生成コストがかかる。
        // 作った行は使い回し、余った行は隠すだけにする
        private readonly List<ExhibitEntryPanel> entries = new List<ExhibitEntryPanel>();

        public event Action<EntityData> OnAdd;
        public event Action<EntityData> OnRemove;
        public event Action OnClose;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(() => OnClose?.Invoke());

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            root.alpha = visible ? 1f : 0f;
            root.interactable = visible;
            root.blocksRaycasts = visible;
        }

        /// <summary>
        /// 表示をモデルの状態に合わせる
        /// </summary>
        public void Refresh(ExhibitScreenModel model)
        {
            if (model == null) return;

            if (tankName != null)
            {
                tankName.text = model.TankData != null ? model.TankData.Name : string.Empty;
            }

            if (capacityLabel != null)
            {
                model.GetCapacity(out float used, out float total);
                capacityLabel.text = $"容量 {used:0.#} / {total:0.#}";
            }

            IReadOnlyList<ExhibitCandidate> candidates = model.Candidates;

            if (emptyLabel != null) emptyLabel.gameObject.SetActive(candidates.Count == 0);

            EnsureEntryCount(candidates.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                bool used_row = i < candidates.Count;
                entries[i].gameObject.SetActive(used_row);

                if (used_row) entries[i].Bind(candidates[i]);
            }
        }

        private void EnsureEntryCount(int required)
        {
            if (entryPrefab == null || entryParent == null) return;

            while (entries.Count < required)
            {
                ExhibitEntryPanel entry = Instantiate(entryPrefab, entryParent);

                entry.OnAdd += entity => OnAdd?.Invoke(entity);
                entry.OnRemove += entity => OnRemove?.Invoke(entity);

                entries.Add(entry);
            }
        }
    }
}
