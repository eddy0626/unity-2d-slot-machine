using UnityEngine;

namespace SlotClicker.Core
{
    /// <summary>
    /// 사운드 시스템과 게임 매니저들을 연결하는 컴포넌트
    /// GameManager에 부착하여 이벤트 기반으로 사운드 재생
    /// </summary>
    public class SoundConnector : MonoBehaviour
    {
        private GameManager _gameManager;
        private bool _isSpinning = false;

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            SubscribeToEvents();
            Debug.Log("[SoundConnector] Initialized");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #region 이벤트 구독

        private void SubscribeToEvents()
        {
            if (_gameManager == null) return;

            // ClickManager 이벤트
            if (_gameManager.Click != null)
            {
                _gameManager.Click.OnClick += OnClick;
            }

            // SlotManager 이벤트
            if (_gameManager.Slot != null)
            {
                _gameManager.Slot.OnSpinStart += OnSpinStart;
                _gameManager.Slot.OnSpinComplete += OnSpinComplete;
                _gameManager.Slot.OnReelStop += OnReelStop;
            }

            // UpgradeManager 이벤트
            if (_gameManager.Upgrade != null)
            {
                _gameManager.Upgrade.OnUpgradePurchased += OnUpgradePurchased;
            }

            // PrestigeManager 이벤트
            if (_gameManager.Prestige != null)
            {
                _gameManager.Prestige.OnPrestigeComplete += OnPrestigeComplete;
                _gameManager.Prestige.OnCharmPurchased += OnCharmPurchased;
                _gameManager.Prestige.OnVIPRankChanged += OnVIPRankChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameManager == null) return;

            if (_gameManager.Click != null)
            {
                _gameManager.Click.OnClick -= OnClick;
            }

            if (_gameManager.Slot != null)
            {
                _gameManager.Slot.OnSpinStart -= OnSpinStart;
                _gameManager.Slot.OnSpinComplete -= OnSpinComplete;
                _gameManager.Slot.OnReelStop -= OnReelStop;
            }

            if (_gameManager.Upgrade != null)
            {
                _gameManager.Upgrade.OnUpgradePurchased -= OnUpgradePurchased;
            }

            if (_gameManager.Prestige != null)
            {
                _gameManager.Prestige.OnPrestigeComplete -= OnPrestigeComplete;
                _gameManager.Prestige.OnCharmPurchased -= OnCharmPurchased;
                _gameManager.Prestige.OnVIPRankChanged -= OnVIPRankChanged;
            }
        }

        #endregion

        #region 클릭 사운드

        private void OnClick(ClickResult result)
        {
            if (SoundManager.Instance == null) return;

            if (result.IsCritical)
            {
                SoundManager.Instance.PlaySFX(SoundType.ClickCritical);
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundType.ClickNormal);
            }
        }

        #endregion

        #region 슬롯 사운드

        private void OnSpinStart()
        {
            if (SoundManager.Instance == null) return;

            _isSpinning = true;

            // 레버 클릭 사운드
            SoundManager.Instance.PlaySFX(SoundType.LeverClick);

            // 약간의 딜레이 후 스핀 시작 사운드
            Invoke(nameof(PlaySpinStartSound), 0.1f);
        }

        private void PlaySpinStartSound()
        {
            if (SoundManager.Instance == null) return;

            SoundManager.Instance.PlaySFX(SoundType.SpinStart);

            // 스핀 루프 시작
            SoundManager.Instance.PlayLoopSFX(SoundType.SpinLoop, 0.6f);
        }

        private void OnReelStop(int reelIndex, int symbolIndex)
        {
            if (SoundManager.Instance == null) return;

            // 릴 정지 사운드 (열이 정지할 때마다)
            // reelIndex는 0-8이므로, 열(0, 1, 2)별로 한 번만 재생
            if (reelIndex == 0 || reelIndex == 1 || reelIndex == 2)
            {
                SoundManager.Instance.PlaySFX(SoundType.ReelStop, 0.8f);
            }
        }

        private void OnSpinComplete(SlotResult result)
        {
            if (SoundManager.Instance == null) return;

            _isSpinning = false;

            // 루프 사운드 정지
            SoundManager.Instance.StopLoopSFX();

            // 결과 사운드 재생
            SoundManager.Instance.PlaySlotResultSound(result.Outcome);
        }

        #endregion

        #region 업그레이드 사운드

        private void OnUpgradePurchased(string upgradeId, int newLevel)
        {
            if (SoundManager.Instance == null) return;

            // 최대 레벨 도달 시 특별 사운드
            if (_gameManager.Upgrade.IsMaxLevel(upgradeId))
            {
                SoundManager.Instance.PlaySFX(SoundType.UpgradeMax);
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundType.UpgradePurchase);
            }
        }

        #endregion

        #region 프레스티지 사운드

        private void OnPrestigeComplete()
        {
            if (SoundManager.Instance == null) return;

            SoundManager.Instance.PlaySFX(SoundType.PrestigeConfirm);

            // 칩 획득 사운드 (딜레이)
            Invoke(nameof(PlayChipEarnSound), 0.5f);
        }

        private void PlayChipEarnSound()
        {
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySFX(SoundType.ChipEarn);
        }

        private void OnCharmPurchased(string charmId)
        {
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySFX(SoundType.CharmUnlock);
        }

        private void OnVIPRankChanged(VIPRank newRank)
        {
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySFX(SoundType.VIPLevelUp);
        }

        #endregion
    }
}
