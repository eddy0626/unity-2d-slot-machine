using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotClicker.Data
{
    [System.Serializable]
    public class PlayerData
    {
        // 기본 재화
        public double gold;
        public int chips;

        // 통계
        public double totalGoldEarned;
        public double totalGoldLost;
        public int totalClicks;
        public int totalSpins;
        public int jackpotCount;
        public int megaJackpotCount;

        // 프레스티지
        public int prestigeCount;
        public List<string> ownedLuckyCharms = new List<string>();

        // 업그레이드 레벨
        public Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();

        // 메타
        public string lastPlayTime;
        public string version = "1.0";

        public PlayerData()
        {
            gold = 100; // 시작 골드
            chips = 0;
            totalGoldEarned = 0;
            totalGoldLost = 0;
            totalClicks = 0;
            totalSpins = 0;
            jackpotCount = 0;
            megaJackpotCount = 0;
            prestigeCount = 0;
            lastPlayTime = DateTime.Now.ToString("o");
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            return upgradeLevels.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        public void SetUpgradeLevel(string upgradeId, int level)
        {
            upgradeLevels[upgradeId] = level;
        }
    }
}
