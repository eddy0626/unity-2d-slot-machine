using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Core;

namespace SlotMachine.UI
{
    public class PaylineRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform[] symbolPositions; // 9개 심볼 위치
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private Transform lineContainer;

        [Header("Line Settings")]
        [SerializeField] private float lineWidth = 8f;
        [SerializeField] private float glowIntensity = 1.5f;
        [SerializeField] private float animationSpeed = 2f;

        private List<LineRenderer> _activeLines = new List<LineRenderer>();
        private List<Image> _activeUILines = new List<Image>();

        // 페이라인 정의
        private readonly int[][] _paylines = new int[][]
        {
            new int[] { 3, 4, 5 },  // 중앙 가로
            new int[] { 0, 1, 2 },  // 상단 가로
            new int[] { 6, 7, 8 },  // 하단 가로
            new int[] { 0, 4, 8 },  // 대각선 ↘
            new int[] { 6, 4, 2 }   // 대각선 ↗
        };

        private readonly Color[] _paylineColors = new Color[]
        {
            new Color(1f, 0.84f, 0f, 0.8f),   // Gold
            new Color(0f, 1f, 0.6f, 0.8f),    // Cyan
            new Color(1f, 0.2f, 0.6f, 0.8f),  // Pink
            new Color(0.6f, 0.2f, 1f, 0.8f),  // Purple
            new Color(0.2f, 0.8f, 1f, 0.8f)   // Light Blue
        };

        /// <summary>
        /// 당첨 페이라인 표시
        /// </summary>
        public void ShowWinningPayline(int paylineIndex, float duration = 2f)
        {
            if (paylineIndex < 0 || paylineIndex >= _paylines.Length) return;

            StartCoroutine(AnimatePayline(paylineIndex, duration));
        }

        /// <summary>
        /// 여러 페이라인 순차 표시
        /// </summary>
        public void ShowMultiplePaylines(List<int> paylineIndices, float delayBetween = 0.5f)
        {
            StartCoroutine(ShowPaylinesSequentially(paylineIndices, delayBetween));
        }

        private IEnumerator ShowPaylinesSequentially(List<int> indices, float delay)
        {
            foreach (int index in indices)
            {
                ShowWinningPayline(index, 1.5f);
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator AnimatePayline(int paylineIndex, float duration)
        {
            int[] positions = _paylines[paylineIndex];
            Color lineColor = _paylineColors[paylineIndex];

            // UI 라인 생성
            List<Image> lineSegments = CreateUILine(positions, lineColor);
            _activeUILines.AddRange(lineSegments);

            // 애니메이션
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.PingPong(elapsed * animationSpeed, 1f) * 0.5f + 0.5f;

                foreach (var segment in lineSegments)
                {
                    if (segment != null)
                    {
                        Color c = segment.color;
                        c.a = alpha * 0.8f;
                        segment.color = c;
                    }
                }

                yield return null;
            }

            // 정리
            foreach (var segment in lineSegments)
            {
                if (segment != null)
                {
                    _activeUILines.Remove(segment);
                    Destroy(segment.gameObject);
                }
            }
        }

        private List<Image> CreateUILine(int[] positions, Color color)
        {
            List<Image> segments = new List<Image>();

            for (int i = 0; i < positions.Length - 1; i++)
            {
                int fromIndex = positions[i];
                int toIndex = positions[i + 1];

                if (fromIndex >= symbolPositions.Length || toIndex >= symbolPositions.Length) continue;

                Vector3 fromPos = symbolPositions[fromIndex].position;
                Vector3 toPos = symbolPositions[toIndex].position;

                // 라인 세그먼트 생성
                GameObject lineObj = new GameObject($"PaylineSeg_{i}");
                lineObj.transform.SetParent(lineContainer != null ? lineContainer : transform);

                Image lineImage = lineObj.AddComponent<Image>();
                lineImage.color = color;

                RectTransform rt = lineObj.GetComponent<RectTransform>();

                // 두 점 사이 위치 및 회전 계산
                Vector3 midPoint = (fromPos + toPos) / 2f;
                float distance = Vector3.Distance(fromPos, toPos);
                float angle = Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg;

                rt.position = midPoint;
                rt.sizeDelta = new Vector2(distance, lineWidth);
                rt.rotation = Quaternion.Euler(0, 0, angle);

                segments.Add(lineImage);
            }

            return segments;
        }

        /// <summary>
        /// 스캐터 위치 하이라이트
        /// </summary>
        public void HighlightScatterPositions(int[] positions, Color color)
        {
            StartCoroutine(AnimateScatterHighlight(positions, color));
        }

        private IEnumerator AnimateScatterHighlight(int[] positions, Color color)
        {
            List<GameObject> highlights = new List<GameObject>();

            foreach (int pos in positions)
            {
                if (pos >= 0 && pos < symbolPositions.Length)
                {
                    GameObject highlight = CreateHighlight(symbolPositions[pos], color);
                    highlights.Add(highlight);
                }
            }

            // 애니메이션
            float duration = 2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed * Mathf.PI * 4f) * 0.1f;

                foreach (var highlight in highlights)
                {
                    if (highlight != null)
                        highlight.transform.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            // 정리
            foreach (var highlight in highlights)
            {
                if (highlight != null)
                    Destroy(highlight);
            }
        }

        private GameObject CreateHighlight(RectTransform target, Color color)
        {
            GameObject highlight = new GameObject("ScatterHighlight");
            highlight.transform.SetParent(target);
            highlight.transform.localPosition = Vector3.zero;
            highlight.transform.localScale = Vector3.one;

            Image img = highlight.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 0.4f);
            img.raycastTarget = false;

            RectTransform rt = highlight.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return highlight;
        }

        /// <summary>
        /// 모든 페이라인 표시 지우기
        /// </summary>
        public void ClearAllLines()
        {
            foreach (var line in _activeUILines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            _activeUILines.Clear();

            StopAllCoroutines();
        }

        /// <summary>
        /// 페이라인 미리보기 (호버 시)
        /// </summary>
        public void PreviewPayline(int paylineIndex)
        {
            ClearAllLines();

            if (paylineIndex < 0 || paylineIndex >= _paylines.Length) return;

            int[] positions = _paylines[paylineIndex];
            Color color = _paylineColors[paylineIndex];
            color.a = 0.4f; // 미리보기는 반투명

            List<Image> segments = CreateUILine(positions, color);
            _activeUILines.AddRange(segments);
        }

        public Color GetPaylineColor(int index)
        {
            if (index >= 0 && index < _paylineColors.Length)
                return _paylineColors[index];
            return Color.white;
        }
    }
}
