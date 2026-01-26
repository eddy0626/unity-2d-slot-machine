using System;
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
    public class SlotClickerUI : MonoBehaviour
    {
        [Header("=== 자동 생성 ===")]
        [SerializeField] private bool _autoCreateUI = true;

        [Header("=== UI 참조 ===")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _chipsText;
        [SerializeField] private Button _clickArea;
        [SerializeField] private Image[] _reelSymbols;
        [SerializeField] private Button[] _betButtons;
        [SerializeField] private TextMeshProUGUI _betAmountText;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TextMeshProUGUI _resultText;

        [Header("=== 심볼 스프라이트 ===")]
        [SerializeField] private Sprite[] _symbolSprites;

        [Header("=== 색상 설정 ===")]
        [SerializeField] private Color _normalClickColor = new Color(0.2f, 0.6f, 0.2f);
        [SerializeField] private Color _criticalColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color _jackpotColor = new Color(1f, 0.2f, 0.2f);

        [Header("=== 업그레이드 UI ===")]
        [SerializeField] private Button _upgradeButton;
        private UpgradeUI _upgradeUI;

        // 내부 상태
        private GameManager _game;
        private float _currentBetPercentage = 0.1f;
        private double _currentBetAmount = 0;

        // 클릭 이펙트 풀
        private GameObject _floatingTextPrefab;

        private void Start()
        {
            StartCoroutine(WaitForGameManager());
        }

        private System.Collections.IEnumerator WaitForGameManager()
        {
            // GameManager 초기화 대기
            while (GameManager.Instance == null || GameManager.Instance.Gold == null)
            {
                yield return null;
            }

            _game = GameManager.Instance;

            if (_autoCreateUI)
            {
                CreateUI();
            }

            BindEvents();
            UpdateUI();
        }

        private void CreateUI()
        {
            // EventSystem 확인 및 생성 (UI 입력 처리에 필수!)
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>(); // Input System 패키지용
                Debug.Log("[SlotClickerUI] EventSystem created (Input System)");
            }

            // 캔버스 생성
            if (_mainCanvas == null)
            {
                GameObject canvasObj = new GameObject("SlotClickerCanvas");
                _mainCanvas = canvasObj.AddComponent<Canvas>();
                _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _mainCanvas.sortingOrder = 100;

                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            RectTransform canvasRect = _mainCanvas.GetComponent<RectTransform>();

            // === 상단 HUD ===
            CreateTopHUD(canvasRect);

            // === 슬롯머신 영역 ===
            CreateSlotArea(canvasRect);

            // === 클릭 영역 ===
            CreateClickArea(canvasRect);

            // === 하단 베팅 UI ===
            CreateBettingUI(canvasRect);

            // === 결과 텍스트 ===
            CreateResultText(canvasRect);

            // === 플로팅 텍스트 프리팹 ===
            CreateFloatingTextPrefab();

            // === 업그레이드 버튼 ===
            CreateUpgradeButton(canvasRect);

            // === 업그레이드 UI ===
            CreateUpgradeUI();

            Debug.Log("[SlotClickerUI] UI created successfully!");
        }

        private void CreateTopHUD(RectTransform parent)
        {
            // HUD 배경
            GameObject hudPanel = CreatePanel(parent, "TopHUD", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -100), new Vector2(0, 0), new Color(0.1f, 0.1f, 0.15f, 0.95f));
            RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
            hudRect.sizeDelta = new Vector2(0, 120);

            // 골드 표시
            GameObject goldObj = CreateTextObject(hudRect, "GoldText", "0 Gold",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), 36);
            _goldText = goldObj.GetComponent<TextMeshProUGUI>();
            _goldText.color = new Color(1f, 0.85f, 0.2f);
            _goldText.alignment = TextAlignmentOptions.Left;

            // 칩 표시
            GameObject chipsObj = CreateTextObject(hudRect, "ChipsText", "0 Chips",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), 28);
            _chipsText = chipsObj.GetComponent<TextMeshProUGUI>();
            _chipsText.color = new Color(0.6f, 0.8f, 1f);
            _chipsText.alignment = TextAlignmentOptions.Right;
        }

        private void CreateSlotArea(RectTransform parent)
        {
            // 슬롯 패널
            GameObject slotPanel = CreatePanel(parent, "SlotPanel", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -250), new Vector2(600, 250), new Color(0.15f, 0.1f, 0.2f, 1f));
            RectTransform slotRect = slotPanel.GetComponent<RectTransform>();

            // 슬롯 프레임
            Image frameImg = slotPanel.GetComponent<Image>();
            AddOutline(slotPanel, new Color(0.8f, 0.6f, 0.2f), 4);

            // 릴 심볼들
            _reelSymbols = new Image[3];
            float spacing = 180f;
            float startX = -spacing;

            for (int i = 0; i < 3; i++)
            {
                GameObject reelBg = CreatePanel(slotRect, $"ReelBg_{i}",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + (i * spacing), 0), new Vector2(150, 150),
                    new Color(0.05f, 0.05f, 0.1f, 1f));

                GameObject symbolObj = new GameObject($"Symbol_{i}");
                symbolObj.transform.SetParent(reelBg.transform, false);
                RectTransform symRect = symbolObj.AddComponent<RectTransform>();
                symRect.anchorMin = Vector2.zero;
                symRect.anchorMax = Vector2.one;
                symRect.offsetMin = new Vector2(10, 10);
                symRect.offsetMax = new Vector2(-10, -10);

                _reelSymbols[i] = symbolObj.AddComponent<Image>();
                _reelSymbols[i].color = GetSymbolColor(i);
            }
        }

        private void CreateClickArea(RectTransform parent)
        {
            // 클릭 영역 (카지노 테이블)
            GameObject clickPanel = CreatePanel(parent, "ClickArea", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -100), new Vector2(800, 400), new Color(0.1f, 0.4f, 0.15f, 1f));
            RectTransform clickRect = clickPanel.GetComponent<RectTransform>();

            AddOutline(clickPanel, new Color(0.6f, 0.4f, 0.1f), 6);

            // 버튼 컴포넌트
            _clickArea = clickPanel.AddComponent<Button>();
            _clickArea.transition = Selectable.Transition.None;

            // 테이블 텍스트
            GameObject tableText = CreateTextObject(clickRect, "TableText", "TAP TO EARN",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 48);
            TextMeshProUGUI tableTmp = tableText.GetComponent<TextMeshProUGUI>();
            tableTmp.color = new Color(1f, 0.9f, 0.6f, 0.8f);
            tableTmp.alignment = TextAlignmentOptions.Center;

            // 펄스 애니메이션
            tableTmp.transform.DOScale(1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        private void CreateBettingUI(RectTransform parent)
        {
            // 베팅 패널
            GameObject betPanel = CreatePanel(parent, "BetPanel", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 300), new Vector2(0, 280), new Color(0.12f, 0.1f, 0.18f, 0.95f));
            RectTransform betRect = betPanel.GetComponent<RectTransform>();

            // 현재 베팅액 표시
            GameObject betAmountObj = CreateTextObject(betRect, "BetAmountText", "Bet: 0",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), 32);
            _betAmountText = betAmountObj.GetComponent<TextMeshProUGUI>();
            _betAmountText.color = Color.white;
            _betAmountText.alignment = TextAlignmentOptions.Center;

            // 베팅 버튼들
            _betButtons = new Button[4];
            string[] betLabels = { "10%", "30%", "50%", "ALL" };
            float[] betValues = { 0.1f, 0.3f, 0.5f, 1f };
            float buttonWidth = 150f;
            float startX = -((buttonWidth * 4) + 30) / 2 + buttonWidth / 2;

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                float betValue = betValues[i];

                GameObject btnObj = CreateButton(betRect, $"BetBtn_{i}", betLabels[i],
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + (i * (buttonWidth + 10)), 20),
                    new Vector2(buttonWidth, 60),
                    new Color(0.3f, 0.3f, 0.5f));

                _betButtons[i] = btnObj.GetComponent<Button>();
                _betButtons[i].onClick.AddListener(() => SetBetPercentage(betValue));
            }

            // 스핀 버튼
            GameObject spinObj = CreateButton(betRect, "SpinButton", "SPIN!",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 60), new Vector2(300, 80),
                new Color(0.8f, 0.2f, 0.2f));
            _spinButton = spinObj.GetComponent<Button>();
            _spinButton.onClick.AddListener(OnSpinClicked);

            TextMeshProUGUI spinText = spinObj.GetComponentInChildren<TextMeshProUGUI>();
            spinText.fontSize = 36;
            spinText.fontStyle = FontStyles.Bold;
        }

        private void CreateResultText(RectTransform parent)
        {
            GameObject resultObj = CreateTextObject(parent, "ResultText", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), 48);
            _resultText = resultObj.GetComponent<TextMeshProUGUI>();
            _resultText.color = Color.white;
            _resultText.alignment = TextAlignmentOptions.Center;
            _resultText.gameObject.SetActive(false);
        }

        private void CreateFloatingTextPrefab()
        {
            _floatingTextPrefab = new GameObject("FloatingTextPrefab");
            _floatingTextPrefab.SetActive(false);

            RectTransform rect = _floatingTextPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            TextMeshProUGUI tmp = _floatingTextPrefab.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.yellow;

            _floatingTextPrefab.transform.SetParent(_mainCanvas.transform, false);
        }

        #region Helper Methods

        private GameObject CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image img = panel.AddComponent<Image>();
            img.color = color;

            return panel;
        }

        private GameObject CreateTextObject(RectTransform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 60);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;

            return textObj;
        }

        private GameObject CreateButton(RectTransform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject btnObj = CreatePanel(parent, name, anchorMin, anchorMax, position, size, color);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;

            GameObject textObj = CreateTextObject(btnObj.GetComponent<RectTransform>(), "Label", label,
                Vector2.zero, Vector2.one, Vector2.zero, 24);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btnObj;
        }

        private void AddOutline(GameObject obj, Color color, float width)
        {
            var outline = obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(width, width);
        }

        private Color GetSymbolColor(int index)
        {
            Color[] colors = {
                new Color(1f, 0.3f, 0.3f),  // 빨강
                new Color(0.3f, 1f, 0.3f),  // 초록
                new Color(0.3f, 0.3f, 1f),  // 파랑
                new Color(1f, 1f, 0.3f),    // 노랑
                new Color(1f, 0.5f, 0f),    // 주황
                new Color(0.8f, 0.3f, 1f),  // 보라
                new Color(1f, 0.8f, 0f)     // 금
            };
            return colors[index % colors.Length];
        }

        private void CreateUpgradeButton(RectTransform parent)
        {
            // 업그레이드 버튼 (화면 왼쪽 상단)
            GameObject btnObj = CreateButton(parent, "UpgradeButton", "UPGRADES",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(100, -180), new Vector2(160, 50),
                new Color(0.4f, 0.3f, 0.7f));

            _upgradeButton = btnObj.GetComponent<Button>();
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

            // 아이콘 효과 (펄스)
            btnObj.transform.DOScale(1.05f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void CreateUpgradeUI()
        {
            GameObject upgradeUIObj = new GameObject("UpgradeUI");
            upgradeUIObj.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = upgradeUIObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _upgradeUI = upgradeUIObj.AddComponent<UpgradeUI>();
            _upgradeUI.Initialize(_game);
            _upgradeUI.Hide();
        }

        private void OnUpgradeButtonClicked()
        {
            if (_upgradeUI != null)
            {
                _upgradeUI.Toggle();
            }
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            // 클릭 이벤트
            _clickArea.onClick.AddListener(OnClickAreaClicked);

            // 게임 매니저 이벤트
            _game.Gold.OnGoldChanged += OnGoldChanged;
            _game.Click.OnClick += OnClickResult;
            _game.Slot.OnSpinStart += OnSlotSpinStart;
            _game.Slot.OnSpinComplete += OnSlotSpinComplete;
            _game.Slot.OnReelStop += OnReelStop;
        }

        private void OnDestroy()
        {
            if (_game != null && _game.Gold != null)
            {
                _game.Gold.OnGoldChanged -= OnGoldChanged;
                _game.Click.OnClick -= OnClickResult;
                _game.Slot.OnSpinStart -= OnSlotSpinStart;
                _game.Slot.OnSpinComplete -= OnSlotSpinComplete;
                _game.Slot.OnReelStop -= OnReelStop;
            }
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            if (_game == null) return;

            _goldText.text = $"{_game.Gold.GetFormattedGold()} Gold";
            _chipsText.text = $"{_game.PlayerData.chips} Chips";
            UpdateBetAmount();
        }

        private void UpdateBetAmount()
        {
            _currentBetAmount = _game.Gold.CalculateBetAmount(_currentBetPercentage);
            _betAmountText.text = $"Bet: {GoldManager.FormatNumber(_currentBetAmount)}";
        }

        private void OnGoldChanged(double newGold)
        {
            _goldText.text = $"{GoldManager.FormatNumber(newGold)} Gold";
            _goldText.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            UpdateBetAmount();
        }

        #endregion

        #region Input Handlers

        private void OnClickAreaClicked()
        {
            // Input System 사용
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            _game.Click.ProcessClick(mousePos);

            // 클릭 피드백
            _clickArea.transform.DOPunchScale(Vector3.one * 0.05f, 0.1f);
        }

        private void OnClickResult(ClickResult result)
        {
            // 플로팅 텍스트 생성
            SpawnFloatingText(result.Position, result.GoldEarned, result.IsCritical);
        }

        private void SpawnFloatingText(Vector2 position, double amount, bool isCritical)
        {
            GameObject floatText = Instantiate(_floatingTextPrefab, _mainCanvas.transform);
            floatText.SetActive(true);

            RectTransform rect = floatText.GetComponent<RectTransform>();
            rect.position = position;

            TextMeshProUGUI tmp = floatText.GetComponent<TextMeshProUGUI>();
            tmp.text = $"+{GoldManager.FormatNumber(amount)}";
            tmp.color = isCritical ? _criticalColor : Color.yellow;
            tmp.fontSize = isCritical ? 42 : 32;

            // 애니메이션
            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOAnchorPosY(rect.anchoredPosition.y + 100, 0.8f).SetEase(Ease.OutQuad));
            seq.Join(tmp.DOFade(0, 0.8f).SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(floatText));
        }

        private void SetBetPercentage(float percentage)
        {
            _currentBetPercentage = percentage;
            UpdateBetAmount();

            // 버튼 하이라이트
            for (int i = 0; i < _betButtons.Length; i++)
            {
                Image img = _betButtons[i].GetComponent<Image>();
                float[] values = { 0.1f, 0.3f, 0.5f, 1f };
                img.color = Mathf.Approximately(values[i], percentage)
                    ? new Color(0.5f, 0.4f, 0.8f)
                    : new Color(0.3f, 0.3f, 0.5f);
            }
        }

        private void OnSpinClicked()
        {
            if (_currentBetAmount <= 0)
            {
                ShowResult("Not enough gold!", Color.red);
                return;
            }

            _game.Slot.TrySpin(_currentBetAmount);
        }

        #endregion

        #region Slot Events

        private void OnSlotSpinStart()
        {
            _spinButton.interactable = false;
            _resultText.gameObject.SetActive(false);

            // 릴 스핀 애니메이션
            for (int i = 0; i < _reelSymbols.Length; i++)
            {
                _reelSymbols[i].transform.DORotate(new Vector3(0, 0, 360 * 5), 2f, RotateMode.FastBeyond360)
                    .SetEase(Ease.InOutQuad);
            }
        }

        private void OnReelStop(int reelIndex, int symbolIndex)
        {
            if (reelIndex < _reelSymbols.Length)
            {
                _reelSymbols[reelIndex].DOKill();
                _reelSymbols[reelIndex].transform.rotation = Quaternion.identity;
                _reelSymbols[reelIndex].color = GetSymbolColor(symbolIndex);
                _reelSymbols[reelIndex].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
        }

        private void OnSlotSpinComplete(SlotResult result)
        {
            _spinButton.interactable = true;

            string message;
            Color color;

            switch (result.Outcome)
            {
                case SlotOutcome.MegaJackpot:
                    message = $"MEGA JACKPOT! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = _jackpotColor;
                    CelebrationEffect();
                    break;
                case SlotOutcome.Jackpot:
                    message = $"JACKPOT! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = _jackpotColor;
                    CelebrationEffect();
                    break;
                case SlotOutcome.BigWin:
                    message = $"BIG WIN! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = Color.green;
                    break;
                case SlotOutcome.SmallWin:
                    message = $"Win! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = Color.cyan;
                    break;
                case SlotOutcome.MiniWin:
                    message = $"Mini Win! +{GoldManager.FormatNumber(result.FinalReward)}";
                    color = Color.white;
                    break;
                case SlotOutcome.Draw:
                    message = "Draw - Money Back!";
                    color = Color.gray;
                    break;
                default:
                    message = "No Match...";
                    color = Color.gray;
                    break;
            }

            ShowResult(message, color);
        }

        private void ShowResult(string message, Color color)
        {
            _resultText.text = message;
            _resultText.color = color;
            _resultText.gameObject.SetActive(true);

            _resultText.transform.localScale = Vector3.zero;
            _resultText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // 3초 후 페이드아웃
            DOVirtual.DelayedCall(3f, () =>
            {
                _resultText.DOFade(0, 0.5f).OnComplete(() =>
                {
                    _resultText.gameObject.SetActive(false);
                    _resultText.alpha = 1f;
                });
            });
        }

        private void CelebrationEffect()
        {
            // 화면 플래시
            GameObject flash = CreatePanel(_mainCanvas.GetComponent<RectTransform>(), "Flash",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.5f));

            flash.GetComponent<Image>().DOFade(0, 0.5f).OnComplete(() => Destroy(flash));

            // 화면 흔들림
            _mainCanvas.transform.DOShakePosition(0.5f, 30f, 20);
        }

        #endregion
    }
}
