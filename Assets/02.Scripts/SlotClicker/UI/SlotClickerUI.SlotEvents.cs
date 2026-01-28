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
        #region Slot Events

        private void OnSlotSpinStart()
        {
            _spinButton.interactable = false;
            SetBetButtonsInteractable(false);
            SetSpinState(SpinUIState.Spinning);

            // ★ 스핀 시작 사운드 + 루프 사운드
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.SpinStart);
                SoundManager.Instance.PlayLoopSFX(SoundType.SpinLoop, 0.5f);
            }

            // ★ 스핀 버튼 피드백
            if (_spinButton != null)
            {
                _spinButton.transform.DOKill();
                _spinButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f);
            }

            if (_resultGroup != null)
            {
                _resultGroup.DOKill();
                _resultGroup.alpha = 0f;
            }
            else if (_resultText != null)
            {
                _resultText.gameObject.SetActive(false);
                _resultText.alpha = 1f;
            }

            // 3x3 그리드 모든 릴 스핀 애니메이션 시작 (9개)
            for (int i = 0; i < 9; i++)
            {
                if (i < _reelSymbols.Length && _reelSymbols[i] != null)
                {
                    _isReelSpinning[i] = true;
                    if (_spinCoroutines[i] != null)
                        StopCoroutine(_spinCoroutines[i]);
                    _spinCoroutines[i] = StartCoroutine(SpinReelAnimation(i));
                }
            }
        }

        /// <summary>
        /// 릴 스핀 애니메이션 코루틴 - 가속/고속/감속 3단계 애니메이션
        /// SpinProfile이 있으면 프로파일 값 사용, 없으면 기본값 사용
        /// </summary>
        private System.Collections.IEnumerator SpinReelAnimation(int reelIndex)
        {
            // 범위 체크
            if (reelIndex < 0 || reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null)
                yield break;

            int symbolCount = _symbolSprites != null && _symbolSprites.Length > 0
                ? _symbolSprites.Length
                : _game.Config.symbolCount;

            // ★ 열(column)별 시작 딜레이 - 프로파일 또는 기본값
            int column = reelIndex % 3;
            float columnDelay = _spinProfile != null
                ? _spinProfile.GetColumnStopDelay(column)
                : column * 0.08f;
            yield return MobileOptimizer.GetWait(columnDelay);

            // ★ 프로파일 또는 기본값에서 파라미터 가져오기
            float accelerationDuration = _spinProfile != null ? _spinProfile.accelDuration : 0.3f;
            float startSpeed = _spinProfile != null ? _spinProfile.accelStartSpeed : 0.15f;
            float maxSpeed = _spinProfile != null ? _spinProfile.maxSpeed : 0.03f;

            // ★ Phase 1: 가속 - 느리게 시작해서 빠르게
            float accelerationTime = 0f;

            while (accelerationTime < accelerationDuration && reelIndex < _isReelSpinning.Length && _isReelSpinning[reelIndex])
            {
                // EaseOutQuad 가속 곡선
                float t = accelerationTime / accelerationDuration;
                float currentSpeed = Mathf.Lerp(startSpeed, maxSpeed, t * t);

                // 심볼 변경 + 슬라이드 효과
                SpinReelStep(reelIndex, symbolCount, currentSpeed, true);

                yield return MobileOptimizer.GetWait(currentSpeed);
                accelerationTime += currentSpeed;
            }

            // ★ Phase 2: 고속 스핀 (정지 신호까지 지속)
            while (reelIndex < _isReelSpinning.Length && _isReelSpinning[reelIndex])
            {
                SpinReelStep(reelIndex, symbolCount, maxSpeed, false);
                yield return MobileOptimizer.GetWait(maxSpeed);
            }
        }

        /// <summary>
        /// 스핀 단계별 심볼 변경 및 효과
        /// SpinProfile 파라미터 사용
        /// </summary>
        private void SpinReelStep(int reelIndex, int symbolCount, float speed, bool slideEffect)
        {
            if (reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null) return;

            // 랜덤 심볼 설정
            int randomSymbol = UnityEngine.Random.Range(0, symbolCount);
            SetReelSymbol(reelIndex, randomSymbol);

            Transform symbolTransform = _reelSymbols[reelIndex].transform;
            symbolTransform.DOKill();

            // ★ 프로파일에서 값 가져오기
            float slideDistance = _spinProfile != null ? _spinProfile.slideDistance : 15f;
            float punchScale = _spinProfile != null ? _spinProfile.accelPunchScale : 0.06f;
            float blurAlpha = _spinProfile != null ? _spinProfile.spinBlurAlpha : 0.85f;

            if (slideEffect)
            {
                // 가속 단계: 아래에서 위로 슬라이드 + 스케일 펀치
                RectTransform rect = symbolTransform.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -slideDistance);
                    rect.DOAnchorPosY(0f, speed * 0.8f).SetEase(Ease.OutQuad);
                }
                symbolTransform.localScale = Vector3.one * 0.9f;
                symbolTransform.DOScale(1f, speed * 0.8f);
            }
            else
            {
                // 고속 단계: 빠른 스케일 펀치
                symbolTransform.localScale = Vector3.one;
                symbolTransform.DOPunchScale(Vector3.one * punchScale, speed * 0.8f, 0, 0);
            }

            // 블러 효과 (고속 시 약간 투명하게)
            Image symbolImage = _reelSymbols[reelIndex];
            if (symbolImage != null && speed < 0.05f)
            {
                symbolImage.DOKill();
                symbolImage.color = new Color(1f, 1f, 1f, blurAlpha);
            }
        }

        /// <summary>
        /// 릴에 심볼 설정
        /// </summary>
        private void SetReelSymbol(int reelIndex, int symbolIndex)
        {
            if (reelIndex < 0 || _reelSymbols == null || reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null)
                return;

            Sprite sprite = GetSymbolSprite(symbolIndex);
            if (sprite != null)
            {
                _reelSymbols[reelIndex].sprite = sprite;
                _reelSymbols[reelIndex].color = Color.white;
            }
            else
            {
                _reelSymbols[reelIndex].sprite = null;
                _reelSymbols[reelIndex].color = GetSymbolColor(symbolIndex);
            }
        }

        private void OnReelStop(int reelIndex, int symbolIndex)
        {
            if (reelIndex >= 0 && reelIndex < _reelSymbols.Length && _reelSymbols[reelIndex] != null)
            {
                // 스핀 애니메이션 중지
                _isReelSpinning[reelIndex] = false;
                if (reelIndex < _spinCoroutines.Length && _spinCoroutines[reelIndex] != null)
                {
                    StopCoroutine(_spinCoroutines[reelIndex]);
                    _spinCoroutines[reelIndex] = null;
                }

                // ★ 감속 정지 애니메이션 시작
                StartCoroutine(ReelStopAnimation(reelIndex, symbolIndex));
            }

            if (_spinState == SpinUIState.Spinning)
            {
                SetSpinState(SpinUIState.Stopping);
            }
        }

        /// <summary>
        /// 릴 정지 애니메이션 - 감속 효과와 바운스
        /// SpinProfile 파라미터 사용
        /// </summary>
        private System.Collections.IEnumerator ReelStopAnimation(int reelIndex, int finalSymbolIndex)
        {
            if (reelIndex >= _reelSymbols.Length || _reelSymbols[reelIndex] == null)
                yield break;

            int symbolCount = _symbolSprites != null && _symbolSprites.Length > 0
                ? _symbolSprites.Length
                : _game.Config.symbolCount;

            // ★ 프로파일에서 감속 파라미터 가져오기
            int decelSteps = _spinProfile != null ? _spinProfile.decelerationSteps : 3;
            float blurAlpha = _spinProfile != null ? _spinProfile.spinBlurAlpha : 0.85f;

            // ★ Phase 1: 감속 (프로파일 기반 심볼 변경하며 느려짐)
            for (int i = 0; i < decelSteps; i++)
            {
                float decelSpeed = _spinProfile != null
                    ? _spinProfile.GetDecelerationSpeed(i, decelSteps)
                    : 0.06f + (i * 0.04f); // 기본: 0.06, 0.10, 0.14...

                int randomSymbol = UnityEngine.Random.Range(0, symbolCount);
                SetReelSymbol(reelIndex, randomSymbol);

                Transform symbolTransform = _reelSymbols[reelIndex].transform;
                RectTransform rect = symbolTransform.GetComponent<RectTransform>();
                symbolTransform.DOKill();

                // 위에서 아래로 슬라이드 (감속 느낌)
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 4.804f);
                    rect.DOAnchorPosY(0f, decelSpeed * 0.9f).SetEase(Ease.OutQuad);
                }

                // 점점 선명해지는 효과
                _reelSymbols[reelIndex].DOKill();
                float alphaProgress = (float)i / Mathf.Max(1, decelSteps - 1);
                float alpha = blurAlpha + ((1f - blurAlpha) * alphaProgress);
                _reelSymbols[reelIndex].color = new Color(1f, 1f, 1f, alpha);

                yield return MobileOptimizer.GetWait(decelSpeed);
            }

            // ★ 프로파일에서 바운스 파라미터 가져오기
            float bounceIntensity = _spinProfile != null ? _spinProfile.bounceIntensity : 0.12f;
            float bounceDuration = _spinProfile != null ? _spinProfile.bounceDuration : 0.3f;
            int bounceVibrato = _spinProfile != null ? _spinProfile.bounceVibrato : 4;
            float bounceElasticity = _spinProfile != null ? _spinProfile.bounceElasticity : 0.6f;
            bool enableFlash = _spinProfile != null ? _spinProfile.enableLandingFlash : true;
            float flashIntensity = _spinProfile != null ? _spinProfile.flashIntensity : 1.4f;
            float flashDuration = _spinProfile != null ? _spinProfile.flashDuration : 0.1f;

            // ★ Phase 2: 최종 심볼 설정 + 바운스 정지
            Transform finalTransform = _reelSymbols[reelIndex].transform;
            RectTransform finalRect = finalTransform.GetComponent<RectTransform>();

            finalTransform.DOKill();
            finalTransform.localScale = Vector3.one;
            finalTransform.rotation = Quaternion.identity;

            // 최종 심볼 설정
            SetReelSymbol(reelIndex, finalSymbolIndex);

            // 위에서 떨어지며 착지하는 효과
            if (finalRect != null)
            {
                finalRect.anchoredPosition = new Vector2(finalRect.anchoredPosition.x, 7.206f);
                finalRect.DOAnchorPosY(0f, 0.2f).SetEase(Ease.OutBounce);
            }

            // 마지막 열인지 확인 (column 2 = 인덱스 2, 5, 8)
            bool isLastColumn = (reelIndex == 2 || reelIndex == 5 || reelIndex == 8);
            bool isVeryLastReel = (reelIndex == 8);  // 가장 마지막 릴

            // 마지막 열은 더 강한 바운스
            float finalBounce = isLastColumn ? bounceIntensity * 1.5f : bounceIntensity;
            int finalVibrato = isLastColumn ? bounceVibrato + 2 : bounceVibrato;

            // 바운스 스케일 효과 (프로파일 파라미터 사용)
            finalTransform.DOPunchScale(Vector3.one * finalBounce, bounceDuration, finalVibrato, bounceElasticity);

            // 정지 플래시 효과 (선명하게 복원)
            _reelSymbols[reelIndex].DOKill();
            _reelSymbols[reelIndex].color = Color.white;

            if (enableFlash)
            {
                float finalFlash = isLastColumn ? flashIntensity * 1.3f : flashIntensity;
                _reelSymbols[reelIndex].DOColor(Color.white * finalFlash, flashDuration)
                    .OnComplete(() => _reelSymbols[reelIndex].DOColor(Color.white, flashDuration * 2f));
            }

            // ★ 릴 정지 사운드
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.ReelStop);
            }

            // 햅틱 피드백 (마지막 열은 더 강하게)
            if (isVeryLastReel)
            {
                UIFeedback.TriggerHaptic(UIFeedback.HapticType.Medium);
                // 마지막 릴 정지 시 화면 살짝 흔들림
                if (_mainCanvas != null)
                {
                    _mainCanvas.transform.DOShakePosition(0.15f, 5f, 12, 90f, false, true);
                }
            }
            else
            {
                UIFeedback.TriggerHaptic(UIFeedback.HapticType.Light);
            }
        }

        private void OnSlotSpinComplete(SlotResult result)
        {
            // ★ 스핀 루프 사운드 정지
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopLoopSFX();
            }

            _spinButton.interactable = true;
            SetBetButtonsInteractable(true);
            SetSpinState(SpinUIState.Result);

            // 세션 통계 업데이트
            _sessionSpins++;
            if (result.IsWin)
            {
                _sessionWins++;
                _sessionEarnings += result.FinalReward - result.BetAmount;
            }
            else
            {
                _sessionEarnings -= result.BetAmount;
            }
            UpdateStatistics();

            // 당첨 릴 인덱스 계산
            int[] highlightIndices = GetWinningReelIndices(result);

            // 슬롯 승리 피드백 시스템 활용 (승리 또는 무승부인 경우)
            if (_slotWinFeedback != null && result.Outcome != SlotOutcome.Loss)
            {
                _slotWinFeedback.PlayWinFeedback(result, highlightIndices);

                // 큰 승리일수록 화면 중앙에 추가 임팩트를 얹는다
                if (result.Outcome >= SlotOutcome.BigWin)
                {
                    Vector2 centerPos = Vector2.zero;
                    SpawnClickParticles(centerPos, true);
                    SpawnClickRipple(centerPos, true, _jackpotColor, 1.45f, 1.25f);
                }

                // 잭팟 계열은 화면 테두리/플래시를 한 번 더 강조
                if (result.Outcome == SlotOutcome.Jackpot || result.Outcome == SlotOutcome.MegaJackpot)
                {
                    PlayScreenGlow(true);
                    float flashDuration = result.Outcome == SlotOutcome.MegaJackpot ? 1.45f : 1.15f;
                    float flashAlpha = result.Outcome == SlotOutcome.MegaJackpot ? 1.25f : 1.05f;
                    PlayCriticalFlash(_jackpotColor, durationMultiplier: flashDuration, alphaMultiplier: flashAlpha);
                }
            }
            else
            {
                // 니어미스 피드백 (아깝게 놓친 경우)
                if (result.IsNearMiss && result.NearMissPayline != null && result.NearMissPayline.Length > 0)
                {
                    ShowResult("SO CLOSE!", new Color(1f, 0.6f, 0.3f));
                    ShowToast("Almost there! Try again!", new Color(1f, 0.7f, 0.4f), 1.2f);

                    // 니어미스 페이라인 하이라이트 (깜빡임)
                    HighlightNearMissReels(result.NearMissPayline);
                }
                else
                {
                    // 일반 패배
                    ShowResult("No Match...", Color.gray);
                }

                // 패배 사운드 재생
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySlotResultSound(SlotOutcome.Loss);
                }
            }

            // 연승 콤보 피드백 (2연승 이상)
            if (result.WinStreak >= 2)
            {
                ShowComboFeedback(result.WinStreak, result.ComboMultiplier);
            }

            // 잭팟 당첨 시 자동 스핀 중지
            if (_isAutoSpinning && (result.Outcome == SlotOutcome.Jackpot || result.Outcome == SlotOutcome.MegaJackpot))
            {
                StopAutoSpin();
                ShowToast("JACKPOT! Auto-spin stopped", new Color(1f, 0.8f, 0.2f));
            }

            // 결과에 따른 Ready 상태 복귀 지연 시간 조정 (게임 템포 개선)
            float readyDelay = result.Outcome switch
            {
                SlotOutcome.MegaJackpot => 2.5f,  // 6f → 2.5f
                SlotOutcome.Jackpot => 2f,        // 4.5f → 2f
                SlotOutcome.BigWin => 1.5f,       // 3f → 1.5f
                SlotOutcome.SmallWin => 1f,       // 2.5f → 1f
                SlotOutcome.MiniWin => 0.8f,      // 2f → 0.8f
                SlotOutcome.Draw => 0.6f,         // 1.5f → 0.6f
                _ => 0.5f                          // 1.2f → 0.5f
            };

            DOVirtual.DelayedCall(readyDelay, () => SetSpinState(SpinUIState.Ready));
        }

        private void ShowResult(string message, Color color)
        {
            if (_resultText == null) return;

            _resultText.text = message;
            _resultText.color = color;

            if (_resultGroup != null)
            {
                _resultGroup.DOKill();
                _resultGroup.alpha = 1f;
                _resultText.transform.localScale = Vector3.one * 0.9f;
                _resultText.transform.DOKill();
                _resultText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

                _resultTween?.Kill();
                _resultTween = _resultGroup.DOFade(0f, 0.5f).SetDelay(3f);
            }
            else
            {
                _resultText.gameObject.SetActive(true);
                _resultText.transform.localScale = Vector3.zero;
                _resultText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

                DOVirtual.DelayedCall(3f, () =>
                {
                    _resultText.DOFade(0, 0.5f).OnComplete(() =>
                    {
                        _resultText.gameObject.SetActive(false);
                        _resultText.alpha = 1f;
                    });
                });
            }
        }

        /// <summary>
        /// 니어미스 릴 하이라이트 (아깝게 놓친 심볼 강조)
        /// </summary>
        private void HighlightNearMissReels(int[] paylineIndices)
        {
            if (_reelFrames == null || paylineIndices == null) return;

            Color nearMissColor = new Color(1f, 0.5f, 0.2f, 1f);  // 주황색

            foreach (int idx in paylineIndices)
            {
                if (idx >= 0 && idx < _reelFrames.Length && _reelFrames[idx] != null)
                {
                    Image frame = _reelFrames[idx];
                    Color originalColor = frame.color;

                    // 깜빡임 효과 (3회)
                    Sequence blinkSeq = DOTween.Sequence();
                    blinkSeq.Append(frame.DOColor(nearMissColor, 0.12f));
                    blinkSeq.Append(frame.DOColor(originalColor, 0.12f));
                    blinkSeq.SetLoops(3);
                    blinkSeq.OnComplete(() => frame.color = originalColor);

                    // 심볼 흔들기
                    if (_reelSymbols != null && idx < _reelSymbols.Length && _reelSymbols[idx] != null)
                    {
                        _reelSymbols[idx].transform.DOShakePosition(0.5f, 8f, 15, 90f, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// 연승 콤보 피드백
        /// </summary>
        private void ShowComboFeedback(int streak, float multiplier)
        {
            string comboText = streak switch
            {
                2 => "2x COMBO!",
                3 => "3x COMBO!!",
                4 => "4x COMBO!!!",
                5 => "5x STREAK!",
                >= 6 => $"{streak}x SUPER STREAK!",
                _ => ""
            };

            if (string.IsNullOrEmpty(comboText)) return;

            Color comboColor = streak switch
            {
                2 => new Color(0.4f, 0.8f, 1f),      // 하늘색
                3 => new Color(0.4f, 1f, 0.6f),      // 연두색
                4 => new Color(1f, 0.9f, 0.3f),      // 노란색
                5 => new Color(1f, 0.6f, 0.2f),      // 주황색
                >= 6 => new Color(1f, 0.3f, 0.5f),   // 분홍색
                _ => Color.white
            };

            // 콤보 토스트 표시
            ShowToast($"{comboText} (+{(multiplier - 1f) * 100:F0}% Bonus!)", comboColor, 1.5f);

            // 콤보 이펙트 (화면 가장자리 글로우)
            if (streak >= 3)
            {
                PlayScreenGlow(false);
            }

            // 5연승 이상이면 추가 파티클
            if (streak >= 5)
            {
                Vector2 centerPos = Vector2.zero;
                SpawnClickParticles(centerPos, true);
            }
        }

        private int[] GetWinningReelIndices(SlotResult result)
        {
            if (result == null || result.WinningPayline == null || result.WinningPayline.Length == 0)
                return Array.Empty<int>();

            // 당첨 페이라인의 인덱스들을 반환
            System.Collections.Generic.HashSet<int> winningIndices = new System.Collections.Generic.HashSet<int>();

            // 3x3 페이라인 정의 (SlotManager.SlotPaylines와 동일)
            int[][] paylines = new int[][]
            {
                new int[] { 3, 4, 5 },  // 중간 가로
                new int[] { 0, 1, 2 },  // 상단 가로
                new int[] { 6, 7, 8 },  // 하단 가로
                new int[] { 0, 4, 8 },  // 대각선 ↘
                new int[] { 6, 4, 2 }   // 대각선 ↗
            };

            foreach (int paylineIdx in result.WinningPayline)
            {
                if (paylineIdx >= 0 && paylineIdx < paylines.Length)
                {
                    foreach (int idx in paylines[paylineIdx])
                    {
                        winningIndices.Add(idx);
                    }
                }
            }

            int[] resultArray = new int[winningIndices.Count];
            winningIndices.CopyTo(resultArray);
            return resultArray;
        }

        private void HighlightReels(int[] indices, Color color)
        {
            if (indices == null || _reelSymbols == null) return;

            for (int i = 0; i < indices.Length; i++)
            {
                int reelIndex = indices[i];
                if (reelIndex < 0 || reelIndex >= _reelSymbols.Length) continue;

                if (_reelSymbols[reelIndex] != null)
                {
                    _reelSymbols[reelIndex].transform.DOKill();
                    _reelSymbols[reelIndex].transform.localScale = Vector3.one;
                    _reelSymbols[reelIndex].transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 5, 0.6f);
                }

                if (reelIndex < _reelFrames.Length && _reelFrames[reelIndex] != null)
                {
                    Image frame = _reelFrames[reelIndex];
                    Color original = _reelFrameBaseColor;
                    frame.DOKill();
                    frame.DOColor(color, 0.12f).SetLoops(6, LoopType.Yoyo)
                        .OnComplete(() => frame.color = original);
                }
            }
        }

        private void CelebrationEffect()
        {
            // 화면 플래시
            GameObject flash = CreatePanel(_mainCanvas.GetComponent<RectTransform>(), "Flash",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.5f));

            flash.GetComponent<Image>().DOFade(0, 0.5f).OnComplete(() => Destroy(flash));

            // 화면 흔들림
            _mainCanvas.transform.DOShakePosition(0.5f, 30f, 20);

            // 화면 테두리 글로우 (잭팟용)
            PlayScreenGlow(true);

            // 대량 파티클 분출
            Vector2 centerPos = Vector2.zero;
            for (int i = 0; i < 3; i++)
            {
                DOVirtual.DelayedCall(i * 0.15f, () =>
                {
                    SpawnClickParticles(centerPos + new Vector2(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-50f, 50f)), true);
                });
            }

            // 강력한 햅틱
            UIFeedback.TriggerHaptic(UIFeedback.HapticType.Heavy);
        }

        #endregion
    }
}
