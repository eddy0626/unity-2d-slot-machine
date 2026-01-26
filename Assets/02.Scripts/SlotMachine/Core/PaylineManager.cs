using UnityEngine;
using System.Collections.Generic;
using SlotMachine.Data;

namespace SlotMachine.Core
{
    [System.Serializable]
    public class WinResult
    {
        public int paylineIndex;
        public SymbolData symbol;
        public int matchCount;
        public int payout;
        public int[] positions;
    }

    public class PaylineManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private SlotMachineConfig config;

        // 페이라인 정의 (3x3 그리드)
        // 그리드 인덱스:
        // 0 1 2  (상단)
        // 3 4 5  (중앙)
        // 6 7 8  (하단)
        private readonly int[][] _paylines = new int[][]
        {
            new int[] { 3, 4, 5 },  // 라인1: 중앙 가로
            new int[] { 0, 1, 2 },  // 라인2: 상단 가로
            new int[] { 6, 7, 8 },  // 라인3: 하단 가로
            new int[] { 0, 4, 8 },  // 라인4: 대각선 ↘
            new int[] { 6, 4, 2 }   // 라인5: 대각선 ↗
        };

        private readonly Color[] _paylineColors = new Color[]
        {
            new Color(1f, 0.84f, 0f),     // Gold
            new Color(0f, 1f, 0.6f),      // Cyan
            new Color(1f, 0.2f, 0.6f),    // Pink
            new Color(0.6f, 0.2f, 1f),    // Purple
            new Color(0.2f, 0.8f, 1f)     // Light Blue
        };

        public int PaylineCount => _paylines.Length;

        public void Initialize(SlotMachineConfig slotConfig)
        {
            config = slotConfig;
        }

        /// <summary>
        /// 모든 페이라인의 당첨 여부 확인
        /// </summary>
        /// <param name="grid">3x3 그리드의 심볼 데이터 (인덱스 0-8)</param>
        /// <param name="betAmount">배팅 금액</param>
        /// <returns>당첨 결과 리스트</returns>
        public List<WinResult> CheckAllPaylines(SymbolData[] grid, int betAmount)
        {
            List<WinResult> results = new List<WinResult>();

            for (int i = 0; i < _paylines.Length; i++)
            {
                WinResult result = CheckPayline(grid, i, betAmount);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            // 스캐터 보너스 체크
            WinResult scatterResult = CheckScatterBonus(grid, betAmount);
            if (scatterResult != null)
            {
                results.Add(scatterResult);
            }

            return results;
        }

        /// <summary>
        /// 단일 페이라인 당첨 확인
        /// </summary>
        private WinResult CheckPayline(SymbolData[] grid, int paylineIndex, int betAmount)
        {
            int[] positions = _paylines[paylineIndex];
            SymbolData[] lineSymbols = new SymbolData[3];

            for (int i = 0; i < 3; i++)
            {
                lineSymbols[i] = grid[positions[i]];
            }

            // WILD 처리를 포함한 매칭 확인
            SymbolData matchSymbol = GetMatchingSymbol(lineSymbols);

            if (matchSymbol != null)
            {
                int matchCount = CountMatches(lineSymbols, matchSymbol);

                if (matchCount == 3)
                {
                    return new WinResult
                    {
                        paylineIndex = paylineIndex,
                        symbol = matchSymbol,
                        matchCount = matchCount,
                        payout = matchSymbol.payoutMultiplier * betAmount,
                        positions = positions
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// WILD 심볼을 고려하여 매칭되는 심볼 찾기
        /// </summary>
        private SymbolData GetMatchingSymbol(SymbolData[] symbols)
        {
            SymbolData nonWildSymbol = null;
            int wildCount = 0;

            foreach (var symbol in symbols)
            {
                if (symbol == null) return null;

                if (symbol.symbolType == SymbolType.Wild)
                {
                    wildCount++;
                }
                else if (symbol.symbolType != SymbolType.Scatter)
                {
                    if (nonWildSymbol == null)
                    {
                        nonWildSymbol = symbol;
                    }
                    else if (nonWildSymbol.symbolId != symbol.symbolId)
                    {
                        return null; // 다른 심볼 발견
                    }
                }
            }

            // 모두 WILD인 경우
            if (wildCount == 3)
            {
                return symbols[0]; // WILD 심볼 자체 반환
            }

            return nonWildSymbol;
        }

        /// <summary>
        /// 매칭 개수 계산 (WILD 포함)
        /// </summary>
        private int CountMatches(SymbolData[] symbols, SymbolData matchSymbol)
        {
            int count = 0;
            foreach (var symbol in symbols)
            {
                if (symbol.symbolId == matchSymbol.symbolId ||
                    symbol.symbolType == SymbolType.Wild)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 스캐터 보너스 확인 (위치 무관)
        /// </summary>
        private WinResult CheckScatterBonus(SymbolData[] grid, int betAmount)
        {
            int scatterCount = 0;
            List<int> scatterPositions = new List<int>();
            SymbolData scatterSymbol = null;

            for (int i = 0; i < grid.Length; i++)
            {
                if (grid[i] != null && grid[i].symbolType == SymbolType.Scatter)
                {
                    scatterCount++;
                    scatterPositions.Add(i);
                    scatterSymbol = grid[i];
                }
            }

            if (scatterCount >= 3 && scatterSymbol != null)
            {
                return new WinResult
                {
                    paylineIndex = -1, // 스캐터는 페이라인 무관
                    symbol = scatterSymbol,
                    matchCount = scatterCount,
                    payout = config.scatterBonusMultiplier * betAmount * scatterCount,
                    positions = scatterPositions.ToArray()
                };
            }

            return null;
        }

        /// <summary>
        /// 페이라인 색상 가져오기
        /// </summary>
        public Color GetPaylineColor(int index)
        {
            if (index >= 0 && index < _paylineColors.Length)
                return _paylineColors[index];
            return Color.white;
        }

        /// <summary>
        /// 페이라인 위치 배열 가져오기
        /// </summary>
        public int[] GetPaylinePositions(int index)
        {
            if (index >= 0 && index < _paylines.Length)
                return _paylines[index];
            return null;
        }
    }
}
