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
        public int[] Symbols; // 3x3 그리드 심볼 (9개: 0-2 상단, 3-5 중간, 6-8 하단)
        public SlotOutcome Outcome;
        public double BetAmount;
        public float RewardMultiplier;
        public double FinalReward;
        public int[] WinningPayline; // 당첨 페이라인 인덱스들
        public bool IsWin => Outcome != SlotOutcome.Loss;
    }

    /// <summary>
    /// 3x3 슬롯 페이라인 정의
    /// </summary>
    public static class SlotPaylines
    {
        // 5개 페이라인 (인덱스 기준)
        // 0 1 2 (상단)
        // 3 4 5 (중간)
        // 6 7 8 (하단)
        public static readonly int[][] Lines = new int[][]
        {
            new int[] { 3, 4, 5 },  // 중간 가로 (메인 라인)
            new int[] { 0, 1, 2 },  // 상단 가로
            new int[] { 6, 7, 8 },  // 하단 가로
            new int[] { 0, 4, 8 },  // 대각선 ↘
            new int[] { 6, 4, 2 }   // 대각선 ↗
        };
    }

    public class SlotManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private PlayerData _playerData;
        private GameConfig _config;

        // 상태
        private bool _isSpinning = false;
        private readonly object _spinLock = new object();
        public bool IsSpinning => _isSpinning;

        // 연패 추적
        private int _currentLossStreak = 0;
        public int CurrentLossStreak => _currentLossStreak;

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
            // Race condition 방지: 락을 사용하여 동시 호출 차단
            lock (_spinLock)
            {
                if (_isSpinning)
                {
                    Debug.LogWarning("[SlotManager] Already spinning!");
                    return false;
                }
                // 락 내에서 즉시 플래그 설정하여 이중 호출 방지
                _isSpinning = true;
            }

            // 유효성 검사 (실패 시 플래그 복원)
            if (betAmount <= 0)
            {
                Debug.LogWarning("[SlotManager] Invalid bet amount!");
                _isSpinning = false;
                return false;
            }

            if (!_gameManager.Gold.CanAfford(betAmount))
            {
                Debug.LogWarning("[SlotManager] Not enough gold!");
                _isSpinning = false;
                return false;
            }

            // 베팅액 차감
            if (!_gameManager.Gold.SpendGold(betAmount))
            {
                _isSpinning = false;
                return false;
            }

            _playerData.totalSpins++;
            OnSpinStart?.Invoke();

            // 결과 계산
            SlotResult result = CalculateResult(betAmount);

            // 애니메이션 후 결과 처리 (코루틴으로)
            StartCoroutine(SpinSequence(result));

            return true;
        }

        private System.Collections.IEnumerator SpinSequence(SlotResult result)
        {
            // 3x3 그리드: 열(column)별로 정지 (왼쪽→오른쪽)
            // 열 0: 인덱스 0, 3, 6 / 열 1: 인덱스 1, 4, 7 / 열 2: 인덱스 2, 5, 8
            for (int col = 0; col < 3; col++)
            {
                yield return new WaitForSeconds(_config.reelStopDelay);

                // 해당 열의 3개 심볼 동시 정지
                for (int row = 0; row < 3; row++)
                {
                    int idx = row * 3 + col;
                    OnReelStop?.Invoke(idx, result.Symbols[idx]);
                }
            }

            // 최종 결과 처리
            yield return new WaitForSeconds(0.5f);

            // 보상 지급 및 연패 추적
            if (result.Outcome == SlotOutcome.Loss)
            {
                _currentLossStreak++;
            }
            else
            {
                _currentLossStreak = 0; // 승리 시 연패 초기화
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
        /// 슬롯 결과 계산 (3x3 그리드)
        /// </summary>
        private SlotResult CalculateResult(double betAmount)
        {
            SlotResult result = new SlotResult
            {
                BetAmount = betAmount,
                Symbols = new int[9], // 3x3 = 9개 심볼
                WinningPayline = Array.Empty<int>()
            };

            // 결과 확률 계산 (업그레이드 적용)
            SlotOutcome outcome = DetermineOutcome();
            result.Outcome = outcome;

            // 배당률 적용 (보상 배율 업그레이드 포함)
            result.RewardMultiplier = GetMultiplier(outcome);

            // 골드 부스트 적용
            float goldBoost = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.GoldBoost);
            result.FinalReward = betAmount * result.RewardMultiplier * goldBoost;

            // 심볼 생성 (3x3 결과에 맞게)
            GenerateSymbols3x3(result);

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

            // 럭키참 보너스 적용
            float slotCharmBonus = _gameManager.Prestige?.GetSlotSuccessMultiplier() ?? 1f;
            float jackpotCharmBonus = _gameManager.Prestige?.GetJackpotRateMultiplier() ?? 1f;

            successBonus *= slotCharmBonus;
            jackpotBonus *= jackpotCharmBonus;

            // 연패 보호 보너스 (5연패 이상 시 발동)
            float lossStreakBonus = 1f;
            if (_config.enableLossStreakProtection && _currentLossStreak >= _config.lossStreakThreshold)
            {
                lossStreakBonus = 1f + _config.lossStreakBonusRate;
                Debug.Log($"[SlotManager] Loss streak protection active! ({_currentLossStreak} losses, +{_config.lossStreakBonusRate * 100}% win rate)");
            }

            // 조정된 확률 (연패 보호 적용)
            float adjustedMegaJackpot = _config.megaJackpotRate * jackpotBonus;
            float adjustedJackpot = _config.jackpotRate * jackpotBonus;
            float adjustedBigWin = _config.bigWinRate * successBonus * lossStreakBonus;
            float adjustedSmallWin = _config.smallWinRate * successBonus * lossStreakBonus;
            float adjustedMiniWin = _config.miniWinRate * successBonus * lossStreakBonus;
            float adjustedDraw = _config.drawRate * lossStreakBonus;

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
        /// 3x3 그리드용 심볼 배열 생성
        /// 그리드 인덱스: 0 1 2 (상단) / 3 4 5 (중간) / 6 7 8 (하단)
        /// </summary>
        private void GenerateSymbols3x3(SlotResult result)
        {
            int symbolCount = _config.symbolCount;

            // 먼저 전체 그리드를 랜덤으로 채움
            for (int i = 0; i < 9; i++)
            {
                result.Symbols[i] = UnityEngine.Random.Range(0, symbolCount);
            }

            // 당첨 페이라인 선택 (결과에 따라)
            int winPaylineIndex = UnityEngine.Random.Range(0, SlotPaylines.Lines.Length);
            int[] winLine = SlotPaylines.Lines[winPaylineIndex];

            switch (result.Outcome)
            {
                case SlotOutcome.MegaJackpot:
                    // 모든 페이라인에 777 (전체 그리드가 동일 심볼)
                    int megaSymbol = symbolCount - 1;
                    for (int i = 0; i < 9; i++)
                        result.Symbols[i] = megaSymbol;
                    result.WinningPayline = new int[] { 0, 1, 2, 3, 4 }; // 모든 라인
                    break;

                case SlotOutcome.Jackpot:
                    // 2개 이상 페이라인에서 같은 심볼 3개
                    int jackpotSymbol = UnityEngine.Random.Range(symbolCount - 3, symbolCount);
                    // 중간 라인(3,4,5)과 대각선 하나(0,4,8)를 당첨으로
                    result.Symbols[3] = jackpotSymbol; result.Symbols[4] = jackpotSymbol; result.Symbols[5] = jackpotSymbol;
                    result.Symbols[0] = jackpotSymbol; result.Symbols[8] = jackpotSymbol;
                    result.WinningPayline = new int[] { 0, 3 }; // 중간 가로 + 대각선
                    break;

                case SlotOutcome.BigWin:
                    // 1개 페이라인에서 같은 심볼 3개 (높은 등급)
                    int bigWinSymbol = UnityEngine.Random.Range(symbolCount / 2, symbolCount);
                    foreach (int idx in winLine)
                        result.Symbols[idx] = bigWinSymbol;
                    result.WinningPayline = new int[] { winPaylineIndex };
                    break;

                case SlotOutcome.SmallWin:
                    // 1개 페이라인에서 같은 심볼 3개 (중간 등급)
                    int smallWinSymbol = UnityEngine.Random.Range(2, symbolCount - 2);
                    foreach (int idx in winLine)
                        result.Symbols[idx] = smallWinSymbol;
                    result.WinningPayline = new int[] { winPaylineIndex };
                    break;

                case SlotOutcome.MiniWin:
                case SlotOutcome.Draw:
                    // 1개 페이라인에서 같은 심볼 3개 (낮은 등급)
                    int miniWinSymbol = UnityEngine.Random.Range(0, symbolCount / 2 + 1);
                    foreach (int idx in winLine)
                        result.Symbols[idx] = miniWinSymbol;
                    result.WinningPayline = new int[] { winPaylineIndex };
                    break;

                default: // Loss
                    // 어떤 페이라인도 3개 일치하지 않도록 보장
                    EnsureNoWinningPayline(result.Symbols, symbolCount);
                    result.WinningPayline = Array.Empty<int>();
                    break;
            }
        }

        /// <summary>
        /// 어떤 페이라인도 당첨되지 않도록 심볼 조정
        /// </summary>
        private void EnsureNoWinningPayline(int[] symbols, int symbolCount)
        {
            foreach (var line in SlotPaylines.Lines)
            {
                // 이 라인의 3개가 모두 같은지 체크
                if (symbols[line[0]] == symbols[line[1]] && symbols[line[1]] == symbols[line[2]])
                {
                    // 마지막 심볼을 다르게 변경
                    int current = symbols[line[2]];
                    symbols[line[2]] = (current + 1) % symbolCount;
                }
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
            float charmBonus = _gameManager.Prestige?.GetJackpotRateMultiplier() ?? 1f;
            return _config.jackpotRate * jackpotBonus * charmBonus;
        }

        /// <summary>
        /// 현재 메가잭팟 확률
        /// </summary>
        public float GetCurrentMegaJackpotRate()
        {
            float jackpotBonus = _gameManager.Upgrade.GetEffectMultiplier(UpgradeEffect.JackpotRate);
            float charmBonus = _gameManager.Prestige?.GetJackpotRateMultiplier() ?? 1f;
            return _config.megaJackpotRate * jackpotBonus * charmBonus;
        }
    }
}
