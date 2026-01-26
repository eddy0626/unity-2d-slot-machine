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

        // 업그레이드 효과
        private int _clickPowerLevel = 0;
        private int _criticalChanceLevel = 0;
        private int _criticalMultiplierLevel = 0;

        // 이벤트
        public event Action<ClickResult> OnClick;

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            _playerData = gameManager.PlayerData;
            _config = gameManager.Config;

            // 업그레이드 레벨 로드
            LoadUpgradeLevels();

            Debug.Log("[ClickManager] Initialized");
        }

        private void LoadUpgradeLevels()
        {
            _clickPowerLevel = _playerData.GetUpgradeLevel("click_power");
            _criticalChanceLevel = _playerData.GetUpgradeLevel("critical_chance");
            _criticalMultiplierLevel = _playerData.GetUpgradeLevel("critical_multiplier");
        }

        /// <summary>
        /// 클릭 처리
        /// </summary>
        public ClickResult ProcessClick(Vector2 clickPosition)
        {
            _playerData.totalClicks++;

            // 기본 클릭 파워 계산
            double basePower = _config.baseClickPower;

            // 클릭 파워 업그레이드 적용 (레벨당 +50%)
            double upgradeMultiplier = 1 + (_clickPowerLevel * 0.5);

            // 프레스티지 보너스 적용
            double prestigeBonus = _gameManager.GetPrestigeBonus();

            // 최종 기본 골드
            double baseGold = basePower * upgradeMultiplier * prestigeBonus;

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
            float critChance = _config.criticalChance + (_criticalChanceLevel * 0.01f);
            critChance = Mathf.Min(critChance, 0.5f); // 최대 50%
            return UnityEngine.Random.value < critChance;
        }

        /// <summary>
        /// 크리티컬 배율 계산
        /// </summary>
        private float GetCriticalMultiplier()
        {
            return _config.criticalMultiplier + (_criticalMultiplierLevel * 0.5f);
        }

        /// <summary>
        /// 현재 클릭당 예상 골드
        /// </summary>
        public double GetExpectedGoldPerClick()
        {
            double basePower = _config.baseClickPower;
            double upgradeMultiplier = 1 + (_clickPowerLevel * 0.5);
            double prestigeBonus = _gameManager.GetPrestigeBonus();
            return basePower * upgradeMultiplier * prestigeBonus;
        }

        /// <summary>
        /// 현재 크리티컬 확률
        /// </summary>
        public float GetCurrentCriticalChance()
        {
            return Mathf.Min(_config.criticalChance + (_criticalChanceLevel * 0.01f), 0.5f);
        }

        /// <summary>
        /// 업그레이드 레벨 갱신
        /// </summary>
        public void RefreshUpgrades()
        {
            LoadUpgradeLevels();
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
