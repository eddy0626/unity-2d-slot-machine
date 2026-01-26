using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotClicker.Core;
using DG.Tweening;

namespace SlotClicker.UI
{
    public class PrestigeUI : MonoBehaviour
    {
        [Header("패널")]
        [SerializeField] private GameObject _prestigePanel;
        [SerializeField] private GameObject _charmShopPanel;

        [Header("프레스티지 정보")]
        [SerializeField] private TextMeshProUGUI _vipRankText;
        [SerializeField] private TextMeshProUGUI _totalChipsText;
        [SerializeField] private TextMeshProUGUI _prestigeCountText;
        [SerializeField] private TextMeshProUGUI _totalGoldEarnedText;
        [SerializeField] private TextMeshProUGUI _chipsToGainText;
        [SerializeField] private TextMeshProUGUI _currentBonusText;
        [SerializeField] private TextMeshProUGUI _nextBonusText;

        [Header("프레스티지 버튼")]
        [SerializeField] private Button _prestigeButton;
        [SerializeField] private TextMeshProUGUI _prestigeButtonText;

        [Header("탭 버튼")]
        [SerializeField] private Button _prestigeTabButton;
        [SerializeField] private Button _charmShopTabButton;

        [Header("럭키참 상점")]
        [SerializeField] private Transform _charmListContainer;
        [SerializeField] private GameObject _charmItemPrefab;

        private GameManager _gameManager;
        private PrestigeManager _prestige;
        private List<GameObject> _charmItems = new List<GameObject>();

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            _prestige = gameManager.Prestige;

            SetupButtons();
            SetupTabs();

            // 이벤트 구독
            _prestige.OnPrestigeComplete += OnPrestigeComplete;
            _prestige.OnCharmPurchased += OnCharmPurchased;
            _prestige.OnVIPRankChanged += OnVIPRankChanged;
            _gameManager.OnGameStateChanged += RefreshUI;

            RefreshUI();
            ShowPrestigeTab();

            Debug.Log("[PrestigeUI] Initialized");
        }

        private void OnDestroy()
        {
            if (_prestige != null)
            {
                _prestige.OnPrestigeComplete -= OnPrestigeComplete;
                _prestige.OnCharmPurchased -= OnCharmPurchased;
                _prestige.OnVIPRankChanged -= OnVIPRankChanged;
            }
            if (_gameManager != null)
            {
                _gameManager.OnGameStateChanged -= RefreshUI;
            }
        }

        private void SetupButtons()
        {
            if (_prestigeButton != null)
            {
                _prestigeButton.onClick.AddListener(OnPrestigeButtonClicked);
                UIFeedback.AddButtonFeedback(_prestigeButton);
            }
        }

        private void SetupTabs()
        {
            if (_prestigeTabButton != null)
            {
                _prestigeTabButton.onClick.AddListener(ShowPrestigeTab);
                UIFeedback.AddButtonFeedback(_prestigeTabButton);
            }
            if (_charmShopTabButton != null)
            {
                _charmShopTabButton.onClick.AddListener(ShowCharmShopTab);
                UIFeedback.AddButtonFeedback(_charmShopTabButton);
            }
        }

        private void ShowPrestigeTab()
        {
            if (_prestigePanel != null) _prestigePanel.SetActive(true);
            if (_charmShopPanel != null) _charmShopPanel.SetActive(false);
            RefreshPrestigeInfo();
        }

        private void ShowCharmShopTab()
        {
            if (_prestigePanel != null) _prestigePanel.SetActive(false);
            if (_charmShopPanel != null) _charmShopPanel.SetActive(true);
            RefreshCharmShop();
        }

        public void RefreshUI()
        {
            RefreshPrestigeInfo();
            if (_charmShopPanel != null && _charmShopPanel.activeSelf)
            {
                RefreshCharmShop();
            }
        }

        #region 프레스티지 정보

        private void RefreshPrestigeInfo()
        {
            if (_prestige == null) return;

            // VIP 등급
            if (_vipRankText != null)
            {
                VIPRank rank = _prestige.CurrentVIPRank;
                string rankName = _prestige.GetVIPRankName(rank);
                string rankColor = GetVIPRankColor(rank);
                _vipRankText.text = $"<color={rankColor}>{rankName}</color>";
            }

            // 칩 보유량
            if (_totalChipsText != null)
            {
                _totalChipsText.text = $"{_prestige.TotalChips}";
            }

            // 프레스티지 횟수
            if (_prestigeCountText != null)
            {
                int nextThreshold = _prestige.GetNextVIPThreshold();
                string nextText = nextThreshold > 0 ? $"(다음 등급: {nextThreshold}회)" : "(최고 등급)";
                _prestigeCountText.text = $"{_prestige.PrestigeCount}회 {nextText}";
            }

            // 총 획득 골드
            if (_totalGoldEarnedText != null)
            {
                double totalGold = _gameManager.PlayerData.totalGoldEarned;
                _totalGoldEarnedText.text = GoldManager.FormatNumber(totalGold);
            }

            // 획득 예정 칩
            int chipsToGain = _prestige.CalculateChipsToGain();
            if (_chipsToGainText != null)
            {
                _chipsToGainText.text = chipsToGain > 0 ? $"+{chipsToGain}" : "0";
                _chipsToGainText.color = chipsToGain > 0 ? Color.green : Color.gray;
            }

            // 현재 보너스
            if (_currentBonusText != null)
            {
                float bonus = (_prestige.GetPrestigeBonus() - 1f) * 100f;
                float vipBonus = _prestige.GetVIPBonus() * 100f;
                _currentBonusText.text = $"칩 보너스: +{bonus:F0}%\nVIP 보너스: +{vipBonus:F0}%";
            }

            // 다음 보너스 (프레스티지 후)
            if (_nextBonusText != null && chipsToGain > 0)
            {
                int futureChips = _prestige.TotalChips + chipsToGain;
                float futureBonus = futureChips * 10f; // 칩당 10%
                _nextBonusText.text = $"프레스티지 후: +{futureBonus:F0}%";
            }
            else if (_nextBonusText != null)
            {
                _nextBonusText.text = "골드를 더 모으세요";
            }

            // 버튼 상태
            UpdatePrestigeButton(chipsToGain);
        }

        private void UpdatePrestigeButton(int chipsToGain)
        {
            if (_prestigeButton == null) return;

            bool canPrestige = chipsToGain > 0;
            _prestigeButton.interactable = canPrestige;

            if (_prestigeButtonText != null)
            {
                if (canPrestige)
                {
                    _prestigeButtonText.text = $"프레스티지 (+{chipsToGain} 칩)";
                }
                else
                {
                    double current = _gameManager.PlayerData.totalGoldEarned;
                    double required = 1_000_000;
                    double progress = current / required * 100;
                    _prestigeButtonText.text = $"목표: 1M 골드 ({progress:F1}%)";
                }
            }
        }

        private string GetVIPRankColor(VIPRank rank)
        {
            return rank switch
            {
                VIPRank.Bronze => "#CD7F32",
                VIPRank.Silver => "#C0C0C0",
                VIPRank.Gold => "#FFD700",
                VIPRank.Platinum => "#E5E4E2",
                VIPRank.Diamond => "#B9F2FF",
                _ => "#FFFFFF"
            };
        }

        private void OnPrestigeButtonClicked()
        {
            if (!_prestige.CanPrestige()) return;

            int chipsToGain = _prestige.CalculateChipsToGain();

            // 확인 팝업 표시
            string title = "프레스티지 확인";
            string message = $"프레스티지를 실행하면:\n\n" +
                           $"<color=#FF6666>- 모든 골드가 초기화됩니다</color>\n" +
                           $"<color=#FF6666>- 모든 업그레이드가 초기화됩니다</color>\n\n" +
                           $"<color=#66FF66>+ {chipsToGain} 칩을 획득합니다</color>\n" +
                           $"<color=#66FF66>+ 영구 보너스가 증가합니다</color>\n\n" +
                           $"계속하시겠습니까?";

            UIFeedback.Instance.ShowConfirmPopup(
                title,
                message,
                onConfirm: () => ExecutePrestigeWithFeedback(chipsToGain),
                onCancel: null,
                confirmText: "프레스티지!",
                cancelText: "취소",
                confirmColor: new Color(0.8f, 0.4f, 0.8f) // 보라색
            );
        }

        private void ExecutePrestigeWithFeedback(int chipsToGain)
        {
            _prestige.ExecutePrestige();

            // 성공 피드백
            UIFeedback.Instance.ShowToast(
                $"프레스티지 완료! +{chipsToGain} 칩 획득!",
                new Color(1f, 0.8f, 0.2f),
                3f,
                ToastType.Success
            );
        }

        private void OnPrestigeComplete()
        {
            RefreshUI();

            // 애니메이션 효과
            if (_totalChipsText != null)
            {
                _totalChipsText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5);
            }

            Debug.Log("[PrestigeUI] Prestige complete!");
        }

        private void OnVIPRankChanged(VIPRank newRank)
        {
            RefreshUI();

            // VIP 등급 업 효과
            if (_vipRankText != null)
            {
                _vipRankText.transform.DOPunchScale(Vector3.one * 0.5f, 0.5f, 5);
            }

            // VIP 등급 업 축하 토스트
            string rankName = _prestige.GetVIPRankName(newRank);
            string rankColor = GetVIPRankColor(newRank);
            float vipBonus = _prestige.GetVIPBonus() * 100f;

            UIFeedback.Instance.ShowToast(
                $"🎉 VIP 등급 상승! {rankName} (+{vipBonus:F0}% 보너스)",
                new Color(1f, 0.85f, 0.2f),
                3f,
                ToastType.Success
            );

            Debug.Log($"[PrestigeUI] VIP Rank upgraded to: {newRank}");
        }

        #endregion

        #region 럭키참 상점

        private void RefreshCharmShop()
        {
            if (_charmListContainer == null || _charmItemPrefab == null) return;

            // 기존 아이템 제거
            foreach (var item in _charmItems)
            {
                Destroy(item);
            }
            _charmItems.Clear();

            // 티어별 럭키참 표시
            List<LuckyCharmData> allCharms = _prestige.GetAllCharms();

            foreach (var charm in allCharms)
            {
                CreateCharmItem(charm);
            }
        }

        private void CreateCharmItem(LuckyCharmData charm)
        {
            GameObject item = Instantiate(_charmItemPrefab, _charmListContainer);
            _charmItems.Add(item);

            // 이름
            TextMeshProUGUI nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                string tierStars = new string('*', charm.tier);
                nameText.text = $"{tierStars} {charm.name}";
            }

            // 설명
            TextMeshProUGUI descText = item.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            if (descText != null)
            {
                descText.text = charm.description;
            }

            // 비용
            TextMeshProUGUI costText = item.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            if (costText != null)
            {
                costText.text = $"{charm.chipCost}";
            }

            // 버튼
            Button buyButton = item.transform.Find("BuyButton")?.GetComponent<Button>();
            TextMeshProUGUI buttonText = buyButton?.GetComponentInChildren<TextMeshProUGUI>();

            bool owned = _prestige.OwnsCharm(charm.id);
            bool canPurchase = _prestige.CanPurchaseCharm(charm.id);
            bool rankLocked = _prestige.CurrentVIPRank < charm.requiredRank;

            if (buyButton != null)
            {
                buyButton.interactable = canPurchase;

                string charmId = charm.id; // 클로저용
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => OnCharmBuyClicked(charmId));
            }

            if (buttonText != null)
            {
                if (owned)
                {
                    buttonText.text = "보유중";
                }
                else if (rankLocked)
                {
                    buttonText.text = $"{_prestige.GetVIPRankName(charm.requiredRank)} 필요";
                }
                else if (!canPurchase)
                {
                    buttonText.text = "칩 부족";
                }
                else
                {
                    buttonText.text = "구매";
                }
            }

            // 보유 시 시각적 표시
            Image bg = item.GetComponent<Image>();
            if (bg != null)
            {
                if (owned)
                {
                    bg.color = new Color(0.3f, 0.6f, 0.3f, 0.5f); // 녹색
                }
                else if (rankLocked)
                {
                    bg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 회색
                }
            }
        }

        private void OnCharmBuyClicked(string charmId)
        {
            // 구매 전 칩 수량 확인
            int previousChips = _prestige.TotalChips;

            if (_prestige.TryPurchaseCharm(charmId))
            {
                RefreshUI();

                // 구매 성공 피드백
                var charmData = _prestige.GetAllCharms().Find(c => c.id == charmId);
                string charmName = charmData != null ? charmData.name : "럭키참";

                UIFeedback.Instance.ShowToast(
                    $"✨ {charmName} 구매 완료!",
                    new Color(0.4f, 1f, 0.6f),
                    2f,
                    ToastType.Success
                );
            }
            else
            {
                // 구매 실패 피드백
                if (_prestige.TotalChips < _prestige.GetAllCharms().Find(c => c.id == charmId)?.chipCost)
                {
                    UIFeedback.Instance.ShowToast(
                        "칩이 부족합니다!",
                        new Color(1f, 0.4f, 0.4f),
                        2f,
                        ToastType.Error
                    );
                }
            }
        }

        private void OnCharmPurchased(string charmId)
        {
            RefreshCharmShop();
            RefreshPrestigeInfo();

            Debug.Log($"[PrestigeUI] Charm purchased: {charmId}");
        }

        #endregion

        #region 공개 메서드

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshUI();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (gameObject.activeSelf)
                Hide();
            else
                Show();
        }

        #endregion
    }
}
