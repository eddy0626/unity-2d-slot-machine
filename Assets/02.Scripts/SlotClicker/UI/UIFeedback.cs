using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace SlotClicker.UI
{
    /// <summary>
    /// UI 피드백 및 공통 팝업 유틸리티
    /// </summary>
    public class UIFeedback : MonoBehaviour
    {
        private static UIFeedback _instance;
        public static UIFeedback Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("UIFeedback");
                    _instance = obj.AddComponent<UIFeedback>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        // 확인 팝업 UI 참조
        private GameObject _confirmPopup;
        private TextMeshProUGUI _confirmTitleText;
        private TextMeshProUGUI _confirmMessageText;
        private Button _confirmYesButton;
        private Button _confirmNoButton;
        private CanvasGroup _confirmCanvasGroup;
        private Action _onConfirm;
        private Action _onCancel;

        // 토스트 UI 참조
        private GameObject _toastPanel;
        private TextMeshProUGUI _toastText;
        private CanvasGroup _toastCanvasGroup;
        private Tween _toastTween;

        // 햅틱 피드백 (모바일)
        private bool _hapticEnabled = true;

        private Canvas _mainCanvas;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 캔버스 설정 (SlotClickerUI에서 호출)
        /// </summary>
        public void SetCanvas(Canvas canvas)
        {
            _mainCanvas = canvas;
            CreateConfirmPopup();
            CreateToastUI();
        }

        #region 확인 팝업

        private void CreateConfirmPopup()
        {
            if (_mainCanvas == null || _confirmPopup != null) return;

            // 배경 오버레이
            _confirmPopup = new GameObject("ConfirmPopup");
            _confirmPopup.transform.SetParent(_mainCanvas.transform, false);

            RectTransform popupRect = _confirmPopup.AddComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;

            Image overlay = _confirmPopup.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.7f);

            _confirmCanvasGroup = _confirmPopup.AddComponent<CanvasGroup>();

            // 팝업 내용 패널
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_confirmPopup.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 300);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.15f, 0.12f, 0.2f, 0.98f);

            // 제목
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(0, 50);

            _confirmTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            _confirmTitleText.text = "확인";
            _confirmTitleText.fontSize = 36;
            _confirmTitleText.fontStyle = FontStyles.Bold;
            _confirmTitleText.color = Color.white;
            _confirmTitleText.alignment = TextAlignmentOptions.Center;

            // 메시지
            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(panel.transform, false);

            RectTransform msgRect = msgObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.35f);
            msgRect.anchorMax = new Vector2(0.9f, 0.75f);
            msgRect.offsetMin = Vector2.zero;
            msgRect.offsetMax = Vector2.zero;

            _confirmMessageText = msgObj.AddComponent<TextMeshProUGUI>();
            _confirmMessageText.text = "";
            _confirmMessageText.fontSize = 28;
            _confirmMessageText.color = new Color(0.9f, 0.9f, 0.9f);
            _confirmMessageText.alignment = TextAlignmentOptions.Center;

            // 버튼 컨테이너
            GameObject btnContainer = new GameObject("Buttons");
            btnContainer.transform.SetParent(panel.transform, false);

            RectTransform btnContainerRect = btnContainer.AddComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0, 0);
            btnContainerRect.anchorMax = new Vector2(1, 0.3f);
            btnContainerRect.offsetMin = new Vector2(30, 20);
            btnContainerRect.offsetMax = new Vector2(-30, -10);

            HorizontalLayoutGroup btnLayout = btnContainer.AddComponent<HorizontalLayoutGroup>();
            btnLayout.spacing = 30;
            btnLayout.childForceExpandWidth = true;
            btnLayout.childForceExpandHeight = true;
            btnLayout.childControlWidth = true;
            btnLayout.childControlHeight = true;

            // 취소 버튼
            _confirmNoButton = CreatePopupButton(btnContainer.transform, "취소", new Color(0.5f, 0.5f, 0.5f));
            _confirmNoButton.onClick.AddListener(OnConfirmNo);

            // 확인 버튼
            _confirmYesButton = CreatePopupButton(btnContainer.transform, "확인", new Color(0.2f, 0.6f, 0.2f));
            _confirmYesButton.onClick.AddListener(OnConfirmYes);

            _confirmPopup.SetActive(false);
        }

        private Button CreatePopupButton(Transform parent, string label, Color color)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;

            ColorBlock colors = btn.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;

            // 터치 타겟 크기 보장 (최소 48px)
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 55;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        /// <summary>
        /// 확인 팝업 표시
        /// </summary>
        public void ShowConfirmPopup(string title, string message, Action onConfirm, Action onCancel = null,
            string confirmText = "확인", string cancelText = "취소", Color? confirmColor = null)
        {
            if (_confirmPopup == null)
            {
                onConfirm?.Invoke(); // 팝업이 없으면 바로 실행
                return;
            }

            _onConfirm = onConfirm;
            _onCancel = onCancel;

            _confirmTitleText.text = title;
            _confirmMessageText.text = message;

            // 버튼 텍스트 변경
            _confirmYesButton.GetComponentInChildren<TextMeshProUGUI>().text = confirmText;
            _confirmNoButton.GetComponentInChildren<TextMeshProUGUI>().text = cancelText;

            // 확인 버튼 색상
            if (confirmColor.HasValue)
            {
                _confirmYesButton.GetComponent<Image>().color = confirmColor.Value;
            }
            else
            {
                _confirmYesButton.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
            }

            // 애니메이션으로 표시
            _confirmPopup.SetActive(true);
            _confirmCanvasGroup.alpha = 0;
            _confirmCanvasGroup.DOFade(1f, 0.2f);

            Transform panel = _confirmPopup.transform.Find("Panel");
            if (panel != null)
            {
                panel.localScale = Vector3.one * 0.8f;
                panel.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            PlayButtonFeedback(_confirmPopup);
        }

        private void OnConfirmYes()
        {
            HideConfirmPopup();
            _onConfirm?.Invoke();
        }

        private void OnConfirmNo()
        {
            HideConfirmPopup();
            _onCancel?.Invoke();
        }

        private void HideConfirmPopup()
        {
            if (_confirmPopup == null) return;

            _confirmCanvasGroup.DOFade(0f, 0.15f).OnComplete(() =>
            {
                _confirmPopup.SetActive(false);
            });
        }

        #endregion

        #region 토스트 메시지

        private void CreateToastUI()
        {
            if (_mainCanvas == null || _toastPanel != null) return;

            _toastPanel = new GameObject("FeedbackToast");
            _toastPanel.transform.SetParent(_mainCanvas.transform, false);

            RectTransform toastRect = _toastPanel.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.15f);
            toastRect.anchorMax = new Vector2(0.5f, 0.15f);
            toastRect.sizeDelta = new Vector2(600, 60);

            Image toastBg = _toastPanel.AddComponent<Image>();
            toastBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            _toastCanvasGroup = _toastPanel.AddComponent<CanvasGroup>();
            _toastCanvasGroup.alpha = 0;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(_toastPanel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15, 5);
            textRect.offsetMax = new Vector2(-15, -5);

            _toastText = textObj.AddComponent<TextMeshProUGUI>();
            _toastText.fontSize = 28;
            _toastText.color = Color.white;
            _toastText.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// 토스트 메시지 표시
        /// </summary>
        public void ShowToast(string message, Color? color = null, float duration = 2f, ToastType type = ToastType.Info)
        {
            if (_toastPanel == null || _toastText == null) return;

            _toastTween?.Kill();

            _toastText.text = message;

            // 타입별 색상
            Color bgColor = type switch
            {
                ToastType.Success => new Color(0.1f, 0.4f, 0.1f, 0.95f),
                ToastType.Error => new Color(0.5f, 0.1f, 0.1f, 0.95f),
                ToastType.Warning => new Color(0.5f, 0.4f, 0.1f, 0.95f),
                _ => new Color(0.1f, 0.1f, 0.2f, 0.95f)
            };

            _toastPanel.GetComponent<Image>().color = bgColor;
            _toastText.color = color ?? Color.white;

            // 애니메이션
            _toastCanvasGroup.alpha = 0;
            _toastPanel.transform.localScale = Vector3.one * 0.9f;

            Sequence seq = DOTween.Sequence();
            seq.Append(_toastCanvasGroup.DOFade(1f, 0.2f));
            seq.Join(_toastPanel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
            seq.AppendInterval(duration);
            seq.Append(_toastCanvasGroup.DOFade(0f, 0.3f));

            _toastTween = seq;
        }

        #endregion

        #region 버튼 피드백

        /// <summary>
        /// 버튼에 클릭 피드백 추가
        /// </summary>
        public static void AddButtonFeedback(Button button, bool playSound = true)
        {
            if (button == null) return;

            button.onClick.AddListener(() =>
            {
                PlayButtonFeedback(button.gameObject);
                if (playSound)
                {
                    // ★ 버튼 클릭 사운드 재생
                    if (SlotClicker.Core.SoundManager.Instance != null)
                    {
                        SlotClicker.Core.SoundManager.Instance.PlaySFX(SlotClicker.Core.SoundType.UIButtonClick);
                    }
                }
            });
        }

        /// <summary>
        /// 오브젝트에 클릭 피드백 애니메이션
        /// </summary>
        public static void PlayButtonFeedback(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();
            obj.transform.localScale = Vector3.one;
            obj.transform.DOPunchScale(Vector3.one * 0.08f, 0.15f, 5, 0.5f);

            // ★ 버튼 색상 플래시 효과
            Image buttonImage = obj.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.DOKill();
                Color originalColor = buttonImage.color;
                buttonImage.DOColor(originalColor * 1.3f, 0.08f)
                    .OnComplete(() => buttonImage.DOColor(originalColor, 0.12f));
            }

            // 햅틱 피드백 (모바일)
            TriggerHaptic();
        }

        /// <summary>
        /// 구매 성공 피드백
        /// </summary>
        public static void PlayPurchaseSuccessFeedback(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();

            // 스케일 바운스
            Sequence seq = DOTween.Sequence();
            seq.Append(obj.transform.DOScale(1.15f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(obj.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBounce));

            // 색상 플래시 (Image가 있는 경우)
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                Color originalColor = img.color;
                img.DOColor(Color.white, 0.1f).OnComplete(() =>
                {
                    img.DOColor(originalColor, 0.2f);
                });
            }

            TriggerHaptic(HapticType.Success);
        }

        /// <summary>
        /// 구매 실패 피드백
        /// </summary>
        public static void PlayPurchaseFailFeedback(GameObject obj)
        {
            if (obj == null) return;

            obj.transform.DOKill();

            // 흔들림 효과
            obj.transform.DOShakePosition(0.3f, new Vector3(10, 0, 0), 20, 90, false, true);

            // 색상 플래시
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                Color originalColor = img.color;
                img.DOColor(new Color(1f, 0.3f, 0.3f), 0.1f).OnComplete(() =>
                {
                    img.DOColor(originalColor, 0.2f);
                });
            }

            TriggerHaptic(HapticType.Error);
        }

        #endregion

        #region 햅틱 피드백

        public enum HapticType
        {
            Light,
            Medium,
            Heavy,
            Success,
            Warning,
            Error
        }

        public static void TriggerHaptic(HapticType type = HapticType.Light)
        {
#if UNITY_IOS || UNITY_ANDROID
            if (!Instance._hapticEnabled) return;

            // Unity의 Handheld.Vibrate()는 기본적인 진동만 지원
            // 더 세밀한 햅틱은 플랫폼별 플러그인 필요
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Android 기본 진동 (짧은 진동)
                if (type == HapticType.Error || type == HapticType.Heavy)
                {
                    Handheld.Vibrate();
                }
#elif UNITY_IOS && !UNITY_EDITOR
                // iOS는 추가 플러그인 필요 (Taptic Engine)
                Handheld.Vibrate();
#endif
            }
            catch (System.Exception)
            {
                // 진동 기능이 없는 기기에서 무시
            }
#endif
        }

        public void SetHapticEnabled(bool enabled)
        {
            _hapticEnabled = enabled;
        }

        #endregion

        private void OnDestroy()
        {
            _toastTween?.Kill();

            if (_confirmPopup != null)
            {
                _confirmPopup.transform.DOKill();
            }
        }
    }

    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
