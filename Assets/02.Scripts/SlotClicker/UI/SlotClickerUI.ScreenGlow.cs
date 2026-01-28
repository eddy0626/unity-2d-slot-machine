using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;
using DG.Tweening;
using SlotClicker.Core;
using SlotClicker.Data;

namespace SlotClicker.UI
{
    public partial class SlotClickerUI : MonoBehaviour
    {
        #region Screen Edge Glow

        /// <summary>
        /// 화면 테두리 글로우 생성
        /// </summary>
        private void CreateScreenGlowEdges()
        {
            if (_mainCanvas == null || _screenGlowEdges != null) return;

            _screenGlowEdges = new Image[4]; // 상, 하, 좌, 우
            _screenGlowTweens = new Tween[4];
            _createdScreenGlow = true;

            RectTransform canvasRect = _mainCanvas.GetComponent<RectTransform>();

            // 상단 테두리
            _screenGlowEdges[0] = CreateGlowEdge(canvasRect, "ScreenGlow_Top",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -_screenGlowThickness / 2), new Vector2(0, _screenGlowThickness));

            // 하단 테두리
            _screenGlowEdges[1] = CreateGlowEdge(canvasRect, "ScreenGlow_Bottom",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, _screenGlowThickness / 2), new Vector2(0, _screenGlowThickness));

            // 좌측 테두리
            _screenGlowEdges[2] = CreateGlowEdge(canvasRect, "ScreenGlow_Left",
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(_screenGlowThickness / 2, 0), new Vector2(_screenGlowThickness, 0));

            // 우측 테두리
            _screenGlowEdges[3] = CreateGlowEdge(canvasRect, "ScreenGlow_Right",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-_screenGlowThickness / 2, 0), new Vector2(_screenGlowThickness, 0));

            // 초기 상태: 투명
            foreach (var edge in _screenGlowEdges)
            {
                if (edge != null)
                {
                    Color c = edge.color;
                    edge.color = new Color(c.r, c.g, c.b, 0f);
                }
            }
        }

        private Image CreateGlowEdge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = _criticalScreenGlowColor;

            // 단색 오버레이로 사용 (스프라이트 불필요)
            img.sprite = null;

            return img;
        }

        /// <summary>
        /// 화면 테두리 글로우 효과 실행
        /// </summary>
        private void PlayScreenGlow(bool isJackpot = false)
        {
            if (!_enableScreenGlow || _screenGlowEdges == null) return;

            Color glowColor = isJackpot ? _jackpotScreenGlowColor : _criticalScreenGlowColor;
            float duration = isJackpot ? _screenGlowDuration * 1.5f : _screenGlowDuration;
            int loops = isJackpot ? 4 : 2;

            for (int i = 0; i < _screenGlowEdges.Length; i++)
            {
                if (_screenGlowEdges[i] == null) continue;

                _screenGlowTweens[i]?.Kill();
                _screenGlowEdges[i].transform.SetAsLastSibling();
                _screenGlowEdges[i].color = glowColor;

                int index = i; // 클로저용
                _screenGlowTweens[i] = _screenGlowEdges[i]
                    .DOFade(0f, duration / loops)
                    .SetLoops(loops, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        if (_screenGlowEdges[index] != null)
                        {
                            Color c = _screenGlowEdges[index].color;
                            _screenGlowEdges[index].color = new Color(c.r, c.g, c.b, 0f);
                        }
                    });
            }
        }

        private void CleanupScreenGlow()
        {
            if (_screenGlowTweens != null)
            {
                for (int i = 0; i < _screenGlowTweens.Length; i++)
                {
                    _screenGlowTweens[i]?.Kill();
                }
            }

            if (_screenGlowEdges != null && _createdScreenGlow)
            {
                foreach (var edge in _screenGlowEdges)
                {
                    if (edge != null)
                        Destroy(edge.gameObject);
                }
            }
            _screenGlowEdges = null;
        }

        #endregion
    }
}
