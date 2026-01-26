using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotClicker.Data
{
    /// <summary>
    /// JsonUtility로 직렬화 가능한 업그레이드 레벨 항목
    /// </summary>
    [System.Serializable]
    public class UpgradeLevelEntry
    {
        public string upgradeId;
        public int level;

        public UpgradeLevelEntry() { }

        public UpgradeLevelEntry(string id, int lvl)
        {
            upgradeId = id;
            level = lvl;
        }
    }

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

        // 업그레이드 레벨 (JsonUtility 직렬화 가능한 List 사용)
        public List<UpgradeLevelEntry> upgradeLevelsList = new List<UpgradeLevelEntry>();

        // 런타임 캐시용 Dictionary (직렬화 제외)
        [NonSerialized]
        private Dictionary<string, int> _upgradeLevelsCache;

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
            _upgradeLevelsCache = new Dictionary<string, int>();
        }

        /// <summary>
        /// 로드 후 캐시 초기화
        /// </summary>
        public void InitializeCache()
        {
            _upgradeLevelsCache = new Dictionary<string, int>();
            foreach (var entry in upgradeLevelsList)
            {
                _upgradeLevelsCache[entry.upgradeId] = entry.level;
            }
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            if (_upgradeLevelsCache == null)
                InitializeCache();

            return _upgradeLevelsCache.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        public void SetUpgradeLevel(string upgradeId, int level)
        {
            if (_upgradeLevelsCache == null)
                _upgradeLevelsCache = new Dictionary<string, int>();

            _upgradeLevelsCache[upgradeId] = level;

            // List도 동기화
            var existing = upgradeLevelsList.Find(e => e.upgradeId == upgradeId);
            if (existing != null)
            {
                existing.level = level;
            }
            else
            {
                upgradeLevelsList.Add(new UpgradeLevelEntry(upgradeId, level));
            }
        }
    }
}
