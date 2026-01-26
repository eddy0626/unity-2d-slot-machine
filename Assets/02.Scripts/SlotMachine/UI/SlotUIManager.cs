using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Core;
using SlotMachine.Data;

namespace SlotMachine.UI
{
    public class SlotUIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Core.SlotMachine slotMachine;

        [Header("Coin Display")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private TextMeshProUGUI winText;

        [Header("Bet Display")]
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private Button betIncreaseButton;
        [SerializeField] private Button betDecreaseButton;
        [SerializeField] private Button maxBetButton;

        [Header("Spin Button")]
        [SerializeField] private Button spinButton;
        [SerializeField] private TextMeshProUGUI spinButtonText;
        [SerializeField] private Image spinButtonGlow;

        [Header("Payline Display")]
        [SerializeField] private GameObject paylineDisplayPrefab;
        [SerializeField] private Transform paylineContainer;
        [SerializeField] private RectTransform[] symbolSlots; // 9개 슬롯 위치

        [Header("Win Animation")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private TextMeshProUGUI bigWinText;
        [SerializeField] private ParticleSystem coinParticle;

        [Header("Colors")]
        [SerializeField] private Color normalTextColor = new Color(1f, 1f, 1f);
        [SerializeField] private Color winTextColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color glowColor = new Color(0f, 1f, 0.8f, 0.5f);

        private List<GameObject> _activePaylineDisplays = new List<GameObject>();
        private Coroutine _winAnimationCoroutine;

        private void Start()
        {
            SetupUI();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SetupUI()
        {
            // 버튼 리스너 설정
            if (spinButton != null)
                spinButton.onClick.AddListener(OnSpinButtonClick);

            if (betIncreaseButton != null)
                betIncreaseButton.onClick.AddListener(OnBetIncreaseClick);

            if (betDecreaseButton != null)
                betDecreaseButton.onClick.AddListener(OnBetDecreaseClick);

            if (maxBetButton != null)
                maxBetButton.onClick.AddListener(OnMaxBetClick);

            // 초기 UI 상태
            if (winPanel != null)
                winPanel.SetActive(false);

            if (winText != null)
                winText.text = "";
        }

        private void SubscribeToEvents()
        {
            if (slotMachine == null) return;

            slotMachine.OnCoinsChanged.AddListener(UpdateCoinDisplay);
            slotMachine.OnBetChanged.AddListener(UpdateBetDisplay);
            slotMachine.OnSpinStart.AddListener(OnSpinStart);
            slotMachine.OnSpinEnd.AddListener(OnSpinEnd);
            slotMachine.OnWin.AddListener(OnWin);
        }

        private void UnsubscribeFromEvents()
        {
            if (slotMachine == null) return;

            slotMachine.OnCoinsChanged.RemoveListener(UpdateCoinDisplay);
            slotMachine.OnBetChanged.RemoveListener(UpdateBetDisplay);
            slotMachine.OnSpinStart.RemoveListener(OnSpinStart);
            slotMachine.OnSpinEnd.RemoveListener(OnSpinEnd);
            slotMachine.OnWin.RemoveListener(OnWin);
        }

        #region UI Updates

        private void UpdateCoinDisplay(int coins)
        {
            if (coinText != null)
            {
                coinText.text = FormatNumber(coins);
                StartCoroutine(PulseText(coinText));
            }
        }

        private void UpdateBetDisplay(int bet)
        {
            if (betText != null)
            {
                betText.text = FormatNumber(bet);
            }
        }

        private void OnSpinStart()
        {
            // 스핀 버튼 비활성화
            SetSpinButtonState(false);

            // Win 텍스트 초기화
            if (winText != null)
                winText.text = "";

            // 기존 페이라인 표시 제거
            ClearPaylineDisplays();
        }

        private void OnSpinEnd()
        {
            // 스핀 버튼 활성화
            SetSpinButtonState(slotMachine.CanSpin);
        }

        private void OnWin(List<WinResult> results, int totalWin)
        {
            // Win 텍스트 표시
            if (winText != null)
            {
                winText.text = $"+{FormatNumber(totalWin)}";
                winText.color = winTextColor;
                StartCoroutine(PulseText(winText, 1.5f));
            }

            // 페이라인 하이라이트
            StartCoroutine(ShowWinningPaylines(results));

            // 큰 당첨 애니메이션
            if (totalWin >= slotMachine.CurrentBet * 10)
            {
                ShowBigWin(totalWin);
            }

            // 파티클 효과
            if (coinParticle != null)
            {
                coinParticle.Play();
            }
        }

        #endregion

        #region Button Handlers

        private void OnSpinButtonClick()
        {
            if (slotMachine != null && slotMachine.CanSpin)
            {
                slotMachine.Spin();
            }
        }

        private void OnBetIncreaseClick()
        {
            slotMachine?.IncreaseBet();
        }

        private void OnBetDecreaseClick()
        {
            slotMachine?.DecreaseBet();
        }

        private void OnMaxBetClick()
        {
            slotMachine?.SetMaxBet();
        }

        #endregion

        #region Visual Effects

        private void SetSpinButtonState(bool interactable)
        {
            if (spinButton != null)
            {
                spinButton.interactable = interactable;
            }

            if (spinButtonText != null)
            {
                spinButtonText.text = interactable ? "SPIN" : "...";
            }

            if (spinButtonGlow != null)
            {
                spinButtonGlow.enabled = interactable;
            }
        }

        private IEnumerator ShowWinningPaylines(List<WinResult> results)
        {
            foreach (var result in results)
            {
                if (result.paylineIndex >= 0)
                {
                    // 일반 페이라인 하이라이트
                    HighlightPayline(result.positions, result.symbol);
                }
                else
                {
                    // 스캐터 하이라이트
                    HighlightScatter(result.positions);
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        private void HighlightPayline(int[] positions, SymbolData symbol)
        {
            if (symbolSlots == null || positions == null) return;

            foreach (int pos in positions)
            {
                if (pos >= 0 && pos < symbolSlots.Length && symbolSlots[pos] != null)
                {
                    // 글로우 효과 추가
                    StartCoroutine(GlowEffect(symbolSlots[pos], symbol.glowColor));
                }
            }
        }

        private void HighlightScatter(int[] positions)
        {
            if (symbolSlots == null || positions == null) return;

            foreach (int pos in positions)
            {
                if (pos >= 0 && pos < symbolSlots.Length && symbolSlots[pos] != null)
                {
                    StartCoroutine(GlowEffect(symbolSlots[pos], Color.magenta));
                }
            }
        }

        private IEnumerator GlowEffect(RectTransform target, Color color)
        {
            Image glowImage = target.GetComponent<Image>();
            if (glowImage == null) yield break;

            Color originalColor = glowImage.color;
            float duration = 0.5f;
            int blinkCount = 3;

            for (int i = 0; i < blinkCount; i++)
            {
                // 밝아지기
                float elapsed = 0f;
                while (elapsed < duration / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (duration / 2);
                    glowImage.color = Color.Lerp(originalColor, color, t);
                    yield return null;
                }

                // 원래대로
                elapsed = 0f;
                while (elapsed < duration / 2)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (duration / 2);
                    glowImage.color = Color.Lerp(color, originalColor, t);
                    yield return null;
                }
            }

            glowImage.color = originalColor;
        }

        private void ShowBigWin(int amount)
        {
            if (winPanel != null && bigWinText != null)
            {
                winPanel.SetActive(true);
                bigWinText.text = $"BIG WIN!\n{FormatNumber(amount)}";

                if (_winAnimationCoroutine != null)
                    StopCoroutine(_winAnimationCoroutine);

                _winAnimationCoroutine = StartCoroutine(HideBigWinAfterDelay(3f));
            }
        }

        private IEnumerator HideBigWinAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (winPanel != null)
                winPanel.SetActive(false);
        }

        private void ClearPaylineDisplays()
        {
            foreach (var display in _activePaylineDisplays)
            {
                if (display != null)
                    Destroy(display);
            }
            _activePaylineDisplays.Clear();
        }

        private IEnumerator PulseText(TextMeshProUGUI text, float scale = 1.2f)
        {
            if (text == null) yield break;

            Vector3 originalScale = text.transform.localScale;
            Vector3 targetScale = originalScale * scale;
            float duration = 0.15f;

            // 확대
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                text.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                yield return null;
            }

            // 축소
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                text.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                yield return null;
            }

            text.transform.localScale = originalScale;
        }

        #endregion

        #region Utilities

        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            if (number >= 1000)
                return $"{number / 1000f:F1}K";
            return number.ToString("N0");
        }

        #endregion

        #region Public Methods for External Access

        public void RefreshUI()
        {
            if (slotMachine != null)
            {
                UpdateCoinDisplay(slotMachine.CurrentCoins);
                UpdateBetDisplay(slotMachine.CurrentBet);
                SetSpinButtonState(slotMachine.CanSpin);
            }
        }

        public void AddCoinsDebug()
        {
            slotMachine?.AddCoins(1000);
        }

        #endregion
    }
}
