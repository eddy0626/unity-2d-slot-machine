using UnityEngine;

namespace SlotClicker.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "SlotClicker/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== 클릭 설정 ===")]
        public double baseClickPower = 1;
        public float criticalChance = 0.05f;
        public float criticalMultiplier = 3f;

        [Header("=== 슬롯 설정 ===")]
        public int reelCount = 3;
        public int symbolCount = 7;

        [Header("슬롯 확률 (합계 100%) - EV 약 85%")]
        [Range(0, 100)] public float lossRate = 60f;        // 50 → 60
        [Range(0, 100)] public float drawRate = 15f;        // 20 → 15
        [Range(0, 100)] public float miniWinRate = 12f;     // 15 → 12
        [Range(0, 100)] public float smallWinRate = 8f;     // 10 → 8
        [Range(0, 100)] public float bigWinRate = 3.5f;     // 유지
        [Range(0, 100)] public float jackpotRate = 1.3f;    // 1 → 1.3
        [Range(0, 100)] public float megaJackpotRate = 0.2f; // 0.5 → 0.2

        [Header("슬롯 배당률 (개선됨)")]
        public float drawMultiplier = 1f;
        public float miniWinMultiplier = 2f;      // 1.5 → 2.0 (당첨감 증가)
        public float smallWinMultiplier = 2.5f;   // 2 → 2.5
        public float bigWinMultiplier = 5f;       // 4 → 5 (빅윈 가치 복원)
        public float jackpotMultiplier = 10f;     // 8 → 10 (희귀성 보상)
        public float megaJackpotMultiplier = 100f; // 50 → 100 (초희귀 보상)

        [Header("=== 베팅 설정 ===")]
        public float[] betPercentages = { 0.1f, 0.3f, 0.5f, 1f };
        public double minimumBet = 10;
        public double maximumBet = 1000000000; // 최대 베팅 제한 (10억)
        [Range(0.1f, 1f)] public float maxBetPercentage = 1f; // 잔액의 최대 100% 베팅 가능 (ALL 배팅 지원)

        [Header("=== 연패 보호 ===")]
        public bool enableLossStreakProtection = true;
        public int lossStreakThreshold = 5; // 5연패 후 보호 발동
        [Range(0f, 0.2f)] public float lossStreakBonusRate = 0.1f; // 연패 보호 시 승률 10% 증가

        [Header("=== 업그레이드 기본값 ===")]
        public float upgradeCostMultiplier = 1.15f;

        [Header("=== 프레스티지 설정 ===")]
        public double prestigeThreshold = 1000000; // 100만 골드
        public float prestigeBonusPerChip = 0.1f; // 칩당 10% 보너스

        [Header("=== 애니메이션 설정 ===")]
        public float spinDuration = 2f;
        public float reelStopDelay = 0.3f;

        // 확률 정규화
        public void NormalizeProbabilities()
        {
            float total = lossRate + drawRate + miniWinRate + smallWinRate + bigWinRate + jackpotRate + megaJackpotRate;
            if (total != 100f)
            {
                float scale = 100f / total;
                lossRate *= scale;
                drawRate *= scale;
                miniWinRate *= scale;
                smallWinRate *= scale;
                bigWinRate *= scale;
                jackpotRate *= scale;
                megaJackpotRate *= scale;
            }
        }
    }
}
