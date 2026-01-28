using System;
using System.Collections.Generic;
using UnityEngine;
using SlotClicker.Data;

namespace SlotClicker.Core
{
    /// <summary>
    /// 일일 퀘스트 시스템
    /// - 매일 3개의 퀘스트 제공
    /// - 자정에 리셋
    /// - 완료 시 보상 지급
    /// </summary>
    public class DailyQuestManager
    {
        #region Quest Definitions

        // 퀘스트 타입
        public enum QuestType
        {
            Click,      // 클릭 N회
            Spin,       // 스핀 N회
            EarnGold,   // 골드 N 획득
            WinSpin,    // 스핀 승리 N회
            Critical,   // 크리티컬 N회
            Jackpot     // 잭팟 N회
        }

        // 퀘스트 난이도
        public enum QuestDifficulty
        {
            Easy,
            Medium,
            Hard
        }

        // 퀘스트 템플릿
        private static readonly QuestTemplate[] QUEST_TEMPLATES = new QuestTemplate[]
        {
            // Easy
            new QuestTemplate(QuestType.Click, QuestDifficulty.Easy, 100, "클릭 100회", 1000, 0),
            new QuestTemplate(QuestType.Spin, QuestDifficulty.Easy, 10, "슬롯 10회 돌리기", 2000, 0),
            new QuestTemplate(QuestType.EarnGold, QuestDifficulty.Easy, 10000, "골드 10K 획득", 1500, 0),
            new QuestTemplate(QuestType.WinSpin, QuestDifficulty.Easy, 3, "슬롯 3회 승리", 2500, 0),

            // Medium
            new QuestTemplate(QuestType.Click, QuestDifficulty.Medium, 500, "클릭 500회", 5000, 1),
            new QuestTemplate(QuestType.Spin, QuestDifficulty.Medium, 30, "슬롯 30회 돌리기", 8000, 1),
            new QuestTemplate(QuestType.EarnGold, QuestDifficulty.Medium, 50000, "골드 50K 획득", 6000, 1),
            new QuestTemplate(QuestType.Critical, QuestDifficulty.Medium, 10, "크리티컬 10회", 7000, 1),
            new QuestTemplate(QuestType.WinSpin, QuestDifficulty.Medium, 10, "슬롯 10회 승리", 10000, 1),

            // Hard
            new QuestTemplate(QuestType.Click, QuestDifficulty.Hard, 1000, "클릭 1000회", 15000, 2),
            new QuestTemplate(QuestType.Spin, QuestDifficulty.Hard, 50, "슬롯 50회 돌리기", 20000, 2),
            new QuestTemplate(QuestType.EarnGold, QuestDifficulty.Hard, 200000, "골드 200K 획득", 25000, 2),
            new QuestTemplate(QuestType.Jackpot, QuestDifficulty.Hard, 1, "잭팟 1회 달성", 50000, 3),
            new QuestTemplate(QuestType.Critical, QuestDifficulty.Hard, 30, "크리티컬 30회", 30000, 2),
        };

        #endregion

        #region Events

        public event Action<DailyQuest> OnQuestProgress;
        public event Action<DailyQuest> OnQuestCompleted;
        public event Action OnQuestsRefreshed;
        public event Action OnAllQuestsCompleted;

        #endregion

        #region Properties

        public List<DailyQuest> ActiveQuests { get; private set; } = new List<DailyQuest>();
        public DateTime LastRefreshDate { get; private set; }
        public bool AllQuestsCompleted => ActiveQuests.TrueForAll(q => q.IsCompleted);
        public int CompletedCount => ActiveQuests.FindAll(q => q.IsCompleted).Count;

        #endregion

        #region Private Fields

        private readonly PlayerData _playerData;
        private readonly GameManager _gameManager;
        private System.Random _random;

        #endregion

        #region Constructor

        public DailyQuestManager(PlayerData playerData, GameManager gameManager)
        {
            _playerData = playerData;
            _gameManager = gameManager;
            _random = new System.Random(DateTime.Now.DayOfYear);

            LoadQuestData();
            CheckDailyReset();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 퀘스트 진행도 업데이트
        /// </summary>
        public void UpdateProgress(QuestType type, int amount = 1)
        {
            foreach (var quest in ActiveQuests)
            {
                if (quest.Type == type && !quest.IsCompleted)
                {
                    quest.CurrentProgress += amount;

                    if (quest.CurrentProgress >= quest.TargetAmount)
                    {
                        quest.CurrentProgress = quest.TargetAmount;
                        quest.IsCompleted = true;
                        quest.CompletedTime = DateTime.Now;

                        OnQuestCompleted?.Invoke(quest);
                        Debug.Log($"[DailyQuest] Quest completed: {quest.Description}");

                        // 모든 퀘스트 완료 체크
                        if (AllQuestsCompleted)
                        {
                            OnAllQuestsCompleted?.Invoke();
                        }
                    }
                    else
                    {
                        OnQuestProgress?.Invoke(quest);
                    }

                    SaveQuestData();
                }
            }
        }

        /// <summary>
        /// 퀘스트 보상 수령
        /// </summary>
        public bool ClaimReward(DailyQuest quest)
        {
            if (!quest.IsCompleted || quest.IsRewardClaimed)
            {
                return false;
            }

            // 보상 지급
            if (quest.GoldReward > 0)
            {
                _gameManager.Gold.AddGold(quest.GoldReward);
            }

            if (quest.ChipReward > 0)
            {
                _playerData.chips += quest.ChipReward;
            }

            quest.IsRewardClaimed = true;
            SaveQuestData();

            Debug.Log($"[DailyQuest] Reward claimed: {quest.GoldReward} gold, {quest.ChipReward} chips");
            return true;
        }

        /// <summary>
        /// 모든 완료된 퀘스트 보상 일괄 수령
        /// </summary>
        public int ClaimAllRewards()
        {
            int claimed = 0;
            foreach (var quest in ActiveQuests)
            {
                if (quest.IsCompleted && !quest.IsRewardClaimed)
                {
                    if (ClaimReward(quest))
                    {
                        claimed++;
                    }
                }
            }
            return claimed;
        }

        /// <summary>
        /// 일일 리셋 체크
        /// </summary>
        public void CheckDailyReset()
        {
            DateTime today = DateTime.Now.Date;

            if (LastRefreshDate.Date < today)
            {
                RefreshQuests();
            }
        }

        /// <summary>
        /// 퀘스트 새로고침 (매일 자정)
        /// </summary>
        public void RefreshQuests()
        {
            ActiveQuests.Clear();

            // 난이도별 1개씩 선택
            var easyQuests = GetQuestsByDifficulty(QuestDifficulty.Easy);
            var mediumQuests = GetQuestsByDifficulty(QuestDifficulty.Medium);
            var hardQuests = GetQuestsByDifficulty(QuestDifficulty.Hard);

            if (easyQuests.Count > 0)
                ActiveQuests.Add(CreateQuest(easyQuests[_random.Next(easyQuests.Count)]));

            if (mediumQuests.Count > 0)
                ActiveQuests.Add(CreateQuest(mediumQuests[_random.Next(mediumQuests.Count)]));

            if (hardQuests.Count > 0)
                ActiveQuests.Add(CreateQuest(hardQuests[_random.Next(hardQuests.Count)]));

            LastRefreshDate = DateTime.Now.Date;
            SaveQuestData();

            OnQuestsRefreshed?.Invoke();
            Debug.Log($"[DailyQuest] Quests refreshed: {ActiveQuests.Count} quests");
        }

        /// <summary>
        /// 총 보상 반환
        /// </summary>
        public (double gold, int chips) GetTotalRewards()
        {
            double gold = 0;
            int chips = 0;

            foreach (var quest in ActiveQuests)
            {
                gold += quest.GoldReward;
                chips += quest.ChipReward;
            }

            return (gold, chips);
        }

        /// <summary>
        /// 수령 가능한 보상 반환
        /// </summary>
        public (double gold, int chips) GetClaimableRewards()
        {
            double gold = 0;
            int chips = 0;

            foreach (var quest in ActiveQuests)
            {
                if (quest.IsCompleted && !quest.IsRewardClaimed)
                {
                    gold += quest.GoldReward;
                    chips += quest.ChipReward;
                }
            }

            return (gold, chips);
        }

        #endregion

        #region Private Methods

        private List<QuestTemplate> GetQuestsByDifficulty(QuestDifficulty difficulty)
        {
            var result = new List<QuestTemplate>();
            foreach (var template in QUEST_TEMPLATES)
            {
                if (template.Difficulty == difficulty)
                {
                    result.Add(template);
                }
            }
            return result;
        }

        private DailyQuest CreateQuest(QuestTemplate template)
        {
            return new DailyQuest
            {
                Type = template.Type,
                Difficulty = template.Difficulty,
                Description = template.Description,
                TargetAmount = template.TargetAmount,
                CurrentProgress = 0,
                GoldReward = template.GoldReward,
                ChipReward = template.ChipReward,
                IsCompleted = false,
                IsRewardClaimed = false,
                CreatedTime = DateTime.Now
            };
        }

        private void LoadQuestData()
        {
            // PlayerData에서 퀘스트 로드
            if (_playerData.dailyQuests != null && _playerData.dailyQuests.Count > 0)
            {
                ActiveQuests = new List<DailyQuest>(_playerData.dailyQuests);
            }

            if (!string.IsNullOrEmpty(_playerData.questRefreshDate))
            {
                DateTime.TryParse(_playerData.questRefreshDate, out DateTime parsed);
                LastRefreshDate = parsed;
            }

            Debug.Log($"[DailyQuest] Loaded {ActiveQuests.Count} quests");
        }

        private void SaveQuestData()
        {
            _playerData.dailyQuests = new List<DailyQuest>(ActiveQuests);
            _playerData.questRefreshDate = LastRefreshDate.ToString("o");
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// 퀘스트 템플릿
    /// </summary>
    public class QuestTemplate
    {
        public DailyQuestManager.QuestType Type;
        public DailyQuestManager.QuestDifficulty Difficulty;
        public int TargetAmount;
        public string Description;
        public double GoldReward;
        public int ChipReward;

        public QuestTemplate(DailyQuestManager.QuestType type, DailyQuestManager.QuestDifficulty difficulty,
            int target, string desc, double gold, int chips)
        {
            Type = type;
            Difficulty = difficulty;
            TargetAmount = target;
            Description = desc;
            GoldReward = gold;
            ChipReward = chips;
        }
    }

    /// <summary>
    /// 활성 퀘스트 데이터
    /// </summary>
    [Serializable]
    public class DailyQuest
    {
        public DailyQuestManager.QuestType Type;
        public DailyQuestManager.QuestDifficulty Difficulty;
        public string Description;
        public int TargetAmount;
        public int CurrentProgress;
        public double GoldReward;
        public int ChipReward;
        public bool IsCompleted;
        public bool IsRewardClaimed;
        public DateTime CreatedTime;
        public DateTime CompletedTime;

        public float ProgressPercent => TargetAmount > 0 ? (float)CurrentProgress / TargetAmount : 0f;

        public string GetProgressText()
        {
            return $"{CurrentProgress}/{TargetAmount}";
        }

        public string GetRewardText()
        {
            string text = "";
            if (GoldReward > 0)
                text += $"{GoldManager.FormatNumber(GoldReward)} Gold";
            if (ChipReward > 0)
            {
                if (!string.IsNullOrEmpty(text)) text += " + ";
                text += $"{ChipReward} Chip";
            }
            return text;
        }

        public Color GetDifficultyColor()
        {
            return Difficulty switch
            {
                DailyQuestManager.QuestDifficulty.Easy => new Color(0.5f, 0.8f, 0.5f),
                DailyQuestManager.QuestDifficulty.Medium => new Color(0.9f, 0.7f, 0.3f),
                DailyQuestManager.QuestDifficulty.Hard => new Color(0.9f, 0.4f, 0.4f),
                _ => Color.white
            };
        }
    }

    #endregion
}
