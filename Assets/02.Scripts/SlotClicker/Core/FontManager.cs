using UnityEngine;
using TMPro;

namespace SlotClicker.Core
{
    /// <summary>
    /// 런타임 폰트 관리자
    /// 동적으로 생성되는 TMP 텍스트에 커스텀 폰트 적용
    /// </summary>
    public static class FontManager
    {
        private static TMP_FontAsset _customFont;
        private static bool _initialized = false;
        private static bool _fontValid = false;

        /// <summary>
        /// 현재 적용 중인 커스텀 폰트
        /// </summary>
        public static TMP_FontAsset CustomFont
        {
            get
            {
                if (!_initialized)
                {
                    Initialize();
                }
                return _customFont;
            }
        }

        /// <summary>
        /// 커스텀 폰트가 로드되었고 유효한지 여부
        /// </summary>
        public static bool HasCustomFont => _fontValid && _customFont != null;

        /// <summary>
        /// 초기화 - Resources 폴더에서 폰트 로드
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Resources 폴더에서 폰트 로드 시도
            _customFont = Resources.Load<TMP_FontAsset>("Fonts/NeoDunggeunmoPro-Regular SDF");

            if (_customFont == null)
            {
                // 대체 경로 시도
                _customFont = Resources.Load<TMP_FontAsset>("NeoDunggeunmoPro-Regular SDF");
            }

            if (_customFont != null)
            {
                // 폰트 에셋 유효성 검사
                _fontValid = ValidateFontAsset(_customFont);

                if (_fontValid)
                {
                    Debug.Log("[FontManager] Custom font loaded: NeoDunggeunmoPro-Regular");
                }
                else
                {
                    Debug.LogWarning("[FontManager] Custom font found but atlas is invalid. Using default font.");
                    _customFont = null;
                }
            }
            else
            {
                Debug.Log("[FontManager] Custom font not found in Resources. Using default font.");
            }
        }

        /// <summary>
        /// 폰트 에셋 유효성 검사
        /// </summary>
        private static bool ValidateFontAsset(TMP_FontAsset font)
        {
            if (font == null) return false;

            // 아틀라스 텍스처 체크
            if (font.atlasTexture == null)
            {
                Debug.LogWarning("[FontManager] Font atlas texture is null");
                return false;
            }

            // 아틀라스 크기 체크
            if (font.atlasWidth <= 0 || font.atlasHeight <= 0)
            {
                Debug.LogWarning("[FontManager] Font atlas size is invalid");
                return false;
            }

            // Material 체크
            if (font.material == null)
            {
                Debug.LogWarning("[FontManager] Font material is null");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 폰트 에셋 직접 설정 (Editor에서 사용)
        /// </summary>
        public static void SetCustomFont(TMP_FontAsset font)
        {
            if (font != null && ValidateFontAsset(font))
            {
                _customFont = font;
                _fontValid = true;
                _initialized = true;
                Debug.Log($"[FontManager] Custom font set: {font.name}");
            }
            else
            {
                _customFont = null;
                _fontValid = false;
                Debug.LogWarning("[FontManager] Failed to set custom font - invalid or null");
            }
        }

        /// <summary>
        /// TMP 컴포넌트에 커스텀 폰트 적용
        /// </summary>
        public static void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null || !HasCustomFont) return;

            try
            {
                text.font = _customFont;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FontManager] Failed to apply font: {e.Message}");
                _fontValid = false;
            }
        }

        /// <summary>
        /// GameObject 하위의 모든 TMP 컴포넌트에 폰트 적용
        /// </summary>
        public static void ApplyFontToAll(GameObject root)
        {
            if (root == null || !HasCustomFont) return;

            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                try
                {
                    text.font = _customFont;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FontManager] Failed to apply font to {text.name}: {e.Message}");
                    _fontValid = false;
                    return;
                }
            }
        }

        /// <summary>
        /// 새 TMP 텍스트 생성 시 자동으로 폰트 적용
        /// </summary>
        public static TextMeshProUGUI CreateText(GameObject parent, string name = "Text")
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();

            if (HasCustomFont)
            {
                try
                {
                    text.font = _customFont;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FontManager] Failed to apply font to new text: {e.Message}");
                }
            }

            return text;
        }

        /// <summary>
        /// 폰트 상태 리셋 (디버깅용)
        /// </summary>
        public static void Reset()
        {
            _initialized = false;
            _fontValid = false;
            _customFont = null;
        }
    }
}
