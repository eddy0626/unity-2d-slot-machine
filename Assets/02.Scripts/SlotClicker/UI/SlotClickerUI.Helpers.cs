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
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(160.139f, 24.021f);

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
                Vector2.zero, Vector2.one, Vector2.zero, Layout.DefaultButtonLabelFont);
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

        private void LoadSymbolSprites()
        {
            // Resources 폴더에서 스프라이트 시트 로드
            _symbolSprites = Resources.LoadAll<Sprite>("Sprites/SymbolSprites");

            if (_symbolSprites == null || _symbolSprites.Length == 0)
            {
                Debug.LogWarning("[SlotClickerUI] Failed to load from 'Sprites/SymbolSprites', trying alternative paths...");

                // 대안 경로 시도
                _symbolSprites = Resources.LoadAll<Sprite>("SymbolSprites");

                if (_symbolSprites == null || _symbolSprites.Length == 0)
                {
                    Debug.LogError("[SlotClickerUI] Could not load symbol sprites! Using fallback colors.");
                    _symbolSprites = null;
                }
            }

            if (_symbolSprites != null && _symbolSprites.Length > 0)
            {
                Debug.Log($"[SlotClickerUI] Successfully loaded {_symbolSprites.Length} symbol sprites");
                // 스프라이트 이름 로깅
                for (int i = 0; i < Mathf.Min(3, _symbolSprites.Length); i++)
                {
                    Debug.Log($"  - Sprite {i}: {_symbolSprites[i].name}");
                }
            }
        }

        /// <summary>
        /// 커스텀 UI 스프라이트 로드 (배경, 터치 패널)
        /// Resources/UI/ 폴더에서 자동 로드
        /// </summary>
        private void LoadCustomUISprites()
        {
            // Inspector에서 설정되지 않은 경우에만 Resources에서 로드 시도
            if (_backgroundSprite == null)
            {
                // Resources/UI/ 폴더에서 로드 (가장 우선)
                Sprite[] bgSprites = Resources.LoadAll<Sprite>("UI/백그라운드 일러스트");

                if (bgSprites != null && bgSprites.Length > 0)
                {
                    _backgroundSprite = bgSprites[0];
                    Debug.Log($"[SlotClickerUI] Background sprite auto-loaded: {_backgroundSprite.name}");
                }
            }

            if (_clickPanelSprite == null)
            {
                // Resources/UI/ 폴더에서 로드
                Sprite[] panelSprites = Resources.LoadAll<Sprite>("UI/터치영역 테이블(패널)");

                if (panelSprites != null && panelSprites.Length > 0)
                {
                    // 첫 번째 슬라이스 사용 (_0)
                    _clickPanelSprite = panelSprites[0];
                    Debug.Log($"[SlotClickerUI] Click panel sprite auto-loaded: {_clickPanelSprite.name}");
                }
            }

            // 버튼 스프라이트 로드 (스프라이트 시트에서 모든 스프라이트 로드)
            if (_allButtonSprites == null || _allButtonSprites.Length == 0)
            {
                _allButtonSprites = Resources.LoadAll<Sprite>("UI/배팅_스핀버튼");

                if (_allButtonSprites != null && _allButtonSprites.Length > 0)
                {
                    Debug.Log($"[SlotClickerUI] Button sprites loaded: {_allButtonSprites.Length} sprites");

                    // 스프라이트 시트 구성 (0-19번 스프라이트):
                    // _0~_3: 베팅 버튼 노멀 상태 (10%, 30%, 50%, ALL)
                    // _4~_7: 베팅 버튼 선택 상태
                    // _8~_9: 스핀 버튼 (노멀, 프레스)
                    // _10~_13: 오토 버튼 (노멀, 프레스, 활성, 비활성)
                    // 나머지는 여분

                    // 버튼별 스프라이트 할당
                    _bet10Sprite = GetSpriteByIndex(0);   // 10% 버튼
                    _bet30Sprite = GetSpriteByIndex(1);   // 30% 버튼
                    _bet50Sprite = GetSpriteByIndex(2);   // 50% 버튼
                    _betAllSprite = GetSpriteByIndex(3);  // ALL 버튼
                    _spinSprite = GetSpriteByIndex(8);    // SPIN 버튼 (큰 버튼)
                    _autoSpinSprite = GetSpriteByIndex(10); // AUTO 버튼 (작은 버튼)

                    Debug.Log($"[SlotClickerUI] Button sprites assigned - Bet10:{_bet10Sprite?.name}, Spin:{_spinSprite?.name}, Auto:{_autoSpinSprite?.name}");
                }
            }

            // 코인 스프라이트 로드 (플로팅 텍스트용)
            if (_coinSprite == null)
            {
                Sprite[] coinSprites = Resources.LoadAll<Sprite>("UI/코인");

                if (coinSprites != null && coinSprites.Length > 0)
                {
                    // 첫 번째 스프라이트(코인_0)만 사용
                    _coinSprite = coinSprites[0];
                    Debug.Log($"[SlotClickerUI] Coin sprite auto-loaded: {_coinSprite.name}");
                }
            }

            // 로드 결과 로깅
            if (_backgroundSprite != null)
                Debug.Log($"[SlotClickerUI] ✓ Background ready: {_backgroundSprite.name} ({_backgroundSprite.rect.width}x{_backgroundSprite.rect.height})");
            else
                Debug.LogWarning("[SlotClickerUI] ✗ No background sprite - using default color");

            if (_clickPanelSprite != null)
                Debug.Log($"[SlotClickerUI] ✓ Click panel ready: {_clickPanelSprite.name}");
            else
                Debug.LogWarning("[SlotClickerUI] ✗ No click panel sprite - using default color");

            if (_coinSprite != null)
                Debug.Log($"[SlotClickerUI] ✓ Coin sprite ready: {_coinSprite.name}");
            else
                Debug.LogWarning("[SlotClickerUI] ✗ No coin sprite - floating text will show without coin icon");
        }

        /// <summary>
        /// 배경 이미지 생성
        /// </summary>
        private void CreateBackground(RectTransform parent)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(parent, false);
            bgObj.transform.SetAsFirstSibling(); // 가장 뒤에 렌더링되도록

            RectTransform rect = bgObj.AddComponent<RectTransform>();
            // 전체 화면 채우기
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _backgroundImage = bgObj.AddComponent<Image>();

            if (_backgroundSprite != null)
            {
                _backgroundImage.sprite = _backgroundSprite;
                _backgroundImage.type = Image.Type.Sliced; // 9-slice 지원
                _backgroundImage.preserveAspect = false;
                _backgroundImage.color = Color.white;
                Debug.Log("[SlotClickerUI] Custom background applied");
            }
            else
            {
                // 기본 그라디언트 배경색 (커스텀 스프라이트 없을 때)
                _backgroundImage.color = new Color(0.08f, 0.06f, 0.12f, 1f);
            }

            // 배경은 클릭 이벤트 받지 않음
            _backgroundImage.raycastTarget = false;
        }

        /// <summary>
        /// 기존 UI에 커스텀 스프라이트 적용 (SetupExistingUI용)
        /// </summary>
        private void ApplyCustomSpritesToExistingUI()
        {
            // 배경 이미지 찾기 또는 생성
            Transform bgTransform = _mainCanvas.transform.Find("Background");
            if (bgTransform == null && _backgroundSprite != null)
            {
                // 배경이 없으면 생성
                CreateBackground(_mainCanvas.GetComponent<RectTransform>());
            }
            else if (bgTransform != null && _backgroundSprite != null)
            {
                // 기존 배경에 스프라이트 적용
                _backgroundImage = bgTransform.GetComponent<Image>();
                if (_backgroundImage != null)
                {
                    _backgroundImage.sprite = _backgroundSprite;
                    _backgroundImage.type = Image.Type.Sliced;
                    _backgroundImage.color = Color.white;
                }
            }

            // 클릭 영역에 스프라이트 적용
            if (_clickArea != null && _clickPanelSprite != null)
            {
                Image clickImage = _clickArea.GetComponent<Image>();
                if (clickImage != null)
                {
                    clickImage.sprite = _clickPanelSprite;
                    clickImage.type = Image.Type.Sliced;
                    clickImage.color = Color.white;
                    Debug.Log("[SlotClickerUI] Custom click panel sprite applied to existing UI");
                }
            }
        }

        private Sprite GetSymbolSprite(int index)
        {
            if (_symbolSprites != null && _symbolSprites.Length > 0 && index >= 0)
            {
                int safeIndex = index % _symbolSprites.Length;
                return _symbolSprites[safeIndex];
            }
            return null;
        }

        private void CreateUpgradeButton(RectTransform parent)
        {
            // 업그레이드 버튼 (화면 오른쪽 상단, HUD 바로 아래 - HUD끝 -150 + 10px 간격)
            GameObject btnObj = CreateButton(parent, "UpgradeButton", "UPGRADES",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-36.031f, -66.056f), new Vector2(Layout.UpgradeButtonWidth, Layout.UpgradeButtonHeight),
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
            _upgradeUI.Initialize(_game, _upgradeLayoutProfile);
            _upgradeUI.Hide();
        }

        private void OnUpgradeButtonClicked()
        {
            if (_upgradeUI != null)
            {
                _upgradeUI.Toggle();
            }
        }

        private void CreatePrestigeButton(RectTransform parent)
        {
            // 프레스티지 버튼 (화면 왼쪽 상단, HUD 바로 아래 - HUD끝 -150 + 10px 간격)
            GameObject btnObj = CreateButton(parent, "PrestigeButton", "PRESTIGE",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(36.031f, -66.056f), new Vector2(Layout.PrestigeButtonWidth, Layout.PrestigeButtonHeight),
                new Color(0.6f, 0.3f, 0.6f));

            _prestigeButton = btnObj.GetComponent<Button>();
            _prestigeButton.onClick.AddListener(OnPrestigeButtonClicked);

            // 아이콘 효과 (반짝임)
            btnObj.transform.DOScale(1.05f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void CreatePrestigeUI()
        {
            GameObject prestigeUIObj = new GameObject("PrestigeUI");
            prestigeUIObj.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = prestigeUIObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.1f);
            rect.anchorMax = new Vector2(0.95f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 배경 패널
            Image bg = prestigeUIObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.15f, 0.98f);

            _prestigeUI = prestigeUIObj.AddComponent<PrestigeUI>();
            _prestigeUI.Initialize(_game);
            _prestigeUI.Hide();
        }

        private void OnPrestigeButtonClicked()
        {
            if (_prestigeUI != null)
            {
                _prestigeUI.Toggle();
            }
        }

        #endregion
    }
}
