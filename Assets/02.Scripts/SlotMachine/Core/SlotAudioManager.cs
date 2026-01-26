using UnityEngine;
using System.Collections;

namespace SlotMachine.Core
{
    public class SlotAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource spinLoopSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip spinStartClip;
        [SerializeField] private AudioClip spinLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip winSmallClip;
        [SerializeField] private AudioClip winMediumClip;
        [SerializeField] private AudioClip winBigClip;
        [SerializeField] private AudioClip winJackpotClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip coinAddClip;
        [SerializeField] private AudioClip betChangeClip;

        [Header("Background Music")]
        [SerializeField] private AudioClip bgmClip;

        [Header("Settings")]
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;

        private static SlotAudioManager _instance;
        public static SlotAudioManager Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeAudioSources();
            PlayBackgroundMusic();
        }

        private void InitializeAudioSources()
        {
            // SFX 소스 생성
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            // 음악 소스 생성
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            // 스핀 루프 소스 생성
            if (spinLoopSource == null)
            {
                spinLoopSource = gameObject.AddComponent<AudioSource>();
                spinLoopSource.playOnAwake = false;
                spinLoopSource.loop = true;
            }
        }

        #region Spin Sounds

        public void PlaySpinStart()
        {
            PlaySFX(spinStartClip);

            // 스핀 루프 시작
            if (spinLoopClip != null && spinLoopSource != null)
            {
                spinLoopSource.clip = spinLoopClip;
                spinLoopSource.volume = sfxVolume * 0.5f;
                spinLoopSource.Play();
            }
        }

        public void PlayReelStop(int reelIndex)
        {
            // 약간의 피치 변화로 다양성 추가
            float pitch = 0.95f + (reelIndex * 0.05f);
            PlaySFXWithPitch(reelStopClip, pitch);

            // 마지막 릴이면 스핀 루프 정지
            if (reelIndex >= 2)
            {
                StopSpinLoop();
            }
        }

        public void StopSpinLoop()
        {
            if (spinLoopSource != null && spinLoopSource.isPlaying)
            {
                StartCoroutine(FadeOutAudio(spinLoopSource, 0.3f));
            }
        }

        #endregion

        #region Win Sounds

        public void PlayWinSound(int multiplier, int betAmount)
        {
            int winAmount = multiplier * betAmount;

            AudioClip clipToPlay;

            if (winAmount >= betAmount * 50)
            {
                clipToPlay = winJackpotClip;
            }
            else if (winAmount >= betAmount * 20)
            {
                clipToPlay = winBigClip;
            }
            else if (winAmount >= betAmount * 10)
            {
                clipToPlay = winMediumClip;
            }
            else
            {
                clipToPlay = winSmallClip;
            }

            PlaySFX(clipToPlay);
        }

        public void PlayWinByTier(WinTier tier)
        {
            AudioClip clip = tier switch
            {
                WinTier.Small => winSmallClip,
                WinTier.Medium => winMediumClip,
                WinTier.Big => winBigClip,
                WinTier.Jackpot => winJackpotClip,
                _ => winSmallClip
            };

            PlaySFX(clip);
        }

        #endregion

        #region UI Sounds

        public void PlayButtonClick()
        {
            PlaySFX(buttonClickClip);
        }

        public void PlayBetChange()
        {
            PlaySFX(betChangeClip);
        }

        public void PlayCoinAdd()
        {
            PlaySFX(coinAddClip);
        }

        #endregion

        #region Background Music

        public void PlayBackgroundMusic()
        {
            if (bgmClip != null && musicSource != null)
            {
                musicSource.clip = bgmClip;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        public void StopBackgroundMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        #endregion

        #region Helper Methods

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        private void PlaySFXWithPitch(AudioClip clip, float pitch)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(clip, sfxVolume);
                sfxSource.pitch = 1f;
            }
        }

        private IEnumerator FadeOutAudio(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }

        #endregion

        public enum WinTier
        {
            Small,
            Medium,
            Big,
            Jackpot
        }
    }
}
