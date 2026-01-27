using System.Collections.Generic;
using UnityEngine;

namespace SlotClicker.Core
{
    /// <summary>
    /// 모바일 최적화를 위한 유틸리티 클래스
    /// - WaitForSeconds 캐싱 (GC 절감)
    /// - 디바이스 성능 프로파일링
    /// - 품질 설정 자동 조정
    /// </summary>
    public static class MobileOptimizer
    {
        #region WaitForSeconds Cache

        private static readonly Dictionary<float, WaitForSeconds> _waitCache = new Dictionary<float, WaitForSeconds>();
        private static readonly Dictionary<float, WaitForSecondsRealtime> _waitRealtimeCache = new Dictionary<float, WaitForSecondsRealtime>();

        /// <summary>
        /// 캐시된 WaitForSeconds 반환 (GC 할당 방지)
        /// </summary>
        public static WaitForSeconds GetWait(float seconds)
        {
            // 일반적인 값들에 대해 캐시 사용
            float roundedSeconds = Mathf.Round(seconds * 100f) / 100f; // 소수점 2자리로 반올림

            if (!_waitCache.TryGetValue(roundedSeconds, out WaitForSeconds wait))
            {
                wait = new WaitForSeconds(roundedSeconds);
                _waitCache[roundedSeconds] = wait;
            }
            return wait;
        }

        /// <summary>
        /// 캐시된 WaitForSecondsRealtime 반환 (GC 할당 방지)
        /// </summary>
        public static WaitForSecondsRealtime GetWaitRealtime(float seconds)
        {
            float roundedSeconds = Mathf.Round(seconds * 100f) / 100f;

            if (!_waitRealtimeCache.TryGetValue(roundedSeconds, out WaitForSecondsRealtime wait))
            {
                wait = new WaitForSecondsRealtime(roundedSeconds);
                _waitRealtimeCache[roundedSeconds] = wait;
            }
            return wait;
        }

        /// <summary>
        /// 자주 사용되는 대기 시간 미리 캐싱
        /// </summary>
        public static void PrewarmWaitCache()
        {
            float[] commonWaits = { 0.03f, 0.05f, 0.06f, 0.08f, 0.1f, 0.15f, 0.2f, 0.25f, 0.3f, 0.5f, 1f, 2f };
            foreach (float t in commonWaits)
            {
                GetWait(t);
                GetWaitRealtime(t);
            }
            Debug.Log($"[MobileOptimizer] WaitForSeconds cache prewarmed with {commonWaits.Length * 2} entries");
        }

        #endregion

        #region Device Profile

        public enum DeviceProfile
        {
            LowEnd,     // < 2GB RAM, old GPU
            MidRange,   // 2-4GB RAM
            HighEnd     // > 4GB RAM, modern GPU
        }

        private static DeviceProfile? _cachedProfile = null;

        /// <summary>
        /// 현재 디바이스 성능 프로파일 반환
        /// </summary>
        public static DeviceProfile GetDeviceProfile()
        {
            if (_cachedProfile.HasValue)
                return _cachedProfile.Value;

            int systemMemoryMB = SystemInfo.systemMemorySize;
            int graphicsMemoryMB = SystemInfo.graphicsMemorySize;

            if (systemMemoryMB < 2000 || graphicsMemoryMB < 500)
            {
                _cachedProfile = DeviceProfile.LowEnd;
            }
            else if (systemMemoryMB < 4000 || graphicsMemoryMB < 1500)
            {
                _cachedProfile = DeviceProfile.MidRange;
            }
            else
            {
                _cachedProfile = DeviceProfile.HighEnd;
            }

            Debug.Log($"[MobileOptimizer] Device Profile: {_cachedProfile.Value} (RAM: {systemMemoryMB}MB, VRAM: {graphicsMemoryMB}MB)");
            return _cachedProfile.Value;
        }

        #endregion

        #region Quality Settings

        /// <summary>
        /// 디바이스에 맞는 품질 설정 자동 적용
        /// </summary>
        public static void ApplyOptimalQualitySettings()
        {
            DeviceProfile profile = GetDeviceProfile();

            switch (profile)
            {
                case DeviceProfile.LowEnd:
                    ApplyLowEndSettings();
                    break;
                case DeviceProfile.MidRange:
                    ApplyMidRangeSettings();
                    break;
                case DeviceProfile.HighEnd:
                    ApplyHighEndSettings();
                    break;
            }
        }

        private static void ApplyLowEndSettings()
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.globalTextureMipmapLimit = 1; // 텍스처 해상도 50%
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            Debug.Log("[MobileOptimizer] Applied Low-End quality settings (30 FPS)");
        }

        private static void ApplyMidRangeSettings()
        {
            QualitySettings.SetQualityLevel(1, true);
            QualitySettings.globalTextureMipmapLimit = 0; // 풀 해상도
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Debug.Log("[MobileOptimizer] Applied Mid-Range quality settings (60 FPS)");
        }

        private static void ApplyHighEndSettings()
        {
            QualitySettings.SetQualityLevel(2, true);
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Debug.Log("[MobileOptimizer] Applied High-End quality settings (60 FPS)");
        }

        #endregion

        #region Effect Multipliers

        /// <summary>
        /// 디바이스 프로파일에 따른 이펙트 배율 반환
        /// </summary>
        public static float GetEffectMultiplier()
        {
            return GetDeviceProfile() switch
            {
                DeviceProfile.LowEnd => 0.5f,    // 이펙트 50%
                DeviceProfile.MidRange => 0.75f, // 이펙트 75%
                DeviceProfile.HighEnd => 1.0f,   // 이펙트 100%
                _ => 0.75f
            };
        }

        /// <summary>
        /// 파티클 수 조정 (디바이스에 맞게)
        /// </summary>
        public static int AdjustParticleCount(int baseCount)
        {
            return Mathf.CeilToInt(baseCount * GetEffectMultiplier());
        }

        #endregion
    }
}
