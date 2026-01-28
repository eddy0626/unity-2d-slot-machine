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
        public bool hasSeenTutorial = false;  // 튜토리얼 본 적 있는지

        public PlayerData()
        {
            gold = 300; // 시작 골드 (100 → 300 초반 진행 개선)
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
            if (upgradeLevelsList != null)
            {
                foreach (var entry in upgradeLevelsList)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.upgradeId))
                    {
                        _upgradeLevelsCache[entry.upgradeId] = Mathf.Max(0, entry.level);
                    }
                }
            }
        }

        /// <summary>
        /// 세이브 데이터 검증 및 복구
        /// </summary>
        public void ValidateAndRepair()
        {
            // null 체크 및 초기화
            if (upgradeLevelsList == null)
                upgradeLevelsList = new List<UpgradeLevelEntry>();

            if (ownedLuckyCharms == null)
                ownedLuckyCharms = new List<string>();

            // 음수 값 방지
            gold = Math.Max(0, gold);
            chips = Math.Max(0, chips);
            totalGoldEarned = Math.Max(0, totalGoldEarned);
            totalGoldLost = Math.Max(0, totalGoldLost);
            totalClicks = Math.Max(0, totalClicks);
            totalSpins = Math.Max(0, totalSpins);
            jackpotCount = Math.Max(0, jackpotCount);
            megaJackpotCount = Math.Max(0, megaJackpotCount);
            prestigeCount = Math.Max(0, prestigeCount);

            // 비정상적인 값 체크 (double overflow 방지)
            const double MAX_GOLD = 1e30; // 1 nonillion
            if (double.IsNaN(gold) || double.IsInfinity(gold) || gold > MAX_GOLD)
            {
                Debug.LogWarning($"[PlayerData] Invalid gold value detected ({gold}), resetting to safe value");
                gold = Math.Min(gold, MAX_GOLD);
                if (double.IsNaN(gold) || double.IsInfinity(gold))
                    gold = 300;
            }

            if (double.IsNaN(totalGoldEarned) || double.IsInfinity(totalGoldEarned))
                totalGoldEarned = gold;

            // 버전 체크
            if (string.IsNullOrEmpty(version))
                version = "1.0";

            // 캐시 초기화
            InitializeCache();

            Debug.Log("[PlayerData] Data validation complete");
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
