using System;
using UnityEngine;
using UnityEngine.UI;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// 화면 방향에 따라 UI 레이아웃을 자동 조정하는 컴포넌트
    /// - Portrait/Landscape 별 위치, 크기, 앵커 설정
    /// - 자연스러운 전환 애니메이션
    /// - Canvas Scaler 자동 조정
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ResponsiveLayout : MonoBehaviour
    {
        #region Layout Settings

        [Serializable]
        public class LayoutSettings
        {
            [Header("앵커 설정")]
            public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
            public Vector2 anchorMax = new Vector2(0.5f, 0.5f);
            public Vector2 pivot = new Vector2(0.5f, 0.5f);

            [Header("위치/크기")]
            public Vector2 anchoredPosition = Vector2.zero;
            public Vector2 sizeDelta = new Vector2(100, 100);

            [Header("회전/스케일")]
            public float rotation = 0f;
            public Vector3 scale = Vector3.one;

            [Header("활성화 설정")]
            public bool isActive = true;

            /// <summary>
            /// 현재 RectTransform에서 설정 복사
            /// </summary>
            public void CopyFromRectTransform(RectTransform rt)
            {
                anchorMin = rt.anchorMin;
                anchorMax = rt.anchorMax;
                pivot = rt.pivot;
                anchoredPosition = rt.anchoredPosition;
                sizeDelta = rt.sizeDelta;
                rotation = rt.localEulerAngles.z;
                scale = rt.localScale;
                isActive = rt.gameObject.activeSelf;
            }

            /// <summary>
            /// 설정을 RectTransform에 적용
            /// </summary>
            public void ApplyToRectTransform(RectTransform rt)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
                rt.localEulerAngles = new Vector3(0, 0, rotation);
                rt.localScale = scale;
                rt.gameObject.SetActive(isActive);
            }

            /// <summary>
            /// 두 설정 사이를 보간
            /// </summary>
            public static LayoutSettings Lerp(LayoutSettings a, LayoutSettings b, float t)
            {
                return new LayoutSettings
                {
                    anchorMin = Vector2.Lerp(a.anchorMin, b.anchorMin, t),
                    anchorMax = Vector2.Lerp(a.anchorMax, b.anchorMax, t),
                    pivot = Vector2.Lerp(a.pivot, b.pivot, t),
                    anchoredPosition = Vector2.Lerp(a.anchoredPosition, b.anchoredPosition, t),
                    sizeDelta = Vector2.Lerp(a.sizeDelta, b.sizeDelta, t),
                    rotation = Mathf.LerpAngle(a.rotation, b.rotation, t),
                    scale = Vector3.Lerp(a.scale, b.scale, t),
                    isActive = t < 0.5f ? a.isActive : b.isActive
                };
            }
        }

        #endregion

        #region Serialized Fields

        [Header("=== 레이아웃 설정 ===")]
        [Tooltip("세로(Portrait) 모드 레이아웃")]
        [SerializeField] private LayoutSettings _portraitLayout = new LayoutSettings();

        [Tooltip("가로(Landscape) 모드 레이아웃")]
        [SerializeField] private LayoutSettings _landscapeLayout = new LayoutSettings();

        [Header("=== 전환 설정 ===")]
        [Tooltip("전환 애니메이션 사용")]
        [SerializeField] private bool _useTransitionAnimation = true;

        [Tooltip("자체 전환 시간 (0 = OrientationManager 시간 사용)")]
        [SerializeField, Range(0f, 1f)] private float _customTransitionDuration = 0f;

        [Header("=== 에디터 도구 ===")]
        [SerializeField] private bool _autoCapture = false;

        #endregion

        #region Private Fields

        private RectTransform _rectTransform;
        private OrientationManager.DeviceOrientation _currentOrientation;
        private bool _isTransitioning = false;
        private float _transitionProgress = 0f;
        private LayoutSettings _startLayout;
        private LayoutSettings _targetLayout;

        #endregion

        #region Properties

        /// <summary>
        /// 세로 레이아웃 설정
        /// </summary>
        public LayoutSettings PortraitLayout => _portraitLayout;

        /// <summary>
        /// 가로 레이아웃 설정
        /// </summary>
        public LayoutSettings LandscapeLayout => _landscapeLayout;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            // OrientationManager 이벤트 구독
            if (OrientationManager.Instance != null)
            {
                OrientationManager.Instance.OnOrientationTransitionStart += OnOrientationTransitionStart;
                OrientationManager.Instance.OnOrientationTransitionComplete += OnOrientationTransitionComplete;

                // 초기 레이아웃 적용
                _currentOrientation = OrientationManager.Instance.CurrentOrientation;
                ApplyLayoutImmediate(_currentOrientation);
            }
            else
            {
                // OrientationManager가 없으면 화면 비율로 판단
                bool isPortrait = Screen.height > Screen.width;
                _currentOrientation = isPortrait
                    ? OrientationManager.DeviceOrientation.Portrait
                    : OrientationManager.DeviceOrientation.LandscapeLeft;
                ApplyLayoutImmediate(_currentOrientation);
            }
        }

        private void Update()
        {
            if (_isTransitioning)
            {
                ProcessTransition();
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (OrientationManager.Instance != null)
            {
                OrientationManager.Instance.OnOrientationTransitionStart -= OnOrientationTransitionStart;
                OrientationManager.Instance.OnOrientationTransitionComplete -= OnOrientationTransitionComplete;
            }
        }

        #endregion

        #region Event Handlers

        private void OnOrientationTransitionStart(
            OrientationManager.DeviceOrientation from,
            OrientationManager.DeviceOrientation to)
        {
            if (!_useTransitionAnimation)
            {
                ApplyLayoutImmediate(to);
                return;
            }

            // 전환 시작
            _startLayout = GetLayoutForOrientation(from);
            _targetLayout = GetLayoutForOrientation(to);
            _isTransitioning = true;
            _transitionProgress = 0f;
            _currentOrientation = to;
        }

        private void OnOrientationTransitionComplete(OrientationManager.DeviceOrientation orientation)
        {
            _isTransitioning = false;
            _transitionProgress = 1f;

            // 최종 레이아웃 확실히 적용
            ApplyLayoutImmediate(orientation);
        }

        #endregion

        #region Layout Application

        /// <summary>
        /// 즉시 레이아웃 적용
        /// </summary>
        public void ApplyLayoutImmediate(OrientationManager.DeviceOrientation orientation)
        {
            LayoutSettings layout = GetLayoutForOrientation(orientation);
            layout.ApplyToRectTransform(_rectTransform);
            _currentOrientation = orientation;
        }

        /// <summary>
        /// 전환 애니메이션 처리
        /// </summary>
        private void ProcessTransition()
        {
            float duration = _customTransitionDuration > 0
                ? _customTransitionDuration
                : (OrientationManager.Instance != null ? 0.3f : 0.3f);

            _transitionProgress += Time.deltaTime / duration;

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioning = false;
            }

            // 보간된 레이아웃 적용
            float easedProgress = EaseInOutQuad(_transitionProgress);
            LayoutSettings interpolated = LayoutSettings.Lerp(_startLayout, _targetLayout, easedProgress);
            interpolated.ApplyToRectTransform(_rectTransform);
        }

        /// <summary>
        /// 방향에 따른 레이아웃 설정 반환
        /// </summary>
        private LayoutSettings GetLayoutForOrientation(OrientationManager.DeviceOrientation orientation)
        {
            bool isPortrait = orientation == OrientationManager.DeviceOrientation.Portrait ||
                              orientation == OrientationManager.DeviceOrientation.PortraitUpsideDown;

            return isPortrait ? _portraitLayout : _landscapeLayout;
        }

        /// <summary>
        /// Ease In Out Quad 보간
        /// </summary>
        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        #endregion

        #region Editor Tools

        /// <summary>
        /// 현재 RectTransform 설정을 Portrait 레이아웃으로 저장
        /// </summary>
        [ContextMenu("Capture Current as Portrait Layout")]
        public void CaptureAsPortraitLayout()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            _portraitLayout.CopyFromRectTransform(_rectTransform);
            Debug.Log($"[ResponsiveLayout] Portrait layout captured for {gameObject.name}");
        }

        /// <summary>
        /// 현재 RectTransform 설정을 Landscape 레이아웃으로 저장
        /// </summary>
        [ContextMenu("Capture Current as Landscape Layout")]
        public void CaptureAsLandscapeLayout()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            _landscapeLayout.CopyFromRectTransform(_rectTransform);
            Debug.Log($"[ResponsiveLayout] Landscape layout captured for {gameObject.name}");
        }

        /// <summary>
        /// Portrait 레이아웃 미리보기
        /// </summary>
        [ContextMenu("Preview Portrait Layout")]
        public void PreviewPortraitLayout()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            _portraitLayout.ApplyToRectTransform(_rectTransform);
        }

        /// <summary>
        /// Landscape 레이아웃 미리보기
        /// </summary>
        [ContextMenu("Preview Landscape Layout")]
        public void PreviewLandscapeLayout()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            _landscapeLayout.ApplyToRectTransform(_rectTransform);
        }

        #endregion
    }

    /// <summary>
    /// Canvas Scaler를 화면 방향에 따라 자동 조정하는 컴포넌트
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvasScaler : MonoBehaviour
    {
        [Header("=== Canvas Scaler 설정 ===")]
        [Tooltip("세로 모드 기준 해상도")]
        [SerializeField] private Vector2 _portraitReferenceResolution = new Vector2(390, 844);

        [Tooltip("가로 모드 기준 해상도")]
        [SerializeField] private Vector2 _landscapeReferenceResolution = new Vector2(844, 390);

        [Tooltip("세로 모드 Match (0=Width, 1=Height)")]
        [SerializeField, Range(0f, 1f)] private float _portraitMatch = 0.5f;

        [Tooltip("가로 모드 Match (0=Width, 1=Height)")]
        [SerializeField, Range(0f, 1f)] private float _landscapeMatch = 0.5f;

        [Header("=== 전환 설정 ===")]
        [Tooltip("전환 시 부드럽게 보간")]
        [SerializeField] private bool _smoothTransition = true;

        [Tooltip("보간 속도")]
        [SerializeField, Range(1f, 20f)] private float _transitionSpeed = 10f;

        private CanvasScaler _canvasScaler;
        private Vector2 _targetResolution;
        private float _targetMatch;

        private void Awake()
        {
            _canvasScaler = GetComponent<CanvasScaler>();
        }

        private void Start()
        {
            if (OrientationManager.Instance != null)
            {
                OrientationManager.Instance.OnOrientationChanged += OnOrientationChanged;
                ApplySettings(OrientationManager.Instance.IsPortrait);
            }
            else
            {
                ApplySettings(Screen.height > Screen.width);
            }
        }

        private void Update()
        {
            if (_smoothTransition)
            {
                // 부드러운 보간
                _canvasScaler.referenceResolution = Vector2.Lerp(
                    _canvasScaler.referenceResolution,
                    _targetResolution,
                    Time.deltaTime * _transitionSpeed
                );

                _canvasScaler.matchWidthOrHeight = Mathf.Lerp(
                    _canvasScaler.matchWidthOrHeight,
                    _targetMatch,
                    Time.deltaTime * _transitionSpeed
                );
            }
        }

        private void OnDestroy()
        {
            if (OrientationManager.Instance != null)
            {
                OrientationManager.Instance.OnOrientationChanged -= OnOrientationChanged;
            }
        }

        private void OnOrientationChanged(OrientationManager.DeviceOrientation orientation)
        {
            bool isPortrait = orientation == OrientationManager.DeviceOrientation.Portrait ||
                              orientation == OrientationManager.DeviceOrientation.PortraitUpsideDown;

            ApplySettings(isPortrait);
        }

        private void ApplySettings(bool isPortrait)
        {
            _targetResolution = isPortrait ? _portraitReferenceResolution : _landscapeReferenceResolution;
            _targetMatch = isPortrait ? _portraitMatch : _landscapeMatch;

            if (!_smoothTransition)
            {
                _canvasScaler.referenceResolution = _targetResolution;
                _canvasScaler.matchWidthOrHeight = _targetMatch;
            }
        }

        /// <summary>
        /// 세로 모드 설정
        /// </summary>
        public void SetPortraitSettings(Vector2 resolution, float match)
        {
            _portraitReferenceResolution = resolution;
            _portraitMatch = match;
        }

        /// <summary>
        /// 가로 모드 설정
        /// </summary>
        public void SetLandscapeSettings(Vector2 resolution, float match)
        {
            _landscapeReferenceResolution = resolution;
            _landscapeMatch = match;
        }
    }
}
