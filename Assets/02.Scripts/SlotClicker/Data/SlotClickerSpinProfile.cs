using UnityEngine;

namespace SlotClicker.Data
{
    /// <summary>
    /// SlotClicker 전용 스핀 프로파일.
    /// DOTween 기반 애니메이션에 맞춤화된 파라미터 구성.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotClickerSpinProfile", menuName = "SlotClicker/Spin Profile")]
    public class SlotClickerSpinProfile : ScriptableObject
    {
        [Header("=== 가속 단계 ===")]
        [Tooltip("가속 구간 지속 시간 (초)")]
        [Range(0.1f, 1f)]
        public float accelDuration = 0.3f;

        [Tooltip("가속 시작 속도 (프레임당 대기 시간)")]
        [Range(0.05f, 0.3f)]
        public float accelStartSpeed = 0.15f;

        [Header("=== 고속 회전 ===")]
        [Tooltip("최고 속도 (프레임당 대기 시간, 낮을수록 빠름)")]
        [Range(0.02f, 0.1f)]
        public float maxSpeed = 0.03f;

        [Tooltip("고속 유지 최소 시간 (초)")]
        [Range(0.3f, 2f)]
        public float minSteadyDuration = 0.8f;

        [Header("=== 감속 단계 ===")]
        [Tooltip("감속 단계 수 (최종 심볼 전 변경 횟수)")]
        [Range(1, 6)]
        public int decelerationSteps = 3;

        [Tooltip("감속 커브 (0=시작, 1=종료)")]
        public AnimationCurve decelerationCurve = new AnimationCurve(
            new Keyframe(0f, 0.06f),
            new Keyframe(0.5f, 0.1f),
            new Keyframe(1f, 0.15f)
        );

        [Header("=== 릴 스톱 딜레이 ===")]
        [Tooltip("열별 스톱 딜레이 (초). 왼쪽→오른쪽 순서")]
        public float[] columnStopDelays = new float[] { 0f, 0.08f, 0.16f };

        [Header("=== 착지 연출 ===")]
        [Tooltip("최종 바운스 강도")]
        [Range(0f, 0.3f)]
        public float bounceIntensity = 0.12f;

        [Tooltip("바운스 지속 시간 (초)")]
        [Range(0.1f, 0.5f)]
        public float bounceDuration = 0.3f;

        [Tooltip("바운스 진동 횟수")]
        [Range(1, 8)]
        public int bounceVibrato = 4;

        [Tooltip("바운스 탄성 (0~1)")]
        [Range(0f, 1f)]
        public float bounceElasticity = 0.6f;

        [Header("=== 이펙트 ===")]
        [Tooltip("착지 시 플래시 효과 사용")]
        public bool enableLandingFlash = true;

        [Tooltip("플래시 밝기 배율")]
        [Range(1f, 2f)]
        public float flashIntensity = 1.4f;

        [Tooltip("플래시 인/아웃 시간 (초)")]
        [Range(0.05f, 0.3f)]
        public float flashDuration = 0.1f;

        [Tooltip("회전 중 블러 효과 알파")]
        [Range(0.5f, 1f)]
        public float spinBlurAlpha = 0.85f;

        [Header("=== 슬라이드 애니메이션 ===")]
        [Tooltip("심볼 슬라이드 거리 (픽셀)")]
        [Range(5f, 30f)]
        public float slideDistance = 15f;

        [Tooltip("가속 중 펀치 스케일 강도")]
        [Range(0f, 0.2f)]
        public float accelPunchScale = 0.06f;

        /// <summary>
        /// 열 인덱스에 따른 스톱 딜레이 반환
        /// </summary>
        public float GetColumnStopDelay(int columnIndex)
        {
            if (columnStopDelays == null || columnStopDelays.Length == 0)
                return columnIndex * 0.08f; // 기본값

            int clampedIndex = Mathf.Clamp(columnIndex, 0, columnStopDelays.Length - 1);
            return Mathf.Max(0f, columnStopDelays[clampedIndex]);
        }

        /// <summary>
        /// 감속 단계별 속도 반환 (프레임당 대기 시간)
        /// </summary>
        public float GetDecelerationSpeed(int step, int totalSteps)
        {
            if (decelerationCurve == null || totalSteps <= 0)
                return 0.1f;

            float t = (float)step / Mathf.Max(1, totalSteps - 1);
            return Mathf.Max(0.02f, decelerationCurve.Evaluate(t));
        }

        /// <summary>
        /// 기본 프로파일 생성 (코드에서 동적 생성용)
        /// </summary>
        public static SlotClickerSpinProfile CreateDefault()
        {
            var profile = CreateInstance<SlotClickerSpinProfile>();
            // 기본값은 필드 초기화에서 이미 설정됨
            return profile;
        }

#if UNITY_EDITOR
        [ContextMenu("Reset to Default")]
        private void ResetToDefault()
        {
            accelDuration = 0.3f;
            accelStartSpeed = 0.15f;
            maxSpeed = 0.03f;
            minSteadyDuration = 0.8f;
            decelerationSteps = 3;
            decelerationCurve = new AnimationCurve(
                new Keyframe(0f, 0.06f),
                new Keyframe(0.5f, 0.1f),
                new Keyframe(1f, 0.15f)
            );
            columnStopDelays = new float[] { 0f, 0.08f, 0.16f };
            bounceIntensity = 0.12f;
            bounceDuration = 0.3f;
            bounceVibrato = 4;
            bounceElasticity = 0.6f;
            enableLandingFlash = true;
            flashIntensity = 1.4f;
            flashDuration = 0.1f;
            spinBlurAlpha = 0.85f;
            slideDistance = 15f;
            accelPunchScale = 0.06f;
        }

        [ContextMenu("Apply Fast Spin Preset")]
        private void ApplyFastPreset()
        {
            accelDuration = 0.2f;
            accelStartSpeed = 0.1f;
            maxSpeed = 0.02f;
            minSteadyDuration = 0.5f;
            decelerationSteps = 2;
            columnStopDelays = new float[] { 0f, 0.05f, 0.1f };
            bounceIntensity = 0.08f;
            bounceDuration = 0.2f;
        }

        [ContextMenu("Apply Classic Slot Preset")]
        private void ApplyClassicPreset()
        {
            accelDuration = 0.4f;
            accelStartSpeed = 0.2f;
            maxSpeed = 0.04f;
            minSteadyDuration = 1.2f;
            decelerationSteps = 4;
            columnStopDelays = new float[] { 0f, 0.15f, 0.3f };
            bounceIntensity = 0.15f;
            bounceDuration = 0.35f;
            bounceVibrato = 5;
        }

        [ContextMenu("Apply Dramatic Preset")]
        private void ApplyDramaticPreset()
        {
            accelDuration = 0.35f;
            accelStartSpeed = 0.18f;
            maxSpeed = 0.025f;
            minSteadyDuration = 1f;
            decelerationSteps = 5;
            columnStopDelays = new float[] { 0f, 0.2f, 0.4f };
            bounceIntensity = 0.2f;
            bounceDuration = 0.4f;
            bounceVibrato = 6;
            bounceElasticity = 0.7f;
            flashIntensity = 1.6f;
        }
#endif
    }
}
