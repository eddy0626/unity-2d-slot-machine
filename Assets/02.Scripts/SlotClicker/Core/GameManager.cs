using System;
using UnityEngine;
using SlotClicker.Data;

namespace SlotClicker.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private GameConfig _config;

        /// <summary>
        /// Config 설정 (외부에서 초기화 전에 호출)
        /// </summary>
        public void SetConfig(GameConfig config)
        {
            if (!_isInitialized && config != null)
            {
                _config = config;
            }
        }

        [Header("상태")]
        [SerializeField] private bool _isInitialized = false;

        // 매니저 참조
        public GoldManager Gold { get; private set; }
        public ClickManager Click { get; private set; }
        public SlotManager Slot { get; private set; }
        public UpgradeManager Upgrade { get; private set; }
        public PrestigeManager Prestige { get; private set; }

        // 플레이어 데이터
        public PlayerData PlayerData { get; private set; }
        public GameConfig Config => _config;

        // 이벤트
        public event Action OnGameInitialized;
        public event Action OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            // Config 기본값 생성
            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<GameConfig>();
                Debug.LogWarning("[GameManager] GameConfig not assigned, using default values.");
            }

            _config.NormalizeProbabilities();

            // 플레이어 데이터 로드 또는 생성
            LoadPlayerData();

            // 매니저 초기화
            InitializeManagers();

            _isInitialized = true;
            Debug.Log("[GameManager] Game initialized successfully!");
            OnGameInitialized?.Invoke();
        }

        private void InitializeManagers()
        {
            // GoldManager 초기화 (먼저 - 다른 매니저들이 골드 체크에 필요)
            Gold = gameObject.AddComponent<GoldManager>();
            Gold.Initialize(this);

            // UpgradeManager 초기화 (ClickManager, SlotManager보다 먼저!)
            Upgrade = gameObject.AddComponent<UpgradeManager>();
            Upgrade.Initialize(this);

            // ClickManager 초기화 (UpgradeManager 효과 사용)
            Click = gameObject.AddComponent<ClickManager>();
            Click.Initialize(this);

            // SlotManager 초기화 (UpgradeManager 효과 사용)
            Slot = gameObject.AddComponent<SlotManager>();
            Slot.Initialize(this);

            // PrestigeManager 초기화
            Prestige = gameObject.AddComponent<PrestigeManager>();
            Prestige.Initialize(this);
        }

        private void LoadPlayerData()
        {
            // TODO: 실제 저장/로드 구현
            string savedData = PlayerPrefs.GetString("SlotClickerSaveData", "");

            if (!string.IsNullOrEmpty(savedData))
            {
                try
                {
                    PlayerData = JsonUtility.FromJson<PlayerData>(savedData);
                    PlayerData.InitializeCache(); // Dictionary 캐시 초기화
                    Debug.Log("[GameManager] Player data loaded.");
                }
                catch (System.Exception ex)
                {
                    PlayerData = new PlayerData();
                    Debug.LogWarning($"[GameManager] Failed to load save data: {ex.Message}. Creating new data.");
                }
            }
            else
            {
                PlayerData = new PlayerData();
                Debug.Log("[GameManager] New player data created.");
            }
        }

        public void SavePlayerData()
        {
            if (PlayerData == null) return;

            PlayerData.lastPlayTime = DateTime.Now.ToString("o");
            string json = JsonUtility.ToJson(PlayerData);
            PlayerPrefs.SetString("SlotClickerSaveData", json);
            PlayerPrefs.Save();
            Debug.Log("[GameManager] Player data saved.");
        }

        public void NotifyStateChanged()
        {
            OnGameStateChanged?.Invoke();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SavePlayerData();
        }

        private void OnApplicationQuit()
        {
            SavePlayerData();
        }

        // 프레스티지 (나중에 구현)
        public int CalculatePrestigeChips()
        {
            if (PlayerData.totalGoldEarned < _config.prestigeThreshold)
                return 0;

            return Mathf.FloorToInt((float)(Math.Log10(PlayerData.totalGoldEarned) - 5));
        }

        public float GetPrestigeBonus()
        {
            if (Prestige != null)
                return Prestige.GetTotalPrestigeMultiplier();
            return 1f + (PlayerData.chips * _config.prestigeBonusPerChip);
        }
    }
}
