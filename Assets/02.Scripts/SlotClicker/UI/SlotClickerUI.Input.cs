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
        #region Input Handlers

        private void OnClickAreaClicked()
        {
            if (_game == null || _game.Click == null) return;

            // ★ 모바일 최적화: 클릭 디바운싱 (고주사율 디바이스에서 중복 클릭 방지)
            if (Time.realtimeSinceStartup - _lastClickTime < CLICK_MIN_INTERVAL)
                return;
            _lastClickTime = Time.realtimeSinceStartup;

            Vector2 screenPos = GetPointerScreenPosition();
            if (screenPos == Vector2.zero && _clickArea != null)
            {
                Camera cam = _mainCanvas != null && _mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? _mainCanvas.worldCamera
                    : null;
                screenPos = RectTransformUtility.WorldToScreenPoint(cam, _clickArea.transform.position);
            }

            Vector2 localPos = ScreenToCanvasPosition(screenPos);
            _game.Click.ProcessClick(localPos);
        }

        private void OnClickResult(ClickResult result)
        {
            // WebGL 이펙트 쓰로틀링 - 프레임당 이펙트 수 제한
            float currentTime = Time.realtimeSinceStartup;
            bool shouldThrottle = _isWebGL && (currentTime - _lastEffectTime) < EFFECT_THROTTLE_INTERVAL;

            // 아이들 펄스 일시 정지
            PauseIdlePulse();

            // 스트릭 갱신 (사운드/이펙트 강도에 영향)
            UpdateClickStreak(result.Position, result.IsCritical);

            // 필수 피드백 (항상 실행)
            PlayClickSound(result.IsCritical);
            SpawnFloatingText(result.Position, result.GoldEarned, result.IsCritical);

            // 선택적 이펙트 (쓰로틀링 적용)
            if (!shouldThrottle)
            {
                _lastEffectTime = currentTime;

                SpawnClickRipple(result.Position, result.IsCritical);
                if (!_streakBurstTriggeredThisClick)
                {
                    SpawnStreakEchoRipples(result.Position, result.IsCritical, burstMode: false);
                }
                PlayClickAreaFeedback(result.IsCritical);

                // 파티클 이펙트 (WebGL에서 더 제한적으로)
                if (!_isWebGL || _frameEffectCount < MAX_EFFECTS_PER_FRAME)
                {
                    SpawnClickParticles(result.Position, result.IsCritical);
                    _frameEffectCount++;
                }
            }

            // 일반 클릭에도 아주 약한 화면 쉐이크 추가 (WebGL에서는 스킵)
            if (!result.IsCritical && !_isWebGL)
            {
                PlayMicroShake();
            }

            // 크리티컬 또는 고스트릭 구간에서는 히트스톱 허용
            bool allowHitStop = result.IsCritical ||
                (_hitStopOnStreakBurst && _enableClickStreak && _streakBurstTriggeredThisClick && _streakLevel >= _streakHitStopMinLevel);

            // WebGL에서는 히트스톱 비활성화 (입력 지연 방지)
            if (allowHitStop && !_isWebGL)
            {
                PlayHitStop(result.IsCritical);
            }

            if (result.IsCritical)
            {
                PlayCriticalFlash();
                PlayCriticalShake();
                PlayScreenGlow(false); // 화면 테두리 글로우
                UIFeedback.TriggerHaptic(UIFeedback.HapticType.Medium);
            }
        }

        private void SpawnFloatingText(Vector2 position, double amount, bool isCritical)
        {
            // 오브젝트 풀에서 가져오기 (Instantiate 대신)
            GameObject floatText = GetFloatingTextFromPool();
            floatText.SetActive(true);
            floatText.transform.SetAsLastSibling();

            RectTransform rect = floatText.GetComponent<RectTransform>();
            rect.DOKill();
            floatText.transform.DOKill();

            Vector2 startPos = position + new Vector2(
                UnityEngine.Random.Range(-5.605f, 5.605f),
                UnityEngine.Random.Range(-2.402f, 4.804f));

            rect.anchoredPosition = startPos;
            rect.localScale = Vector3.one * (isCritical ? 0.95f : 0.85f);
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-6f, 6f));

            // 자식에서 텍스트 컴포넌트 찾기
            Transform textChild = floatText.transform.Find("Text");
            TextMeshProUGUI tmp = textChild != null
                ? textChild.GetComponent<TextMeshProUGUI>()
                : floatText.GetComponent<TextMeshProUGUI>();

            tmp.DOKill();
            tmp.fontSize = isCritical ? 20.818f : 16.014f;
            tmp.alpha = 1f; // 알파 초기화 (풀에서 재사용 시 필요)

            // 코인 아이콘 알파 초기화 및 크기 조정
            Transform coinChild = floatText.transform.Find("CoinIcon");
            Image coinImage = coinChild != null ? coinChild.GetComponent<Image>() : null;
            if (coinImage != null)
            {
                coinImage.DOKill();
                Color coinColor = coinImage.color;
                coinColor.a = 1f;
                coinImage.color = coinColor;

                // 크리티컬일 때 코인도 크게
                RectTransform coinRect = coinChild.GetComponent<RectTransform>();
                if (coinRect != null)
                {
                    float coinSize = isCritical ? 20.818f : 16.014f;
                    coinRect.sizeDelta = new Vector2(coinSize, coinSize);
                    LayoutElement coinLayout = coinChild.GetComponent<LayoutElement>();
                    if (coinLayout != null)
                    {
                        coinLayout.preferredWidth = coinSize;
                        coinLayout.preferredHeight = coinSize;
                    }
                }
            }

            // 애니메이션 (완료 시 풀에 반환)
            float travelY = isCritical ? 74.064f : 54.047f;
            float horizontalDrift = UnityEngine.Random.Range(-16.014f, 16.014f);
            float duration = isCritical ? 1.0f : 0.85f;
            Vector2 targetPos = startPos + new Vector2(horizontalDrift, travelY);

            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOScale(isCritical ? 1.25f : 1.08f, 0.12f).SetEase(Ease.OutQuad));
            seq.Append(rect.DOScale(1f, isCritical ? 0.26f : 0.18f).SetEase(Ease.OutBack));

            seq.Join(rect.DOAnchorPos(targetPos, duration).SetEase(isCritical ? Ease.OutCubic : Ease.OutQuad));
            seq.Join(tmp.DOFade(0f, duration * 0.9f).SetDelay(duration * 0.1f).SetEase(Ease.OutQuad));

            // 코인 이미지도 함께 페이드 아웃
            if (coinImage != null)
            {
                seq.Join(coinImage.DOFade(0f, duration * 0.9f).SetDelay(duration * 0.1f).SetEase(Ease.OutQuad));
            }

            // ★ 크리티컬: 카운트업 애니메이션 + 색상 펄스 효과
            if (isCritical)
            {
                // 0에서 최종 값까지 카운트업
                double countupValue = 0;
                float countupDuration = 0.25f;
                DOTween.To(() => countupValue, x => {
                    countupValue = x;
                    tmp.text = $"+{GoldManager.FormatNumber(countupValue)}";
                }, amount, countupDuration).SetEase(Ease.OutQuad);

                // 색상 펄스: 흰색 → 크리티컬 색상 → 약간 밝게
                tmp.color = Color.white;
                seq.Join(tmp.DOColor(_criticalColor, 0.15f).SetEase(Ease.OutQuad));
                seq.Join(DOVirtual.DelayedCall(0.15f, () => {
                    if (tmp != null)
                    {
                        tmp.DOColor(_criticalColor * 1.2f, 0.1f)
                            .OnComplete(() => tmp.DOColor(_criticalColor, 0.1f));
                    }
                }));

                // 회전 펀치 효과
                seq.Join(rect.DOPunchRotation(new Vector3(0f, 0f, 16f), 0.45f, 12, 0.85f));

                // 스케일 펄스 (2회)
                seq.Join(DOVirtual.DelayedCall(0.2f, () => {
                    if (rect != null)
                    {
                        rect.DOPunchScale(Vector3.one * 0.08f, 0.15f, 3, 0.5f);
                    }
                }));
            }
            else
            {
                // 일반 클릭: 바로 값 표시
                tmp.text = $"+{GoldManager.FormatNumber(amount)}";
                tmp.color = Color.yellow;
            }

            seq.OnComplete(() => ReturnFloatingTextToPool(floatText));
        }

        private Vector2 GetPointerScreenPosition()
        {
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            return Vector2.zero;
        }

        private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            if (_mainCanvas == null)
                return screenPosition;

            RectTransform canvasRect = _mainCanvas.transform as RectTransform;
            Camera cam = _mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out Vector2 localPoint))
                return localPoint;

            return screenPosition;
        }

        private void ShowToast(string message, Color color, float duration = 1.6f)
        {
            if (_toastText == null || _toastGroup == null) return;

            _toastText.text = message;
            _toastText.color = color;

            _toastGroup.DOKill();
            _toastGroup.alpha = 1f;

            _toastTween?.Kill();
            _toastTween = _toastGroup.DOFade(0f, 0.4f).SetDelay(duration);
        }

        private void SetBetPercentage(float percentage)
        {
            _currentBetPercentage = percentage;
            UpdateBetAmount();

            // 버튼 하이라이트
            bool hasCustomSprites = _allButtonSprites != null && _allButtonSprites.Length > 0;

            for (int i = 0; i < _betButtons.Length; i++)
            {
                if (_betButtons[i] == null) continue;

                Image img = _betButtons[i].GetComponent<Image>();
                if (img == null) continue;

                float[] values = { 0.1f, 0.3f, 0.5f, 1f };
                bool isSelected = Mathf.Approximately(values[i], percentage);

                // 커스텀 스프라이트가 있으면 밝기로 하이라이트, 없으면 색상 변경
                if (hasCustomSprites)
                {
                    img.color = isSelected ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
                else
                {
                    img.color = isSelected
                        ? new Color(0.5f, 0.4f, 0.8f)
                        : new Color(0.3f, 0.3f, 0.5f);
                }
            }
        }

        private void OnSpinClicked()
        {
            if (_game == null || _game.Slot == null) return;

            if (_game.Slot.IsSpinning)
            {
                ShowToast("Spinning...", new Color(1f, 0.85f, 0.4f));
                return;
            }

            if (_currentBetAmount <= 0 || !_game.Gold.CanAfford(_currentBetAmount))
            {
                ShowToast("Not enough gold!", Color.red);
                _goldText?.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
                return;
            }

            bool started = _game.Slot.TrySpin(_currentBetAmount);
            if (!started)
            {
                ShowToast("Cannot spin right now", Color.red);
            }
        }

        private float _lastAutoSpinClickTime = 0f;
        private const float DOUBLE_CLICK_TIME = 0.4f;

        private void OnAutoSpinClicked()
        {
            if (_isAutoSpinning)
            {
                // 자동 스핀 중이면 중지
                StopAutoSpin();
                return;
            }

            float currentTime = Time.time;

            // 더블클릭 감지 - 횟수 변경 (싱글클릭은 즉시 시작)
            if (currentTime - _lastAutoSpinClickTime < DOUBLE_CLICK_TIME)
            {
                // 더블클릭: 횟수 순환
                int currentIndex = System.Array.IndexOf(_autoSpinOptions, _autoSpinCount);
                currentIndex = (currentIndex + 1) % _autoSpinOptions.Length;
                _autoSpinCount = _autoSpinOptions[currentIndex];
                UpdateAutoSpinButton();
                ShowToast($"Auto-spin: x{_autoSpinCount}", new Color(0.7f, 0.7f, 0.9f), 0.8f);
                _lastAutoSpinClickTime = currentTime;
                return;
            }

            _lastAutoSpinClickTime = currentTime;

            // 싱글클릭: 즉시 자동 스핀 시작
            StartAutoSpin();
        }

        public void StartAutoSpin()
        {
            if (_isAutoSpinning || _autoSpinCount <= 0) return;

            _isAutoSpinning = true;
            _autoSpinRemaining = _autoSpinCount;
            UpdateAutoSpinButton();
            StartCoroutine(AutoSpinCoroutine());
        }

        public void StopAutoSpin()
        {
            _isAutoSpinning = false;
            _autoSpinRemaining = 0;
            UpdateAutoSpinButton();
            ShowToast("Auto-spin stopped", Color.yellow);
        }

        private System.Collections.IEnumerator AutoSpinCoroutine()
        {
            while (_isAutoSpinning && _autoSpinRemaining > 0)
            {
                // 스핀 중이면 대기
                while (_game.Slot.IsSpinning)
                {
                    yield return null;
                }

                // 골드 부족 체크
                if (_currentBetAmount <= 0 || !_game.Gold.CanAfford(_currentBetAmount))
                {
                    ShowToast("Auto-spin stopped: Not enough gold!", Color.red);
                    StopAutoSpin();
                    yield break;
                }

                // 스핀 실행
                bool started = _game.Slot.TrySpin(_currentBetAmount);
                if (started)
                {
                    _autoSpinRemaining--;
                    UpdateAutoSpinButton();
                }
                else
                {
                    ShowToast("Auto-spin stopped: Cannot spin", Color.red);
                    StopAutoSpin();
                    yield break;
                }

                // 스핀 완료 대기
                while (_game.Slot.IsSpinning)
                {
                    yield return null;
                }

                // 잭팟 당첨 시 중지
                // (OnSlotSpinComplete에서 체크하여 StopAutoSpin 호출)

                // 다음 스핀 전 짧은 딜레이 (게임 템포 개선)
                yield return MobileOptimizer.GetWait(0.2f);
            }

            if (_autoSpinRemaining <= 0)
            {
                ShowToast("Auto-spin completed!", new Color(0.5f, 0.9f, 0.5f));
            }
            _isAutoSpinning = false;
            UpdateAutoSpinButton();
        }

        private void UpdateAutoSpinButton()
        {
            if (_autoSpinText == null) return;

            Button autoBtn = _autoSpinButton ?? _autoSpinButtonRef;
            Image autoImg = autoBtn?.GetComponent<Image>();
            bool hasCustomSprites = _allButtonSprites != null && _allButtonSprites.Length > 0;

            if (_isAutoSpinning)
            {
                _autoSpinText.text = $"STOP\n({_autoSpinRemaining})";
                if (autoImg != null)
                {
                    // 커스텀 스프라이트가 있으면 붉은 틴트, 없으면 색상 변경
                    autoImg.color = hasCustomSprites
                        ? new Color(1f, 0.7f, 0.7f, 1f)  // 붉은 틴트
                        : new Color(0.8f, 0.3f, 0.3f);
                }
            }
            else
            {
                _autoSpinText.text = $"x{_autoSpinCount}";
                if (autoImg != null)
                {
                    autoImg.color = hasCustomSprites
                        ? Color.white
                        : new Color(0.3f, 0.5f, 0.7f);
                }
            }
        }

        // 길게 눌러 자동 스핀 시작
        public void OnAutoSpinHold()
        {
            if (!_isAutoSpinning)
            {
                StartAutoSpin();
            }
        }

        #endregion
    }
}
