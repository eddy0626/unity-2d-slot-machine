using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using DG.Tweening;
using SlotClicker.Core;
using SlotClicker.Data;

namespace SlotClicker.UI
{
    public class SlotClickerUI : MonoBehaviour
    {
        [Header("=== 자동 생성 ===")]
        [SerializeField] private bool _autoCreateUI = true;

        [Header("=== UI 참조 (Canvas) ===")]
        [SerializeField] private Canvas _mainCanvas;

        [Header("=== HUD 참조 ===")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _chipsText;

        [Header("=== 클릭 영역 ===")]
        [SerializeField] private Button _clickArea;

        [Header("=== 슬롯 영역 ===")]
        [SerializeField] private RectTransform _slotPanel;
        [SerializeField] private Image[] _reelSymbols;
        [SerializeField] private Image[] _reelFramesRef;
        [SerializeField] private TextMeshProUGUI _spinStateText;

        [Header("=== 3x3 슬롯 그리드 설정 ===")]
        [SerializeField] private Vector2 _slotPanelSize = new Vector2(480, 480);
        [SerializeField] private float _slotCellSize = 130f;
        [SerializeField] private float _slotCellSpacing = 145f;
        [SerializeField] private Vector2 _slotPanelPosition = new Vector2(0, -475);

        [Header("=== 클릭 영역 설정 ===")]
        [SerializeField] private Vector2 _clickAreaSize = new Vector2(420, 150);
        [SerializeField] private Vector2 _clickAreaPosition = new Vector2(0, -280);

        [Header("=== 베팅 UI ===")]
        [SerializeField] private Button[] _betButtons;
        [SerializeField] private TextMeshProUGUI _betAmountText;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TextMeshProUGUI _spinButtonText;
        [SerializeField] private Button _autoSpinButtonRef;
        [SerializeField] private TextMeshProUGUI _autoSpinTextRef;

        [Header("=== 결과/토스트 ===")]
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private CanvasGroup _resultGroup;
        [SerializeField] private TextMeshProUGUI _toastText;
        [SerializeField] private CanvasGroup _toastGroup;

        [Header("=== 심볼 스프라이트 ===")]
        [SerializeField] private Sprite[] _symbolSprites;

        [Header("=== 커스텀 UI 스프라이트 ===")]
        [Tooltip("배경 이미지 스프라이트 (Assets/04.Images/백그라운드 일러스트에서 드래그)")]
        [SerializeField] private Sprite _backgroundSprite;
        [Tooltip("터치 영역 패널 스프라이트 (Assets/04.Images/터치영역 테이블(패널)에서 드래그)")]
        [SerializeField] private Sprite _clickPanelSprite;
        private Image _backgroundImage;

        // 버튼 스프라이트들 (배팅_스핀버튼 스프라이트 시트에서 로드)
        private Sprite[] _allButtonSprites;
        private Sprite _bet10Sprite;      // 10% 버튼
        private Sprite _bet30Sprite;      // 30% 버튼
        private Sprite _bet50Sprite;      // 50% 버튼
        private Sprite _betAllSprite;     // ALL 버튼
        private Sprite _spinSprite;       // SPIN 버튼
        private Sprite _autoSpinSprite;   // AUTO 버튼

        [Header("=== 색상 설정 ===")]
        [SerializeField] private Color _normalClickColor = new Color(0.2f, 0.6f, 0.2f);
        [SerializeField] private Color _criticalColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color _jackpotColor = new Color(1f, 0.2f, 0.2f);

        [Header("=== 업그레이드 UI ===")]
        [SerializeField] private Button _upgradeButton;
        private UpgradeUI _upgradeUI;

        [Header("=== 프레스티지 UI ===")]
        [SerializeField] private Button _prestigeButton;
        private PrestigeUI _prestigeUI;

        [Header("=== 세션 통계 UI ===")]
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private TextMeshProUGUI _winRateText;
        [SerializeField] private TextMeshProUGUI _prestigeProgressText;

        // 내부 상태
        private GameManager _game;
        private float _currentBetPercentage = 0.1f;
        private double _currentBetAmount = 0;
        private SpinUIState _spinState = SpinUIState.Ready;

        // 세션 통계
        private int _sessionSpins = 0;
        private int _sessionWins = 0;
        private double _sessionEarnings = 0;

        // 골드 애니메이션
        private double _displayedGold = 0;
        private Tween _goldCountTween;

        // 자동 스핀
        private bool _isAutoSpinning = false;
        private int _autoSpinCount = 0;
        private int _autoSpinRemaining = 0;
        private Button _autoSpinButton;
        private TextMeshProUGUI _autoSpinText;
        private readonly int[] _autoSpinOptions = { 10, 25, 50, 100 };

        // 클릭 이펙트 풀 (오브젝트 풀링으로 성능 최적화)
        private GameObject _floatingTextPrefab;
        private Queue<GameObject> _floatingTextPool = new Queue<GameObject>();
        private List<GameObject> _activeFloatingTexts = new List<GameObject>();
        private const int POOL_INITIAL_SIZE = 10;
        private const int POOL_MAX_SIZE = 30;

        // 슬롯 스핀 관련 (3x3 = 9개)
        private Coroutine[] _spinCoroutines = new Coroutine[9];
        private bool[] _isReelSpinning = new bool[9];
        private Image[] _reelFrames = new Image[9];
        private readonly Color _reelFrameBaseColor = new Color(0.2f, 0.15f, 0.25f, 1f);
        private Tween _resultTween;
        private Tween _toastTween;
        private RectTransform _slotAreaRect;

        private enum SpinUIState
        {
            Ready,
            Spinning,
            Stopping,
            Result
        }

        private void Start()
        {
            StartCoroutine(WaitForGameManager());
        }

        private System.Collections.IEnumerator WaitForGameManager()
        {
            // GameManager 초기화 대기
            while (GameManager.Instance == null || GameManager.Instance.Gold == null)
            {
                yield return null;
            }

            _game = GameManager.Instance;

            if (_autoCreateUI)
            {
                CreateUI();
            }
            else
            {
                // 에디터에서 설정한 참조 사용
                SetupExistingUI();
            }

            BindEvents();

            // 초기 골드 값 설정 (애니메이션 없이)
            _displayedGold = _game.Gold.CurrentGold;

            // 자동 스핀 초기화
            _autoSpinCount = _autoSpinOptions[0];

            UpdateUI();
            UpdateStatistics();
            UpdateAutoSpinButton();
            SetSpinState(SpinUIState.Ready);
        }

        /// <summary>
        /// 에디터에서 설정한 UI 참조를 사용하여 초기화
        /// </summary>
        private void SetupExistingUI()
        {
            // Canvas 확인
            if (_mainCanvas == null)
            {
                _mainCanvas = GetComponent<Canvas>();
                if (_mainCanvas == null)
                {
                    Debug.LogError("[SlotClickerUI] Canvas not found! Please assign the Canvas reference.");
                    return;
                }
            }

            // 스프라이트 로드
            LoadSymbolSprites();
            LoadCustomUISprites();

            // 기존 클릭 영역에 커스텀 스프라이트 적용
            ApplyCustomSpritesToExistingUI();

            // 클릭 영역 위치/크기 적용
            ApplyClickAreaSettings();

            // 버튼 스프라이트 적용
            ApplyButtonSprites();

            // 슬롯 영역: 기존 슬롯 패널을 3x3 그리드로 재생성
            Recreate3x3SlotGrid();

            // 자동 스핀 버튼 설정
            if (_autoSpinButtonRef != null)
            {
                _autoSpinButton = _autoSpinButtonRef;
            }
            if (_autoSpinTextRef != null)
            {
                _autoSpinText = _autoSpinTextRef;
            }

            // 스핀 버튼 텍스트 찾기
            if (_spinButton != null && _spinButtonText == null)
            {
                _spinButtonText = _spinButton.GetComponentInChildren<TextMeshProUGUI>();
            }

            // 클릭 이벤트 바인딩
            if (_clickArea != null)
            {
                _clickArea.onClick.RemoveAllListeners();
                _clickArea.onClick.AddListener(OnClickAreaClicked);
            }

            // 베팅 버튼 이벤트 바인딩
            if (_betButtons != null)
            {
                float[] betValues = { 0.1f, 0.3f, 0.5f, 1f };
                for (int i = 0; i < _betButtons.Length && i < betValues.Length; i++)
                {
                    if (_betButtons[i] != null)
                    {
                        float betValue = betValues[i];
                        _betButtons[i].onClick.RemoveAllListeners();
                        _betButtons[i].onClick.AddListener(() => SetBetPercentage(betValue));
                    }
                }
            }

            // 스핀 버튼 이벤트
            if (_spinButton != null)
            {
                _spinButton.onClick.RemoveAllListeners();
                _spinButton.onClick.AddListener(OnSpinClicked);
            }

            // 자동 스핀 버튼 이벤트
            if (_autoSpinButton != null)
            {
                _autoSpinButton.onClick.RemoveAllListeners();
                _autoSpinButton.onClick.AddListener(OnAutoSpinClicked);
            }

            // 플로팅 텍스트 프리팹 생성
            CreateFloatingTextPrefab();

            // 업그레이드 UI 생성
            CreateUpgradeUI();

            // 프레스티지 UI 생성
            CreatePrestigeUI();

            // 업그레이드 버튼 이벤트
            if (_upgradeButton != null)
            {
                _upgradeButton.onClick.RemoveAllListeners();
                _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            }

            // 프레스티지 버튼 이벤트
            if (_prestigeButton != null)
            {
                _prestigeButton.onClick.RemoveAllListeners();
                _prestigeButton.onClick.AddListener(OnPrestigeButtonClicked);
            }

            // UIFeedback 초기화
            UIFeedback.Instance.SetCanvas(_mainCanvas);

            Debug.Log("[SlotClickerUI] Existing UI setup complete!");
        }

        private void CreateUI()
        {
            // EventSystem 확인 및 생성 (UI 입력 처리에 필수!)
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>(); // Input System 패키지용
                Debug.Log("[SlotClickerUI] EventSystem created (Input System)");
            }

            // 스프라이트 로드
            LoadSymbolSprites();
            LoadCustomUISprites();

            // 캔버스 생성
            if (_mainCanvas == null)
            {
                GameObject canvasObj = new GameObject("SlotClickerCanvas");
                _mainCanvas = canvasObj.AddComponent<Canvas>();
                _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _mainCanvas.sortingOrder = 100;

                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            RectTransform canvasRect = _mainCanvas.GetComponent<RectTransform>();

            // === 배경 이미지 (가장 먼저 생성 - 가장 뒤에 렌더링) ===
            CreateBackground(canvasRect);

            // === 클릭 영역 (배경 다음에 생성) ===
            CreateClickArea(canvasRect);

            // === 하단 베팅 UI ===
            CreateBettingUI(canvasRect);

            // === 상단 HUD ===
            CreateTopHUD(canvasRect);

            // === 슬롯머신 영역 (나중에 생성 - 앞에 렌더링) ===
            CreateSlotArea(canvasRect);

            // === 결과 텍스트 ===
            CreateResultText(canvasRect);

            // === 토스트 메시지 ===
            CreateToast(canvasRect);

            // === 플로팅 텍스트 프리팹 ===
            CreateFloatingTextPrefab();

            // === 업그레이드 버튼 ===
            CreateUpgradeButton(canvasRect);

            // === 업그레이드 UI ===
            CreateUpgradeUI();

            // === 프레스티지 버튼 ===
            CreatePrestigeButton(canvasRect);

            // === 프레스티지 UI ===
            CreatePrestigeUI();

            // === UIFeedback 초기화 ===
            UIFeedback.Instance.SetCanvas(_mainCanvas);

            Debug.Log("[SlotClickerUI] UI created successfully!");
        }

        private void CreateTopHUD(RectTransform parent)
        {
            // HUD 배경 - 화면 최상단에 고정
            GameObject hudPanel = CreatePanel(parent, "TopHUD", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, 0), new Color(0.1f, 0.1f, 0.15f, 0.95f));
            RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
            hudRect.anchoredPosition = new Vector2(0, -50); // 상단에서 50px
            hudRect.sizeDelta = new Vector2(0, 100); // 높이 100px

            // 골드 표시 (상단 좌측)
            GameObject goldObj = CreateTextObject(hudRect, "GoldText", "GOLD: 0",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -15), 42);
            _goldText = goldObj.GetComponent<TextMeshProUGUI>();
            _goldText.color = new Color(1f, 0.85f, 0.2f);
            _goldText.alignment = TextAlignmentOptions.Left;

            // 칩 표시 (상단 우측)
            GameObject chipsObj = CreateTextObject(hudRect, "ChipsText", "0 Chips",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -15), 32);
            _chipsText = chipsObj.GetComponent<TextMeshProUGUI>();
            _chipsText.color = new Color(0.6f, 0.8f, 1f);
            _chipsText.alignment = TextAlignmentOptions.Right;

            // 세션 통계 (하단 좌측)
            GameObject statsObj = CreateTextObject(hudRect, "StatsText", "Spins: 0 | Wins: 0",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 15), 22);
            _statsText = statsObj.GetComponent<TextMeshProUGUI>();
            _statsText.color = new Color(0.7f, 0.7f, 0.7f);
            _statsText.alignment = TextAlignmentOptions.Left;

            // 승률 표시 (하단 중앙)
            GameObject winRateObj = CreateTextObject(hudRect, "WinRateText", "Win Rate: --",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 15), 22);
            _winRateText = winRateObj.GetComponent<TextMeshProUGUI>();
            _winRateText.color = new Color(0.5f, 0.9f, 0.5f);
            _winRateText.alignment = TextAlignmentOptions.Center;

            // 프레스티지 진행률 (하단 우측)
            GameObject prestigeObj = CreateTextObject(hudRect, "PrestigeText", "Prestige: 0%",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 15), 22);
            _prestigeProgressText = prestigeObj.GetComponent<TextMeshProUGUI>();
            _prestigeProgressText.color = new Color(0.9f, 0.6f, 1f);
            _prestigeProgressText.alignment = TextAlignmentOptions.Right;
        }

        private void CreateSlotArea(RectTransform parent)
        {
            // 슬롯 패널 - 3x3 그리드용 확장된 크기
            GameObject slotPanel = CreatePanel(parent, "SlotPanel", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                _slotPanelPosition, _slotPanelSize, new Color(0.15f, 0.1f, 0.2f, 1f));
            RectTransform slotRect = slotPanel.GetComponent<RectTransform>();
            _slotAreaRect = slotRect;

            // 슬롯 프레임
            Image frameImg = slotPanel.GetComponent<Image>();
            AddOutline(slotPanel, new Color(0.8f, 0.6f, 0.2f), 4);

            // 스핀 상태 텍스트 (상단에 배치)
            GameObject stateObj = CreateTextObject(slotRect, "SpinStateText", "READY",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 30), 28);
            _spinStateText = stateObj.GetComponent<TextMeshProUGUI>();
            _spinStateText.color = new Color(0.8f, 0.8f, 0.9f);
            _spinStateText.alignment = TextAlignmentOptions.Center;

            // 3x3 그리드 심볼들 (9개)
            _reelSymbols = new Image[9];
            float cellSize = _slotCellSize;
            float spacing = _slotCellSpacing;
            float gridOffset = spacing; // 그리드 중앙 정렬용

            // 그리드 인덱스: 0 1 2 (상단) / 3 4 5 (중간) / 6 7 8 (하단)
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int idx = row * 3 + col;

                    // 위치 계산 (중앙 기준)
                    float x = (col - 1) * spacing;
                    float y = (1 - row) * spacing; // row 0이 상단

                    GameObject reelBg = CreatePanel(slotRect, $"ReelBg_{idx}",
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(x, y), new Vector2(cellSize, cellSize),
                        _reelFrameBaseColor);

                    reelBg.AddComponent<RectMask2D>();
                    _reelFrames[idx] = reelBg.GetComponent<Image>();

                    GameObject symbolObj = new GameObject($"Symbol_{idx}");
                    symbolObj.transform.SetParent(reelBg.transform, false);
                    RectTransform symRect = symbolObj.AddComponent<RectTransform>();
                    symRect.anchorMin = Vector2.zero;
                    symRect.anchorMax = Vector2.one;
                    symRect.offsetMin = new Vector2(5, 5);
                    symRect.offsetMax = new Vector2(-5, -5);

                    _reelSymbols[idx] = symbolObj.AddComponent<Image>();
                    _reelSymbols[idx].preserveAspect = true;
                    _reelSymbols[idx].raycastTarget = false;

                    // 초기 스프라이트 설정
                    Sprite sprite = GetSymbolSprite(idx);
                    if (sprite != null)
                    {
                        _reelSymbols[idx].sprite = sprite;
                        _reelSymbols[idx].color = Color.white;
                    }
                    else
                    {
                        _reelSymbols[idx].color = GetSymbolColor(idx);
                    }
                }
            }

            // 페이라인 표시용 라인 (선택적 - 시각적 가이드)
            CreatePaylineIndicators(slotRect);

            Debug.Log("[SlotClickerUI] 3x3 Slot grid created successfully!");
        }

        /// <summary>
        /// 페이라인 시각적 표시 생성 (당첨 시 하이라이트용)
        /// </summary>
        private void CreatePaylineIndicators(RectTransform parent)
        {
            // 페이라인 표시는 당첨 시에만 동적으로 표시
            // 여기서는 배경 패턴만 추가 (선택적)
        }

        /// <summary>
        /// 클릭 영역 위치/크기 설정 적용 (SetupExistingUI용)
        /// </summary>
        private void ApplyClickAreaSettings()
        {
            if (_clickArea == null) return;

            RectTransform clickRect = _clickArea.GetComponent<RectTransform>();
            if (clickRect != null)
            {
                clickRect.anchoredPosition = _clickAreaPosition;
                clickRect.sizeDelta = _clickAreaSize;
                Debug.Log($"[SlotClickerUI] Click area updated - Position: {_clickAreaPosition}, Size: {_clickAreaSize}");
            }
        }

        /// <summary>
        /// 스프라이트 시트에서 인덱스로 스프라이트 가져오기
        /// </summary>
        private Sprite GetSpriteByIndex(int index)
        {
            if (_allButtonSprites == null || _allButtonSprites.Length == 0)
                return null;

            // 스프라이트 이름으로 찾기 (배팅_스핀버튼_N 형식)
            string targetName = $"배팅_스핀버튼_{index}";
            foreach (var sprite in _allButtonSprites)
            {
                if (sprite.name == targetName)
                    return sprite;
            }

            // 이름으로 못 찾으면 인덱스로 시도
            if (index >= 0 && index < _allButtonSprites.Length)
                return _allButtonSprites[index];

            return null;
        }

        /// <summary>
        /// 버튼 스프라이트 적용 (베팅/스핀/오토 버튼)
        /// </summary>
        private void ApplyButtonSprites()
        {
            bool hasAnySprite = _allButtonSprites != null && _allButtonSprites.Length > 0;
            if (!hasAnySprite) return;

            // 베팅 버튼들에 각각 다른 스프라이트 적용
            if (_betButtons != null)
            {
                Sprite[] betSprites = { _bet10Sprite, _bet30Sprite, _bet50Sprite, _betAllSprite };
                for (int i = 0; i < _betButtons.Length && i < betSprites.Length; i++)
                {
                    if (_betButtons[i] != null && betSprites[i] != null)
                    {
                        ApplySpriteToButton(_betButtons[i].gameObject, betSprites[i]);
                    }
                }
            }

            // 스핀 버튼에 스프라이트 적용
            if (_spinButton != null && _spinSprite != null)
            {
                ApplySpriteToButton(_spinButton.gameObject, _spinSprite);
            }

            // 오토 스핀 버튼에 스프라이트 적용
            Button autoBtn = _autoSpinButton ?? _autoSpinButtonRef;
            if (autoBtn != null && _autoSpinSprite != null)
            {
                ApplySpriteToButton(autoBtn.gameObject, _autoSpinSprite);
            }

            Debug.Log("[SlotClickerUI] Button sprites applied with individual sprites");
        }

        /// <summary>
        /// 개별 버튼에 스프라이트 적용 (기존 배경 완전 제거)
        /// </summary>
        private void ApplySpriteToButton(GameObject buttonObj, Sprite sprite)
        {
            if (buttonObj == null || sprite == null) return;

            // 메인 버튼 이미지에 스프라이트 적용
            Image mainImg = buttonObj.GetComponent<Image>();
            if (mainImg != null)
            {
                // 기존 배경색 제거하고 스프라이트만 표시
                mainImg.sprite = sprite;
                mainImg.type = Image.Type.Simple;
                mainImg.preserveAspect = true;
                mainImg.color = Color.white; // 스프라이트 원본 색상 유지

                // 버튼 색상 트랜지션 - 모두 흰색으로 (스프라이트 색상 유지)
                Button btn = buttonObj.GetComponent<Button>();
                if (btn != null)
                {
                    // 트랜지션을 None으로 설정하여 색상 변화 방지
                    btn.transition = Selectable.Transition.None;
                }
            }

            // 자식 Label의 텍스트만 유지하고, 텍스트 위치/스타일 조정
            for (int i = buttonObj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = buttonObj.transform.GetChild(i);

                // 텍스트 컴포넌트 확인
                TextMeshProUGUI tmpText = child.GetComponent<TextMeshProUGUI>();
                UnityEngine.UI.Text legacyText = child.GetComponent<UnityEngine.UI.Text>();

                if (tmpText != null || legacyText != null)
                {
                    // 텍스트는 유지하되, 배경 이미지가 있으면 제거
                    Image textBgImg = child.GetComponent<Image>();
                    if (textBgImg != null)
                    {
                        textBgImg.enabled = false;
                    }
                    continue;
                }

                // 텍스트가 아닌 자식은 모두 비활성화 (배경 이미지 등)
                child.gameObject.SetActive(false);
                Debug.Log($"[SlotClickerUI] Disabled child '{child.name}' in button '{buttonObj.name}'");
            }

            Debug.Log($"[SlotClickerUI] Applied sprite '{sprite.name}' to button '{buttonObj.name}'");
        }

        /// <summary>
        /// 기존 슬롯 패널을 3x3 그리드로 재생성 (SetupExistingUI용)
        /// </summary>
        private void Recreate3x3SlotGrid()
        {
            RectTransform canvasRect = _mainCanvas.GetComponent<RectTransform>();

            // 기존 슬롯 패널 삭제
            if (_slotPanel != null)
            {
                Destroy(_slotPanel.gameObject);
            }

            // 기존 릴 심볼들 삭제 (씬에서 설정된 것들)
            if (_reelSymbols != null)
            {
                foreach (var symbol in _reelSymbols)
                {
                    if (symbol != null && symbol.transform.parent != null)
                    {
                        Destroy(symbol.transform.parent.gameObject);
                    }
                }
            }

            // 3x3 그리드 슬롯 패널 새로 생성
            GameObject slotPanel = CreatePanel(canvasRect, "SlotPanel3x3", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                _slotPanelPosition, _slotPanelSize, new Color(0.15f, 0.1f, 0.2f, 1f));
            RectTransform slotRect = slotPanel.GetComponent<RectTransform>();
            _slotAreaRect = slotRect;
            _slotPanel = slotRect;

            // 슬롯 프레임 아웃라인
            AddOutline(slotPanel, new Color(0.8f, 0.6f, 0.2f), 4);

            // 스핀 상태 텍스트
            GameObject stateObj = CreateTextObject(slotRect, "SpinStateText", "READY",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 30), 28);
            _spinStateText = stateObj.GetComponent<TextMeshProUGUI>();
            _spinStateText.color = new Color(0.8f, 0.8f, 0.9f);
            _spinStateText.alignment = TextAlignmentOptions.Center;

            // 3x3 그리드 심볼들 (9개)
            _reelSymbols = new Image[9];
            _reelFrames = new Image[9];
            float cellSize = _slotCellSize;
            float spacing = _slotCellSpacing;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int idx = row * 3 + col;

                    float x = (col - 1) * spacing;
                    float y = (1 - row) * spacing;

                    GameObject reelBg = CreatePanel(slotRect, $"ReelBg_{idx}",
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(x, y), new Vector2(cellSize, cellSize),
                        _reelFrameBaseColor);

                    reelBg.AddComponent<RectMask2D>();
                    _reelFrames[idx] = reelBg.GetComponent<Image>();

                    GameObject symbolObj = new GameObject($"Symbol_{idx}");
                    symbolObj.transform.SetParent(reelBg.transform, false);
                    RectTransform symRect = symbolObj.AddComponent<RectTransform>();
                    symRect.anchorMin = Vector2.zero;
                    symRect.anchorMax = Vector2.one;
                    symRect.offsetMin = new Vector2(5, 5);
                    symRect.offsetMax = new Vector2(-5, -5);

                    _reelSymbols[idx] = symbolObj.AddComponent<Image>();
                    _reelSymbols[idx].preserveAspect = true;
                    _reelSymbols[idx].raycastTarget = false;

                    Sprite sprite = GetSymbolSprite(idx);
                    if (sprite != null)
                    {
                        _reelSymbols[idx].sprite = sprite;
                        _reelSymbols[idx].color = Color.white;
                    }
                    else
                    {
                        _reelSymbols[idx].color = GetSymbolColor(idx);
                    }
                }
            }

            Debug.Log("[SlotClickerUI] 3x3 Slot grid recreated for existing UI!");
        }

        private void CreateClickArea(RectTransform parent)
        {
            // 클릭 영역 (카지노 테이블) - Inspector에서 설정 가능
            GameObject clickPanel = CreatePanel(parent, "ClickArea", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                _clickAreaPosition, _clickAreaSize, new Color(0.1f, 0.4f, 0.15f, 1f));
            RectTransform clickRect = clickPanel.GetComponent<RectTransform>();

            // 커스텀 스프라이트 적용
            Image clickImage = clickPanel.GetComponent<Image>();
            if (_clickPanelSprite != null)
            {
                clickImage.sprite = _clickPanelSprite;
                clickImage.type = Image.Type.Sliced; // 9-slice 지원
                clickImage.color = Color.white;
                // 커스텀 스프라이트 크기에 맞게 조정 (비율 유지)
                float aspectRatio = _clickPanelSprite.rect.width / _clickPanelSprite.rect.height;
                float newWidth = clickRect.sizeDelta.y * aspectRatio;
                clickRect.sizeDelta = new Vector2(Mathf.Max(newWidth, 520), clickRect.sizeDelta.y);
                Debug.Log("[SlotClickerUI] Custom click panel sprite applied");
            }
            else
            {
                // 기본 스타일: 아웃라인 추가
                AddOutline(clickPanel, new Color(0.6f, 0.4f, 0.1f), 5);
            }

            // 버튼 컴포넌트
            _clickArea = clickPanel.AddComponent<Button>();
            _clickArea.transition = Selectable.Transition.None;

            // 테이블 텍스트
            GameObject tableText = CreateTextObject(clickRect, "TableText", "TAP TO EARN",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 48);
            TextMeshProUGUI tableTmp = tableText.GetComponent<TextMeshProUGUI>();
            tableTmp.color = new Color(1f, 0.9f, 0.6f, 0.8f);
            tableTmp.alignment = TextAlignmentOptions.Center;
            tableTmp.raycastTarget = false;

            // 펄스 애니메이션
            tableTmp.transform.DOScale(1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        private void CreateBettingUI(RectTransform parent)
        {
            // 베팅 패널 - 하단에 고정
            GameObject betPanel = CreatePanel(parent, "BetPanel", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, 220), new Color(0.12f, 0.1f, 0.18f, 0.95f));
            RectTransform betRect = betPanel.GetComponent<RectTransform>();
            betRect.anchoredPosition = new Vector2(0, 110); // 하단에서 110px 위

            // 현재 베팅액 표시
            GameObject betAmountObj = CreateTextObject(betRect, "BetAmountText", "Bet: 0",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), 34);
            _betAmountText = betAmountObj.GetComponent<TextMeshProUGUI>();
            _betAmountText.color = Color.white;
            _betAmountText.alignment = TextAlignmentOptions.Center;

            // 베팅 버튼들
            _betButtons = new Button[4];
            string[] betLabels = { "10%", "30%", "50%", "ALL" };
            float[] betValues = { 0.1f, 0.3f, 0.5f, 1f };
            float buttonWidth = 120f;  // 버튼 폭 조정
            float buttonSpacing = 12f;  // 간격 조정
            float totalWidth = (buttonWidth * 4) + (buttonSpacing * 3);
            float startX = -totalWidth / 2 + buttonWidth / 2;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                float betValue = betValues[i];

                GameObject btnObj = CreateButton(betRect, $"BetBtn_{i}", betLabels[i],
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + (i * (buttonWidth + buttonSpacing)), 35),
                    new Vector2(buttonWidth, 45),
                    new Color(0.3f, 0.3f, 0.5f));

                _betButtons[i] = btnObj.GetComponent<Button>();
                _betButtons[i].onClick.AddListener(() => SetBetPercentage(betValue));
            }

            // 스핀 버튼 - 하단에 배치 (좌측으로 이동)
            GameObject spinObj = CreateButton(betRect, "SpinButton", "SPIN!",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-60, 40), new Vector2(180, 60),
                new Color(0.8f, 0.2f, 0.2f));
            _spinButton = spinObj.GetComponent<Button>();
            _spinButton.onClick.AddListener(OnSpinClicked);

            _spinButtonText = spinObj.GetComponentInChildren<TextMeshProUGUI>();
            _spinButtonText.fontSize = 34;
            _spinButtonText.fontStyle = FontStyles.Bold;

            // 자동 스핀 버튼 - 스핀 버튼 우측에 배치
            GameObject autoSpinObj = CreateButton(betRect, "AutoSpinButton", "AUTO\nx10",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(105, 40), new Vector2(90, 60),
                new Color(0.3f, 0.5f, 0.7f));
            _autoSpinButton = autoSpinObj.GetComponent<Button>();
            _autoSpinButton.onClick.AddListener(OnAutoSpinClicked);

            _autoSpinText = autoSpinObj.GetComponentInChildren<TextMeshProUGUI>();
            _autoSpinText.fontSize = 22;
            _autoSpinText.fontStyle = FontStyles.Bold;
        }

        private void CreateResultText(RectTransform parent)
        {
            RectTransform resultParent = _slotAreaRect != null ? _slotAreaRect : parent;

            // 결과 배너 - 슬롯 영역 하단 고정
            _resultPanel = CreatePanel(resultParent, "ResultPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, -30), new Vector2(480, 55),
                new Color(0f, 0f, 0f, 0.6f));

            RectTransform panelRect = _resultPanel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0.5f, 1f);

            _resultGroup = _resultPanel.AddComponent<CanvasGroup>();
            _resultGroup.alpha = 0f;

            GameObject resultObj = CreateTextObject(panelRect, "ResultText", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 44);
            _resultText = resultObj.GetComponent<TextMeshProUGUI>();
            _resultText.color = Color.white;
            _resultText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateToast(RectTransform parent)
        {
            GameObject toastPanel = CreatePanel(parent, "ToastPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 290), new Vector2(480, 45),
                new Color(0f, 0f, 0f, 0.6f));

            _toastGroup = toastPanel.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;

            RectTransform panelRect = toastPanel.GetComponent<RectTransform>();

            GameObject toastObj = CreateTextObject(panelRect, "ToastText", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 32);
            _toastText = toastObj.GetComponent<TextMeshProUGUI>();
            _toastText.color = Color.white;
            _toastText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateFloatingTextPrefab()
        {
            _floatingTextPrefab = new GameObject("FloatingTextPrefab");
            _floatingTextPrefab.SetActive(false);

            RectTransform rect = _floatingTextPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            TextMeshProUGUI tmp = _floatingTextPrefab.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 40;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;

            _floatingTextPrefab.transform.SetParent(_mainCanvas.transform, false);

            // 오브젝트 풀 초기화
            InitializeFloatingTextPool();
        }

        /// <summary>
        /// 플로팅 텍스트 오브젝트 풀 초기화
        /// </summary>
        private void InitializeFloatingTextPool()
        {
            for (int i = 0; i < POOL_INITIAL_SIZE; i++)
            {
                GameObject pooledObj = CreatePooledFloatingText();
                _floatingTextPool.Enqueue(pooledObj);
            }
        }

        private GameObject CreatePooledFloatingText()
        {
            GameObject obj = Instantiate(_floatingTextPrefab, _mainCanvas.transform);
            obj.name = "PooledFloatingText";
            obj.SetActive(false);
            return obj;
        }

        /// <summary>
        /// 풀에서 플로팅 텍스트 가져오기
        /// </summary>
        private GameObject GetFloatingTextFromPool()
        {
            GameObject obj;
            if (_floatingTextPool.Count > 0)
            {
                obj = _floatingTextPool.Dequeue();
            }
            else if (_activeFloatingTexts.Count < POOL_MAX_SIZE)
            {
                obj = CreatePooledFloatingText();
            }
            else
            {
                // 풀이 가득 찼으면 가장 오래된 활성 텍스트 재활용
                obj = _activeFloatingTexts[0];
                _activeFloatingTexts.RemoveAt(0);
                obj.transform.DOKill();
            }

            _activeFloatingTexts.Add(obj);
            return obj;
        }

        /// <summary>
        /// 플로팅 텍스트를 풀에 반환
        /// </summary>
        private void ReturnFloatingTextToPool(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();
            obj.SetActive(false);
            _activeFloatingTexts.Remove(obj);

            if (_floatingTextPool.Count < POOL_MAX_SIZE)
            {
                _floatingTextPool.Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// 플로팅 텍스트 풀 정리
        /// </summary>
        private void CleanupFloatingTextPool()
        {
            foreach (var obj in _activeFloatingTexts)
            {
                if (obj != null)
                {
                    obj.transform.DOKill();
                    Destroy(obj);
                }
            }
            _activeFloatingTexts.Clear();

            while (_floatingTextPool.Count > 0)
            {
                var obj = _floatingTextPool.Dequeue();
                if (obj != null) Destroy(obj);
            }

            if (_floatingTextPrefab != null)
                Destroy(_floatingTextPrefab);
        }

        #region Helper Methods

        private GameObject CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image img = panel.AddComponent<Image>();
            img.color = color;

            return panel;
        }

        private GameObject CreateTextObject(RectTransform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 60);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;

            return textObj;
        }

        private GameObject CreateButton(RectTransform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject btnObj = CreatePanel(parent, name, anchorMin, anchorMax, position, size, color);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;

            GameObject textObj = CreateTextObject(btnObj.GetComponent<RectTransform>(), "Label", label,
                Vector2.zero, Vector2.one, Vector2.zero, 30);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btnObj;
        }

        private void AddOutline(GameObject obj, Color color, float width)
        {
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, width);
        }

        private Color GetSymbolColor(int index)
        {
            Color[] colors = {
                new Color(1f, 0.3f, 0.3f),  // 빨강
                new Color(0.3f, 1f, 0.3f),  // 초록
                new Color(0.3f, 0.3f, 1f),  // 파랑
                new Color(1f, 1f, 0.3f),    // 노랑
                new Color(1f, 0.5f, 0f),    // 주황
                new Color(0.8f, 0.3f, 1f),  // 보라
                new Color(1f, 0.8f, 0f)     // 금
            };
            return colors[index % colors.Length];
        }

        private void LoadSymbolSprites()
        {
            // Resources 폴더에서 스프라이트 시트 로드
            _symbolSprites = Resources.LoadAll<Sprite>("Sprites/SymbolSprites");

            if (_symbolSprites == null || _symbolSprites.Length == 0)
            {
                Debug.LogWarning("[SlotClickerUI] Failed to load from 'Sprites/SymbolSprites', trying alternative paths...");

                // 대안 경로 시도
                _symbolSprites = Resources.LoadAll<Sprite>("SymbolSprites");

                if (_symbolSprites == null || _symbolSprites.Length == 0)
                {
                    Debug.LogError("[SlotClickerUI] Could not load symbol sprites! Using fallback colors.");
                    _symbolSprites = null;
                }
            }

            if (_symbolSprites != null && _symbolSprites.Length > 0)
            {
                Debug.Log($"[SlotClickerUI] Successfully loaded {_symbolSprites.Length} symbol sprites");
                // 스프라이트 이름 로깅
                for (int i = 0; i < Mathf.Min(3, _symbolSprites.Length); i++)
                {
                    Debug.Log($"  - Sprite {i}: {_symbolSprites[i].name}");
                }
            }
        }

        /// <summary>
        /// 커스텀 UI 스프라이트 로드 (배경, 터치 패널)
        /// Resources/UI/ 폴더에서 자동 로드
        /// </summary>
        private void LoadCustomUISprites()
        {
            // Inspector에서 설정되지 않은 경우에만 Resources에서 로드 시도
            if (_backgroundSprite == null)
            {
                // Resources/UI/ 폴더에서 로드 (가장 우선)
                Sprite[] bgSprites = Resources.LoadAll<Sprite>("UI/백그라운드 일러스트");

                if (bgSprites != null && bgSprites.Length > 0)
                {
                    _backgroundSprite = bgSprites[0];
                    Debug.Log($"[SlotClickerUI] Background sprite auto-loaded: {_backgroundSprite.name}");
                }
            }

            if (_clickPanelSprite == null)
            {
                // Resources/UI/ 폴더에서 로드
                Sprite[] panelSprites = Resources.LoadAll<Sprite>("UI/터치영역 테이블(패널)");

                if (panelSprites != null && panelSprites.Length > 0)
                {
                    // 첫 번째 슬라이스 사용 (_0)
                    _clickPanelSprite = panelSprites[0];
                    Debug.Log($"[SlotClickerUI] Click panel sprite auto-loaded: {_clickPanelSprite.name}");
                }
            }

            // 버튼 스프라이트 로드 (스프라이트 시트에서 모든 스프라이트 로드)
            if (_allButtonSprites == null || _allButtonSprites.Length == 0)
            {
                _allButtonSprites = Resources.LoadAll<Sprite>("UI/배팅_스핀버튼");

                if (_allButtonSprites != null && _allButtonSprites.Length > 0)
                {
                    Debug.Log($"[SlotClickerUI] Button sprites loaded: {_allButtonSprites.Length} sprites");

                    // 스프라이트 시트 구성 (0-19번 스프라이트):
                    // _0~_3: 베팅 버튼 노멀 상태 (10%, 30%, 50%, ALL)
                    // _4~_7: 베팅 버튼 선택 상태
                    // _8~_9: 스핀 버튼 (노멀, 프레스)
                    // _10~_13: 오토 버튼 (노멀, 프레스, 활성, 비활성)
                    // 나머지는 여분

                    // 버튼별 스프라이트 할당
                    _bet10Sprite = GetSpriteByIndex(0);   // 10% 버튼
                    _bet30Sprite = GetSpriteByIndex(1);   // 30% 버튼
                    _bet50Sprite = GetSpriteByIndex(2);   // 50% 버튼
                    _betAllSprite = GetSpriteByIndex(3);  // ALL 버튼
                    _spinSprite = GetSpriteByIndex(8);    // SPIN 버튼 (큰 버튼)
                    _autoSpinSprite = GetSpriteByIndex(10); // AUTO 버튼 (작은 버튼)

                    Debug.Log($"[SlotClickerUI] Button sprites assigned - Bet10:{_bet10Sprite?.name}, Spin:{_spinSprite?.name}, Auto:{_autoSpinSprite?.name}");
                }
            }

            // 로드 결과 로깅
            if (_backgroundSprite != null)
                Debug.Log($"[SlotClickerUI] ✓ Background ready: {_backgroundSprite.name} ({_backgroundSprite.rect.width}x{_backgroundSprite.rect.height})");
            else
                Debug.LogWarning("[SlotClickerUI] ✗ No background sprite - using default color");

            if (_clickPanelSprite != null)
                Debug.Log($"[SlotClickerUI] ✓ Click panel ready: {_clickPanelSprite.name}");
            else
                Debug.LogWarning("[SlotClickerUI] ✗ No click panel sprite - using default color");
        }

        /// <summary>
        /// 배경 이미지 생성
        /// </summary>
        private void CreateBackground(RectTransform parent)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(parent, false);
            bgObj.transform.SetAsFirstSibling(); // 가장 뒤에 렌더링되도록

            RectTransform rect = bgObj.AddComponent<RectTransform>();
            // 전체 화면 채우기
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _backgroundImage = bgObj.AddComponent<Image>();

            if (_backgroundSprite != null)
            {
                _backgroundImage.sprite = _backgroundSprite;
                _backgroundImage.type = Image.Type.Sliced; // 9-slice 지원
                _backgroundImage.preserveAspect = false;
                _backgroundImage.color = Color.white;
                Debug.Log("[SlotClickerUI] Custom background applied");
            }
            else
            {
                // 기본 그라디언트 배경색 (커스텀 스프라이트 없을 때)
                _backgroundImage.color = new Color(0.08f, 0.06f, 0.12f, 1f);
            }

            // 배경은 클릭 이벤트 받지 않음
            _backgroundImage.raycastTarget = false;
        }

        /// <summary>
        /// 기존 UI에 커스텀 스프라이트 적용 (SetupExistingUI용)
        /// </summary>
        private void ApplyCustomSpritesToExistingUI()
        {
            // 배경 이미지 찾기 또는 생성
            Transform bgTransform = _mainCanvas.transform.Find("Background");
            if (bgTransform == null && _backgroundSprite != null)
            {
                // 배경이 없으면 생성
                CreateBackground(_mainCanvas.GetComponent<RectTransform>());
            }
            else if (bgTransform != null && _backgroundSprite != null)
            {
                // 기존 배경에 스프라이트 적용
                _backgroundImage = bgTransform.GetComponent<Image>();
                if (_backgroundImage != null)
                {
                    _backgroundImage.sprite = _backgroundSprite;
                    _backgroundImage.type = Image.Type.Sliced;
                    _backgroundImage.color = Color.white;
                }
            }

            // 클릭 영역에 스프라이트 적용
            if (_clickArea != null && _clickPanelSprite != null)
            {
                Image clickImage = _clickArea.GetComponent<Image>();
                if (clickImage != null)
                {
                    clickImage.sprite = _clickPanelSprite;
                    clickImage.type = Image.Type.Sliced;
                    clickImage.color = Color.white;
                    Debug.Log("[SlotClickerUI] Custom click panel sprite applied to existing UI");
                }
            }
        }

        private Sprite GetSymbolSprite(int index)
        {
            if (_symbolSprites != null && _symbolSprites.Length > 0 && index >= 0)
            {
                int safeIndex = index % _symbolSprites.Length;
                return _symbolSprites[safeIndex];
            }
            return null;
        }

        private void CreateUpgradeButton(RectTransform parent)
        {
            // 업그레이드 버튼 (화면 오른쪽 상단, HUD 바로 아래 - HUD끝 -150 + 10px 간격)
            GameObject btnObj = CreateButton(parent, "UpgradeButton", "UPGRADES",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-90, -165), new Vector2(140, 45),
                new Color(0.4f, 0.3f, 0.7f));

            _upgradeButton = btnObj.GetComponent<Button>();
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

            // 아이콘 효과 (펄스)
            btnObj.transform.DOScale(1.05f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void CreateUpgradeUI()
        {
            GameObject upgradeUIObj = new GameObject("UpgradeUI");
            upgradeUIObj.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = upgradeUIObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _upgradeUI = upgradeUIObj.AddComponent<UpgradeUI>();
            _upgradeUI.Initialize(_game);
            _upgradeUI.Hide();
        }

        private void OnUpgradeButtonClicked()
        {
            if (_upgradeUI != null)
            {
                _upgradeUI.Toggle();
            }
        }

        private void CreatePrestigeButton(RectTransform parent)
        {
            // 프레스티지 버튼 (화면 왼쪽 상단, HUD 바로 아래 - HUD끝 -150 + 10px 간격)
            GameObject btnObj = CreateButton(parent, "PrestigeButton", "PRESTIGE",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(90, -165), new Vector2(140, 45),
                new Color(0.6f, 0.3f, 0.6f));

            _prestigeButton = btnObj.GetComponent<Button>();
            _prestigeButton.onClick.AddListener(OnPrestigeButtonClicked);

            // 아이콘 효과 (반짝임)
            btnObj.transform.DOScale(1.05f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void CreatePrestigeUI()
        {
            GameObject prestigeUIObj = new GameObject("PrestigeUI");
            prestigeUIObj.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = prestigeUIObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.1f);
            rect.anchorMax = new Vector2(0.95f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 배경 패널
            Image bg = prestigeUIObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.15f, 0.98f);

            _prestigeUI = prestigeUIObj.AddComponent<PrestigeUI>();
            _prestigeUI.Initialize(_game);
            _prestigeUI.Hide();
        }

        private void OnPrestigeButtonClicked()
        {
            if (_prestigeUI != null)
            {
                _prestigeUI.Toggle();
            }
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            // 클릭 이벤트 (autoCreateUI 모드에서만 - SetupExistingUI에서는 이미 바인딩됨)
            if (_autoCreateUI && _clickArea != null)
            {
                _clickArea.onClick.AddListener(OnClickAreaClicked);
            }

            // 게임 매니저 이벤트
            _game.Gold.OnGoldChanged += OnGoldChanged;
            _game.Click.OnClick += OnClickResult;
            _game.Slot.OnSpinStart += OnSlotSpinStart;
            _game.Slot.OnSpinComplete += OnSlotSpinComplete;
            _game.Slot.OnReelStop += OnReelStop;
        }

        private void OnDestroy()
        {
            // DOTween 정리 - 모든 활성 트윈 중지
            _goldCountTween?.Kill();
            _resultTween?.Kill();
            _toastTween?.Kill();

            // 무한 루프 DOTween 애니메이션 정리
            if (_upgradeButton != null) _upgradeButton.transform.DOKill();
            if (_prestigeButton != null) _prestigeButton.transform.DOKill();
            if (_clickArea != null)
            {
                var tableText = _clickArea.GetComponentInChildren<TextMeshProUGUI>();
                if (tableText != null) tableText.transform.DOKill();
            }

            // 릴 애니메이션 정리 (3x3 = 9개)
            if (_reelSymbols != null)
            {
                for (int i = 0; i < _reelSymbols.Length; i++)
                {
                    if (_reelSymbols[i] != null)
                    {
                        _reelSymbols[i].transform.DOKill();
                        _reelSymbols[i].DOKill();
                    }
                    if (i < _reelFrames.Length && _reelFrames[i] != null)
                        _reelFrames[i].DOKill();
                }
            }

            // 스핀 상태 텍스트 정리
            if (_spinStateText != null) _spinStateText.transform.DOKill();

            // 플로팅 텍스트 풀 정리
            CleanupFloatingTextPool();

            // 이벤트 구독 해제 (null-safe)
            if (_game != null)
            {
                if (_game.Gold != null)
                    _game.Gold.OnGoldChanged -= OnGoldChanged;
                if (_game.Click != null)
                    _game.Click.OnClick -= OnClickResult;
                if (_game.Slot != null)
                {
                    _game.Slot.OnSpinStart -= OnSlotSpinStart;
                    _game.Slot.OnSpinComplete -= OnSlotSpinComplete;
                    _game.Slot.OnReelStop -= OnReelStop;
                }
            }
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            if (_game == null) return;

            _goldText.text = $"GOLD: {_game.Gold.GetFormattedGold()}";
            int chips = _game.Prestige?.TotalChips ?? _game.PlayerData.chips;
            _chipsText.text = $"{chips} Chips";
            UpdateBetAmount();

            if (_game.Slot != null)
            {
                bool spinning = _game.Slot.IsSpinning;
                _spinButton.interactable = !spinning;
                SetBetButtonsInteractable(!spinning);
            }
        }

        private void UpdateBetAmount()
        {
            _currentBetAmount = _game.Gold.CalculateBetAmount(_currentBetPercentage);
            _betAmountText.text = $"Bet: {GoldManager.FormatNumber(_currentBetAmount)}";
        }

        private void UpdateStatistics()
        {
            if (_game == null) return;

            // 세션 통계
            if (_statsText != null)
            {
                _statsText.text = $"Spins: {_sessionSpins} | Wins: {_sessionWins}";
            }

            // 승률
            if (_winRateText != null)
            {
                if (_sessionSpins > 0)
                {
                    float winRate = (float)_sessionWins / _sessionSpins * 100f;
                    string earningsSign = _sessionEarnings >= 0 ? "+" : "";
                    _winRateText.text = $"Win: {winRate:F1}% ({earningsSign}{GoldManager.FormatNumber(_sessionEarnings)})";
                    _winRateText.color = _sessionEarnings >= 0 ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.9f, 0.5f, 0.5f);
                }
                else
                {
                    _winRateText.text = "Win Rate: --";
                }
            }

            // 프레스티지 진행률
            if (_prestigeProgressText != null && _game.Prestige != null)
            {
                int chips = _game.Prestige.CalculateChipsToGain();
                string vipRank = _game.Prestige.GetVIPRankName(_game.Prestige.CurrentVIPRank);

                if (chips > 0)
                {
                    _prestigeProgressText.text = $"{vipRank} | +{chips} Chips";
                    _prestigeProgressText.color = new Color(1f, 0.8f, 0.2f);
                }
                else
                {
                    double threshold = 1_000_000; // 1M
                    double current = _game.PlayerData.totalGoldEarned;
                    float progress = Mathf.Clamp01((float)(current / threshold)) * 100f;
                    _prestigeProgressText.text = $"{vipRank} | {progress:F1}%";
                    _prestigeProgressText.color = new Color(0.9f, 0.6f, 1f);
                }
            }
        }

        private void SetSpinState(SpinUIState state)
        {
            _spinState = state;

            if (_spinStateText != null)
            {
                switch (state)
                {
                    case SpinUIState.Ready:
                        _spinStateText.text = "READY";
                        _spinStateText.color = new Color(0.8f, 0.8f, 0.9f);
                        break;
                    case SpinUIState.Spinning:
                        _spinStateText.text = "SPINNING...";
                        _spinStateText.color = new Color(0.6f, 1f, 0.8f);
                        break;
                    case SpinUIState.Stopping:
                        _spinStateText.text = "STOPPING...";
                        _spinStateText.color = new Color(1f, 0.85f, 0.4f);
                        break;
                    case SpinUIState.Result:
                        _spinStateText.text = "RESULT";
                        _spinStateText.color = new Color(1f, 0.6f, 0.6f);
                        break;
                }

                _spinStateText.transform.DOKill();
                _spinStateText.transform.localScale = Vector3.one;
                _spinStateText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 3, 0.5f);
            }

            UpdateSpinButtonState();
        }

        private void UpdateSpinButtonState()
        {
            if (_spinButtonText == null || _spinButton == null) return;

            if (_spinState == SpinUIState.Spinning || _spinState == SpinUIState.Stopping)
            {
                _spinButtonText.text = "SPINNING";
            }
            else
            {
                _spinButtonText.text = "SPIN!";
            }
        }

        private void SetBetButtonsInteractable(bool interactable)
        {
            if (_betButtons == null) return;
            for (int i = 0; i < _betButtons.Length; i++)
            {
                if (_betButtons[i] != null)
                    _betButtons[i].interactable = interactable;
            }
        }

        private void OnGoldChanged(double newGold)
        {
            // 골드 카운팅 애니메이션
            _goldCountTween?.Kill();

            double startValue = _displayedGold;
            double endValue = newGold;
            float duration = Mathf.Clamp((float)Math.Abs(endValue - startValue) / 10000f, 0.2f, 1f);

            _goldCountTween = DOTween.To(
                () => _displayedGold,
                x => {
                    _displayedGold = x;
                    _goldText.text = $"GOLD: {GoldManager.FormatNumber(_displayedGold)}";
                },
                endValue,
                duration
            ).SetEase(Ease.OutQuad);

            // 이전 스케일 애니메이션 정리 후 새 애니메이션 (연속 클릭 시 누적 방지)
            _goldText.transform.DOKill();
            _goldText.transform.localScale = Vector3.one;
            _goldText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            UpdateBetAmount();
            UpdateStatistics();
        }

        #endregion

        #region Input Handlers

        private void OnClickAreaClicked()
        {
            if (_game == null || _game.Click == null) return;

            Vector2 screenPos = GetPointerScreenPosition();
            if (screenPos == Vector2.zero && _clickArea != null)
            {
                Camera cam = _mainCanvas != null && _mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? _mainCanvas.worldCamera
                    : null;
                screenPos = RectTransformUtility.WorldToScreenPoint(cam, _clickArea.transform.position);
            }

            Vector2 localPos = ScreenToCanvasPosition(screenPos);
            _game.Click.ProcessClick(localPos);

            // 클릭 피드백 (이전 애니메이션 정리 - 연속 클릭 시 스케일 누적 방지)
            _clickArea.transform.DOKill();
            _clickArea.transform.localScale = Vector3.one;
            _clickArea.transform.DOPunchScale(Vector3.one * 0.05f, 0.1f);
        }

        private void OnClickResult(ClickResult result)
        {
            // 플로팅 텍스트 생성
            SpawnFloatingText(result.Position, result.GoldEarned, result.IsCritical);
        }

        private void SpawnFloatingText(Vector2 position, double amount, bool isCritical)
        {
            // 오브젝트 풀에서 가져오기 (Instantiate 대신)
            GameObject floatText = GetFloatingTextFromPool();
            floatText.SetActive(true);

            RectTransform rect = floatText.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;

            TextMeshProUGUI tmp = floatText.GetComponent<TextMeshProUGUI>();
            tmp.text = $"+{GoldManager.FormatNumber(amount)}";
            tmp.color = isCritical ? _criticalColor : Color.yellow;
            tmp.fontSize = isCritical ? 52 : 40;
            tmp.alpha = 1f; // 알파 초기화 (풀에서 재사용 시 필요)

            // 애니메이션 (완료 시 풀에 반환)
            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOAnchorPosY(rect.anchoredPosition.y + 100, 0.8f).SetEase(Ease.OutQuad));
            seq.Join(tmp.DOFade(0, 0.8f).SetEase(Ease.InQuad));
            seq.OnComplete(() => ReturnFloatingTextToPool(floatText));
        }

        private Vector2 GetPointerScreenPosition()
        {
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            return Vector2.zero;
        }

        private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            if (_mainCanvas == null)
                return screenPosition;

            RectTransform canvasRect = _mainCanvas.transform as RectTransform;
            Camera cam = _mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out Vector2 localPoint))
                return localPoint;

            return screenPosition;
        }

        private void ShowToast(string message, Color color, float duration = 1.6f)
        {
            if (_toastText == null || _toastGroup == null) return;

            _toastText.text = message;
            _toastText.color = color;

            _toastGroup.DOKill();
            _toastGroup.alpha = 1f;

            _toastTween?.Kill();
            _toastTween = _toastGroup.DOFade(0f, 0.4f).SetDelay(duration);
        }

        private void SetBetPercentage(float percentage)
        {
            _currentBetPercentage = percentage;
            UpdateBetAmount();

            // 버튼 하이라이트
            bool hasCustomSprites = _allButtonSprites != null && _allButtonSprites.Length > 0;

            for (int i = 0; i < _betButtons.Length; i++)
            {
                if (_betButtons[i] == null) continue;

                Image img = _betButtons[i].GetComponent<Image>();
                if (img == null) continue;

                float[] values = { 0.1f, 0.3f, 0.5f, 1f };
                bool isSelected = Mathf.Approximately(values[i], percentage);

                // 커스텀 스프라이트가 있으면 밝기로 하이라이트, 없으면 색상 변경
                if (hasCustomSprites)
                {
                    img.color = isSelected ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
                else
                {
                    img.color = isSelected
                        ? new Color(0.5f, 0.4f, 0.8f)
                        : new Color(0.3f, 0.3f, 0.5f);
                }
            }
        }

        private void OnSpinClicked()
        {
            if (_game == null || _game.Slot == null) return;

            if (_game.Slot.IsSpinning)
            {
                ShowToast("Spinning...", new Color(1f, 0.85f, 0.4f));
                return;
            }

            if (_currentBetAmount <= 0 || !_game.Gold.CanAfford(_currentBetAmount))
            {
                ShowToast("Not enough gold!", Color.red);
                _goldText?.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
                return;
            }

            bool started = _game.Slot.TrySpin(_currentBetAmount);
            if (!started)
            {
                ShowToast("Cannot spin right now", Color.red);
            }
        }

        private float _lastAutoSpinClickTime = 0f;
        private const float DOUBLE_CLICK_TIME = 0.3f;

        private void OnAutoSpinClicked()
        {
            if (_isAutoSpinning)
            {
                // 자동 스핀 중이면 중지
                StopAutoSpin();
                return;
            }

            float currentTime = Time.time;

            // 더블클릭 감지 - 자동 스핀 시작
            if (currentTime - _lastAutoSpinClickTime < DOUBLE_CLICK_TIME)
            {
                StartAutoSpin();
                _lastAutoSpinClickTime = 0f;
                return;
            }

            _lastAutoSpinClickTime = currentTime;

            // 자동 스핀 횟수 순환
            int currentIndex = System.Array.IndexOf(_autoSpinOptions, _autoSpinCount);
            currentIndex = (currentIndex + 1) % _autoSpinOptions.Length;
            _autoSpinCount = _autoSpinOptions[currentIndex];

            UpdateAutoSpinButton();
            ShowToast($"Auto-spin: x{_autoSpinCount} (Double-tap to start)", new Color(0.7f, 0.7f, 0.9f), 1f);
        }

        public void StartAutoSpin()
        {
            if (_isAutoSpinning || _autoSpinCount <= 0) return;

            _isAutoSpinning = true;
            _autoSpinRemaining = _autoSpinCount;
            UpdateAutoSpinButton();
            StartCoroutine(AutoSpinCoroutine());
        }

        public void StopAutoSpin()
        {
            _isAutoSpinning = false;
            _autoSpinRemaining = 0;
            UpdateAutoSpinButton();
            ShowToast("Auto-spin stopped", Color.yellow);
        }

        private System.Collections.IEnumerator AutoSpinCoroutine()
        {
            while (_isAutoSpinning && _autoSpinRemaining > 0)
            {
                // 스핀 중이면 대기
                while (_game.Slot.IsSpinning)
                {
                    yield return null;
                }

                // 골드 부족 체크
                if (_currentBetAmount <= 0 || !_game.Gold.CanAfford(_currentBetAmount))
                {
                    ShowToast("Auto-spin stopped: Not enough gold!", Color.red);
                    StopAutoSpin();
                    yield break;
                }

                // 스핀 실행
                bool started = _game.Slot.TrySpin(_currentBetAmount);
                if (started)
                {
                    _autoSpinRemaining--;
                    UpdateAutoSpinButton();
                }
                else
                {
                    ShowToast("Auto-spin stopped: Cannot spin", Color.red);
                    StopAutoSpin();
                    yield break;
                }

                // 스핀 완료 대기
                while (_game.Slot.IsSpinning)
                {
                    yield return null;
                }

                // 잭팟 당첨 시 중지
                // (OnSlotSpinComplete에서 체크하여 StopAutoSpin 호출)

                // 다음 스핀 전 짧은 딜레이
                yield return new WaitForSeconds(0.5f);
            }

            if (_autoSpinRemaining <= 0)
            {
                ShowToast("Auto-spin completed!", new Color(0.5f, 0.9f, 0.5f));
            }
            _isAutoSpinning = false;
            UpdateAutoSpinButton();
        }

        private void UpdateAutoSpinButton()
        {
            if (_autoSpinText == null) return;

            Button autoBtn = _autoSpinButton ?? _autoSpinButtonRef;
            Image autoImg = autoBtn?.GetComponent<Image>();
            bool hasCustomSprites = _allButtonSprites != null && _allButtonSprites.Length > 0;

            if (_isAutoSpinning)
            {
                _autoSpinText.text = $"STOP\n({_autoSpinRemaining})";
                if (autoImg != null)
                {
                    // 커스텀 스프라이트가 있으면 붉은 틴트, 없으면 색상 변경
                    autoImg.color = hasCustomSprites
                        ? new Color(1f, 0.7f, 0.7f, 1f)  // 붉은 틴트
                        : new Color(0.8f, 0.3f, 0.3f);
                }
            }
            else
            {
                _autoSpinText.text = $"AUTO\nx{_autoSpinCount}";
                if (autoImg != null)
                {
                    autoImg.color = hasCustomSprites
                        ? Color.white
                        : new Color(0.3f, 0.5f, 0.7f);
                }
            }
        }

        // 길게 눌러 자동 스핀 시작
        public void OnAutoSpinHold()
        {
            if (!_isAutoSpinning)
            {
                StartAutoSpin();
            }
        }

        #endregion

        #region Slot Events

        private void OnSlotSpinStart()
        {
            _spinButton.interactable = false;
            SetBetButtonsInteractable(false);
            SetSpinState(SpinUIState.Spinning);

            if (_resultGroup != null)
            {
                _resultGroup.DOKill();
                _resultGroup.alpha = 0f;
            }
            else if (_resultText != null)
            {
                _resultText.gameObject.SetActive(false);
                _resultText.alpha = 1f;
            }

            // 3x3 그리드 모든 릴 스핀 애니메이션 시작 (9개)
            for (int i = 0; i < 9; i++)
            {
                if (i < _reelSymbols.Length && _reelSymbols[i] != null)
                {
                    _isReelSpinning[i] = true;
                    if (_spinCoroutines[i] != null)
                        StopCoroutine(_spinCoroutines[i]);
                    _spinCoroutines[i] = StartCoroutine(SpinReelAnimation(i));
                }
            }
        }

        /// <summary>
        /// 릴 스핀 애니메이션 코루틴 - 심볼이 빠르게 변경됨
        /// </summary>
        private System.Collections.IEnumerator SpinReelAnimation(int reelIndex)
        {
            // 범위 체크
            if (reelIndex < 0 || reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null)
                yield break;

            float spinSpeed = 0.05f; // 심볼 변경 속도
            int symbolCount = _symbolSprites != null && _symbolSprites.Length > 0
                ? _symbolSprites.Length
                : _game.Config.symbolCount;

            while (reelIndex < _isReelSpinning.Length && _isReelSpinning[reelIndex])
            {
                // 랜덤 심볼로 변경
                int randomSymbol = UnityEngine.Random.Range(0, symbolCount);
                SetReelSymbol(reelIndex, randomSymbol);

                // 심볼 변경 시 살짝 흔들림 효과
                if (_reelSymbols[reelIndex] != null)
                {
                    _reelSymbols[reelIndex].transform.DOKill();
                    _reelSymbols[reelIndex].transform.localScale = Vector3.one;
                    _reelSymbols[reelIndex].transform.DOPunchScale(Vector3.one * 0.08f, spinSpeed * 0.7f, 0, 0);
                }

                yield return new WaitForSeconds(spinSpeed);
            }
        }

        /// <summary>
        /// 릴에 심볼 설정
        /// </summary>
        private void SetReelSymbol(int reelIndex, int symbolIndex)
        {
            if (reelIndex < 0 || _reelSymbols == null || reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null)
                return;

            Sprite sprite = GetSymbolSprite(symbolIndex);
            if (sprite != null)
            {
                _reelSymbols[reelIndex].sprite = sprite;
                _reelSymbols[reelIndex].color = Color.white;
            }
            else
            {
                _reelSymbols[reelIndex].sprite = null;
                _reelSymbols[reelIndex].color = GetSymbolColor(symbolIndex);
            }
        }

        private void OnReelStop(int reelIndex, int symbolIndex)
        {
            if (reelIndex >= 0 && reelIndex < _reelSymbols.Length && _reelSymbols[reelIndex] != null)
            {
                // 스핀 애니메이션 중지
                _isReelSpinning[reelIndex] = false;
                if (reelIndex < _spinCoroutines.Length && _spinCoroutines[reelIndex] != null)
                {
                    StopCoroutine(_spinCoroutines[reelIndex]);
                    _spinCoroutines[reelIndex] = null;
                }

                // 최종 심볼 설정
                _reelSymbols[reelIndex].transform.DOKill();
                _reelSymbols[reelIndex].transform.localScale = Vector3.one;
                _reelSymbols[reelIndex].transform.rotation = Quaternion.identity;

                SetReelSymbol(reelIndex, symbolIndex);

                // 정지 효과 (바운스)
                _reelSymbols[reelIndex].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 4, 0.5f);

                // 정지 플래시 효과
                _reelSymbols[reelIndex].DOColor(Color.white * 1.3f, 0.08f)
                    .OnComplete(() => _reelSymbols[reelIndex].DOColor(Color.white, 0.15f));
            }

            if (_spinState == SpinUIState.Spinning)
            {
                SetSpinState(SpinUIState.Stopping);
            }
        }

        private void OnSlotSpinComplete(SlotResult result)
        {
            _spinButton.interactable = true;
            SetBetButtonsInteractable(true);
            SetSpinState(SpinUIState.Result);

            // 세션 통계 업데이트
            _sessionSpins++;
            if (result.IsWin)
            {
                _sessionWins++;
                _sessionEarnings += result.FinalReward - result.BetAmount;
            }
            else
            {
                _sessionEarnings -= result.BetAmount;
            }
            UpdateStatistics();

            string message;
            Color color;

            switch (result.Outcome)
            {
                case SlotOutcome.MegaJackpot:
                    message = $"MEGA JACKPOT! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = _jackpotColor;
                    CelebrationEffect();
                    break;
                case SlotOutcome.Jackpot:
                    message = $"JACKPOT! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = _jackpotColor;
                    CelebrationEffect();
                    break;
                case SlotOutcome.BigWin:
                    message = $"BIG WIN! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = Color.green;
                    break;
                case SlotOutcome.SmallWin:
                    message = $"Win! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = Color.cyan;
                    break;
                case SlotOutcome.MiniWin:
                    message = $"Mini Win! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = Color.white;
                    break;
                case SlotOutcome.Draw:
                    message = "Draw - Money Back!";
                    color = Color.gray;
                    break;
                default:
                    message = "No Match...";
                    color = Color.gray;
                    break;
            }

            ShowResult(message, color);

            int[] highlightIndices = GetWinningReelIndices(result);
            if (highlightIndices.Length > 0)
            {
                HighlightReels(highlightIndices, color);
            }

            // 잭팟 당첨 시 자동 스핀 중지
            if (_isAutoSpinning && (result.Outcome == SlotOutcome.Jackpot || result.Outcome == SlotOutcome.MegaJackpot))
            {
                StopAutoSpin();
                ShowToast("JACKPOT! Auto-spin stopped", new Color(1f, 0.8f, 0.2f));
            }

            DOVirtual.DelayedCall(1.2f, () => SetSpinState(SpinUIState.Ready));
        }

        private void ShowResult(string message, Color color)
        {
            if (_resultText == null) return;

            _resultText.text = message;
            _resultText.color = color;

            if (_resultGroup != null)
            {
                _resultGroup.DOKill();
                _resultGroup.alpha = 1f;
                _resultText.transform.localScale = Vector3.one * 0.9f;
                _resultText.transform.DOKill();
                _resultText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

                _resultTween?.Kill();
                _resultTween = _resultGroup.DOFade(0f, 0.5f).SetDelay(3f);
            }
            else
            {
                _resultText.gameObject.SetActive(true);
                _resultText.transform.localScale = Vector3.zero;
                _resultText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

                DOVirtual.DelayedCall(3f, () =>
                {
                    _resultText.DOFade(0, 0.5f).OnComplete(() =>
                    {
                        _resultText.gameObject.SetActive(false);
                        _resultText.alpha = 1f;
                    });
                });
            }
        }

        private int[] GetWinningReelIndices(SlotResult result)
        {
            if (result == null || result.WinningPayline == null || result.WinningPayline.Length == 0)
                return Array.Empty<int>();

            // 당첨 페이라인의 인덱스들을 반환
            System.Collections.Generic.HashSet<int> winningIndices = new System.Collections.Generic.HashSet<int>();

            // 3x3 페이라인 정의 (SlotManager.SlotPaylines와 동일)
            int[][] paylines = new int[][]
            {
                new int[] { 3, 4, 5 },  // 중간 가로
                new int[] { 0, 1, 2 },  // 상단 가로
                new int[] { 6, 7, 8 },  // 하단 가로
                new int[] { 0, 4, 8 },  // 대각선 ↘
                new int[] { 6, 4, 2 }   // 대각선 ↗
            };

            foreach (int paylineIdx in result.WinningPayline)
            {
                if (paylineIdx >= 0 && paylineIdx < paylines.Length)
                {
                    foreach (int idx in paylines[paylineIdx])
                    {
                        winningIndices.Add(idx);
                    }
                }
            }

            int[] resultArray = new int[winningIndices.Count];
            winningIndices.CopyTo(resultArray);
            return resultArray;
        }

        private void HighlightReels(int[] indices, Color color)
        {
            if (indices == null || _reelSymbols == null) return;

            for (int i = 0; i < indices.Length; i++)
            {
                int reelIndex = indices[i];
                if (reelIndex < 0 || reelIndex >= _reelSymbols.Length) continue;

                if (_reelSymbols[reelIndex] != null)
                {
                    _reelSymbols[reelIndex].transform.DOKill();
                    _reelSymbols[reelIndex].transform.localScale = Vector3.one;
                    _reelSymbols[reelIndex].transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 5, 0.6f);
                }

                if (reelIndex < _reelFrames.Length && _reelFrames[reelIndex] != null)
                {
                    Image frame = _reelFrames[reelIndex];
                    Color original = _reelFrameBaseColor;
                    frame.DOKill();
                    frame.DOColor(color, 0.12f).SetLoops(6, LoopType.Yoyo)
                        .OnComplete(() => frame.color = original);
                }
            }
        }

        private void CelebrationEffect()
        {
            // 화면 플래시
            GameObject flash = CreatePanel(_mainCanvas.GetComponent<RectTransform>(), "Flash",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.5f));

            flash.GetComponent<Image>().DOFade(0, 0.5f).OnComplete(() => Destroy(flash));

            // 화면 흔들림
            _mainCanvas.transform.DOShakePosition(0.5f, 30f, 20);
        }

        #endregion
    }
}
