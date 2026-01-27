using System;
using System.Collections.Generic;
using UnityEngine;
using SlotClicker.Data;

namespace SlotClicker.Core
{
    public class UpgradeManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private PlayerData _playerData;
        private UpgradeDatabase _database;

        // 효과 캐시
        private Dictionary<UpgradeEffect, float> _effectCache = new Dictionary<UpgradeEffect, float>();

        // 자동 수집 관련
        private float _autoCollectTimer = 0f;
        private const float AUTO_COLLECT_INTERVAL = 1f;

        // 이자 처리 관련 (배칭으로 성능 최적화)
        private float _interestTimer = 0f;
        private const float INTEREST_INTERVAL = 0.5f;

        // 이벤트
        public event Action<string, int> OnUpgradePurchased; // upgradeId, newLevel
        public event Action OnUpgradesChanged;

        public UpgradeDatabase Database => _database;

        public void Initialize(GameManager gameManager, UpgradeDatabase database = null)
        {
            _gameManager = gameManager;
            _playerData = gameManager.PlayerData;

            // 데이터베이스 설정
            if (database != null)
            {
                _database = database;
            }
            else
            {
                // 기본 데이터베이스 생성
                _database = ScriptableObject.CreateInstance<UpgradeDatabase>();
                _database.CreateDefaultUpgrades();
            }

            // 효과 캐시 초기화
            RefreshEffectCache();

            Debug.Log("[UpgradeManager] Initialized");
        }

        private void Update()
        {
            // 자동 수집 처리
            ProcessAutoCollect();

            // 이자 처리
            ProcessInterest();
        }

        #region 업그레이드 구매

        /// <summary>
        /// 업그레이드 구매 시도
        /// </summary>
        public bool TryPurchase(string upgradeId)
        {
            UpgradeData upgrade = _database.GetUpgrade(upgradeId);
            if (upgrade == null)
            {
                Debug.LogWarning($"[UpgradeManager] Upgrade not found: {upgradeId}");
                return false;
            }

            int currentLevel = GetLevel(upgradeId);

            // 최대 레벨 체크
            if (upgrade.IsMaxLevel(currentLevel))
            {
                Debug.Log($"[UpgradeManager] {upgradeId} is at max level");
                return false;
            }

            // 비용 계산
            double cost = upgrade.GetCost(currentLevel);

            // 골드 충분한지 체크
            if (!_gameManager.Gold.CanAfford(cost))
            {
                Debug.Log($"[UpgradeManager] Not enough gold for {upgradeId}");
                return false;
            }

            // 구매 처리
            if (_gameManager.Gold.SpendGold(cost))
            {
                int newLevel = currentLevel + 1;
                _playerData.SetUpgradeLevel(upgradeId, newLevel);

                // 효과 캐시 갱신
                RefreshEffectCache();

                // 다른 매니저에 알림
                NotifyManagers();

                OnUpgradePurchased?.Invoke(upgradeId, newLevel);
                OnUpgradesChanged?.Invoke();

                Debug.Log($"[UpgradeManager] Purchased {upgradeId} -> Lv.{newLevel}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 여러 레벨 한번에 구매
        /// </summary>
        public int PurchaseMultiple(string upgradeId, int count)
        {
            int purchased = 0;
            for (int i = 0; i < count; i++)
            {
                if (TryPurchase(upgradeId))
                    purchased++;
                else
                    break;
            }
            return purchased;
        }

        /// <summary>
        /// 구매 가능한 최대 레벨까지 구매
        /// </summary>
        public int PurchaseMax(string upgradeId)
        {
            int purchased = 0;
            while (TryPurchase(upgradeId))
            {
                purchased++;
                if (purchased > 1000) break; // 안전장치
            }
            return purchased;
        }

        #endregion

        #region 레벨 및 비용 조회

        public int GetLevel(string upgradeId)
        {
            return _playerData.GetUpgradeLevel(upgradeId);
        }

        public double GetCost(string upgradeId)
        {
            UpgradeData upgrade = _database.GetUpgrade(upgradeId);
            if (upgrade == null) return 0;
            return upgrade.GetCost(GetLevel(upgradeId));
        }

        public double GetNextCost(string upgradeId, int levels = 1)
        {
            UpgradeData upgrade = _database.GetUpgrade(upgradeId);
            if (upgrade == null) return 0;

            double total = 0;
            int currentLevel = GetLevel(upgradeId);
            for (int i = 0; i < levels; i++)
            {
                if (upgrade.IsMaxLevel(currentLevel + i)) break;
                total += upgrade.GetCost(currentLevel + i);
            }
            return total;
        }

        public bool CanAfford(string upgradeId)
        {
            return _gameManager.Gold.CanAfford(GetCost(upgradeId));
        }

        public bool IsMaxLevel(string upgradeId)
        {
            UpgradeData upgrade = _database.GetUpgrade(upgradeId);
            if (upgrade == null) return true;
            return upgrade.IsMaxLevel(GetLevel(upgradeId));
        }

        #endregion

        #region 효과 계산

        /// <summary>
        /// 효과 캐시 갱신
        /// </summary>
        public void RefreshEffectCache()
        {
            _effectCache.Clear();

            // 모든 업그레이드의 효과 계산
            foreach (var upgrade in _database.clickUpgrades)
                CacheEffect(upgrade);
            foreach (var upgrade in _database.slotUpgrades)
                CacheEffect(upgrade);
            foreach (var upgrade in _database.goldUpgrades)
                CacheEffect(upgrade);
        }

        private void CacheEffect(UpgradeData upgrade)
        {
            int level = GetLevel(upgrade.id);
            float value = upgrade.GetEffectValue(level);

            if (_effectCache.ContainsKey(upgrade.effect))
                _effectCache[upgrade.effect] += value;
            else
                _effectCache[upgrade.effect] = value;
        }

        /// <summary>
        /// 특정 효과의 총 값 가져오기
        /// </summary>
        public float GetEffectValue(UpgradeEffect effect)
        {
            return _effectCache.TryGetValue(effect, out float value) ? value : 0f;
        }

        /// <summary>
        /// 특정 효과의 배율 가져오기
        /// - ClickPower: 지수적 증가 (레벨당 1.12배 복리)
        /// - 기타: 선형 증가 (1 + value/100)
        /// </summary>
        public float GetEffectMultiplier(UpgradeEffect effect)
        {
            // ClickPower는 지수적 증가 적용 (클리커 게임다운 성장감)
            if (effect == UpgradeEffect.ClickPower)
            {
                int clickPowerLevel = GetClickPowerLevel();
                if (clickPowerLevel <= 0) return 1f;

                // 레벨당 12% 복리 증가: 1.12^level
                // 레벨 10: 3.1배, 레벨 20: 9.6배, 레벨 50: 289배
                return (float)Math.Pow(1.12, clickPowerLevel);
            }

            return 1f + (GetEffectValue(effect) / 100f);
        }

        /// <summary>
        /// 클릭 파워 업그레이드 레벨 가져오기
        /// </summary>
        public int GetClickPowerLevel()
        {
            return GetLevel("click_power");
        }

        private void NotifyManagers()
        {
            _gameManager.Click?.RefreshUpgrades();
            _gameManager.Slot?.RefreshUpgrades();
        }

        #endregion

        #region 자동 수집 & 이자

        private void ProcessAutoCollect()
        {
            float autoCollectLevel = GetEffectValue(UpgradeEffect.AutoCollect);
            if (autoCollectLevel <= 0) return;

            _autoCollectTimer += Time.deltaTime;
            if (_autoCollectTimer >= AUTO_COLLECT_INTERVAL)
            {
                _autoCollectTimer = 0f;

                // 자동 수집량 계산
                double baseAmount = autoCollectLevel;
                double goldBoost = GetEffectMultiplier(UpgradeEffect.GoldBoost);
                double prestigeBonus = _gameManager.GetPrestigeBonus();

                // 럭키참 보너스 적용
                double charmBonus = _gameManager.Prestige?.GetAutoCollectMultiplier() ?? 1f;

                double autoGold = baseAmount * goldBoost * prestigeBonus * charmBonus;
                _gameManager.Gold.AddGold(autoGold, false);
            }
        }

        private void ProcessInterest()
        {
            float interestRate = GetEffectValue(UpgradeEffect.Interest);
            if (interestRate <= 0) return;

            _interestTimer += Time.deltaTime;
            if (_interestTimer < INTEREST_INTERVAL) return;

            // 배칭: 0.5초마다 계산 (프레임별 계산 대신 성능 최적화)
            float elapsedTime = _interestTimer;
            _interestTimer = 0f;

            // 럭키참 보너스 적용
            float charmBonus = _gameManager.Prestige?.GetInterestMultiplier() ?? 1f;
            float adjustedRate = interestRate * charmBonus;

            // 이자 계산 (배칭된 시간 적용)
            double currentGold = _gameManager.Gold.CurrentGold;
            double interest = currentGold * (adjustedRate / 100f) * elapsedTime;

            if (interest > 0.01) // 최소 이자
            {
                _gameManager.Gold.AddGold(interest, false);
            }
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 모든 업그레이드 정보 가져오기
        /// </summary>
        public List<UpgradeInfo> GetAllUpgradeInfo(UpgradeCategory category)
        {
            List<UpgradeInfo> infos = new List<UpgradeInfo>();

            UpgradeData[] upgrades = _database.GetUpgradesByCategory(category);
            foreach (var upgrade in upgrades)
            {
                infos.Add(new UpgradeInfo
                {
                    Data = upgrade,
                    CurrentLevel = GetLevel(upgrade.id),
                    CurrentCost = GetCost(upgrade.id),
                    CanAfford = CanAfford(upgrade.id),
                    IsMaxLevel = IsMaxLevel(upgrade.id)
                });
            }

            return infos;
        }

        #endregion
    }

    /// <summary>
    /// UI 표시용 업그레이드 정보
    /// </summary>
    public struct UpgradeInfo
    {
        public UpgradeData Data;
        public int CurrentLevel;
        public double CurrentCost;
        public bool CanAfford;
        public bool IsMaxLevel;
    }
}
