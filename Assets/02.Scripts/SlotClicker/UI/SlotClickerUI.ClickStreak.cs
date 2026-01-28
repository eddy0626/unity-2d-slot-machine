using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SlotClicker.Core;
using SlotClicker.Data;

namespace SlotClicker.UI
{
    public partial class SlotClickerUI : MonoBehaviour
    {
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
    }
}
