using System;
using UnityEngine;
using UnityEngine.Events;

namespace SlotClicker.Core
{
    /// <summary>
    /// 자이로/가속도계 센서를 이용한 화면 방향 관리 시스템
    /// - 자연스러운 Portrait ↔ Landscape 전환
    /// - 회전 애니메이션 및 보간 처리
    /// - 회전 잠금 및 감도 조절 기능
    /// </summary>
    public class OrientationManager : MonoBehaviour
    {
        public static OrientationManager Instance { get; private set; }

        #region Enums

        /// <summary>
        /// 화면 방향 상태
        /// </summary>
        public enum DeviceOrientation
        {
            Portrait,           // 세로 (기본)
            PortraitUpsideDown, // 세로 뒤집힘
            LandscapeLeft,      // 가로 왼쪽 (홈버튼 오른쪽)
            LandscapeRight      // 가로 오른쪽 (홈버튼 왼쪽)
        }

        /// <summary>
        /// 회전 잠금 모드
        /// </summary>
        public enum LockMode
        {
            Auto,           // 자동 회전
            LockPortrait,   // 세로 고정
            LockLandscape,  // 가로 고정
            LockCurrent     // 현재 방향 고정
        }

        #endregion

        #region Settings

        [Header("=== 기본 설정 ===")]
        [Tooltip("자동 회전 기능 ON/OFF")]
        [SerializeField] private bool _autoRotationEnabled = true;

        [Tooltip("회전 잠금 모드")]
        [SerializeField] private LockMode _lockMode = LockMode.Auto;

        [Header("=== 센서 설정 ===")]
        [Tooltip("센서 읽기 간격 (초) - 배터리 절약용")]
        [SerializeField, Range(0.05f, 0.5f)] private float _sensorReadInterval = 0.1f;

        [Tooltip("회전 감지 임계값 (0~1) - 높을수록 더 많이 기울여야 전환")]
        [SerializeField, Range(0.3f, 0.8f)] private float _rotationThreshold = 0.5f;

        [Tooltip("회전 히스테리시스 (급격한 전환 방지)")]
        [SerializeField, Range(0.05f, 0.2f)] private float _hysteresis = 0.1f;

        [Header("=== 전환 애니메이션 ===")]
        [Tooltip("방향 전환 시 애니메이션 시간")]
        [SerializeField, Range(0.1f, 1f)] private float _transitionDuration = 0.3f;

        [Tooltip("전환 애니메이션 커브")]
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("전환 전 대기 시간 (실수 전환 방지)")]
        [SerializeField, Range(0.1f, 1f)] private float _transitionDelay = 0.3f;

        [Header("=== 디버그 ===")]
        [SerializeField] private bool _showDebugInfo = false;

        #endregion

        #region Events

        /// <summary>
        /// 화면 방향 변경 시 발생
        /// </summary>
        public event Action<DeviceOrientation> OnOrientationChanged;

        /// <summary>
        /// 화면 방향 전환 시작 시 발생
        /// </summary>
        public event Action<DeviceOrientation, DeviceOrientation> OnOrientationTransitionStart;

        /// <summary>
        /// 화면 방향 전환 완료 시 발생
        /// </summary>
        public event Action<DeviceOrientation> OnOrientationTransitionComplete;

        /// <summary>
        /// Unity 이벤트 (Inspector에서 바인딩 가능)
        /// </summary>
        [Header("=== Unity Events ===")]
        public UnityEvent<DeviceOrientation> OnOrientationChangedEvent;

        #endregion

        #region Properties

        /// <summary>
        /// 현재 화면 방향
        /// </summary>
        public DeviceOrientation CurrentOrientation { get; private set; } = DeviceOrientation.Portrait;

        /// <summary>
        /// 이전 화면 방향
        /// </summary>
        public DeviceOrientation PreviousOrientation { get; private set; } = DeviceOrientation.Portrait;

        /// <summary>
        /// 현재 방향이 세로인지
        /// </summary>
        public bool IsPortrait => CurrentOrientation == DeviceOrientation.Portrait ||
                                  CurrentOrientation == DeviceOrientation.PortraitUpsideDown;

        /// <summary>
        /// 현재 방향이 가로인지
        /// </summary>
        public bool IsLandscape => CurrentOrientation == DeviceOrientation.LandscapeLeft ||
                                   CurrentOrientation == DeviceOrientation.LandscapeRight;

        /// <summary>
        /// 전환 중인지
        /// </summary>
        public bool IsTransitioning { get; private set; } = false;

        /// <summary>
        /// 자동 회전 활성화 여부
        /// </summary>
        public bool AutoRotationEnabled
        {
            get => _autoRotationEnabled;
            set => _autoRotationEnabled = value;
        }

        /// <summary>
        /// 회전 감도 (0~1)
        /// </summary>
        public float RotationSensitivity
        {
            get => 1f - _rotationThreshold;
            set => _rotationThreshold = Mathf.Clamp01(1f - value);
        }

        /// <summary>
        /// 현재 가속도계 값 (Raw)
        /// </summary>
        public Vector3 CurrentAcceleration { get; private set; }

        /// <summary>
        /// 보간된 가속도계 값 (Smoothed)
        /// </summary>
        public Vector3 SmoothedAcceleration { get; private set; }

        #endregion

        #region Private Fields

        private float _lastSensorReadTime;
        private float _pendingTransitionTime;
        private DeviceOrientation _pendingOrientation;
        private bool _hasPendingTransition;

        // 보간용
        private Vector3 _accelerationVelocity;
        private const float SMOOTHING_TIME = 0.1f;

        // 전환 애니메이션
        private float _transitionProgress;
        private Quaternion _startRotation;
        private Quaternion _targetRotation;

        // 캐시된 화면 크기
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        // ★ 모바일 최적화: 화면 크기 체크 간격 (매 프레임 X)
        private float _lastScreenCheckTime = 0f;
        private const float SCREEN_CHECK_INTERVAL = 1f; // 1초마다 체크

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSensor();
        }

        private void Start()
        {
            // 초기 방향 설정
            DetectInitialOrientation();

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void Update()
        {
            if (!_autoRotationEnabled || _lockMode != LockMode.Auto)
                return;

            // 센서 읽기 최적화 (매 프레임 X)
            if (Time.time - _lastSensorReadTime >= _sensorReadInterval)
            {
                ReadSensor();
                _lastSensorReadTime = Time.time;
            }

            // 전환 대기 중 처리
            if (_hasPendingTransition)
            {
                ProcessPendingTransition();
            }

            // 전환 애니메이션 처리
            if (IsTransitioning)
            {
                ProcessTransitionAnimation();
            }

            // ★ 모바일 최적화: 화면 크기 변경 감지 (1초마다, 매 프레임 X)
            if (Time.time - _lastScreenCheckTime >= SCREEN_CHECK_INTERVAL)
            {
                _lastScreenCheckTime = Time.time;
                CheckScreenSizeChange();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Sensor Reading

        /// <summary>
        /// 센서 초기화
        /// </summary>
        private void InitializeSensor()
        {
            // 자이로 활성화 (지원하는 기기에서)
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                Debug.Log("[OrientationManager] Gyroscope enabled");
            }

            // 가속도계는 별도 초기화 불필요 (Input.acceleration 항상 사용 가능)
            Debug.Log("[OrientationManager] Sensor initialized");
        }

        /// <summary>
        /// 센서 값 읽기 및 방향 판단
        /// </summary>
        private void ReadSensor()
        {
            // 가속도계 값 읽기
            CurrentAcceleration = Input.acceleration;

            // 부드러운 보간 (급격한 변화 방지)
            SmoothedAcceleration = Vector3.SmoothDamp(
                SmoothedAcceleration,
                CurrentAcceleration,
                ref _accelerationVelocity,
                SMOOTHING_TIME
            );

            // 새로운 방향 감지
            DeviceOrientation detectedOrientation = DetectOrientationFromAcceleration(SmoothedAcceleration);

            // 현재 방향과 다르고, 전환 중이 아닐 때만 처리
            if (detectedOrientation != CurrentOrientation && !IsTransitioning)
            {
                // 히스테리시스 체크 (같은 방향이 지속되는지 확인)
                if (!_hasPendingTransition || _pendingOrientation != detectedOrientation)
                {
                    // 새로운 전환 시작 대기
                    _hasPendingTransition = true;
                    _pendingOrientation = detectedOrientation;
                    _pendingTransitionTime = Time.time;
                }
            }
            else if (detectedOrientation == CurrentOrientation)
            {
                // 원래 방향으로 돌아가면 대기 취소
                _hasPendingTransition = false;
            }
        }

        /// <summary>
        /// 가속도계 값으로부터 방향 감지
        /// </summary>
        private DeviceOrientation DetectOrientationFromAcceleration(Vector3 acceleration)
        {
            float absX = Mathf.Abs(acceleration.x);
            float absY = Mathf.Abs(acceleration.y);

            // 현재 방향에 따른 임계값 조정 (히스테리시스)
            float threshold = _rotationThreshold;
            float adjustedThreshold = IsPortrait
                ? threshold - _hysteresis  // Portrait에서는 약간 더 민감하게 Landscape로
                : threshold + _hysteresis; // Landscape에서는 약간 덜 민감하게 Portrait로

            // 세로 방향 (Y축이 더 강함)
            if (absY > absX && absY > adjustedThreshold)
            {
                return acceleration.y > 0
                    ? DeviceOrientation.PortraitUpsideDown
                    : DeviceOrientation.Portrait;
            }
            // 가로 방향 (X축이 더 강함)
            else if (absX > absY && absX > (1f - adjustedThreshold))
            {
                return acceleration.x > 0
                    ? DeviceOrientation.LandscapeRight
                    : DeviceOrientation.LandscapeLeft;
            }

            // 변화 없음 (현재 방향 유지)
            return CurrentOrientation;
        }

        /// <summary>
        /// 초기 방향 감지
        /// </summary>
        private void DetectInitialOrientation()
        {
            // Unity의 Screen.orientation 사용하여 초기 방향 설정
            switch (Screen.orientation)
            {
                case ScreenOrientation.Portrait:
                    CurrentOrientation = DeviceOrientation.Portrait;
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                    CurrentOrientation = DeviceOrientation.PortraitUpsideDown;
                    break;
                case ScreenOrientation.LandscapeLeft:
                    CurrentOrientation = DeviceOrientation.LandscapeLeft;
                    break;
                case ScreenOrientation.LandscapeRight:
                    CurrentOrientation = DeviceOrientation.LandscapeRight;
                    break;
                default:
                    // 화면 비율로 판단
                    CurrentOrientation = Screen.width > Screen.height
                        ? DeviceOrientation.LandscapeLeft
                        : DeviceOrientation.Portrait;
                    break;
            }

            PreviousOrientation = CurrentOrientation;
            SmoothedAcceleration = Input.acceleration;

            Debug.Log($"[OrientationManager] Initial orientation: {CurrentOrientation}");
        }

        #endregion

        #region Transition Processing

        /// <summary>
        /// 대기 중인 전환 처리
        /// </summary>
        private void ProcessPendingTransition()
        {
            // 대기 시간 확인
            if (Time.time - _pendingTransitionTime >= _transitionDelay)
            {
                // 전환 시작
                StartOrientationTransition(_pendingOrientation);
                _hasPendingTransition = false;
            }
        }

        /// <summary>
        /// 방향 전환 시작
        /// </summary>
        private void StartOrientationTransition(DeviceOrientation newOrientation)
        {
            if (IsTransitioning) return;

            PreviousOrientation = CurrentOrientation;
            IsTransitioning = true;
            _transitionProgress = 0f;

            // 전환 시작 이벤트
            OnOrientationTransitionStart?.Invoke(PreviousOrientation, newOrientation);

            Debug.Log($"[OrientationManager] Transition started: {PreviousOrientation} -> {newOrientation}");

            // 실제 화면 회전 적용
            ApplyScreenOrientation(newOrientation);

            // 현재 방향 업데이트
            CurrentOrientation = newOrientation;

            // 방향 변경 이벤트
            OnOrientationChanged?.Invoke(CurrentOrientation);
            OnOrientationChangedEvent?.Invoke(CurrentOrientation);
        }

        /// <summary>
        /// 전환 애니메이션 처리
        /// </summary>
        private void ProcessTransitionAnimation()
        {
            _transitionProgress += Time.deltaTime / _transitionDuration;

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                IsTransitioning = false;

                // 전환 완료 이벤트
                OnOrientationTransitionComplete?.Invoke(CurrentOrientation);

                Debug.Log($"[OrientationManager] Transition complete: {CurrentOrientation}");
            }
        }

        /// <summary>
        /// 화면 방향 적용
        /// </summary>
        private void ApplyScreenOrientation(DeviceOrientation orientation)
        {
            switch (orientation)
            {
                case DeviceOrientation.Portrait:
                    Screen.orientation = ScreenOrientation.Portrait;
                    break;
                case DeviceOrientation.PortraitUpsideDown:
                    Screen.orientation = ScreenOrientation.PortraitUpsideDown;
                    break;
                case DeviceOrientation.LandscapeLeft:
                    Screen.orientation = ScreenOrientation.LandscapeLeft;
                    break;
                case DeviceOrientation.LandscapeRight:
                    Screen.orientation = ScreenOrientation.LandscapeRight;
                    break;
            }
        }

        /// <summary>
        /// 화면 크기 변경 감지 (외부 회전)
        /// </summary>
        private void CheckScreenSizeChange()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;

                // 외부에서 회전이 발생한 경우 상태 동기화
                DetectInitialOrientation();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 특정 방향으로 강제 전환
        /// </summary>
        public void SetOrientation(DeviceOrientation orientation)
        {
            if (CurrentOrientation == orientation) return;

            StartOrientationTransition(orientation);
        }

        /// <summary>
        /// 회전 잠금 설정
        /// </summary>
        public void SetLockMode(LockMode mode)
        {
            _lockMode = mode;

            switch (mode)
            {
                case LockMode.Auto:
                    Screen.autorotateToPortrait = true;
                    Screen.autorotateToPortraitUpsideDown = true;
                    Screen.autorotateToLandscapeLeft = true;
                    Screen.autorotateToLandscapeRight = true;
                    Screen.orientation = ScreenOrientation.AutoRotation;
                    break;

                case LockMode.LockPortrait:
                    Screen.autorotateToPortrait = true;
                    Screen.autorotateToPortraitUpsideDown = true;
                    Screen.autorotateToLandscapeLeft = false;
                    Screen.autorotateToLandscapeRight = false;
                    if (!IsPortrait)
                    {
                        SetOrientation(DeviceOrientation.Portrait);
                    }
                    break;

                case LockMode.LockLandscape:
                    Screen.autorotateToPortrait = false;
                    Screen.autorotateToPortraitUpsideDown = false;
                    Screen.autorotateToLandscapeLeft = true;
                    Screen.autorotateToLandscapeRight = true;
                    if (!IsLandscape)
                    {
                        SetOrientation(DeviceOrientation.LandscapeLeft);
                    }
                    break;

                case LockMode.LockCurrent:
                    Screen.autorotateToPortrait = false;
                    Screen.autorotateToPortraitUpsideDown = false;
                    Screen.autorotateToLandscapeLeft = false;
                    Screen.autorotateToLandscapeRight = false;
                    break;
            }

            Debug.Log($"[OrientationManager] Lock mode set to: {mode}");
        }

        /// <summary>
        /// 자동 회전 토글
        /// </summary>
        public void ToggleAutoRotation()
        {
            AutoRotationEnabled = !AutoRotationEnabled;

            if (!AutoRotationEnabled)
            {
                SetLockMode(LockMode.LockCurrent);
            }
            else
            {
                SetLockMode(LockMode.Auto);
            }
        }

        /// <summary>
        /// 회전 감도 설정 (0~1, 높을수록 민감)
        /// </summary>
        public void SetSensitivity(float sensitivity)
        {
            RotationSensitivity = Mathf.Clamp01(sensitivity);
        }

        /// <summary>
        /// 전환 애니메이션 시간 설정
        /// </summary>
        public void SetTransitionDuration(float duration)
        {
            _transitionDuration = Mathf.Clamp(duration, 0.1f, 1f);
        }

        /// <summary>
        /// 센서 읽기 간격 설정 (배터리 절약)
        /// </summary>
        public void SetSensorReadInterval(float interval)
        {
            _sensorReadInterval = Mathf.Clamp(interval, 0.05f, 0.5f);
        }

        /// <summary>
        /// 현재 전환 진행도 (0~1)
        /// </summary>
        public float GetTransitionProgress()
        {
            return _transitionCurve.Evaluate(_transitionProgress);
        }

        /// <summary>
        /// 설정 저장 (PlayerPrefs)
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetInt("OrientationManager_AutoRotation", _autoRotationEnabled ? 1 : 0);
            PlayerPrefs.SetInt("OrientationManager_LockMode", (int)_lockMode);
            PlayerPrefs.SetFloat("OrientationManager_Sensitivity", RotationSensitivity);
            PlayerPrefs.Save();

            Debug.Log("[OrientationManager] Settings saved");
        }

        /// <summary>
        /// 설정 로드 (PlayerPrefs)
        /// </summary>
        public void LoadSettings()
        {
            _autoRotationEnabled = PlayerPrefs.GetInt("OrientationManager_AutoRotation", 1) == 1;
            _lockMode = (LockMode)PlayerPrefs.GetInt("OrientationManager_LockMode", 0);
            RotationSensitivity = PlayerPrefs.GetFloat("OrientationManager_Sensitivity", 0.5f);

            // 설정 적용
            SetLockMode(_lockMode);

            Debug.Log("[OrientationManager] Settings loaded");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 방향에 따른 화면 회전 각도 반환
        /// </summary>
        public static float GetRotationAngle(DeviceOrientation orientation)
        {
            switch (orientation)
            {
                case DeviceOrientation.Portrait: return 0f;
                case DeviceOrientation.LandscapeLeft: return 90f;
                case DeviceOrientation.PortraitUpsideDown: return 180f;
                case DeviceOrientation.LandscapeRight: return 270f;
                default: return 0f;
            }
        }

        /// <summary>
        /// 방향에 따른 화면 크기 비율 반환 (width/height)
        /// </summary>
        public static float GetAspectRatio(DeviceOrientation orientation)
        {
            bool isLandscape = orientation == DeviceOrientation.LandscapeLeft ||
                               orientation == DeviceOrientation.LandscapeRight;

            float width = isLandscape ? Mathf.Max(Screen.width, Screen.height) : Mathf.Min(Screen.width, Screen.height);
            float height = isLandscape ? Mathf.Min(Screen.width, Screen.height) : Mathf.Max(Screen.width, Screen.height);

            return width / height;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!_showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Current Orientation: {CurrentOrientation}");
            GUILayout.Label($"Is Portrait: {IsPortrait}");
            GUILayout.Label($"Is Landscape: {IsLandscape}");
            GUILayout.Label($"Is Transitioning: {IsTransitioning}");
            GUILayout.Label($"Auto Rotation: {_autoRotationEnabled}");
            GUILayout.Label($"Lock Mode: {_lockMode}");
            GUILayout.Label($"Acceleration: {SmoothedAcceleration:F2}");
            GUILayout.Label($"Screen: {Screen.width}x{Screen.height}");
            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
