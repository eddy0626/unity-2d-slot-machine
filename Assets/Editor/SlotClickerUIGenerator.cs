using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;

/// <summary>
/// SlotClicker UI를 씬에 생성하는 에디터 도구
/// </summary>
public class SlotClickerUIGenerator : EditorWindow
{
    [MenuItem("Tools/SlotClicker/Generate UI Canvas")]
    public static void GenerateUICanvas()
    {
        // 기존 Canvas 확인
        var existingCanvas = GameObject.Find("SlotClickerCanvas");
        if (existingCanvas != null)
        {
            if (!EditorUtility.DisplayDialog("Canvas 존재",
                "SlotClickerCanvas가 이미 존재합니다. 삭제하고 새로 생성하시겠습니까?",
                "예", "아니오"))
            {
                return;
            }
            Undo.DestroyObjectImmediate(existingCanvas);
        }

        // EventSystem 확인
        if (GameObject.FindObjectOfType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Canvas 생성
        var canvasObj = new GameObject("SlotClickerCanvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create SlotClickerCanvas");

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();

        // === UI 요소 생성 ===

        // 1. 클릭 영역 (먼저 - 뒤에 렌더링)
        CreateClickArea(canvasRect);

        // 2. 베팅 UI
        CreateBettingUI(canvasRect);

        // 3. 상단 HUD
        CreateTopHUD(canvasRect);

        // 4. 슬롯 영역
        CreateSlotArea(canvasRect);

        // 5. 결과 텍스트
        CreateResultPanel(canvasRect);

        // 6. 토스트
        CreateToast(canvasRect);

        // 7. 버튼들
        CreateUpgradeButton(canvasRect);
        CreatePrestigeButton(canvasRect);

        // SlotClickerUI 컴포넌트 추가
        var slotClickerUI = canvasObj.AddComponent<SlotClicker.UI.SlotClickerUI>();

        // Selection
        Selection.activeGameObject = canvasObj;

        Debug.Log("[SlotClickerUIGenerator] Canvas 생성 완료! Inspector에서 SlotClickerUI 컴포넌트의 참조를 연결하세요.");
        EditorUtility.DisplayDialog("완료",
            "Canvas가 생성되었습니다.\n\n" +
            "1. SlotClickerUI 컴포넌트의 'Auto Create UI'를 비활성화하세요.\n" +
            "2. Inspector에서 각 UI 참조를 연결하세요.\n" +
            "3. 씬을 저장하세요.",
            "확인");
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    static GameObject CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, int fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400, 60);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;

        return textObj;
    }

    static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color color, int fontSize = 30)
    {
        var btnObj = CreatePanel(parent, name, anchorMin, anchorMax, position, size, color);

        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;

        var textObj = CreateText(btnObj.transform, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, fontSize, Color.white);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    static void CreateTopHUD(RectTransform parent)
    {
        var hudPanel = CreatePanel(parent, "TopHUD", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Color(0.1f, 0.1f, 0.15f, 0.95f));
        var hudRect = hudPanel.GetComponent<RectTransform>();
        hudRect.anchoredPosition = new Vector2(0, -50);
        hudRect.sizeDelta = new Vector2(0, 100);

        // 골드 텍스트
        var goldText = CreateText(hudRect, "GoldText", "GOLD: 0",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -15), 42,
            new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Left);

        // 칩 텍스트
        var chipsText = CreateText(hudRect, "ChipsText", "0 Chips",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -15), 32,
            new Color(0.6f, 0.8f, 1f), TextAlignmentOptions.Right);

        // 통계 텍스트
        var statsText = CreateText(hudRect, "StatsText", "Spins: 0 | Wins: 0",
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 15), 22,
            new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);

        // 승률 텍스트
        var winRateText = CreateText(hudRect, "WinRateText", "Win Rate: --",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 15), 22,
            new Color(0.5f, 0.9f, 0.5f), TextAlignmentOptions.Center);

        // 프레스티지 텍스트
        var prestigeText = CreateText(hudRect, "PrestigeProgressText", "Prestige: 0%",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 15), 22,
            new Color(0.9f, 0.6f, 1f), TextAlignmentOptions.Right);
    }

    static void CreateSlotArea(RectTransform parent)
    {
        var slotPanel = CreatePanel(parent, "SlotPanel", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -180), new Vector2(520, 160), new Color(0.15f, 0.1f, 0.2f, 1f));
        var slotRect = slotPanel.GetComponent<RectTransform>();

        // 아웃라인
        var outline = slotPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.6f, 0.2f);
        outline.effectDistance = new Vector2(4, 4);

        // 스핀 상태 텍스트
        var stateText = CreateText(slotRect, "SpinStateText", "READY",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), 28,
            new Color(0.8f, 0.8f, 0.9f), TextAlignmentOptions.Center);

        // 릴 심볼들
        float spacing = 150f;
        float startX = -spacing;
        Color reelBgColor = new Color(0.2f, 0.15f, 0.25f, 1f);

        for (int i = 0; i < 3; i++)
        {
            var reelBg = CreatePanel(slotRect, $"ReelBg_{i}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (i * spacing), 0), new Vector2(110, 110),
                reelBgColor);

            reelBg.AddComponent<RectMask2D>();

            var symbolObj = new GameObject($"Symbol_{i}");
            symbolObj.transform.SetParent(reelBg.transform, false);
            var symRect = symbolObj.AddComponent<RectTransform>();
            symRect.anchorMin = Vector2.zero;
            symRect.anchorMax = Vector2.one;
            symRect.offsetMin = new Vector2(5, 5);
            symRect.offsetMax = new Vector2(-5, -5);

            var symbolImg = symbolObj.AddComponent<Image>();
            symbolImg.color = GetSymbolColor(i);
            symbolImg.preserveAspect = true;
            symbolImg.raycastTarget = false;
        }
    }

    static void CreateClickArea(RectTransform parent)
    {
        var clickPanel = CreatePanel(parent, "ClickArea", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -80), new Vector2(520, 200), new Color(0.1f, 0.4f, 0.15f, 1f));

        var outline = clickPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.4f, 0.1f);
        outline.effectDistance = new Vector2(5, 5);

        clickPanel.AddComponent<Button>().transition = Selectable.Transition.None;

        var tableText = CreateText(clickPanel.transform, "TableText", "TAP TO EARN",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 48,
            new Color(1f, 0.9f, 0.6f, 0.8f), TextAlignmentOptions.Center);
        tableText.GetComponent<TextMeshProUGUI>().raycastTarget = false;
    }

    static void CreateBettingUI(RectTransform parent)
    {
        var betPanel = CreatePanel(parent, "BetPanel", new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 220), new Color(0.12f, 0.1f, 0.18f, 0.95f));
        var betRect = betPanel.GetComponent<RectTransform>();
        betRect.anchoredPosition = new Vector2(0, 110);

        // 베팅액 텍스트
        var betAmountText = CreateText(betRect, "BetAmountText", "Bet: 0",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), 34,
            Color.white, TextAlignmentOptions.Center);

        // 베팅 버튼들
        string[] betLabels = { "10%", "30%", "50%", "ALL" };
        float buttonWidth = 120f;
        float buttonSpacing = 12f;
        float totalWidth = (buttonWidth * 4) + (buttonSpacing * 3);
        float startX = -totalWidth / 2 + buttonWidth / 2;

        for (int i = 0; i < 4; i++)
        {
            CreateButton(betRect, $"BetBtn_{i}", betLabels[i],
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (i * (buttonWidth + buttonSpacing)), 35),
                new Vector2(buttonWidth, 45),
                new Color(0.3f, 0.3f, 0.5f), 26);
        }

        // 스핀 버튼
        var spinBtn = CreateButton(betRect, "SpinButton", "SPIN!",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-60, 40), new Vector2(180, 60),
            new Color(0.8f, 0.2f, 0.2f), 34);

        // 자동 스핀 버튼
        var autoSpinBtn = CreateButton(betRect, "AutoSpinButton", "AUTO\nx10",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(105, 40), new Vector2(90, 60),
            new Color(0.3f, 0.5f, 0.7f), 22);
    }

    static void CreateResultPanel(RectTransform parent)
    {
        // SlotPanel 찾기
        var slotPanel = parent.Find("SlotPanel");
        Transform resultParent = slotPanel != null ? slotPanel : parent;

        var resultPanel = CreatePanel(resultParent, "ResultPanel",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, -30), new Vector2(480, 55),
            new Color(0f, 0f, 0f, 0.6f));

        var panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.pivot = new Vector2(0.5f, 1f);

        resultPanel.AddComponent<CanvasGroup>().alpha = 0f;

        var resultText = CreateText(panelRect, "ResultText", "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 44,
            Color.white, TextAlignmentOptions.Center);
    }

    static void CreateToast(RectTransform parent)
    {
        var toastPanel = CreatePanel(parent, "ToastPanel",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 290), new Vector2(480, 45),
            new Color(0f, 0f, 0f, 0.6f));

        toastPanel.AddComponent<CanvasGroup>().alpha = 0f;

        var toastText = CreateText(toastPanel.transform, "ToastText", "",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 32,
            Color.white, TextAlignmentOptions.Center);
    }

    static void CreateUpgradeButton(RectTransform parent)
    {
        var btnObj = CreateButton(parent, "UpgradeButton", "UPGRADES",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-90, -165), new Vector2(140, 45),
            new Color(0.4f, 0.3f, 0.7f), 24);
    }

    static void CreatePrestigeButton(RectTransform parent)
    {
        var btnObj = CreateButton(parent, "PrestigeButton", "PRESTIGE",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(90, -165), new Vector2(140, 45),
            new Color(0.6f, 0.3f, 0.6f), 24);
    }

    static Color GetSymbolColor(int index)
    {
        Color[] colors = {
            new Color(1f, 0.3f, 0.3f),
            new Color(0.3f, 1f, 0.3f),
            new Color(0.3f, 0.3f, 1f)
        };
        return colors[index % colors.Length];
    }
}
