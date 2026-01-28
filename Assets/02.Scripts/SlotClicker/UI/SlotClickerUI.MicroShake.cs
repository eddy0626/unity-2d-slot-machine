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
        #region Micro Shake

        /// <summary>
        /// 진행 중인 미세 쉐이크를 안전하게 중지하고 원위치 복구
        /// </summary>
        private void KillMicroShakeAndRestore()
        {
            if (_microShakeTween != null && _microShakeTween.IsActive())
            {
                _microShakeTween.Kill();
            }

            if (_microShakeHadOriginalPosition && _microShakeTarget != null)
            {
                _microShakeTarget.position = _microShakeOriginalPosition;
            }

            _microShakeHadOriginalPosition = false;
            _microShakeTarget = null;
        }

        /// <summary>
        /// 일반 클릭에도 아주 짧은 미세 쉐이크를 넣어 손맛 강화
        /// </summary>
        private void PlayMicroShake(float strengthMultiplier = 1f)
        {
            if (!_enableMicroShake) return;

            float now = Time.unscaledTime;
            if (now - _lastMicroShakeRealtime < _microShakeCooldown) return;

            // 크리티컬 쉐이크가 재생 중이면 건드리지 않는다
            if (_shakeTween != null && _shakeTween.IsActive() && _shakeTween.IsPlaying())
            {
                return;
            }

            _lastMicroShakeRealtime = now;

            ResolveShakeTarget();
            if (_shakeTarget == null) return;

            KillMicroShakeAndRestore();

            _microShakeTarget = _shakeTarget;
            _microShakeOriginalPosition = _microShakeTarget.position;
            _microShakeHadOriginalPosition = true;

            float strength = _microShakeStrength * Mathf.Max(0.2f, strengthMultiplier);
            if (_microShakeTarget.GetComponent<Camera>() != null)
            {
                strength *= 0.02f;
            }

            _microShakeTween = _microShakeTarget
                .DOShakePosition(
                    _microShakeDuration,
                    strength,
                    _microShakeVibrato,
                    _criticalShakeRandomness,
                    false,
                    true)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (_microShakeHadOriginalPosition && _microShakeTarget != null)
                    {
                        _microShakeTarget.position = _microShakeOriginalPosition;
                    }
                    _microShakeHadOriginalPosition = false;
                    _microShakeTarget = null;
                });
        }

        #endregion
    }
}
