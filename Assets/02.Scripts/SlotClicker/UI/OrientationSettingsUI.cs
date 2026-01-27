using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// 화면 회전 설정 UI 패널
    /// - 자동 회전 ON/OFF
    /// - 회전 잠금 모드
    /// - 회전 감도 조절
    /// </summary>
    public class OrientationSettingsUI : MonoBehaviour
    {
        [Header("=== UI 참조 ===")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button _openSettingsButton;
        [SerializeField] private Button _closeSettingsButton;

        [Header("=== 자동 회전 설정 ===")]
        [SerializeField] private Toggle _autoRotationToggle;
        [SerializeField] private TextMeshProUGUI _autoRotationLabel;

        [Header("=== 회전 잠금 ===")]
        [SerializeField] private Button _lockPortraitButton;
        [SerializeField] private Button _lockLandscapeButton;
        [SerializeField] private Button _lockCurrentButton;
        [SerializeField] private Button _unlockButton;
        [SerializeField] private TextMeshProUGUI _lockStatusText;

        [Header("=== 감도 조절 ===")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private TextMeshProUGUI _sensitivityValueText;

        [Header("=== 방향 강제 전환 ===")]
        [SerializeField] private Button _forcePortraitButton;
        [SerializeField] private Button _forceLandscapeButton;

        [Header("=== 디버그 정보 ===")]
        [SerializeField] private TextMeshProUGUI _currentOrientationText;
        [SerializeField] private TextMeshProUGUI _accelerationText;
        [SerializeField] private bool _showDebugInfo = false;

        [Header("=== 자동 생성 옵션 ===")]
        [SerializeField] private bool _autoCreateUI = false;
        [SerializeField] private Canvas _targetCanvas;

        private OrientationManager _orientationManager;
        private bool _isInitialized = false;

        private void Start()
        {
            StartCoroutine(WaitForOrientationManager());
        }

        private System.Collections.IEnumerator WaitForOrientationManager()
        {
            // OrientationManager 대기
            while (OrientationManager.Instance == null)
            {
                yield return null;
            }

            _orientationManager = OrientationManager.Instance;

            if (_autoCreateUI && _settingsPanel == null)
            {
                CreateSettingsUI();
            }

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // 이벤트 바인딩
            BindEvents();

            // 초기 상태 업데이트
            UpdateUI();

            // 설정 로드
            _orientationManager.LoadSettings();

            _isInitialized = true;

            // 초기에는 패널 숨기기
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }

            Debug.Log("[OrientationSettingsUI] Initialized");
        }

        private void BindEvents()
        {
            // 설정 패널 열기/닫기
            if (_openSettingsButton != null)
            {
                _openSettingsButton.onClick.AddListener(OpenSettings);
            }

            if (_closeSettingsButton != null)
            {
                _closeSettingsButton.onClick.AddListener(CloseSettings);
            }

            // 자동 회전 토글
            if (_autoRotationToggle != null)
            {
                _autoRotationToggle.onValueChanged.AddListener(OnAutoRotationToggled);
            }

            // 잠금 버튼들
            if (_lockPortraitButton != null)
            {
                _lockPortraitButton.onClick.AddListener(() => SetLockMode(OrientationManager.LockMode.LockPortrait));
            }

            if (_lockLandscapeButton != null)
            {
                _lockLandscapeButton.onClick.AddListener(() => SetLockMode(OrientationManager.LockMode.LockLandscape));
            }

            if (_lockCurrentButton != null)
            {
                _lockCurrentButton.onClick.AddListener(() => SetLockMode(OrientationManager.LockMode.LockCurrent));
            }

            if (_unlockButton != null)
            {
                _unlockButton.onClick.AddListener(() => SetLockMode(OrientationManager.LockMode.Auto));
            }

            // 감도 슬라이더
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            // 강제 전환 버튼
            if (_forcePortraitButton != null)
            {
                _forcePortraitButton.onClick.AddListener(() =>
                    _orientationManager.SetOrientation(OrientationManager.DeviceOrientation.Portrait));
            }

            if (_forceLandscapeButton != null)
            {
                _forceLandscapeButton.onClick.AddListener(() =>
                    _orientationManager.SetOrientation(OrientationManager.DeviceOrientation.LandscapeLeft));
            }

            // OrientationManager 이벤트 구독
            _orientationManager.OnOrientationChanged += OnOrientationChanged;
        }

        private void Update()
        {
            if (!_isInitialized || !_showDebugInfo) return;

            // 디버그 정보 업데이트
            if (_currentOrientationText != null)
            {
                _currentOrientationText.text = $"Orientation: {_orientationManager.CurrentOrientation}";
            }

            if (_accelerationText != null)
            {
                Vector3 acc = _orientationManager.SmoothedAcceleration;
                _accelerationText.text = $"Accel: ({acc.x:F2}, {acc.y:F2}, {acc.z:F2})";
            }
        }

        private void OnDestroy()
        {
            if (_orientationManager != null)
            {
                _orientationManager.OnOrientationChanged -= OnOrientationChanged;
            }
        }

        #region UI Event Handlers

        private void OnAutoRotationToggled(bool isOn)
        {
            _orientationManager.AutoRotationEnabled = isOn;

            if (!isOn)
            {
                _orientationManager.SetLockMode(OrientationManager.LockMode.LockCurrent);
            }
            else
            {
                _orientationManager.SetLockMode(OrientationManager.LockMode.Auto);
            }

            UpdateUI();
            _orientationManager.SaveSettings();
        }

        private void SetLockMode(OrientationManager.LockMode mode)
        {
            _orientationManager.SetLockMode(mode);
            UpdateUI();
            _orientationManager.SaveSettings();
        }

        private void OnSensitivityChanged(float value)
        {
            _orientationManager.SetSensitivity(value);

            if (_sensitivityValueText != null)
            {
                _sensitivityValueText.text = $"{value:P0}";
            }

            _orientationManager.SaveSettings();
        }

        private void OnOrientationChanged(OrientationManager.DeviceOrientation orientation)
        {
            UpdateUI();
        }

        #endregion

        #region UI Control

        public void OpenSettings()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
                UpdateUI();
            }
        }

        public void CloseSettings()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        public void ToggleSettings()
        {
            if (_settingsPanel != null)
            {
                bool isActive = !_settingsPanel.activeSelf;
                _settingsPanel.SetActive(isActive);

                if (isActive)
                {
                    UpdateUI();
                }
            }
        }

        private void UpdateUI()
        {
            if (_orientationManager == null) return;

            // 자동 회전 토글
            if (_autoRotationToggle != null)
            {
                _autoRotationToggle.SetIsOnWithoutNotify(_orientationManager.AutoRotationEnabled);
            }

            if (_autoRotationLabel != null)
            {
                _autoRotationLabel.text = _orientationManager.AutoRotationEnabled ? "Auto Rotation: ON" : "Auto Rotation: OFF";
            }

            // 잠금 상태
            if (_lockStatusText != null)
            {
                string status = _orientationManager.IsPortrait ? "Portrait" : "Landscape";
                _lockStatusText.text = $"Current: {status}";
            }

            // 감도 슬라이더
            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.SetValueWithoutNotify(_orientationManager.RotationSensitivity);
            }

            if (_sensitivityValueText != null)
            {
                _sensitivityValueText.text = $"{_orientationManager.RotationSensitivity:P0}";
            }
        }

        #endregion

        #region Auto Create UI

        private void CreateSettingsUI()
        {
            if (_targetCanvas == null)
            {
                _targetCanvas = FindObjectOfType<Canvas>();
                if (_targetCanvas == null)
                {
                    Debug.LogError("[OrientationSettingsUI] No Canvas found!");
                    return;
                }
            }

            // 설정 패널 생성
            _settingsPanel = CreatePanel(_targetCanvas.transform, "OrientationSettingsPanel",
                new Vector2(400, 500), new Color(0.1f, 0.1f, 0.15f, 0.95f));

            RectTransform panelRect = _settingsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            float yOffset = 200f;

            // 제목
            CreateLabel(panelRect, "Title", "Orientation Settings", new Vector2(0, yOffset), 32);
            yOffset -= 60f;

            // 자동 회전 토글
            _autoRotationToggle = CreateToggle(panelRect, "AutoRotationToggle",
                "Auto Rotation", new Vector2(0, yOffset));
            _autoRotationLabel = _autoRotationToggle.GetComponentInChildren<TextMeshProUGUI>();
            yOffset -= 50f;

            // 감도 슬라이더
            CreateLabel(panelRect, "SensitivityLabel", "Sensitivity", new Vector2(0, yOffset), 20);
            yOffset -= 30f;

            _sensitivitySlider = CreateSlider(panelRect, "SensitivitySlider", new Vector2(0, yOffset));
            _sensitivitySlider.minValue = 0.2f;
            _sensitivitySlider.maxValue = 0.8f;
            _sensitivitySlider.value = 0.5f;
            yOffset -= 50f;

            // 잠금 버튼들
            CreateLabel(panelRect, "LockLabel", "Lock Orientation", new Vector2(0, yOffset), 20);
            yOffset -= 40f;

            _lockPortraitButton = CreateButton(panelRect, "LockPortrait", "Portrait", new Vector2(-100, yOffset));
            _lockLandscapeButton = CreateButton(panelRect, "LockLandscape", "Landscape", new Vector2(100, yOffset));
            yOffset -= 50f;

            _lockCurrentButton = CreateButton(panelRect, "LockCurrent", "Lock Current", new Vector2(-100, yOffset));
            _unlockButton = CreateButton(panelRect, "Unlock", "Unlock", new Vector2(100, yOffset));
            yOffset -= 60f;

            // 강제 전환 버튼
            CreateLabel(panelRect, "ForceLabel", "Force Orientation", new Vector2(0, yOffset), 20);
            yOffset -= 40f;

            _forcePortraitButton = CreateButton(panelRect, "ForcePortrait", "To Portrait", new Vector2(-100, yOffset));
            _forceLandscapeButton = CreateButton(panelRect, "ForceLandscape", "To Landscape", new Vector2(100, yOffset));
            yOffset -= 60f;

            // 닫기 버튼
            _closeSettingsButton = CreateButton(panelRect, "CloseSettings", "Close", new Vector2(0, yOffset));
            _closeSettingsButton.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f);

            Debug.Log("[OrientationSettingsUI] Settings UI created");
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = panel.AddComponent<Image>();
            img.color = color;

            // 외곽선
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.6f, 0.2f);
            outline.effectDistance = new Vector2(2, 2);

            return panel;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Vector2 position, int fontSize)
        {
            GameObject labelObj = new GameObject(name);
            labelObj.transform.SetParent(parent, false);

            RectTransform rect = labelObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(350, 40);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            return label;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(160, 40);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.4f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // 버튼 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.text = text;
            btnText.fontSize = 18;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;

            return btn;
        }

        private Toggle CreateToggle(Transform parent, string name, string labelText, Vector2 position)
        {
            GameObject toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 40);

            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // 배경
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(20, 0);
            bgRect.sizeDelta = new Vector2(30, 30);

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.4f);

            // 체크마크
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);

            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(5, 5);
            checkRect.offsetMax = new Vector2(-5, -5);

            Image checkImg = checkObj.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.8f, 0.2f);

            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = true;

            // 라벨
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(60, 0);
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 22;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;

            return toggle;
        }

        private Slider CreateSlider(Transform parent, string name, Vector2 position)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 20);

            Slider slider = sliderObj.AddComponent<Slider>();

            // 배경
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f);

            // 채우기 영역
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);

            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // 채우기
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);

            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 0.9f);

            slider.fillRect = fillRect;

            // 핸들 영역
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);

            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // 핸들
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);

            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            return slider;
        }

        #endregion
    }
}
