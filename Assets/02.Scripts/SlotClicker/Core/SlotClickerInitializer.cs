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

        [Header("=== 화면 회전 설정 ===")]
        [Tooltip("화면 회전 관리자 자동 생성")]
        [SerializeField] private bool _enableOrientationManager = true;
        [Tooltip("자동 회전 기본값")]
        [SerializeField] private bool _autoRotationDefault = true;
        [Tooltip("반응형 UI 자동 설정")]
        [SerializeField] private bool _enableResponsiveUI = true;

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

            // OrientationManager 생성
            if (_enableOrientationManager && OrientationManager.Instance == null)
            {
                CreateOrientationManager();
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
            SlotClickerUI existingUI = FindObjectOfType<SlotClickerUI>();
            if (existingUI != null)
            {
                Debug.Log("[SlotClickerInitializer] UI already exists.");

                // 반응형 UI 컴포넌트 추가 (기존 UI에)
                if (_enableResponsiveUI && existingUI.GetComponent<SlotClickerResponsiveUI>() == null)
                {
                    SlotClickerResponsiveUI responsiveUI = existingUI.gameObject.AddComponent<SlotClickerResponsiveUI>();
                    responsiveUI.AutoFindUIReferences();
                    Debug.Log("[SlotClickerInitializer] Responsive UI added to existing UI.");
                }
                return;
            }

            // UI 오브젝트 생성
            GameObject uiObj = new GameObject("SlotClickerUI");
            uiObj.AddComponent<SlotClickerUI>();

            // 반응형 UI 컴포넌트 추가
            if (_enableResponsiveUI)
            {
                SlotClickerResponsiveUI responsiveUI = uiObj.AddComponent<SlotClickerResponsiveUI>();
                // UI 생성 후 참조 찾기 (Start에서 처리됨)
                Debug.Log("[SlotClickerInitializer] Responsive UI component added.");
            }

            Debug.Log("[SlotClickerInitializer] UI created.");
        }

        /// <summary>
        /// OrientationManager 생성 및 초기화
        /// </summary>
        private void CreateOrientationManager()
        {
            GameObject orientationObj = new GameObject("OrientationManager");
            OrientationManager orientationManager = orientationObj.AddComponent<OrientationManager>();

            // 기본 설정 적용
            orientationManager.AutoRotationEnabled = _autoRotationDefault;

            // 저장된 설정 로드
            orientationManager.LoadSettings();

            Debug.Log("[SlotClickerInitializer] OrientationManager created.");
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
