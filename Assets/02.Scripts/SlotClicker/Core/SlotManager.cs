using System;
using UnityEngine;
using SlotClicker.Data;

namespace SlotClicker.Core
{
    public enum SlotOutcome
    {
        Loss,
        Draw,
        MiniWin,
        SmallWin,
        BigWin,
        Jackpot,
        MegaJackpot
    }

    [System.Serializable]
    public class SlotResult
    {
        public int[] Symbols; // 릴별 심볼 인덱스
        public SlotOutcome Outcome;
        public double BetAmount;
        public float RewardMultiplier;
        public double FinalReward;
        public bool IsWin => Outcome != SlotOutcome.Loss;
    }

    public class SlotManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private PlayerData _playerData;
        private GameConfig _config;

        // 상태
        private bool _isSpinning = false;
        public bool IsSpinning => _isSpinning;

        // 이벤트
        public event Action OnSpinStart;
        public event Action<SlotResult> OnSpinComplete;
        public event Action<int, int> OnReelStop; // reelIndex, symbolIndex

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            _playerData = gameManager.PlayerData;
            _config = gameManager.Config;

            Debug.Log("[SlotManager] Initialized");
        }

        /// <summary>
        /// 슬롯 스핀 시작
        /// </summary>
        public bool TrySpin(double betAmount)
        {
            if (_isSpinning)
            {
                Debug.LogWarning("[SlotManager] Already spinning!");
                return false;
            }

            if (betAmount <= 0)
            {
                Debug.LogWarning("[SlotManager] Invalid bet amount!");
                return false;
            }

            if (!_gameManager.Gold.CanAfford(betAmount))
            {
                Debug.LogWarning("[SlotManager] Not enough gold!");
                return false;
            }

            // 베팅액 차감
            if (!_gameManager.Gold.SpendGold(betAmount))
            {
                return false;
            }

            _playerData.totalSpins++;
            _isSpinning = true;
            OnSpinStart?.Invoke();

            // 결과 계산
            SlotResult result = CalculateResult(betAmount);

            // 애니메이션 후 결과 처리 (코루틴으로)
            StartCoroutine(SpinSequence(result));

            return true;
        }

        private System.Collections.IEnumerator SpinSequence(SlotResult result)
        {
            // 각 릴 정지 애니메이션
            for (int i = 0; i < result.Symbols.Length; i++)
            {
                yield return new WaitForSeconds(_config.reelStopDelay);
                OnReelStop?.Invoke(i, result.Symbols[i]);
            }

            // 최종 결과 처리
            yield return new WaitForSeconds(0.5f);

            // 보상 지급
            if (result.FinalReward > 0)
            {
                _gameManager.Gold.AddGold(result.FinalReward, false);

                // 잭팟 카운트
                if (result.Outcome == SlotOutcome.Jackpot)
                    _playerData.jackpotCount++;
                else if (result.Outcome == SlotOutcome.MegaJackpot)
                    _playerData.megaJackpotCount++;
            }

            _isSpinning = false;
            OnSpinComplete?.Invoke(result);

            Debug.Log($"[SlotManager] Spin complete: {result.Outcome}, Reward: {result.FinalReward:N0}");
        }

        /// <summary>
        /// 슬롯 결과 계산
        /// </summary>
        private SlotResult CalculateResult(double betAmount)
        {
            SlotResult result = new SlotResult
            {
                BetAmount = betAmount,
                Symbols = new int[_config.reelCount]
            };

            // 결과 확률 계산 (업그레이드 적용)
            SlotOutcome outcome = DetermineOutcome();
            result.Outcome = outcome;

            // 배당률 적용 (보상 배율 업그레이드 포함)
            result.RewardMultiplier = GetMultiplier(outcome);

            // 골드 부스트 적용
            float goldBoost = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.GoldBoost);
            result.FinalReward = betAmount * result.RewardMultiplier * goldBoost;

            // 심볼 생성 (결과에 맞게)
            GenerateSymbols(result);

            return result;
        }

        /// <summary>
        /// 확률 기반 결과 결정
        /// </summary>
        private SlotOutcome DetermineOutcome()
        {
            // 성공률 업그레이드 적용 (UpgradeManager에서)
            float successBonus = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.SlotSuccessRate);

            // 잭팟 확률 업그레이드
            float jackpotBonus = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.JackpotRate);

            // 조정된 확률
            float adjustedMegaJackpot = _config.megaJackpotRate * jackpotBonus;
            float adjustedJackpot = _config.jackpotRate * jackpotBonus;
            float adjustedBigWin = _config.bigWinRate * successBonus;
            float adjustedSmallWin = _config.smallWinRate * successBonus;
            float adjustedMiniWin = _config.miniWinRate * successBonus;
            float adjustedDraw = _config.drawRate;

            // 누적 확률
            float roll = UnityEngine.Random.Range(0f, 100f);
            float cumulative = 0f;

            cumulative += adjustedMegaJackpot;
            if (roll < cumulative) return SlotOutcome.MegaJackpot;

            cumulative += adjustedJackpot;
            if (roll < cumulative) return SlotOutcome.Jackpot;

            cumulative += adjustedBigWin;
            if (roll < cumulative) return SlotOutcome.BigWin;

            cumulative += adjustedSmallWin;
            if (roll < cumulative) return SlotOutcome.SmallWin;

            cumulative += adjustedMiniWin;
            if (roll < cumulative) return SlotOutcome.MiniWin;

            cumulative += adjustedDraw;
            if (roll < cumulative) return SlotOutcome.Draw;

            return SlotOutcome.Loss;
        }

        /// <summary>
        /// 결과별 배당률 (보상 배율 업그레이드 적용)
        /// </summary>
        private float GetMultiplier(SlotOutcome outcome)
        {
            float baseMultiplier = outcome switch
            {
                SlotOutcome.MegaJackpot => _config.megaJackpotMultiplier,
                SlotOutcome.Jackpot => _config.jackpotMultiplier,
                SlotOutcome.BigWin => _config.bigWinMultiplier,
                SlotOutcome.SmallWin => _config.smallWinMultiplier,
                SlotOutcome.MiniWin => _config.miniWinMultiplier,
                SlotOutcome.Draw => _config.drawMultiplier,
                _ => 0f
            };

            // 보상 배율 업그레이드 적용 (승리 시에만)
            if (outcome != SlotOutcome.Loss && outcome != SlotOutcome.Draw)
            {
                float rewardBonus = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.RewardMultiplier);
                baseMultiplier *= rewardBonus;
            }

            return baseMultiplier;
        }

        /// <summary>
        /// 결과에 맞는 심볼 배열 생성
        /// </summary>
        private void GenerateSymbols(SlotResult result)
        {
            int symbolCount = _config.symbolCount;

            switch (result.Outcome)
            {
                case SlotOutcome.MegaJackpot:
                    // 777 (특별 심볼)
                    int megaSymbol = symbolCount - 1; // 가장 높은 심볼
                    for (int i = 0; i < result.Symbols.Length; i++)
                        result.Symbols[i] = megaSymbol;
                    break;

                case SlotOutcome.Jackpot:
                    // 같은 심볼 3개
                    int jackpotSymbol = UnityEngine.Random.Range(symbolCount - 3, symbolCount);
                    for (int i = 0; i < result.Symbols.Length; i++)
                        result.Symbols[i] = jackpotSymbol;
                    break;

                case SlotOutcome.BigWin:
                    // 같은 심볼 3개 (낮은 등급)
                    int bigWinSymbol = UnityEngine.Random.Range(2, symbolCount - 2);
                    for (int i = 0; i < result.Symbols.Length; i++)
                        result.Symbols[i] = bigWinSymbol;
                    break;

                case SlotOutcome.SmallWin:
                    // 2개 일치 + 1개 다름
                    int smallWinSymbol = UnityEngine.Random.Range(1, symbolCount);
                    result.Symbols[0] = smallWinSymbol;
                    result.Symbols[1] = smallWinSymbol;
                    result.Symbols[2] = (smallWinSymbol + UnityEngine.Random.Range(1, symbolCount - 1)) % symbolCount;
                    break;

                case SlotOutcome.MiniWin:
                case SlotOutcome.Draw:
                    // 2개 일치
                    int matchSymbol = UnityEngine.Random.Range(0, symbolCount);
                    int pos = UnityEngine.Random.Range(0, 2);
                    result.Symbols[pos] = matchSymbol;
                    result.Symbols[(pos + 1) % 3] = matchSymbol;
                    result.Symbols[(pos + 2) % 3] = (matchSymbol + UnityEngine.Random.Range(1, symbolCount - 1)) % symbolCount;
                    break;

                default: // Loss
                    // 모두 다른 심볼
                    result.Symbols[0] = UnityEngine.Random.Range(0, symbolCount);
                    do { result.Symbols[1] = UnityEngine.Random.Range(0, symbolCount); }
                    while (result.Symbols[1] == result.Symbols[0]);
                    do { result.Symbols[2] = UnityEngine.Random.Range(0, symbolCount); }
                    while (result.Symbols[2] == result.Symbols[0] || result.Symbols[2] == result.Symbols[1]);
                    break;
            }
        }

        /// <summary>
        /// 업그레이드 레벨 갱신 (호환성 유지)
        /// </summary>
        public void RefreshUpgrades()
        {
            // UpgradeManager가 캐시를 관리하므로 별도 작업 불필요
        }

        /// <summary>
        /// 현재 잭팟 확률
        /// </summary>
        public float GetCurrentJackpotRate()
        {
            float jackpotBonus = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.JackpotRate);
            return _config.jackpotRate * jackpotBonus;
        }

        /// <summary>
        /// 현재 메가잭팟 확률
        /// </summary>
        public float GetCurrentMegaJackpotRate()
        {
            float jackpotBonus = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.JackpotRate);
            return _config.megaJackpotRate * jackpotBonus;
        }
    }
}
