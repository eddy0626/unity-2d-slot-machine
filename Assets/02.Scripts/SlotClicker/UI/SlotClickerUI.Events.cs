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
        #region Event Binding

        private void BindEvents()
        {
            // 클릭 이벤트 (autoCreateUI 모드에서만 - SetupExistingUI에서는 이미 바인딩됨)
            if (_autoCreateUI && _clickArea != null)
            {
                _clickArea.onClick.AddListener(OnClickAreaClicked);
            }

            // 게임 매니저 이벤트
            _game.Gold.OnGoldChanged += OnGoldChanged;
            _game.Click.OnClick += OnClickResult;
            _game.Slot.OnSpinStart += OnSlotSpinStart;
            _game.Slot.OnSpinComplete += OnSlotSpinComplete;
            _game.Slot.OnReelStop += OnReelStop;
        }

        private void OnDestroy()
        {
            // DOTween 정리 - 모든 활성 트윈 중지
            _goldCountTween?.Kill();
            _resultTween?.Kill();
            _toastTween?.Kill();

            // 무한 루프 DOTween 애니메이션 정리
            if (_upgradeButton != null) _upgradeButton.transform.DOKill();
            if (_prestigeButton != null) _prestigeButton.transform.DOKill();
            if (_clickArea != null)
            {
                var tableText = _clickArea.GetComponentInChildren<TextMeshProUGUI>();
                if (tableText != null) tableText.transform.DOKill();
            }

            // 릴 애니메이션 정리 (3x3 = 9개)
            if (_reelSymbols != null)
            {
                for (int i = 0; i < _reelSymbols.Length; i++)
                {
                    if (_reelSymbols[i] != null)
                    {
                        _reelSymbols[i].transform.DOKill();
                        _reelSymbols[i].DOKill();
                    }
                    if (i < _reelFrames.Length && _reelFrames[i] != null)
                        _reelFrames[i].DOKill();
                }
            }

            // 스핀 상태 텍스트 정리
            if (_spinStateText != null) _spinStateText.transform.DOKill();

            // 클릭 피드백 정리
            _clickGlowTween?.Kill();
            if (_clickAreaImage != null) _clickAreaImage.DOKill();
            if (_clickGlowImage != null)
            {
                _clickGlowImage.DOKill();
                _clickGlowImage.rectTransform.DOKill();
            }
            _criticalFlashTween?.Kill();
            _shakeTween?.Kill();
            KillMicroShakeAndRestore();
            if (_criticalFlashImage != null)
            {
                _criticalFlashImage.DOKill();
                if (_createdCriticalFlash)
                {
                    Destroy(_criticalFlashImage.gameObject);
                }
                else
                {
                    Color c = _criticalFlashImage.color;
                    _criticalFlashImage.color = new Color(c.r, c.g, c.b, 0f);
                }
                _criticalFlashImage = null;
            }
            if (_clickAudioSource != null)
            {
                if (_createdClickAudioSource)
                {
                    Destroy(_clickAudioSource.gameObject);
                }
                else
                {
                    _clickAudioSource.Stop();
                }
                _clickAudioSource = null;
            }

            // 플로팅 텍스트 풀 정리
            CleanupFloatingTextPool();
            CleanupRipplePool();

            // 향상된 피드백 시스템 정리
            CleanupParticlePool();
            CleanupScreenGlow();

            // 슬롯 승리 피드백 정리
            if (_slotWinFeedback != null)
            {
                _slotWinFeedback.StopAllFeedback();
            }

            // 아이들 펄스 정리
            StopIdlePulse();

            // 히트 스톱 정리
            if (_hitStopCoroutine != null)
            {
                StopCoroutine(_hitStopCoroutine);
                Time.timeScale = _originalTimeScale;
            }
            else if (!Mathf.Approximately(Time.timeScale, _originalTimeScale))
            {
                Time.timeScale = _originalTimeScale;
            }

            // 이벤트 구독 해제 (null-safe)
            if (_game != null)
            {
                if (_game.Gold != null)
                    _game.Gold.OnGoldChanged -= OnGoldChanged;
                if (_game.Click != null)
                    _game.Click.OnClick -= OnClickResult;
                if (_game.Slot != null)
                {
                    _game.Slot.OnSpinStart -= OnSlotSpinStart;
                    _game.Slot.OnSpinComplete -= OnSlotSpinComplete;
                    _game.Slot.OnReelStop -= OnReelStop;
                }
            }
        }

        #endregion
    }
}
