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

        [Header("슬롯 확률 (합계 100%)")]
        [Range(0, 100)] public float lossRate = 50f;
        [Range(0, 100)] public float drawRate = 20f;
        [Range(0, 100)] public float miniWinRate = 15f;
        [Range(0, 100)] public float smallWinRate = 10f;
        [Range(0, 100)] public float bigWinRate = 3.5f;
        [Range(0, 100)] public float jackpotRate = 1f;
        [Range(0, 100)] public float megaJackpotRate = 0.5f;

        [Header("슬롯 배당률")]
        public float drawMultiplier = 1f;
        public float miniWinMultiplier = 1.5f;
        public float smallWinMultiplier = 2f;
        public float bigWinMultiplier = 5f;
        public float jackpotMultiplier = 10f;
        public float megaJackpotMultiplier = 100f;

        [Header("=== 베팅 설정 ===")]
        public float[] betPercentages = { 0.1f, 0.3f, 0.5f, 1f };
        public double minimumBet = 10;
        public double maximumBet = 1000000000; // 최대 베팅 제한 (10억)
        [Range(0.1f, 1f)] public float maxBetPercentage = 0.5f; // 잔액의 최대 50%만 베팅 가능

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
