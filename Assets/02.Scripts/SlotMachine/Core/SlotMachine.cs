using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Data;

namespace SlotMachine.Core
{
    public enum GameState
    {
        Idle,
        Spinning,
        Stopping,
        ShowingResults
    }

    [System.Serializable]
    public class SpinResultEvent : UnityEvent<List<WinResult>, int> { }

    public class SlotMachine : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SlotMachineConfig config;

        [Header("References")]
        [SerializeField] private Reel[] reels;
        [SerializeField] private PaylineManager paylineManager;

        [Header("Game State")]
        [SerializeField] private int currentCoins;
        [SerializeField] private int currentBetIndex;
        [SerializeField] private GameState gameState = GameState.Idle;

        [Header("Events")]
        public UnityEvent OnSpinStart;
        public UnityEvent OnSpinEnd;
        public SpinResultEvent OnWin;
        public UnityEvent<int> OnCoinsChanged;
        public UnityEvent<int> OnBetChanged;

        public int CurrentCoins => currentCoins;
        public int CurrentBet => config.betAmounts[currentBetIndex];
        public GameState State => gameState;
        public bool CanSpin => gameState == GameState.Idle && currentCoins >= CurrentBet;

        private SymbolData[,] _reelResults; // [릴 인덱스, 심볼 인덱스(상/중/하)]

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (config == null)
            {
                Debug.LogWarning("SlotMachineConfig is not assigned. SlotMachine will not initialize.");
                enabled = false;
                return;
            }

            currentCoins = config.initialCoins;
            currentBetIndex = config.defaultBetIndex;
            _reelResults = new SymbolData[config.reelCount, 3];

            // 릴 초기화
            if (reels == null || reels.Length == 0)
            {
                Debug.LogWarning("No reels assigned. SlotMachine will not initialize.");
                enabled = false;
                return;
            }

            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].Initialize(config, i);
                }
            }

            // PaylineManager 초기화
            if (paylineManager != null)
            {
                paylineManager.Initialize(config);
            }

            OnCoinsChanged?.Invoke(currentCoins);
            OnBetChanged?.Invoke(CurrentBet);
        }

        /// <summary>
        /// 스핀 시작
        /// </summary>
        public void Spin()
        {
            if (!CanSpin)
            {
                Debug.Log("Cannot spin: " + (gameState != GameState.Idle ? "Already spinning" : "Not enough coins"));
                return;
            }

            StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            gameState = GameState.Spinning;

            // 배팅 차감
            currentCoins -= CurrentBet;
            OnCoinsChanged?.Invoke(currentCoins);
            OnSpinStart?.Invoke();

            // 결과 미리 결정
            GenerateResults();

            // 모든 릴 회전 시작
            foreach (var reel in reels)
            {
                reel.StartSpin();
            }

            // 스핀 지속 시간
            yield return new WaitForSeconds(config.spinDuration);

            gameState = GameState.Stopping;

            // 순차적으로 릴 정지
            for (int i = 0; i < reels.Length; i++)
            {
                SymbolData[] reelSymbols = new SymbolData[3];
                for (int j = 0; j < 3; j++)
                {
                    reelSymbols[j] = _reelResults[i, j];
                }

                reels[i].StopSpin(reelSymbols);
                yield return new WaitForSeconds(config.reelStopDelay);
            }

            // 모든 릴 정지 대기
            yield return new WaitForSeconds(0.5f);

            gameState = GameState.ShowingResults;

            // 결과 확인
            CheckResults();

            gameState = GameState.Idle;
            OnSpinEnd?.Invoke();
        }

        /// <summary>
        /// 스핀 결과 생성
        /// </summary>
        private void GenerateResults()
        {
            for (int reel = 0; reel < config.reelCount; reel++)
            {
                for (int pos = 0; pos < 3; pos++)
                {
                    _reelResults[reel, pos] = config.GetRandomSymbol();
                }
            }
        }

        /// <summary>
        /// 당첨 확인
        /// </summary>
        private void CheckResults()
        {
            // 3x3 그리드로 변환
            // 그리드 인덱스:
            // 0 1 2  (상단) - row 0
            // 3 4 5  (중앙) - row 1
            // 6 7 8  (하단) - row 2
            SymbolData[] grid = new SymbolData[9];

            for (int reel = 0; reel < 3; reel++)
            {
                grid[reel] = _reelResults[reel, 0];     // 상단
                grid[reel + 3] = _reelResults[reel, 1]; // 중앙
                grid[reel + 6] = _reelResults[reel, 2]; // 하단
            }

            // 페이라인 확인
            List<WinResult> wins = paylineManager.CheckAllPaylines(grid, CurrentBet);

            if (wins.Count > 0)
            {
                int totalWin = 0;
                foreach (var win in wins)
                {
                    totalWin += win.payout;
                    Debug.Log($"WIN! Line {win.paylineIndex}: {win.symbol.symbolName} x{win.matchCount} = {win.payout}");
                }

                currentCoins += totalWin;
                OnCoinsChanged?.Invoke(currentCoins);
                OnWin?.Invoke(wins, totalWin);
            }
        }

        /// <summary>
        /// 배팅 금액 증가
        /// </summary>
        public void IncreaseBet()
        {
            if (currentBetIndex < config.betAmounts.Length - 1)
            {
                currentBetIndex++;
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        /// <summary>
        /// 배팅 금액 감소
        /// </summary>
        public void DecreaseBet()
        {
            if (currentBetIndex > 0)
            {
                currentBetIndex--;
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        /// <summary>
        /// 코인 추가 (데모/치트용)
        /// </summary>
        public void AddCoins(int amount)
        {
            currentCoins += amount;
            OnCoinsChanged?.Invoke(currentCoins);
        }

        /// <summary>
        /// 최대 배팅 설정
        /// </summary>
        public void SetMaxBet()
        {
            currentBetIndex = config.betAmounts.Length - 1;
            OnBetChanged?.Invoke(CurrentBet);
        }

#if UNITY_EDITOR
        [ContextMenu("Add 1000 Coins")]
        private void DebugAddCoins()
        {
            AddCoins(1000);
        }

        [ContextMenu("Force Win")]
        private void DebugForceWin()
        {
            // 디버그용: 모든 릴에 같은 심볼 배치
            if (config.symbols.Length > 0)
            {
                var symbol = config.symbols[0];
                for (int reel = 0; reel < 3; reel++)
                {
                    for (int pos = 0; pos < 3; pos++)
                    {
                        _reelResults[reel, pos] = symbol;
                    }
                }
            }
        }
#endif
    }
}
