using System;
using UnityEngine;
using SlotClicker.Data;

namespace SlotClicker.Core
{
    public class GoldManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private PlayerData _playerData;

        // 이벤트
        public event Action<double> OnGoldChanged;
        public event Action<double, bool> OnGoldEarned; // amount, isCritical
        public event Action<double> OnGoldSpent;

        public double CurrentGold => _playerData?.gold ?? 0;
        public double TotalEarned => _playerData?.totalGoldEarned ?? 0;
        public double TotalLost => _playerData?.totalGoldLost ?? 0;

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            _playerData = gameManager.PlayerData;
            Debug.Log($"[GoldManager] Initialized with {CurrentGold:N0} gold");
        }

        /// <summary>
        /// 골드 획득
        /// </summary>
        public void AddGold(double amount, bool isCritical = false)
        {
            if (amount <= 0) return;

            _playerData.gold += amount;
            _playerData.totalGoldEarned += amount;

            OnGoldEarned?.Invoke(amount, isCritical);
            OnGoldChanged?.Invoke(_playerData.gold);
            _gameManager.NotifyStateChanged();

            Debug.Log($"[GoldManager] +{amount:N0} gold (Critical: {isCritical}), Total: {_playerData.gold:N0}");
        }

        /// <summary>
        /// 골드 소비 (베팅, 업그레이드 등)
        /// </summary>
        public bool SpendGold(double amount)
        {
            if (amount <= 0) return false;
            if (_playerData.gold < amount) return false;

            _playerData.gold -= amount;
            _playerData.totalGoldLost += amount;

            OnGoldSpent?.Invoke(amount);
            OnGoldChanged?.Invoke(_playerData.gold);
            _gameManager.NotifyStateChanged();

            Debug.Log($"[GoldManager] -{amount:N0} gold, Remaining: {_playerData.gold:N0}");
            return true;
        }

        /// <summary>
        /// 골드 충분한지 확인
        /// </summary>
        public bool CanAfford(double amount)
        {
            return _playerData.gold >= amount;
        }

        /// <summary>
        /// 베팅 금액 계산 (비율 기반)
        /// </summary>
        public double CalculateBetAmount(float percentage)
        {
            double bet = _playerData.gold * percentage;
            double minBet = _gameManager.Config.minimumBet;

            // 최소 베팅액 보장
            if (bet < minBet && _playerData.gold >= minBet)
            {
                bet = minBet;
            }

            return Math.Floor(bet);
        }

        /// <summary>
        /// 포맷된 골드 문자열
        /// </summary>
        public string GetFormattedGold()
        {
            return FormatNumber(_playerData.gold);
        }

        public static string FormatNumber(double value)
        {
            if (value >= 1_000_000_000_000)
                return $"{value / 1_000_000_000_000:F2}T";
            if (value >= 1_000_000_000)
                return $"{value / 1_000_000_000:F2}B";
            if (value >= 1_000_000)
                return $"{value / 1_000_000:F2}M";
            if (value >= 1_000)
                return $"{value / 1_000:F1}K";
            return $"{value:N0}";
        }
    }
}
