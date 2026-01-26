using UnityEngine;

namespace SlotMachine.Data
{
    [CreateAssetMenu(fileName = "SlotMachineConfig", menuName = "SlotMachine/Config")]
    public class SlotMachineConfig : ScriptableObject
    {
        [Header("Game Settings")]
        public int initialCoins = 1000;
        public int[] betAmounts = { 10, 25, 50, 100, 250 };
        public int defaultBetIndex = 1;

        [Header("Reel Settings")]
        public int reelCount = 3;
        public int visibleSymbolsPerReel = 3;
        public float symbolHeight = 150f;

        [Header("Spin Animation")]
        public float spinSpeed = 2000f;
        public float spinDuration = 2f;
        public float reelStopDelay = 0.3f;
        public float bounceAmount = 20f;
        public float bounceDuration = 0.2f;

        [Header("Symbols")]
        public SymbolData[] symbols;

        [Header("Bonus Settings")]
        public int scatterBonusMultiplier = 10;
        public int wildSymbolId = 12;
        public int scatterSymbolId = 13;

        // 심볼 ID로 SymbolData 찾기
        public SymbolData GetSymbolById(int id)
        {
            foreach (var symbol in symbols)
            {
                if (symbol != null && symbol.symbolId == id)
                    return symbol;
            }
            return null;
        }

        // 가중치 기반 랜덤 심볼 선택
        public SymbolData GetRandomSymbol()
        {
            int totalWeight = 0;
            foreach (var symbol in symbols)
            {
                if (symbol != null)
                    totalWeight += symbol.weight;
            }

            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var symbol in symbols)
            {
                if (symbol != null)
                {
                    currentWeight += symbol.weight;
                    if (randomValue < currentWeight)
                        return symbol;
                }
            }

            return symbols[0];
        }
    }
}
