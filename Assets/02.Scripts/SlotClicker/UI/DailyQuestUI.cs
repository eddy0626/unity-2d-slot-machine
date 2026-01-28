using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// 일일 퀘스트 UI
    /// - 퀘스트 목록 표시
    /// - 진행도 표시
    /// - 보상 수령 버튼
    /// </summary>
    public class DailyQuestUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("=== 패널 참조 ===")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _panelCanvasGroup;

        [Header("=== UI 요소 ===")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Transform _questContainer;
        [SerializeField] private Button _claimAllButton;
        [SerializeField] private TextMeshProUGUI _claimAllText;
        [SerializeField] private Button _closeButton;

        [Header("=== 퀘스트 버튼 (HUD) ===")]
        [SerializeField] private Button _questButton;
        [SerializeField] private TextMeshProUGUI _questButtonText;
        [SerializeField] private Image _questButtonNotification;

        #endregion

        #region Private Fields

        private DailyQuestManager _questManager;
        private Canvas _mainCanvas;
        private bool _isShowing = false;
        private List<QuestItemUI> _questItems = new List<QuestItemUI>();

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (_questManager != null)
            {
                _questManager.OnQuestProgress -= OnQuestProgress;
                _questManager.OnQuestCompleted -= OnQuestCompleted;
                _questManager.OnQuestsRefreshed -= OnQuestsRefreshed;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// UI 초기화
        /// </summary>
        public void Initialize(DailyQuestManager questManager, Canvas canvas)
        {
            _questManager = questManager;
            _mainCanvas = canvas;

            // 이벤트 구독
            _questManager.OnQuestProgress += OnQuestProgress;
            _questManager.OnQuestCompleted += OnQuestCompleted;
            _questManager.OnQuestsRefreshed += OnQuestsRefreshed;

            // UI 생성
            if (_panel == null)
            {
                CreateUI();
            }

            // 퀘스트 버튼 생성
            if (_questButton == null)
            {
                CreateQuestButton();
            }

            // 커스텀 폰트 적용
            if (_panel != null && FontManager.HasCustomFont)
            {
                FontManager.ApplyFontToAll(_panel);
            }
            if (_questButton != null && FontManager.HasCustomFont)
            {
                FontManager.ApplyFontToAll(_questButton.gameObject);
            }

            Hide();
            UpdateQuestButton();

            Debug.Log("[DailyQuestUI] Initialized");
        }

        /// <summary>
        /// UI 표시
        /// </summary>
        public void Show()
        {
            if (_panel == null) return;

            _isShowing = true;
            _panel.SetActive(true);

            UpdateQuestList();
            UpdateClaimAllButton();

            // 애니메이션
            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 0f;
                _panelCanvasGroup.DOFade(1f, 0.3f);
            }

            _panel.transform.localScale = Vector3.one * 0.8f;
            _panel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// UI 숨김
        /// </summary>
        public void Hide()
        {
            if (_panel == null) return;

            _isShowing = false;

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.DOFade(0f, 0.2f).OnComplete(() =>
                {
                    _panel.SetActive(false);
                });
            }
            else
            {
                _panel.SetActive(false);
            }
        }

        /// <summary>
        /// 토글
        /// </summary>
        public void Toggle()
        {
            if (_isShowing)
                Hide();
            else
                Show();
        }

        #endregion

        #region Private Methods - UI Creation

        private void CreateUI()
        {
            if (_mainCanvas == null) return;

            // 패널 생성
            GameObject panelObj = new GameObject("DailyQuestPanel");
            panelObj.transform.SetParent(_mainCanvas.transform, false);
            _panel = panelObj;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            Image bgImage = panelObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.85f);

            Button bgButton = panelObj.AddComponent<Button>();
            bgButton.onClick.AddListener(Hide);

            _panelCanvasGroup = panelObj.AddComponent<CanvasGroup>();

            // 컨테이너
            GameObject container = new GameObject("Container");
            container.transform.SetParent(panelObj.transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(360, 420);

            Image containerBg = container.AddComponent<Image>();
            containerBg.color = new Color(0.12f, 0.08f, 0.2f, 1f);

            Button containerBtn = container.AddComponent<Button>();
            containerBtn.onClick.AddListener(() => { });

            // 타이틀
            _titleText = CreateText(container.transform, "Title", "일일 퀘스트", 26,
                FontStyles.Bold, new Vector2(0, 175), new Vector2(320, 40));
            _titleText.color = new Color(1f, 0.85f, 0.3f);

            // 퀘스트 컨테이너
            GameObject questContainerObj = new GameObject("QuestContainer");
            questContainerObj.transform.SetParent(container.transform, false);

            RectTransform questContainerRect = questContainerObj.AddComponent<RectTransform>();
            questContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            questContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            questContainerRect.sizeDelta = new Vector2(340, 260);
            questContainerRect.anchoredPosition = new Vector2(0, 20);

            _questContainer = questContainerObj.transform;

            // 모두 수령 버튼
            CreateClaimAllButton(container.transform);

            // 닫기 버튼
            CreateCloseButton(container.transform);
        }

        private void CreateQuestButton()
        {
            if (_mainCanvas == null) return;

            GameObject btnObj = new GameObject("QuestButton");
            btnObj.transform.SetParent(_mainCanvas.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(0, 1);
            btnRect.sizeDelta = new Vector2(80, 36);
            btnRect.anchoredPosition = new Vector2(50, -100);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.5f, 0.8f);

            _questButton = btnObj.AddComponent<Button>();
            _questButton.targetGraphic = btnImage;
            _questButton.onClick.AddListener(Toggle);

            _questButtonText = CreateText(btnObj.transform, "Text", "퀘스트", 14,
                FontStyles.Bold, Vector2.zero, new Vector2(80, 36));
            _questButtonText.alignment = TextAlignmentOptions.Center;

            // 알림 뱃지
            GameObject notifObj = new GameObject("Notification");
            notifObj.transform.SetParent(btnObj.transform, false);

            RectTransform notifRect = notifObj.AddComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(1, 1);
            notifRect.anchorMax = new Vector2(1, 1);
            notifRect.sizeDelta = new Vector2(20, 20);
            notifRect.anchoredPosition = new Vector2(-5, -5);

            _questButtonNotification = notifObj.AddComponent<Image>();
            _questButtonNotification.color = new Color(1f, 0.3f, 0.3f);
            notifObj.SetActive(false);
        }

        private void CreateClaimAllButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ClaimAllButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(200, 45);
            btnRect.anchoredPosition = new Vector2(0, -160);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.7f, 0.3f);

            _claimAllButton = btnObj.AddComponent<Button>();
            _claimAllButton.targetGraphic = btnImage;
            _claimAllButton.onClick.AddListener(OnClaimAllClicked);

            _claimAllText = CreateText(btnObj.transform, "Text", "모두 수령", 18,
                FontStyles.Bold, Vector2.zero, new Vector2(200, 45));
            _claimAllText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.sizeDelta = new Vector2(36, 36);
            btnRect.anchoredPosition = new Vector2(-8, -8);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.7f, 0.25f, 0.25f);

            _closeButton = btnObj.AddComponent<Button>();
            _closeButton.targetGraphic = btnImage;
            _closeButton.onClick.AddListener(Hide);

            var closeText = CreateText(btnObj.transform, "X", "X", 20,
                FontStyles.Bold, Vector2.zero, new Vector2(36, 36));
            closeText.alignment = TextAlignmentOptions.Center;
        }

        private QuestItemUI CreateQuestItem(Transform parent, int index)
        {
            GameObject itemObj = new GameObject($"QuestItem_{index}");
            itemObj.transform.SetParent(parent, false);

            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0.5f, 1);
            itemRect.anchorMax = new Vector2(0.5f, 1);
            itemRect.sizeDelta = new Vector2(320, 75);
            itemRect.anchoredPosition = new Vector2(0, -45 - index * 85);

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.2f, 0.15f, 0.3f, 0.9f);

            QuestItemUI item = new QuestItemUI();
            item.Root = itemObj;
            item.Background = itemBg;

            // 난이도 표시
            item.DifficultyText = CreateText(itemObj.transform, "Difficulty", "Easy", 11,
                FontStyles.Bold, new Vector2(-130, 25), new Vector2(60, 20));

            // 설명
            item.DescriptionText = CreateText(itemObj.transform, "Desc", "퀘스트 설명", 15,
                FontStyles.Normal, new Vector2(-20, 22), new Vector2(200, 25));
            item.DescriptionText.alignment = TextAlignmentOptions.Left;

            // 진행도
            item.ProgressText = CreateText(itemObj.transform, "Progress", "0/100", 13,
                FontStyles.Normal, new Vector2(-20, -2), new Vector2(200, 20));
            item.ProgressText.alignment = TextAlignmentOptions.Left;
            item.ProgressText.color = new Color(0.7f, 0.7f, 0.7f);

            // 진행바 배경
            GameObject barBgObj = new GameObject("BarBg");
            barBgObj.transform.SetParent(itemObj.transform, false);

            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRect.sizeDelta = new Vector2(200, 8);
            barBgRect.anchoredPosition = new Vector2(-20, -22);

            Image barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.1f, 0.1f, 0.1f);

            // 진행바
            GameObject barObj = new GameObject("Bar");
            barObj.transform.SetParent(barBgObj.transform, false);

            RectTransform barRect = barObj.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(0, 1);
            barRect.sizeDelta = new Vector2(0, 0);
            barRect.anchoredPosition = Vector2.zero;
            barRect.pivot = new Vector2(0, 0.5f);

            item.ProgressBar = barObj.AddComponent<Image>();
            item.ProgressBar.color = new Color(0.3f, 0.7f, 0.3f);
            item.ProgressBarRect = barRect;

            // 보상 텍스트
            item.RewardText = CreateText(itemObj.transform, "Reward", "1000 Gold", 12,
                FontStyles.Normal, new Vector2(120, 0), new Vector2(80, 40));
            item.RewardText.alignment = TextAlignmentOptions.Right;
            item.RewardText.color = new Color(1f, 0.85f, 0.3f);

            // 수령 버튼
            GameObject claimBtnObj = new GameObject("ClaimBtn");
            claimBtnObj.transform.SetParent(itemObj.transform, false);

            RectTransform claimRect = claimBtnObj.AddComponent<RectTransform>();
            claimRect.anchorMin = new Vector2(1, 0.5f);
            claimRect.anchorMax = new Vector2(1, 0.5f);
            claimRect.sizeDelta = new Vector2(55, 30);
            claimRect.anchoredPosition = new Vector2(-35, 0);

            Image claimImage = claimBtnObj.AddComponent<Image>();
            claimImage.color = new Color(0.3f, 0.6f, 0.3f);

            item.ClaimButton = claimBtnObj.AddComponent<Button>();
            item.ClaimButton.targetGraphic = claimImage;
            item.ClaimButtonImage = claimImage;

            item.ClaimButtonText = CreateText(claimBtnObj.transform, "Text", "수령", 12,
                FontStyles.Bold, Vector2.zero, new Vector2(55, 30));
            item.ClaimButtonText.alignment = TextAlignmentOptions.Center;

            return item;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            float fontSize, FontStyles style, Vector2 position, Vector2 size)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = size;
            textRect.anchoredPosition = position;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        #endregion

        #region Private Methods - Updates

        private void UpdateQuestList()
        {
            if (_questManager == null || _questContainer == null) return;

            // 기존 아이템 정리
            foreach (var item in _questItems)
            {
                if (item.Root != null)
                    Destroy(item.Root);
            }
            _questItems.Clear();

            // 퀘스트 아이템 생성
            var quests = _questManager.ActiveQuests;
            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                var item = CreateQuestItem(_questContainer, i);
                _questItems.Add(item);

                UpdateQuestItem(item, quest, i);
            }
        }

        private void UpdateQuestItem(QuestItemUI item, DailyQuest quest, int index)
        {
            if (item == null || quest == null) return;

            // 난이도
            string diffText = quest.Difficulty switch
            {
                DailyQuestManager.QuestDifficulty.Easy => "쉬움",
                DailyQuestManager.QuestDifficulty.Medium => "보통",
                DailyQuestManager.QuestDifficulty.Hard => "어려움",
                _ => ""
            };
            item.DifficultyText.text = diffText;
            item.DifficultyText.color = quest.GetDifficultyColor();

            // 설명
            item.DescriptionText.text = quest.Description;

            // 진행도
            item.ProgressText.text = quest.GetProgressText();

            // 진행바
            float progress = quest.ProgressPercent;
            item.ProgressBarRect.anchorMax = new Vector2(progress, 1);

            if (quest.IsCompleted)
            {
                item.ProgressBar.color = new Color(0.3f, 0.8f, 0.3f);
            }
            else
            {
                item.ProgressBar.color = new Color(0.4f, 0.6f, 0.9f);
            }

            // 보상
            item.RewardText.text = quest.GetRewardText();

            // 수령 버튼
            if (quest.IsRewardClaimed)
            {
                item.ClaimButtonText.text = "완료";
                item.ClaimButtonImage.color = new Color(0.4f, 0.4f, 0.4f);
                item.ClaimButton.interactable = false;
            }
            else if (quest.IsCompleted)
            {
                item.ClaimButtonText.text = "수령";
                item.ClaimButtonImage.color = new Color(0.3f, 0.7f, 0.3f);
                item.ClaimButton.interactable = true;

                // 버튼 클릭 이벤트
                int questIndex = index;
                item.ClaimButton.onClick.RemoveAllListeners();
                item.ClaimButton.onClick.AddListener(() => OnClaimClicked(questIndex));

                // 펄스 애니메이션
                item.ClaimButton.transform.DOScale(1.1f, 0.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                item.ClaimButtonText.text = "진행중";
                item.ClaimButtonImage.color = new Color(0.5f, 0.5f, 0.5f);
                item.ClaimButton.interactable = false;
            }

            // 배경 색상
            if (quest.IsRewardClaimed)
            {
                item.Background.color = new Color(0.15f, 0.2f, 0.15f, 0.9f);
            }
            else if (quest.IsCompleted)
            {
                item.Background.color = new Color(0.2f, 0.25f, 0.15f, 0.9f);
            }
            else
            {
                item.Background.color = new Color(0.2f, 0.15f, 0.3f, 0.9f);
            }
        }

        private void UpdateClaimAllButton()
        {
            if (_claimAllButton == null || _questManager == null) return;

            var (gold, chips) = _questManager.GetClaimableRewards();
            bool hasClaimable = gold > 0 || chips > 0;

            _claimAllButton.interactable = hasClaimable;

            if (hasClaimable)
            {
                _claimAllText.text = $"모두 수령";
                _claimAllButton.GetComponent<Image>().color = new Color(0.3f, 0.7f, 0.3f);
            }
            else
            {
                _claimAllText.text = "수령 가능한 보상 없음";
                _claimAllButton.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);
            }
        }

        private void UpdateQuestButton()
        {
            if (_questButtonNotification == null || _questManager == null) return;

            // 수령 가능한 보상이 있으면 알림 표시
            var (gold, chips) = _questManager.GetClaimableRewards();
            bool hasClaimable = gold > 0 || chips > 0;

            _questButtonNotification.gameObject.SetActive(hasClaimable);

            // 진행도 표시
            if (_questButtonText != null)
            {
                int completed = _questManager.CompletedCount;
                int total = _questManager.ActiveQuests.Count;
                _questButtonText.text = $"퀘스트\n{completed}/{total}";
            }
        }

        #endregion

        #region Event Handlers

        private void OnQuestProgress(DailyQuest quest)
        {
            if (_isShowing)
            {
                int index = _questManager.ActiveQuests.IndexOf(quest);
                if (index >= 0 && index < _questItems.Count)
                {
                    UpdateQuestItem(_questItems[index], quest, index);
                }
            }
            UpdateQuestButton();
        }

        private void OnQuestCompleted(DailyQuest quest)
        {
            if (_isShowing)
            {
                int index = _questManager.ActiveQuests.IndexOf(quest);
                if (index >= 0 && index < _questItems.Count)
                {
                    UpdateQuestItem(_questItems[index], quest, index);

                    // 완료 애니메이션
                    _questItems[index].Root.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
                }
                UpdateClaimAllButton();
            }
            UpdateQuestButton();
        }

        private void OnQuestsRefreshed()
        {
            if (_isShowing)
            {
                UpdateQuestList();
                UpdateClaimAllButton();
            }
            UpdateQuestButton();
        }

        private void OnClaimClicked(int questIndex)
        {
            if (_questManager == null) return;

            var quests = _questManager.ActiveQuests;
            if (questIndex >= 0 && questIndex < quests.Count)
            {
                var quest = quests[questIndex];
                if (_questManager.ClaimReward(quest))
                {
                    UpdateQuestItem(_questItems[questIndex], quest, questIndex);
                    UpdateClaimAllButton();
                    UpdateQuestButton();

                    // 수령 애니메이션
                    _questItems[questIndex].ClaimButton.transform.DOKill();
                    _questItems[questIndex].ClaimButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                }
            }
        }

        private void OnClaimAllClicked()
        {
            if (_questManager == null) return;

            int claimed = _questManager.ClaimAllRewards();
            if (claimed > 0)
            {
                UpdateQuestList();
                UpdateClaimAllButton();
                UpdateQuestButton();

                // 버튼 애니메이션
                _claimAllButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }
        }

        #endregion

        #region Helper Classes

        private class QuestItemUI
        {
            public GameObject Root;
            public Image Background;
            public TextMeshProUGUI DifficultyText;
            public TextMeshProUGUI DescriptionText;
            public TextMeshProUGUI ProgressText;
            public Image ProgressBar;
            public RectTransform ProgressBarRect;
            public TextMeshProUGUI RewardText;
            public Button ClaimButton;
            public Image ClaimButtonImage;
            public TextMeshProUGUI ClaimButtonText;
        }

        #endregion
    }
}
