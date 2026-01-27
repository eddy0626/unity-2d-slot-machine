using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// 슬롯 승리 피드백 시스템
    /// 각 승리 등급에 따라 차등화된 시각적/청각적 피드백을 제공
    /// </summary>
    public class SlotWinFeedback : MonoBehaviour
    {
        #region Configuration

        [Header("=== Win Text Settings ===")]
        [SerializeField] private Color _miniWinColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color _smallWinColor = new Color(0.4f, 1f, 0.8f);
        [SerializeField] private Color _bigWinColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color _jackpotColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _megaJackpotColor = new Color(1f, 0.5f, 0.8f);

        [Header("=== Glow Settings ===")]
        [SerializeField] private Color _miniWinGlowColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color _smallWinGlowColor = new Color(0.4f, 1f, 0.8f, 0.3f);
        [SerializeField] private Color _bigWinGlowColor = new Color(0.3f, 1f, 0.3f, 0.5f);
        [SerializeField] private Color _jackpotGlowColor = new Color(1f, 0.85f, 0.2f, 0.7f);
        [SerializeField] private Color _megaJackpotGlowColor = new Color(1f, 0.5f, 0.8f, 0.9f);

        [Header("=== Particle Settings ===")]
        [SerializeField] private int _miniWinParticleCount = 5;
        [SerializeField] private int _smallWinParticleCount = 12;
        [SerializeField] private int _bigWinParticleCount = 25;
        [SerializeField] private int _jackpotParticleCount = 50;
        [SerializeField] private int _megaJackpotParticleCount = 100;

        [Header("=== Screen Shake Settings ===")]
        [SerializeField] private float _bigWinShakeStrength = 15f;
        [SerializeField] private float _jackpotShakeStrength = 30f;
        [SerializeField] private float _megaJackpotShakeStrength = 50f;

        [Header("=== Countup Settings ===")]
        [SerializeField] private float _miniWinCountupDuration = 0.5f;
        [SerializeField] private float _smallWinCountupDuration = 1f;
        [SerializeField] private float _bigWinCountupDuration = 1.5f;
        [SerializeField] private float _jackpotCountupDuration = 2.5f;
        [SerializeField] private float _megaJackpotCountupDuration = 4f;

        [Header("=== Symbol Highlight Settings ===")]
        [SerializeField] private float _miniWinPulseIntensity = 0.1f;
        [SerializeField] private float _smallWinPulseIntensity = 0.2f;
        [SerializeField] private float _bigWinPulseIntensity = 0.35f;
        [SerializeField] private float _jackpotPulseIntensity = 0.5f;
        [SerializeField] private float _megaJackpotPulseIntensity = 0.6f;

        #endregion

        #region Private Fields

        private Canvas _mainCanvas;
        private RectTransform _canvasRect;

        // Win Banner UI
        private GameObject _winBanner;
        private TextMeshProUGUI _winTitleText;
        private TextMeshProUGUI _winAmountText;
        private Image _winBannerBg;
        private Image _winBannerGlow;
        private CanvasGroup _winBannerGroup;

        // Screen Effects
        private Image _screenFlash;
        private Image[] _screenGlowEdges;
        private float _screenGlowThickness = 60f;

        // Particle System
        private GameObject _particlePrefab;
        private Queue<GameObject> _particlePool = new Queue<GameObject>();
        private List<GameObject> _activeParticles = new List<GameObject>();
        private const int PARTICLE_POOL_SIZE = 120;

        // Gold Coin Rain
        private GameObject _coinPrefab;
        private Queue<GameObject> _coinPool = new Queue<GameObject>();
        private List<GameObject> _activeCoins = new List<GameObject>();
        private const int COIN_POOL_SIZE = 50;
        private Sprite _coinSprite;

        // Tweens
        private Sequence _currentWinSequence;
        private Tween _countupTween;
        private Coroutine _megaJackpotCoroutine;

        // Symbol References
        private Image[] _reelSymbols;
        private Image[] _reelFrames;

        // State
        private bool _isShowingFeedback = false;

        #endregion

        #region Initialization

        public void Initialize(Canvas canvas, Image[] reelSymbols, Image[] reelFrames)
        {
            _mainCanvas = canvas;
            _canvasRect = canvas.GetComponent<RectTransform>();
            _reelSymbols = reelSymbols;
            _reelFrames = reelFrames;

            LoadSprites();
            CreateWinBannerUI();
            CreateScreenFlash();
            CreateScreenGlowEdges();
            CreateParticlePool();
            CreateCoinPool();

            Debug.Log("[SlotWinFeedback] Initialized");
        }

        private void LoadSprites()
        {
            // Load coin sprite from Resources/UI/코인
            Sprite[] coinSprites = Resources.LoadAll<Sprite>("UI/코인");
            if (coinSprites != null && coinSprites.Length > 0)
            {
                // 코인_0 사용 (첫 번째 스프라이트)
                _coinSprite = coinSprites[0];
                Debug.Log($"[SlotWinFeedback] Coin sprite loaded: {_coinSprite.name}");
            }
            else
            {
                Debug.LogWarning("[SlotWinFeedback] Coin sprite not found at Resources/UI/코인");
            }
        }

        #endregion

        #region UI Creation

        private void CreateWinBannerUI()
        {
            if (_mainCanvas == null) return;

            // Main Banner Container
            _winBanner = new GameObject("WinBanner");
            _winBanner.transform.SetParent(_mainCanvas.transform, false);

            RectTransform bannerRect = _winBanner.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(700, 250);
            bannerRect.anchoredPosition = new Vector2(0, 100);

            _winBannerGroup = _winBanner.AddComponent<CanvasGroup>();
            _winBannerGroup.alpha = 0;

            // Glow Layer (behind banner)
            GameObject glowObj = new GameObject("BannerGlow");
            glowObj.transform.SetParent(_winBanner.transform, false);

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-50, -50);
            glowRect.offsetMax = new Vector2(50, 50);

            _winBannerGlow = glowObj.AddComponent<Image>();
            // 단색 글로우로 사용 (스프라이트 불필요)
            _winBannerGlow.color = new Color(1f, 0.85f, 0.2f, 0);
            _winBannerGlow.raycastTarget = false;

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_winBanner.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _winBannerBg = bgObj.AddComponent<Image>();
            _winBannerBg.color = new Color(0.1f, 0.08f, 0.15f, 0.95f);
            _winBannerBg.raycastTarget = false;

            // Win Title Text
            GameObject titleObj = new GameObject("WinTitle");
            titleObj.transform.SetParent(_winBanner.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.55f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(20, 0);
            titleRect.offsetMax = new Vector2(-20, -15);

            _winTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            _winTitleText.text = "";
            _winTitleText.fontSize = 72;
            _winTitleText.fontStyle = FontStyles.Bold;
            _winTitleText.alignment = TextAlignmentOptions.Center;
            _winTitleText.color = Color.white;
            _winTitleText.enableWordWrapping = false;

            // Win Amount Text
            GameObject amountObj = new GameObject("WinAmount");
            amountObj.transform.SetParent(_winBanner.transform, false);

            RectTransform amountRect = amountObj.AddComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0, 0);
            amountRect.anchorMax = new Vector2(1, 0.55f);
            amountRect.offsetMin = new Vector2(20, 15);
            amountRect.offsetMax = new Vector2(-20, 0);

            _winAmountText = amountObj.AddComponent<TextMeshProUGUI>();
            _winAmountText.text = "";
            _winAmountText.fontSize = 56;
            _winAmountText.fontStyle = FontStyles.Bold;
            _winAmountText.alignment = TextAlignmentOptions.Center;
            _winAmountText.color = new Color(1f, 0.9f, 0.4f);

            _winBanner.SetActive(false);
        }

        private void CreateScreenFlash()
        {
            if (_mainCanvas == null) return;

            GameObject flashObj = new GameObject("ScreenFlash");
            flashObj.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = flashObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _screenFlash = flashObj.AddComponent<Image>();
            _screenFlash.color = new Color(1, 1, 1, 0);
            _screenFlash.raycastTarget = false;

            flashObj.SetActive(false);
        }

        private void CreateScreenGlowEdges()
        {
            if (_mainCanvas == null) return;

            _screenGlowEdges = new Image[4];

            // Top
            _screenGlowEdges[0] = CreateGlowEdge("ScreenGlow_Top",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -_screenGlowThickness / 2), new Vector2(0, _screenGlowThickness));

            // Bottom
            _screenGlowEdges[1] = CreateGlowEdge("ScreenGlow_Bottom",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, _screenGlowThickness / 2), new Vector2(0, _screenGlowThickness));

            // Left
            _screenGlowEdges[2] = CreateGlowEdge("ScreenGlow_Left",
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(_screenGlowThickness / 2, 0), new Vector2(_screenGlowThickness, 0));

            // Right
            _screenGlowEdges[3] = CreateGlowEdge("ScreenGlow_Right",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-_screenGlowThickness / 2, 0), new Vector2(_screenGlowThickness, 0));

            foreach (var edge in _screenGlowEdges)
            {
                edge.color = new Color(1, 1, 1, 0);
                edge.gameObject.SetActive(false);
            }
        }

        private Image CreateGlowEdge(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(_canvasRect, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.raycastTarget = false;
            // 단색 글로우로 사용 (스프라이트 불필요)

            return img;
        }

        private void CreateParticlePool()
        {
            if (_mainCanvas == null) return;

            // Particle Prefab
            _particlePrefab = new GameObject("ParticlePrefab");
            _particlePrefab.SetActive(false);

            RectTransform rect = _particlePrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20, 20);

            Image img = _particlePrefab.AddComponent<Image>();
            // 원형 파티클은 스프라이트 없이 단색으로 사용
            img.raycastTarget = false;

            _particlePrefab.transform.SetParent(_mainCanvas.transform, false);

            // Pool
            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                GameObject pooled = Instantiate(_particlePrefab, _mainCanvas.transform);
                pooled.name = "WinParticle";
                pooled.SetActive(false);
                _particlePool.Enqueue(pooled);
            }
        }

        private void CreateCoinPool()
        {
            if (_mainCanvas == null) return;

            // Coin Prefab (스프라이트 없으면 스킵)
            if (_coinSprite == null)
            {
                Debug.LogWarning("[SlotWinFeedback] Coin sprite not loaded, skipping coin pool creation");
                return;
            }

            _coinPrefab = new GameObject("CoinPrefab");
            _coinPrefab.SetActive(false);

            RectTransform rect = _coinPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50, 50);

            Image img = _coinPrefab.AddComponent<Image>();
            img.sprite = _coinSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            // 코인 스프라이트 원본 색상 사용
            img.color = Color.white;

            _coinPrefab.transform.SetParent(_mainCanvas.transform, false);

            // Pool
            for (int i = 0; i < COIN_POOL_SIZE; i++)
            {
                GameObject pooled = Instantiate(_coinPrefab, _mainCanvas.transform);
                pooled.name = "GoldCoin";
                pooled.SetActive(false);
                _coinPool.Enqueue(pooled);
            }
        }

        #endregion

        #region Main Feedback Method

        /// <summary>
        /// 슬롯 결과에 따른 피드백 실행
        /// </summary>
        public void PlayWinFeedback(SlotResult result, int[] winningReelIndices)
        {
            if (result == null || !result.IsWin) return;

            _isShowingFeedback = true;

            // Cancel any existing feedback
            StopAllFeedback();

            // Play sound based on outcome
            PlayResultSound(result.Outcome);

            // Execute feedback based on outcome
            switch (result.Outcome)
            {
                case SlotOutcome.MiniWin:
                    PlayMiniWinFeedback(result, winningReelIndices);
                    break;
                case SlotOutcome.SmallWin:
                    PlaySmallWinFeedback(result, winningReelIndices);
                    break;
                case SlotOutcome.BigWin:
                    PlayBigWinFeedback(result, winningReelIndices);
                    break;
                case SlotOutcome.Jackpot:
                    PlayJackpotFeedback(result, winningReelIndices);
                    break;
                case SlotOutcome.MegaJackpot:
                    PlayMegaJackpotFeedback(result, winningReelIndices);
                    break;
                case SlotOutcome.Draw:
                    PlayDrawFeedback(result, winningReelIndices);
                    break;
            }
        }

        private void PlayResultSound(SlotOutcome outcome)
        {
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySlotResultSound(outcome);
        }

        #endregion

        #region Mini Win Feedback

        private void PlayMiniWinFeedback(SlotResult result, int[] winningReelIndices)
        {
            _currentWinSequence = DOTween.Sequence();

            // Simple shimmer effect on winning symbols
            HighlightWinningSymbols(winningReelIndices, _miniWinColor, _miniWinPulseIntensity, 3);

            // Light sparkle particles
            SpawnWinParticles(_miniWinParticleCount, _miniWinColor, false);

            // Show win banner with countup
            ShowWinBanner("MINI WIN!", result.FinalReward, _miniWinColor, _miniWinGlowColor,
                _miniWinCountupDuration, 48, 36);

            // Auto hide after delay
            _currentWinSequence.AppendInterval(2f);
            _currentWinSequence.OnComplete(() => HideWinBanner());
        }

        #endregion

        #region Small Win Feedback

        private void PlaySmallWinFeedback(SlotResult result, int[] winningReelIndices)
        {
            _currentWinSequence = DOTween.Sequence();

            // Glow effect on winning symbols
            HighlightWinningSymbols(winningReelIndices, _smallWinColor, _smallWinPulseIntensity, 4);

            // Medium particle burst
            SpawnWinParticles(_smallWinParticleCount, _smallWinColor, false);

            // Show win banner
            ShowWinBanner("SMALL WIN!", result.FinalReward, _smallWinColor, _smallWinGlowColor,
                _smallWinCountupDuration, 56, 42);

            // Light screen glow
            PlayScreenGlow(_smallWinGlowColor, 0.4f, 2);

            _currentWinSequence.AppendInterval(2.5f);
            _currentWinSequence.OnComplete(() => HideWinBanner());
        }

        #endregion

        #region Big Win Feedback

        private void PlayBigWinFeedback(SlotResult result, int[] winningReelIndices)
        {
            _currentWinSequence = DOTween.Sequence();

            // Strong glow on winning symbols
            HighlightWinningSymbols(winningReelIndices, _bigWinColor, _bigWinPulseIntensity, 6);

            // Large particle burst
            SpawnWinParticles(_bigWinParticleCount, _bigWinColor, true);

            // Screen shake
            PlayScreenShake(_bigWinShakeStrength, 0.4f);

            // Screen glow
            PlayScreenGlow(_bigWinGlowColor, 0.5f, 3);

            // Show win banner with bigger text
            ShowWinBanner("BIG WIN!", result.FinalReward, _bigWinColor, _bigWinGlowColor,
                _bigWinCountupDuration, 64, 48);

            // Haptic feedback
            UIFeedback.TriggerHaptic(UIFeedback.HapticType.Medium);

            _currentWinSequence.AppendInterval(3f);
            _currentWinSequence.OnComplete(() => HideWinBanner());
        }

        #endregion

        #region Jackpot Feedback

        private void PlayJackpotFeedback(SlotResult result, int[] winningReelIndices)
        {
            _currentWinSequence = DOTween.Sequence();

            // Intense golden glow on ALL symbols (jackpot highlights multiple paylines)
            HighlightWinningSymbols(winningReelIndices, _jackpotColor, _jackpotPulseIntensity, 8);

            // Golden explosion particles
            SpawnWinParticles(_jackpotParticleCount, _jackpotColor, true);

            // Strong screen shake
            PlayScreenShake(_jackpotShakeStrength, 0.6f);

            // Golden screen glow with border
            PlayScreenGlow(_jackpotGlowColor, 0.6f, 5);

            // Screen flash
            PlayScreenFlash(new Color(1f, 0.9f, 0.4f, 0.5f), 0.3f);

            // Gold coin rain
            StartCoinRain(20, 2f);

            // Show win banner with large text
            ShowWinBanner("JACKPOT!", result.FinalReward, _jackpotColor, _jackpotGlowColor,
                _jackpotCountupDuration, 80, 56);

            // Strong haptic
            UIFeedback.TriggerHaptic(UIFeedback.HapticType.Heavy);

            _currentWinSequence.AppendInterval(4f);
            _currentWinSequence.OnComplete(() => HideWinBanner());
        }

        #endregion

        #region Mega Jackpot Feedback

        private void PlayMegaJackpotFeedback(SlotResult result, int[] winningReelIndices)
        {
            _megaJackpotCoroutine = StartCoroutine(MegaJackpotSequence(result, winningReelIndices));
        }

        private IEnumerator MegaJackpotSequence(SlotResult result, int[] winningReelIndices)
        {
            // Phase 1: Initial flash
            PlayScreenFlash(new Color(1f, 1f, 1f, 0.8f), 0.2f);
            yield return new WaitForSeconds(0.2f);

            // Phase 2: Rainbow/golden explosion
            HighlightWinningSymbols(winningReelIndices, _megaJackpotColor, _megaJackpotPulseIntensity, 12);

            // Phase 3: Massive particle burst in waves
            for (int i = 0; i < 3; i++)
            {
                Color burstColor = i switch
                {
                    0 => _megaJackpotColor,
                    1 => _jackpotColor,
                    _ => new Color(1f, 0.3f, 0.8f) // Magenta
                };
                SpawnWinParticles(_megaJackpotParticleCount / 3, burstColor, true);
                yield return new WaitForSeconds(0.3f);
            }

            // Phase 4: Strong screen effects
            PlayScreenShake(_megaJackpotShakeStrength, 1f);
            PlayScreenGlow(_megaJackpotGlowColor, 0.8f, 8);

            // Phase 5: Continuous screen flashes
            for (int i = 0; i < 5; i++)
            {
                Color flashColor = i % 2 == 0
                    ? new Color(1f, 0.8f, 0.2f, 0.4f)
                    : new Color(1f, 0.5f, 0.8f, 0.4f);
                PlayScreenFlash(flashColor, 0.15f);
                yield return new WaitForSeconds(0.25f);
            }

            // Phase 6: Heavy coin rain
            StartCoinRain(50, 4f);

            // Phase 7: Show mega banner
            ShowWinBanner("MEGA JACKPOT!", result.FinalReward, _megaJackpotColor, _megaJackpotGlowColor,
                _megaJackpotCountupDuration, 90, 64);

            // Phase 8: Continuous particle effects during countup
            float elapsed = 0;
            while (elapsed < _megaJackpotCountupDuration)
            {
                SpawnWinParticles(10, GetRainbowColor(elapsed * 2f), false);
                elapsed += 0.3f;
                yield return new WaitForSeconds(0.3f);
            }

            // Multiple haptic bursts
            for (int i = 0; i < 3; i++)
            {
                UIFeedback.TriggerHaptic(UIFeedback.HapticType.Heavy);
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(2f);
            HideWinBanner();
        }

        private Color GetRainbowColor(float t)
        {
            float hue = (t % 1f);
            return Color.HSVToRGB(hue, 0.8f, 1f);
        }

        #endregion

        #region Draw Feedback

        private void PlayDrawFeedback(SlotResult result, int[] winningReelIndices)
        {
            // Simple highlight
            HighlightWinningSymbols(winningReelIndices, Color.gray, 0.1f, 2);

            // Show brief banner
            ShowWinBanner("DRAW", result.FinalReward, Color.gray, new Color(0.5f, 0.5f, 0.5f, 0.3f),
                0.3f, 48, 36);

            DOVirtual.DelayedCall(1.5f, () => HideWinBanner());
        }

        #endregion

        #region Win Banner

        private void ShowWinBanner(string title, double amount, Color textColor, Color glowColor,
            float countupDuration, int titleFontSize, int amountFontSize)
        {
            if (_winBanner == null) return;

            _winBanner.SetActive(true);
            _winBanner.transform.SetAsLastSibling();

            // Setup text
            _winTitleText.text = title;
            _winTitleText.color = textColor;
            _winTitleText.fontSize = titleFontSize;

            _winAmountText.text = "+0";
            _winAmountText.color = new Color(textColor.r * 0.9f + 0.1f, textColor.g * 0.9f + 0.1f, textColor.b * 0.5f);
            _winAmountText.fontSize = amountFontSize;

            // Setup glow
            _winBannerGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0);

            // Entrance animation
            _winBannerGroup.alpha = 0;
            _winBanner.transform.localScale = Vector3.one * 0.5f;

            Sequence showSeq = DOTween.Sequence();
            showSeq.Append(_winBannerGroup.DOFade(1f, 0.3f));
            showSeq.Join(_winBanner.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            showSeq.Join(_winBannerGlow.DOFade(glowColor.a, 0.3f));

            // Title pulse animation
            _winTitleText.transform.DOKill();
            _winTitleText.transform.localScale = Vector3.one;
            _winTitleText.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5, 0.5f);

            // Glow pulse
            _winBannerGlow.DOFade(glowColor.a * 0.5f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

            // Countup animation
            PlayCountupAnimation(amount, countupDuration);
        }

        private void PlayCountupAnimation(double finalAmount, float duration)
        {
            _countupTween?.Kill();

            double currentValue = 0;
            _countupTween = DOTween.To(
                () => currentValue,
                x => {
                    currentValue = x;
                    _winAmountText.text = $"+{GoldManager.FormatNumber(currentValue)}";
                },
                finalAmount,
                duration
            ).SetEase(Ease.OutCubic);

            // Punch scale during countup for emphasis
            if (duration > 1f)
            {
                int pulseCount = Mathf.CeilToInt(duration / 0.5f);
                for (int i = 0; i < pulseCount; i++)
                {
                    float delay = 0.5f * i;
                    DOVirtual.DelayedCall(delay, () =>
                    {
                        if (_winAmountText != null)
                        {
                            _winAmountText.transform.DOKill();
                            _winAmountText.transform.localScale = Vector3.one;
                            _winAmountText.transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 3, 0.5f);
                        }
                    });
                }
            }
        }

        private void HideWinBanner()
        {
            if (_winBanner == null || !_winBanner.activeSelf) return;

            _countupTween?.Kill();
            _winBannerGlow.DOKill();

            Sequence hideSeq = DOTween.Sequence();
            hideSeq.Append(_winBannerGroup.DOFade(0f, 0.3f));
            hideSeq.Join(_winBanner.transform.DOScale(0.8f, 0.3f));
            hideSeq.OnComplete(() =>
            {
                _winBanner.SetActive(false);
                _winBanner.transform.localScale = Vector3.one;
                _isShowingFeedback = false;
            });
        }

        #endregion

        #region Symbol Highlight

        private void HighlightWinningSymbols(int[] indices, Color highlightColor, float intensity, int pulseCount)
        {
            if (indices == null || _reelSymbols == null || _reelFrames == null) return;

            foreach (int idx in indices)
            {
                if (idx < 0 || idx >= _reelSymbols.Length) continue;

                // Symbol glow/pulse
                if (_reelSymbols[idx] != null)
                {
                    Transform symbolTransform = _reelSymbols[idx].transform;
                    symbolTransform.DOKill();
                    symbolTransform.localScale = Vector3.one;

                    // Pulse animation
                    symbolTransform.DOScale(1f + intensity, 0.15f)
                        .SetLoops(pulseCount * 2, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine)
                        .OnComplete(() => symbolTransform.localScale = Vector3.one);
                }

                // Frame color flash
                if (idx < _reelFrames.Length && _reelFrames[idx] != null)
                {
                    Image frame = _reelFrames[idx];
                    Color originalColor = frame.color;

                    frame.DOKill();
                    frame.DOColor(highlightColor, 0.12f)
                        .SetLoops(pulseCount * 2, LoopType.Yoyo)
                        .OnComplete(() => frame.color = originalColor);
                }
            }
        }

        #endregion

        #region Screen Effects

        private void PlayScreenShake(float strength, float duration)
        {
            if (_mainCanvas == null) return;

            _mainCanvas.transform.DOKill();
            _mainCanvas.transform.DOShakePosition(duration, strength, 20, 90, false, true);
        }

        private void PlayScreenFlash(Color color, float duration)
        {
            if (_screenFlash == null) return;

            _screenFlash.gameObject.SetActive(true);
            _screenFlash.transform.SetAsLastSibling();
            _screenFlash.color = color;

            _screenFlash.DOKill();
            _screenFlash.DOFade(0, duration).OnComplete(() =>
            {
                _screenFlash.gameObject.SetActive(false);
            });
        }

        private void PlayScreenGlow(Color color, float duration, int loops)
        {
            if (_screenGlowEdges == null) return;

            foreach (var edge in _screenGlowEdges)
            {
                if (edge == null) continue;

                edge.gameObject.SetActive(true);
                edge.transform.SetAsLastSibling();
                edge.color = color;

                edge.DOKill();
                edge.DOFade(0f, duration / loops)
                    .SetLoops(loops, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        Color c = edge.color;
                        edge.color = new Color(c.r, c.g, c.b, 0f);
                        edge.gameObject.SetActive(false);
                    });
            }
        }

        #endregion

        #region Particles

        private void SpawnWinParticles(int count, Color color, bool burst)
        {
            if (_mainCanvas == null) return;

            Vector2 centerPos = _winBanner != null && _winBanner.activeSelf
                ? _winBanner.GetComponent<RectTransform>().anchoredPosition
                : Vector2.zero;

            float baseSpeed = burst ? 300f : 150f;
            float baseLifetime = burst ? 1.2f : 0.8f;

            for (int i = 0; i < count; i++)
            {
                GameObject particle = GetParticleFromPool();
                if (particle == null) continue;

                particle.SetActive(true);
                particle.transform.SetAsLastSibling();

                RectTransform rect = particle.GetComponent<RectTransform>();
                rect.anchoredPosition = centerPos + new Vector2(
                    UnityEngine.Random.Range(-50f, 50f),
                    UnityEngine.Random.Range(-30f, 30f));
                rect.localScale = Vector3.one * UnityEngine.Random.Range(0.5f, 1.5f);

                Image img = particle.GetComponent<Image>();
                Color particleColor = new Color(
                    color.r + UnityEngine.Random.Range(-0.1f, 0.1f),
                    color.g + UnityEngine.Random.Range(-0.1f, 0.1f),
                    color.b + UnityEngine.Random.Range(-0.1f, 0.1f),
                    1f
                );
                img.color = particleColor;

                // Movement
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = baseSpeed * UnityEngine.Random.Range(0.6f, 1.4f);
                float lifetime = baseLifetime * UnityEngine.Random.Range(0.8f, 1.2f);

                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 targetPos = rect.anchoredPosition + direction * speed * lifetime;
                targetPos.y -= UnityEngine.Random.Range(50f, 150f); // Gravity

                Sequence seq = DOTween.Sequence();
                seq.Append(rect.DOAnchorPos(targetPos, lifetime).SetEase(Ease.OutQuad));
                seq.Join(rect.DOScale(0f, lifetime).SetEase(Ease.InQuad));
                seq.Join(img.DOFade(0f, lifetime * 0.7f).SetDelay(lifetime * 0.3f));
                seq.Join(rect.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-720f, 720f)), lifetime, RotateMode.FastBeyond360));
                seq.OnComplete(() => ReturnParticleToPool(particle));
            }
        }

        private GameObject GetParticleFromPool()
        {
            if (_particlePool.Count > 0)
            {
                GameObject obj = _particlePool.Dequeue();
                _activeParticles.Add(obj);
                return obj;
            }
            else if (_activeParticles.Count > 0)
            {
                // Recycle oldest
                GameObject obj = _activeParticles[0];
                _activeParticles.RemoveAt(0);
                obj.transform.DOKill();
                _activeParticles.Add(obj);
                return obj;
            }
            return null;
        }

        private void ReturnParticleToPool(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();
            obj.GetComponent<Image>()?.DOKill();
            obj.SetActive(false);
            _activeParticles.Remove(obj);
            _particlePool.Enqueue(obj);
        }

        #endregion

        #region Coin Rain

        private void StartCoinRain(int count, float duration)
        {
            StartCoroutine(CoinRainCoroutine(count, duration));
        }

        private IEnumerator CoinRainCoroutine(int count, float duration)
        {
            float interval = duration / count;

            for (int i = 0; i < count; i++)
            {
                SpawnCoin();
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnCoin()
        {
            GameObject coin = GetCoinFromPool();
            if (coin == null) return;

            coin.SetActive(true);
            coin.transform.SetAsLastSibling();

            RectTransform rect = coin.GetComponent<RectTransform>();

            // Start from top of screen
            float screenWidth = _canvasRect.rect.width;
            float startX = UnityEngine.Random.Range(-screenWidth * 0.4f, screenWidth * 0.4f);
            float startY = _canvasRect.rect.height * 0.5f + 50f;

            rect.anchoredPosition = new Vector2(startX, startY);
            rect.localScale = Vector3.one * UnityEngine.Random.Range(0.6f, 1.2f);
            rect.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));

            Image img = coin.GetComponent<Image>();
            // 코인 스프라이트 원본 색상 사용
            img.color = Color.white;

            // Fall animation
            float endY = -_canvasRect.rect.height * 0.5f - 100f;
            float fallDuration = UnityEngine.Random.Range(1.5f, 2.5f);
            float horizontalDrift = UnityEngine.Random.Range(-100f, 100f);

            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOAnchorPosY(endY, fallDuration).SetEase(Ease.InQuad));
            seq.Join(rect.DOAnchorPosX(startX + horizontalDrift, fallDuration).SetEase(Ease.InOutSine));
            seq.Join(rect.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-1080f, 1080f)), fallDuration, RotateMode.FastBeyond360));
            seq.Join(img.DOFade(0f, fallDuration * 0.3f).SetDelay(fallDuration * 0.7f));
            seq.OnComplete(() => ReturnCoinToPool(coin));
        }

        private GameObject GetCoinFromPool()
        {
            if (_coinPool.Count > 0)
            {
                GameObject obj = _coinPool.Dequeue();
                _activeCoins.Add(obj);
                return obj;
            }
            else if (_activeCoins.Count > 0)
            {
                GameObject obj = _activeCoins[0];
                _activeCoins.RemoveAt(0);
                obj.transform.DOKill();
                _activeCoins.Add(obj);
                return obj;
            }
            return null;
        }

        private void ReturnCoinToPool(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();
            obj.GetComponent<Image>()?.DOKill();
            obj.SetActive(false);
            _activeCoins.Remove(obj);
            _coinPool.Enqueue(obj);
        }

        #endregion

        #region Cleanup

        public void StopAllFeedback()
        {
            _currentWinSequence?.Kill();
            _countupTween?.Kill();

            if (_megaJackpotCoroutine != null)
            {
                StopCoroutine(_megaJackpotCoroutine);
                _megaJackpotCoroutine = null;
            }

            // Stop all symbol animations
            if (_reelSymbols != null)
            {
                foreach (var symbol in _reelSymbols)
                {
                    if (symbol != null)
                    {
                        symbol.transform.DOKill();
                        symbol.transform.localScale = Vector3.one;
                    }
                }
            }

            if (_reelFrames != null)
            {
                foreach (var frame in _reelFrames)
                {
                    if (frame != null)
                    {
                        frame.DOKill();
                    }
                }
            }

            // Stop screen effects
            if (_screenFlash != null)
            {
                _screenFlash.DOKill();
                _screenFlash.gameObject.SetActive(false);
            }

            if (_screenGlowEdges != null)
            {
                foreach (var edge in _screenGlowEdges)
                {
                    if (edge != null)
                    {
                        edge.DOKill();
                        edge.gameObject.SetActive(false);
                    }
                }
            }

            // Return all active particles to pool
            foreach (var particle in _activeParticles.ToArray())
            {
                ReturnParticleToPool(particle);
            }

            foreach (var coin in _activeCoins.ToArray())
            {
                ReturnCoinToPool(coin);
            }

            _isShowingFeedback = false;
        }

        private void OnDestroy()
        {
            StopAllFeedback();

            // Cleanup tweens
            if (_winBanner != null)
            {
                _winBanner.transform.DOKill();
                _winBannerGroup?.DOKill();
                _winBannerGlow?.DOKill();
                _winTitleText?.transform.DOKill();
                _winAmountText?.transform.DOKill();
            }

            // Cleanup pools
            while (_particlePool.Count > 0)
            {
                var obj = _particlePool.Dequeue();
                if (obj != null) Destroy(obj);
            }

            while (_coinPool.Count > 0)
            {
                var obj = _coinPool.Dequeue();
                if (obj != null) Destroy(obj);
            }

            if (_particlePrefab != null) Destroy(_particlePrefab);
            if (_coinPrefab != null) Destroy(_coinPrefab);
        }

        #endregion

        #region Public Properties

        public bool IsShowingFeedback => _isShowingFeedback;

        #endregion
    }
}
