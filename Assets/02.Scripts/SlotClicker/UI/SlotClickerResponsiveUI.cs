using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// SlotClicker UI를 화면 방향에 따라 자동 조정하는 컴포넌트
    /// - Portrait: 기본 세로 레이아웃
    /// - Landscape: 가로 최적화 레이아웃
    /// </summary>
    public class SlotClickerResponsiveUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("=== Canvas 참조 ===")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private CanvasScaler _canvasScaler;

        [Header("=== UI 영역 참조 ===")]
        [Tooltip("상단 HUD 패널")]
        [SerializeField] private RectTransform _topHUD;

        [Tooltip("슬롯 머신 영역")]
        [SerializeField] private RectTransform _slotPanel;

        [Tooltip("클릭 영역")]
        [SerializeField] private RectTransform _clickArea;

        [Tooltip("베팅 UI 영역")]
        [SerializeField] private RectTransform _bettingPanel;

        [Tooltip("업그레이드 버튼")]
        [SerializeField] private RectTransform _upgradeButton;

        [Tooltip("프레스티지 버튼")]
        [SerializeField] private RectTransform _prestigeButton;

        [Header("=== Portrait 레이아웃 설정 ===")]
        [SerializeField] private Vector2 _portraitSlotPosition = new Vector2(0, -475);
        [SerializeField] private Vector2 _portraitSlotSize = new Vector2(480, 480);
        [SerializeField] private Vector2 _portraitClickPosition = new Vector2(0, -280);
        [SerializeField] private Vector2 _portraitClickSize = new Vector2(420, 150);
        [SerializeField] private Vector2 _portraitBettingPosition = new Vector2(0, 150);

        [Header("=== Landscape 레이아웃 설정 ===")]
        [SerializeField] private Vector2 _landscapeSlotPosition = new Vector2(-300, 0);
        [SerializeField] private Vector2 _landscapeSlotSize = new Vector2(400, 400);
        [SerializeField] private Vector2 _landscapeClickPosition = new Vector2(300, 100);
        [SerializeField] private Vector2 _landscapeClickSize = new Vector2(350, 120);
        [SerializeField] private Vector2 _landscapeBettingPosition = new Vector2(300, -150);

        [Header("=== CanvasScaler 설정 ===")]
        [SerializeField] private Vector2 _portraitResolution = new Vector2(1080, 1920);
        [SerializeField] private Vector2 _landscapeResolution = new Vector2(1920, 1080);
        [SerializeField] private float _portraitMatch = 0.5f;
        [SerializeField] private float _landscapeMatch = 0.5f;

        [Header("=== 전환 설정 ===")]
        [SerializeField] private float _transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("=== 디버그 ===")]
        [SerializeField] private bool _debugMode = false;

        #endregion

        #region Private Fields

        private OrientationManager _orientationManager;
        private bool _isInitialized = false;
        private bool _isTransitioning = false;
        private float _transitionProgress = 0f;

        // 현재/목표 레이아웃 캐시
        private LayoutCache _currentLayout;
        private LayoutCache _targetLayout;
        private LayoutCache _portraitLayout;
        private LayoutCache _landscapeLayout;

        private struct LayoutCache
        {
            public Vector2 slotPosition;
            public Vector2 slotSize;
            public Vector2 clickPosition;
            public Vector2 clickSize;
            public Vector2 bettingPosition;
            public Vector2 canvasResolution;
            public float canvasMatch;
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private System.Collections.IEnumerator InitializeAsync()
        {
            // OrientationManager 대기
            while (OrientationManager.Instance == null)
            {
                yield return null;
            }

            _orientationManager = OrientationManager.Instance;

            // Canvas 참조 확인
            if (_mainCanvas == null)
            {
                _mainCanvas = GetComponentInParent<Canvas>();
            }

            if (_canvasScaler == null && _mainCanvas != null)
            {
                _canvasScaler = _mainCanvas.GetComponent<CanvasScaler>();
            }

            // 레이아웃 캐시 초기화
            InitializeLayoutCaches();

            // 이벤트 구독
            _orientationManager.OnOrientationTransitionStart += OnOrientationTransitionStart;
            _orientationManager.OnOrientationTransitionComplete += OnOrientationTransitionComplete;

            // 초기 레이아웃 적용
            ApplyLayoutImmediate(_orientationManager.IsPortrait);

            _isInitialized = true;

            Debug.Log("[SlotClickerResponsiveUI] Initialized");
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (_isTransitioning)
            {
                ProcessTransition();
            }
        }

        private void OnDestroy()
        {
            if (_orientationManager != null)
            {
                _orientationManager.OnOrientationTransitionStart -= OnOrientationTransitionStart;
                _orientationManager.OnOrientationTransitionComplete -= OnOrientationTransitionComplete;
            }
        }

        #endregion

        #region Initialization

        private void InitializeLayoutCaches()
        {
            // Portrait 레이아웃
            _portraitLayout = new LayoutCache
            {
                slotPosition = _portraitSlotPosition,
                slotSize = _portraitSlotSize,
                clickPosition = _portraitClickPosition,
                clickSize = _portraitClickSize,
                bettingPosition = _portraitBettingPosition,
                canvasResolution = _portraitResolution,
                canvasMatch = _portraitMatch
            };

            // Landscape 레이아웃
            _landscapeLayout = new LayoutCache
            {
                slotPosition = _landscapeSlotPosition,
                slotSize = _landscapeSlotSize,
                clickPosition = _landscapeClickPosition,
                clickSize = _landscapeClickSize,
                bettingPosition = _landscapeBettingPosition,
                canvasResolution = _landscapeResolution,
                canvasMatch = _landscapeMatch
            };
        }

        #endregion

        #region Event Handlers

        private void OnOrientationTransitionStart(
            OrientationManager.DeviceOrientation from,
            OrientationManager.DeviceOrientation to)
        {
            bool toPortrait = to == OrientationManager.DeviceOrientation.Portrait ||
                              to == OrientationManager.DeviceOrientation.PortraitUpsideDown;

            _currentLayout = toPortrait ? _landscapeLayout : _portraitLayout;
            _targetLayout = toPortrait ? _portraitLayout : _landscapeLayout;

            _isTransitioning = true;
            _transitionProgress = 0f;

            if (_debugMode)
            {
                Debug.Log($"[SlotClickerResponsiveUI] Transition started: {from} -> {to}");
            }
        }

        private void OnOrientationTransitionComplete(OrientationManager.DeviceOrientation orientation)
        {
            _isTransitioning = false;

            bool isPortrait = orientation == OrientationManager.DeviceOrientation.Portrait ||
                              orientation == OrientationManager.DeviceOrientation.PortraitUpsideDown;

            ApplyLayoutImmediate(isPortrait);

            if (_debugMode)
            {
                Debug.Log($"[SlotClickerResponsiveUI] Transition complete: {orientation}");
            }
        }

        #endregion

        #region Layout Application

        private void ApplyLayoutImmediate(bool isPortrait)
        {
            LayoutCache layout = isPortrait ? _portraitLayout : _landscapeLayout;
            ApplyLayout(layout);
        }

        private void ApplyLayout(LayoutCache layout)
        {
            // 슬롯 패널
            if (_slotPanel != null)
            {
                _slotPanel.anchoredPosition = layout.slotPosition;
                _slotPanel.sizeDelta = layout.slotSize;
            }

            // 클릭 영역
            if (_clickArea != null)
            {
                _clickArea.anchoredPosition = layout.clickPosition;
                _clickArea.sizeDelta = layout.clickSize;
            }

            // 베팅 패널
            if (_bettingPanel != null)
            {
                _bettingPanel.anchoredPosition = layout.bettingPosition;
            }

            // CanvasScaler
            if (_canvasScaler != null)
            {
                _canvasScaler.referenceResolution = layout.canvasResolution;
                _canvasScaler.matchWidthOrHeight = layout.canvasMatch;
            }
        }

        private void ProcessTransition()
        {
            _transitionProgress += Time.deltaTime / _transitionDuration;

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioning = false;
            }

            float t = _transitionCurve.Evaluate(_transitionProgress);
            LayoutCache interpolated = LerpLayout(_currentLayout, _targetLayout, t);
            ApplyLayout(interpolated);
        }

        private LayoutCache LerpLayout(LayoutCache a, LayoutCache b, float t)
        {
            return new LayoutCache
            {
                slotPosition = Vector2.Lerp(a.slotPosition, b.slotPosition, t),
                slotSize = Vector2.Lerp(a.slotSize, b.slotSize, t),
                clickPosition = Vector2.Lerp(a.clickPosition, b.clickPosition, t),
                clickSize = Vector2.Lerp(a.clickSize, b.clickSize, t),
                bettingPosition = Vector2.Lerp(a.bettingPosition, b.bettingPosition, t),
                canvasResolution = Vector2.Lerp(a.canvasResolution, b.canvasResolution, t),
                canvasMatch = Mathf.Lerp(a.canvasMatch, b.canvasMatch, t)
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// Portrait 레이아웃 설정 업데이트
        /// </summary>
        public void SetPortraitLayout(
            Vector2 slotPos, Vector2 slotSize,
            Vector2 clickPos, Vector2 clickSize,
            Vector2 bettingPos)
        {
            _portraitSlotPosition = slotPos;
            _portraitSlotSize = slotSize;
            _portraitClickPosition = clickPos;
            _portraitClickSize = clickSize;
            _portraitBettingPosition = bettingPos;

            InitializeLayoutCaches();

            if (_orientationManager != null && _orientationManager.IsPortrait)
            {
                ApplyLayoutImmediate(true);
            }
        }

        /// <summary>
        /// Landscape 레이아웃 설정 업데이트
        /// </summary>
        public void SetLandscapeLayout(
            Vector2 slotPos, Vector2 slotSize,
            Vector2 clickPos, Vector2 clickSize,
            Vector2 bettingPos)
        {
            _landscapeSlotPosition = slotPos;
            _landscapeSlotSize = slotSize;
            _landscapeClickPosition = clickPos;
            _landscapeClickSize = clickSize;
            _landscapeBettingPosition = bettingPos;

            InitializeLayoutCaches();

            if (_orientationManager != null && _orientationManager.IsLandscape)
            {
                ApplyLayoutImmediate(false);
            }
        }

        /// <summary>
        /// UI 참조 설정 (런타임에서 동적으로 설정)
        /// </summary>
        public void SetUIReferences(
            RectTransform topHUD,
            RectTransform slotPanel,
            RectTransform clickArea,
            RectTransform bettingPanel)
        {
            _topHUD = topHUD;
            _slotPanel = slotPanel;
            _clickArea = clickArea;
            _bettingPanel = bettingPanel;

            // 현재 방향에 맞게 레이아웃 적용
            if (_orientationManager != null)
            {
                ApplyLayoutImmediate(_orientationManager.IsPortrait);
            }
        }

        /// <summary>
        /// 강제로 특정 레이아웃 적용
        /// </summary>
        public void ForceLayout(bool portrait)
        {
            ApplyLayoutImmediate(portrait);
        }

        /// <summary>
        /// 현재 레이아웃을 Portrait 설정으로 캡처
        /// </summary>
        [ContextMenu("Capture Current as Portrait")]
        public void CaptureAsPortrait()
        {
            if (_slotPanel != null)
            {
                _portraitSlotPosition = _slotPanel.anchoredPosition;
                _portraitSlotSize = _slotPanel.sizeDelta;
            }

            if (_clickArea != null)
            {
                _portraitClickPosition = _clickArea.anchoredPosition;
                _portraitClickSize = _clickArea.sizeDelta;
            }

            if (_bettingPanel != null)
            {
                _portraitBettingPosition = _bettingPanel.anchoredPosition;
            }

            InitializeLayoutCaches();
            Debug.Log("[SlotClickerResponsiveUI] Portrait layout captured");
        }

        /// <summary>
        /// 현재 레이아웃을 Landscape 설정으로 캡처
        /// </summary>
        [ContextMenu("Capture Current as Landscape")]
        public void CaptureAsLandscape()
        {
            if (_slotPanel != null)
            {
                _landscapeSlotPosition = _slotPanel.anchoredPosition;
                _landscapeSlotSize = _slotPanel.sizeDelta;
            }

            if (_clickArea != null)
            {
                _landscapeClickPosition = _clickArea.anchoredPosition;
                _landscapeClickSize = _clickArea.sizeDelta;
            }

            if (_bettingPanel != null)
            {
                _landscapeBettingPosition = _bettingPanel.anchoredPosition;
            }

            InitializeLayoutCaches();
            Debug.Log("[SlotClickerResponsiveUI] Landscape layout captured");
        }

        #endregion

        #region Auto-Setup

        /// <summary>
        /// SlotClickerUI에서 참조 자동 찾기
        /// </summary>
        [ContextMenu("Auto Find UI References")]
        public void AutoFindUIReferences()
        {
            // Canvas 찾기
            if (_mainCanvas == null)
            {
                _mainCanvas = FindObjectOfType<Canvas>();
            }

            if (_canvasScaler == null && _mainCanvas != null)
            {
                _canvasScaler = _mainCanvas.GetComponent<CanvasScaler>();
            }

            // 이름으로 UI 요소 찾기
            Transform canvasTransform = _mainCanvas?.transform;
            if (canvasTransform == null) return;

            // TopHUD 찾기
            Transform hudTransform = FindChildRecursive(canvasTransform, "TopHUD");
            if (hudTransform != null)
            {
                _topHUD = hudTransform.GetComponent<RectTransform>();
            }

            // SlotPanel 찾기
            Transform slotTransform = FindChildRecursive(canvasTransform, "SlotPanel");
            if (slotTransform != null)
            {
                _slotPanel = slotTransform.GetComponent<RectTransform>();
            }

            // ClickArea 찾기
            Transform clickTransform = FindChildRecursive(canvasTransform, "ClickArea");
            if (clickTransform == null)
            {
                clickTransform = FindChildRecursive(canvasTransform, "ClickPanel");
            }
            if (clickTransform != null)
            {
                _clickArea = clickTransform.GetComponent<RectTransform>();
            }

            // BettingPanel 찾기
            Transform bettingTransform = FindChildRecursive(canvasTransform, "BettingPanel");
            if (bettingTransform == null)
            {
                bettingTransform = FindChildRecursive(canvasTransform, "BottomPanel");
            }
            if (bettingTransform != null)
            {
                _bettingPanel = bettingTransform.GetComponent<RectTransform>();
            }

            Debug.Log("[SlotClickerResponsiveUI] Auto find complete. Found references:" +
                      $" HUD={_topHUD != null}, Slot={_slotPanel != null}," +
                      $" Click={_clickArea != null}, Betting={_bettingPanel != null}");
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        #endregion
    }
}
