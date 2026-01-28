using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

namespace SlotClicker.Core
{
    /// <summary>
    /// WebGL에서 DPI 스케일링 문제를 해결하는 컴포넌트
    /// - Device Pixel Ratio를 감지하여 고해상도 렌더링
    /// - 흐릿한 텍스트/이미지 문제 해결
    /// - TMP 폰트 품질 최적화
    /// </summary>
    public class WebGLResolutionFix : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern float GetDevicePixelRatio();

        [DllImport("__Internal")]
        private static extern void SetCanvasSize(int width, int height);
#endif

        [Header("=== 해상도 설정 ===")]
        [Tooltip("DPI 스케일링 활성화 (Unity WebGL이 자동 처리하므로 기본 비활성화)")]
        [SerializeField] private bool _enableDPIScaling = false;

        [Tooltip("최대 DPI 배율 (성능 고려)")]
        [SerializeField, Range(1f, 3f)] private float _maxDPIRatio = 2f;

        [Tooltip("최소 해상도 (성능 보장)")]
        [SerializeField] private int _minResolution = 720;

        [Tooltip("최대 해상도 (메모리 보호)")]
        [SerializeField] private int _maxResolution = 2160;

        private float _currentDPI = 1f;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Start()
        {
            // DPI 스케일링이 활성화된 경우에만 적용
            if (_enableDPIScaling)
            {
                ApplyResolutionFix();
            }

            // 약간의 딜레이 후 폰트 최적화 (UI 로딩 후)
            Invoke(nameof(DelayedOptimization), 0.5f);
        }

        private void DelayedOptimization()
        {
            OptimizeTMPFonts();
        }

        private void Update()
        {
            // DPI 스케일링이 비활성화되면 Update 스킵
            if (!_enableDPIScaling) return;

            // 화면 크기 변경 감지
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                ApplyResolutionFix();
            }
        }

        private void ApplyResolutionFix()
        {
            if (!_enableDPIScaling) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                _currentDPI = GetDevicePixelRatio();
                _currentDPI = Mathf.Clamp(_currentDPI, 1f, _maxDPIRatio);

                int targetWidth = Mathf.Clamp(
                    Mathf.RoundToInt(Screen.width * _currentDPI),
                    _minResolution,
                    _maxResolution
                );

                int targetHeight = Mathf.Clamp(
                    Mathf.RoundToInt(Screen.height * _currentDPI),
                    _minResolution,
                    _maxResolution
                );

                // 캔버스 해상도 설정
                SetCanvasSize(targetWidth, targetHeight);

                Debug.Log($"[WebGLResolutionFix] Applied DPI fix: ratio={_currentDPI}, resolution={targetWidth}x{targetHeight}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[WebGLResolutionFix] Failed to apply DPI fix: {e.Message}");
            }
#else
            Debug.Log("[WebGLResolutionFix] DPI fix only applies to WebGL builds");
#endif
        }

        /// <summary>
        /// 현재 DPI 배율 반환
        /// </summary>
        public float GetCurrentDPI()
        {
            return _currentDPI;
        }

        /// <summary>
        /// TMP 폰트 품질 최적화
        /// </summary>
        public void OptimizeTMPFonts()
        {
            // 모든 TMP 텍스트 찾아서 품질 최적화
            var tmpTexts = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var tmp in tmpTexts)
            {
                // 픽셀 밀도가 높을수록 글자당 픽셀 수 증가
                tmp.extraPadding = true;

                // 렌더링 모드 최적화 (기본 SDF는 유지)
                if (tmp.fontStyle != FontStyles.Bold)
                {
                    // Normal 스타일의 얇은 텍스트에 약간의 굵기 추가
                    tmp.fontWeight = FontWeight.Medium;
                }
            }

            Debug.Log($"[WebGLResolutionFix] Optimized {tmpTexts.Length} TMP texts");
        }

        /// <summary>
        /// Canvas Scaler 동적 DPI 조정
        /// </summary>
        public void OptimizeCanvasScalers()
        {
            var scalers = FindObjectsOfType<CanvasScaler>(true);
            foreach (var scaler in scalers)
            {
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    // DPI에 따라 동적으로 조정
                    scaler.scaleFactor = _currentDPI;
                }
            }

            Debug.Log($"[WebGLResolutionFix] Optimized {scalers.Length} Canvas Scalers with DPI {_currentDPI}");
        }
    }
}
