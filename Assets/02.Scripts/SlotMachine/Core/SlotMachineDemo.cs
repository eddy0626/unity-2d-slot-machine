using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace SlotMachine.Core
{
    /// <summary>
    /// Config 없이도 바로 실행 가능한 슬롯머신 데모
    /// 씬에 이 컴포넌트를 추가하면 자동으로 모든 것이 생성됩니다
    /// DOTween Pro를 사용한 부드러운 애니메이션 지원
    /// </summary>
    public class SlotMachineDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private int startCoins = 10000;
        [SerializeField] private int currentBet = 50;
        [SerializeField] private int[] betOptions = { 10, 25, 50, 100, 250 };
        [SerializeField] private int betIndex = 2;

        [Header("Spin Settings")]
        [SerializeField] private float spinDuration = 2.0f;
        [SerializeField] private float reelStopDelay = 0.3f;

        [Header("DOTween Animation Settings")]
        [SerializeField] private float symbolSpinSpeed = 0.08f;
        [SerializeField] private float reelBounceStrength = 20f;
        [SerializeField] private float winShakeStrength = 10f;
        [SerializeField] private float coinCountDuration = 0.5f;

        [Header("Advanced Features")]
        [SerializeField] private bool enableAutoSpin = true;
        [SerializeField] private bool enableTurbo = true;
        [SerializeField] private float turboMultiplier = 0.5f;
        [SerializeField] private float autoSpinDelay = 0.2f;

        [Header("Jackpot Settings")]
        [SerializeField] private int jackpotStart = 5000;
        [SerializeField] [Range(0f, 0.2f)] private float jackpotContributionRate = 0.02f;
        [SerializeField] private int jackpotSymbolIndex = 12;

        [Header("Scatter / Free Spins")]
        [SerializeField] private int wildSymbolIndex = 12;
        [SerializeField] private int scatterSymbolIndex = 13;
        [SerializeField] private int scatterFreeSpins = 5;
        [SerializeField] private int scatterExtraSpinsPerAdditional = 2;
        [SerializeField] private int scatterPayoutMultiplier = 2;

        [Header("RNG Weights (Optional)")]
        [SerializeField] private int[] symbolWeights;

        [Header("Sprites (Auto-loaded from Resources)")]
        [SerializeField] private Sprite[] _symbolSprites;
        [SerializeField] private Sprite _machineFrameSprite;


        // UI References
        private Canvas _canvas;
        private TextMeshProUGUI _coinText;
        private TextMeshProUGUI _betText;
        private TextMeshProUGUI _winText;
        private Button _spinButton;
        private TextMeshProUGUI _spinButtonText;
        private Button _betUpButton;
        private Button _betDownButton;
        private Button _autoSpinButton;
        private TextMeshProUGUI _autoSpinText;
        private Button _turboButton;
        private TextMeshProUGUI _turboText;
        private TextMeshProUGUI _jackpotText;
        private TextMeshProUGUI _freeSpinsText;

        // Reel data
        private Image[][] _symbolImages;
        private RectTransform[][] _symbolTransforms;
        private Vector2[][] _originalSymbolPositions; // 원래 위치 저장
        private int[][] _reelResults;
        private bool _isSpinning;
        private int _currentCoins;
        private int _displayedCoins;
        private int _jackpotValue;
        private int _freeSpinsRemaining;
        private bool _autoSpinEnabled;
        private bool _turboEnabled;
        private CanvasGroup _canvasGroup;
        private RectTransform _reelContainer;
        private int _lastScatterCount;
        private int _lastFreeSpinsAwarded;
        private bool _lastJackpotHit;
        private readonly List<int> _lastWinningPositions = new List<int>();
        private readonly List<int> _lastScatterPositions = new List<int>();

        private static readonly int[][] _paylines = new int[][]
        {
            new int[] { 3, 4, 5 },  // Center
            new int[] { 0, 1, 2 },  // Top
            new int[] { 6, 7, 8 },  // Bottom
            new int[] { 0, 4, 8 },  // Diagonal ↘
            new int[] { 6, 4, 2 }   // Diagonal ↗
        };

        // Symbol colors (demo용 색상으로 구분) - 16개
        private readonly Color[] _symbolColors = new Color[]
        {
            new Color(1f, 0.5f, 0.8f),    // 0: Dealer Girl - Pink
            new Color(0.4f, 0.8f, 1f),    // 1: Bunny Girl - Cyan
            new Color(0.6f, 0.3f, 0.9f),  // 2: Mage Girl - Purple
            new Color(0.2f, 0.8f, 0.8f),  // 3: Pirate Girl - Teal
            new Color(0.8f, 0.9f, 1f),    // 4: Maid - Light Blue
            new Color(1f, 0.6f, 0.9f),    // 5: Fairy - Light Pink
            new Color(1f, 0.84f, 0f),     // 6: Gold Coin - Gold
            new Color(0.6f, 0.9f, 1f),    // 7: Diamond - Sky Blue
            new Color(1f, 0.5f, 0.7f),    // 8: Pink Coin - Pink
            new Color(0.3f, 0.9f, 0.4f),  // 9: Clover - Green
            new Color(0.9f, 0.3f, 0.3f),  // 10: Ruby Clover - Red
            new Color(0.5f, 0.3f, 0.9f),  // 11: Vortex - Dark Purple
            new Color(1f, 0.9f, 0.3f),    // 12: WILD - Yellow
            new Color(0.9f, 0.5f, 1f),    // 13: Scatter - Magenta
            new Color(0.4f, 0.9f, 1f),    // 14: Gift Box - Aqua
            new Color(1f, 0.7f, 0.3f),    // 15: Chip - Orange
        };

        private readonly string[] _symbolNames = new string[]
        {
            "★", "♦", "♣", "♥", "7", "A", "K", "Q", "J", "10",
            "9", "8", "W", "S", "B", "C"
        };

        // 16개 심볼 배당금 (고가치 -> 저가치 순서)
        private readonly int[] _symbolPayouts = new int[]
        {
            50, 40, 30, 25, // 고가치 (0-3)
            20, 15, 10, 8,  // 중가치 (4-7)
            5, 4, 3, 2,     // 저가치 (8-11)
            100, 0, 15, 10  // 특수: WILD(12), Scatter(13), Bonus(14), Chip(15)
        };

        private void Start()
        {
            // DOTween 초기화
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(200, 50);

            _currentCoins = startCoins;
            _displayedCoins = startCoins;
            currentBet = betOptions[betIndex];
            _jackpotValue = jackpotStart;
            _autoSpinEnabled = false;
            _turboEnabled = false;
            CreateUI();
            UpdateStatusUI();
            UpdateAutoSpinButton();
            UpdateTurboButton();
            UpdateSpinButtonLabel();

            // 시작 시 페이드 인 효과
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            }
        }

        private void OnDestroy()
        {
            // DOTween 정리
            DOTween.Kill(this);
        }

        private void CreateUI()
        {
            // EventSystem
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            // Canvas
            GameObject canvasObj = new GameObject("DemoCanvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            _canvasGroup = canvasObj.AddComponent<CanvasGroup>();

            // Main Container
            GameObject main = CreatePanel("Main", _canvas.transform);
            SetFullStretch(main.GetComponent<RectTransform>());
            main.GetComponent<Image>().color = new Color(0.06f, 0.03f, 0.1f);

            // Title
            CreateTitle(main.transform);

            // Reel Container
            CreateReels(main.transform);

            // Status Panel (Jackpot / Free Spins)
            CreateStatusPanel(main.transform);

            // UI Panel
            CreateUIPanel(main.transform);

            UpdateUI();
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "NEON SLOTS";
            title.fontSize = 64;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.2f, 0.8f);
            title.fontStyle = FontStyles.Bold;

            RectTransform rt = titleObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(600, 80);

            // 타이틀 입장 애니메이션
            rt.anchoredPosition = new Vector2(0, 100);
            rt.DOAnchorPosY(-20, 0.8f).SetEase(Ease.OutBounce).SetDelay(0.2f);

            // 네온 글로우 펄스 효과 (지속적)
            title.DOColor(new Color(1f, 0.4f, 1f), 1.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void CreateReels(Transform parent)
        {
            // 항상 임시 틀 사용 (프레임 스프라이트 비활성화)
            CreateReelsWithFrame(parent);
            return;

            // 아래는 기존 코드 (현재 미사용)
            /*
            GameObject container = CreatePanel("ReelContainer", parent);
            RectTransform containerRT = container.GetComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.5f);
            containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.anchoredPosition = new Vector2(0, 30);
            containerRT.sizeDelta = new Vector2(480, 450);
            container.GetComponent<Image>().color = new Color(0.1f, 0.06f, 0.15f);
            _reelContainer = containerRT;

            // Border glow
            GameObject border = CreatePanel("Border", container.transform);
            RectTransform borderRT = border.GetComponent<RectTransform>();
            SetFullStretch(borderRT);
            borderRT.offsetMin = new Vector2(-4, -4);
            borderRT.offsetMax = new Vector2(4, 4);
            border.GetComponent<Image>().color = new Color(0f, 1f, 0.8f, 0.4f);
            border.transform.SetAsFirstSibling();

            // Create 3 reels
            _symbolImages = new Image[3][];
            _symbolTransforms = new RectTransform[3][];
            _originalSymbolPositions = new Vector2[3][];
            _reelResults = new int[3][];

            for (int r = 0; r < 3; r++)
            {
                CreateReel(container.transform, r);
            }
            */
        }

        private void CreateStatusPanel(Transform parent)
        {
            GameObject status = CreatePanel("StatusPanel", parent);
            RectTransform statusRT = status.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0.5f, 0.5f);
            statusRT.anchorMax = new Vector2(0.5f, 0.5f);
            statusRT.anchoredPosition = new Vector2(0, 220);
            statusRT.sizeDelta = new Vector2(600, 60);

            Image bg = status.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.06f, 0.15f, 0.6f);
            bg.raycastTarget = false;

            GameObject jackpotObj = CreateText("JackpotText", status.transform, "JACKPOT 0", 28, new Color(1f, 0.84f, 0.2f));
            _jackpotText = jackpotObj.GetComponent<TextMeshProUGUI>();
            _jackpotText.fontStyle = FontStyles.Bold;
            _jackpotText.alignment = TextAlignmentOptions.Left;
            RectTransform jackpotRT = jackpotObj.GetComponent<RectTransform>();
            jackpotRT.anchorMin = new Vector2(0, 0.5f);
            jackpotRT.anchorMax = new Vector2(0, 0.5f);
            jackpotRT.pivot = new Vector2(0, 0.5f);
            jackpotRT.anchoredPosition = new Vector2(20, 0);
            jackpotRT.sizeDelta = new Vector2(280, 50);

            GameObject freeObj = CreateText("FreeSpinsText", status.transform, "FREE 0", 24, new Color(1f, 0.4f, 1f));
            _freeSpinsText = freeObj.GetComponent<TextMeshProUGUI>();
            _freeSpinsText.fontStyle = FontStyles.Bold;
            _freeSpinsText.alignment = TextAlignmentOptions.Right;
            RectTransform freeRT = freeObj.GetComponent<RectTransform>();
            freeRT.anchorMin = new Vector2(1, 0.5f);
            freeRT.anchorMax = new Vector2(1, 0.5f);
            freeRT.pivot = new Vector2(1, 0.5f);
            freeRT.anchoredPosition = new Vector2(-20, 0);
            freeRT.sizeDelta = new Vector2(240, 50);
        }

        private void CreateReelsWithFrame(Transform parent)
        {
            // ========== 메인 컨테이너 ==========
            GameObject machineContainer = new GameObject("SlotMachineContainer");
            machineContainer.transform.SetParent(parent, false);
            RectTransform containerRT = machineContainer.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.5f, 0.5f);
            containerRT.anchorMax = new Vector2(0.5f, 0.5f);
            containerRT.anchoredPosition = new Vector2(0, 30);
            containerRT.sizeDelta = new Vector2(500, 350);
            _reelContainer = containerRT;

            // ========== 임시 프레임 틀 (배경) ==========
            GameObject frameBg = CreatePanel("FrameBackground", machineContainer.transform);
            RectTransform frameBgRT = frameBg.GetComponent<RectTransform>();
            frameBgRT.anchorMin = Vector2.zero;
            frameBgRT.anchorMax = Vector2.one;
            frameBgRT.offsetMin = Vector2.zero;
            frameBgRT.offsetMax = Vector2.zero;
            Image frameBgImage = frameBg.GetComponent<Image>();
            frameBgImage.color = new Color(0.3f, 0.15f, 0.4f, 1f); // 보라색 프레임 배경

            // ========== 3개의 릴 창 (각각 마스크 적용) ==========
            _symbolImages = new Image[3][];
            _symbolTransforms = new RectTransform[3][];
            _originalSymbolPositions = new Vector2[3][];
            _reelResults = new int[3][];

            float windowWidth = 140f;  // 각 창 너비
            float windowHeight = 250f; // 각 창 높이
            float gap = 15f;           // 창 사이 간격
            float totalWidth = (windowWidth * 3) + (gap * 2);
            float startX = -totalWidth / 2f + windowWidth / 2f;

            for (int r = 0; r < 3; r++)
            {
                // 각 릴의 창 (마스크 영역)
                GameObject reelWindow = CreatePanel($"ReelWindow_{r}", machineContainer.transform);
                RectTransform windowRT = reelWindow.GetComponent<RectTransform>();
                windowRT.anchorMin = new Vector2(0.5f, 0.5f);
                windowRT.anchorMax = new Vector2(0.5f, 0.5f);
                windowRT.anchoredPosition = new Vector2(startX + r * (windowWidth + gap), 10);
                windowRT.sizeDelta = new Vector2(windowWidth, windowHeight);

                // 창 배경 (어두운 색)
                Image windowBg = reelWindow.GetComponent<Image>();
                windowBg.color = new Color(0.02f, 0.01f, 0.03f, 1f);

                // 마스크 적용 - 심볼이 이 영역 밖으로 나가면 안 보임
                reelWindow.AddComponent<RectMask2D>();

                // 이 창 안에 릴 생성
                CreateReelInWindow(reelWindow.transform, r, windowWidth, windowHeight);
            }

            // ========== 임시 프레임 테두리 (맨 앞) ==========
            GameObject frameBorder = CreatePanel("FrameBorder", machineContainer.transform);
            RectTransform borderRT = frameBorder.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = new Vector2(-5, -5);
            borderRT.offsetMax = new Vector2(5, 5);
            Image borderImage = frameBorder.GetComponent<Image>();
            borderImage.color = new Color(0.6f, 0.3f, 0.7f, 1f); // 밝은 보라색 테두리

            // Outline 효과를 위해 중앙을 투명하게
            // (실제로는 9-slice나 별도 이미지 필요, 여기선 단순 테두리)
            Outline outline = frameBorder.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.8f, 1f, 0.8f);
            outline.effectDistance = new Vector2(3, 3);

            // 중앙 투명 처리 (테두리만 보이게)
            borderImage.color = Color.clear;
            borderImage.raycastTarget = false;
        }

        private void CreateReelInWindow(Transform parent, int reelIndex, float windowWidth, float windowHeight)
        {
            // 릴 컨테이너 (심볼들을 담음)
            GameObject reel = new GameObject($"Reel_{reelIndex}");
            reel.transform.SetParent(parent, false);
            RectTransform reelRT = reel.AddComponent<RectTransform>();
            reelRT.anchorMin = new Vector2(0.5f, 0.5f);
            reelRT.anchorMax = new Vector2(0.5f, 0.5f);
            reelRT.anchoredPosition = Vector2.zero;
            reelRT.sizeDelta = new Vector2(windowWidth, windowHeight * 2); // 스크롤 여유

            _symbolImages[reelIndex] = new Image[3];
            _symbolTransforms[reelIndex] = new RectTransform[3];
            _originalSymbolPositions[reelIndex] = new Vector2[3];
            _reelResults[reelIndex] = new int[3];

            // 심볼 크기 (창 높이의 1/3)
            float symbolHeight = windowHeight / 3f;
            float actualSymbolSize = Mathf.Min(symbolHeight - 8, windowWidth - 10);

            for (int s = 0; s < 3; s++)
            {
                CreateSymbolInWindow(reel.transform, reelIndex, s, actualSymbolSize, symbolHeight);
            }
        }

        private void CreateSymbolInWindow(Transform parent, int reelIndex, int symbolIndex, float symbolSize, float slotHeight)
        {
            float startY = slotHeight; // 상단부터

            GameObject symbol = CreatePanel($"Symbol_{symbolIndex}", parent);
            RectTransform symbolRT = symbol.GetComponent<RectTransform>();
            symbolRT.anchoredPosition = new Vector2(0, startY - (symbolIndex * slotHeight));
            symbolRT.sizeDelta = new Vector2(symbolSize, symbolSize);

            Image img = symbol.GetComponent<Image>();
            int randomSymbol = Random.Range(0, _symbolSprites != null && _symbolSprites.Length > 0 ? _symbolSprites.Length : _symbolColors.Length);
            _symbolImages[reelIndex][symbolIndex] = img;
            _symbolTransforms[reelIndex][symbolIndex] = symbolRT;
            _originalSymbolPositions[reelIndex][symbolIndex] = symbolRT.anchoredPosition;
            _reelResults[reelIndex][symbolIndex] = randomSymbol;

            // 심볼 표시
            SetSymbolDisplay(reelIndex, symbolIndex, randomSymbol);
        }

        private void CreateReel(Transform parent, int reelIndex)
        {
            float reelWidth = 150f;
            float startX = -reelWidth;

            GameObject reel = CreatePanel($"Reel_{reelIndex}", parent);
            RectTransform reelRT = reel.GetComponent<RectTransform>();
            reelRT.anchoredPosition = new Vector2(startX + (reelIndex * reelWidth), 0);
            reelRT.sizeDelta = new Vector2(reelWidth - 8, 430);
            reel.GetComponent<Image>().color = new Color(0.05f, 0.02f, 0.08f);

            _symbolImages[reelIndex] = new Image[3];
            _symbolTransforms[reelIndex] = new RectTransform[3];
            _originalSymbolPositions[reelIndex] = new Vector2[3];
            _reelResults[reelIndex] = new int[3];

            for (int s = 0; s < 3; s++)
            {
                CreateSymbol(reel.transform, reelIndex, s);
            }
        }

        private void CreateSymbol(Transform parent, int reelIndex, int symbolIndex)
        {
            float symbolHeight = 140f;
            float startY = symbolHeight;

            GameObject symbol = CreatePanel($"Symbol_{symbolIndex}", parent);
            RectTransform symbolRT = symbol.GetComponent<RectTransform>();
            symbolRT.anchoredPosition = new Vector2(0, startY - (symbolIndex * symbolHeight));
            symbolRT.sizeDelta = new Vector2(130, 130);

            Image img = symbol.GetComponent<Image>();
            int randomSymbol = GetRandomSymbolIndex();
            bool useSprite = _symbolSprites != null && _symbolSprites.Length > 0 &&
                             randomSymbol < _symbolSprites.Length && _symbolSprites[randomSymbol] != null;

            if (useSprite)
            {
                img.sprite = _symbolSprites[randomSymbol];
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = _symbolColors[randomSymbol % _symbolColors.Length];
            }
            _symbolImages[reelIndex][symbolIndex] = img;
            _symbolTransforms[reelIndex][symbolIndex] = symbolRT;
            _originalSymbolPositions[reelIndex][symbolIndex] = symbolRT.anchoredPosition; // 원래 위치 저장
            _reelResults[reelIndex][symbolIndex] = randomSymbol;

            // Symbol text
            GameObject textObj = new GameObject("SymbolText");
            textObj.transform.SetParent(symbol.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = useSprite ? "" : _symbolNames[randomSymbol % _symbolNames.Length];
            text.fontSize = 48;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            SetFullStretch(textRT);
        }

        private void CreateUIPanel(Transform parent)
        {
            GameObject panel = CreatePanel("UIPanel", parent);
            RectTransform panelRT = panel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(1, 0);
            panelRT.pivot = new Vector2(0.5f, 0);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(0, 160);
            panel.GetComponent<Image>().color = new Color(0.08f, 0.04f, 0.12f, 0.95f);

            // Coins
            CreateCoinsDisplay(panel.transform);

            // Bet
            CreateBetDisplay(panel.transform);

            // Spin Button
            CreateSpinButton(panel.transform);

            // Auto/Turbo Controls
            CreateAutoControls(panel.transform);

            // Win
            CreateWinDisplay(panel.transform);
        }

        private void CreateCoinsDisplay(Transform parent)
        {
            GameObject coins = new GameObject("CoinsPanel");
            coins.transform.SetParent(parent, false);
            coins.AddComponent<RectTransform>();

            RectTransform rt = coins.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(40, 0);
            rt.sizeDelta = new Vector2(250, 70);

            // Label
            GameObject label = CreateText("Label", coins.transform, "COINS", 20, Color.white);
            label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

            // Value
            GameObject value = CreateText("Value", coins.transform, "10,000", 36, new Color(1f, 0.84f, 0f));
            value.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);
            _coinText = value.GetComponent<TextMeshProUGUI>();
        }

        private void CreateBetDisplay(Transform parent)
        {
            GameObject bet = new GameObject("BetPanel");
            bet.transform.SetParent(parent, false);
            bet.AddComponent<RectTransform>();

            RectTransform rt = bet.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-160, 0);
            rt.sizeDelta = new Vector2(200, 70);

            // Label
            GameObject label = CreateText("Label", bet.transform, "BET", 18, Color.white);
            label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 28);

            // - Button
            _betDownButton = CreateButton("BetDown", bet.transform, "-", new Vector2(-60, -5), new Vector2(40, 40));
            _betDownButton.onClick.AddListener(DecreaseBet);
            AddButtonBounceEffect(_betDownButton);

            // Value
            GameObject value = CreateText("Value", bet.transform, "50", 30, Color.white);
            value.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -5);
            _betText = value.GetComponent<TextMeshProUGUI>();

            // + Button
            _betUpButton = CreateButton("BetUp", bet.transform, "+", new Vector2(60, -5), new Vector2(40, 40));
            _betUpButton.onClick.AddListener(IncreaseBet);
            AddButtonBounceEffect(_betUpButton);
        }

        private void CreateSpinButton(Transform parent)
        {
            _spinButton = CreateButton("SpinButton", parent, "SPIN", new Vector2(160, 0), new Vector2(130, 60));
            RectTransform rt = _spinButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);

            _spinButton.GetComponent<Image>().color = new Color(0f, 0.9f, 0.7f);
            _spinButtonText = _spinButton.GetComponentInChildren<TextMeshProUGUI>();
            _spinButtonText.fontSize = 28;
            _spinButton.onClick.AddListener(Spin);

            // DOTween 버튼 바운스 효과 추가
            AddButtonBounceEffect(_spinButton);
        }

        private void CreateAutoControls(Transform parent)
        {
            if (enableTurbo)
            {
                _turboButton = CreateButton("TurboButton", parent, "TURBO OFF", new Vector2(160, 50), new Vector2(120, 32));
                _turboButton.GetComponent<Image>().color = new Color(0.2f, 0.12f, 0.3f);
                _turboText = _turboButton.GetComponentInChildren<TextMeshProUGUI>();
                if (_turboText != null)
                    _turboText.fontSize = 18;
                _turboButton.onClick.AddListener(ToggleTurbo);
                AddButtonBounceEffect(_turboButton);
            }

            if (enableAutoSpin)
            {
                _autoSpinButton = CreateButton("AutoSpinButton", parent, "AUTO OFF", new Vector2(160, -50), new Vector2(120, 32));
                _autoSpinButton.GetComponent<Image>().color = new Color(0.2f, 0.12f, 0.3f);
                _autoSpinText = _autoSpinButton.GetComponentInChildren<TextMeshProUGUI>();
                if (_autoSpinText != null)
                    _autoSpinText.fontSize = 18;
                _autoSpinButton.onClick.AddListener(ToggleAutoSpin);
                AddButtonBounceEffect(_autoSpinButton);
            }
        }

        private void AddButtonBounceEffect(Button button)
        {
            RectTransform rt = button.GetComponent<RectTransform>();

            button.onClick.AddListener(() =>
            {
                // 버튼 클릭 시 바운스 효과
                rt.DOKill();
                rt.localScale = Vector3.one;
                rt.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f);
            });
        }

        private void CreateWinDisplay(Transform parent)
        {
            GameObject win = new GameObject("WinPanel");
            win.transform.SetParent(parent, false);
            win.AddComponent<RectTransform>();

            RectTransform rt = win.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-40, 0);
            rt.sizeDelta = new Vector2(250, 70);

            // Label
            GameObject label = CreateText("Label", win.transform, "WIN", 20, Color.white);
            label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

            // Value
            GameObject value = CreateText("Value", win.transform, "", 36, new Color(0f, 1f, 0.6f));
            value.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -15);
            _winText = value.GetComponent<TextMeshProUGUI>();
        }

        #region Game Logic

        private bool CanSpin()
        {
            return !_isSpinning && (_freeSpinsRemaining > 0 || _currentCoins >= currentBet);
        }

        private int GetSymbolCount()
        {
            return _symbolSprites != null && _symbolSprites.Length > 0 ? _symbolSprites.Length : _symbolColors.Length;
        }

        private int GetRandomSymbolIndex()
        {
            int symbolCount = GetSymbolCount();

            if (symbolWeights != null && symbolWeights.Length >= symbolCount)
            {
                int totalWeight = 0;
                for (int i = 0; i < symbolCount; i++)
                {
                    totalWeight += Mathf.Max(0, symbolWeights[i]);
                }

                if (totalWeight > 0)
                {
                    int roll = Random.Range(0, totalWeight);
                    int cumulative = 0;
                    for (int i = 0; i < symbolCount; i++)
                    {
                        cumulative += Mathf.Max(0, symbolWeights[i]);
                        if (roll < cumulative)
                            return i;
                    }
                }
            }

            return Random.Range(0, symbolCount);
        }

        private bool IsWild(int symbolIndex)
        {
            return symbolIndex == wildSymbolIndex;
        }

        private bool IsScatter(int symbolIndex)
        {
            return symbolIndex == scatterSymbolIndex;
        }

        private bool IsJackpotSymbol(int symbolIndex)
        {
            return symbolIndex == jackpotSymbolIndex;
        }

        private void ToggleAutoSpin()
        {
            if (!enableAutoSpin)
                return;

            _autoSpinEnabled = !_autoSpinEnabled;
            UpdateAutoSpinButton();

            if (_autoSpinEnabled)
            {
                QueueNextAutoSpin();
            }
        }

        private void ToggleTurbo()
        {
            if (!enableTurbo)
                return;

            _turboEnabled = !_turboEnabled;
            UpdateTurboButton();
        }

        private void Spin()
        {
            if (_isSpinning) return;
            bool isFreeSpin = _freeSpinsRemaining > 0;

            if (!isFreeSpin && _currentCoins < currentBet)
            {
                // 코인 부족 애니메이션
                _winText.text = "NO COINS!";
                _winText.color = Color.red;

                _winText.transform.DOKill();
                _winText.transform.localScale = Vector3.one;
                _winText.transform.DOShakePosition(0.5f, 10f, 20, 90, false, true);
                _winText.DOFade(1f, 0.1f).OnComplete(() =>
                    _winText.DOFade(0f, 1f).SetDelay(1f));

                // 코인 텍스트도 강조
                if (_coinText != null)
                {
                    _coinText.DOKill();
                    _coinText.DOColor(Color.red, 0.1f).SetLoops(4, LoopType.Yoyo)
                        .OnComplete(() => _coinText.color = new Color(1f, 0.84f, 0f));
                    _coinText.transform.DOShakePosition(0.3f, 5f, 15, 90, false, true);
                }
                if (_autoSpinEnabled)
                {
                    _autoSpinEnabled = false;
                    UpdateAutoSpinButton();
                }
                return;
            }

            if (!isFreeSpin)
            {
                _currentCoins -= currentBet;
                _displayedCoins = _currentCoins; // 즉시 반영
                AddJackpotContribution();
            }
            else
            {
                _freeSpinsRemaining--;
                UpdateStatusUI();
                UpdateSpinButtonLabel();
            }

            UpdateUI();
            StartCoroutine(SpinCoroutine());
        }

        private IEnumerator SpinCoroutine()
        {
            _isSpinning = true;
            _spinButton.interactable = false;
            _winText.text = "";
            _winText.transform.localScale = Vector3.one;
            if (_betUpButton != null) _betUpButton.interactable = false;
            if (_betDownButton != null) _betDownButton.interactable = false;
            UpdateSpinButtonLabel();

            // 모든 심볼 위치/스케일 리셋
            ResetAllSymbolTransforms();

            // Generate results
            for (int r = 0; r < 3; r++)
            {
                for (int s = 0; s < 3; s++)
                {
                    _reelResults[r][s] = GetRandomSymbolIndex();
                }
            }

            // 스핀 시작 - 각 릴 시작 애니메이션
            for (int r = 0; r < 3; r++)
            {
                for (int s = 0; s < 3; s++)
                {
                    if (_symbolTransforms[r][s] != null)
                    {
                        // 시작 시 살짝 위로 튀어오르는 효과 (원래 위치 기준)
                        _symbolTransforms[r][s].DOKill();
                        Vector2 originalPos = _originalSymbolPositions[r][s];
                        _symbolTransforms[r][s].DOAnchorPosY(originalPos.y + 20f, 0.1f)
                            .SetEase(Ease.OutQuad);
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);

            // DOTween 기반 스핀 애니메이션
            float speedScale = _turboEnabled ? Mathf.Clamp(turboMultiplier, 0.2f, 1f) : 1f;
            float spinInterval = Mathf.Max(0.02f, symbolSpinSpeed * speedScale);
            float accelerationTime = 0.3f * speedScale;
            float currentInterval = spinInterval * 3f; // 느리게 시작
            float elapsed = 0f;
            bool[] reelStopped = new bool[3];

            // 가속 단계
            while (elapsed < accelerationTime)
            {
                currentInterval = Mathf.Lerp(spinInterval * 3f, spinInterval, elapsed / accelerationTime);

                for (int r = 0; r < 3; r++)
                {
                    for (int s = 0; s < 3; s++)
                    {
                        int randomSymbol = GetRandomSymbolIndex();
                        SetSymbolDisplayWithAnimation(r, s, randomSymbol, currentInterval * 0.5f);
                    }
                }

                yield return new WaitForSeconds(currentInterval);
                elapsed += currentInterval;
            }

            // 일정 속도 스핀
            elapsed = 0f;
            float scaledSpinDuration = spinDuration * speedScale;
            while (elapsed < scaledSpinDuration)
            {
                for (int r = 0; r < 3; r++)
                {
                    if (!reelStopped[r])
                    {
                        for (int s = 0; s < 3; s++)
                        {
                            int randomSymbol = GetRandomSymbolIndex();
                            SetSymbolDisplayWithAnimation(r, s, randomSymbol, spinInterval * 0.5f);
                        }
                    }
                }

                yield return new WaitForSeconds(spinInterval);
                elapsed += spinInterval;
            }

            // 순차적 릴 정지 (DOTween 바운스 효과)
            for (int r = 0; r < 3; r++)
            {
                reelStopped[r] = true;

                // 감속 효과
                for (int i = 0; i < 3; i++)
                {
                    float slowInterval = spinInterval * (1f + i * 0.5f);
                    for (int s = 0; s < 3; s++)
                    {
                        int randomSymbol = GetRandomSymbolIndex();
                        SetSymbolDisplayWithAnimation(r, s, randomSymbol, slowInterval * 0.5f);
                    }
                    yield return new WaitForSeconds(slowInterval);
                }

                // 최종 결과 표시 + 바운스
                for (int s = 0; s < 3; s++)
                {
                    SetSymbolDisplay(r, s, _reelResults[r][s]);

                    if (_symbolTransforms[r][s] != null)
                    {
                        RectTransform rt = _symbolTransforms[r][s];
                        rt.DOKill();

                        // 바운스 효과 (원래 위치 기준)
                        Vector2 originalPos = _originalSymbolPositions[r][s];
                        rt.anchoredPosition = originalPos; // 먼저 원래 위치로 리셋
                        rt.localScale = Vector3.one;

                        Sequence bounceSeq = DOTween.Sequence();
                        bounceSeq.Append(rt.DOAnchorPosY(originalPos.y - reelBounceStrength, 0.1f).SetEase(Ease.OutQuad));
                        bounceSeq.Append(rt.DOAnchorPosY(originalPos.y, 0.15f).SetEase(Ease.OutBounce));
                    }
                }

                // 릴 정지 사운드 효과를 위한 딜레이
                yield return new WaitForSeconds(reelStopDelay * speedScale);
            }

            yield return new WaitForSeconds(0.2f);

            // 당첨 확인
            int winAmount = CheckWins();
            if (winAmount > 0)
                _currentCoins += winAmount;

            if (winAmount > 0 || _lastFreeSpinsAwarded > 0 || _lastJackpotHit)
                PlayWinAnimation(winAmount);

            // 코인 카운터 애니메이션
            AnimateCoinCounter();

            _isSpinning = false;
            _spinButton.interactable = CanSpin();
            if (_betUpButton != null) _betUpButton.interactable = true;
            if (_betDownButton != null) _betDownButton.interactable = true;
            UpdateSpinButtonLabel();

            QueueNextAutoSpin();
        }

        private void ResetAllSymbolTransforms()
        {
            // 모든 심볼의 위치와 스케일을 원래 상태로 리셋
            for (int r = 0; r < 3; r++)
            {
                for (int s = 0; s < 3; s++)
                {
                    if (_symbolTransforms[r][s] != null)
                    {
                        _symbolTransforms[r][s].DOKill(); // 진행 중인 애니메이션 중지
                        _symbolTransforms[r][s].anchoredPosition = _originalSymbolPositions[r][s];
                        _symbolTransforms[r][s].localScale = Vector3.one;
                    }
                    if (_symbolImages[r][s] != null)
                    {
                        _symbolImages[r][s].DOKill();
                    }
                }
            }
        }

        private void SetSymbolDisplayWithAnimation(int reel, int slot, int symbolIndex, float duration)
        {
            SetSymbolDisplay(reel, slot, symbolIndex);

            // 심볼 변경 시 살짝 스케일 효과만 (위치는 변경하지 않음)
            if (_symbolTransforms[reel][slot] != null)
            {
                _symbolTransforms[reel][slot].DOKill();
                _symbolTransforms[reel][slot].localScale = Vector3.one * 0.9f;
                _symbolTransforms[reel][slot].DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
            }
        }

        private void SetSymbolDisplay(int reel, int slot, int symbolIndex)
        {
            if (_symbolSprites != null && symbolIndex < _symbolSprites.Length && _symbolSprites[symbolIndex] != null)
            {
                // 실제 스프라이트 사용
                _symbolImages[reel][slot].sprite = _symbolSprites[symbolIndex];
                _symbolImages[reel][slot].color = Color.white;
                var text = _symbolImages[reel][slot].GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = "";
            }
            else
            {
                // 플레이스홀더 사용
                _symbolImages[reel][slot].sprite = null;
                _symbolImages[reel][slot].color = _symbolColors[symbolIndex % _symbolColors.Length];
                var text = _symbolImages[reel][slot].GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = _symbolNames[symbolIndex % _symbolNames.Length];
            }
        }

        private int GetSymbolAtGridIndex(int gridIndex)
        {
            int row = Mathf.Clamp(gridIndex / 3, 0, 2);
            int col = Mathf.Clamp(gridIndex % 3, 0, 2);
            return _reelResults[col][row];
        }

        private bool TryGetPaylineWin(int[] payline, out int symbolIndex)
        {
            symbolIndex = -1;

            for (int i = 0; i < payline.Length; i++)
            {
                int symbol = GetSymbolAtGridIndex(payline[i]);

                if (IsScatter(symbol))
                    return false;

                if (!IsWild(symbol))
                {
                    if (symbolIndex == -1)
                    {
                        symbolIndex = symbol;
                    }
                    else if (symbolIndex != symbol)
                    {
                        return false;
                    }
                }
            }

            if (symbolIndex == -1)
                symbolIndex = wildSymbolIndex; // 모두 WILD인 경우

            return true;
        }

        private int GetSymbolPayout(int symbolIndex)
        {
            if (_symbolPayouts == null || _symbolPayouts.Length == 0)
                return 0;

            int idx = Mathf.Clamp(symbolIndex, 0, _symbolPayouts.Length - 1);
            return _symbolPayouts[idx];
        }

        private void AddPositions(List<int> target, int[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                target.Add(positions[i]);
            }
        }

        private bool IsJackpotTriggered()
        {
            int a = _reelResults[0][1];
            int b = _reelResults[1][1];
            int c = _reelResults[2][1];
            return IsJackpotSymbol(a) && IsJackpotSymbol(b) && IsJackpotSymbol(c);
        }

        private void AwardFreeSpins(int count)
        {
            if (count <= 0)
                return;

            _freeSpinsRemaining += count;
            UpdateStatusUI();
            UpdateSpinButtonLabel();

            if (_freeSpinsText != null)
            {
                _freeSpinsText.DOKill();
                _freeSpinsText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 4, 0.5f);
            }
        }

        private int CheckWins()
        {
            _lastWinningPositions.Clear();
            _lastScatterPositions.Clear();
            _lastScatterCount = 0;
            _lastFreeSpinsAwarded = 0;
            _lastJackpotHit = false;

            int totalWin = 0;

            // Payline wins (WILD 지원)
            for (int i = 0; i < _paylines.Length; i++)
            {
                if (TryGetPaylineWin(_paylines[i], out int symbolIndex))
                {
                    int payout = GetSymbolPayout(symbolIndex) * currentBet;
                    if (payout > 0)
                    {
                        totalWin += payout;
                        AddPositions(_lastWinningPositions, _paylines[i]);
                    }
                }
            }

            // Scatter wins (라인 무관)
            int scatterCount = 0;
            for (int i = 0; i < 9; i++)
            {
                int symbol = GetSymbolAtGridIndex(i);
                if (IsScatter(symbol))
                {
                    scatterCount++;
                    _lastScatterPositions.Add(i);
                }
            }

            if (scatterCount >= 3)
            {
                totalWin += scatterPayoutMultiplier * currentBet * scatterCount;
                _lastScatterCount = scatterCount;
                _lastFreeSpinsAwarded = scatterFreeSpins + (scatterCount - 3) * scatterExtraSpinsPerAdditional;
                AwardFreeSpins(_lastFreeSpinsAwarded);
            }

            // Jackpot (중앙 라인 3개 지정 심볼)
            if (IsJackpotTriggered())
            {
                _lastJackpotHit = true;
                totalWin += _jackpotValue;
                _jackpotValue = jackpotStart;
                UpdateStatusUI();
            }

            return totalWin;
        }

        private void PlayWinAnimation(int winAmount)
        {
            // WIN 텍스트 설정
            if (_lastJackpotHit)
            {
                _winText.text = $"JACKPOT!\n+{winAmount:N0}";
                _winText.color = new Color(1f, 0.6f, 0.1f);
            }
            else if (_lastFreeSpinsAwarded > 0)
            {
                _winText.text = $"+{winAmount:N0}\nFREE +{_lastFreeSpinsAwarded}";
                _winText.color = new Color(1f, 0.4f, 1f);
            }
            else
            {
                _winText.text = $"+{winAmount:N0}";
                _winText.color = new Color(0f, 1f, 0.6f);
            }

            // WIN 텍스트 펀치 스케일 애니메이션
            _winText.transform.DOKill();
            _winText.transform.localScale = Vector3.zero;

            Sequence winSeq = DOTween.Sequence();
            winSeq.Append(_winText.transform.DOScale(Vector3.one * 1.5f, 0.3f).SetEase(Ease.OutBack));
            winSeq.Append(_winText.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.InOutQuad));
            winSeq.Append(_winText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 3, 0.5f));

            // 릴 컨테이너 흔들림 효과
            if (_reelContainer != null)
            {
                _reelContainer.DOKill();
                _reelContainer.DOShakeAnchorPos(0.5f, winShakeStrength, 10, 90, false, true);
            }

            // 당첨 심볼 하이라이트 (예: 중간 라인이 당첨인 경우)
            HighlightWinningSymbols();

            // 색상 펄스 효과
            _winText.DOColor(new Color(1f, 1f, 0f), 0.2f).SetLoops(4, LoopType.Yoyo);
        }

        private void HighlightWinningSymbols()
        {
            if (_lastWinningPositions.Count > 0)
            {
                HighlightPositions(_lastWinningPositions, Color.white, 1.15f);
            }

            if (_lastScatterPositions.Count > 0)
            {
                HighlightPositions(_lastScatterPositions, new Color(1f, 0.3f, 1f), 1.12f);
            }
        }

        private void HighlightPositions(List<int> positions, Color color, float scale)
        {
            bool[] used = new bool[9];

            for (int i = 0; i < positions.Count; i++)
            {
                int pos = positions[i];
                if (pos < 0 || pos >= 9 || used[pos])
                    continue;

                used[pos] = true;

                int row = pos / 3;
                int reel = pos % 3;

                RectTransform rt = _symbolTransforms[reel][row];
                Image img = _symbolImages[reel][row];

                if (rt != null)
                {
                    rt.DOKill();
                    rt.DOScale(Vector3.one * scale, 0.25f).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutQuad);
                }

                if (img != null)
                {
                    Color originalColor = img.color;
                    img.DOKill();
                    img.DOColor(color, 0.15f).SetLoops(4, LoopType.Yoyo)
                        .OnComplete(() => img.color = originalColor);
                }
            }
        }

        private void AnimateCoinCounter()
        {
            // 코인 카운터 스무스 애니메이션
            DOTween.To(() => _displayedCoins, x =>
            {
                _displayedCoins = x;
                if (_coinText != null)
                    _coinText.text = _displayedCoins.ToString("N0");
            }, _currentCoins, coinCountDuration).SetEase(Ease.OutQuad);

            // 코인 텍스트 펀치 효과
            if (_coinText != null)
            {
                _coinText.transform.DOKill();
                _coinText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f);

                // 코인이 늘었으면 금색, 줄었으면 빨간색으로 잠시 표시
                Color targetColor = _currentCoins > _displayedCoins ?
                    new Color(1f, 1f, 0f) : new Color(1f, 0.3f, 0.3f);
                Color originalColor = new Color(1f, 0.84f, 0f);

                _coinText.DOColor(targetColor, 0.1f).OnComplete(() =>
                    _coinText.DOColor(originalColor, coinCountDuration));
            }
        }

        private IEnumerator WinAnimation()
        {
            // Legacy - DOTween 버전으로 대체됨
            yield return null;
        }

        private void IncreaseBet()
        {
            if (betIndex < betOptions.Length - 1)
            {
                betIndex++;
                currentBet = betOptions[betIndex];
                UpdateUI();
                AnimateBetChange(true);
            }
        }

        private void DecreaseBet()
        {
            if (betIndex > 0)
            {
                betIndex--;
                currentBet = betOptions[betIndex];
                UpdateUI();
                AnimateBetChange(false);
            }
        }

        private void AnimateBetChange(bool increased)
        {
            if (_betText != null)
            {
                _betText.transform.DOKill();
                _betText.transform.localScale = Vector3.one;

                // 증가 시 위로, 감소 시 아래로 살짝 이동하면서 스케일
                float direction = increased ? 1f : -1f;
                RectTransform rt = _betText.GetComponent<RectTransform>();
                Vector2 originalPos = rt.anchoredPosition;

                Sequence seq = DOTween.Sequence();
                seq.Append(rt.DOAnchorPosY(originalPos.y + (10f * direction), 0.1f).SetEase(Ease.OutQuad));
                seq.Join(_betText.transform.DOScale(Vector3.one * 1.2f, 0.1f));
                seq.Append(rt.DOAnchorPosY(originalPos.y, 0.15f).SetEase(Ease.OutBounce));
                seq.Join(_betText.transform.DOScale(Vector3.one, 0.15f));

                // 색상 피드백
                Color highlightColor = increased ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.5f, 0.5f);
                _betText.DOColor(highlightColor, 0.1f).OnComplete(() =>
                    _betText.DOColor(Color.white, 0.2f));
            }
        }

        private void UpdateUI()
        {
            if (_coinText != null)
                _coinText.text = _currentCoins.ToString("N0");

            if (_betText != null)
                _betText.text = currentBet.ToString("N0");

            if (_spinButton != null && !_isSpinning)
                _spinButton.interactable = CanSpin();

            UpdateSpinButtonLabel();
            UpdateStatusUI();
        }

        private void UpdateStatusUI()
        {
            if (_jackpotText != null)
                _jackpotText.text = $"JACKPOT {_jackpotValue:N0}";

            if (_freeSpinsText != null)
                _freeSpinsText.text = _freeSpinsRemaining > 0 ? $"FREE {_freeSpinsRemaining}" : "FREE 0";
        }

        private void UpdateSpinButtonLabel()
        {
            if (_spinButtonText == null)
                return;

            _spinButtonText.text = _freeSpinsRemaining > 0 ? $"FREE {_freeSpinsRemaining}" : "SPIN";
        }

        private void UpdateAutoSpinButton()
        {
            if (_autoSpinButton == null || _autoSpinText == null)
                return;

            _autoSpinText.text = _autoSpinEnabled ? "AUTO ON" : "AUTO OFF";
            _autoSpinButton.GetComponent<Image>().color = _autoSpinEnabled
                ? new Color(1f, 0.6f, 0.2f)
                : new Color(0.2f, 0.12f, 0.3f);
        }

        private void UpdateTurboButton()
        {
            if (_turboButton == null || _turboText == null)
                return;

            _turboText.text = _turboEnabled ? "TURBO ON" : "TURBO OFF";
            _turboButton.GetComponent<Image>().color = _turboEnabled
                ? new Color(0.2f, 0.9f, 1f)
                : new Color(0.2f, 0.12f, 0.3f);
        }

        private void AddJackpotContribution()
        {
            if (jackpotContributionRate <= 0f)
                return;

            int add = Mathf.Max(1, Mathf.RoundToInt(currentBet * jackpotContributionRate));
            _jackpotValue += add;
            UpdateStatusUI();
        }

        private void QueueNextAutoSpin()
        {
            if (!_autoSpinEnabled)
                return;

            if (_isSpinning)
                return;

            if (!CanSpin())
            {
                _autoSpinEnabled = false;
                UpdateAutoSpinButton();
                return;
            }

            StartCoroutine(AutoSpinDelayThenSpin());
        }

        private IEnumerator AutoSpinDelayThenSpin()
        {
            yield return new WaitForSeconds(autoSpinDelay);

            if (_autoSpinEnabled && CanSpin())
            {
                Spin();
            }
            else if (_autoSpinEnabled && !CanSpin())
            {
                _autoSpinEnabled = false;
                UpdateAutoSpinButton();
            }
        }

        #endregion

        #region Helper Methods

        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            obj.AddComponent<Image>();
            return obj;
        }

        private GameObject CreateText(string name, Transform parent, string text, int fontSize, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);

            return obj;
        }

        private Button CreateButton(string name, Transform parent, string text, Vector2 pos, Vector2 size)
        {
            GameObject obj = CreatePanel(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = obj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.12f, 0.3f);

            Button btn = obj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.2f, 0.4f);
            colors.pressedColor = new Color(0.1f, 0.06f, 0.15f);
            btn.colors = colors;

            GameObject textObj = CreateText("Text", obj.transform, text, 20, Color.white);
            SetFullStretch(textObj.GetComponent<RectTransform>());

            return btn;
        }

        private void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
