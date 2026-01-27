using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// 사운드 설정 UI
    /// 볼륨 슬라이더 및 음소거 버튼 관리
    /// </summary>
    public class SoundSettingsUI : MonoBehaviour
    {
        [Header("=== 슬라이더 ===")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _bgmVolumeSlider;

        [Header("=== 라벨 ===")]
        [SerializeField] private TextMeshProUGUI _masterVolumeLabel;
        [SerializeField] private TextMeshProUGUI _sfxVolumeLabel;
        [SerializeField] private TextMeshProUGUI _bgmVolumeLabel;

        [Header("=== 음소거 버튼 ===")]
        [SerializeField] private Button _muteButton;
        [SerializeField] private Image _muteIcon;
        [SerializeField] private Sprite _soundOnSprite;
        [SerializeField] private Sprite _soundOffSprite;

        [Header("=== 간단 모드 ===")]
        [SerializeField] private bool _simpleMode = false;
        [SerializeField] private Slider _singleVolumeSlider;
        [SerializeField] private TextMeshProUGUI _singleVolumeLabel;

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeUI()
        {
            if (SoundManager.Instance == null) return;

            if (_simpleMode)
            {
                // 간단 모드: 마스터 볼륨만 표시
                if (_singleVolumeSlider != null)
                {
                    _singleVolumeSlider.value = SoundManager.Instance.MasterVolume;
                    _singleVolumeSlider.onValueChanged.AddListener(OnSingleVolumeChanged);
                    UpdateSingleVolumeLabel();
                }
            }
            else
            {
                // 전체 모드: 개별 볼륨 표시
                if (_masterVolumeSlider != null)
                {
                    _masterVolumeSlider.value = SoundManager.Instance.MasterVolume;
                    _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                }

                if (_sfxVolumeSlider != null)
                {
                    _sfxVolumeSlider.value = SoundManager.Instance.SFXVolume;
                    _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                }

                if (_bgmVolumeSlider != null)
                {
                    _bgmVolumeSlider.value = SoundManager.Instance.BGMVolume;
                    _bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
                }

                UpdateVolumeLabels();
            }

            if (_muteButton != null)
            {
                _muteButton.onClick.AddListener(OnMuteButtonClicked);
            }

            UpdateMuteIcon();
        }

        private void SubscribeToEvents()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.OnVolumeChanged += OnVolumeSettingsChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.OnVolumeChanged -= OnVolumeSettingsChanged;
            }
        }

        #region 슬라이더 콜백

        private void OnMasterVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.MasterVolume = value;
            }
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SFXVolume = value;
            }
        }

        private void OnBGMVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.BGMVolume = value;
            }
        }

        private void OnSingleVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.MasterVolume = value;
            }
        }

        #endregion

        #region 음소거 버튼

        private void OnMuteButtonClicked()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToggleMute();
            }

            // 버튼 클릭 사운드는 음소거 해제 후에만 재생
            if (!SoundManager.Instance.IsMuted)
            {
                UISoundHelper.PlayButtonClick();
            }
        }

        private void UpdateMuteIcon()
        {
            if (_muteIcon == null) return;

            if (SoundManager.Instance != null)
            {
                bool isMuted = SoundManager.Instance.IsMuted;

                if (_soundOnSprite != null && _soundOffSprite != null)
                {
                    _muteIcon.sprite = isMuted ? _soundOffSprite : _soundOnSprite;
                }
                else
                {
                    // 스프라이트 없을 경우 색상으로 표현
                    _muteIcon.color = isMuted ? Color.red : Color.white;
                }
            }
        }

        #endregion

        #region UI 업데이트

        private void OnVolumeSettingsChanged()
        {
            if (_simpleMode)
            {
                UpdateSingleVolumeLabel();
            }
            else
            {
                UpdateVolumeLabels();
            }
            UpdateMuteIcon();
        }

        private void UpdateVolumeLabels()
        {
            if (SoundManager.Instance == null) return;

            if (_masterVolumeLabel != null)
            {
                _masterVolumeLabel.text = $"{Mathf.RoundToInt(SoundManager.Instance.MasterVolume * 100)}%";
            }

            if (_sfxVolumeLabel != null)
            {
                _sfxVolumeLabel.text = $"{Mathf.RoundToInt(SoundManager.Instance.SFXVolume * 100)}%";
            }

            if (_bgmVolumeLabel != null)
            {
                _bgmVolumeLabel.text = $"{Mathf.RoundToInt(SoundManager.Instance.BGMVolume * 100)}%";
            }
        }

        private void UpdateSingleVolumeLabel()
        {
            if (_singleVolumeLabel != null && SoundManager.Instance != null)
            {
                _singleVolumeLabel.text = $"{Mathf.RoundToInt(SoundManager.Instance.MasterVolume * 100)}%";
            }
        }

        #endregion

        #region 외부 호출용

        /// <summary>
        /// 설정 UI 표시 시 호출
        /// </summary>
        public void RefreshUI()
        {
            if (SoundManager.Instance == null) return;

            if (_simpleMode)
            {
                if (_singleVolumeSlider != null)
                {
                    _singleVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
                }
                UpdateSingleVolumeLabel();
            }
            else
            {
                if (_masterVolumeSlider != null)
                {
                    _masterVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
                }
                if (_sfxVolumeSlider != null)
                {
                    _sfxVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.SFXVolume);
                }
                if (_bgmVolumeSlider != null)
                {
                    _bgmVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume);
                }
                UpdateVolumeLabels();
            }

            UpdateMuteIcon();
        }

        #endregion
    }
}
