using UnityEngine;
using UnityEditor;
using SlotMachine.Core;
using System.Collections.Generic;

public class SlotMachineQuickSetup : Editor
{
    [MenuItem("Tools/Slot Machine/Quick Setup (Add to Scene) #&s")]
    public static void QuickSetup()
    {
        // 이미 Initializer가 있는지 확인
        SlotMachineInitializer existing = FindObjectOfType<SlotMachineInitializer>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("알림", "SlotMachineInitializer가 이미 씬에 존재합니다!", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // 새 게임오브젝트 생성
        GameObject initObj = new GameObject("SlotMachineInitializer");
        SlotMachineInitializer initializer = initObj.AddComponent<SlotMachineInitializer>();

        // Config 자동 연결 시도
        string[] configGuids = AssetDatabase.FindAssets("t:SlotMachineConfig");
        if (configGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(configGuids[0]);
            var config = AssetDatabase.LoadAssetAtPath<SlotMachine.Data.SlotMachineConfig>(path);

            SerializedObject so = new SerializedObject(initializer);
            so.FindProperty("config").objectReferenceValue = config;
            so.ApplyModifiedProperties();
        }

        Selection.activeGameObject = initObj;

        EditorUtility.DisplayDialog("성공!",
            "SlotMachineInitializer가 씬에 추가되었습니다!\n\n" +
            "플레이 버튼을 누르면 슬롯머신 UI가 자동 생성됩니다.\n\n" +
            "※ 아직 Config가 없다면:\n" +
            "   Tools > Slot Machine > Setup Wizard 에서\n" +
            "   먼저 설정을 생성하세요.",
            "OK");
    }

    [MenuItem("Tools/Slot Machine/★ Run Demo (No Setup Required) ★")]
    public static void RunDemo()
    {
        // 이미 Demo가 있는지 확인
        SlotMachineDemo existing = FindObjectOfType<SlotMachineDemo>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("알림", "슬롯머신 데모가 이미 씬에 존재합니다!", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // 기존 Initializer 제거
        SlotMachineInitializer oldInit = FindObjectOfType<SlotMachineInitializer>();
        if (oldInit != null)
        {
            DestroyImmediate(oldInit.gameObject);
        }

        // 데모 오브젝트 생성
        GameObject demoObj = new GameObject("SlotMachineDemo");
        SlotMachineDemo demo = demoObj.AddComponent<SlotMachineDemo>();

        // 스프라이트 자동 로드 시도
        Sprite[] sprites = LoadSymbolSprites();
        Sprite frameSprite = LoadMachineFrameSprite();

        SerializedObject so = new SerializedObject(demo);

        if (sprites != null && sprites.Length > 0)
        {
            SerializedProperty spritesProp = so.FindProperty("_symbolSprites");
            spritesProp.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        if (frameSprite != null)
        {
            SerializedProperty frameProp = so.FindProperty("_machineFrameSprite");
            frameProp.objectReferenceValue = frameSprite;
        }

        so.ApplyModifiedProperties();

        Selection.activeGameObject = demoObj;

        string spriteStatus = sprites != null && sprites.Length > 0
            ? $"✓ {sprites.Length}개 심볼 스프라이트 로드됨"
            : "※ 심볼 스프라이트 없음 (플레이스홀더 사용)";

        string frameStatus = frameSprite != null
            ? "\n✓ 머신 프레임 스프라이트 로드됨"
            : "\n※ 머신 프레임 없음 (기본 스타일 사용)";

        EditorUtility.DisplayDialog("데모 준비 완료!",
            "슬롯머신 데모가 씬에 추가되었습니다!\n\n" +
            spriteStatus + frameStatus + "\n\n" +
            "▶ Play 버튼을 눌러 게임을 실행하세요!",
            "OK");
    }

    private static Sprite[] LoadSymbolSprites()
    {
        // 직접 경로로 심볼 스프라이트 로드
        string symbolPath = "Assets/04.Images/심볼 스프라이트.png";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(symbolPath);

        List<Sprite> sprites = new List<Sprite>();

        if (allAssets != null && allAssets.Length > 0)
        {
            Debug.Log($"[SlotMachine] Loading from {symbolPath}, found {allAssets.Length} assets");

            foreach (Object asset in allAssets)
            {
                if (asset is Sprite sprite)
                {
                    // 메인 텍스처가 아닌 슬라이스된 스프라이트만 추가
                    if (sprite.name.Contains("_"))
                    {
                        sprites.Add(sprite);
                        Debug.Log($"[SlotMachine] Added sprite: {sprite.name}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[SlotMachine] No assets found at {symbolPath}");
        }

        // 숫자 인덱스로 정렬 (_0, _1, _2, ... _15)
        sprites.Sort((a, b) => {
            int indexA = ExtractSpriteIndex(a.name);
            int indexB = ExtractSpriteIndex(b.name);
            return indexA.CompareTo(indexB);
        });

        Debug.Log($"[SlotMachine] Total symbol sprites loaded: {sprites.Count}");
        return sprites.ToArray();
    }

    private static int ExtractSpriteIndex(string name)
    {
        int lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < name.Length - 1)
        {
            if (int.TryParse(name.Substring(lastUnderscore + 1), out int index))
            {
                return index;
            }
        }
        return 999;
    }

    private static Sprite LoadMachineFrameSprite()
    {
        // 직접 경로로 로드 시도 (괄호가 있는 파일명)
        string directPath = "Assets/04.Images/슬롯머신 본체(머신 프레임).png";

        // 모든 에셋 로드
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(directPath);

        if (allAssets != null && allAssets.Length > 0)
        {
            // 스프라이트 타입만 필터링
            List<Sprite> sprites = new List<Sprite>();
            foreach (Object asset in allAssets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                    Debug.Log($"[SlotMachine] Found sprite: {sprite.name}");
                }
            }

            // 스프라이트가 있으면 첫 번째 반환 (멀티 스프라이트 또는 단일)
            if (sprites.Count > 0)
            {
                Debug.Log($"[SlotMachine] Using frame sprite: {sprites[0].name}");
                return sprites[0];
            }
        }

        // 폴백: Assets/04.Images에서 "본체" 또는 "프레임" 포함된 텍스처 검색
        Debug.Log("[SlotMachine] Direct path failed, searching for frame sprites...");
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/04.Images" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("본체") || path.Contains("프레임") || path.Contains("frame"))
            {
                Debug.Log($"[SlotMachine] Checking path: {path}");
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object asset in assets)
                {
                    if (asset is Sprite sprite)
                    {
                        Debug.Log($"[SlotMachine] Found fallback sprite: {sprite.name}");
                        return sprite;
                    }
                }
            }
        }

        Debug.LogWarning("[SlotMachine] No machine frame sprite found!");
        return null;
    }

    [MenuItem("Tools/Slot Machine/Create Demo Config (No Sprites)")]
    public static void CreateDemoConfig()
    {
        // 출력 폴더 생성
        string outputPath = "Assets/09.ScriptableObjects/SlotMachine";
        if (!System.IO.Directory.Exists(outputPath))
        {
            System.IO.Directory.CreateDirectory(outputPath);
        }

        // Demo Config 생성
        SlotMachine.Data.SlotMachineConfig config = ScriptableObject.CreateInstance<SlotMachine.Data.SlotMachineConfig>();

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

        string configPath = $"{outputPath}/DemoSlotMachineConfig.asset";
        AssetDatabase.CreateAsset(config, configPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("성공!",
            "데모 Config가 생성되었습니다!\n\n" +
            "경로: " + configPath + "\n\n" +
            "※ 심볼 데이터는 별도로 Setup Wizard에서 생성하세요.",
            "OK");

        Selection.activeObject = config;
    }
}
