using System;
using UnityEngine;

namespace SlotClicker.Data
{
    public enum UpgradeCategory
    {
        Click,  // 클릭 관련
        Slot,   // 슬롯 관련
        Gold    // 골드 관련
    }

    public enum UpgradeEffect
    {
        ClickPower,         // 클릭당 골드 증가
        CriticalChance,     // 크리티컬 확률
        CriticalMultiplier, // 크리티컬 배율
        SlotSuccessRate,    // 슬롯 성공률
        JackpotRate,        // 잭팟 확률
        RewardMultiplier,   // 보상 배율
        GoldBoost,          // 골드 획득량 증가
        AutoCollect,        // 자동 수집
        Interest            // 이자
    }

    [System.Serializable]
    public class UpgradeData
    {
        public string id;
        public string name;
        public string description;
        public UpgradeCategory category;
        public UpgradeEffect effect;
        public Sprite icon;

        [Header("비용")]
        public double baseCost;
        public float costMultiplier;
        public int maxLevel; // -1 = 무제한

        [Header("효과")]
        public float effectPerLevel;
        public string effectFormat; // "{0}%" 또는 "+{0}"

        public UpgradeData()
        {
            costMultiplier = 1.15f;
            maxLevel = -1;
            effectFormat = "+{0}";
        }

        /// <summary>
        /// 특정 레벨의 업그레이드 비용 계산
        /// </summary>
        public double GetCost(int level)
        {
            // 기본 공식: Cost = BaseCost × Multiplier^level
            double cost = baseCost * Math.Pow(costMultiplier, level);

            // 소프트캡: 레벨 100 이후 추가 증가
            if (level > 100)
            {
                cost = baseCost * Math.Pow(costMultiplier, 100)
                     * Math.Pow(costMultiplier * 1.05, level - 100);
            }

            return Math.Floor(cost);
        }

        /// <summary>
        /// 특정 레벨의 효과 값 계산
        /// </summary>
        public float GetEffectValue(int level)
        {
            return effectPerLevel * level;
        }

        /// <summary>
        /// 효과 설명 텍스트
        /// </summary>
        public string GetEffectDescription(int level)
        {
            float value = GetEffectValue(level);
            return string.Format(effectFormat, value.ToString("F1"));
        }

        /// <summary>
        /// 최대 레벨 도달 여부
        /// </summary>
        public bool IsMaxLevel(int currentLevel)
        {
            return maxLevel > 0 && currentLevel >= maxLevel;
        }
    }

    /// <summary>
    /// 모든 업그레이드 정의를 담는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "SlotClicker/Upgrade Database")]
    public class UpgradeDatabase : ScriptableObject
    {
        [Header("클릭 업그레이드")]
        public UpgradeData[] clickUpgrades;

        [Header("슬롯 업그레이드")]
        public UpgradeData[] slotUpgrades;

        [Header("골드 업그레이드")]
        public UpgradeData[] goldUpgrades;

        public UpgradeData GetUpgrade(string id)
        {
            foreach (var upgrade in clickUpgrades)
                if (upgrade.id == id) return upgrade;
            foreach (var upgrade in slotUpgrades)
                if (upgrade.id == id) return upgrade;
            foreach (var upgrade in goldUpgrades)
                if (upgrade.id == id) return upgrade;
            return null;
        }

        public UpgradeData[] GetUpgradesByCategory(UpgradeCategory category)
        {
            return category switch
            {
                UpgradeCategory.Click => clickUpgrades,
                UpgradeCategory.Slot => slotUpgrades,
                UpgradeCategory.Gold => goldUpgrades,
                _ => new UpgradeData[0]
            };
        }

        /// <summary>
        /// 기본 업그레이드 데이터 생성
        /// </summary>
        public void CreateDefaultUpgrades()
        {
            // 클릭 업그레이드
            clickUpgrades = new UpgradeData[]
            {
                new UpgradeData
                {
                    id = "click_power",
                    name = "클릭 파워",
                    description = "클릭당 골드 획득량 증가",
                    category = UpgradeCategory.Click,
                    effect = UpgradeEffect.ClickPower,
                    baseCost = 10,
                    costMultiplier = 1.15f,
                    maxLevel = -1,
                    effectPerLevel = 25f,  // 레벨당 25% 증가 (기존 0.5%)
                    effectFormat = "+{0}%"
                },
                new UpgradeData
                {
                    id = "critical_chance",
                    name = "크리티컬 확률",
                    description = "크리티컬 발생 확률 증가",
                    category = UpgradeCategory.Click,
                    effect = UpgradeEffect.CriticalChance,
                    baseCost = 50,
                    costMultiplier = 1.2f,
                    maxLevel = 45, // 최대 50% (기본 5% + 45%)
                    effectPerLevel = 1f,
                    effectFormat = "+{0}%"
                },
                new UpgradeData
                {
                    id = "critical_multiplier",
                    name = "크리티컬 배율",
                    description = "크리티컬 데미지 배율 증가",
                    category = UpgradeCategory.Click,
                    effect = UpgradeEffect.CriticalMultiplier,
                    baseCost = 100,
                    costMultiplier = 1.25f,
                    maxLevel = -1,
                    effectPerLevel = 0.25f,  // 레벨당 +0.25x (기존 0.5f)
                    effectFormat = "+{0}x"
                }
            };

            // 슬롯 업그레이드
            slotUpgrades = new UpgradeData[]
            {
                new UpgradeData
                {
                    id = "slot_success_rate",
                    name = "성공률 증가",
                    description = "슬롯 당첨 확률 증가",
                    category = UpgradeCategory.Slot,
                    effect = UpgradeEffect.SlotSuccessRate,
                    baseCost = 100,
                    costMultiplier = 1.18f,
                    maxLevel = 50,
                    effectPerLevel = 2f,
                    effectFormat = "+{0}%"
                },
                new UpgradeData
                {
                    id = "jackpot_rate",
                    name = "잭팟 확률",
                    description = "잭팟 당첨 확률 증가",
                    category = UpgradeCategory.Slot,
                    effect = UpgradeEffect.JackpotRate,
                    baseCost = 500,
                    costMultiplier = 1.3f,
                    maxLevel = 20,
                    effectPerLevel = 5f,
                    effectFormat = "+{0}%"
                },
                new UpgradeData
                {
                    id = "reward_multiplier",
                    name = "보상 배율",
                    description = "슬롯 보상 배율 증가",
                    category = UpgradeCategory.Slot,
                    effect = UpgradeEffect.RewardMultiplier,
                    baseCost = 200,
                    costMultiplier = 1.22f,
                    maxLevel = -1,
                    effectPerLevel = 5f,
                    effectFormat = "+{0}%"
                }
            };

            // 골드 업그레이드
            goldUpgrades = new UpgradeData[]
            {
                new UpgradeData
                {
                    id = "gold_boost",
                    name = "골드 부스트",
                    description = "모든 골드 획득량 증가",
                    category = UpgradeCategory.Gold,
                    effect = UpgradeEffect.GoldBoost,
                    baseCost = 150,
                    costMultiplier = 1.2f,
                    maxLevel = -1,
                    effectPerLevel = 10f,
                    effectFormat = "+{0}%"
                },
                new UpgradeData
                {
                    id = "auto_collect",
                    name = "자동 수집",
                    description = "초당 자동으로 골드 획득",
                    category = UpgradeCategory.Gold,
                    effect = UpgradeEffect.AutoCollect,
                    baseCost = 1000,
                    costMultiplier = 1.25f,
                    maxLevel = -1,
                    effectPerLevel = 0.1f,
                    effectFormat = "+{0}/초"
                },
                new UpgradeData
                {
                    id = "interest",
                    name = "이자",
                    description = "보유 골드의 일정량을 초당 획득",
                    category = UpgradeCategory.Gold,
                    effect = UpgradeEffect.Interest,
                    baseCost = 5000,
                    costMultiplier = 1.35f,
                    maxLevel = 20,
                    effectPerLevel = 0.1f,
                    effectFormat = "+{0}%/초"
                }
            };
        }
    }
}
