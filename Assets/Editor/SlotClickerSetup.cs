using UnityEngine;
using UnityEditor;
using SlotClicker.Core;
using SlotClicker.Data;

public class SlotClickerSetup : Editor
{
    [MenuItem("Tools/Slot Clicker/★ Run Slot Clicker Demo ★ #&c")]
    public static void RunDemo()
    {
        // 기존 초기화 객체 제거
        SlotClickerInitializer existing = FindObjectOfType<SlotClickerInitializer>();
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        // 기존 GameManager 제거
        GameManager existingManager = FindObjectOfType<GameManager>();
        if (existingManager != null)
        {
            DestroyImmediate(existingManager.gameObject);
        }

        // 새 초기화 오브젝트 생성
        GameObject initObj = new GameObject("SlotClickerInitializer");
        SlotClickerInitializer initializer = initObj.AddComponent<SlotClickerInitializer>();

        // Config 자동 로드 시도
        string[] configGuids = AssetDatabase.FindAssets("t:GameConfig");
        if (configGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(configGuids[0]);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);

            SerializedObject so = new SerializedObject(initializer);
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_debugMode").boolValue = true;
            so.FindProperty("_debugStartGold").doubleValue = 1000;
            so.ApplyModifiedProperties();
        }

        Selection.activeGameObject = initObj;

        EditorUtility.DisplayDialog("Slot Clicker 준비 완료!",
            "슬롯 클리커 게임이 씬에 추가되었습니다!\n\n" +
            "▶ Play 버튼을 눌러 게임을 실행하세요!\n\n" +
            "조작법:\n" +
            "• 녹색 테이블 클릭 = 골드 획득\n" +
            "• 베팅 % 선택 후 SPIN 클릭 = 슬롯 도박",
            "OK");
    }

    [MenuItem("Tools/Slot Clicker/Create Game Config")]
    public static void CreateConfig()
    {
        // 폴더 생성
        string outputPath = "Assets/09.ScriptableObjects/SlotClicker";
        if (!System.IO.Directory.Exists(outputPath))
        {
            System.IO.Directory.CreateDirectory(outputPath);
        }

        // Config 생성
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();

        string configPath = $"{outputPath}/DefaultGameConfig.asset";
        AssetDatabase.CreateAsset(config, configPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = config;

        EditorUtility.DisplayDialog("Config 생성 완료!",
            $"GameConfig가 생성되었습니다.\n\n경로: {configPath}\n\n" +
            "Inspector에서 값을 조정할 수 있습니다.",
            "OK");
    }

    [MenuItem("Tools/Slot Clicker/Reset Save Data")]
    public static void ResetSaveData()
    {
        if (EditorUtility.DisplayDialog("데이터 초기화",
            "정말로 저장 데이터를 초기화하시겠습니까?\n\n모든 진행 상황이 삭제됩니다.",
            "초기화", "취소"))
        {
            PlayerPrefs.DeleteKey("SlotClickerSaveData");
            PlayerPrefs.Save();

            EditorUtility.DisplayDialog("완료", "저장 데이터가 초기화되었습니다.", "OK");
        }
    }

    [MenuItem("Tools/Slot Clicker/Open Documentation")]
    public static void OpenDocumentation()
    {
        EditorUtility.DisplayDialog("Slot Clicker 게임 구조",
            "=== 핵심 시스템 ===\n\n" +
            "• GameManager: 게임 상태 관리 (싱글톤)\n" +
            "• GoldManager: 골드 획득/소비\n" +
            "• ClickManager: 클릭 처리, 크리티컬\n" +
            "• SlotManager: 슬롯 확률, 보상\n\n" +
            "=== 데이터 ===\n\n" +
            "• PlayerData: 플레이어 진행 상황\n" +
            "• GameConfig: 게임 밸런스 설정\n\n" +
            "=== 파일 위치 ===\n\n" +
            "Scripts: Assets/02.Scripts/SlotClicker/\n" +
            "Config: Assets/09.ScriptableObjects/SlotClicker/",
            "OK");
    }
}
