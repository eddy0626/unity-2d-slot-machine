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
    public partial class SlotClickerUI : MonoBehaviour
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

        [Header("=== Layout Profile ===")]
        [SerializeField] private SlotClickerUILayoutProfile _layoutProfile;
        [SerializeField] private UpgradeUILayoutProfile _upgradeLayoutProfile;
        [SerializeField] private OrientationUILayoutProfile _orientationLayoutProfile;
        [SerializeField] private UIFeedbackLayoutProfile _feedbackLayoutProfile;

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

        private SlotClickerUILayoutProfile Layout => _layoutProfile != null
            ? _layoutProfile
            : SlotClickerUILayoutProfile.Default;

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
            UIFeedback.Instance.SetLayoutProfile(_feedbackLayoutProfile);
            UIFeedback.Instance.SetCanvas(_mainCanvas);

            ApplyOptionalLayoutProfiles();

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
            UIFeedback.Instance.SetLayoutProfile(_feedbackLayoutProfile);
            UIFeedback.Instance.SetCanvas(_mainCanvas);

            ApplyOptionalLayoutProfiles();

            Debug.Log("[SlotClickerUI] UI created successfully!");
        }

        private void ApplyOptionalLayoutProfiles()
        {
            if (_orientationLayoutProfile == null) return;

            OrientationSettingsUI orientationUI = FindObjectOfType<OrientationSettingsUI>();
            if (orientationUI != null)
            {
                orientationUI.SetLayoutProfile(_orientationLayoutProfile);
            }
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
                new Vector2(0, 0), new Vector2(0, Layout.BetPanelHeight), new Color(0.12f, 0.1f, 0.18f, 0.95f));  // 높이 확대
            RectTransform betRect = betPanel.GetComponent<RectTransform>();
            betRect.anchoredPosition = new Vector2(0, Layout.BetPanelYOffset); // 하단 오프셋

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
            float buttonWidth = Layout.BetButtonWidth;  // 버튼 폭
            float buttonSpacing = Layout.BetButtonSpacing;  // 간격
            float totalWidth = (buttonWidth * 4) + (buttonSpacing * 3);
            float startX = -totalWidth / 2 + buttonWidth / 2;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                float betValue = betValues[i];

                GameObject btnObj = CreateButton(betRect, $"BetBtn_{i}", betLabels[i],
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + (i * (buttonWidth + buttonSpacing)), Layout.BetButtonY),
                    new Vector2(buttonWidth, Layout.BetButtonHeight),  // 버튼 높이
                    new Color(0.3f, 0.3f, 0.5f));

                _betButtons[i] = btnObj.GetComponent<Button>();
                _betButtons[i].onClick.AddListener(() => SetBetPercentage(betValue));

                // 버튼 텍스트 크기 확대
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.fontSize = Layout.BetButtonFont;
            }

            // 스핀 버튼 - 크게 확대하여 하단 중앙에 배치
            float spinWidth = Layout.SpinButtonWidth;
            float autoWidth = Layout.AutoButtonWidth;
            float spinAutoHeight = Layout.SpinAutoHeight;
            float spinAutoGap = Layout.SpinAutoGap;
            float spinAutoTotal = spinWidth + autoWidth + spinAutoGap;
            float spinX = -spinAutoTotal / 2f + spinWidth / 2f;
            float autoX = spinX + spinWidth / 2f + spinAutoGap + autoWidth / 2f;
            float spinAutoY = Layout.SpinAutoY;

            GameObject spinObj = CreateButton(betRect, "SpinButton", "SPIN!",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(spinX, spinAutoY), new Vector2(spinWidth, spinAutoHeight),  // 스핀 버튼 크기
                new Color(0.8f, 0.2f, 0.2f));
            _spinButton = spinObj.GetComponent<Button>();
            _spinButton.onClick.AddListener(OnSpinClicked);

            _spinButtonText = spinObj.GetComponentInChildren<TextMeshProUGUI>();
            _spinButtonText.fontSize = Layout.SpinButtonFont;  // 폰트 크기
            _spinButtonText.fontStyle = FontStyles.Bold;

            // 자동 스핀 버튼 - 크게 확대하여 스핀 버튼 우측에 배치
            GameObject autoSpinObj = CreateButton(betRect, "AutoSpinButton", "AUTO",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(autoX, spinAutoY), new Vector2(autoWidth, spinAutoHeight),  // 자동 스핀 버튼 크기
                new Color(0.3f, 0.5f, 0.7f));
            _autoSpinButton = autoSpinObj.GetComponent<Button>();
            _autoSpinButton.onClick.AddListener(OnAutoSpinClicked);

            _autoSpinText = autoSpinObj.GetComponentInChildren<TextMeshProUGUI>();
            _autoSpinText.fontSize = Layout.AutoButtonFont;  // 폰트 크기
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







    }
}
