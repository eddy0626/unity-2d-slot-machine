using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Data;

namespace SlotMachine.Core
{
    public class Reel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int reelIndex;
        [SerializeField] private RectTransform symbolContainer;
        [SerializeField] private GameObject symbolPrefab;

        [Header("Spin Settings")]
        [SerializeField] private float symbolHeight = 150f;
        [SerializeField] private int visibleSymbols = 3;
        [SerializeField] private int bufferSymbols = 2;

        private List<Symbol> _symbols = new List<Symbol>();
        private SlotMachineConfig _config;
        private bool _isSpinning;
        private float _spinSpeed;
        private float _currentSpeed;

        // 결과 심볼들 (상단, 중앙, 하단)
        private SymbolData[] _resultSymbols;

        public int ReelIndex => reelIndex;
        public bool IsSpinning => _isSpinning;
        public SymbolData[] ResultSymbols => _resultSymbols;

        public void Initialize(SlotMachineConfig config, int index)
        {
            _config = config;
            reelIndex = index;
            symbolHeight = config.symbolHeight;
            _spinSpeed = config.spinSpeed;

            // symbolContainer 자동 탐색
            if (symbolContainer == null)
            {
                symbolContainer = transform.Find("SymbolContainer") as RectTransform;
                if (symbolContainer == null)
                {
                    symbolContainer = GetComponent<RectTransform>();
                }
            }

            CreateSymbols();
        }

        private void CreateSymbols()
        {
            // symbolContainer가 없으면 스킵
            if (symbolContainer == null)
            {
                Debug.LogWarning($"Reel {reelIndex}: symbolContainer is null, skipping symbol creation");
                return;
            }

            // symbolPrefab이 없으면 동적 생성
            if (symbolPrefab == null)
            {
                CreateSymbolsWithoutPrefab();
                return;
            }

            // 기존 심볼 제거
            foreach (Transform child in symbolContainer)
            {
                Destroy(child.gameObject);
            }
            _symbols.Clear();

            // 보이는 심볼 + 버퍼 심볼 생성
            int totalSymbols = visibleSymbols + bufferSymbols;
            float startY = (visibleSymbols / 2f) * symbolHeight + symbolHeight;

            for (int i = 0; i < totalSymbols; i++)
            {
                GameObject symbolObj = Instantiate(symbolPrefab, symbolContainer);
                Symbol symbol = symbolObj.GetComponent<Symbol>();

                if (symbol == null)
                    symbol = symbolObj.AddComponent<Symbol>();

                RectTransform rt = symbolObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, startY - (i * symbolHeight));
                rt.sizeDelta = new Vector2(symbolHeight, symbolHeight);

                // 랜덤 심볼 할당
                SymbolData randomSymbol = _config.GetRandomSymbol();
                symbol.Setup(randomSymbol);

                _symbols.Add(symbol);
            }
        }

        public void StartSpin()
        {
            if (_isSpinning) return;

            _isSpinning = true;
            _currentSpeed = _spinSpeed;
            StartCoroutine(SpinRoutine());
        }

        public void StopSpin(SymbolData[] targetSymbols)
        {
            _resultSymbols = targetSymbols;
            _isSpinning = false;
        }

        private IEnumerator SpinRoutine()
        {
            while (_isSpinning)
            {
                MoveSymbols();
                yield return null;
            }

            // 정지 시 감속 효과
            yield return StartCoroutine(DecelerateAndStop());
        }

        private void MoveSymbols()
        {
            float delta = _currentSpeed * Time.deltaTime;

            foreach (var symbol in _symbols)
            {
                float newY = symbol.GetPositionY() - delta;
                symbol.SetPosition(newY);
            }

            // 화면 밖으로 나간 심볼 재배치
            RecycleSymbols();
        }

        private void RecycleSymbols()
        {
            float bottomThreshold = -(visibleSymbols / 2f + 1) * symbolHeight;
            float topPosition = (visibleSymbols / 2f + bufferSymbols - 1) * symbolHeight;

            foreach (var symbol in _symbols)
            {
                if (symbol.GetPositionY() < bottomThreshold)
                {
                    // 맨 위로 이동하고 새 심볼 할당
                    symbol.SetPosition(topPosition);

                    if (_isSpinning)
                    {
                        SymbolData randomSymbol = _config.GetRandomSymbol();
                        symbol.Setup(randomSymbol);
                    }
                }
            }
        }

        private IEnumerator DecelerateAndStop()
        {
            float deceleration = _spinSpeed * 2f;

            // 감속
            while (_currentSpeed > 100f)
            {
                _currentSpeed -= deceleration * Time.deltaTime;
                MoveSymbols();
                yield return null;
            }

            // 결과 심볼 배치
            ArrangeResultSymbols();

            // 바운스 효과
            yield return StartCoroutine(BounceEffect());
        }

        private void ArrangeResultSymbols()
        {
            if (_resultSymbols == null || _resultSymbols.Length < 3) return;

            // 상단, 중앙, 하단 심볼 배치
            float[] positions = { symbolHeight, 0, -symbolHeight };

            for (int i = 0; i < 3 && i < _symbols.Count; i++)
            {
                _symbols[i].SetPosition(positions[i]);
                _symbols[i].Setup(_resultSymbols[i]);
            }
        }

        private IEnumerator BounceEffect()
        {
            float bounceAmount = _config.bounceAmount;
            float bounceDuration = _config.bounceDuration;
            float elapsed = 0f;

            // 아래로 바운스
            while (elapsed < bounceDuration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (bounceDuration / 2f);
                float offset = Mathf.Sin(t * Mathf.PI) * bounceAmount;

                foreach (var symbol in _symbols)
                {
                    float baseY = symbol.GetPositionY();
                    // 바운스는 시각적으로만 적용
                }
                yield return null;
            }

            // 원위치로 복귀
            elapsed = 0f;
            while (elapsed < bounceDuration / 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 프리팹 없이 동적으로 심볼 생성
        /// </summary>
        private void CreateSymbolsWithoutPrefab()
        {
            // 기존 심볼 제거
            foreach (Transform child in symbolContainer)
            {
                Destroy(child.gameObject);
            }
            _symbols.Clear();

            int totalSymbols = visibleSymbols + bufferSymbols;
            float startY = (visibleSymbols / 2f) * symbolHeight + symbolHeight;

            for (int i = 0; i < totalSymbols; i++)
            {
                // 동적으로 심볼 게임오브젝트 생성
                GameObject symbolObj = new GameObject($"Symbol_{i}");
                symbolObj.transform.SetParent(symbolContainer, false);

                // RectTransform 설정
                RectTransform rt = symbolObj.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, startY - (i * symbolHeight));
                rt.sizeDelta = new Vector2(symbolHeight - 10, symbolHeight - 10);

                // Image 컴포넌트 추가
                UnityEngine.UI.Image img = symbolObj.AddComponent<UnityEngine.UI.Image>();
                img.color = Color.white;

                // Symbol 컴포넌트 추가
                Symbol symbol = symbolObj.AddComponent<Symbol>();

                // 랜덤 심볼 할당
                if (_config != null && _config.symbols != null && _config.symbols.Length > 0)
                {
                    SymbolData randomSymbol = _config.GetRandomSymbol();
                    symbol.Setup(randomSymbol);
                }

                _symbols.Add(symbol);
            }
        }

        // 현재 보이는 3개 심볼의 데이터 반환 (상단, 중앙, 하단)
        public SymbolData[] GetVisibleSymbols()
        {
            if (_resultSymbols != null)
                return _resultSymbols;

            SymbolData[] visible = new SymbolData[3];
            // 위치 기준으로 정렬하여 반환
            List<Symbol> sorted = new List<Symbol>(_symbols);
            sorted.Sort((a, b) => b.GetPositionY().CompareTo(a.GetPositionY()));

            for (int i = 0; i < 3 && i < sorted.Count; i++)
            {
                visible[i] = sorted[i].Data;
            }

            return visible;
        }
    }
}
