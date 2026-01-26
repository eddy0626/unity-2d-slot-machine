using UnityEngine;
using UnityEditor;
using System.IO;
using SlotMachine.Data;

public class SlotMachineSetupEditor : EditorWindow
{
    private Texture2D spriteSheet;
    private string outputPath = "Assets/09.ScriptableObjects/SlotMachine";

    // 심볼 정보 정의
    private static readonly SymbolInfo[] symbolInfos = new SymbolInfo[]
    {
        // 고가치 심볼 (1행)
        new SymbolInfo(0, "Dealer Girl", SymbolType.Normal, 50, 5, new Color(1f, 0.5f, 0.8f)),
        new SymbolInfo(1, "Bunny Girl", SymbolType.Normal, 40, 6, new Color(0.4f, 0.8f, 1f)),
        new SymbolInfo(2, "Mage Girl", SymbolType.Normal, 30, 7, new Color(0.6f, 0.3f, 0.9f)),
        new SymbolInfo(3, "Pirate Girl", SymbolType.Normal, 25, 8, new Color(0.2f, 0.8f, 0.8f)),

        // 중가치 심볼 (2행)
        new SymbolInfo(4, "Maid", SymbolType.Normal, 20, 10, new Color(0.8f, 0.9f, 1f)),
        new SymbolInfo(5, "Fairy", SymbolType.Normal, 15, 12, new Color(1f, 0.6f, 0.9f)),
        new SymbolInfo(6, "Gold Coin", SymbolType.Normal, 10, 15, new Color(1f, 0.84f, 0f)),
        new SymbolInfo(7, "Diamond", SymbolType.Normal, 8, 12, new Color(0.6f, 0.9f, 1f)),

        // 저가치 심볼 (3행)
        new SymbolInfo(8, "Pink Coin", SymbolType.Normal, 5, 18, new Color(1f, 0.5f, 0.7f)),
        new SymbolInfo(9, "Clover", SymbolType.Normal, 4, 20, new Color(0.3f, 0.9f, 0.4f)),
        new SymbolInfo(10, "Ruby Clover", SymbolType.Normal, 3, 20, new Color(0.9f, 0.3f, 0.3f)),
        new SymbolInfo(11, "Vortex", SymbolType.Normal, 2, 22, new Color(0.5f, 0.3f, 0.9f)),

        // 특수 심볼 (4행)
        new SymbolInfo(12, "WILD", SymbolType.Wild, 100, 3, new Color(1f, 0.9f, 0.3f)),
        new SymbolInfo(13, "Scatter", SymbolType.Scatter, 0, 4, new Color(0.9f, 0.5f, 1f)),
        new SymbolInfo(14, "Gift Box", SymbolType.Bonus, 15, 8, new Color(0.4f, 0.9f, 1f)),
        new SymbolInfo(15, "Chip", SymbolType.Normal, 10, 15, new Color(1f, 0.7f, 0.3f))
    };

    [MenuItem("Tools/Slot Machine/Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<SlotMachineSetupEditor>("Slot Machine Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Slot Machine Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Symbol Sprite Sheet", spriteSheet, typeof(Texture2D), false);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create All Symbol Data", GUILayout.Height(40)))
        {
            CreateAllSymbolData();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Create Slot Machine Config", GUILayout.Height(40)))
        {
            CreateSlotMachineConfig();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. 먼저 'Symbol Sprite Sheet'에 '심볼 스프라이트' 이미지를 할당하세요.\n" +
            "2. 'Create All Symbol Data'를 클릭하여 16개 심볼 데이터를 생성합니다.\n" +
            "3. 'Create Slot Machine Config'를 클릭하여 게임 설정을 생성합니다.",
            MessageType.Info);
    }

    private void CreateAllSymbolData()
    {
        if (spriteSheet == null)
        {
            EditorUtility.DisplayDialog("Error", "Sprite Sheet를 먼저 할당해주세요!", "OK");
            return;
        }

        // 출력 폴더 생성
        string symbolsPath = $"{outputPath}/Symbols";
        if (!Directory.Exists(symbolsPath))
        {
            Directory.CreateDirectory(symbolsPath);
        }

        // 스프라이트 로드
        string spritePath = AssetDatabase.GetAssetPath(spriteSheet);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        Sprite[] sprites = new Sprite[16];

        foreach (Object asset in allAssets)
        {
            if (asset is Sprite sprite)
            {
                // "심볼 스프라이트_0" ~ "심볼 스프라이트_15" 형식
                string name = sprite.name;
                int index = -1;

                if (name.Contains("_"))
                {
                    string[] parts = name.Split('_');
                    if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out index))
                    {
                        if (index >= 0 && index < 16)
                        {
                            sprites[index] = sprite;
                        }
                    }
                }
            }
        }

        // SymbolData 생성
        for (int i = 0; i < symbolInfos.Length; i++)
        {
            var info = symbolInfos[i];
            SymbolData data = CreateInstance<SymbolData>();

            data.symbolId = info.id;
            data.symbolName = info.name;
            data.symbolType = info.type;
            data.payoutMultiplier = info.payout;
            data.weight = info.weight;
            data.glowColor = info.glowColor;
            data.sprite = sprites[i];

            string assetPath = $"{symbolsPath}/Symbol_{i:D2}_{info.name.Replace(" ", "")}.asset";
            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "16개의 Symbol Data가 생성되었습니다!", "OK");
    }

    private void CreateSlotMachineConfig()
    {
        // 기존 심볼 데이터 로드
        string symbolsPath = $"{outputPath}/Symbols";
        string[] symbolGuids = AssetDatabase.FindAssets("t:SymbolData", new[] { symbolsPath });

        if (symbolGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "먼저 Symbol Data를 생성해주세요!", "OK");
            return;
        }

        SlotMachineConfig config = CreateInstance<SlotMachineConfig>();

        // 기본 설정
        config.initialCoins = 10000;
        config.betAmounts = new int[] { 10, 25, 50, 100, 250, 500 };
        config.defaultBetIndex = 2;

        config.reelCount = 3;
        config.visibleSymbolsPerReel = 3;
        config.symbolHeight = 150f;

        config.spinSpeed = 2000f;
        config.spinDuration = 2f;
        config.reelStopDelay = 0.4f;
        config.bounceAmount = 15f;
        config.bounceDuration = 0.2f;

        config.scatterBonusMultiplier = 10;
        config.wildSymbolId = 12;
        config.scatterSymbolId = 13;

        // 심볼 데이터 할당
        config.symbols = new SymbolData[symbolGuids.Length];
        for (int i = 0; i < symbolGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(symbolGuids[i]);
            config.symbols[i] = AssetDatabase.LoadAssetAtPath<SymbolData>(path);
        }

        // 정렬 (ID 순)
        System.Array.Sort(config.symbols, (a, b) => a.symbolId.CompareTo(b.symbolId));

        string configPath = $"{outputPath}/SlotMachineConfig.asset";
        AssetDatabase.CreateAsset(config, configPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Slot Machine Config가 생성되었습니다!", "OK");
        Selection.activeObject = config;
    }

    private class SymbolInfo
    {
        public int id;
        public string name;
        public SymbolType type;
        public int payout;
        public int weight;
        public Color glowColor;

        public SymbolInfo(int id, string name, SymbolType type, int payout, int weight, Color glowColor)
        {
            this.id = id;
            this.name = name;
            this.type = type;
            this.payout = payout;
            this.weight = weight;
            this.glowColor = glowColor;
        }
    }
}
