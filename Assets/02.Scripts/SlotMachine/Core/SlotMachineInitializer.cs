using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using SlotMachine.Data;
using SlotMachine.UI;

namespace SlotMachine.Core
{
    /// <summary>
    /// 씬 시작 시 슬롯머신 UI를 자동으로 생성하는 초기화 클래스
    /// </summary>
    public class SlotMachineInitializer : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private SlotMachineConfig config;

        [Header("Auto Create")]
        [SerializeField] private bool createOnStart = true;

        private Canvas _canvas;
        private GameObject _slotMachineObj;
        private SlotMachine _slotMachine;
        private SlotUIManager _uiManager;

        private void Start()
        {
            if (createOnStart)
            {
                CreateSlotMachineUI();
            }
        }

        public void CreateSlotMachineUI()
        {
            // EventSystem 확인
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            // Canvas 생성
            CreateCanvas();

            // 메인 슬롯머신 컨테이너
            _slotMachineObj = CreateUIElement("SlotMachine", _canvas.transform);
            RectTransform slotRT = _slotMachineObj.GetComponent<RectTransform>();
            slotRT.anchorMin = Vector2.zero;
            slotRT.anchorMax = Vector2.one;
            slotRT.offsetMin = Vector2.zero;
            slotRT.offsetMax = Vector2.zero;

            // 배경
            CreateBackground(_slotMachineObj.transform);

            // 타이틀
            CreateTitle(_slotMachineObj.transform);

            // 릴 컨테이너
            GameObject reelContainer = CreateReelContainer(_slotMachineObj.transform);

            // 3개 릴 생성
            Reel[] reels = new Reel[3];
            for (int i = 0; i < 3; i++)
            {
                reels[i] = CreateReel(reelContainer.transform, i);
            }

            // UI 패널
            var uiElements = CreateUIPanel(_slotMachineObj.transform);

            // 컴포넌트 추가
            _slotMachine = _slotMachineObj.AddComponent<SlotMachine>();
            PaylineManager paylineManager = _slotMachineObj.AddComponent<PaylineManager>();
            _uiManager = _slotMachineObj.AddComponent<SlotUIManager>();
            SlotAudioManager audioManager = _slotMachineObj.AddComponent<SlotAudioManager>();
            PaylineRenderer paylineRenderer = _slotMachineObj.AddComponent<PaylineRenderer>();

            // 릴 참조 설정
            SetupReferences(_slotMachine, reels, uiElements);

            Debug.Log("슬롯머신 UI가 성공적으로 생성되었습니다!");
        }

        private void CreateCanvas()
        {
            _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreateBackground(Transform parent)
        {
            GameObject background = CreateUIElement("Background", parent);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.04f, 0.12f); // 네온 테마 어두운 보라색

            RectTransform bgRT = background.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
        }

        private void CreateTitle(Transform parent)
        {
            GameObject title = CreateUIElement("Title", parent);
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "NEON SLOTS";
            titleText.fontSize = 72;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.2f, 0.6f); // 네온 핑크
            titleText.fontStyle = FontStyles.Bold;

            RectTransform titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1);
            titleRT.anchorMax = new Vector2(0.5f, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -30);
            titleRT.sizeDelta = new Vector2(800, 100);
        }

        private GameObject CreateReelContainer(Transform parent)
        {
            GameObject reelContainer = CreateUIElement("ReelContainer", parent);
            RectTransform reelContainerRT = reelContainer.GetComponent<RectTransform>();
            reelContainerRT.anchoredPosition = new Vector2(0, 50);
            reelContainerRT.sizeDelta = new Vector2(500, 480);

            // 릴 마스크 배경
            Image reelMaskBg = reelContainer.AddComponent<Image>();
            reelMaskBg.color = new Color(0.12f, 0.08f, 0.18f);

            // 마스크
            Mask mask = reelContainer.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // 테두리 효과 (글로우)
            GameObject border = CreateUIElement("Border", reelContainer.transform);
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0f, 1f, 0.8f, 0.3f); // 네온 시안 글로우

            RectTransform borderRT = border.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = new Vector2(-5, -5);
            borderRT.offsetMax = new Vector2(5, 5);
            borderRT.SetAsFirstSibling();

            return reelContainer;
        }

        private Reel CreateReel(Transform parent, int index)
        {
            float reelWidth = 160f;
            float startX = -reelWidth;

            GameObject reel = CreateUIElement($"Reel_{index}", parent);
            RectTransform reelRT = reel.GetComponent<RectTransform>();
            reelRT.anchoredPosition = new Vector2(startX + (index * reelWidth), 0);
            reelRT.sizeDelta = new Vector2(reelWidth - 10, 460);

            // 릴 배경
            Image reelBg = reel.AddComponent<Image>();
            reelBg.color = new Color(0.06f, 0.03f, 0.1f);

            // 심볼 컨테이너
            GameObject symbolContainer = CreateUIElement("SymbolContainer", reel.transform);
            RectTransform symbolContainerRT = symbolContainer.GetComponent<RectTransform>();
            symbolContainerRT.anchorMin = Vector2.zero;
            symbolContainerRT.anchorMax = Vector2.one;
            symbolContainerRT.offsetMin = Vector2.zero;
            symbolContainerRT.offsetMax = Vector2.zero;

            // Reel 컴포넌트 추가
            Reel reelComponent = reel.AddComponent<Reel>();

            // 심볼 3개 생성
            for (int i = 0; i < 3; i++)
            {
                CreateSymbol(symbolContainer.transform, i);
            }

            return reelComponent;
        }

        private void CreateSymbol(Transform parent, int index)
        {
            float symbolHeight = 150f;
            float startY = symbolHeight;

            GameObject symbol = CreateUIElement($"Symbol_{index}", parent);
            RectTransform symbolRT = symbol.GetComponent<RectTransform>();
            symbolRT.anchoredPosition = new Vector2(0, startY - (index * symbolHeight));
            symbolRT.sizeDelta = new Vector2(140, 140);

            // 심볼 이미지
            Image symbolImage = symbol.AddComponent<Image>();
            symbolImage.color = Color.white;

            // Symbol 컴포넌트
            symbol.AddComponent<Symbol>();
        }

        private (Button spinBtn, TextMeshProUGUI coinText, TextMeshProUGUI betText, TextMeshProUGUI winText)
            CreateUIPanel(Transform parent)
        {
            // 하단 UI 패널
            GameObject uiPanel = CreateUIElement("UIPanel", parent);
            RectTransform uiPanelRT = uiPanel.GetComponent<RectTransform>();
            uiPanelRT.anchorMin = new Vector2(0, 0);
            uiPanelRT.anchorMax = new Vector2(1, 0);
            uiPanelRT.pivot = new Vector2(0.5f, 0);
            uiPanelRT.anchoredPosition = Vector2.zero;
            uiPanelRT.sizeDelta = new Vector2(0, 180);

            Image panelBg = uiPanel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.06f, 0.15f, 0.95f);

            // 코인 표시
            var coinText = CreateCoinDisplay(uiPanel.transform);

            // 배팅 표시
            var betText = CreateBetDisplay(uiPanel.transform);

            // 스핀 버튼
            var spinBtn = CreateSpinButton(uiPanel.transform);

            // 당첨금 표시
            var winText = CreateWinDisplay(uiPanel.transform);

            return (spinBtn, coinText, betText, winText);
        }

        private TextMeshProUGUI CreateCoinDisplay(Transform parent)
        {
            GameObject coinPanel = CreateUIElement("CoinPanel", parent);
            RectTransform coinRT = coinPanel.GetComponent<RectTransform>();
            coinRT.anchorMin = new Vector2(0, 0.5f);
            coinRT.anchorMax = new Vector2(0, 0.5f);
            coinRT.pivot = new Vector2(0, 0.5f);
            coinRT.anchoredPosition = new Vector2(50, 0);
            coinRT.sizeDelta = new Vector2(280, 80);

            // 라벨
            GameObject label = CreateTextElement("CoinLabel", coinPanel.transform, "COINS", 22);
            RectTransform labelRT = label.GetComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(0, 22);

            // 값
            GameObject value = CreateTextElement("CoinText", coinPanel.transform, "10,000", 38);
            TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
            valueText.color = new Color(1f, 0.84f, 0f); // Gold
            RectTransform valueRT = value.GetComponent<RectTransform>();
            valueRT.anchoredPosition = new Vector2(0, -18);

            return valueText;
        }

        private TextMeshProUGUI CreateBetDisplay(Transform parent)
        {
            GameObject betPanel = CreateUIElement("BetPanel", parent);
            RectTransform betRT = betPanel.GetComponent<RectTransform>();
            betRT.anchorMin = new Vector2(0.5f, 0.5f);
            betRT.anchorMax = new Vector2(0.5f, 0.5f);
            betRT.anchoredPosition = new Vector2(-180, 0);
            betRT.sizeDelta = new Vector2(220, 80);

            // 라벨
            GameObject label = CreateTextElement("BetLabel", betPanel.transform, "BET", 18);
            RectTransform labelRT = label.GetComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(0, 32);

            // - 버튼
            CreateButton("BetDecreaseBtn", betPanel.transform, "-", new Vector2(-70, -5), new Vector2(45, 45));

            // 배팅 값
            GameObject betValue = CreateTextElement("BetText", betPanel.transform, "50", 32);
            TextMeshProUGUI betText = betValue.GetComponent<TextMeshProUGUI>();
            betText.color = Color.white;
            RectTransform betValueRT = betValue.GetComponent<RectTransform>();
            betValueRT.anchoredPosition = new Vector2(0, -5);

            // + 버튼
            CreateButton("BetIncreaseBtn", betPanel.transform, "+", new Vector2(70, -5), new Vector2(45, 45));

            return betText;
        }

        private Button CreateSpinButton(Transform parent)
        {
            GameObject spinBtn = CreateButton("SpinButton", parent, "SPIN", new Vector2(180, 0), new Vector2(140, 70));
            RectTransform spinRT = spinBtn.GetComponent<RectTransform>();
            spinRT.anchorMin = new Vector2(0.5f, 0.5f);
            spinRT.anchorMax = new Vector2(0.5f, 0.5f);

            // 네온 그린 색상
            Image btnImage = spinBtn.GetComponent<Image>();
            btnImage.color = new Color(0f, 0.85f, 0.65f);

            TextMeshProUGUI btnText = spinBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontSize = 32;
                btnText.fontStyle = FontStyles.Bold;
                btnText.color = Color.white;
            }

            return spinBtn.GetComponent<Button>();
        }

        private TextMeshProUGUI CreateWinDisplay(Transform parent)
        {
            GameObject winPanel = CreateUIElement("WinPanel", parent);
            RectTransform winRT = winPanel.GetComponent<RectTransform>();
            winRT.anchorMin = new Vector2(1, 0.5f);
            winRT.anchorMax = new Vector2(1, 0.5f);
            winRT.pivot = new Vector2(1, 0.5f);
            winRT.anchoredPosition = new Vector2(-50, 0);
            winRT.sizeDelta = new Vector2(280, 80);

            // 라벨
            GameObject label = CreateTextElement("WinLabel", winPanel.transform, "WIN", 22);
            RectTransform labelRT = label.GetComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(0, 22);

            // 값
            GameObject value = CreateTextElement("WinText", winPanel.transform, "", 38);
            TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
            valueText.color = new Color(0f, 1f, 0.6f); // 네온 그린
            RectTransform valueRT = value.GetComponent<RectTransform>();
            valueRT.anchoredPosition = new Vector2(0, -18);

            return valueText;
        }

        private void SetupReferences(SlotMachine slotMachine, Reel[] reels,
            (Button spinBtn, TextMeshProUGUI coinText, TextMeshProUGUI betText, TextMeshProUGUI winText) ui)
        {
            // Config가 있으면 설정
            if (config != null)
            {
                // Reflection을 사용하여 private serialized field 설정
                var configField = typeof(SlotMachine).GetField("config",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (configField != null)
                {
                    configField.SetValue(slotMachine, config);
                }
            }

            // Reels 설정
            var reelsField = typeof(SlotMachine).GetField("reels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (reelsField != null)
            {
                reelsField.SetValue(slotMachine, reels);
            }
        }

        #region Helper Methods

        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private GameObject CreateTextElement(string name, Transform parent, string text, int fontSize)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250, 50);

            return obj;
        }

        private GameObject CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchoredPosition = position;
            btnRT.sizeDelta = size;

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.25f, 0.15f, 0.35f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.25f, 0.15f, 0.35f);
            colors.highlightedColor = new Color(0.35f, 0.25f, 0.45f);
            colors.pressedColor = new Color(0.15f, 0.08f, 0.25f);
            colors.selectedColor = new Color(0.3f, 0.2f, 0.4f);
            btn.colors = colors;

            // 버튼 텍스트
            GameObject textObj = CreateTextElement("Text", btnObj.transform, text, 22);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            return btnObj;
        }

        #endregion
    }
}
