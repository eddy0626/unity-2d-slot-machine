using System;
using UnityEngine;

namespace SlotClicker.Core
{
    /// <summary>
    /// 일일 로그인 보상 시스템
    /// - 7일 스트릭 보상 (1.1x → 2.0x 골드 배율)
    /// - 연속 로그인 시 보너스 증가
    /// - 하루 놓치면 스트릭 리셋
    /// </summary>
    public class DailyLoginManager
    {
        #region Constants

        // 일일 보상 배율 (Day 1~7)
        private static readonly float[] DAILY_MULTIPLIERS = new float[]
        {
            1.1f,   // Day 1
            1.15f,  // Day 2
            1.2f,   // Day 3
            1.3f,   // Day 4
            1.4f,   // Day 5
            1.6f,   // Day 6
            2.0f    // Day 7
        };

        // 일일 보너스 칩 (Day 7에만)
        private const int DAY7_BONUS_CHIPS = 5;

        // 보상 지속 시간 (시간 단위)
        private static readonly int[] REWARD_DURATION_HOURS = new int[]
        {
            8,   // Day 1: 8시간
            8,   // Day 2: 8시간
            12,  // Day 3: 12시간
            12,  // Day 4: 12시간
            16,  // Day 5: 16시간
            20,  // Day 6: 20시간
            24   // Day 7: 24시간
        };

        #endregion

        #region Events

        public event Action<DailyLoginReward> OnDailyRewardAvailable;
        public event Action<DailyLoginReward> OnDailyRewardClaimed;
        public event Action<int> OnStreakUpdated;

        #endregion

        #region Properties

        public int CurrentStreak { get; private set; }
        public int TotalLoginDays { get; private set; }
        public bool HasClaimedToday { get; private set; }
        public DateTime LastLoginDate { get; private set; }
        public DateTime RewardExpiryTime { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1f;

        public bool IsRewardActive => DateTime.Now < RewardExpiryTime;
        public TimeSpan RewardTimeRemaining => IsRewardActive ? RewardExpiryTime - DateTime.Now : TimeSpan.Zero;

        #endregion

        #region Private Fields

        private readonly Data.PlayerData _playerData;

        #endregion

        #region Constructor

        public DailyLoginManager(Data.PlayerData playerData)
        {
            _playerData = playerData;
            LoadLoginData();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 앱 시작 시 호출 - 로그인 체크
        /// </summary>
        public void CheckDailyLogin()
        {
            DateTime today = DateTime.Now.Date;
            DateTime lastLogin = LastLoginDate.Date;

            // 이미 오늘 보상 받음
            if (HasClaimedToday && lastLogin == today)
            {
                Debug.Log($"[DailyLogin] Already claimed today. Streak: {CurrentStreak}");
                return;
            }

            // 어제 로그인했으면 스트릭 유지
            if (lastLogin == today.AddDays(-1))
            {
                // 스트릭 증가 (최대 7일)
                CurrentStreak = Mathf.Min(CurrentStreak + 1, 7);
                Debug.Log($"[DailyLogin] Streak continued! Day {CurrentStreak}");
            }
            // 오늘 첫 로그인 (스트릭 유지 중)
            else if (lastLogin == today)
            {
                Debug.Log($"[DailyLogin] Same day login. Streak: {CurrentStreak}");
            }
            // 이틀 이상 안 들어옴 - 스트릭 리셋
            else
            {
                CurrentStreak = 1;
                Debug.Log("[DailyLogin] Streak reset! Starting Day 1");
            }

            // 보상 알림
            HasClaimedToday = false;
            LastLoginDate = today;
            SaveLoginData();

            var reward = GetCurrentReward();
            OnDailyRewardAvailable?.Invoke(reward);
        }

        /// <summary>
        /// 보상 수령
        /// </summary>
        public DailyLoginReward ClaimReward()
        {
            if (HasClaimedToday)
            {
                Debug.LogWarning("[DailyLogin] Already claimed today!");
                return null;
            }

            var reward = GetCurrentReward();

            // 보상 적용
            CurrentMultiplier = reward.GoldMultiplier;
            RewardExpiryTime = DateTime.Now.AddHours(reward.DurationHours);

            // Day 7 보너스 칩
            if (reward.BonusChips > 0)
            {
                _playerData.chips += reward.BonusChips;
            }

            // 상태 업데이트
            HasClaimedToday = true;
            TotalLoginDays++;
            LastLoginDate = DateTime.Now.Date;
            SaveLoginData();

            Debug.Log($"[DailyLogin] Claimed Day {CurrentStreak} reward: {reward.GoldMultiplier}x for {reward.DurationHours}h");

            OnDailyRewardClaimed?.Invoke(reward);
            OnStreakUpdated?.Invoke(CurrentStreak);

            return reward;
        }

        /// <summary>
        /// 현재 보상 정보 반환
        /// </summary>
        public DailyLoginReward GetCurrentReward()
        {
            int dayIndex = Mathf.Clamp(CurrentStreak - 1, 0, 6);

            return new DailyLoginReward
            {
                Day = CurrentStreak,
                GoldMultiplier = DAILY_MULTIPLIERS[dayIndex],
                DurationHours = REWARD_DURATION_HOURS[dayIndex],
                BonusChips = CurrentStreak == 7 ? DAY7_BONUS_CHIPS : 0
            };
        }

        /// <summary>
        /// 전체 보상 목록 반환 (UI 표시용)
        /// </summary>
        public DailyLoginReward[] GetAllRewards()
        {
            var rewards = new DailyLoginReward[7];
            for (int i = 0; i < 7; i++)
            {
                rewards[i] = new DailyLoginReward
                {
                    Day = i + 1,
                    GoldMultiplier = DAILY_MULTIPLIERS[i],
                    DurationHours = REWARD_DURATION_HOURS[i],
                    BonusChips = i == 6 ? DAY7_BONUS_CHIPS : 0
                };
            }
            return rewards;
        }

        /// <summary>
        /// 현재 활성 골드 배율 반환
        /// </summary>
        public float GetActiveMultiplier()
        {
            if (IsRewardActive)
            {
                return CurrentMultiplier;
            }
            return 1f;
        }

        #endregion

        #region Save/Load

        private void LoadLoginData()
        {
            CurrentStreak = _playerData.loginStreak;
            TotalLoginDays = _playerData.totalLoginDays;
            HasClaimedToday = _playerData.hasClaimedDailyReward;
            CurrentMultiplier = _playerData.dailyMultiplier;

            if (!string.IsNullOrEmpty(_playerData.lastLoginDate))
            {
                DateTime.TryParse(_playerData.lastLoginDate, out DateTime parsed);
                LastLoginDate = parsed;
            }
            else
            {
                LastLoginDate = DateTime.MinValue;
            }

            if (!string.IsNullOrEmpty(_playerData.rewardExpiryTime))
            {
                DateTime.TryParse(_playerData.rewardExpiryTime, out DateTime parsed);
                RewardExpiryTime = parsed;
            }
            else
            {
                RewardExpiryTime = DateTime.MinValue;
            }

            Debug.Log($"[DailyLogin] Loaded: Streak={CurrentStreak}, LastLogin={LastLoginDate:yyyy-MM-dd}");
        }

        private void SaveLoginData()
        {
            _playerData.loginStreak = CurrentStreak;
            _playerData.totalLoginDays = TotalLoginDays;
            _playerData.hasClaimedDailyReward = HasClaimedToday;
            _playerData.lastLoginDate = LastLoginDate.ToString("o");
            _playerData.rewardExpiryTime = RewardExpiryTime.ToString("o");
            _playerData.dailyMultiplier = CurrentMultiplier;
        }

        #endregion
    }

    /// <summary>
    /// 일일 보상 데이터
    /// </summary>
    [Serializable]
    public class DailyLoginReward
    {
        public int Day;
        public float GoldMultiplier;
        public int DurationHours;
        public int BonusChips;

        public string GetDescription()
        {
            string desc = $"골드 {GoldMultiplier:F1}x ({DurationHours}시간)";
            if (BonusChips > 0)
            {
                desc += $"\n+ 보너스 칩 {BonusChips}개!";
            }
            return desc;
        }
    }
}
