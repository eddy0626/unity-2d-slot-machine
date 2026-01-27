using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotClicker.Core
{
    /// <summary>
    /// 사운드 종류 정의
    /// </summary>
    public enum SoundType
    {
        // 클릭
        ClickNormal,
        ClickCritical,

        // 슬롯
        LeverClick,
        SpinStart,
        SpinLoop,
        ReelTick,
        ReelStop,

        // 결과
        ResultLoss,
        ResultDraw,
        ResultMiniWin,
        ResultSmallWin,
        ResultBigWin,
        ResultJackpot,
        ResultMegaJackpot,

        // 업그레이드
        UpgradePurchase,
        UpgradeMax,

        // 프레스티지
        PrestigeConfirm,
        ChipEarn,
        CharmUnlock,
        VIPLevelUp,

        // UI
        UIButtonClick,
        UIPopupOpen,
        UIPopupClose,
        UITabSwitch
    }

    /// <summary>
    /// 사운드 시스템 매니저
    /// 싱글톤 패턴, AudioSource 풀링, 볼륨 관리
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("=== 설정 ===")]
        [SerializeField] private int _audioSourcePoolSize = 10;
        [SerializeField] private string _soundResourcePath = "Sounds/";

        [Header("=== 볼륨 (디버그용) ===")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.7f;
        [SerializeField] private bool _isMuted = false;

        // 오디오 소스 풀
        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private AudioSource _bgmSource;
        private AudioSource _loopSfxSource; // 루프 효과음용 (spin_loop 등)

        // 사운드 클립 캐시
        private Dictionary<SoundType, AudioClip> _clipCache = new Dictionary<SoundType, AudioClip>();

        // 사운드 파일명 매핑
        private static readonly Dictionary<SoundType, string> _soundFileNames = new Dictionary<SoundType, string>
        {
            // 클릭
            { SoundType.ClickNormal, "click_normal" },
            { SoundType.ClickCritical, "click_critical" },

            // 슬롯
            { SoundType.LeverClick, "lever_click" },
            { SoundType.SpinStart, "spin_start" },
            { SoundType.SpinLoop, "spin_loop" },
            { SoundType.ReelTick, "reel_tick" },
            { SoundType.ReelStop, "reel_stop" },

            // 결과
            { SoundType.ResultLoss, "result_loss" },
            { SoundType.ResultDraw, "result_draw" },
            { SoundType.ResultMiniWin, "result_mini_win" },
            { SoundType.ResultSmallWin, "result_small_win" },
            { SoundType.ResultBigWin, "result_big_win" },
            { SoundType.ResultJackpot, "result_jackpot" },
            { SoundType.ResultMegaJackpot, "result_mega_jackpot" },

            // 업그레이드
            { SoundType.UpgradePurchase, "upgrade_purchase" },
            { SoundType.UpgradeMax, "upgrade_max" },

            // 프레스티지
            { SoundType.PrestigeConfirm, "prestige_confirm" },
            { SoundType.ChipEarn, "chip_earn" },
            { SoundType.CharmUnlock, "charm_unlock" },
            { SoundType.VIPLevelUp, "vip_levelup" },

            // UI
            { SoundType.UIButtonClick, "ui_button_click" },
            { SoundType.UIPopupOpen, "ui_popup_open" },
            { SoundType.UIPopupClose, "ui_popup_close" },
            { SoundType.UITabSwitch, "ui_tab_switch" }
        };

        // 이벤트
        public event Action OnVolumeChanged;

        // Properties
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
                SaveSettings();
                OnVolumeChanged?.Invoke();
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
                SaveSettings();
                OnVolumeChanged?.Invoke();
            }
        }

        public float BGMVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
                SaveSettings();
                OnVolumeChanged?.Invoke();
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                ApplyVolumeSettings();
                SaveSettings();
                OnVolumeChanged?.Invoke();
            }
        }

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 초기화

        private void Initialize()
        {
            LoadSettings();
            CreateAudioSources();
            PreloadSounds();

            Debug.Log("[SoundManager] Initialized");
        }

        /// <summary>
        /// AudioSource 풀 생성
        /// </summary>
        private void CreateAudioSources()
        {
            // BGM용 AudioSource
            GameObject bgmObj = new GameObject("BGM_Source");
            bgmObj.transform.SetParent(transform);
            _bgmSource = bgmObj.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            // 루프 SFX용 AudioSource
            GameObject loopSfxObj = new GameObject("LoopSFX_Source");
            loopSfxObj.transform.SetParent(transform);
            _loopSfxSource = loopSfxObj.AddComponent<AudioSource>();
            _loopSfxSource.loop = true;
            _loopSfxSource.playOnAwake = false;

            // SFX 풀
            for (int i = 0; i < _audioSourcePoolSize; i++)
            {
                CreatePooledAudioSource();
            }
        }

        private AudioSource CreatePooledAudioSource()
        {
            GameObject sfxObj = new GameObject($"SFX_Source_{_sfxPool.Count}");
            sfxObj.transform.SetParent(transform);
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            _sfxPool.Add(source);
            return source;
        }

        /// <summary>
        /// 사운드 미리 로드
        /// </summary>
        private void PreloadSounds()
        {
            foreach (SoundType soundType in Enum.GetValues(typeof(SoundType)))
            {
                LoadClip(soundType);
            }

            Debug.Log($"[SoundManager] Preloaded {_clipCache.Count} sound clips");
        }

        /// <summary>
        /// AudioClip 로드
        /// </summary>
        private AudioClip LoadClip(SoundType soundType)
        {
            if (_clipCache.TryGetValue(soundType, out AudioClip cached))
            {
                return cached;
            }

            if (_soundFileNames.TryGetValue(soundType, out string fileName))
            {
                string path = _soundResourcePath + fileName;
                AudioClip clip = Resources.Load<AudioClip>(path);

                if (clip != null)
                {
                    _clipCache[soundType] = clip;
                    return clip;
                }
                else
                {
                    Debug.LogWarning($"[SoundManager] Failed to load sound: {path}");
                }
            }

            return null;
        }

        #endregion

        #region 재생

        /// <summary>
        /// 효과음 재생
        /// </summary>
        public void PlaySFX(SoundType soundType, float volumeScale = 1f)
        {
            if (_isMuted) return;

            AudioClip clip = LoadClip(soundType);
            if (clip == null) return;

            AudioSource source = GetAvailableAudioSource();
            if (source == null) return;

            float volume = _masterVolume * _sfxVolume * volumeScale;
            source.volume = volume;
            source.clip = clip;
            source.Play();
        }

        /// <summary>
        /// 루프 효과음 재생 (spin_loop 등)
        /// </summary>
        public void PlayLoopSFX(SoundType soundType, float volumeScale = 1f)
        {
            if (_isMuted)
            {
                _loopSfxSource.Stop();
                return;
            }

            AudioClip clip = LoadClip(soundType);
            if (clip == null) return;

            float volume = _masterVolume * _sfxVolume * volumeScale;
            _loopSfxSource.volume = volume;
            _loopSfxSource.clip = clip;
            _loopSfxSource.loop = true;
            _loopSfxSource.Play();
        }

        /// <summary>
        /// 루프 효과음 정지
        /// </summary>
        public void StopLoopSFX()
        {
            if (_loopSfxSource.isPlaying)
            {
                _loopSfxSource.Stop();
            }
        }

        /// <summary>
        /// BGM 재생
        /// </summary>
        public void PlayBGM(AudioClip clip, bool fadeIn = true)
        {
            if (clip == null) return;

            _bgmSource.clip = clip;
            _bgmSource.volume = _isMuted ? 0f : _masterVolume * _bgmVolume;
            _bgmSource.Play();
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBGM()
        {
            _bgmSource.Stop();
        }

        /// <summary>
        /// 모든 사운드 정지
        /// </summary>
        public void StopAll()
        {
            StopBGM();
            StopLoopSFX();

            foreach (var source in _sfxPool)
            {
                source.Stop();
            }
        }

        /// <summary>
        /// 사용 가능한 AudioSource 가져오기
        /// </summary>
        private AudioSource GetAvailableAudioSource()
        {
            foreach (var source in _sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // 풀이 모두 사용 중이면 새로 생성 (최대 20개까지)
            if (_sfxPool.Count < 20)
            {
                return CreatePooledAudioSource();
            }

            // 가장 오래된 것 재사용
            return _sfxPool[0];
        }

        #endregion

        #region 슬롯 결과별 사운드

        /// <summary>
        /// 슬롯 결과에 따른 사운드 재생
        /// </summary>
        public void PlaySlotResultSound(SlotOutcome outcome)
        {
            SoundType soundType = outcome switch
            {
                SlotOutcome.Loss => SoundType.ResultLoss,
                SlotOutcome.Draw => SoundType.ResultDraw,
                SlotOutcome.MiniWin => SoundType.ResultMiniWin,
                SlotOutcome.SmallWin => SoundType.ResultSmallWin,
                SlotOutcome.BigWin => SoundType.ResultBigWin,
                SlotOutcome.Jackpot => SoundType.ResultJackpot,
                SlotOutcome.MegaJackpot => SoundType.ResultMegaJackpot,
                _ => SoundType.ResultLoss
            };

            PlaySFX(soundType);
        }

        #endregion

        #region 설정 저장/로드

        private const string PREF_MASTER_VOLUME = "SoundSettings_MasterVolume";
        private const string PREF_SFX_VOLUME = "SoundSettings_SFXVolume";
        private const string PREF_BGM_VOLUME = "SoundSettings_BGMVolume";
        private const string PREF_IS_MUTED = "SoundSettings_IsMuted";

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, _masterVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, _sfxVolume);
            PlayerPrefs.SetFloat(PREF_BGM_VOLUME, _bgmVolume);
            PlayerPrefs.SetInt(PREF_IS_MUTED, _isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 1f);
            _bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOLUME, 0.7f);
            _isMuted = PlayerPrefs.GetInt(PREF_IS_MUTED, 0) == 1;

            ApplyVolumeSettings();
        }

        /// <summary>
        /// 볼륨 설정 적용
        /// </summary>
        private void ApplyVolumeSettings()
        {
            float effectiveVolume = _isMuted ? 0f : _masterVolume;

            if (_bgmSource != null)
            {
                _bgmSource.volume = effectiveVolume * _bgmVolume;
            }

            if (_loopSfxSource != null)
            {
                _loopSfxSource.volume = effectiveVolume * _sfxVolume;
            }

            // 현재 재생 중인 SFX는 다음 재생 시 적용됨
        }

        #endregion

        #region 음소거 토글

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        public void SetMute(bool mute)
        {
            IsMuted = mute;
        }

        #endregion
    }
}
