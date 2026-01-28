using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// 일일 로그인 보상 UI
    /// - 7일 스트릭 표시
    /// - 보상 수령 버튼
    /// - 남은 시간 표시
    /// </summary>
    public class DailyLoginUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("=== 패널 참조 ===")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private CanvasGroup _panelCanvasGroup;

        [Header("=== UI 요소 ===")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TextMeshProUGUI _claimButtonText;
        [SerializeField] private Button _closeButton;

        [Header("=== 스트릭 표시 ===")]
        [SerializeField] private Transform _streakContainer;
        [SerializeField] private Image[] _dayIcons;
        [SerializeField] private TextMeshProUGUI[] _dayTexts;

        [Header("=== 상태 표시 ===")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Image _statusIcon;

        [Header("=== 색상 설정 ===")]
        [SerializeField] private Color _completedColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color _currentColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f);

        #endregion

        #region Private Fields

        private DailyLoginManager _loginManager;
        private Canvas _mainCanvas;
        private bool _isShowing = false;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            // 보상 남은 시간 업데이트
            if (_isShowing && _loginManager != null && _loginManager.IsRewardActive)
            {
                UpdateRemainingTime();
            }
        }

        private void OnDestroy()
        {
            if (_loginManager != null)
            {
                _loginManager.OnDailyRewardAvailable -= OnRewardAvailable;
                _loginManager.OnDailyRewardClaimed -= OnRewardClaimed;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// UI 초기화
        /// </summary>
        public void Initialize(DailyLoginManager loginManager, Canvas canvas)
        {
            _loginManager = loginManager;
            _mainCanvas = canvas;

            // 이벤트 구독
            _loginManager.OnDailyRewardAvailable += OnRewardAvailable;
            _loginManager.OnDailyRewardClaimed += OnRewardClaimed;

            // 버튼 이벤트
            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(OnClaimButtonClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }

            // UI 생성
            if (_panel == null)
            {
                CreateUI();
            }

            // 커스텀 폰트 적용
            if (_panel != null && FontManager.HasCustomFont)
            {
                FontManager.ApplyFontToAll(_panel);
            }

            Hide();

            Debug.Log("[DailyLoginUI] Initialized");
        }

        /// <summary>
        /// UI 표시
        /// </summary>
        public void Show()
        {
            if (_panel == null) return;

            _isShowing = true;
            _panel.SetActive(true);

            UpdateUI();

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

        #endregion

        #region Private Methods

        private void CreateUI()
        {
            if (_mainCanvas == null) return;

            // 패널 생성
            GameObject panelObj = new GameObject("DailyLoginPanel");
            panelObj.transform.SetParent(_mainCanvas.transform, false);

            _panel = panelObj;

            // RectTransform 설정 (전체 화면)
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // 배경 (반투명 검정)
            Image bgImage = panelObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.85f);

            // 버튼으로 배경 클릭 시 닫기
            Button bgButton = panelObj.AddComponent<Button>();
            bgButton.onClick.AddListener(Hide);

            // CanvasGroup
            _panelCanvasGroup = panelObj.AddComponent<CanvasGroup>();

            // 내부 컨테이너
            GameObject container = new GameObject("Container");
            container.transform.SetParent(panelObj.transform, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(340, 480);
            containerRect.anchoredPosition = Vector2.zero;

            Image containerBg = container.AddComponent<Image>();
            containerBg.color = new Color(0.15f, 0.1f, 0.25f, 1f);

            // 컨테이너 클릭 시 닫히지 않도록
            Button containerBtn = container.AddComponent<Button>();
            containerBtn.onClick.AddListener(() => { }); // 빈 리스너

            // 타이틀
            _titleText = CreateText(container.transform, "TitleText",
                "일일 로그인 보상", 28, FontStyles.Bold,
                new Vector2(0, 200), new Vector2(300, 50));
            _titleText.color = _currentColor;

            // 설명
            _descriptionText = CreateText(container.transform, "DescText",
                "연속 로그인으로 더 큰 보상을 받으세요!", 16, FontStyles.Normal,
                new Vector2(0, 160), new Vector2(300, 30));

            // 스트릭 표시 (7일)
            CreateStreakDisplay(container.transform);

            // 보상 텍스트
            _rewardText = CreateText(container.transform, "RewardText",
                "오늘의 보상", 20, FontStyles.Bold,
                new Vector2(0, -60), new Vector2(300, 80));

            // 상태 텍스트
            _statusText = CreateText(container.transform, "StatusText",
                "", 14, FontStyles.Normal,
                new Vector2(0, -130), new Vector2(300, 30));
            _statusText.color = new Color(0.7f, 0.7f, 0.7f);

            // 수령 버튼
            CreateClaimButton(container.transform);

            // 닫기 버튼
            CreateCloseButton(container.transform);

            Debug.Log("[DailyLoginUI] UI Created");
        }

        private void CreateStreakDisplay(Transform parent)
        {
            GameObject streakObj = new GameObject("StreakContainer");
            streakObj.transform.SetParent(parent, false);

            RectTransform streakRect = streakObj.AddComponent<RectTransform>();
            streakRect.anchorMin = new Vector2(0.5f, 0.5f);
            streakRect.anchorMax = new Vector2(0.5f, 0.5f);
            streakRect.sizeDelta = new Vector2(320, 80);
            streakRect.anchoredPosition = new Vector2(0, 80);

            _streakContainer = streakObj.transform;
            _dayIcons = new Image[7];
            _dayTexts = new TextMeshProUGUI[7];

            float startX = -140f;
            float spacing = 46f;

            for (int i = 0; i < 7; i++)
            {
                // 일별 아이콘
                GameObject dayObj = new GameObject($"Day{i + 1}");
                dayObj.transform.SetParent(streakObj.transform, false);

                RectTransform dayRect = dayObj.AddComponent<RectTransform>();
                dayRect.anchorMin = new Vector2(0.5f, 0.5f);
                dayRect.anchorMax = new Vector2(0.5f, 0.5f);
                dayRect.sizeDelta = new Vector2(40, 40);
                dayRect.anchoredPosition = new Vector2(startX + i * spacing, 10);

                Image dayImage = dayObj.AddComponent<Image>();
                dayImage.color = _lockedColor;
                _dayIcons[i] = dayImage;

                // 일 텍스트
                _dayTexts[i] = CreateText(dayObj.transform, "DayText",
                    $"D{i + 1}", 12, FontStyles.Bold,
                    Vector2.zero, new Vector2(40, 40));
                _dayTexts[i].alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateClaimButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ClaimButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(200, 50);
            btnRect.anchoredPosition = new Vector2(0, -180);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = _currentColor;

            _claimButton = btnObj.AddComponent<Button>();
            _claimButton.targetGraphic = btnImage;
            _claimButton.onClick.AddListener(OnClaimButtonClicked);

            _claimButtonText = CreateText(btnObj.transform, "BtnText",
                "보상 받기", 20, FontStyles.Bold,
                Vector2.zero, new Vector2(200, 50));
            _claimButtonText.alignment = TextAlignmentOptions.Center;
            _claimButtonText.color = Color.white;
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 1f);
            btnRect.anchorMax = new Vector2(1f, 1f);
            btnRect.sizeDelta = new Vector2(40, 40);
            btnRect.anchoredPosition = new Vector2(-10, -10);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.8f, 0.3f, 0.3f);

            _closeButton = btnObj.AddComponent<Button>();
            _closeButton.targetGraphic = btnImage;
            _closeButton.onClick.AddListener(Hide);

            TextMeshProUGUI closeText = CreateText(btnObj.transform, "CloseText",
                "X", 20, FontStyles.Bold,
                Vector2.zero, new Vector2(40, 40));
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;
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

        private void UpdateUI()
        {
            if (_loginManager == null) return;

            var reward = _loginManager.GetCurrentReward();
            int currentDay = _loginManager.CurrentStreak;
            bool claimed = _loginManager.HasClaimedToday;

            // 스트릭 표시 업데이트
            for (int i = 0; i < 7 && i < _dayIcons.Length; i++)
            {
                if (i < currentDay - 1)
                {
                    // 완료된 일
                    _dayIcons[i].color = _completedColor;
                }
                else if (i == currentDay - 1)
                {
                    // 오늘
                    _dayIcons[i].color = claimed ? _completedColor : _currentColor;

                    // 펄스 애니메이션
                    if (!claimed)
                    {
                        _dayIcons[i].transform.DOScale(1.2f, 0.5f)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);
                    }
                }
                else
                {
                    // 미래 일
                    _dayIcons[i].color = _lockedColor;
                }
            }

            // 보상 텍스트
            if (_rewardText != null)
            {
                if (claimed)
                {
                    _rewardText.text = $"<color=#80FF80>오늘 보상 수령 완료!</color>\n" +
                        $"골드 <color=yellow>{reward.GoldMultiplier:F1}x</color> 적용 중";
                }
                else
                {
                    string rewardDesc = $"Day {currentDay} 보상\n" +
                        $"골드 <color=yellow>{reward.GoldMultiplier:F1}x</color> ({reward.DurationHours}시간)";

                    if (reward.BonusChips > 0)
                    {
                        rewardDesc += $"\n+ <color=cyan>보너스 칩 {reward.BonusChips}개!</color>";
                    }

                    _rewardText.text = rewardDesc;
                }
            }

            // 버튼 상태
            if (_claimButton != null && _claimButtonText != null)
            {
                _claimButton.interactable = !claimed;
                _claimButtonText.text = claimed ? "수령 완료" : "보상 받기";

                Image btnImage = _claimButton.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = claimed ? _lockedColor : _currentColor;
                }
            }

            // 상태 텍스트
            UpdateRemainingTime();
        }

        private void UpdateRemainingTime()
        {
            if (_statusText == null || _loginManager == null) return;

            if (_loginManager.IsRewardActive)
            {
                TimeSpan remaining = _loginManager.RewardTimeRemaining;
                _statusText.text = $"보상 남은 시간: {remaining.Hours}시간 {remaining.Minutes}분";
            }
            else if (_loginManager.HasClaimedToday)
            {
                _statusText.text = "내일 다시 로그인하세요!";
            }
            else
            {
                _statusText.text = "보상을 받으세요!";
            }
        }

        #endregion

        #region Event Handlers

        private void OnRewardAvailable(DailyLoginReward reward)
        {
            // 보상 수령 가능 시 자동으로 표시
            Show();
        }

        private void OnRewardClaimed(DailyLoginReward reward)
        {
            // 수령 완료 애니메이션
            if (_claimButton != null)
            {
                _claimButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }

            // UI 업데이트
            UpdateUI();

            // 토스트 메시지 (SlotClickerUI에서 처리)
            Debug.Log($"[DailyLoginUI] Reward claimed: {reward.GoldMultiplier}x for {reward.DurationHours}h");
        }

        private void OnClaimButtonClicked()
        {
            if (_loginManager == null || _loginManager.HasClaimedToday) return;

            _loginManager.ClaimReward();
        }

        #endregion
    }
}
