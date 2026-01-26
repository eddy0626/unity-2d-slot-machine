using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotClicker.Core;
using SlotClicker.Data;

namespace SlotClicker.UI
{
    /// <summary>
    /// 업그레이드 상점 UI
    /// </summary>
    public class UpgradeUI : MonoBehaviour
    {
        [Header("패널")]
        [SerializeField] private GameObject _upgradePanel;
        [SerializeField] private RectTransform _contentArea;

        [Header("탭 버튼")]
        [SerializeField] private Button _clickTabButton;
        [SerializeField] private Button _slotTabButton;
        [SerializeField] private Button _goldTabButton;

        [Header("색상")]
        [SerializeField] private Color _activeTabColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.5f, 0.5f, 0.5f);

        private GameManager _gameManager;
        private UpgradeManager _upgradeManager;
        private UpgradeCategory _currentCategory = UpgradeCategory.Click;

        private List<UpgradeItemUI> _upgradeItems = new List<UpgradeItemUI>();
        private bool _isInitialized = false;

        // 이벤트 구독용 델리게이트 (메모리 누수 방지)
        private Action<double> _onGoldChangedHandler;

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            _upgradeManager = gameManager.Upgrade;

            if (_upgradeManager == null)
            {
                Debug.LogError("[UpgradeUI] UpgradeManager not found!");
                return;
            }

            // 이벤트 등록 (델리게이트 저장으로 메모리 누수 방지)
            _upgradeManager.OnUpgradesChanged += RefreshUI;
            _onGoldChangedHandler = (_) => RefreshUI();
            _gameManager.Gold.OnGoldChanged += _onGoldChangedHandler;

            _isInitialized = true;

            // 초기 UI 생성
            CreateUpgradePanel();
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_upgradeManager != null)
                _upgradeManager.OnUpgradesChanged -= RefreshUI;

            if (_gameManager?.Gold != null && _onGoldChangedHandler != null)
                _gameManager.Gold.OnGoldChanged -= _onGoldChangedHandler;
        }

        private void CreateUpgradePanel()
        {
            if (_upgradePanel != null) return;

            // 메인 패널 생성
            _upgradePanel = new GameObject("UpgradePanel");
            _upgradePanel.transform.SetParent(transform);

            RectTransform panelRect = _upgradePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = new Vector2(10, 10);
            panelRect.offsetMax = new Vector2(-10, -10);

            Image panelBg = _upgradePanel.AddComponent<Image>();
            panelBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // 레이아웃 그룹
            VerticalLayoutGroup layout = _upgradePanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // 제목
            CreateTitle(_upgradePanel.transform);

            // 탭 버튼 영역
            CreateTabButtons(_upgradePanel.transform);

            // 스크롤 뷰
            CreateScrollView(_upgradePanel.transform);

            // 닫기 버튼
            CreateCloseButton(_upgradePanel.transform);
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 50);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "UPGRADES";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;

            LayoutElement le = titleObj.AddComponent<LayoutElement>();
            le.minHeight = 50;
            le.preferredHeight = 50;
        }

        private void CreateTabButtons(Transform parent)
        {
            GameObject tabContainer = new GameObject("TabContainer");
            tabContainer.transform.SetParent(parent);

            RectTransform tabRect = tabContainer.AddComponent<RectTransform>();
            tabRect.sizeDelta = new Vector2(0, 50);

            HorizontalLayoutGroup tabLayout = tabContainer.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 10;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;

            LayoutElement tabLE = tabContainer.AddComponent<LayoutElement>();
            tabLE.minHeight = 50;
            tabLE.preferredHeight = 50;

            // 탭 버튼 생성
            _clickTabButton = CreateTabButton(tabContainer.transform, "CLICK", () => SetCategory(UpgradeCategory.Click));
            _slotTabButton = CreateTabButton(tabContainer.transform, "SLOT", () => SetCategory(UpgradeCategory.Slot));
            _goldTabButton = CreateTabButton(tabContainer.transform, "GOLD", () => SetCategory(UpgradeCategory.Gold));
        }

        private Button CreateTabButton(Transform parent, string label, Action onClick)
        {
            GameObject btnObj = new GameObject($"Tab_{label}");
            btnObj.transform.SetParent(parent);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = _inactiveTabColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // 버튼 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private void CreateScrollView(Transform parent)
        {
            // 스크롤 뷰 컨테이너
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(parent);

            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            LayoutElement scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;

            // 뷰포트
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(5, 5);
            viewportRect.offsetMax = new Vector2(-5, -5);

            Image viewportMask = viewportObj.AddComponent<Image>();
            viewportMask.color = Color.white;
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            scroll.viewport = viewportRect;

            // 콘텐츠 영역
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform);

            _contentArea = contentObj.AddComponent<RectTransform>();
            _contentArea.anchorMin = new Vector2(0, 1);
            _contentArea.anchorMax = new Vector2(1, 1);
            _contentArea.pivot = new Vector2(0.5f, 1);
            _contentArea.offsetMin = Vector2.zero;
            _contentArea.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 8;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;

            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = _contentArea;
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(parent);

            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(0, 45);

            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.6f, 0.2f, 0.2f);

            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(Hide);

            LayoutElement closeLE = closeObj.AddComponent<LayoutElement>();
            closeLE.minHeight = 45;
            closeLE.preferredHeight = 45;

            // 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = "CLOSE";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void SetCategory(UpgradeCategory category)
        {
            _currentCategory = category;
            UpdateTabVisuals();
            PopulateUpgrades();
        }

        private void UpdateTabVisuals()
        {
            if (_clickTabButton != null)
                _clickTabButton.GetComponent<Image>().color =
                    _currentCategory == UpgradeCategory.Click ? _activeTabColor : _inactiveTabColor;

            if (_slotTabButton != null)
                _slotTabButton.GetComponent<Image>().color =
                    _currentCategory == UpgradeCategory.Slot ? _activeTabColor : _inactiveTabColor;

            if (_goldTabButton != null)
                _goldTabButton.GetComponent<Image>().color =
                    _currentCategory == UpgradeCategory.Gold ? _activeTabColor : _inactiveTabColor;
        }

        private void PopulateUpgrades()
        {
            // 기존 아이템 제거
            foreach (var item in _upgradeItems)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            _upgradeItems.Clear();

            if (_contentArea == null || _upgradeManager == null) return;

            // 해당 카테고리의 업그레이드 정보 가져오기
            List<UpgradeInfo> upgrades = _upgradeManager.GetAllUpgradeInfo(_currentCategory);

            foreach (var info in upgrades)
            {
                UpgradeItemUI item = CreateUpgradeItem(info);
                _upgradeItems.Add(item);
            }
        }

        private UpgradeItemUI CreateUpgradeItem(UpgradeInfo info)
        {
            GameObject itemObj = new GameObject($"Upgrade_{info.Data.id}");
            itemObj.transform.SetParent(_contentArea);

            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 80);

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.2f, 0.2f, 0.25f);

            HorizontalLayoutGroup itemLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
            itemLayout.padding = new RectOffset(10, 10, 10, 10);
            itemLayout.spacing = 10;
            itemLayout.childForceExpandWidth = false;
            itemLayout.childForceExpandHeight = true;
            itemLayout.childControlWidth = true;
            itemLayout.childControlHeight = true;

            LayoutElement itemLE = itemObj.AddComponent<LayoutElement>();
            itemLE.minHeight = 80;
            itemLE.preferredHeight = 80;

            // 정보 영역
            GameObject infoArea = CreateInfoArea(itemObj.transform, info);

            // 구매 버튼
            Button buyButton = CreateBuyButton(itemObj.transform, info);

            // 컴포넌트 추가
            UpgradeItemUI itemUI = itemObj.AddComponent<UpgradeItemUI>();
            itemUI.Initialize(info, buyButton, infoArea.GetComponentInChildren<Text>(),
                () => _upgradeManager.TryPurchase(info.Data.id));

            return itemUI;
        }

        private GameObject CreateInfoArea(Transform parent, UpgradeInfo info)
        {
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(parent);

            RectTransform infoRect = infoObj.AddComponent<RectTransform>();

            VerticalLayoutGroup infoLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.spacing = 2;

            LayoutElement infoLE = infoObj.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;

            // 이름 + 레벨
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(infoObj.transform);

            Text nameText = nameObj.AddComponent<Text>();
            nameText.text = $"{info.Data.name}  Lv.{info.CurrentLevel}";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.white;

            // 설명
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(infoObj.transform);

            Text descText = descObj.AddComponent<Text>();
            descText.text = info.Data.description;
            descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descText.fontSize = 14;
            descText.color = new Color(0.7f, 0.7f, 0.7f);

            // 효과
            GameObject effectObj = new GameObject("Effect");
            effectObj.transform.SetParent(infoObj.transform);

            Text effectText = effectObj.AddComponent<Text>();
            effectText.text = $"효과: {info.Data.GetEffectDescription(info.CurrentLevel)}";
            effectText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            effectText.fontSize = 14;
            effectText.color = new Color(0.4f, 0.8f, 0.4f);

            return infoObj;
        }

        private Button CreateBuyButton(Transform parent, UpgradeInfo info)
        {
            GameObject btnObj = new GameObject("BuyButton");
            btnObj.transform.SetParent(parent);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = info.CanAfford ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.interactable = info.CanAfford && !info.IsMaxLevel;

            LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
            btnLE.minWidth = 100;
            btnLE.preferredWidth = 100;

            // 버튼 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            if (info.IsMaxLevel)
            {
                text.text = "MAX";
            }
            else
            {
                text.text = FormatGold(info.CurrentCost);
            }
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private void RefreshUI()
        {
            if (!_isInitialized) return;
            PopulateUpgrades();
            UpdateTabVisuals();
        }

        public void Show()
        {
            if (_upgradePanel != null)
            {
                _upgradePanel.SetActive(true);
                SetCategory(_currentCategory);
            }
        }

        public void Hide()
        {
            if (_upgradePanel != null)
                _upgradePanel.SetActive(false);
        }

        public void Toggle()
        {
            if (_upgradePanel != null)
            {
                if (_upgradePanel.activeSelf)
                    Hide();
                else
                    Show();
            }
        }

        private string FormatGold(double amount)
        {
            if (amount >= 1_000_000_000_000) return $"{amount / 1_000_000_000_000:F1}T";
            if (amount >= 1_000_000_000) return $"{amount / 1_000_000_000:F1}B";
            if (amount >= 1_000_000) return $"{amount / 1_000_000:F1}M";
            if (amount >= 1_000) return $"{amount / 1_000:F1}K";
            return amount.ToString("N0");
        }
    }

    /// <summary>
    /// 개별 업그레이드 아이템 UI
    /// </summary>
    public class UpgradeItemUI : MonoBehaviour
    {
        private UpgradeInfo _info;
        private Button _buyButton;
        private Text _nameText;
        private Action _onPurchase;

        public void Initialize(UpgradeInfo info, Button buyButton, Text nameText, Action onPurchase)
        {
            _info = info;
            _buyButton = buyButton;
            _nameText = nameText;
            _onPurchase = onPurchase;

            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyClicked);
            }
        }

        private void OnBuyClicked()
        {
            _onPurchase?.Invoke();
        }

        public void UpdateDisplay(UpgradeInfo newInfo)
        {
            _info = newInfo;
            // UI 갱신은 부모에서 재생성으로 처리
        }
    }
}
