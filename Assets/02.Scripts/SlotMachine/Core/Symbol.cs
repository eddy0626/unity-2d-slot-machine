using UnityEngine;
using UnityEngine.UI;
using SlotMachine.Data;

namespace SlotMachine.Core
{
    public class Symbol : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image symbolImage;
        [SerializeField] private Image glowImage;

        private SymbolData _data;
        private RectTransform _rectTransform;

        public SymbolData Data => _data;
        public int SymbolId => _data != null ? _data.symbolId : -1;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (symbolImage == null)
                symbolImage = GetComponent<Image>();
        }

        public void Setup(SymbolData data)
        {
            _data = data;
            if (data != null && symbolImage != null)
            {
                symbolImage.sprite = data.sprite;
                symbolImage.enabled = true;
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (symbolImage != null)
                symbolImage.sprite = sprite;
        }

        public void ShowGlow(bool show)
        {
            if (glowImage != null)
            {
                glowImage.enabled = show;
                if (show && _data != null)
                {
                    glowImage.color = _data.glowColor;
                }
            }
        }

        public void SetPosition(float y)
        {
            if (_rectTransform != null)
            {
                Vector2 pos = _rectTransform.anchoredPosition;
                pos.y = y;
                _rectTransform.anchoredPosition = pos;
            }
        }

        public float GetPositionY()
        {
            return _rectTransform != null ? _rectTransform.anchoredPosition.y : 0f;
        }

        public bool IsWild()
        {
            return _data != null && _data.symbolType == SymbolType.Wild;
        }

        public bool IsScatter()
        {
            return _data != null && _data.symbolType == SymbolType.Scatter;
        }
    }
}
