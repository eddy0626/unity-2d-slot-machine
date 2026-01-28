using System;
using System.Collections.Generic;
using UnityEngine;
using SlotClicker.Data;

namespace SlotClicker.Core
{
    public enum VIPRank
    {
        None,       // 0회
        Bronze,     // 1회
        Silver,     // 3회
        Gold,       // 5회
        Platinum,   // 10회
        Diamond     // 20회
    }

    [System.Serializable]
    public class LuckyCharmData
    {
        public string id;
        public string name;
        public string description;
        public int tier; // 1, 2, 3
        public int chipCost;
        public LuckyCharmEffect effect;
        public float effectValue;
        public VIPRank requiredRank;

        public LuckyCharmData(string id, string name, string desc, int tier, int cost,
            LuckyCharmEffect effect, float value, VIPRank required = VIPRank.None)
        {
            this.id = id;
            this.name = name;
            this.description = desc;
            this.tier = tier;
            this.chipCost = cost;
            this.effect = effect;
            this.effectValue = value;
            this.requiredRank = required;
        }
    }

    public enum LuckyCharmEffect
    {
        ClickGoldBonus,     // 클릭 골드 증가
        SlotSuccessBonus,   // 슬롯 성공률 증가
        JackpotRateBonus,   // 잭팟 확률 증가
        CriticalBonus,      // 크리티컬 확률 증가
        GoldBoostBonus,     // 모든 골드 증가
        AutoCollectBonus,   // 자동 수집 보너스
        InterestBonus       // 이자 보너스
    }

    public class PrestigeManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private PlayerData _playerData;

        // 럭키 참 데이터
        private List<LuckyCharmData> _allCharms = new List<LuckyCharmData>();

        // 이벤트
        public event Action OnPrestigeComplete;
        public event Action<string> OnCharmPurchased;
        public event Action<VIPRank> OnVIPRankChanged;

        // VIP 등급 임계값
        private static readonly int[] VIP_THRESHOLDS = { 0, 1, 3, 5, 10, 20 };

        public VIPRank CurrentVIPRank => GetVIPRank(_playerData.prestigeCount);
        public int TotalChips => _playerData.chips;
        public int PrestigeCount => _playerData.prestigeCount;

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            _playerData = gameManager.PlayerData;

            InitializeLuckyCharms();

            Debug.Log($"[PrestigeManager] Initialized. VIP: {CurrentVIPRank}, Chips: {TotalChips}");
        }

        private void InitializeLuckyCharms()
        {
            _allCharms = new List<LuckyCharmData>
            {
                // TIER 1 (기본)
                new LuckyCharmData("golden_finger", "골든 핑거", "클릭 골드 +20%",
                    1, 5, LuckyCharmEffect.ClickGoldBonus, 20f),
                new LuckyCharmData("lucky_dice", "럭키 다이스", "슬롯 성공률 +5%",
                    1, 5, LuckyCharmEffect.SlotSuccessBonus, 5f),
                new LuckyCharmData("golden_horseshoe", "황금 말발굽", "크리티컬 확률 +5%",
                    1, 8, LuckyCharmEffect.CriticalBonus, 5f),

                // TIER 2 (실버 VIP 필요)
                new LuckyCharmData("fortune_star", "포츈 스타", "잭팟 확률 +10%",
                    2, 10, LuckyCharmEffect.JackpotRateBonus, 10f, VIPRank.Silver),
                new LuckyCharmData("midas_touch", "마이다스 터치", "모든 골드 +15%",
                    2, 12, LuckyCharmEffect.GoldBoostBonus, 15f, VIPRank.Silver),
                new LuckyCharmData("auto_collector", "자동 수집기", "자동 수집 +50%",
                    2, 15, LuckyCharmEffect.AutoCollectBonus, 50f, VIPRank.Silver),

                // TIER 3 (골드 VIP 필요)
                new LuckyCharmData("diamond_dice", "다이아몬드 다이스", "슬롯 성공률 +10%",
                    3, 20, LuckyCharmEffect.SlotSuccessBonus, 10f, VIPRank.Gold),
                new LuckyCharmData("mega_fortune", "메가 포츈", "잭팟 확률 +20%",
                    3, 25, LuckyCharmEffect.JackpotRateBonus, 20f, VIPRank.Gold),
                new LuckyCharmData("compound_interest", "복리 이자", "이자 +100%",
                    3, 30, LuckyCharmEffect.InterestBonus, 100f, VIPRank.Platinum)
            };
        }

        #region 프레스티지 실행

        /// <summary>
        /// 획득 가능한 칩 수 계산 (마일스톤 시스템)
        /// </summary>
        public int CalculateChipsToGain()
        {
            double earned = _playerData.totalGoldEarned;

            // 마일스톤 시스템 (초반 진행 개선)
            // 50K: 1칩, 250K: 2칩, 500K+: 기존 공식
            if (earned < 50_000)
                return 0;

            if (earned < 250_000)
                return 1;  // 50K 달성: 1칩

            if (earned < 500_000)
                return 2;  // 250K 달성: 2칩

            // 500K 이상: 기존 공식 (3칩부터 시작)
            // floor((log10(총 획득 골드) - 5) * 1.5) + 1
            // 500K: 3칩, 1M: 4칩, 10M: 5칩, 100M: 7칩, 1B: 9칩
            double logValue = Math.Log10(earned) - 5;
            int baseChips = Mathf.FloorToInt((float)(logValue * 1.5)) + 1;

            return Mathf.Max(3, baseChips);
        }

        /// <summary>
        /// 프레스티지 가능 여부
        /// </summary>
        public bool CanPrestige()
        {
            return CalculateChipsToGain() > 0;
        }

        /// <summary>
        /// 프레스티지 실행
        /// </summary>
        public bool ExecutePrestige()
        {
            int chipsToGain = CalculateChipsToGain();
            if (chipsToGain <= 0)
            {
                Debug.LogWarning("[PrestigeManager] Not enough gold to prestige!");
                return false;
            }

            VIPRank oldRank = CurrentVIPRank;

            // 칩 지급
            _playerData.chips += chipsToGain;
            _playerData.prestigeCount++;

            // 골드 및 업그레이드 초기화
            _playerData.gold = 300; // 시작 골드 (100 → 300 초반 진행 개선)
            _playerData.totalGoldEarned = 0;
            _playerData.totalGoldLost = 0;
            _playerData.totalClicks = 0;
            _playerData.totalSpins = 0;
            _playerData.upgradeLevelsList.Clear();
            _playerData.InitializeCache(); // 캐시도 초기화

            // 잭팟 카운트는 유지 (선택적)
            // _playerData.jackpotCount = 0;
            // _playerData.megaJackpotCount = 0;

            // 효과 캐시 갱신
            _gameManager.Upgrade.RefreshEffectCache();

            // VIP 등급 변경 체크
            VIPRank newRank = CurrentVIPRank;
            if (newRank != oldRank)
            {
                OnVIPRankChanged?.Invoke(newRank);
                Debug.Log($"[PrestigeManager] VIP Rank upgraded: {oldRank} -> {newRank}");
            }

            OnPrestigeComplete?.Invoke();
            _gameManager.NotifyStateChanged();
            _gameManager.SavePlayerData();

            Debug.Log($"[PrestigeManager] Prestige complete! +{chipsToGain} chips, Total: {_playerData.chips}");
            return true;
        }

        #endregion

        #region VIP 시스템

        public VIPRank GetVIPRank(int prestigeCount)
        {
            if (prestigeCount >= 20) return VIPRank.Diamond;
            if (prestigeCount >= 10) return VIPRank.Platinum;
            if (prestigeCount >= 5) return VIPRank.Gold;
            if (prestigeCount >= 3) return VIPRank.Silver;
            if (prestigeCount >= 1) return VIPRank.Bronze;
            return VIPRank.None;
        }

        public string GetVIPRankName(VIPRank rank)
        {
            return rank switch
            {
                VIPRank.None => "일반",
                VIPRank.Bronze => "브론즈",
                VIPRank.Silver => "실버",
                VIPRank.Gold => "골드",
                VIPRank.Platinum => "플래티넘",
                VIPRank.Diamond => "다이아몬드",
                _ => "일반"
            };
        }

        public int GetNextVIPThreshold()
        {
            int current = _playerData.prestigeCount;
            foreach (int threshold in VIP_THRESHOLDS)
            {
                if (threshold > current)
                    return threshold;
            }
            return -1; // 최고 등급
        }

        /// <summary>
        /// VIP 등급 보너스 (강화된 보너스)
        /// </summary>
        public float GetVIPBonus()
        {
            return CurrentVIPRank switch
            {
                VIPRank.Bronze => 0.15f,    // +15% (기존 5%)
                VIPRank.Silver => 0.35f,    // +35% (기존 10%)
                VIPRank.Gold => 0.60f,      // +60% (기존 15%)
                VIPRank.Platinum => 1.00f,  // +100% (기존 25%)
                VIPRank.Diamond => 2.00f,   // +200% (기존 50%)
                _ => 0f
            };
        }

        #endregion

        #region 럭키 참 시스템

        public List<LuckyCharmData> GetAllCharms()
        {
            return _allCharms;
        }

        public List<LuckyCharmData> GetCharmsByTier(int tier)
        {
            return _allCharms.FindAll(c => c.tier == tier);
        }

        public bool OwnsCharm(string charmId)
        {
            return _playerData.ownedLuckyCharms.Contains(charmId);
        }

        public bool CanPurchaseCharm(string charmId)
        {
            LuckyCharmData charm = _allCharms.Find(c => c.id == charmId);
            if (charm == null) return false;

            // 이미 보유 중
            if (OwnsCharm(charmId)) return false;

            // 칩 부족
            if (_playerData.chips < charm.chipCost) return false;

            // VIP 등급 부족
            if (CurrentVIPRank < charm.requiredRank) return false;

            return true;
        }

        public bool TryPurchaseCharm(string charmId)
        {
            if (!CanPurchaseCharm(charmId))
            {
                Debug.LogWarning($"[PrestigeManager] Cannot purchase charm: {charmId}");
                return false;
            }

            LuckyCharmData charm = _allCharms.Find(c => c.id == charmId);

            _playerData.chips -= charm.chipCost;
            _playerData.ownedLuckyCharms.Add(charmId);

            OnCharmPurchased?.Invoke(charmId);
            _gameManager.NotifyStateChanged();
            _gameManager.SavePlayerData();

            Debug.Log($"[PrestigeManager] Purchased charm: {charm.name}");
            return true;
        }

        /// <summary>
        /// 특정 효과에 대한 럭키 참 보너스 합계
        /// </summary>
        public float GetCharmBonus(LuckyCharmEffect effect)
        {
            float bonus = 0f;
            foreach (var charm in _allCharms)
            {
                if (charm.effect == effect && OwnsCharm(charm.id))
                {
                    bonus += charm.effectValue;
                }
            }
            return bonus;
        }

        #endregion

        #region 전체 보너스 계산

        /// <summary>
        /// 프레스티지 기본 보너스 (칩당 25% + 복리 효과)
        /// 칩이 많을수록 보너스가 가속됨
        /// </summary>
        public float GetPrestigeBonus()
        {
            int chips = _playerData.chips;
            if (chips <= 0) return 1f;

            // 기본: 칩당 25% (기존 10% → 25%)
            float baseBonus = chips * 0.25f;

            // 복리 효과: 칩 10개마다 추가 50% 보너스
            float compoundBonus = (chips / 10) * 0.5f;

            return 1f + baseBonus + compoundBonus;
        }

        /// <summary>
        /// 전체 프레스티지 배율 (기본 + VIP + 럭키참)
        /// </summary>
        public float GetTotalPrestigeMultiplier()
        {
            float baseBonus = GetPrestigeBonus();
            float vipBonus = 1f + GetVIPBonus();
            return baseBonus * vipBonus;
        }

        /// <summary>
        /// 클릭 골드 보너스 (럭키참)
        /// </summary>
        public float GetClickGoldMultiplier()
        {
            float bonus = GetCharmBonus(LuckyCharmEffect.ClickGoldBonus);
            return 1f + (bonus / 100f);
        }

        /// <summary>
        /// 슬롯 성공률 보너스 (럭키참)
        /// </summary>
        public float GetSlotSuccessMultiplier()
        {
            float bonus = GetCharmBonus(LuckyCharmEffect.SlotSuccessBonus);
            return 1f + (bonus / 100f);
        }

        /// <summary>
        /// 잭팟 확률 보너스 (럭키참)
        /// </summary>
        public float GetJackpotRateMultiplier()
        {
            float bonus = GetCharmBonus(LuckyCharmEffect.JackpotRateBonus);
            return 1f + (bonus / 100f);
        }

        /// <summary>
        /// 크리티컬 확률 보너스 (럭키참)
        /// </summary>
        public float GetCriticalBonusValue()
        {
            return GetCharmBonus(LuckyCharmEffect.CriticalBonus);
        }

        /// <summary>
        /// 골드 부스트 보너스 (럭키참)
        /// </summary>
        public float GetGoldBoostMultiplier()
        {
            float bonus = GetCharmBonus(LuckyCharmEffect.GoldBoostBonus);
            return 1f + (bonus / 100f);
        }

        /// <summary>
        /// 자동 수집 보너스 (럭키참)
        /// </summary>
        public float GetAutoCollectMultiplier()
        {
            float bonus = GetCharmBonus(LuckyCharmEffect.AutoCollectBonus);
            return 1f + (bonus / 100f);
        }

        /// <summary>
        /// 이자 보너스 (럭키참)
        /// </summary>
        public float GetInterestMultiplier()
        {
            float bonus = GetCharmBonus(LuckyCharmEffect.InterestBonus);
            return 1f + (bonus / 100f);
        }

        #endregion
    }
}
