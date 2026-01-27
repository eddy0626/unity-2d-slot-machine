using UnityEngine;
using UnityEngine.UI;
using SlotClicker.Core;

namespace SlotClicker.UI
{
    /// <summary>
    /// UI 요소에 사운드를 자동으로 연결하는 헬퍼 컴포넌트
    /// Button에 부착하면 클릭 시 사운드 재생
    /// </summary>
    public class UISoundHelper : MonoBehaviour
    {
        [Header("=== 사운드 설정 ===")]
        [SerializeField] private SoundType _clickSound = SoundType.UIButtonClick;
        [SerializeField] private float _volumeScale = 1f;

        [Header("=== 자동 연결 ===")]
        [SerializeField] private bool _autoConnectButton = true;

        private Button _button;

        private void Awake()
        {
            if (_autoConnectButton)
            {
                _button = GetComponent<Button>();
                if (_button != null)
                {
                    _button.onClick.AddListener(PlayClickSound);
                }
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClickSound);
            }
        }

        public void PlayClickSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(_clickSound, _volumeScale);
            }
        }

        /// <summary>
        /// 외부에서 호출 가능한 사운드 재생
        /// </summary>
        public void PlaySound(SoundType soundType)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundType, _volumeScale);
            }
        }

        #region 정적 헬퍼 메서드

        /// <summary>
        /// 버튼 클릭 사운드 재생
        /// </summary>
        public static void PlayButtonClick()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.UIButtonClick);
            }
        }

        /// <summary>
        /// 팝업 열기 사운드 재생
        /// </summary>
        public static void PlayPopupOpen()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.UIPopupOpen);
            }
        }

        /// <summary>
        /// 팝업 닫기 사운드 재생
        /// </summary>
        public static void PlayPopupClose()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.UIPopupClose);
            }
        }

        /// <summary>
        /// 탭 전환 사운드 재생
        /// </summary>
        public static void PlayTabSwitch()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.UITabSwitch);
            }
        }

        #endregion
    }
}
