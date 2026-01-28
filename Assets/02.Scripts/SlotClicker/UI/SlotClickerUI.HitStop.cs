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
        #region Hit Stop Effect

        /// <summary>
        /// 히트 스톱 효과 실행 (크리티컬/고스트릭 시)
        /// </summary>
        private void PlayHitStop(bool isCritical)
        {
            if (!_enableHitStop) return;

            float duration = _hitStopDuration;
            float timeScale = _hitStopTimeScale;

            if (isCritical)
            {
                duration *= 1.15f;
                timeScale = Mathf.Min(timeScale, 0.08f);
            }

            if (_enableClickStreak && _streakLevel > 0)
            {
                duration *= GetStreakFactor(0.08f);
                float streakT = Mathf.Clamp01(_streakLevel * 0.18f);
                timeScale = Mathf.Lerp(timeScale, timeScale * 0.7f, streakT);
            }

            timeScale = Mathf.Clamp(timeScale, 0.02f, 1f);

            if (_hitStopCoroutine != null)
            {
                StopCoroutine(_hitStopCoroutine);
                Time.timeScale = _originalTimeScale;
            }

            _hitStopCoroutine = StartCoroutine(HitStopCoroutine(duration, timeScale));
        }

        private System.Collections.IEnumerator HitStopCoroutine(float duration, float timeScale)
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            yield return new WaitForSecondsRealtime(duration);

            // 부드러운 복귀
            float elapsed = 0f;
            float recoveryDuration = 0.05f;
            while (elapsed < recoveryDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(timeScale, _originalTimeScale, elapsed / recoveryDuration);
                yield return null;
            }

            Time.timeScale = _originalTimeScale;
            _hitStopCoroutine = null;
        }

        #endregion
    }
}
