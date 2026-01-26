using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using SlotMachine.Data;
using SlotMachineCore = SlotMachine.Core;
using SlotMachineUI = SlotMachine.UI;

public class SlotMachineSceneSetup : EditorWindow
{
    private SlotMachineConfig config;

    [MenuItem("Tools/Slot Machine/Create Scene Objects")]
    public static void ShowWindow()
    {
        GetWindow<SlotMachineSceneSetup>("Scene Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Slot Machine Scene Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        config = (SlotMachineConfig)EditorGUILayout.ObjectField("Config", config, typeof(SlotMachineConfig), false);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create Complete Slot Machine UI", GUILayout.Height(50)))
        {
            CreateSlotMachineUI();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "이 버튼을 클릭하면 Canvas, 릴, UI 버튼 등\n슬롯머신에 필요한 모든 게임오브젝트가 생성됩니다.",
            MessageType.Info);
    }

    private void CreateSlotMachineUI()
    {
        // 기존 Canvas 확인 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 메인 슬롯머신 컨테이너
        GameObject slotMachineObj = CreateUIElement("SlotMachine", canvas.transform);
        RectTransform slotRT = slotMachineObj.GetComponent<RectTransform>();
        slotRT.anchorMin = Vector2.zero;
        slotRT.anchorMax = Vector2.one;
        slotRT.offsetMin = Vector2.zero;
        slotRT.offsetMax = Vector2.zero;

        // 배경
        GameObject background = CreateUIElement("Background", slotMachineObj.transform);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.05f, 0.15f); // 네온 테마용 어두운 보라색
        RectTransform bgRT = background.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // 릴 컨테이너
        GameObject reelContainer = CreateUIElement("ReelContainer", slotMachineObj.transform);
        RectTransform reelContainerRT = reelContainer.GetComponent<RectTransform>();
        reelContainerRT.anchoredPosition = new Vector2(0, 50);
        reelContainerRT.sizeDelta = new Vector2(500, 500);

        // 릴 마스크 (심볼이 밖으로 안 보이게)
        Image reelMaskBg = reelContainer.AddComponent<Image>();
        reelMaskBg.color = new Color(0.15f, 0.1f, 0.2f);
        Mask mask = reelContainer.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // 3개 릴 생성
        for (int i = 0; i < 3; i++)
        {
            CreateReel(reelContainer.transform, i);
        }

        // UI 패널 생성
        CreateUIPanel(slotMachineObj.transform);

        // 컴포넌트 추가
        SlotMachineCore.SlotMachine slotMachine = slotMachineObj.AddComponent<SlotMachineCore.SlotMachine>();
        SlotMachineCore.PaylineManager paylineManager = slotMachineObj.AddComponent<SlotMachineCore.PaylineManager>();
        SlotMachineUI.SlotUIManager uiManager = slotMachineObj.AddComponent<SlotMachineUI.SlotUIManager>();
        SlotMachineCore.SlotAudioManager audioManager = slotMachineObj.AddComponent<SlotMachineCore.SlotAudioManager>();
        SlotMachineUI.PaylineRenderer paylineRenderer = slotMachineObj.AddComponent<SlotMachineUI.PaylineRenderer>();

        // Serialized field 설정을 위한 SerializedObject 사용
        if (config != null)
        {
            SerializedObject so = new SerializedObject(slotMachine);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        Selection.activeGameObject = slotMachineObj;
        EditorUtility.DisplayDialog("Success", "슬롯머신 UI가 생성되었습니다!\n\n다음 단계:\n1. Inspector에서 Config 할당\n2. 릴에 Symbol Prefab 할당\n3. UI 요소들 연결", "OK");
    }

    private void CreateReel(Transform parent, int index)
    {
        float reelWidth = 150f;
        float startX = -reelWidth;

        GameObject reel = CreateUIElement($"Reel_{index}", parent);
        RectTransform reelRT = reel.GetComponent<RectTransform>();
        reelRT.anchoredPosition = new Vector2(startX + (index * reelWidth), 0);
        reelRT.sizeDelta = new Vector2(reelWidth, 450);

        // 릴 배경
        Image reelBg = reel.AddComponent<Image>();
        reelBg.color = new Color(0.08f, 0.05f, 0.12f);

        // 심볼 컨테이너
        GameObject symbolContainer = CreateUIElement("SymbolContainer", reel.transform);
        RectTransform symbolContainerRT = symbolContainer.GetComponent<RectTransform>();
        symbolContainerRT.anchorMin = Vector2.zero;
        symbolContainerRT.anchorMax = Vector2.one;
        symbolContainerRT.offsetMin = Vector2.zero;
        symbolContainerRT.offsetMax = Vector2.zero;

        // Reel 컴포넌트 추가
        SlotMachineCore.Reel reelComponent = reel.AddComponent<SlotMachineCore.Reel>();

        // 샘플 심볼 3개 생성
        for (int i = 0; i < 3; i++)
        {
            CreateSymbol(symbolContainer.transform, i);
        }
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

        // Symbol 컴포넌트 추가
        symbol.AddComponent<SlotMachineCore.Symbol>();
    }

    private void CreateUIPanel(Transform parent)
    {
        // 하단 UI 패널
        GameObject uiPanel = CreateUIElement("UIPanel", parent);
        RectTransform uiPanelRT = uiPanel.GetComponent<RectTransform>();
        uiPanelRT.anchorMin = new Vector2(0, 0);
        uiPanelRT.anchorMax = new Vector2(1, 0);
        uiPanelRT.pivot = new Vector2(0.5f, 0);
        uiPanelRT.anchoredPosition = Vector2.zero;
        uiPanelRT.sizeDelta = new Vector2(0, 200);

        Image panelBg = uiPanel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.08f, 0.18f, 0.95f);

        // 코인 표시
        CreateCoinDisplay(uiPanel.transform);

        // 배팅 표시
        CreateBetDisplay(uiPanel.transform);

        // 스핀 버튼
        CreateSpinButton(uiPanel.transform);

        // 당첨금 표시
        CreateWinDisplay(uiPanel.transform);

        // 상단 타이틀
        CreateTitle(parent);
    }

    private void CreateCoinDisplay(Transform parent)
    {
        GameObject coinPanel = CreateUIElement("CoinPanel", parent);
        RectTransform coinRT = coinPanel.GetComponent<RectTransform>();
        coinRT.anchorMin = new Vector2(0, 0.5f);
        coinRT.anchorMax = new Vector2(0, 0.5f);
        coinRT.pivot = new Vector2(0, 0.5f);
        coinRT.anchoredPosition = new Vector2(50, 0);
        coinRT.sizeDelta = new Vector2(300, 80);

        // 라벨
        GameObject label = CreateTextElement("CoinLabel", coinPanel.transform, "COINS", 24);
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(0, 20);

        // 값
        GameObject value = CreateTextElement("CoinText", coinPanel.transform, "10,000", 36);
        TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
        valueText.color = new Color(1f, 0.84f, 0f); // Gold
        RectTransform valueRT = value.GetComponent<RectTransform>();
        valueRT.anchoredPosition = new Vector2(0, -20);
    }

    private void CreateBetDisplay(Transform parent)
    {
        GameObject betPanel = CreateUIElement("BetPanel", parent);
        RectTransform betRT = betPanel.GetComponent<RectTransform>();
        betRT.anchorMin = new Vector2(0.5f, 0.5f);
        betRT.anchorMax = new Vector2(0.5f, 0.5f);
        betRT.anchoredPosition = new Vector2(-200, 0);
        betRT.sizeDelta = new Vector2(250, 80);

        // - 버튼
        CreateButton("BetDecreaseBtn", betPanel.transform, "-", new Vector2(-80, 0), new Vector2(50, 50));

        // 배팅 값
        GameObject betValue = CreateTextElement("BetText", betPanel.transform, "50", 32);
        TextMeshProUGUI betText = betValue.GetComponent<TextMeshProUGUI>();
        betText.color = Color.white;

        // + 버튼
        CreateButton("BetIncreaseBtn", betPanel.transform, "+", new Vector2(80, 0), new Vector2(50, 50));

        // 라벨
        GameObject label = CreateTextElement("BetLabel", betPanel.transform, "BET", 20);
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(0, 35);
    }

    private void CreateSpinButton(Transform parent)
    {
        GameObject spinBtn = CreateButton("SpinButton", parent, "SPIN", new Vector2(200, 0), new Vector2(150, 80));
        RectTransform spinRT = spinBtn.GetComponent<RectTransform>();
        spinRT.anchorMin = new Vector2(0.5f, 0.5f);
        spinRT.anchorMax = new Vector2(0.5f, 0.5f);

        // 글로우 효과용 이미지
        Image btnImage = spinBtn.GetComponent<Image>();
        btnImage.color = new Color(0f, 0.8f, 0.6f); // 네온 그린

        TextMeshProUGUI btnText = spinBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            btnText.fontSize = 36;
            btnText.fontStyle = FontStyles.Bold;
        }
    }

    private void CreateWinDisplay(Transform parent)
    {
        GameObject winPanel = CreateUIElement("WinPanel", parent);
        RectTransform winRT = winPanel.GetComponent<RectTransform>();
        winRT.anchorMin = new Vector2(1, 0.5f);
        winRT.anchorMax = new Vector2(1, 0.5f);
        winRT.pivot = new Vector2(1, 0.5f);
        winRT.anchoredPosition = new Vector2(-50, 0);
        winRT.sizeDelta = new Vector2(300, 80);

        // 라벨
        GameObject label = CreateTextElement("WinLabel", winPanel.transform, "WIN", 24);
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchoredPosition = new Vector2(0, 20);

        // 값
        GameObject value = CreateTextElement("WinText", winPanel.transform, "", 36);
        TextMeshProUGUI valueText = value.GetComponent<TextMeshProUGUI>();
        valueText.color = new Color(0f, 1f, 0.6f); // 네온 그린
        RectTransform valueRT = value.GetComponent<RectTransform>();
        valueRT.anchoredPosition = new Vector2(0, -20);
    }

    private void CreateTitle(Transform parent)
    {
        GameObject title = CreateTextElement("Title", parent, "NEON SLOTS", 64);
        TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
        titleText.color = new Color(1f, 0.2f, 0.6f); // 네온 핑크
        titleText.fontStyle = FontStyles.Bold;

        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1);
        titleRT.anchorMax = new Vector2(0.5f, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -30);
        titleRT.sizeDelta = new Vector2(600, 100);
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
        rt.sizeDelta = new Vector2(300, 50);

        return obj;
    }

    private GameObject CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size)
    {
        GameObject btnObj = CreateUIElement(name, parent);
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchoredPosition = position;
        btnRT.sizeDelta = size;

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.2f, 0.4f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.4f, 0.3f, 0.5f);
        colors.pressedColor = new Color(0.2f, 0.1f, 0.3f);
        btn.colors = colors;

        // 버튼 텍스트
        GameObject textObj = CreateTextElement("Text", btnObj.transform, text, 24);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btnObj;
    }

    #endregion
}
