using System;
using System.Collections.Generic;
using Blue.Audio;
using Blue.Recipe;
using Blue.Upgrade;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI.Garage.Customize
{
    public class CustomizeScreenView : MonoBehaviour
    {
        [SerializeField] private List<UpgradePanel> upgradePanels;
        [SerializeField] private TMP_Text upgradeName;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text currentEffect;
        [SerializeField] private TMP_Text nextEffect;
        [SerializeField] private TMP_Text requireItems;
        [SerializeField] private Slider progressGauge;
        [SerializeField] private CanvasGroup upgradeCompleteNotification;
        [SerializeField] private float notificationDisplayDuration = 2f;
        [SerializeField] private int blinkCount = 2;
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("Screen Transition")]
        [SerializeField] private CanvasGroup screenTransitionPanel;
        [SerializeField] private int screenBlinkCount = 3;
        [SerializeField] private float screenBlinkInterval = 0.08f;

        [Header("Sub Upgrade")]
        [SerializeField] private List<SubUpgradePanel> subUpgradePanels;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Button equipToggleButton;
        [SerializeField] private TMP_Text equipToggleButtonText;

        private CustomizeScreenModel model;
        private Tween notificationTween;
        private Tween screenTransitionTween;
        private UpgradeData currentUpgrade;
        private bool isPointerPressed;

        private SubUpgradeData currentSubUpgrade;
        private List<SubUpgradeData> allSubUpgrades;
        private UpgradeData subCapacityUpgrade;

        public Action<UpgradeData> OnConfirmUpgrade;
        public Action<SubUpgradeData> OnConfirmUnlock;
        public Action<SubUpgradeData> OnToggleEquip;

        public void Initialize(
            List<UpgradeData> upgrades,
            CustomizeScreenModel customizeModel,
            Action<UpgradeData> upgradeCallback)
        {
            model = customizeModel;
            OnConfirmUpgrade = upgradeCallback;

            for (int i = 0; i < upgradePanels.Count && i < upgrades.Count; i++)
            {
                UpgradePanel panel = upgradePanels[i];
                UpgradeData upgrade = upgrades[i];

                panel.Initialize(upgrade);
                panel.OnPointerEnter += data => SetUpgradeInformation(data);
                panel.OnPointerDown += OnPanelPointerDown;
                panel.OnPointerUp += OnPanelPointerUp;
            }

            ClearDisplay();
            RefreshDisplay();
            PlayScreenOnAnimation();
        }

        private void Update()
        {
            UpdateProgressGauge();
        }

        private void UpdateProgressGauge()
        {
            if (progressGauge == null) return;

            if (isPointerPressed && currentUpgrade != null && model.CanUpgrade(currentUpgrade))
            {
                if (!SoundController.Instance.IsLoopSEPlaying)
                {
                    SoundController.Instance.PlayLoopSE(SEType.CraftCharge);
                }

                progressGauge.value += Time.deltaTime;
                if (progressGauge.value >= 1f)
                {
                    progressGauge.value = 0f;
                    SoundController.Instance.StopLoopSE();
                    OnConfirmUpgrade?.Invoke(currentUpgrade);
                    SoundController.Instance.PlaySE(SEType.CraftSuccess);
                    ShowUpgradeCompleteNotification();
                }
            }
            else
            {
                progressGauge.value = 0f;
            }
        }

        private void OnPanelPointerDown(UpgradeData upgrade)
        {
            currentUpgrade = upgrade;
            isPointerPressed = true;

            if (!model.CanUpgrade(upgrade))
            {
                SoundController.Instance.PlaySE(SEType.CraftFailed);
            }
        }

        private void OnPanelPointerUp(UpgradeData upgrade)
        {
            isPointerPressed = false;
            SoundController.Instance.StopLoopSE();
        }

        public void RefreshDisplay()
        {
            // 各パネルのレベル表示を更新
            foreach (UpgradePanel panel in upgradePanels)
            {
                if (panel.UpgradeData != null)
                {
                    panel.SetLevel(model.GetCurrentLevel(panel.UpgradeData.UpgradeType));
                }
            }

            if (currentUpgrade != null)
            {
                SetUpgradeInformation(currentUpgrade, forceRefresh: true);
            }
        }

        private void ClearDisplay()
        {
            currentUpgrade = null;

            if (upgradeName != null) upgradeName.text = "";
            if (description != null) description.text = "";
            if (currentEffect != null) currentEffect.text = "";
            if (nextEffect != null) nextEffect.text = "";
            if (requireItems != null) requireItems.text = "";
            if (progressGauge != null) progressGauge.value = 0f;
        }

        private void SetUpgradeInformation(UpgradeData upgrade, bool forceRefresh = false)
        {
            if (!forceRefresh && currentUpgrade == upgrade) return;

            currentUpgrade = upgrade;
            int level = model.GetCurrentLevel(upgrade.UpgradeType);
            bool isMaxLevel = model.IsMaxLevel(upgrade);
            string unit = GetUnitString(upgrade.UpgradeType);

            if (upgradeName != null) upgradeName.text = upgrade.UpgradeName;
            if (description != null) description.text = upgrade.Description;

            if (currentEffect != null)
            {
                int effectValue = upgrade.GetEffectValue(level);
                currentEffect.text = $"現在: {effectValue}{unit}";
            }

            if (!isMaxLevel)
            {
                UpgradeLevelData nextLevelData = upgrade.GetLevelData(level);
                if (nextEffect != null)
                {
                    nextEffect.text = $"次: {nextLevelData.EffectValue}{unit}";
                }
                if (requireItems != null)
                {
                    requireItems.text = GenerateRequireItemText(nextLevelData.RequiredResources);
                }
            }
            else
            {
                if (nextEffect != null) nextEffect.text = "最大レベル";
                if (requireItems != null) requireItems.text = "";
            }
        }

        private string GetUnitString(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Oxygen => "秒",
                UpgradeType.Depth => "m",
                UpgradeType.SubCapacity => "",
                _ => ""
            };
        }

        private string GenerateRequireItemText(List<RequireItemData> requires)
        {
            if (requires == null) return "";

            string result = "";
            foreach (RequireItemData require in requires)
            {
                bool hasEnough = model.CheckEnoughResource(require.Item, require.Count);
                string color = hasEnough ? "#FF9600" : "red";
                result += $"<color={color}>{require.Item.Name} x {require.Count}</color>\n";
            }
            return result;
        }

        private void ShowUpgradeCompleteNotification()
        {
            if (upgradeCompleteNotification == null) return;

            notificationTween?.Kill();

            upgradeCompleteNotification.gameObject.SetActive(true);
            upgradeCompleteNotification.alpha = 1f;

            notificationTween = DOTween.Sequence()
                .Append(upgradeCompleteNotification.DOFade(0.2f, blinkInterval)
                    .SetLoops(blinkCount * 2, LoopType.Yoyo))
                .AppendInterval(notificationDisplayDuration)
                .Append(upgradeCompleteNotification.DOFade(0f, 0.05f))
                .OnComplete(() => upgradeCompleteNotification.gameObject.SetActive(false));
        }

        private void PlayScreenOnAnimation()
        {
            if (screenTransitionPanel == null) return;

            screenTransitionTween?.Kill();

            screenTransitionPanel.gameObject.SetActive(true);
            screenTransitionPanel.alpha = 1f;

            screenTransitionTween = DOTween.Sequence()
                .Append(screenTransitionPanel.DOFade(0f, screenBlinkInterval)
                    .SetLoops(screenBlinkCount * 2, LoopType.Yoyo))
                .Append(screenTransitionPanel.DOFade(0f, screenBlinkInterval))
                .OnComplete(() => screenTransitionPanel.gameObject.SetActive(false));
        }

        public void ShowScreenOffPanel()
        {
            if (screenTransitionPanel == null) return;

            screenTransitionTween?.Kill();
            screenTransitionPanel.gameObject.SetActive(true);
            screenTransitionPanel.alpha = 1f;
        }

        #region サブアップグレード

        public void InitializeSubUpgrades(
            List<SubUpgradeData> subUpgrades,
            UpgradeData capacityUpgrade,
            Action<SubUpgradeData> unlockCallback,
            Action<SubUpgradeData> equipCallback)
        {
            allSubUpgrades = subUpgrades;
            subCapacityUpgrade = capacityUpgrade;
            OnConfirmUnlock = unlockCallback;
            OnToggleEquip = equipCallback;

            for (int i = 0; i < subUpgradePanels.Count && i < subUpgrades.Count; i++)
            {
                SubUpgradePanel panel = subUpgradePanels[i];
                SubUpgradeData subUpgrade = subUpgrades[i];

                panel.Initialize(subUpgrade);
                panel.OnPointerEnter += SetSubUpgradeInformation;
                panel.OnPointerDown += OnSubPanelPointerDown;
                panel.OnPointerUp += OnSubPanelPointerUp;
            }

            if (unlockButton != null)
            {
                unlockButton.onClick.AddListener(OnUnlockButtonClicked);
            }
            if (equipToggleButton != null)
            {
                equipToggleButton.onClick.AddListener(OnEquipToggleButtonClicked);
            }

            RefreshSubUpgradeDisplay();
        }

        public void RefreshSubUpgradeDisplay()
        {
            foreach (SubUpgradePanel panel in subUpgradePanels)
            {
                if (panel.SubUpgradeData != null)
                {
                    bool unlocked = model.IsSubUpgradeUnlocked(panel.SubUpgradeData);
                    bool equipped = model.IsSubUpgradeEquipped(panel.SubUpgradeData);
                    panel.UpdateState(unlocked, equipped);
                }
            }

            UpdateCapacityDisplay();

            if (currentSubUpgrade != null)
            {
                SetSubUpgradeInformation(currentSubUpgrade, forceRefresh: true);
            }
        }

        private void UpdateCapacityDisplay()
        {
            if (capacityText != null && allSubUpgrades != null)
            {
                int used = model.GetCurrentCapacityUsed(allSubUpgrades);
                int max = model.GetMaxSubUpgradeCapacity(subCapacityUpgrade);
                capacityText.text = $"{used}/{max}";
            }
        }

        private void SetSubUpgradeInformation(SubUpgradeData subUpgrade, bool forceRefresh = false)
        {
            if (!forceRefresh && currentSubUpgrade == subUpgrade) return;

            currentSubUpgrade = subUpgrade;
            currentUpgrade = null;

            bool isUnlocked = model.IsSubUpgradeUnlocked(subUpgrade);
            bool isEquipped = model.IsSubUpgradeEquipped(subUpgrade);

            if (upgradeName != null) upgradeName.text = subUpgrade.UpgradeName;
            if (description != null) description.text = subUpgrade.Description;

            if (currentEffect != null) currentEffect.text = "";
            if (nextEffect != null) nextEffect.text = "";

            UpdateSubUpgradeButtons(subUpgrade, isUnlocked, isEquipped);

            if (!isUnlocked)
            {
                if (requireItems != null)
                {
                    requireItems.text = GenerateRequireItemText(subUpgrade.RequiredResources);
                }
            }
            else
            {
                if (requireItems != null) requireItems.text = "";
            }
        }

        private void SetSubUpgradeInformation(SubUpgradeData subUpgrade)
        {
            SetSubUpgradeInformation(subUpgrade, forceRefresh: false);
        }

        private void UpdateSubUpgradeButtons(SubUpgradeData subUpgrade, bool isUnlocked, bool isEquipped)
        {
            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(!isUnlocked);
                unlockButton.interactable = model.CanUnlockSubUpgrade(subUpgrade);
            }

            if (equipToggleButton != null)
            {
                equipToggleButton.gameObject.SetActive(isUnlocked);

                if (isEquipped)
                {
                    if (equipToggleButtonText != null) equipToggleButtonText.text = "解除";
                    equipToggleButton.interactable = true;
                }
                else
                {
                    if (equipToggleButtonText != null) equipToggleButtonText.text = "装備";
                    equipToggleButton.interactable = model.CanEquipSubUpgrade(subUpgrade, subCapacityUpgrade, allSubUpgrades);
                }
            }
        }

        private void OnSubPanelPointerDown(SubUpgradeData subUpgrade)
        {
            currentSubUpgrade = subUpgrade;
        }

        private void OnSubPanelPointerUp(SubUpgradeData subUpgrade)
        {
        }

        private void OnUnlockButtonClicked()
        {
            if (currentSubUpgrade != null)
            {
                OnConfirmUnlock?.Invoke(currentSubUpgrade);
            }
        }

        private void OnEquipToggleButtonClicked()
        {
            if (currentSubUpgrade != null)
            {
                OnToggleEquip?.Invoke(currentSubUpgrade);
            }
        }

        #endregion

        private void OnDestroy()
        {
            notificationTween?.Kill();
            screenTransitionTween?.Kill();
        }
    }
}
