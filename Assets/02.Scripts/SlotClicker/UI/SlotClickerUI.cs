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
        [SerializeField] private Vector2 _slotPanelSize = new Vector2(192.167f, 192.167f);
        [SerializeField] private float _slotCellSize = 52.045f;
        [SerializeField] private float _slotCellSpacing = 58.05f;
        [SerializeField] private Vector2 _slotPanelPosition = new Vector2(0, -152.132f); // 위로 이동 (클릭 영역과 겹치지 않도록)

        [Header("=== 스핀 프로파일 ===")]
        [Tooltip("슬롯 스핀 애니메이션 설정. 없으면 기본값 사용")]
        [SerializeField] private SlotClickerSpinProfile _spinProfile;

        [Header("=== 클릭 영역 설정 ===")]
        [SerializeField] private Vector2 _clickAreaSize = new Vector2(180.156f, 32.028f); // 크기 축소
        [SerializeField] private Vector2 _clickAreaPosition = new Vector2(0, -88.076f); // 슬롯 아래, 베팅 버튼 위에 배치

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
        private Sprite _coinSprite;       // 플로팅 텍스트용 코인 스프라이트

        [Header("=== 색상 설정 ===")]
        [SerializeField] private Color _normalClickColor = new Color(0.2f, 0.6f, 0.2f);
        [SerializeField] private Color _criticalColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color _jackpotColor = new Color(1f, 0.2f, 0.2f);

        [Header("=== 클릭 피드백 ===")]
        [SerializeField] private bool _enableClickRipple = true;
        [SerializeField, Range(0.03f, 0.2f)] private float _clickPulseStrength = 0.08f;
        [SerializeField, Range(0.08f, 0.35f)] private float _criticalPulseStrength = 0.16f;
        [SerializeField, Range(0.08f, 0.4f)] private float _clickPulseDuration = 0.18f;
        [SerializeField, Range(0.25f, 1.2f)] private float _rippleDuration = 0.55f;
        [SerializeField, Range(1.2f, 3.5f)] private float _rippleMaxScale = 2.3f;
        [SerializeField] private Color _rippleColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        [SerializeField] private Color _criticalRippleColor = new Color(1f, 0.55f, 0.15f, 1f);

        [Header("=== 클릭 사운드 ===")]
        [SerializeField] private AudioClip _clickSfx;
        [SerializeField] private AudioClip _criticalClickSfx;
        [SerializeField, Range(0f, 1f)] private float _clickSfxVolume = 0.7f;
        [SerializeField, Range(0f, 0.2f)] private float _clickPitchJitter = 0.06f;
        [SerializeField, Range(0.9f, 1.4f)] private float _criticalPitch = 1.08f;
        [SerializeField, Range(1f, 2f)] private float _criticalSfxVolumeMultiplier = 1.35f;

        [Header("=== 크리티컬 연출 ===")]
        [SerializeField] private bool _enableCriticalFlash = true;
        [SerializeField] private Color _criticalFlashColor = new Color(1f, 0.85f, 0.35f, 0.6f);
        [SerializeField, Range(0.05f, 0.35f)] private float _criticalFlashDuration = 0.2f;
        [SerializeField] private bool _enableCriticalShake = true;
        [SerializeField, Range(0.05f, 0.6f)] private float _criticalShakeDuration = 0.22f;
        [SerializeField, Range(5f, 60f)] private float _criticalShakeStrength = 22f;
        [SerializeField, Range(8, 40)] private int _criticalShakeVibrato = 20;
        [SerializeField, Range(0f, 90f)] private float _criticalShakeRandomness = 70f;

        [Header("=== 연속 클릭 / 오버드라이브 ===")]
        [SerializeField] private bool _enableClickStreak = true;
        [SerializeField, Range(0.15f, 1f)] private float _streakWindow = 0.45f;
        [SerializeField, Range(4, 20)] private int _streakThreshold = 8;
        [SerializeField, Range(1, 6)] private int _streakMaxLevel = 4;
        [SerializeField, Range(0.02f, 0.12f)] private float _streakPulseBonusPerLevel = 0.045f;
        [SerializeField, Range(0.02f, 0.2f)] private float _streakGlowBonusPerLevel = 0.08f;
        [SerializeField, Range(0f, 0.08f)] private float _streakPitchBonusPerLevel = 0.03f;
        [SerializeField, Range(0f, 0.15f)] private float _streakVolumeBonusPerLevel = 0.06f;
        [SerializeField, Range(0, 4)] private int _streakExtraRipplesPerLevel = 1;
        [SerializeField, Range(0.02f, 0.2f)] private float _streakRippleInterval = 0.06f;
        [SerializeField] private bool _enableStreakBurst = true;
        [SerializeField] private Color _streakBurstColor = new Color(1f, 0.55f, 0.2f, 0.75f);
        [SerializeField, Range(1.1f, 3f)] private float _streakBurstScaleMultiplier = 1.8f;
        [SerializeField, Range(0.2f, 2f)] private float _streakBurstCooldown = 0.9f;
        [SerializeField] private bool _hitStopOnStreakBurst = true;
        [SerializeField, Range(1, 4)] private int _streakHitStopMinLevel = 2;

        [Header("=== 파티클 이펙트 ===")]
        [SerializeField] private bool _enableClickParticles = true;
        [SerializeField, Range(3, 15)] private int _normalParticleCount = 5;
        [SerializeField, Range(8, 30)] private int _criticalParticleCount = 12;
        [SerializeField] private Color _particleColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color _criticalParticleColor = new Color(1f, 0.6f, 0.2f, 1f);
        [SerializeField, Range(80f, 300f)] private float _particleSpeed = 180f;
        [SerializeField, Range(0.3f, 1.2f)] private float _particleLifetime = 0.7f;
        [SerializeField, Range(0, 10)] private int _streakParticleBonusPerLevel = 2;

        [Header("=== 클릭 영역 펄스 ===")]
        [SerializeField] private bool _enableIdlePulse = true;
        [SerializeField, Range(0.02f, 0.1f)] private float _idlePulseScale = 0.04f;
        [SerializeField, Range(0.8f, 2.5f)] private float _idlePulseDuration = 1.5f;
        [SerializeField] private Color _idlePulseGlowColor = new Color(1f, 0.95f, 0.7f, 0.3f);

        [Header("=== 미세 화면 쉐이크 ===")]
        [SerializeField] private bool _enableMicroShake = true;
        [SerializeField, Range(0.03f, 0.2f)] private float _microShakeDuration = 0.08f;
        [SerializeField, Range(3f, 25f)] private float _microShakeStrength = 10f;
        [SerializeField, Range(6, 30)] private int _microShakeVibrato = 14;
        [SerializeField, Range(0.02f, 0.2f)] private float _microShakeCooldown = 0.05f;

        [Header("=== 히트 스톱 효과 ===")]
        [SerializeField] private bool _enableHitStop = true;
        [SerializeField, Range(0.02f, 0.2f)] private float _hitStopDuration = 0.08f;
        [SerializeField, Range(0f, 0.5f)] private float _hitStopTimeScale = 0.1f;

        [Header("=== 화면 테두리 글로우 ===")]
        [SerializeField] private bool _enableScreenGlow = true;
        [SerializeField] private Color _criticalScreenGlowColor = new Color(1f, 0.7f, 0.2f, 0.6f);
        [SerializeField] private Color _jackpotScreenGlowColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        [SerializeField, Range(20f, 100f)] private float _screenGlowThickness = 50f;
        [SerializeField, Range(0.2f, 1f)] private float _screenGlowDuration = 0.5f;

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

        // ★ 모바일 최적화: 클릭 디바운싱
        private float _lastClickTime = -0.1f;
        private const float CLICK_MIN_INTERVAL = 0.05f; // 50ms 최소 간격

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
        private HashSet<GameObject> _activeFloatingTexts = new HashSet<GameObject>();
        private Queue<GameObject> _activeFloatingTextsQueue = new Queue<GameObject>(); // FIFO 순서 유지
        private const int POOL_INITIAL_SIZE = 10;
        private const int POOL_MAX_SIZE = 30;

        // 클릭 리플 이펙트 풀
        private GameObject _ripplePrefab;
        private Queue<GameObject> _ripplePool = new Queue<GameObject>();
        private HashSet<GameObject> _activeRipples = new HashSet<GameObject>();
        private Queue<GameObject> _activeRipplesQueue = new Queue<GameObject>(); // FIFO 순서 유지
        private const int RIPPLE_POOL_INITIAL_SIZE = 12;
        private const int RIPPLE_POOL_MAX_SIZE = 40;

        // WebGL 성능 최적화
        private bool _isWebGL = false;
        private float _lastEffectTime = 0f;
        private const float EFFECT_THROTTLE_INTERVAL = 0.05f; // 50ms 간격으로 이펙트 제한
        private int _frameEffectCount = 0;
        private const int MAX_EFFECTS_PER_FRAME = 3;

        // 클릭 영역 시각 피드백
        private RectTransform _clickAreaRect;
        private Image _clickAreaImage;
        private Image _clickGlowImage;
        private Color _clickAreaBaseColor = Color.white;
        private Tween _clickGlowTween;
        private bool _createdClickGlow;

        // 클릭 사운드
        private AudioSource _clickAudioSource;
        private bool _createdClickAudioSource;

        // 크리티컬 연출 레이어/셰이크
        private Image _criticalFlashImage;
        private Tween _criticalFlashTween;
        private Transform _shakeTarget;
        private Vector3 _shakeOriginalPosition;
        private Tween _shakeTween;
        private bool _createdCriticalFlash;

        // 연속 클릭(스트릭) 상태
        private int _clickStreakCount = 0;
        private float _lastClickRealtime = -999f;
        private int _streakLevel = 0;
        private int _previousStreakLevel = 0;
        private float _lastStreakBurstRealtime = -999f;
        private bool _streakBurstTriggeredThisClick = false;

        // 파티클 이펙트 풀
        private GameObject _particlePrefab;
        private Queue<GameObject> _particlePool = new Queue<GameObject>();
        private HashSet<GameObject> _activeParticles = new HashSet<GameObject>();
        private Queue<GameObject> _activeParticlesQueue = new Queue<GameObject>(); // FIFO 순서 유지
        private const int PARTICLE_POOL_INITIAL_SIZE = 20;
        private const int PARTICLE_POOL_MAX_SIZE = 60;

        // 클릭 영역 펄스
        private Tween _idlePulseTween;
        private Tween _idleGlowPulseTween;
        private bool _isIdlePulsing = false;

        // 히트 스톱
        private Coroutine _hitStopCoroutine;
        private float _originalTimeScale = 1f;

        // 미세 화면 쉐이크
        private Tween _microShakeTween;
        private float _lastMicroShakeRealtime = -999f;
        private Transform _microShakeTarget;
        private Vector3 _microShakeOriginalPosition;
        private bool _microShakeHadOriginalPosition = false;

        // 화면 테두리 글로우
        private Image[] _screenGlowEdges;
        private Tween[] _screenGlowTweens;
        private bool _createdScreenGlow;

        // 슬롯 스핀 관련 (3x3 = 9개)
        private Coroutine[] _spinCoroutines = new Coroutine[9];
        private bool[] _isReelSpinning = new bool[9];
        private Image[] _reelFrames = new Image[9];
        private readonly Color _reelFrameBaseColor = new Color(0.2f, 0.15f, 0.25f, 1f);
        private Tween _resultTween;
        private Tween _toastTween;
        private RectTransform _slotAreaRect;

        // 슬롯 승리 피드백 시스템
        private SlotWinFeedback _slotWinFeedback;

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

        private void LateUpdate()
        {
            // 매 프레임 이펙트 카운터 리셋
            _frameEffectCount = 0;
        }

        private System.Collections.IEnumerator WaitForGameManager()
        {
            // GameManager 초기화 대기
            while (GameManager.Instance == null || GameManager.Instance.Gold == null)
            {
                yield return null;
            }

            _game = GameManager.Instance;

            // 폰트 매니저 초기화
            FontManager.Initialize();

            if (_autoCreateUI)
            {
                CreateUI();
            }
            else
            {
                // 에디터에서 설정한 참조 사용
                SetupExistingUI();
            }

            // 생성된 모든 UI에 커스텀 폰트 적용
            ApplyCustomFont();

            BindEvents();

            // 초기 골드 값 설정 (애니메이션 없이)
            _displayedGold = _game.Gold.CurrentGold;

            // 자동 스핀 초기화
            _autoSpinCount = _autoSpinOptions[0];

            UpdateUI();
            UpdateStatistics();
            UpdateAutoSpinButton();
            SetSpinState(SpinUIState.Ready);

            // 향상된 피드백 시스템 초기화
            SetupEnhancedFeedbackSystems();

            // WebGL 해상도 수정 컴포넌트 추가
            SetupWebGLResolutionFix();

            // 일일 로그인 시스템 초기화
            SetupDailyLoginSystem();

            // 일일 퀘스트 시스템 초기화
            SetupDailyQuestSystem();

            // 첫 실행 시 도움말 자동 표시
            CheckFirstTimeTutorial();
        }

        private void CheckFirstTimeTutorial()
        {
            // 튜토리얼을 본 적 없거나, 총 스핀이 5회 미만이면 표시
            bool shouldShowTutorial = _game != null && _game.PlayerData != null &&
                (!_game.PlayerData.hasSeenTutorial || _game.PlayerData.totalSpins < 5);

            if (shouldShowTutorial)
            {
                // 1.5초 후 도움말 표시 (UI 로딩 후)
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    if (_helpPanel != null && !_isHelpVisible)
                    {
                        ToggleHelpPanel();
                        _game.PlayerData.hasSeenTutorial = true;
                        ShowToast("게임 방법을 확인하세요!", new Color(0.5f, 0.8f, 1f), 3f);
                    }
                });
            }
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

            // 클릭 피드백(리플/글로우) 준비
            SetupClickFeedback();

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

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(390, 844);  // 기준 해상도 (CanvasScaler가 자동 스케일링)
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;  // 가로/세로 균형 맞춤

                var raycaster = canvasObj.AddComponent<GraphicRaycaster>();
                raycaster.blockingMask = LayerMask.GetMask("UI");  // WebGL 호환성
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

            // === 클릭 피드백(리플/글로우) 준비 ===
            SetupClickFeedback();

            // === 업그레이드 버튼 ===
            CreateUpgradeButton(canvasRect);

            // === 업그레이드 UI ===
            CreateUpgradeUI();

            // === 프레스티지 버튼 ===
            CreatePrestigeButton(canvasRect);

            // === 프레스티지 UI ===
            CreatePrestigeUI();

            // === 도움말 버튼 ===
            CreateHelpButton(canvasRect);

            // === 도움말 UI ===
            CreateHelpUI();

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
            hudRect.anchoredPosition = new Vector2(0, -20.017f); // 상단 오프셋
            hudRect.sizeDelta = new Vector2(0, 40.035f); // HUD 높이

            // 골드 표시 (상단 좌측)
            GameObject goldObj = CreateTextObject(hudRect, "GoldText", "GOLD: 0",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(16.014f, -6.005f), 16.815f);
            _goldText = goldObj.GetComponent<TextMeshProUGUI>();
            _goldText.color = new Color(1f, 0.85f, 0.2f);
            _goldText.alignment = TextAlignmentOptions.Left;

            // 칩 표시 (상단 우측)
            GameObject chipsObj = CreateTextObject(hudRect, "ChipsText", "0 Chips",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16.014f, -6.005f), 12.81f);
            _chipsText = chipsObj.GetComponent<TextMeshProUGUI>();
            _chipsText.color = new Color(0.6f, 0.8f, 1f);
            _chipsText.alignment = TextAlignmentOptions.Right;

            // 세션 통계 (하단 좌측)
            GameObject statsObj = CreateTextObject(hudRect, "StatsText", "Spins: 0 | Wins: 0",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(16.014f, 6.005f), 8.81f);
            _statsText = statsObj.GetComponent<TextMeshProUGUI>();
            _statsText.color = new Color(0.7f, 0.7f, 0.7f);
            _statsText.alignment = TextAlignmentOptions.Left;

            // 승률 표시 (하단 중앙)
            GameObject winRateObj = CreateTextObject(hudRect, "WinRateText", "Win Rate: --",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6.005f), 8.81f);
            _winRateText = winRateObj.GetComponent<TextMeshProUGUI>();
            _winRateText.color = new Color(0.5f, 0.9f, 0.5f);
            _winRateText.alignment = TextAlignmentOptions.Center;

            // 프레스티지 진행률 (하단 우측)
            GameObject prestigeObj = CreateTextObject(hudRect, "PrestigeText", "Prestige: 0%",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16.014f, 6.005f), 8.81f);
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
            AddOutline(slotPanel, new Color(0.8f, 0.6f, 0.2f), 1.601f);

            // 전체 슬롯 영역에 Mask 추가 (WebGL 호환성 - RectMask2D 대신 사용)
            Mask slotMask = slotPanel.AddComponent<Mask>();
            slotMask.showMaskGraphic = true; // 배경 이미지 표시

            // 슬롯 패널을 맨 앞으로 이동 (다른 UI 요소 위에 렌더링)
            slotPanel.transform.SetAsLastSibling();

            // 스핀 상태 텍스트 (상단에 배치)
            GameObject stateObj = CreateTextObject(slotRect, "SpinStateText", "READY",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 12.01f), 11.21f);
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

                    // WebGL 호환성: RectMask2D 대신 Mask 사용
                    Mask cellMask = reelBg.AddComponent<Mask>();
                    cellMask.showMaskGraphic = true; // 배경색 표시
                    _reelFrames[idx] = reelBg.GetComponent<Image>();

                    GameObject symbolObj = new GameObject($"Symbol_{idx}");
                    symbolObj.transform.SetParent(reelBg.transform, false);
                    RectTransform symRect = symbolObj.AddComponent<RectTransform>();
                    symRect.anchorMin = Vector2.zero;
                    symRect.anchorMax = Vector2.one;
                    // 마진을 늘려서 애니메이션 시에도 마스크 내부에 유지
                    symRect.offsetMin = new Vector2(3.203f, 3.203f);
                    symRect.offsetMax = new Vector2(-3.203f, -3.203f);

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
        /// WebGL 호환성: 앵커, 피벗, 위치, 크기 모두 강제 설정
        /// </summary>
        private void ApplyClickAreaSettings()
        {
            if (_clickArea == null) return;

            RectTransform clickRect = _clickArea.GetComponent<RectTransform>();
            if (clickRect != null)
            {
                // ★ 앵커/피벗 강제 설정 (씬 파일의 이전 값 무시)
                clickRect.anchorMin = new Vector2(0.5f, 0.5f);  // 화면 중앙
                clickRect.anchorMax = new Vector2(0.5f, 0.5f);
                clickRect.pivot = new Vector2(0.5f, 0.5f);

                // 위치/크기 강제 적용
                Vector2 correctPosition = new Vector2(0, -88.076f);  // 슬롯 아래
                Vector2 correctSize = new Vector2(180.156f, 32.028f);      // 작은 크기

                clickRect.anchoredPosition = correctPosition;
                clickRect.sizeDelta = correctSize;

                // ★ WebGL용 레이아웃 강제 재계산
                LayoutRebuilder.ForceRebuildLayoutImmediate(clickRect);

                Debug.Log($"[SlotClickerUI] Click area FULLY reset - Anchor:(0.5,0.5), Position:{correctPosition}, Size:{correctSize}");
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

            // 3x3 그리드 슬롯 패널 새로 생성 (강제로 올바른 위치 적용)
            Vector2 correctSlotPosition = new Vector2(0, -152.132f);
            Vector2 correctSlotSize = new Vector2(192.167f, 192.167f);
            GameObject slotPanel = CreatePanel(canvasRect, "SlotPanel3x3", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                correctSlotPosition, correctSlotSize, new Color(0.15f, 0.1f, 0.2f, 1f));
            RectTransform slotRect = slotPanel.GetComponent<RectTransform>();

            // ★ 앵커/피벗 명시적 재설정 (WebGL 호환성)
            slotRect.anchorMin = new Vector2(0.5f, 1f);  // 상단 중앙
            slotRect.anchorMax = new Vector2(0.5f, 1f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = correctSlotPosition;
            slotRect.sizeDelta = correctSlotSize;

            _slotAreaRect = slotRect;
            _slotPanel = slotRect;

            // WebGL 호환성: Mask 컴포넌트 추가 (심볼이 프레임 밖으로 나가지 않도록)
            Mask slotMask = slotPanel.AddComponent<Mask>();
            slotMask.showMaskGraphic = true;

            // ★ 레이아웃 강제 재계산
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotRect);

            // 슬롯 패널을 맨 앞으로 이동 (다른 UI 요소 위에 렌더링)
            slotPanel.transform.SetAsLastSibling();

            // 슬롯 프레임 아웃라인
            AddOutline(slotPanel, new Color(0.8f, 0.6f, 0.2f), 1.601f);

            // 스핀 상태 텍스트
            GameObject stateObj = CreateTextObject(slotRect, "SpinStateText", "READY",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 12.01f), 11.21f);
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

                    // WebGL 호환성: RectMask2D 대신 Mask 사용
                    Mask cellMask = reelBg.AddComponent<Mask>();
                    cellMask.showMaskGraphic = true; // 배경색 표시
                    _reelFrames[idx] = reelBg.GetComponent<Image>();

                    GameObject symbolObj = new GameObject($"Symbol_{idx}");
                    symbolObj.transform.SetParent(reelBg.transform, false);
                    RectTransform symRect = symbolObj.AddComponent<RectTransform>();
                    symRect.anchorMin = Vector2.zero;
                    symRect.anchorMax = Vector2.one;
                    // 마진을 늘려서 애니메이션 시에도 마스크 내부에 유지
                    symRect.offsetMin = new Vector2(8, 8);
                    symRect.offsetMax = new Vector2(-8, -8);

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
                // 설정된 크기 유지 (스프라이트 비율 무시)
                Debug.Log("[SlotClickerUI] Custom click panel sprite applied");
            }
            else
            {
                // 기본 스타일: 아웃라인 추가
                AddOutline(clickPanel, new Color(0.6f, 0.4f, 0.1f), 2.002f);
            }

            // 버튼 컴포넌트
            _clickArea = clickPanel.AddComponent<Button>();
            _clickArea.transition = Selectable.Transition.None;

            // 테이블 텍스트
            GameObject tableText = CreateTextObject(clickRect, "TableText", "TAP TO EARN",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 11.21f); // 축소된 버튼에 맞게 폰트 크기 조정
            TextMeshProUGUI tableTmp = tableText.GetComponent<TextMeshProUGUI>();
            tableTmp.color = new Color(1f, 0.9f, 0.6f, 0.8f);
            tableTmp.alignment = TextAlignmentOptions.Center;
            tableTmp.raycastTarget = false;

            // 펄스 애니메이션
            tableTmp.transform.DOScale(1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        private void CreateBettingUI(RectTransform parent)
        {
            // 베팅 패널 - 하단에 고정 (크기 확대)
            GameObject betPanel = CreatePanel(parent, "BetPanel", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, 104.09f), new Color(0.12f, 0.1f, 0.18f, 0.95f));  // 높이 확대
            RectTransform betRect = betPanel.GetComponent<RectTransform>();
            betRect.anchoredPosition = new Vector2(0, 52.045f); // 하단 오프셋

            // 현재 베팅액 표시
            GameObject betAmountObj = CreateTextObject(betRect, "BetAmountText", "Bet: 0",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6.005f), 13.612f);
            _betAmountText = betAmountObj.GetComponent<TextMeshProUGUI>();
            _betAmountText.color = Color.white;
            _betAmountText.alignment = TextAlignmentOptions.Center;

            // 베팅 버튼들 - 크기 확대 및 가로 배치 개선
            _betButtons = new Button[4];
            string[] betLabels = { "10%", "30%", "50%", "ALL" };
            float[] betValues = { 0.1f, 0.3f, 0.5f, 1f };
            float buttonWidth = 72.062f;  // 버튼 폭
            float buttonSpacing = 7.687f;  // 간격
            float totalWidth = (buttonWidth * 4) + (buttonSpacing * 3);
            float startX = -totalWidth / 2 + buttonWidth / 2;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                float betValue = betValues[i];

                GameObject btnObj = CreateButton(betRect, $"BetBtn_{i}", betLabels[i],
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + (i * (buttonWidth + buttonSpacing)), 14.012f),
                    new Vector2(buttonWidth, 28.825f),  // 버튼 높이
                    new Color(0.3f, 0.3f, 0.5f));

                _betButtons[i] = btnObj.GetComponent<Button>();
                _betButtons[i].onClick.AddListener(() => SetBetPercentage(betValue));

                // 버튼 텍스트 크기 확대
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.fontSize = 15.372f;
            }

            // 스핀 버튼 - 크게 확대하여 하단 중앙에 배치
            float spinWidth = 124.908f;
            float autoWidth = 76.867f;
            float spinAutoHeight = 38.434f;
            float spinAutoGap = 8.007f;
            float spinAutoTotal = spinWidth + autoWidth + spinAutoGap;
            float spinX = -spinAutoTotal / 2f + spinWidth / 2f;
            float autoX = spinX + spinWidth / 2f + spinAutoGap + autoWidth / 2f;
            float spinAutoY = 21.201f;

            GameObject spinObj = CreateButton(betRect, "SpinButton", "SPIN!",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(spinX, spinAutoY), new Vector2(spinWidth, spinAutoHeight),  // 스핀 버튼 크기
                new Color(0.8f, 0.2f, 0.2f));
            _spinButton = spinObj.GetComponent<Button>();
            _spinButton.onClick.AddListener(OnSpinClicked);

            _spinButtonText = spinObj.GetComponentInChildren<TextMeshProUGUI>();
            _spinButtonText.fontSize = 20.178f;  // 폰트 크기
            _spinButtonText.fontStyle = FontStyles.Bold;

            // 자동 스핀 버튼 - 크게 확대하여 스핀 버튼 우측에 배치
            GameObject autoSpinObj = CreateButton(betRect, "AutoSpinButton", "AUTO",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(autoX, spinAutoY), new Vector2(autoWidth, spinAutoHeight),  // 자동 스핀 버튼 크기
                new Color(0.3f, 0.5f, 0.7f));
            _autoSpinButton = autoSpinObj.GetComponent<Button>();
            _autoSpinButton.onClick.AddListener(OnAutoSpinClicked);

            _autoSpinText = autoSpinObj.GetComponentInChildren<TextMeshProUGUI>();
            _autoSpinText.fontSize = 15.372f;  // 폰트 크기
            _autoSpinText.fontStyle = FontStyles.Bold;
        }

        private void CreateResultText(RectTransform parent)
        {
            RectTransform resultParent = _slotAreaRect != null ? _slotAreaRect : parent;

            // 결과 배너 - 슬롯 영역 하단 고정
            _resultPanel = CreatePanel(resultParent, "ResultPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, -12.01f), new Vector2(192.167f, 22.019f),
                new Color(0f, 0f, 0f, 0.6f));

            RectTransform panelRect = _resultPanel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0.5f, 1f);

            _resultGroup = _resultPanel.AddComponent<CanvasGroup>();
            _resultGroup.alpha = 0f;

            GameObject resultObj = CreateTextObject(panelRect, "ResultText", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 17.615f);
            _resultText = resultObj.GetComponent<TextMeshProUGUI>();
            _resultText.color = Color.white;
            _resultText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateToast(RectTransform parent)
        {
            GameObject toastPanel = CreatePanel(parent, "ToastPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 116.101f), new Vector2(192.167f, 18.015f),
                new Color(0f, 0f, 0f, 0.6f));

            _toastGroup = toastPanel.AddComponent<CanvasGroup>();
            _toastGroup.alpha = 0f;

            RectTransform panelRect = toastPanel.GetComponent<RectTransform>();

            GameObject toastObj = CreateTextObject(panelRect, "ToastText", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 12.81f);
            _toastText = toastObj.GetComponent<TextMeshProUGUI>();
            _toastText.color = Color.white;
            _toastText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateFloatingTextPrefab()
        {
            _floatingTextPrefab = new GameObject("FloatingTextPrefab");
            _floatingTextPrefab.SetActive(false);

            RectTransform rect = _floatingTextPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100.087f, 24.021f);

            // 가로 레이아웃 그룹 추가 (코인 + 텍스트)
            HorizontalLayoutGroup layout = _floatingTextPrefab.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 3.203f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 코인 이미지 추가
            if (_coinSprite != null)
            {
                GameObject coinObj = new GameObject("CoinIcon");
                coinObj.transform.SetParent(_floatingTextPrefab.transform, false);

                RectTransform coinRect = coinObj.AddComponent<RectTransform>();
                coinRect.sizeDelta = new Vector2(16.014f, 16.014f);

                Image coinImage = coinObj.AddComponent<Image>();
                coinImage.sprite = _coinSprite;
                coinImage.preserveAspect = true;
                coinImage.raycastTarget = false;

                // LayoutElement로 크기 고정
                LayoutElement coinLayout = coinObj.AddComponent<LayoutElement>();
                coinLayout.preferredWidth = 16.014f;
                coinLayout.preferredHeight = 16.014f;
            }

            // 텍스트 추가
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(_floatingTextPrefab.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(72.062f, 20.017f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 16.014f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.yellow;

            // LayoutElement로 텍스트 영역 설정
            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 72.062f;
            textLayout.preferredHeight = 20.017f;

            // ContentSizeFitter로 전체 크기 자동 조절
            ContentSizeFitter fitter = _floatingTextPrefab.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
                // 풀이 가득 찼으면 가장 오래된 활성 텍스트 재활용 (O(1) 큐 사용)
                obj = _activeFloatingTextsQueue.Dequeue();
                _activeFloatingTexts.Remove(obj); // HashSet O(1)
                obj.transform.DOKill();
            }

            _activeFloatingTexts.Add(obj); // HashSet O(1)
            _activeFloatingTextsQueue.Enqueue(obj); // 순서 유지
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
            _activeFloatingTexts.Remove(obj); // HashSet O(1)
            // Note: Queue에서 제거하지 않음 - 순서대로 재활용될 때 자연스럽게 정리됨

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
            _activeFloatingTextsQueue.Clear();

            while (_floatingTextPool.Count > 0)
            {
                var obj = _floatingTextPool.Dequeue();
                if (obj != null) Destroy(obj);
            }

            if (_floatingTextPrefab != null)
                Destroy(_floatingTextPrefab);
        }

        /// <summary>
        /// 클릭 피드백(리플/글로우) 준비
        /// </summary>
        private void SetupClickFeedback()
        {
            // 스트릭 상태 초기화
            _clickStreakCount = 0;
            _streakLevel = 0;
            _previousStreakLevel = 0;
            _lastClickRealtime = -999f;
            _lastStreakBurstRealtime = -999f;
            _streakBurstTriggeredThisClick = false;

            EnsureClickVisuals();
            CreateRipplePrefab();
            if (_clickSfx != null || _criticalClickSfx != null)
            {
                EnsureClickAudioSource();
            }
        }

        /// <summary>
        /// 향상된 피드백 시스템 초기화
        /// </summary>
        private void SetupEnhancedFeedbackSystems()
        {
            // WebGL 플랫폼 감지 및 성능 최적화
#if UNITY_WEBGL
            _isWebGL = true;
            // WebGL에서 파티클/이펙트 수 감소
            _normalParticleCount = Mathf.Min(_normalParticleCount, 3);
            _criticalParticleCount = Mathf.Min(_criticalParticleCount, 5);
            _streakParticleBonusPerLevel = Mathf.Min(_streakParticleBonusPerLevel, 1);
            _streakExtraRipplesPerLevel = Mathf.Min(_streakExtraRipplesPerLevel, 0);
            Debug.Log("[SlotClickerUI] WebGL detected - reduced particle counts for performance");
#endif

            // 파티클 이펙트 시스템
            if (_enableClickParticles)
            {
                CreateParticlePrefab();
            }

            // 화면 테두리 글로우 생성
            if (_enableScreenGlow)
            {
                CreateScreenGlowEdges();
            }

            // 클릭 영역 아이들 펄스 시작
            if (_enableIdlePulse)
            {
                StartIdlePulse();
            }

            // 슬롯 승리 피드백 시스템 초기화
            InitializeSlotWinFeedback();
        }

        /// <summary>
        /// 슬롯 승리 피드백 시스템 초기화
        /// </summary>
        private void InitializeSlotWinFeedback()
        {
            if (_mainCanvas == null) return;

            // SlotWinFeedback 컴포넌트 추가 또는 가져오기
            _slotWinFeedback = GetComponent<SlotWinFeedback>();
            if (_slotWinFeedback == null)
            {
                _slotWinFeedback = gameObject.AddComponent<SlotWinFeedback>();
            }

            // 초기화
            _slotWinFeedback.Initialize(_mainCanvas, _reelSymbols, _reelFrames);

            Debug.Log("[SlotClickerUI] SlotWinFeedback initialized");
        }

        /// <summary>
        /// WebGL 해상도 수정 컴포넌트 설정
        /// </summary>
        private void SetupWebGLResolutionFix()
        {
#if UNITY_WEBGL
            // WebGLResolutionFix 컴포넌트 추가
            var resolutionFix = FindObjectOfType<WebGLResolutionFix>();
            if (resolutionFix == null)
            {
                var fixObj = new GameObject("WebGLResolutionFix");
                fixObj.AddComponent<WebGLResolutionFix>();
                DontDestroyOnLoad(fixObj);
                Debug.Log("[SlotClickerUI] WebGLResolutionFix component added");
            }
#endif
        }

        /// <summary>
        /// 일일 로그인 시스템 설정
        /// </summary>
        private DailyLoginUI _dailyLoginUI;

        private void SetupDailyLoginSystem()
        {
            if (_game == null || _game.DailyLogin == null || _mainCanvas == null) return;

            // DailyLoginUI 컴포넌트 추가
            _dailyLoginUI = gameObject.AddComponent<DailyLoginUI>();
            _dailyLoginUI.Initialize(_game.DailyLogin, _mainCanvas);

            // 일일 로그인 체크 (약간의 딜레이 후)
            DOVirtual.DelayedCall(0.5f, () =>
            {
                _game.DailyLogin.CheckDailyLogin();
            });

            // 보상 수령 시 토스트 표시
            _game.DailyLogin.OnDailyRewardClaimed += OnDailyRewardClaimed;

            Debug.Log("[SlotClickerUI] Daily login system initialized");
        }

        private void OnDailyRewardClaimed(DailyLoginReward reward)
        {
            string message = $"Day {reward.Day} 보상 수령!\n골드 {reward.GoldMultiplier:F1}x ({reward.DurationHours}시간)";
            if (reward.BonusChips > 0)
            {
                message += $"\n+ 보너스 칩 {reward.BonusChips}개!";
            }
            ShowToast(message, new Color(1f, 0.8f, 0.2f), 4f);
        }

        /// <summary>
        /// 일일 퀘스트 시스템 설정
        /// </summary>
        private DailyQuestUI _dailyQuestUI;

        private void SetupDailyQuestSystem()
        {
            if (_game == null || _game.DailyQuest == null || _mainCanvas == null) return;

            // DailyQuestUI 컴포넌트 추가
            _dailyQuestUI = gameObject.AddComponent<DailyQuestUI>();
            _dailyQuestUI.Initialize(_game.DailyQuest, _mainCanvas);

            // 퀘스트 완료 시 토스트 표시
            _game.DailyQuest.OnQuestCompleted += OnQuestCompleted;

            // 모든 퀘스트 완료 시 축하 메시지
            _game.DailyQuest.OnAllQuestsCompleted += OnAllQuestsCompleted;

            Debug.Log("[SlotClickerUI] Daily quest system initialized");
        }

        private void OnQuestCompleted(SlotClicker.Core.DailyQuest quest)
        {
            ShowToast($"퀘스트 완료!\n{quest.Description}", new Color(0.3f, 0.8f, 0.3f), 3f);
        }

        private void OnAllQuestsCompleted()
        {
            ShowToast("모든 일일 퀘스트 완료!\n보상을 수령하세요!", new Color(1f, 0.85f, 0.3f), 4f);
        }

        /// <summary>
        /// 모든 UI에 커스텀 폰트 적용
        /// </summary>
        private void ApplyCustomFont()
        {
            if (!FontManager.HasCustomFont) return;

            // 메인 캔버스 하위 모든 TMP에 폰트 적용
            if (_mainCanvas != null)
            {
                FontManager.ApplyFontToAll(_mainCanvas.gameObject);
            }

            // 플로팅 텍스트 프리팹에 폰트 적용
            if (_floatingTextPrefab != null)
            {
                FontManager.ApplyFontToAll(_floatingTextPrefab);
            }

            Debug.Log("[SlotClickerUI] Custom font applied to all UI elements");
        }

        #region Click Streak / Overdrive

        /// <summary>
        /// 연속 클릭 스트릭을 갱신하고 레벨업 버스트를 처리
        /// </summary>
        private void UpdateClickStreak(Vector2 position, bool isCritical)
        {
            _streakBurstTriggeredThisClick = false;

            if (!_enableClickStreak)
            {
                _clickStreakCount = 0;
                _streakLevel = 0;
                _previousStreakLevel = 0;
                return;
            }

            float now = Time.unscaledTime;
            bool withinWindow = now - _lastClickRealtime <= _streakWindow;

            _clickStreakCount = withinWindow ? _clickStreakCount + 1 : 1;
            _lastClickRealtime = now;

            _previousStreakLevel = _streakLevel;
            _streakLevel = CalculateStreakLevel(_clickStreakCount);

            if (_streakLevel > _previousStreakLevel)
            {
                TryPlayStreakBurst(position, isCritical, now);
            }
        }

        private int CalculateStreakLevel(int streakCount)
        {
            if (streakCount < _streakThreshold) return 0;

            int level = 1 + (streakCount - _streakThreshold) / Mathf.Max(1, _streakThreshold);
            return Mathf.Clamp(level, 1, _streakMaxLevel);
        }

        private float GetStreakFactor(float perLevelBonus)
        {
            if (!_enableClickStreak || _streakLevel <= 0) return 1f;
            return 1f + (_streakLevel * perLevelBonus);
        }

        private void TryPlayStreakBurst(Vector2 position, bool isCritical, float now)
        {
            if (!_enableStreakBurst) return;
            if (now - _lastStreakBurstRealtime < _streakBurstCooldown) return;

            _lastStreakBurstRealtime = now;
            _streakBurstTriggeredThisClick = true;
            PlayStreakBurst(position, isCritical);
        }

        /// <summary>
        /// 스트릭 레벨업 시 큰 버스트 연출
        /// </summary>
        private void PlayStreakBurst(Vector2 position, bool isCritical)
        {
            Color burstColor = _streakBurstColor;
            if (!isCritical)
            {
                burstColor.a *= 0.9f;
            }

            float burstScale = _streakBurstScaleMultiplier * (1f + _streakLevel * 0.12f);
            SpawnClickRipple(position, true, burstColor, burstScale, 1.15f);
            SpawnStreakEchoRipples(position, isCritical, burstMode: true);

            // 버스트 순간은 살짝 더 강하게 흔들어 준다
            PlayMicroShake(1.35f);

            // 버스트도 화면 가장자리 반응을 유도
            PlayScreenGlow(false);

            // 버스트도 화면 번쩍임에 기여 (살짝만)
            PlayCriticalFlash(burstColor, durationMultiplier: 0.9f, alphaMultiplier: 0.8f);
        }

        /// <summary>
        /// 스트릭이 쌓일수록 잔상 리플을 추가로 생성
        /// </summary>
        private void SpawnStreakEchoRipples(Vector2 position, bool isCritical, bool burstMode = false)
        {
            if (!_enableClickStreak || _streakLevel <= 0) return;
            if (_mainCanvas == null || !_enableClickRipple) return;

            int extraCount = Mathf.Clamp(_streakLevel * _streakExtraRipplesPerLevel, 0, 8);
            if (burstMode)
            {
                extraCount = Mathf.Max(extraCount, _streakLevel + 1);
            }

            if (extraCount <= 0) return;

            Color echoColor = burstMode
                ? _streakBurstColor
                : (isCritical ? _criticalRippleColor : _rippleColor);

            float baseScale = burstMode
                ? _streakBurstScaleMultiplier
                : 1f + (_streakLevel * 0.22f);

            float baseDuration = burstMode
                ? 1.2f
                : 1f + (_streakLevel * 0.08f);

            for (int i = 0; i < extraCount; i++)
            {
                float delay = (i + 1) * _streakRippleInterval;
                float t = extraCount == 1 ? 0f : (float)i / (extraCount - 1);
                float scaleMultiplier = baseScale * Mathf.Lerp(1.05f, 0.72f, t);
                float durationMultiplier = baseDuration * Mathf.Lerp(1f, 1.2f, t);

                // 캡처 변수 고정
                float delayLocal = delay;
                float scaleLocal = scaleMultiplier;
                float durationLocal = durationMultiplier;
                Color colorLocal = echoColor;

                DOVirtual.DelayedCall(delayLocal, () =>
                {
                    SpawnClickRipple(position, isCritical, colorLocal, scaleLocal, durationLocal);
                }, true);
            }
        }

        #endregion

        #region Particle Effects

        /// <summary>
        /// 파티클 프리팹 생성
        /// </summary>
        private void CreateParticlePrefab()
        {
            if (_mainCanvas == null || _particlePrefab != null) return;

            _particlePrefab = new GameObject("ParticlePrefab");
            _particlePrefab.SetActive(false);

            RectTransform rect = _particlePrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(6.406f, 6.406f);

            Image img = _particlePrefab.AddComponent<Image>();
            img.raycastTarget = false;
            // 단색 파티클로 사용 (스프라이트 불필요)
            img.color = _particleColor;

            _particlePrefab.transform.SetParent(_mainCanvas.transform, false);

            // 풀 초기화
            for (int i = 0; i < PARTICLE_POOL_INITIAL_SIZE; i++)
            {
                GameObject pooled = Instantiate(_particlePrefab, _mainCanvas.transform);
                pooled.name = "PooledParticle";
                pooled.SetActive(false);
                _particlePool.Enqueue(pooled);
            }
        }

        private GameObject GetParticleFromPool()
        {
            GameObject obj;
            if (_particlePool.Count > 0)
            {
                obj = _particlePool.Dequeue();
            }
            else if (_activeParticles.Count < PARTICLE_POOL_MAX_SIZE)
            {
                obj = Instantiate(_particlePrefab, _mainCanvas.transform);
                obj.name = "PooledParticle";
            }
            else
            {
                // 가장 오래된 파티클 재활용 (O(1) 큐 사용)
                obj = _activeParticlesQueue.Dequeue();
                _activeParticles.Remove(obj); // HashSet O(1)
                obj.transform.DOKill();
            }

            _activeParticles.Add(obj); // HashSet O(1)
            _activeParticlesQueue.Enqueue(obj); // 순서 유지
            return obj;
        }

        private void ReturnParticleToPool(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();
            // GetComponent 캐시 대신 TryGetComponent 사용 (더 가벼움)
            if (obj.TryGetComponent<Image>(out var img))
            {
                img.DOKill();
            }

            obj.SetActive(false);
            _activeParticles.Remove(obj); // HashSet O(1)

            if (_particlePool.Count < PARTICLE_POOL_MAX_SIZE)
            {
                _particlePool.Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// 클릭 시 파티클 분출
        /// </summary>
        private void SpawnClickParticles(Vector2 position, bool isCritical)
        {
            if (!_enableClickParticles || _mainCanvas == null) return;

            int particleCount = isCritical ? _criticalParticleCount : _normalParticleCount;
            Color baseColor = isCritical ? _criticalParticleColor : _particleColor;
            float speed = isCritical ? _particleSpeed * 1.4f : _particleSpeed;
            float lifetime = isCritical ? _particleLifetime * 1.2f : _particleLifetime;

            if (_enableClickStreak && _streakLevel > 0)
            {
                particleCount += _streakLevel * _streakParticleBonusPerLevel;
                speed *= GetStreakFactor(0.1f);
                lifetime *= GetStreakFactor(0.06f);

                if (!isCritical)
                {
                    float t = Mathf.Clamp01(_streakLevel * 0.22f);
                    baseColor = Color.Lerp(baseColor, _streakBurstColor, t);
                }
            }

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = GetParticleFromPool();
                particle.SetActive(true);
                particle.transform.SetAsLastSibling();

                RectTransform rect = particle.GetComponent<RectTransform>();
                rect.anchoredPosition = position;
                rect.localScale = Vector3.one * UnityEngine.Random.Range(0.6f, 1.2f);

                if (isCritical)
                {
                    rect.localScale *= 1.3f;
                }
                else if (_enableClickStreak && _streakLevel > 0)
                {
                    rect.localScale *= GetStreakFactor(0.06f);
                }

                Image img = particle.GetComponent<Image>();
                // 색상에 약간의 랜덤 변화
                float hueShift = UnityEngine.Random.Range(-0.1f, 0.1f);
                Color particleColor = new Color(
                    Mathf.Clamp01(baseColor.r + hueShift),
                    Mathf.Clamp01(baseColor.g + hueShift * 0.5f),
                    baseColor.b,
                    1f
                );
                img.color = particleColor;

                // 방사형 이동 방향 계산
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = speed * lifetime * UnityEngine.Random.Range(0.6f, 1.2f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 targetPos = position + direction * distance;

                // 약간의 곡선 효과를 위한 중력 시뮬레이션
                targetPos.y -= UnityEngine.Random.Range(30f, 80f);

                // 애니메이션
                float actualLifetime = lifetime * UnityEngine.Random.Range(0.8f, 1.2f);

                Sequence seq = DOTween.Sequence();
                seq.Append(rect.DOAnchorPos(targetPos, actualLifetime).SetEase(Ease.OutQuad));
                seq.Join(rect.DOScale(0f, actualLifetime).SetEase(Ease.InQuad));
                seq.Join(img.DOFade(0f, actualLifetime * 0.8f).SetDelay(actualLifetime * 0.2f));

                // 회전 추가 (크리티컬 시 더 빠르게)
                float rotationSpeed = isCritical ? 720f : 360f;
                seq.Join(rect.DORotate(new Vector3(0, 0, rotationSpeed * (UnityEngine.Random.value > 0.5f ? 1 : -1)), actualLifetime, RotateMode.FastBeyond360));

                seq.OnComplete(() => ReturnParticleToPool(particle));
            }
        }

        private void CleanupParticlePool()
        {
            foreach (var obj in _activeParticles)
            {
                if (obj == null) continue;
                obj.transform.DOKill();
                if (obj.TryGetComponent<Image>(out var img))
                {
                    img.DOKill();
                }
                Destroy(obj);
            }
            _activeParticles.Clear();
            _activeParticlesQueue.Clear();

            while (_particlePool.Count > 0)
            {
                var obj = _particlePool.Dequeue();
                if (obj != null) Destroy(obj);
            }

            if (_particlePrefab != null)
                Destroy(_particlePrefab);
        }

        #endregion

        #region Idle Pulse

        /// <summary>
        /// 클릭 영역 아이들 펄스 시작
        /// </summary>
        private void StartIdlePulse()
        {
            if (!_enableIdlePulse || _clickArea == null || _isIdlePulsing) return;

            _isIdlePulsing = true;

            // 스케일 펄스
            _idlePulseTween?.Kill();
            _idlePulseTween = _clickArea.transform
                .DOScale(1f + _idlePulseScale, _idlePulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            // 글로우 펄스 (있는 경우)
            if (_clickGlowImage != null)
            {
                _idleGlowPulseTween?.Kill();
                _clickGlowImage.color = new Color(_idlePulseGlowColor.r, _idlePulseGlowColor.g, _idlePulseGlowColor.b, 0f);
                _idleGlowPulseTween = _clickGlowImage
                    .DOFade(_idlePulseGlowColor.a, _idlePulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        /// <summary>
        /// 클릭 영역 아이들 펄스 일시 정지 (클릭 시)
        /// </summary>
        private void PauseIdlePulse()
        {
            if (!_isIdlePulsing) return;

            _idlePulseTween?.Pause();
            _idleGlowPulseTween?.Pause();

            // 0.5초 후 재개
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (_isIdlePulsing)
                {
                    bool pulseActive = _idlePulseTween != null && _idlePulseTween.IsActive();
                    bool glowActive = _clickGlowImage == null || (_idleGlowPulseTween != null && _idleGlowPulseTween.IsActive());

                    if (!pulseActive || !glowActive)
                    {
                        // 클릭 피드백에서 DOKill로 끊겼으면 다시 시작
                        _isIdlePulsing = false;
                        StartIdlePulse();
                        return;
                    }

                    _idlePulseTween.Play();
                    _idleGlowPulseTween?.Play();
                }
            }, true);
        }

        /// <summary>
        /// 클릭 영역 아이들 펄스 중지
        /// </summary>
        private void StopIdlePulse()
        {
            _isIdlePulsing = false;
            _idlePulseTween?.Kill();
            _idleGlowPulseTween?.Kill();

            if (_clickArea != null)
            {
                _clickArea.transform.DOKill();
                _clickArea.transform.localScale = Vector3.one;
            }
        }

        #endregion

        #region Hit Stop Effect

        /// <summary>
        /// 히트 스톱 효과 실행 (크리티컬/고스트릭 시)
        /// </summary>
        private void PlayHitStop(bool isCritical)
        {
            if (!_enableHitStop) return;

            float duration = _hitStopDuration;
            float timeScale = _hitStopTimeScale;

            if (isCritical)
            {
                duration *= 1.15f;
                timeScale = Mathf.Min(timeScale, 0.08f);
            }

            if (_enableClickStreak && _streakLevel > 0)
            {
                duration *= GetStreakFactor(0.08f);
                float streakT = Mathf.Clamp01(_streakLevel * 0.18f);
                timeScale = Mathf.Lerp(timeScale, timeScale * 0.7f, streakT);
            }

            timeScale = Mathf.Clamp(timeScale, 0.02f, 1f);

            if (_hitStopCoroutine != null)
            {
                StopCoroutine(_hitStopCoroutine);
                Time.timeScale = _originalTimeScale;
            }

            _hitStopCoroutine = StartCoroutine(HitStopCoroutine(duration, timeScale));
        }

        private System.Collections.IEnumerator HitStopCoroutine(float duration, float timeScale)
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            yield return new WaitForSecondsRealtime(duration);

            // 부드러운 복귀
            float elapsed = 0f;
            float recoveryDuration = 0.05f;
            while (elapsed < recoveryDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(timeScale, _originalTimeScale, elapsed / recoveryDuration);
                yield return null;
            }

            Time.timeScale = _originalTimeScale;
            _hitStopCoroutine = null;
        }

        #endregion

        #region Screen Edge Glow

        /// <summary>
        /// 화면 테두리 글로우 생성
        /// </summary>
        private void CreateScreenGlowEdges()
        {
            if (_mainCanvas == null || _screenGlowEdges != null) return;

            _screenGlowEdges = new Image[4]; // 상, 하, 좌, 우
            _screenGlowTweens = new Tween[4];
            _createdScreenGlow = true;

            RectTransform canvasRect = _mainCanvas.GetComponent<RectTransform>();

            // 상단 테두리
            _screenGlowEdges[0] = CreateGlowEdge(canvasRect, "ScreenGlow_Top",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -_screenGlowThickness / 2), new Vector2(0, _screenGlowThickness));

            // 하단 테두리
            _screenGlowEdges[1] = CreateGlowEdge(canvasRect, "ScreenGlow_Bottom",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, _screenGlowThickness / 2), new Vector2(0, _screenGlowThickness));

            // 좌측 테두리
            _screenGlowEdges[2] = CreateGlowEdge(canvasRect, "ScreenGlow_Left",
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(_screenGlowThickness / 2, 0), new Vector2(_screenGlowThickness, 0));

            // 우측 테두리
            _screenGlowEdges[3] = CreateGlowEdge(canvasRect, "ScreenGlow_Right",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-_screenGlowThickness / 2, 0), new Vector2(_screenGlowThickness, 0));

            // 초기 상태: 투명
            foreach (var edge in _screenGlowEdges)
            {
                if (edge != null)
                {
                    Color c = edge.color;
                    edge.color = new Color(c.r, c.g, c.b, 0f);
                }
            }
        }

        private Image CreateGlowEdge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = _criticalScreenGlowColor;

            // 단색 오버레이로 사용 (스프라이트 불필요)
            img.sprite = null;

            return img;
        }

        /// <summary>
        /// 화면 테두리 글로우 효과 실행
        /// </summary>
        private void PlayScreenGlow(bool isJackpot = false)
        {
            if (!_enableScreenGlow || _screenGlowEdges == null) return;

            Color glowColor = isJackpot ? _jackpotScreenGlowColor : _criticalScreenGlowColor;
            float duration = isJackpot ? _screenGlowDuration * 1.5f : _screenGlowDuration;
            int loops = isJackpot ? 4 : 2;

            for (int i = 0; i < _screenGlowEdges.Length; i++)
            {
                if (_screenGlowEdges[i] == null) continue;

                _screenGlowTweens[i]?.Kill();
                _screenGlowEdges[i].transform.SetAsLastSibling();
                _screenGlowEdges[i].color = glowColor;

                int index = i; // 클로저용
                _screenGlowTweens[i] = _screenGlowEdges[i]
                    .DOFade(0f, duration / loops)
                    .SetLoops(loops, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        if (_screenGlowEdges[index] != null)
                        {
                            Color c = _screenGlowEdges[index].color;
                            _screenGlowEdges[index].color = new Color(c.r, c.g, c.b, 0f);
                        }
                    });
            }
        }

        private void CleanupScreenGlow()
        {
            if (_screenGlowTweens != null)
            {
                for (int i = 0; i < _screenGlowTweens.Length; i++)
                {
                    _screenGlowTweens[i]?.Kill();
                }
            }

            if (_screenGlowEdges != null && _createdScreenGlow)
            {
                foreach (var edge in _screenGlowEdges)
                {
                    if (edge != null)
                        Destroy(edge.gameObject);
                }
            }
            _screenGlowEdges = null;
        }

        #endregion

        /// <summary>
        /// 클릭 영역의 이미지/글로우 레이어를 확보
        /// </summary>
        private void EnsureClickVisuals()
        {
            if (_clickArea == null) return;

            _clickAreaRect = _clickArea.GetComponent<RectTransform>();
            _clickAreaImage = _clickArea.GetComponent<Image>();

            if (_clickAreaImage != null)
            {
                _clickAreaBaseColor = _clickAreaImage.color;
            }

            if (_clickGlowImage == null)
            {
                Transform existingGlow = _clickArea.transform.Find("ClickGlow");
                if (existingGlow != null)
                {
                    _clickGlowImage = existingGlow.GetComponent<Image>();
                }
            }

            if (_clickGlowImage != null) return;

            GameObject glowObj = new GameObject("ClickGlow");
            glowObj.transform.SetParent(_clickArea.transform, false);

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            glowRect.localScale = Vector3.one;

            _clickGlowImage = glowObj.AddComponent<Image>();
            _clickGlowImage.raycastTarget = false;
            _createdClickGlow = true;

            // 클릭 영역의 스프라이트가 있으면 사용, 없으면 단색
            _clickGlowImage.sprite = _clickAreaImage != null ? _clickAreaImage.sprite : null;
            _clickGlowImage.type = _clickAreaImage != null ? _clickAreaImage.type : Image.Type.Sliced;
            _clickGlowImage.color = new Color(_rippleColor.r, _rippleColor.g, _rippleColor.b, 0f);
        }

        /// <summary>
        /// 클릭 리플 프리팹 생성 및 풀 초기화
        /// </summary>
        private void CreateRipplePrefab()
        {
            if (_mainCanvas == null || !_enableClickRipple) return;
            if (_ripplePrefab != null) return;

            _ripplePrefab = new GameObject("ClickRipplePrefab");
            _ripplePrefab.SetActive(false);

            RectTransform rect = _ripplePrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(56.049f, 56.049f);

            Image img = _ripplePrefab.AddComponent<Image>();
            img.raycastTarget = false;
            // 단색 리플로 사용 (스프라이트 불필요)
            img.color = _rippleColor;

            _ripplePrefab.transform.SetParent(_mainCanvas.transform, false);

            InitializeRipplePool();
        }

        private void InitializeRipplePool()
        {
            for (int i = 0; i < RIPPLE_POOL_INITIAL_SIZE; i++)
            {
                GameObject pooled = CreatePooledRipple();
                _ripplePool.Enqueue(pooled);
            }
        }

        private GameObject CreatePooledRipple()
        {
            GameObject obj = Instantiate(_ripplePrefab, _mainCanvas.transform);
            obj.name = "PooledClickRipple";
            obj.SetActive(false);
            return obj;
        }

        private GameObject GetRippleFromPool()
        {
            GameObject obj;
            if (_ripplePool.Count > 0)
            {
                obj = _ripplePool.Dequeue();
            }
            else if (_activeRipples.Count < RIPPLE_POOL_MAX_SIZE)
            {
                obj = CreatePooledRipple();
            }
            else
            {
                // 가장 오래된 리플 재활용 (O(1) 큐 사용)
                obj = _activeRipplesQueue.Dequeue();
                _activeRipples.Remove(obj); // HashSet O(1)
                obj.transform.DOKill();
                if (obj.TryGetComponent<Image>(out var activeImg))
                {
                    activeImg.DOKill();
                }
            }

            _activeRipples.Add(obj); // HashSet O(1)
            _activeRipplesQueue.Enqueue(obj); // 순서 유지
            return obj;
        }

        private void ReturnRippleToPool(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();
            if (obj.TryGetComponent<Image>(out var img))
            {
                img.DOKill();
            }

            obj.SetActive(false);
            _activeRipples.Remove(obj); // HashSet O(1)

            if (_ripplePool.Count < RIPPLE_POOL_MAX_SIZE)
            {
                _ripplePool.Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        private void CleanupRipplePool()
        {
            foreach (var obj in _activeRipples)
            {
                if (obj == null) continue;
                obj.transform.DOKill();
                if (obj.TryGetComponent<Image>(out var img))
                {
                    img.DOKill();
                }
                Destroy(obj);
            }
            _activeRipples.Clear();
            _activeRipplesQueue.Clear();

            while (_ripplePool.Count > 0)
            {
                var obj = _ripplePool.Dequeue();
                if (obj == null) continue;
                obj.transform.DOKill();
                if (obj.TryGetComponent<Image>(out var img2))
                {
                    img2.DOKill();
                }
                Destroy(obj);
            }

            if (_ripplePrefab != null)
            {
                Destroy(_ripplePrefab);
            }
        }

        /// <summary>
        /// 클릭 사운드용 AudioSource 확보
        /// </summary>
        private void EnsureClickAudioSource()
        {
            if (_clickAudioSource != null) return;

            if (_mainCanvas != null)
            {
                Transform existingAudio = _mainCanvas.transform.Find("ClickSFXSource");
                if (existingAudio != null)
                {
                    _clickAudioSource = existingAudio.GetComponent<AudioSource>();
                }
            }

            if (_clickAudioSource != null) return;

            GameObject audioObj = new GameObject("ClickSFXSource");
            if (_mainCanvas != null)
            {
                audioObj.transform.SetParent(_mainCanvas.transform, false);
            }
            else
            {
                audioObj.transform.SetParent(transform, false);
            }

            _clickAudioSource = audioObj.AddComponent<AudioSource>();
            _clickAudioSource.playOnAwake = false;
            _clickAudioSource.loop = false;
            _clickAudioSource.spatialBlend = 0f;
            _createdClickAudioSource = true;
        }

        /// <summary>
        /// 클릭 사운드 재생 (크리티컬은 더 강하게, 콤보 피치 스케일링 적용)
        /// </summary>
        private void PlayClickSound(bool isCritical)
        {
            if (_clickSfx == null && _criticalClickSfx == null) return;
            EnsureClickAudioSource();
            if (_clickAudioSource == null) return;

            AudioClip clipToPlay = isCritical && _criticalClickSfx != null ? _criticalClickSfx : _clickSfx;
            if (clipToPlay == null) return;

            // 피치 계산
            float pitch = isCritical
                ? _criticalPitch
                : 1f + UnityEngine.Random.Range(-_clickPitchJitter, _clickPitchJitter);

            float volume = isCritical
                ? Mathf.Clamp01(_clickSfxVolume * _criticalSfxVolumeMultiplier)
                : _clickSfxVolume;

            // 스트릭이 쌓일수록 피치/볼륨을 살짝 끌어올려 타격감 강화
            if (_enableClickStreak && _streakLevel > 0)
            {
                pitch += _streakLevel * _streakPitchBonusPerLevel;
                volume = Mathf.Clamp01(volume + (_streakLevel * _streakVolumeBonusPerLevel));
            }

            pitch = Mathf.Clamp(pitch, 0.5f, 2.2f);
            _clickAudioSource.pitch = pitch;
            _clickAudioSource.PlayOneShot(clipToPlay, volume);
        }

        /// <summary>
        /// 크리티컬 플래시 레이어 확보
        /// </summary>
        private void EnsureCriticalFlashLayer()
        {
            if (!_enableCriticalFlash || _mainCanvas == null) return;
            if (_criticalFlashImage == null)
            {
                Transform existingFlash = _mainCanvas.transform.Find("CriticalFlash");
                if (existingFlash != null)
                {
                    _criticalFlashImage = existingFlash.GetComponent<Image>();
                }
            }
            if (_criticalFlashImage != null) return;

            GameObject flashObj = new GameObject("CriticalFlash");
            flashObj.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = flashObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            _criticalFlashImage = flashObj.AddComponent<Image>();
            _criticalFlashImage.raycastTarget = false;
            // 단색 플래시로 사용 (스프라이트 불필요)
            _createdCriticalFlash = true;
            _criticalFlashImage.color = new Color(
                _criticalFlashColor.r,
                _criticalFlashColor.g,
                _criticalFlashColor.b,
                0f);
        }

        /// <summary>
        /// 크리티컬 전용 화면 번쩍임
        /// </summary>
        private void PlayCriticalFlash(Color? colorOverride = null, float durationMultiplier = 1f, float alphaMultiplier = 1f)
        {
            if (!_enableCriticalFlash || _mainCanvas == null) return;
            EnsureCriticalFlashLayer();
            if (_criticalFlashImage == null) return;

            _criticalFlashImage.transform.SetAsLastSibling();
            _criticalFlashImage.DOKill();
            _criticalFlashTween?.Kill();

            Color flashColor = colorOverride ?? _criticalFlashColor;
            flashColor.a *= Mathf.Clamp01(alphaMultiplier);
            _criticalFlashImage.color = flashColor;

            float duration = Mathf.Max(0.02f, _criticalFlashDuration * Mathf.Max(0.1f, durationMultiplier));
            _criticalFlashTween = _criticalFlashImage
                .DOFade(0f, duration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 셰이크 타겟(카메라 우선) 해결
        /// </summary>
        private void ResolveShakeTarget()
        {
            if (_shakeTarget != null) return;

            if (Camera.main != null)
            {
                _shakeTarget = Camera.main.transform;
                return;
            }

            if (_mainCanvas != null)
            {
                _shakeTarget = _mainCanvas.transform;
                return;
            }

            _shakeTarget = transform;
        }

        /// <summary>
        /// 크리티컬 전용 카메라/화면 셰이크
        /// </summary>
        private void PlayCriticalShake()
        {
            if (!_enableCriticalShake) return;

            ResolveShakeTarget();
            if (_shakeTarget == null) return;

            // 미세 쉐이크는 크리티컬 연출에 양보
            KillMicroShakeAndRestore();
            _shakeTween?.Kill();
            _shakeTarget.DOKill();

            _shakeOriginalPosition = _shakeTarget.position;

            float strength = _criticalShakeStrength;
            float duration = _criticalShakeDuration;

            // 스트릭이 높을수록 크리티컬의 임팩트를 조금 더 키움
            float streakFactor = GetStreakFactor(0.12f);
            strength *= streakFactor;
            duration *= GetStreakFactor(0.06f);

            if (_shakeTarget.GetComponent<Camera>() != null)
            {
                // 카메라는 월드 유닛이므로 픽셀 기반 강도를 축소
                strength *= 0.02f;
            }

            _shakeTween = _shakeTarget
                .DOShakePosition(
                    duration,
                    strength,
                    _criticalShakeVibrato,
                    _criticalShakeRandomness,
                    false,
                    true)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (_shakeTarget != null)
                    {
                        _shakeTarget.position = _shakeOriginalPosition;
                    }
                });
        }

        #region Micro Shake

        /// <summary>
        /// 진행 중인 미세 쉐이크를 안전하게 중지하고 원위치 복구
        /// </summary>
        private void KillMicroShakeAndRestore()
        {
            if (_microShakeTween != null && _microShakeTween.IsActive())
            {
                _microShakeTween.Kill();
            }

            if (_microShakeHadOriginalPosition && _microShakeTarget != null)
            {
                _microShakeTarget.position = _microShakeOriginalPosition;
            }

            _microShakeHadOriginalPosition = false;
            _microShakeTarget = null;
        }

        /// <summary>
        /// 일반 클릭에도 아주 짧은 미세 쉐이크를 넣어 손맛 강화
        /// </summary>
        private void PlayMicroShake(float strengthMultiplier = 1f)
        {
            if (!_enableMicroShake) return;

            float now = Time.unscaledTime;
            if (now - _lastMicroShakeRealtime < _microShakeCooldown) return;

            // 크리티컬 쉐이크가 재생 중이면 건드리지 않는다
            if (_shakeTween != null && _shakeTween.IsActive() && _shakeTween.IsPlaying())
            {
                return;
            }

            _lastMicroShakeRealtime = now;

            ResolveShakeTarget();
            if (_shakeTarget == null) return;

            KillMicroShakeAndRestore();

            _microShakeTarget = _shakeTarget;
            _microShakeOriginalPosition = _microShakeTarget.position;
            _microShakeHadOriginalPosition = true;

            float strength = _microShakeStrength * Mathf.Max(0.2f, strengthMultiplier);
            if (_microShakeTarget.GetComponent<Camera>() != null)
            {
                strength *= 0.02f;
            }

            _microShakeTween = _microShakeTarget
                .DOShakePosition(
                    _microShakeDuration,
                    strength,
                    _microShakeVibrato,
                    _criticalShakeRandomness,
                    false,
                    true)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (_microShakeHadOriginalPosition && _microShakeTarget != null)
                    {
                        _microShakeTarget.position = _microShakeOriginalPosition;
                    }
                    _microShakeHadOriginalPosition = false;
                    _microShakeTarget = null;
                });
        }

        #endregion

        /// <summary>
        /// 클릭 위치에 리플 이펙트 생성
        /// </summary>
        private void SpawnClickRipple(
            Vector2 position,
            bool isCritical,
            Color? colorOverride = null,
            float scaleMultiplier = 1f,
            float durationMultiplier = 1f)
        {
            if (!_enableClickRipple || _mainCanvas == null) return;

            GameObject ripple = GetRippleFromPool();
            ripple.SetActive(true);
            ripple.transform.SetAsLastSibling();

            RectTransform rect = ripple.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            float baseStartScale = isCritical ? 0.8f : 0.65f;
            float streakScale = GetStreakFactor(0.12f);
            rect.localScale = Vector3.one * baseStartScale * streakScale;
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-12f, 12f));

            Image img = ripple.GetComponent<Image>();
            Color rippleColor = colorOverride ?? (isCritical ? _criticalRippleColor : _rippleColor);
            float startAlpha = isCritical ? 0.95f : 0.8f;
            img.color = new Color(rippleColor.r, rippleColor.g, rippleColor.b, startAlpha);

            float duration = (isCritical ? _rippleDuration * 1.15f : _rippleDuration)
                * GetStreakFactor(0.06f)
                * Mathf.Max(0.1f, durationMultiplier);

            float maxScale = (isCritical ? _rippleMaxScale * 1.25f : _rippleMaxScale)
                * streakScale
                * Mathf.Max(0.1f, scaleMultiplier);

            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOScale(maxScale, duration).SetEase(Ease.OutCubic));
            seq.Join(img.DOFade(0f, duration).SetEase(Ease.OutQuad));
            seq.OnComplete(() => ReturnRippleToPool(ripple));
        }

        /// <summary>
        /// 클릭 영역 자체의 펀치/글로우 피드백
        /// </summary>
        private void PlayClickAreaFeedback(bool isCritical)
        {
            if (_clickArea == null) return;
            EnsureClickVisuals();

            float strength = isCritical ? _criticalPulseStrength : _clickPulseStrength;
            float duration = isCritical ? _clickPulseDuration * 1.2f : _clickPulseDuration;
            float streakStrengthFactor = GetStreakFactor(_streakPulseBonusPerLevel);
            strength *= streakStrengthFactor;
            duration *= GetStreakFactor(0.05f);

            _clickArea.transform.DOKill();
            _clickArea.transform.localScale = Vector3.one;
            _clickArea.transform.DOPunchScale(Vector3.one * strength, duration, 10, 0.9f)
                .SetEase(Ease.OutQuad);

            if (_clickAreaImage != null)
            {
                _clickAreaImage.DOKill();
                Color flashColor = isCritical ? _criticalColor : _normalClickColor;
                if (!isCritical && _enableClickStreak && _streakLevel > 0)
                {
                    float t = Mathf.Clamp01(_streakLevel * 0.25f);
                    flashColor = Color.Lerp(_normalClickColor, _streakBurstColor, t);
                }
                flashColor.a = _clickAreaBaseColor.a;
                _clickAreaImage.color = flashColor;
                _clickAreaImage.DOColor(_clickAreaBaseColor, duration * 1.35f);
            }

            if (_clickGlowImage == null) return;

            _clickGlowImage.DOKill();
            _clickGlowTween?.Kill();

            RectTransform glowRect = _clickGlowImage.rectTransform;
            float glowStartScale = isCritical ? 0.98f : 0.96f;
            glowStartScale *= GetStreakFactor(0.04f);
            glowRect.localScale = Vector3.one * glowStartScale;

            Color glowColor = isCritical ? _criticalRippleColor : _rippleColor;
            float glowAlpha = isCritical ? 0.72f : 0.45f;
            if (_enableClickStreak && _streakLevel > 0)
            {
                glowAlpha = Mathf.Min(glowAlpha + (_streakLevel * _streakGlowBonusPerLevel), 0.95f);
                if (!isCritical)
                {
                    glowColor = Color.Lerp(glowColor, _streakBurstColor, Mathf.Clamp01(_streakLevel * 0.22f));
                }
            }
            _clickGlowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha);

            Sequence glowSeq = DOTween.Sequence();
            float glowTargetScale = isCritical ? 1.1f : 1.06f;
            glowTargetScale *= GetStreakFactor(0.05f);
            glowSeq.Append(glowRect.DOScale(glowTargetScale, duration * 1.25f).SetEase(Ease.OutQuad));
            glowSeq.Join(_clickGlowImage.DOFade(0f, duration * 1.4f).SetEase(Ease.OutQuad));
            _clickGlowTween = glowSeq;
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
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(160.139f, 24.021f);

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
                Vector2.zero, Vector2.one, Vector2.zero, 12.81f);
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

            // 코인 스프라이트 로드 (플로팅 텍스트용)
            if (_coinSprite == null)
            {
                Sprite[] coinSprites = Resources.LoadAll<Sprite>("UI/코인");

                if (coinSprites != null && coinSprites.Length > 0)
                {
                    // 첫 번째 스프라이트(코인_0)만 사용
                    _coinSprite = coinSprites[0];
                    Debug.Log($"[SlotClickerUI] Coin sprite auto-loaded: {_coinSprite.name}");
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

            if (_coinSprite != null)
                Debug.Log($"[SlotClickerUI] ✓ Coin sprite ready: {_coinSprite.name}");
            else
                Debug.LogWarning("[SlotClickerUI] ✗ No coin sprite - floating text will show without coin icon");
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
                new Vector2(-36.031f, -66.056f), new Vector2(67.259f, 21.618f),
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
                new Vector2(36.031f, -66.056f), new Vector2(67.259f, 21.618f),
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

        #region Help System

        private Button _helpButton;
        private GameObject _helpPanel;
        private bool _isHelpVisible = false;

        private void CreateHelpButton(RectTransform parent)
        {
            // 도움말 버튼 (하단 베팅 영역 우측에 배치 - 항상 보이도록)
            GameObject btnObj = CreateButton(parent, "HelpButton", "?",
                new Vector2(1, 0), new Vector2(1, 0),  // 하단 우측 앵커
                new Vector2(-16.014f, 72.062f), new Vector2(31.228f, 31.228f),  // 하단에서 위로
                new Color(0.3f, 0.7f, 0.9f));

            _helpButton = btnObj.GetComponent<Button>();
            _helpButton.onClick.AddListener(ToggleHelpPanel);

            // 버튼 텍스트 크게
            var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontSize = 19.217f;
                btnText.fontStyle = FontStyles.Bold;
            }

            // 눈에 띄게 펄스 애니메이션
            btnObj.transform.DOScale(1.1f, 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void CreateHelpUI()
        {
            // 도움말 패널 (화면 중앙, 큰 팝업)
            _helpPanel = new GameObject("HelpPanel");
            _helpPanel.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = _helpPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.08f);
            rect.anchorMax = new Vector2(0.95f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 배경
            Image bg = _helpPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.12f, 0.98f);

            // 스크롤 가능한 내용을 위한 컨테이너
            CreateHelpContent(rect);

            _helpPanel.SetActive(false);
        }

        private void CreateHelpContent(RectTransform parent)
        {
            // 제목
            GameObject titleObj = CreateTextObject(parent, "HelpTitle", "[SLOT] 슬롯 게임 가이드",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12.01f), 16.815f);
            var titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.color = new Color(1f, 0.85f, 0.3f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;

            // 닫기 버튼
            GameObject closeBtn = CreateButton(parent, "CloseHelp", "X",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12.01f, -12.01f), new Vector2(24.02f, 24.02f),
                new Color(0.7f, 0.3f, 0.3f));
            closeBtn.GetComponent<Button>().onClick.AddListener(ToggleHelpPanel);
            var closeText = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (closeText != null) closeText.fontSize = 13.452f;

            // === 페이라인 설명 섹션 ===
            float yPos = -36.031f;

            CreateHelpSection(parent, ">> 페이라인 (당첨 라인)", ref yPos);
            CreateHelpText(parent, "3x3 슬롯에서 같은 심볼 3개가 라인에 맞으면 당첨!", ref yPos);
            CreateHelpText(parent, "", ref yPos);  // 공백

            // 페이라인 그림 설명
            CreatePaylineVisual(parent, ref yPos);

            yPos -= 8.007f;

            // === 배당률 설명 섹션 ===
            CreateHelpSection(parent, "$ 배당률 (베팅액 기준)", ref yPos);

            string[] payoutInfo = {
                "• 미니윈 (2줄 일치): 2.0배",
                "• 스몰윈 (3줄 일치): 2.5배",
                "• 빅윈 (희귀 심볼): 5.0배",
                "• 잭팟 (특수 조합): 10배",
                "• 메가잭팟 (최고): 100배!"
            };

            foreach (string info in payoutInfo)
            {
                CreateHelpText(parent, info, ref yPos, 11.21f, new Color(0.8f, 1f, 0.8f));
            }

            yPos -= 6.005f;

            // === 연승 콤보 설명 ===
            CreateHelpSection(parent, "* 연승 콤보 보너스", ref yPos);
            CreateHelpText(parent, "연속 당첨 시 보너스 배율 증가!", ref yPos);
            CreateHelpText(parent, "• 2연승: +10% / 3연승: +20%", ref yPos, 10.409f, new Color(1f, 0.9f, 0.6f));
            CreateHelpText(parent, "• 5연승: +50% / 10연승: +100%!", ref yPos, 10.409f, new Color(1f, 0.9f, 0.6f));

            yPos -= 6.005f;

            // === 게임 팁 ===
            CreateHelpSection(parent, "! 게임 팁", ref yPos);
            CreateHelpText(parent, "• 자동수집을 먼저 구매하세요!", ref yPos, 10.409f, new Color(0.7f, 0.9f, 1f));
            CreateHelpText(parent, "• 50K 골드에서 첫 프레스티지 가능", ref yPos, 10.409f, new Color(0.7f, 0.9f, 1f));
            CreateHelpText(parent, "• AUTO 버튼으로 자동 스핀!", ref yPos, 10.409f, new Color(0.7f, 0.9f, 1f));
        }

        private void CreateHelpSection(RectTransform parent, string title, ref float yPos)
        {
            yPos -= 6.005f;
            GameObject sectionObj = CreateTextObject(parent, "Section", title,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(8.007f, yPos), 12.81f);
            var sectionText = sectionObj.GetComponent<TextMeshProUGUI>();
            sectionText.color = new Color(1f, 0.7f, 0.4f);
            sectionText.fontStyle = FontStyles.Bold;
            sectionText.alignment = TextAlignmentOptions.Left;

            var sectionRect = sectionObj.GetComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(-16.014f, 16.014f);
            sectionRect.anchoredPosition = new Vector2(0, yPos);

            yPos -= 18.015f;
        }

        private void CreateHelpText(RectTransform parent, string text, ref float yPos, float fontSize = 10.409f, Color? color = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                yPos -= 4.003f;
                return;
            }

            GameObject textObj = CreateTextObject(parent, "HelpText", text,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(12.01f, yPos), fontSize);
            var helpText = textObj.GetComponent<TextMeshProUGUI>();
            helpText.color = color ?? new Color(0.9f, 0.9f, 0.9f);
            helpText.alignment = TextAlignmentOptions.Left;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(-24.021f, 14.012f);
            textRect.anchoredPosition = new Vector2(0, yPos);

            yPos -= 14.012f;
        }

        private void CreatePaylineVisual(RectTransform parent, ref float yPos)
        {
            // 페이라인 시각적 설명 (간단한 그리드)
            float startY = yPos;
            float gridSize = 16.014f;
            float spacing = 3.203f;
            float startX = 12.01f;

            // 3x3 그리드 + 라인 설명
            string[,] gridLabels = {
                { "0", "1", "2" },
                { "3", "4", "5" },
                { "6", "7", "8" }
            };

            // 그리드 배경
            GameObject gridBg = CreatePanel(parent, "GridBg",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(startX + 28.024f, yPos - 28.024f), new Vector2(64.056f, 64.056f),
                new Color(0.2f, 0.2f, 0.25f, 0.8f));

            // 페이라인 설명 텍스트
            string[] lineDescriptions = {
                "─ 가로 3줄",
                "╲ ╱ 대각선 2줄",
                "= 총 5개 라인"
            };

            float descX = startX + 80.069f;
            float descY = yPos - 12.01f;

            foreach (string desc in lineDescriptions)
            {
                GameObject descObj = CreateTextObject(parent, "LineDesc", desc,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(descX, descY), 9.608f);
                var descText = descObj.GetComponent<TextMeshProUGUI>();
                descText.color = new Color(0.8f, 0.8f, 0.9f);
                descY -= 14.012f;
            }

            // 예시 설명
            GameObject exampleObj = CreateTextObject(parent, "Example", "예: [7][7][7] = 당첨!",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(descX, descY - 4.003f), 10.409f);
            var exampleText = exampleObj.GetComponent<TextMeshProUGUI>();
            exampleText.color = new Color(0.5f, 1f, 0.5f);

            yPos -= 72.062f;
        }

        private void ToggleHelpPanel()
        {
            _isHelpVisible = !_isHelpVisible;
            if (_helpPanel != null)
            {
                _helpPanel.SetActive(_isHelpVisible);

                // 열릴 때 애니메이션
                if (_isHelpVisible)
                {
                    _helpPanel.transform.localScale = Vector3.one * 0.8f;
                    _helpPanel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                }
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

            // 클릭 피드백 정리
            _clickGlowTween?.Kill();
            if (_clickAreaImage != null) _clickAreaImage.DOKill();
            if (_clickGlowImage != null)
            {
                _clickGlowImage.DOKill();
                _clickGlowImage.rectTransform.DOKill();
            }
            _criticalFlashTween?.Kill();
            _shakeTween?.Kill();
            KillMicroShakeAndRestore();
            if (_criticalFlashImage != null)
            {
                _criticalFlashImage.DOKill();
                if (_createdCriticalFlash)
                {
                    Destroy(_criticalFlashImage.gameObject);
                }
                else
                {
                    Color c = _criticalFlashImage.color;
                    _criticalFlashImage.color = new Color(c.r, c.g, c.b, 0f);
                }
                _criticalFlashImage = null;
            }
            if (_clickAudioSource != null)
            {
                if (_createdClickAudioSource)
                {
                    Destroy(_clickAudioSource.gameObject);
                }
                else
                {
                    _clickAudioSource.Stop();
                }
                _clickAudioSource = null;
            }

            // 플로팅 텍스트 풀 정리
            CleanupFloatingTextPool();
            CleanupRipplePool();

            // 향상된 피드백 시스템 정리
            CleanupParticlePool();
            CleanupScreenGlow();

            // 슬롯 승리 피드백 정리
            if (_slotWinFeedback != null)
            {
                _slotWinFeedback.StopAllFeedback();
            }

            // 아이들 펄스 정리
            StopIdlePulse();

            // 히트 스톱 정리
            if (_hitStopCoroutine != null)
            {
                StopCoroutine(_hitStopCoroutine);
                Time.timeScale = _originalTimeScale;
            }
            else if (!Mathf.Approximately(Time.timeScale, _originalTimeScale))
            {
                Time.timeScale = _originalTimeScale;
            }

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

            // 골드 표시 (일일 보너스 활성 시 배율 표시)
            string goldText = $"GOLD: {_game.Gold.GetFormattedGold()}";
            if (_game.DailyLogin != null && _game.DailyLogin.IsRewardActive)
            {
                float mult = _game.DailyLogin.CurrentMultiplier;
                goldText += $" <color=yellow>({mult:F1}x)</color>";
            }
            _goldText.text = goldText;

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

            // ★ 모바일 최적화: 클릭 디바운싱 (고주사율 디바이스에서 중복 클릭 방지)
            if (Time.realtimeSinceStartup - _lastClickTime < CLICK_MIN_INTERVAL)
                return;
            _lastClickTime = Time.realtimeSinceStartup;

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
        }

        private void OnClickResult(ClickResult result)
        {
            // WebGL 이펙트 쓰로틀링 - 프레임당 이펙트 수 제한
            float currentTime = Time.realtimeSinceStartup;
            bool shouldThrottle = _isWebGL && (currentTime - _lastEffectTime) < EFFECT_THROTTLE_INTERVAL;

            // 아이들 펄스 일시 정지
            PauseIdlePulse();

            // 스트릭 갱신 (사운드/이펙트 강도에 영향)
            UpdateClickStreak(result.Position, result.IsCritical);

            // 필수 피드백 (항상 실행)
            PlayClickSound(result.IsCritical);
            SpawnFloatingText(result.Position, result.GoldEarned, result.IsCritical);

            // 선택적 이펙트 (쓰로틀링 적용)
            if (!shouldThrottle)
            {
                _lastEffectTime = currentTime;

                SpawnClickRipple(result.Position, result.IsCritical);
                if (!_streakBurstTriggeredThisClick)
                {
                    SpawnStreakEchoRipples(result.Position, result.IsCritical, burstMode: false);
                }
                PlayClickAreaFeedback(result.IsCritical);

                // 파티클 이펙트 (WebGL에서 더 제한적으로)
                if (!_isWebGL || _frameEffectCount < MAX_EFFECTS_PER_FRAME)
                {
                    SpawnClickParticles(result.Position, result.IsCritical);
                    _frameEffectCount++;
                }
            }

            // 일반 클릭에도 아주 약한 화면 쉐이크 추가 (WebGL에서는 스킵)
            if (!result.IsCritical && !_isWebGL)
            {
                PlayMicroShake();
            }

            // 크리티컬 또는 고스트릭 구간에서는 히트스톱 허용
            bool allowHitStop = result.IsCritical ||
                (_hitStopOnStreakBurst && _enableClickStreak && _streakBurstTriggeredThisClick && _streakLevel >= _streakHitStopMinLevel);

            // WebGL에서는 히트스톱 비활성화 (입력 지연 방지)
            if (allowHitStop && !_isWebGL)
            {
                PlayHitStop(result.IsCritical);
            }

            if (result.IsCritical)
            {
                PlayCriticalFlash();
                PlayCriticalShake();
                PlayScreenGlow(false); // 화면 테두리 글로우
                UIFeedback.TriggerHaptic(UIFeedback.HapticType.Medium);
            }
        }

        private void SpawnFloatingText(Vector2 position, double amount, bool isCritical)
        {
            // 오브젝트 풀에서 가져오기 (Instantiate 대신)
            GameObject floatText = GetFloatingTextFromPool();
            floatText.SetActive(true);
            floatText.transform.SetAsLastSibling();

            RectTransform rect = floatText.GetComponent<RectTransform>();
            rect.DOKill();
            floatText.transform.DOKill();

            Vector2 startPos = position + new Vector2(
                UnityEngine.Random.Range(-5.605f, 5.605f),
                UnityEngine.Random.Range(-2.402f, 4.804f));

            rect.anchoredPosition = startPos;
            rect.localScale = Vector3.one * (isCritical ? 0.95f : 0.85f);
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-6f, 6f));

            // 자식에서 텍스트 컴포넌트 찾기
            Transform textChild = floatText.transform.Find("Text");
            TextMeshProUGUI tmp = textChild != null
                ? textChild.GetComponent<TextMeshProUGUI>()
                : floatText.GetComponent<TextMeshProUGUI>();

            tmp.DOKill();
            tmp.fontSize = isCritical ? 20.818f : 16.014f;
            tmp.alpha = 1f; // 알파 초기화 (풀에서 재사용 시 필요)

            // 코인 아이콘 알파 초기화 및 크기 조정
            Transform coinChild = floatText.transform.Find("CoinIcon");
            Image coinImage = coinChild != null ? coinChild.GetComponent<Image>() : null;
            if (coinImage != null)
            {
                coinImage.DOKill();
                Color coinColor = coinImage.color;
                coinColor.a = 1f;
                coinImage.color = coinColor;

                // 크리티컬일 때 코인도 크게
                RectTransform coinRect = coinChild.GetComponent<RectTransform>();
                if (coinRect != null)
                {
                    float coinSize = isCritical ? 20.818f : 16.014f;
                    coinRect.sizeDelta = new Vector2(coinSize, coinSize);
                    LayoutElement coinLayout = coinChild.GetComponent<LayoutElement>();
                    if (coinLayout != null)
                    {
                        coinLayout.preferredWidth = coinSize;
                        coinLayout.preferredHeight = coinSize;
                    }
                }
            }

            // 애니메이션 (완료 시 풀에 반환)
            float travelY = isCritical ? 74.064f : 54.047f;
            float horizontalDrift = UnityEngine.Random.Range(-16.014f, 16.014f);
            float duration = isCritical ? 1.0f : 0.85f;
            Vector2 targetPos = startPos + new Vector2(horizontalDrift, travelY);

            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOScale(isCritical ? 1.25f : 1.08f, 0.12f).SetEase(Ease.OutQuad));
            seq.Append(rect.DOScale(1f, isCritical ? 0.26f : 0.18f).SetEase(Ease.OutBack));

            seq.Join(rect.DOAnchorPos(targetPos, duration).SetEase(isCritical ? Ease.OutCubic : Ease.OutQuad));
            seq.Join(tmp.DOFade(0f, duration * 0.9f).SetDelay(duration * 0.1f).SetEase(Ease.OutQuad));

            // 코인 이미지도 함께 페이드 아웃
            if (coinImage != null)
            {
                seq.Join(coinImage.DOFade(0f, duration * 0.9f).SetDelay(duration * 0.1f).SetEase(Ease.OutQuad));
            }

            // ★ 크리티컬: 카운트업 애니메이션 + 색상 펄스 효과
            if (isCritical)
            {
                // 0에서 최종 값까지 카운트업
                double countupValue = 0;
                float countupDuration = 0.25f;
                DOTween.To(() => countupValue, x => {
                    countupValue = x;
                    tmp.text = $"+{GoldManager.FormatNumber(countupValue)}";
                }, amount, countupDuration).SetEase(Ease.OutQuad);

                // 색상 펄스: 흰색 → 크리티컬 색상 → 약간 밝게
                tmp.color = Color.white;
                seq.Join(tmp.DOColor(_criticalColor, 0.15f).SetEase(Ease.OutQuad));
                seq.Join(DOVirtual.DelayedCall(0.15f, () => {
                    if (tmp != null)
                    {
                        tmp.DOColor(_criticalColor * 1.2f, 0.1f)
                            .OnComplete(() => tmp.DOColor(_criticalColor, 0.1f));
                    }
                }));

                // 회전 펀치 효과
                seq.Join(rect.DOPunchRotation(new Vector3(0f, 0f, 16f), 0.45f, 12, 0.85f));

                // 스케일 펄스 (2회)
                seq.Join(DOVirtual.DelayedCall(0.2f, () => {
                    if (rect != null)
                    {
                        rect.DOPunchScale(Vector3.one * 0.08f, 0.15f, 3, 0.5f);
                    }
                }));
            }
            else
            {
                // 일반 클릭: 바로 값 표시
                tmp.text = $"+{GoldManager.FormatNumber(amount)}";
                tmp.color = Color.yellow;
            }

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
        private const float DOUBLE_CLICK_TIME = 0.4f;

        private void OnAutoSpinClicked()
        {
            if (_isAutoSpinning)
            {
                // 자동 스핀 중이면 중지
                StopAutoSpin();
                return;
            }

            float currentTime = Time.time;

            // 더블클릭 감지 - 횟수 변경 (싱글클릭은 즉시 시작)
            if (currentTime - _lastAutoSpinClickTime < DOUBLE_CLICK_TIME)
            {
                // 더블클릭: 횟수 순환
                int currentIndex = System.Array.IndexOf(_autoSpinOptions, _autoSpinCount);
                currentIndex = (currentIndex + 1) % _autoSpinOptions.Length;
                _autoSpinCount = _autoSpinOptions[currentIndex];
                UpdateAutoSpinButton();
                ShowToast($"Auto-spin: x{_autoSpinCount}", new Color(0.7f, 0.7f, 0.9f), 0.8f);
                _lastAutoSpinClickTime = currentTime;
                return;
            }

            _lastAutoSpinClickTime = currentTime;

            // 싱글클릭: 즉시 자동 스핀 시작
            StartAutoSpin();
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

                // 다음 스핀 전 짧은 딜레이 (게임 템포 개선)
                yield return MobileOptimizer.GetWait(0.2f);
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
                _autoSpinText.text = $"x{_autoSpinCount}";
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

            // ★ 스핀 시작 사운드 + 루프 사운드
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.SpinStart);
                SoundManager.Instance.PlayLoopSFX(SoundType.SpinLoop, 0.5f);
            }

            // ★ 스핀 버튼 피드백
            if (_spinButton != null)
            {
                _spinButton.transform.DOKill();
                _spinButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f);
            }

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
        /// 릴 스핀 애니메이션 코루틴 - 가속/고속/감속 3단계 애니메이션
        /// SpinProfile이 있으면 프로파일 값 사용, 없으면 기본값 사용
        /// </summary>
        private System.Collections.IEnumerator SpinReelAnimation(int reelIndex)
        {
            // 범위 체크
            if (reelIndex < 0 || reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null)
                yield break;

            int symbolCount = _symbolSprites != null && _symbolSprites.Length > 0
                ? _symbolSprites.Length
                : _game.Config.symbolCount;

            // ★ 열(column)별 시작 딜레이 - 프로파일 또는 기본값
            int column = reelIndex % 3;
            float columnDelay = _spinProfile != null
                ? _spinProfile.GetColumnStopDelay(column)
                : column * 0.08f;
            yield return MobileOptimizer.GetWait(columnDelay);

            // ★ 프로파일 또는 기본값에서 파라미터 가져오기
            float accelerationDuration = _spinProfile != null ? _spinProfile.accelDuration : 0.3f;
            float startSpeed = _spinProfile != null ? _spinProfile.accelStartSpeed : 0.15f;
            float maxSpeed = _spinProfile != null ? _spinProfile.maxSpeed : 0.03f;

            // ★ Phase 1: 가속 - 느리게 시작해서 빠르게
            float accelerationTime = 0f;

            while (accelerationTime < accelerationDuration && reelIndex < _isReelSpinning.Length && _isReelSpinning[reelIndex])
            {
                // EaseOutQuad 가속 곡선
                float t = accelerationTime / accelerationDuration;
                float currentSpeed = Mathf.Lerp(startSpeed, maxSpeed, t * t);

                // 심볼 변경 + 슬라이드 효과
                SpinReelStep(reelIndex, symbolCount, currentSpeed, true);

                yield return MobileOptimizer.GetWait(currentSpeed);
                accelerationTime += currentSpeed;
            }

            // ★ Phase 2: 고속 스핀 (정지 신호까지 지속)
            while (reelIndex < _isReelSpinning.Length && _isReelSpinning[reelIndex])
            {
                SpinReelStep(reelIndex, symbolCount, maxSpeed, false);
                yield return MobileOptimizer.GetWait(maxSpeed);
            }
        }

        /// <summary>
        /// 스핀 단계별 심볼 변경 및 효과
        /// SpinProfile 파라미터 사용
        /// </summary>
        private void SpinReelStep(int reelIndex, int symbolCount, float speed, bool slideEffect)
        {
            if (reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null) return;

            // 랜덤 심볼 설정
            int randomSymbol = UnityEngine.Random.Range(0, symbolCount);
            SetReelSymbol(reelIndex, randomSymbol);

            Transform symbolTransform = _reelSymbols[reelIndex].transform;
            symbolTransform.DOKill();

            // ★ 프로파일에서 값 가져오기
            float slideDistance = _spinProfile != null ? _spinProfile.slideDistance : 15f;
            float punchScale = _spinProfile != null ? _spinProfile.accelPunchScale : 0.06f;
            float blurAlpha = _spinProfile != null ? _spinProfile.spinBlurAlpha : 0.85f;

            if (slideEffect)
            {
                // 가속 단계: 아래에서 위로 슬라이드 + 스케일 펀치
                RectTransform rect = symbolTransform.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -slideDistance);
                    rect.DOAnchorPosY(0f, speed * 0.8f).SetEase(Ease.OutQuad);
                }
                symbolTransform.localScale = Vector3.one * 0.9f;
                symbolTransform.DOScale(1f, speed * 0.8f);
            }
            else
            {
                // 고속 단계: 빠른 스케일 펀치
                symbolTransform.localScale = Vector3.one;
                symbolTransform.DOPunchScale(Vector3.one * punchScale, speed * 0.8f, 0, 0);
            }

            // 블러 효과 (고속 시 약간 투명하게)
            Image symbolImage = _reelSymbols[reelIndex];
            if (symbolImage != null && speed < 0.05f)
            {
                symbolImage.DOKill();
                symbolImage.color = new Color(1f, 1f, 1f, blurAlpha);
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

                // ★ 감속 정지 애니메이션 시작
                StartCoroutine(ReelStopAnimation(reelIndex, symbolIndex));
            }

            if (_spinState == SpinUIState.Spinning)
            {
                SetSpinState(SpinUIState.Stopping);
            }
        }

        /// <summary>
        /// 릴 정지 애니메이션 - 감속 효과와 바운스
        /// SpinProfile 파라미터 사용
        /// </summary>
        private System.Collections.IEnumerator ReelStopAnimation(int reelIndex, int finalSymbolIndex)
        {
            if (reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null)
                yield break;

            int symbolCount = _symbolSprites != null && _symbolSprites.Length > 0
                ? _symbolSprites.Length
                : _game.Config.symbolCount;

            // ★ 프로파일에서 감속 파라미터 가져오기
            int decelSteps = _spinProfile != null ? _spinProfile.decelerationSteps : 3;
            float blurAlpha = _spinProfile != null ? _spinProfile.spinBlurAlpha : 0.85f;

            // ★ Phase 1: 감속 (프로파일 기반 심볼 변경하며 느려짐)
            for (int i = 0; i < decelSteps; i++)
            {
                float decelSpeed = _spinProfile != null
                    ? _spinProfile.GetDecelerationSpeed(i, decelSteps)
                    : 0.06f + (i * 0.04f); // 기본: 0.06, 0.10, 0.14...

                int randomSymbol = UnityEngine.Random.Range(0, symbolCount);
                SetReelSymbol(reelIndex, randomSymbol);

                Transform symbolTransform = _reelSymbols[reelIndex].transform;
                RectTransform rect = symbolTransform.GetComponent<RectTransform>();
                symbolTransform.DOKill();

                // 위에서 아래로 슬라이드 (감속 느낌)
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 4.804f);
                    rect.DOAnchorPosY(0f, decelSpeed * 0.9f).SetEase(Ease.OutQuad);
                }

                // 점점 선명해지는 효과
                _reelSymbols[reelIndex].DOKill();
                float alphaProgress = (float)i / Mathf.Max(1, decelSteps - 1);
                float alpha = blurAlpha + ((1f - blurAlpha) * alphaProgress);
                _reelSymbols[reelIndex].color = new Color(1f, 1f, 1f, alpha);

                yield return MobileOptimizer.GetWait(decelSpeed);
            }

            // ★ 프로파일에서 바운스 파라미터 가져오기
            float bounceIntensity = _spinProfile != null ? _spinProfile.bounceIntensity : 0.12f;
            float bounceDuration = _spinProfile != null ? _spinProfile.bounceDuration : 0.3f;
            int bounceVibrato = _spinProfile != null ? _spinProfile.bounceVibrato : 4;
            float bounceElasticity = _spinProfile != null ? _spinProfile.bounceElasticity : 0.6f;
            bool enableFlash = _spinProfile != null ? _spinProfile.enableLandingFlash : true;
            float flashIntensity = _spinProfile != null ? _spinProfile.flashIntensity : 1.4f;
            float flashDuration = _spinProfile != null ? _spinProfile.flashDuration : 0.1f;

            // ★ Phase 2: 최종 심볼 설정 + 바운스 정지
            Transform finalTransform = _reelSymbols[reelIndex].transform;
            RectTransform finalRect = finalTransform.GetComponent<RectTransform>();

            finalTransform.DOKill();
            finalTransform.localScale = Vector3.one;
            finalTransform.rotation = Quaternion.identity;

            // 최종 심볼 설정
            SetReelSymbol(reelIndex, finalSymbolIndex);

            // 위에서 떨어지며 착지하는 효과
            if (finalRect != null)
            {
                finalRect.anchoredPosition = new Vector2(finalRect.anchoredPosition.x, 7.206f);
                finalRect.DOAnchorPosY(0f, 0.2f).SetEase(Ease.OutBounce);
            }

            // 마지막 열인지 확인 (column 2 = 인덱스 2, 5, 8)
            bool isLastColumn = (reelIndex == 2 || reelIndex == 5 || reelIndex == 8);
            bool isVeryLastReel = (reelIndex == 8);  // 가장 마지막 릴

            // 마지막 열은 더 강한 바운스
            float finalBounce = isLastColumn ? bounceIntensity * 1.5f : bounceIntensity;
            int finalVibrato = isLastColumn ? bounceVibrato + 2 : bounceVibrato;

            // 바운스 스케일 효과 (프로파일 파라미터 사용)
            finalTransform.DOPunchScale(Vector3.one * finalBounce, bounceDuration, finalVibrato, bounceElasticity);

            // 정지 플래시 효과 (선명하게 복원)
            _reelSymbols[reelIndex].DOKill();
            _reelSymbols[reelIndex].color = Color.white;

            if (enableFlash)
            {
                float finalFlash = isLastColumn ? flashIntensity * 1.3f : flashIntensity;
                _reelSymbols[reelIndex].DOColor(Color.white * finalFlash, flashDuration)
                    .OnComplete(() => _reelSymbols[reelIndex].DOColor(Color.white, flashDuration * 2f));
            }

            // ★ 릴 정지 사운드
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.ReelStop);
            }

            // 햅틱 피드백 (마지막 열은 더 강하게)
            if (isVeryLastReel)
            {
                UIFeedback.TriggerHaptic(UIFeedback.HapticType.Medium);
                // 마지막 릴 정지 시 화면 살짝 흔들림
                if (_mainCanvas != null)
                {
                    _mainCanvas.transform.DOShakePosition(0.15f, 5f, 12, 90f, false, true);
                }
            }
            else
            {
                UIFeedback.TriggerHaptic(UIFeedback.HapticType.Light);
            }
        }

        private void OnSlotSpinComplete(SlotResult result)
        {
            // ★ 스핀 루프 사운드 정지
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopLoopSFX();
            }

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

            // 당첨 릴 인덱스 계산
            int[] highlightIndices = GetWinningReelIndices(result);

            // 슬롯 승리 피드백 시스템 활용 (승리 또는 무승부인 경우)
            if (_slotWinFeedback != null && result.Outcome != SlotOutcome.Loss)
            {
                _slotWinFeedback.PlayWinFeedback(result, highlightIndices);

                // 큰 승리일수록 화면 중앙에 추가 임팩트를 얹는다
                if (result.Outcome >= SlotOutcome.BigWin)
                {
                    Vector2 centerPos = Vector2.zero;
                    SpawnClickParticles(centerPos, true);
                    SpawnClickRipple(centerPos, true, _jackpotColor, 1.45f, 1.25f);
                }

                // 잭팟 계열은 화면 테두리/플래시를 한 번 더 강조
                if (result.Outcome == SlotOutcome.Jackpot || result.Outcome == SlotOutcome.MegaJackpot)
                {
                    PlayScreenGlow(true);
                    float flashDuration = result.Outcome == SlotOutcome.MegaJackpot ? 1.45f : 1.15f;
                    float flashAlpha = result.Outcome == SlotOutcome.MegaJackpot ? 1.25f : 1.05f;
                    PlayCriticalFlash(_jackpotColor, durationMultiplier: flashDuration, alphaMultiplier: flashAlpha);
                }
            }
            else
            {
                // 니어미스 피드백 (아깝게 놓친 경우)
                if (result.IsNearMiss && result.NearMissPayline != null && result.NearMissPayline.Length > 0)
                {
                    ShowResult("SO CLOSE!", new Color(1f, 0.6f, 0.3f));
                    ShowToast("Almost there! Try again!", new Color(1f, 0.7f, 0.4f), 1.2f);

                    // 니어미스 페이라인 하이라이트 (깜빡임)
                    HighlightNearMissReels(result.NearMissPayline);
                }
                else
                {
                    // 일반 패배
                    ShowResult("No Match...", Color.gray);
                }

                // 패배 사운드 재생
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySlotResultSound(SlotOutcome.Loss);
                }
            }

            // 연승 콤보 피드백 (2연승 이상)
            if (result.WinStreak >= 2)
            {
                ShowComboFeedback(result.WinStreak, result.ComboMultiplier);
            }

            // 잭팟 당첨 시 자동 스핀 중지
            if (_isAutoSpinning && (result.Outcome == SlotOutcome.Jackpot || result.Outcome == SlotOutcome.MegaJackpot))
            {
                StopAutoSpin();
                ShowToast("JACKPOT! Auto-spin stopped", new Color(1f, 0.8f, 0.2f));
            }

            // 결과에 따른 Ready 상태 복귀 지연 시간 조정 (게임 템포 개선)
            float readyDelay = result.Outcome switch
            {
                SlotOutcome.MegaJackpot => 2.5f,  // 6f → 2.5f
                SlotOutcome.Jackpot => 2f,        // 4.5f → 2f
                SlotOutcome.BigWin => 1.5f,       // 3f → 1.5f
                SlotOutcome.SmallWin => 1f,       // 2.5f → 1f
                SlotOutcome.MiniWin => 0.8f,      // 2f → 0.8f
                SlotOutcome.Draw => 0.6f,         // 1.5f → 0.6f
                _ => 0.5f                          // 1.2f → 0.5f
            };

            DOVirtual.DelayedCall(readyDelay, () => SetSpinState(SpinUIState.Ready));
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

        /// <summary>
        /// 니어미스 릴 하이라이트 (아깝게 놓친 심볼 강조)
        /// </summary>
        private void HighlightNearMissReels(int[] paylineIndices)
        {
            if (_reelFrames == null || paylineIndices == null) return;

            Color nearMissColor = new Color(1f, 0.5f, 0.2f, 1f);  // 주황색

            foreach (int idx in paylineIndices)
            {
                if (idx >= 0 && idx < _reelFrames.Length && _reelFrames[idx] != null)
                {
                    Image frame = _reelFrames[idx];
                    Color originalColor = frame.color;

                    // 깜빡임 효과 (3회)
                    Sequence blinkSeq = DOTween.Sequence();
                    blinkSeq.Append(frame.DOColor(nearMissColor, 0.12f));
                    blinkSeq.Append(frame.DOColor(originalColor, 0.12f));
                    blinkSeq.SetLoops(3);
                    blinkSeq.OnComplete(() => frame.color = originalColor);

                    // 심볼 흔들기
                    if (_reelSymbols != null && idx < _reelSymbols.Length && _reelSymbols[idx] != null)
                    {
                        _reelSymbols[idx].transform.DOShakePosition(0.5f, 8f, 15, 90f, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// 연승 콤보 피드백
        /// </summary>
        private void ShowComboFeedback(int streak, float multiplier)
        {
            string comboText = streak switch
            {
                2 => "2x COMBO!",
                3 => "3x COMBO!!",
                4 => "4x COMBO!!!",
                5 => "5x STREAK!",
                >= 6 => $"{streak}x SUPER STREAK!",
                _ => ""
            };

            if (string.IsNullOrEmpty(comboText)) return;

            Color comboColor = streak switch
            {
                2 => new Color(0.4f, 0.8f, 1f),      // 하늘색
                3 => new Color(0.4f, 1f, 0.6f),      // 연두색
                4 => new Color(1f, 0.9f, 0.3f),      // 노란색
                5 => new Color(1f, 0.6f, 0.2f),      // 주황색
                >= 6 => new Color(1f, 0.3f, 0.5f),   // 분홍색
                _ => Color.white
            };

            // 콤보 토스트 표시
            ShowToast($"{comboText} (+{(multiplier - 1f) * 100:F0}% Bonus!)", comboColor, 1.5f);

            // 콤보 이펙트 (화면 가장자리 글로우)
            if (streak >= 3)
            {
                PlayScreenGlow(false);
            }

            // 5연승 이상이면 추가 파티클
            if (streak >= 5)
            {
                Vector2 centerPos = Vector2.zero;
                SpawnClickParticles(centerPos, true);
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

            // 화면 테두리 글로우 (잭팟용)
            PlayScreenGlow(true);

            // 대량 파티클 분출
            Vector2 centerPos = Vector2.zero;
            for (int i = 0; i < 3; i++)
            {
                DOVirtual.DelayedCall(i * 0.15f, () =>
                {
                    SpawnClickParticles(centerPos + new Vector2(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-50f, 50f)), true);
                });
            }

            // 강력한 햅틱
            UIFeedback.TriggerHaptic(UIFeedback.HapticType.Heavy);
        }

        #endregion

        #region SpinProfile API

        /// <summary>
        /// 현재 스핀 프로파일 (읽기 전용)
        /// </summary>
        public SlotClickerSpinProfile SpinProfile => _spinProfile;

        /// <summary>
        /// 런타임에 스핀 프로파일 설정
        /// </summary>
        /// <param name="profile">새로운 스핀 프로파일 (null이면 기본값 사용)</param>
        public void SetSpinProfile(SlotClickerSpinProfile profile)
        {
            _spinProfile = profile;
            Debug.Log($"[SlotClickerUI] SpinProfile changed to: {(profile != null ? profile.name : "Default")}");
        }

        /// <summary>
        /// Resources 폴더에서 스핀 프로파일 로드
        /// </summary>
        /// <param name="profileName">프로파일 이름 (확장자 제외)</param>
        /// <returns>로드 성공 여부</returns>
        public bool LoadSpinProfileFromResources(string profileName)
        {
            SlotClickerSpinProfile profile = Resources.Load<SlotClickerSpinProfile>(profileName);
            if (profile != null)
            {
                SetSpinProfile(profile);
                return true;
            }

            Debug.LogWarning($"[SlotClickerUI] SpinProfile not found in Resources: {profileName}");
            return false;
        }

        /// <summary>
        /// 현재 프로파일 파라미터 정보 문자열
        /// </summary>
        public string GetSpinProfileInfo()
        {
            if (_spinProfile == null)
                return "SpinProfile: Default (no profile assigned)";

            return $"SpinProfile: {_spinProfile.name}\n" +
                   $"  Accel: {_spinProfile.accelDuration}s, Start:{_spinProfile.accelStartSpeed}, Max:{_spinProfile.maxSpeed}\n" +
                   $"  Decel: {_spinProfile.decelerationSteps} steps\n" +
                   $"  Bounce: {_spinProfile.bounceIntensity}, {_spinProfile.bounceDuration}s, vibrato:{_spinProfile.bounceVibrato}\n" +
                   $"  Flash: {(_spinProfile.enableLandingFlash ? "ON" : "OFF")}, intensity:{_spinProfile.flashIntensity}";
        }

        #endregion
    }
}
