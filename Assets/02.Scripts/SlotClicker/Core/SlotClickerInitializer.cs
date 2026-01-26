using UnityEngine;
using SlotClicker.Data;
using SlotClicker.UI;

namespace SlotClicker.Core
{
    /// <summary>
    /// 슬롯 클리커 게임 초기화 컴포넌트
    /// 씬에 배치하면 자동으로 게임이 시작됩니다.
    /// </summary>
    public class SlotClickerInitializer : MonoBehaviour
    {
        [Header("=== 게임 설정 ===")]
        [SerializeField] private GameConfig _config;
        [SerializeField] private bool _createUIOnStart = true;

        [Header("=== 디버그 ===")]
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private double _debugStartGold = 1000;

        private void Awake()
        {
            // GameManager 생성
            if (GameManager.Instance == null)
            {
                GameObject managerObj = new GameObject("GameManager");
                GameManager gameManager = managerObj.AddComponent<GameManager>();

                // Config 설정 (public setter 사용 - 리플렉션 제거)
                if (_config != null)
                {
                    gameManager.SetConfig(_config);
                }
            }
        }

        private void Start()
        {
            // GameManager 초기화 대기 후 UI 생성
            StartCoroutine(InitializeGame());
        }

        private System.Collections.IEnumerator InitializeGame()
        {
            // GameManager 초기화 대기
            while (GameManager.Instance == null)
            {
                yield return null;
            }

            // 디버그 모드: 시작 골드 설정
            if (_debugMode && GameManager.Instance.PlayerData != null)
            {
                GameManager.Instance.PlayerData.gold = _debugStartGold;
            }

            // UI 생성
            if (_createUIOnStart)
            {
                CreateUI();
            }

            Debug.Log("[SlotClickerInitializer] Game initialized!");
        }

        private void CreateUI()
        {
            // 이미 UI가 있는지 확인
            if (FindObjectOfType<SlotClickerUI>() != null)
            {
                Debug.Log("[SlotClickerInitializer] UI already exists.");
                return;
            }

            // UI 오브젝트 생성
            GameObject uiObj = new GameObject("SlotClickerUI");
            uiObj.AddComponent<SlotClickerUI>();

            Debug.Log("[SlotClickerInitializer] UI created.");
        }

        /// <summary>
        /// 게임 리셋 (디버그용)
        /// </summary>
        [ContextMenu("Reset Game Data")]
        public void ResetGameData()
        {
            PlayerPrefs.DeleteKey("SlotClickerSaveData");
            PlayerPrefs.Save();
            Debug.Log("[SlotClickerInitializer] Game data reset!");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("Reset Complete", "게임 데이터가 초기화되었습니다.\n게임을 다시 시작하세요.", "OK");
#endif
        }
    }
}
