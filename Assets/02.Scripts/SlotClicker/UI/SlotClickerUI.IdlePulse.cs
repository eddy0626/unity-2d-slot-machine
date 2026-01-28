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
        #region Idle Pulse

        /// <summary>
        /// 클릭 영역 아이들 펄스 시작
        /// </summary>
        private void StartIdlePulse()
        {
            if (!_enableIdlePulse || _clickArea == null || _isIdlePulsing) return;

            _isIdlePulsing = true;

            // 스케일 펄스
            _idlePulseTween?.Kill();
            _idlePulseTween = _clickArea.transform
                .DOScale(1f + _idlePulseScale, _idlePulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            // 글로우 펄스 (있는 경우)
            if (_clickGlowImage != null)
            {
                _idleGlowPulseTween?.Kill();
                _clickGlowImage.color = new Color(_idlePulseGlowColor.r, _idlePulseGlowColor.g, _idlePulseGlowColor.b, 0f);
                _idleGlowPulseTween = _clickGlowImage
                    .DOFade(_idlePulseGlowColor.a, _idlePulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }

        /// <summary>
        /// 클릭 영역 아이들 펄스 일시 정지 (클릭 시)
        /// </summary>
        private void PauseIdlePulse()
        {
            if (!_isIdlePulsing) return;

            _idlePulseTween?.Pause();
            _idleGlowPulseTween?.Pause();

            // 0.5초 후 재개
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (_isIdlePulsing)
                {
                    bool pulseActive = _idlePulseTween != null && _idlePulseTween.IsActive();
                    bool glowActive = _clickGlowImage == null || (_idleGlowPulseTween != null && _idleGlowPulseTween.IsActive());

                    if (!pulseActive || !glowActive)
                    {
                        // 클릭 피드백에서 DOKill로 끊겼으면 다시 시작
                        _isIdlePulsing = false;
                        StartIdlePulse();
                        return;
                    }

                    _idlePulseTween.Play();
                    _idleGlowPulseTween?.Play();
                }
            }, true);
        }

        /// <summary>
        /// 클릭 영역 아이들 펄스 중지
        /// </summary>
        private void StopIdlePulse()
        {
            _isIdlePulsing = false;
            _idlePulseTween?.Kill();
            _idleGlowPulseTween?.Kill();

            if (_clickArea != null)
            {
                _clickArea.transform.DOKill();
                _clickArea.transform.localScale = Vector3.one;
            }
        }

        #endregion
    }
}
