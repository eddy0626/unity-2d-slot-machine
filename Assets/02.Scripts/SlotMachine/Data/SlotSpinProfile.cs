using UnityEngine;

namespace SlotMachine.Data
{
    /// <summary>
    /// 릴 스핀 감각을 튜닝하기 위한 프로파일.
    /// 속도 단위는 "cells per second"(심볼 칸/초) 기준이다.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotSpinProfile", menuName = "SlotMachine/Spin Profile")]
    public class SlotSpinProfile : ScriptableObject
    {
        [Header("Speed")]
        [Tooltip("최고 속도 (칸/초)")]
        public float maxSpeed = 20f;

        [Tooltip("모터가 붙는 가속 시간 (초)")]
        public float accelTime = 0.16f;

        [Tooltip("고속 유지 시간 범위 (초)")]
        public Vector2 steadyTimeRange = new Vector2(1.2f, 1.8f);

        [Header("Deceleration")]
        [Tooltip("감속 구간 속도 커브. 0에서 1로 갈 때 1→0 형태를 권장")]
        public AnimationCurve decelCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -1.2f),
            new Keyframe(1f, 0f, -0.2f, 0f)
        );

        [Tooltip("마지막 틱틱 구간 셀 수")]
        public int tickZoneCells = 8;

        [Tooltip("틱 구간에서 간격 배율을 조절하는 커브")]
        public AnimationCurve tickStepCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f),
            new Keyframe(1f, 1.9f)
        );

        [Tooltip("틱 간격의 기본값(초)")]
        public float tickInterval = 0.055f;

        [Tooltip("정지 직전 오버슈트 양(칸 단위, 0~0.49 권장)")]
        public float overshootAmount = 0.25f;

        [Header("Machine Feel")]
        [Range(0f, 0.2f)]
        [Tooltip("고속 유지 중 속도 요동 강도")]
        public float steadySpeedJitter = 0.06f;

        [Tooltip("속도 요동 주파수")]
        public float jitterFrequency = 8f;

        [Header("Stop Travel")]
        [Tooltip("스톱 요청 이후 최소 이동 셀 수")]
        public int minStopCells = 18;

        [Tooltip("스톱 이동 셀 수에 더해지는 랜덤 추가치")]
        public int stopCellsJitter = 6;

        [Tooltip("틱 구간 전에 확보할 여유 셀 수")]
        public int preTickBufferCells = 4;

        [Header("Reel Stop Stagger")]
        [Tooltip("릴별 스톱 딜레이. 인덱스가 부족하면 마지막 값을 재사용")]
        public float[] stopStagger = new float[] { 0f, 0.2f, 0.25f };

        public float GetStopStagger(int reelIndex, float fallback)
        {
            if (stopStagger == null || stopStagger.Length == 0)
                return fallback;

            int clampedIndex = Mathf.Clamp(reelIndex, 0, stopStagger.Length - 1);
            return Mathf.Max(0f, stopStagger[clampedIndex]);
        }
    }
}
