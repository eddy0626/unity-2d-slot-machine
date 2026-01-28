using UnityEngine;
using SlotMachine.Data;

namespace SlotClicker.Data
{
    /// <summary>
    /// SlotMachine.SlotSpinProfile을 SlotClickerSpinProfile로 변환하는 어댑터.
    /// 기존 SlotSpinProfile 에셋을 SlotClicker에서 재사용할 수 있게 해줍니다.
    /// </summary>
    public static class SpinProfileAdapter
    {
        /// <summary>
        /// SlotMachine의 SlotSpinProfile을 SlotClickerSpinProfile로 변환
        /// </summary>
        /// <param name="source">원본 SlotSpinProfile</param>
        /// <returns>변환된 SlotClickerSpinProfile (런타임 인스턴스)</returns>
        public static SlotClickerSpinProfile ConvertFromSlotMachine(SlotSpinProfile source)
        {
            if (source == null)
                return null;

            var converted = ScriptableObject.CreateInstance<SlotClickerSpinProfile>();

            // 속도 파라미터 변환
            // SlotSpinProfile의 maxSpeed는 "cells per second"
            // SlotClickerSpinProfile의 maxSpeed는 "프레임당 대기 시간 (초)"
            // 변환: 1 / maxSpeed 로 근사치 계산 (20 cells/sec -> 0.05s)
            converted.maxSpeed = Mathf.Clamp(1f / Mathf.Max(1f, source.maxSpeed) * 0.5f, 0.02f, 0.1f);
            converted.accelDuration = source.accelTime;
            converted.accelStartSpeed = converted.maxSpeed * 5f; // 최대 속도의 5배로 시작 (느림)
            converted.minSteadyDuration = (source.steadyTimeRange.x + source.steadyTimeRange.y) * 0.5f;

            // 감속 파라미터 변환
            converted.decelerationSteps = Mathf.Clamp(source.tickZoneCells / 2, 2, 6);
            converted.decelerationCurve = ConvertDecelCurve(source.decelCurve, source.tickStepCurve);

            // 스톱 딜레이 변환
            if (source.stopStagger != null && source.stopStagger.Length > 0)
            {
                converted.columnStopDelays = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    converted.columnStopDelays[i] = source.GetStopStagger(i, 0.08f);
                }
            }

            // 바운스/오버슈트 변환
            converted.bounceIntensity = Mathf.Clamp(source.overshootAmount * 0.5f, 0.05f, 0.3f);
            converted.bounceDuration = 0.3f; // 고정값
            converted.bounceVibrato = 4;
            converted.bounceElasticity = 0.6f;

            // 틱 간격을 플래시 속도에 반영
            converted.flashDuration = Mathf.Clamp(source.tickInterval * 2f, 0.05f, 0.3f);

            // 지터를 슬라이드 거리에 반영
            converted.slideDistance = 15f + (source.steadySpeedJitter * 50f);
            converted.accelPunchScale = 0.06f + (source.steadySpeedJitter * 0.2f);

            return converted;
        }

        /// <summary>
        /// SlotSpinProfile의 decelCurve와 tickStepCurve를 조합하여
        /// SlotClickerSpinProfile의 decelerationCurve로 변환
        /// </summary>
        private static AnimationCurve ConvertDecelCurve(AnimationCurve decelCurve, AnimationCurve tickStepCurve)
        {
            var result = new AnimationCurve();

            // 3~5개의 키프레임으로 샘플링
            int sampleCount = 4;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);

                // decelCurve에서 속도 팩터 (1->0)
                float speedFactor = decelCurve != null ? decelCurve.Evaluate(t) : (1f - t);

                // tickStepCurve에서 틱 간격 팩터
                float tickFactor = tickStepCurve != null ? tickStepCurve.Evaluate(t) : (0.7f + t * 1.2f);

                // 조합하여 대기 시간 계산 (0.04 ~ 0.2 범위로 매핑)
                float waitTime = 0.04f + (1f - speedFactor) * tickFactor * 0.08f;
                waitTime = Mathf.Clamp(waitTime, 0.04f, 0.2f);

                result.AddKey(new Keyframe(t, waitTime));
            }

            return result;
        }

        /// <summary>
        /// 변환된 프로파일의 정보를 로그로 출력 (디버그용)
        /// </summary>
        public static void LogConversionResult(SlotSpinProfile source, SlotClickerSpinProfile converted)
        {
            Debug.Log($"[SpinProfileAdapter] Converted '{source.name}':\n" +
                      $"  Original: maxSpeed={source.maxSpeed} cells/s, accel={source.accelTime}s, overshoot={source.overshootAmount}\n" +
                      $"  Converted: maxSpeed={converted.maxSpeed}s, accel={converted.accelDuration}s, bounce={converted.bounceIntensity}");
        }
    }
}
