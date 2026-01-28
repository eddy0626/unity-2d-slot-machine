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
        #region Help System

        private Button _helpButton;
        private GameObject _helpPanel;
        private bool _isHelpVisible = false;

        private void CreateHelpButton(RectTransform parent)
        {
            // 도움말 버튼 (하단 베팅 영역 우측에 배치 - 항상 보이도록)
            GameObject btnObj = CreateButton(parent, "HelpButton", "?",
                new Vector2(1, 0), new Vector2(1, 0),  // 하단 우측 앵커
                new Vector2(-16.014f, 72.062f), new Vector2(Layout.HelpButtonSize, Layout.HelpButtonSize),  // 하단에서 위로
                new Color(0.3f, 0.7f, 0.9f));

            _helpButton = btnObj.GetComponent<Button>();
            _helpButton.onClick.AddListener(ToggleHelpPanel);

            // 버튼 텍스트 크게
            var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontSize = Layout.HelpButtonFont;
                btnText.fontStyle = FontStyles.Bold;
            }

            // 눈에 띄게 펄스 애니메이션
            btnObj.transform.DOScale(1.1f, 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void CreateHelpUI()
        {
            // 도움말 패널 (화면 중앙, 큰 팝업)
            _helpPanel = new GameObject("HelpPanel");
            _helpPanel.transform.SetParent(_mainCanvas.transform, false);

            RectTransform rect = _helpPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.08f);
            rect.anchorMax = new Vector2(0.95f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 배경
            Image bg = _helpPanel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.12f, 0.98f);

            // 스크롤 가능한 내용을 위한 컨테이너
            CreateHelpContent(rect);

            _helpPanel.SetActive(false);
        }

        private void CreateHelpContent(RectTransform parent)
        {
            // 제목
            GameObject titleObj = CreateTextObject(parent, "HelpTitle", "[SLOT] 슬롯 게임 가이드",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12.01f), 16.815f);
            var titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.color = new Color(1f, 0.85f, 0.3f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;

            // 닫기 버튼
            GameObject closeBtn = CreateButton(parent, "CloseHelp", "X",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12.01f, -12.01f), new Vector2(Layout.HelpCloseSize, Layout.HelpCloseSize),
                new Color(0.7f, 0.3f, 0.3f));
            closeBtn.GetComponent<Button>().onClick.AddListener(ToggleHelpPanel);
            var closeText = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (closeText != null) closeText.fontSize = Layout.HelpCloseFont;

            // === 페이라인 설명 섹션 ===
            float yPos = -36.031f;

            CreateHelpSection(parent, ">> 페이라인 (당첨 라인)", ref yPos);
            CreateHelpText(parent, "3x3 슬롯에서 같은 심볼 3개가 라인에 맞으면 당첨!", ref yPos);
            CreateHelpText(parent, "", ref yPos);  // 공백

            // 페이라인 그림 설명
            CreatePaylineVisual(parent, ref yPos);

            yPos -= 8.007f;

            // === 배당률 설명 섹션 ===
            CreateHelpSection(parent, "$ 배당률 (베팅액 기준)", ref yPos);

            string[] payoutInfo = {
                "• 미니윈 (2줄 일치): 2.0배",
                "• 스몰윈 (3줄 일치): 2.5배",
                "• 빅윈 (희귀 심볼): 5.0배",
                "• 잭팟 (특수 조합): 10배",
                "• 메가잭팟 (최고): 100배!"
            };

            foreach (string info in payoutInfo)
            {
                CreateHelpText(parent, info, ref yPos, 11.21f, new Color(0.8f, 1f, 0.8f));
            }

            yPos -= 6.005f;

            // === 연승 콤보 설명 ===
            CreateHelpSection(parent, "* 연승 콤보 보너스", ref yPos);
            CreateHelpText(parent, "연속 당첨 시 보너스 배율 증가!", ref yPos);
            CreateHelpText(parent, "• 2연승: +10% / 3연승: +20%", ref yPos, 10.409f, new Color(1f, 0.9f, 0.6f));
            CreateHelpText(parent, "• 5연승: +50% / 10연승: +100%!", ref yPos, 10.409f, new Color(1f, 0.9f, 0.6f));

            yPos -= 6.005f;

            // === 게임 팁 ===
            CreateHelpSection(parent, "! 게임 팁", ref yPos);
            CreateHelpText(parent, "• 자동수집을 먼저 구매하세요!", ref yPos, 10.409f, new Color(0.7f, 0.9f, 1f));
            CreateHelpText(parent, "• 50K 골드에서 첫 프레스티지 가능", ref yPos, 10.409f, new Color(0.7f, 0.9f, 1f));
            CreateHelpText(parent, "• AUTO 버튼으로 자동 스핀!", ref yPos, 10.409f, new Color(0.7f, 0.9f, 1f));
        }

        private void CreateHelpSection(RectTransform parent, string title, ref float yPos)
        {
            yPos -= 6.005f;
            GameObject sectionObj = CreateTextObject(parent, "Section", title,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(8.007f, yPos), 12.81f);
            var sectionText = sectionObj.GetComponent<TextMeshProUGUI>();
            sectionText.color = new Color(1f, 0.7f, 0.4f);
            sectionText.fontStyle = FontStyles.Bold;
            sectionText.alignment = TextAlignmentOptions.Left;

            var sectionRect = sectionObj.GetComponent<RectTransform>();
            sectionRect.sizeDelta = new Vector2(-16.014f, 16.014f);
            sectionRect.anchoredPosition = new Vector2(0, yPos);

            yPos -= 18.015f;
        }

        private void CreateHelpText(RectTransform parent, string text, ref float yPos, float fontSize = 10.409f, Color? color = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                yPos -= 4.003f;
                return;
            }

            GameObject textObj = CreateTextObject(parent, "HelpText", text,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(12.01f, yPos), fontSize);
            var helpText = textObj.GetComponent<TextMeshProUGUI>();
            helpText.color = color ?? new Color(0.9f, 0.9f, 0.9f);
            helpText.alignment = TextAlignmentOptions.Left;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(-24.021f, 14.012f);
            textRect.anchoredPosition = new Vector2(0, yPos);

            yPos -= 14.012f;
        }

        private void CreatePaylineVisual(RectTransform parent, ref float yPos)
        {
            // 페이라인 시각적 설명 (간단한 그리드)
            float startY = yPos;
            float gridSize = 16.014f;
            float spacing = 3.203f;
            float startX = 12.01f;

            // 3x3 그리드 + 라인 설명
            string[,] gridLabels = {
                { "0", "1", "2" },
                { "3", "4", "5" },
                { "6", "7", "8" }
            };

            // 그리드 배경
            GameObject gridBg = CreatePanel(parent, "GridBg",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(startX + 28.024f, yPos - 28.024f), new Vector2(64.056f, 64.056f),
                new Color(0.2f, 0.2f, 0.25f, 0.8f));

            // 페이라인 설명 텍스트
            string[] lineDescriptions = {
                "─ 가로 3줄",
                "╲ ╱ 대각선 2줄",
                "= 총 5개 라인"
            };

            float descX = startX + 80.069f;
            float descY = yPos - 12.01f;

            foreach (string desc in lineDescriptions)
            {
                GameObject descObj = CreateTextObject(parent, "LineDesc", desc,
                    new Vector2(0, 1), new Vector2(0, 1), new Vector2(descX, descY), 9.608f);
                var descText = descObj.GetComponent<TextMeshProUGUI>();
                descText.color = new Color(0.8f, 0.8f, 0.9f);
                descY -= 14.012f;
            }

            // 예시 설명
            GameObject exampleObj = CreateTextObject(parent, "Example", "예: [7][7][7] = 당첨!",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(descX, descY - 4.003f), 10.409f);
            var exampleText = exampleObj.GetComponent<TextMeshProUGUI>();
            exampleText.color = new Color(0.5f, 1f, 0.5f);

            yPos -= 72.062f;
        }

        private void ToggleHelpPanel()
        {
            _isHelpVisible = !_isHelpVisible;
            if (_helpPanel != null)
            {
                _helpPanel.SetActive(_isHelpVisible);

                // 열릴 때 애니메이션
                if (_isHelpVisible)
                {
                    _helpPanel.transform.localScale = Vector3.one * 0.8f;
                    _helpPanel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                }
            }
        }

        #endregion
    }
}
