using System;
using UnityEngine;
using SlotClicker.Data;

namespace SlotClicker.Core
{
    public class ClickManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private PlayerData _playerData;
        private GameConfig _config;

        // 이벤트
        public event Action<ClickResult> OnClick;

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            _playerData = gameManager.PlayerData;
            _config = gameManager.Config;

            Debug.Log("[ClickManager] Initialized");
        }

        /// <summary>
        /// 클릭 처리
        /// </summary>
        public ClickResult ProcessClick(Vector2 clickPosition)
        {
            _playerData.totalClicks++;

            // 기본 클릭 파워 계산
            double basePower = _config.baseClickPower;

            // 클릭 파워 업그레이드 적용 (UpgradeManager에서 가져옴)
            double upgradeMultiplier = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.ClickPower);

            // 프레스티지 보너스 적용
            double prestigeBonus = _gameManager.GetPrestigeBonus();

            // 골드 부스트 적용
            double goldBoost = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.GoldBoost);

            // 최종 기본 골드
            double baseGold = basePower * upgradeMultiplier * prestigeBonus * goldBoost;

            // 크리티컬 판정
            bool isCritical = CheckCritical();
            double finalGold = baseGold;

            if (isCritical)
            {
                float critMultiplier = GetCriticalMultiplier();
                finalGold = baseGold * critMultiplier;
            }

            // 골드 지급
            _gameManager.Gold.AddGold(finalGold, isCritical);

            // 결과 생성
            ClickResult result = new ClickResult
            {
                Position = clickPosition,
                GoldEarned = finalGold,
                IsCritical = isCritical,
                CriticalMultiplier = isCritical ? GetCriticalMultiplier() : 1f
            };

            OnClick?.Invoke(result);
            return result;
        }

        /// <summary>
        /// 크리티컬 판정
        /// </summary>
        private bool CheckCritical()
        {
            float critChance = GetCurrentCriticalChance();
            return UnityEngine.Random.value < critChance;
        }

        /// <summary>
        /// 크리티컬 배율 계산
        /// </summary>
        private float GetCriticalMultiplier()
        {
            // 기본 배율 + 업그레이드 효과
            float baseMultiplier = _config.criticalMultiplier;
            float upgradeBonus = _gameManager.Upgrade.GetEffectValue(UpgradeEffect.CriticalMultiplier);
            return baseMultiplier + upgradeBonus;
        }

        /// <summary>
        /// 현재 클릭당 예상 골드
        /// </summary>
        public double GetExpectedGoldPerClick()
        {
            double basePower = _config.baseClickPower;
            double upgradeMultiplier = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.ClickPower);
            double prestigeBonus = _gameManager.GetPrestigeBonus();
            double goldBoost = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.GoldBoost);
            return basePower * upgradeMultiplier * prestigeBonus * goldBoost;
        }

        /// <summary>
        /// 현재 크리티컬 확률
        /// </summary>
        public float GetCurrentCriticalChance()
        {
            // 기본 확률 + 업그레이드 효과 (%)
            float baseChance = _config.criticalChance;
            float upgradeBonus = _gameManager.Upgrade.GetEffectValue(UpgradeEffect.CriticalChance) / 100f;
            return Mathf.Min(baseChance + upgradeBonus, 0.5f); // 최대 50%
        }

        /// <summary>
        /// 업그레이드 레벨 갱신 (호환성 유지)
        /// </summary>
        public void RefreshUpgrades()
        {
            // UpgradeManager가 캐시를 관리하므로 별도 작업 불필요
        }
    }

    [System.Serializable]
    public struct ClickResult
    {
        public Vector2 Position;
        public double GoldEarned;
        public bool IsCritical;
        public float CriticalMultiplier;
    }
}
