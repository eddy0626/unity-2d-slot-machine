using System;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Data;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Spin Profile (Optional)")]
        [SerializeField] private SlotSpinProfile spinProfile;

        [Header("Fallback Spin Tuning")]
        [Tooltip("최고 속도 (칸/초). SpinProfile이 없을 때 사용")]
        [SerializeField] private float maxSpeed = 18f;
        [Tooltip("가속 시간 (초). SpinProfile이 없을 때 사용")]
        [SerializeField] private float accelTime = 0.16f;
        [Tooltip("고속 유지 시간 범위 (초). SpinProfile이 없을 때 사용")]
        [SerializeField] private Vector2 steadyTimeRange = new Vector2(1.2f, 1.8f);
        [Tooltip("감속 커브. SpinProfile이 없을 때 사용")]
        [SerializeField] private AnimationCurve decelCurve = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -1.1f),
            new Keyframe(1f, 0f, -0.2f, 0f)
        );
        [Tooltip("마지막 틱틱 구간 셀 수. SpinProfile이 없을 때 사용")]
        [SerializeField] private int tickZoneCells = 8;
        [Tooltip("틱 간격 배율 커브. SpinProfile이 없을 때 사용")]
        [SerializeField] private AnimationCurve tickStepCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f),
            new Keyframe(1f, 1.9f)
        );
        [Tooltip("틱 기본 간격(초). SpinProfile이 없을 때 사용")]
        [SerializeField] private float tickInterval = 0.055f;
        [Tooltip("정지 직전 오버슈트 양(칸 단위). SpinProfile이 없을 때 사용")]
        [SerializeField] private float overshootAmount = 0.25f;
        [Range(0f, 0.2f)]
        [Tooltip("고속 유지 중 속도 요동 강도. SpinProfile이 없을 때 사용")]
        [SerializeField] private float steadySpeedJitter = 0.06f;
        [Tooltip("속도 요동 주파수. SpinProfile이 없을 때 사용")]
        [SerializeField] private float jitterFrequency = 8f;
        [Tooltip("스톱 요청 이후 최소 이동 셀 수. SpinProfile이 없을 때 사용")]
        [SerializeField] private int minStopCells = 18;
        [Tooltip("스톱 이동 셀 수에 더해지는 랜덤 추가치. SpinProfile이 없을 때 사용")]
        [SerializeField] private int stopCellsJitter = 6;
        [Tooltip("틱 구간 전에 확보할 여유 셀 수. SpinProfile이 없을 때 사용")]
        [SerializeField] private int preTickBufferCells = 4;

        private readonly List<Symbol> _symbols = new List<Symbol>();
        private readonly Dictionary<int, SymbolData> _randomSymbolsByIndex = new Dictionary<int, SymbolData>();
        private readonly Dictionary<int, SymbolData> _forcedSymbolsByIndex = new Dictionary<int, SymbolData>();

        private SlotMachineConfig _config;
        private Coroutine _spinRoutine;

        private bool _isSpinning;
        private bool _stopRequested;
        private bool _stopPlanned;

        // 스크롤 위치를 "칸 단위"로 관리해 오차 누적을 줄인다.
        private float _scrollPositionCells;
        private int _lastBaseCellIndex;

        // 심볼 배치(센터 기준 셀 오프셋)
        private int[] _symbolOffsets;
        private SymbolData[] _lastAssignedData;
        private int _visibleHalf;
        private int _bufferPerSide;
        private int _topOffsetCells;

        // 결과 심볼들 (상단, 중앙, 하단)
        private SymbolData[] _resultSymbols;

        // 스핀 감각 파라미터(프로파일 또는 폴백에서 해석됨)
        private SpinParameters _spinParams;

        // 스핀 전반 상태
        private float _spinElapsed;
        private float _minSpinTime;
        private float _currentSpeedCellsPerSec;

        // 요동 노이즈 상태
        private float _jitterSeed;
        private float _jitterTime;

        // 스톱 플랜 상태
        private float _stopAlignCells;
        private int _stopWholeCellsTotal;
        private float _stopTotalCells;
        private float _stopTravelledCells;
        private int _stopCellIndex;
        private int _stopDecelCells;
        private float _minDecelSpeed;

        // 틱틱 스톱 상태
        private bool _tickPhaseActive;
        private int _tickCellsRemaining;
        private int _tickCellsTotal;
        private float _tickTimer;

        public event Action OnSpinStart;
        public event Action<int> OnTick;          // cellPassed (절대 인덱스)
        public event Action<int> OnReelStop;      // reelIndex

        public int ReelIndex => reelIndex;
        public bool IsSpinning => _isSpinning;
        public SymbolData[] ResultSymbols => _resultSymbols;

        private struct SpinParameters
        {
            public float maxSpeed;
            public float accelTime;
            public Vector2 steadyTimeRange;
            public AnimationCurve decelCurve;
            public int tickZoneCells;
            public AnimationCurve tickStepCurve;
            public float tickInterval;
            public float overshootAmount;
            public float steadySpeedJitter;
            public float jitterFrequency;
            public int minStopCells;
            public int stopCellsJitter;
            public int preTickBufferCells;
        }

        public void Initialize(SlotMachineConfig config, int index, SlotSpinProfile profileOverride = null)
        {
            _config = config;
            reelIndex = index;

            if (profileOverride != null)
            {
                spinProfile = profileOverride;
            }

            if (_config != null)
            {
                symbolHeight = _config.symbolHeight;

                // 기존 config.spinSpeed(픽셀/초)를 최대한 존중해 폴백 maxSpeed를 보정한다.
                if (_config.spinSpeed > 0f && symbolHeight > 0f)
                {
                    maxSpeed = _config.spinSpeed / symbolHeight;
                }
            }

            // symbolContainer 자동 탐색
            if (symbolContainer == null)
            {
                symbolContainer = transform.Find("SymbolContainer") as RectTransform;
                if (symbolContainer == null)
                {
                    symbolContainer = GetComponent<RectTransform>();
                }
            }

            _spinParams = ResolveSpinParameters();

            CreateSymbols();
            SnapScrollToNearestCell();
            ApplyScrollPosition();
        }

        public void StartSpin()
        {
            StartSpin(spinProfile);
        }

        public void StartSpin(SlotSpinProfile profileOverride)
        {
            if (_isSpinning)
                return;

            if (profileOverride != null)
            {
                spinProfile = profileOverride;
            }

            _spinParams = ResolveSpinParameters();

            ResetSpinState();

            if (_spinRoutine != null)
            {
                StopCoroutine(_spinRoutine);
            }

            _spinRoutine = StartCoroutine(SpinRoutine());
        }

        public void StopSpin(SymbolData[] targetSymbols)
        {
            _resultSymbols = targetSymbols;

            if (!_isSpinning)
            {
                // 스핀 중이 아니라면 즉시 결과에 스냅한다.
                SnapScrollToNearestCell();
                int baseIndex = Mathf.FloorToInt(_scrollPositionCells);
                ForceResultSymbols(baseIndex, targetSymbols);
                ApplyScrollPosition();
                OnReelStop?.Invoke(reelIndex);
                return;
            }

            _stopRequested = true;
        }

        private IEnumerator SpinRoutine()
        {
            _isSpinning = true;
            OnSpinStart?.Invoke();

            // 모터 붙는 느낌을 위해 최소 스핀 시간 확보
            float steadyDuration = UnityEngine.Random.Range(_spinParams.steadyTimeRange.x, _spinParams.steadyTimeRange.y);
            _minSpinTime = Mathf.Max(0.05f, _spinParams.accelTime + steadyDuration);

            while (true)
            {
                float dt = Time.deltaTime;
                _spinElapsed += dt;

                // stop 요청이 들어와도 최소 시간은 채운다.
                bool canPlanStop = _stopRequested && _spinElapsed >= _minSpinTime;
                if (canPlanStop)
                {
                    break;
                }

                _currentSpeedCellsPerSec = ComputeFreeSpinSpeed(dt);
                AdvanceFree(_currentSpeedCellsPerSec * dt);

                yield return null;
            }

            PlanStopIfNeeded();

            while (_stopTravelledCells < _stopTotalCells - 0.0001f)
            {
                UpdateStopping(Time.deltaTime);
                yield return null;
            }

            // 마지막 오버슈트/정렬 연출
            yield return OvershootAndSnapRoutine();

            FinalizeStop();
        }

        private void ResetSpinState()
        {
            _stopRequested = false;
            _stopPlanned = false;
            _tickPhaseActive = false;

            _spinElapsed = 0f;
            _currentSpeedCellsPerSec = 0f;

            _stopAlignCells = 0f;
            _stopWholeCellsTotal = 0;
            _stopTotalCells = 0f;
            _stopTravelledCells = 0f;
            _stopCellIndex = 0;
            _stopDecelCells = 0;
            _minDecelSpeed = Mathf.Max(2f, _spinParams.maxSpeed * 0.12f);

            _tickCellsRemaining = 0;
            _tickCellsTotal = 0;
            _tickTimer = 0f;

            _randomSymbolsByIndex.Clear();
            _forcedSymbolsByIndex.Clear();

            _jitterSeed = UnityEngine.Random.Range(0f, 1000f);
            _jitterTime = 0f;

            // 스핀 시작 시에도 그리드 정렬을 다시 맞춰 누적 오차를 방지한다.
            SnapScrollToNearestCell();
            ApplyScrollPosition();
        }

        private SpinParameters ResolveSpinParameters()
        {
            if (spinProfile != null)
            {
                return new SpinParameters
                {
                    maxSpeed = Mathf.Max(1f, spinProfile.maxSpeed),
                    accelTime = Mathf.Max(0.01f, spinProfile.accelTime),
                    steadyTimeRange = new Vector2(
                        Mathf.Max(0.05f, spinProfile.steadyTimeRange.x),
                        Mathf.Max(spinProfile.steadyTimeRange.x, spinProfile.steadyTimeRange.y)
                    ),
                    decelCurve = spinProfile.decelCurve != null ? spinProfile.decelCurve : decelCurve,
                    tickZoneCells = Mathf.Max(3, spinProfile.tickZoneCells),
                    tickStepCurve = spinProfile.tickStepCurve != null ? spinProfile.tickStepCurve : tickStepCurve,
                    tickInterval = Mathf.Max(0.01f, spinProfile.tickInterval),
                    overshootAmount = Mathf.Clamp(spinProfile.overshootAmount, 0f, 0.95f),
                    steadySpeedJitter = Mathf.Clamp01(spinProfile.steadySpeedJitter),
                    jitterFrequency = Mathf.Max(0.1f, spinProfile.jitterFrequency),
                    minStopCells = Mathf.Max(spinProfile.tickZoneCells + 6, spinProfile.minStopCells),
                    stopCellsJitter = Mathf.Max(0, spinProfile.stopCellsJitter),
                    preTickBufferCells = Mathf.Max(2, spinProfile.preTickBufferCells)
                };
            }

            return new SpinParameters
            {
                maxSpeed = Mathf.Max(1f, maxSpeed),
                accelTime = Mathf.Max(0.01f, accelTime),
                steadyTimeRange = new Vector2(
                    Mathf.Max(0.05f, steadyTimeRange.x),
                    Mathf.Max(steadyTimeRange.x, steadyTimeRange.y)
                ),
                decelCurve = decelCurve,
                tickZoneCells = Mathf.Max(3, tickZoneCells),
                tickStepCurve = tickStepCurve,
                tickInterval = Mathf.Max(0.01f, tickInterval),
                overshootAmount = Mathf.Clamp(overshootAmount, 0f, 0.95f),
                steadySpeedJitter = Mathf.Clamp01(steadySpeedJitter),
                jitterFrequency = Mathf.Max(0.1f, jitterFrequency),
                minStopCells = Mathf.Max(tickZoneCells + 6, minStopCells),
                stopCellsJitter = Mathf.Max(0, stopCellsJitter),
                preTickBufferCells = Mathf.Max(2, preTickBufferCells)
            };
        }

        private void CreateSymbols()
        {
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

            foreach (Transform child in symbolContainer)
            {
                Destroy(child.gameObject);
            }
            _symbols.Clear();

            SetupLayout();

            for (int i = 0; i < _symbolOffsets.Length; i++)
            {
                GameObject symbolObj = Instantiate(symbolPrefab, symbolContainer);
                Symbol symbol = symbolObj.GetComponent<Symbol>();

                if (symbol == null)
                    symbol = symbolObj.AddComponent<Symbol>();

                RectTransform rt = symbolObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, _symbolOffsets[i] * symbolHeight);
                    rt.sizeDelta = new Vector2(symbolHeight, symbolHeight);
                }

                _symbols.Add(symbol);
            }

            _lastAssignedData = new SymbolData[_symbols.Count];
        }

        private void SetupLayout()
        {
            _visibleHalf = Mathf.Max(1, (visibleSymbols - 1) / 2);
            _bufferPerSide = Mathf.Max(1, bufferSymbols);
            _topOffsetCells = _visibleHalf + _bufferPerSide;

            int totalSymbols = visibleSymbols + (_bufferPerSide * 2);
            _symbolOffsets = new int[totalSymbols];

            for (int i = 0; i < totalSymbols; i++)
            {
                _symbolOffsets[i] = _topOffsetCells - i;
            }
        }

        private float ComputeFreeSpinSpeed(float deltaTime)
        {
            if (_spinElapsed < _spinParams.accelTime)
            {
                float t = Mathf.Clamp01(_spinElapsed / _spinParams.accelTime);

                // 살짝 늦게 최고속에 도달하도록 SmoothStep으로 모터 느낌을 준다.
                float motorT = Mathf.SmoothStep(0f, 1f, t);
                float startSpeed = _spinParams.maxSpeed * 0.35f;
                return Mathf.Lerp(startSpeed, _spinParams.maxSpeed, motorT);
            }

            return GetJitteredMaxSpeed(deltaTime);
        }

        private float GetJitteredMaxSpeed(float deltaTime)
        {
            if (_spinParams.steadySpeedJitter <= 0.0001f)
                return _spinParams.maxSpeed;

            _jitterTime += deltaTime * _spinParams.jitterFrequency;
            float noise = Mathf.PerlinNoise(_jitterSeed, _jitterTime) * 2f - 1f;
            float jitterFactor = 1f + (noise * _spinParams.steadySpeedJitter);

            return Mathf.Max(0.1f, _spinParams.maxSpeed * jitterFactor);
        }

        private void PlanStopIfNeeded()
        {
            if (_stopPlanned)
                return;

            // stop 호출이 없었는데 루틴이 여기 왔다면, 안전하게 현재 결과를 유지한다.
            if (_resultSymbols == null || _resultSymbols.Length < 3)
            {
                _resultSymbols = GetVisibleSymbols();
            }

            float offset = Mathf.Repeat(_scrollPositionCells, 1f);
            _stopAlignCells = offset > 0.0001f ? (1f - offset) : 0f;

            int baseStopCells = Mathf.Max(
                _spinParams.minStopCells,
                _spinParams.tickZoneCells + _spinParams.preTickBufferCells
            );
            int extraStopCells = _spinParams.stopCellsJitter > 0
                ? UnityEngine.Random.Range(0, _spinParams.stopCellsJitter + 1)
                : 0;

            _stopWholeCellsTotal = baseStopCells + extraStopCells;
            _stopTotalCells = _stopAlignCells + _stopWholeCellsTotal;
            _stopDecelCells = Mathf.Max(1, _stopWholeCellsTotal - _spinParams.tickZoneCells);

            // 최종 정지 인덱스(센터 라인 기준). 반드시 정수 칸에 스냅한다.
            _stopCellIndex = Mathf.RoundToInt(_scrollPositionCells + _stopTotalCells);

            ForceResultSymbols(_stopCellIndex, _resultSymbols);

            _stopTravelledCells = 0f;
            _tickPhaseActive = false;
            _tickCellsRemaining = 0;
            _tickCellsTotal = 0;
            _tickTimer = 0f;

            _stopPlanned = true;
        }

        private void UpdateStopping(float deltaTime)
        {
            if (!_stopPlanned)
                return;

            float remainingCells = _stopTotalCells - _stopTravelledCells;
            if (remainingCells <= 0.0001f)
                return;

            // 1) 현재 셀 경계까지 정렬 (누적 오차 방지)
            if (_stopTravelledCells < _stopAlignCells - 0.0001f)
            {
                float alignSpeed = Mathf.Max(_minDecelSpeed, GetJitteredMaxSpeed(deltaTime));
                AdvanceStopping(alignSpeed * deltaTime);
                return;
            }

            float wholeTravelled = _stopTravelledCells - _stopAlignCells;
            float wholeRemaining = _stopWholeCellsTotal - wholeTravelled;

            float cellOffset = Mathf.Repeat(_scrollPositionCells, 1f);

            // 2) 감속 구간
            if (!_tickPhaseActive)
            {
                bool readyForTickZone = wholeTravelled >= _stopDecelCells && cellOffset <= 0.02f;

                if (!readyForTickZone)
                {
                    float decelT = Mathf.Clamp01(wholeTravelled / Mathf.Max(1f, _stopDecelCells));
                    float speedFactor = _spinParams.decelCurve != null
                        ? Mathf.Clamp01(_spinParams.decelCurve.Evaluate(decelT))
                        : Mathf.Clamp01(1f - decelT);

                    float speed = Mathf.Max(_minDecelSpeed, _spinParams.maxSpeed * speedFactor);
                    AdvanceStopping(speed * deltaTime);
                    return;
                }

                BeginTickPhase(wholeRemaining);
            }

            // 3) 틱틱 구간 (계단식 감속)
            UpdateTickPhase(deltaTime);
        }

        private void BeginTickPhase(float wholeRemaining)
        {
            _tickPhaseActive = true;

            int remainingCells = Mathf.Max(1, Mathf.CeilToInt(wholeRemaining - 0.0001f));
            _tickCellsRemaining = remainingCells;
            _tickCellsTotal = remainingCells;
            _tickTimer = 0f;
        }

        private void UpdateTickPhase(float deltaTime)
        {
            if (_tickCellsRemaining <= 0)
                return;

            _tickTimer += deltaTime;

            // 프레임 드랍 시에도 여러 틱을 처리할 수 있도록 while 루프 사용
            int safety = 0;
            while (_tickCellsRemaining > 0 && safety < 8)
            {
                safety++;

                float progress = 1f - (_tickCellsRemaining / (float)Mathf.Max(1, _tickCellsTotal));
                float intervalMultiplier = _spinParams.tickStepCurve != null
                    ? Mathf.Max(0.15f, _spinParams.tickStepCurve.Evaluate(progress))
                    : 1f;

                float interval = Mathf.Max(0.01f, _spinParams.tickInterval * intervalMultiplier);
                if (_tickTimer < interval)
                    break;

                _tickTimer -= interval;

                // 한 틱에 한 칸씩 진행하되, 남은 거리로 클램프한다.
                AdvanceStopping(1f);
                _tickCellsRemaining--;

                if (_stopTravelledCells >= _stopTotalCells - 0.0001f)
                    break;
            }
        }

        private void AdvanceFree(float deltaCells)
        {
            if (deltaCells <= 0f)
                return;

            _scrollPositionCells += deltaCells;
            ApplyScrollPosition();
        }

        private void AdvanceStopping(float deltaCells)
        {
            if (deltaCells <= 0f)
                return;

            float remainingCells = _stopTotalCells - _stopTravelledCells;
            float clampedDelta = Mathf.Min(deltaCells, remainingCells);

            _scrollPositionCells += clampedDelta;
            _stopTravelledCells += clampedDelta;

            ApplyScrollPosition();
        }

        private void ApplyScrollPosition()
        {
            if (_symbols.Count == 0 || symbolHeight <= 0f)
                return;

            int baseCellIndex = Mathf.FloorToInt(_scrollPositionCells);
            float cellOffset = _scrollPositionCells - baseCellIndex;

            for (int i = 0; i < _symbols.Count; i++)
            {
                int offsetCells = _symbolOffsets[i];
                float y = (offsetCells - cellOffset) * symbolHeight;
                _symbols[i].SetPosition(y);

                int symbolIndex = baseCellIndex + offsetCells;
                SymbolData data = GetSymbolForIndex(symbolIndex);

                if (_lastAssignedData == null || i >= _lastAssignedData.Length)
                    continue;

                if (_lastAssignedData[i] != data)
                {
                    _symbols[i].Setup(data);
                    _lastAssignedData[i] = data;
                }
            }

            EmitTickEvents(baseCellIndex);
        }

        private void EmitTickEvents(int baseCellIndex)
        {
            if (baseCellIndex <= _lastBaseCellIndex)
            {
                _lastBaseCellIndex = baseCellIndex;
                return;
            }

            int delta = baseCellIndex - _lastBaseCellIndex;
            for (int i = 1; i <= delta; i++)
            {
                int passedCell = _lastBaseCellIndex + i;
                OnTick?.Invoke(passedCell);
            }

            _lastBaseCellIndex = baseCellIndex;
        }

        private SymbolData GetSymbolForIndex(int index)
        {
            if (_forcedSymbolsByIndex.TryGetValue(index, out SymbolData forced))
                return forced;

            if (_randomSymbolsByIndex.TryGetValue(index, out SymbolData cached))
                return cached;

            SymbolData random = _config != null ? _config.GetRandomSymbol() : null;
            _randomSymbolsByIndex[index] = random;
            return random;
        }

        private void ForceResultSymbols(int centerIndex, SymbolData[] resultSymbols)
        {
            if (resultSymbols == null || resultSymbols.Length < 3)
                return;

            _forcedSymbolsByIndex.Clear();

            // 현재 매핑에서 top은 +1, bottom은 -1 오프셋을 사용한다.
            _forcedSymbolsByIndex[centerIndex + 1] = resultSymbols[0]; // top
            _forcedSymbolsByIndex[centerIndex] = resultSymbols[1];     // center
            _forcedSymbolsByIndex[centerIndex - 1] = resultSymbols[2]; // bottom
        }

        private IEnumerator OvershootAndSnapRoutine()
        {
            float overshoot = Mathf.Clamp(_spinParams.overshootAmount, 0f, 0.49f);

            if (overshoot <= 0.0001f)
            {
                SnapToStopIndex();
                yield break;
            }

            float overshootDuration = 0.08f;
            float snapDuration = 0.12f;

            float start = _scrollPositionCells;
            float overshootTarget = _stopCellIndex + overshoot;

            float t = 0f;
            while (t < overshootDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / overshootDuration);
                _scrollPositionCells = Mathf.Lerp(start, overshootTarget, EaseOutCubic(p));
                ApplyScrollPosition();
                yield return null;
            }

            t = 0f;
            while (t < snapDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / snapDuration);
                _scrollPositionCells = Mathf.Lerp(overshootTarget, _stopCellIndex, EaseOutBack(p));
                ApplyScrollPosition();
                yield return null;
            }

            SnapToStopIndex();
        }

        private void SnapScrollToNearestCell()
        {
            _scrollPositionCells = Mathf.Round(_scrollPositionCells);
            _lastBaseCellIndex = Mathf.FloorToInt(_scrollPositionCells);
        }

        private void SnapToStopIndex()
        {
            _scrollPositionCells = _stopCellIndex;
            _stopTravelledCells = _stopTotalCells;
            _lastBaseCellIndex = Mathf.FloorToInt(_scrollPositionCells);
            ApplyScrollPosition();
        }

        private void FinalizeStop()
        {
            // 결과를 최종 스냅 인덱스 기준으로 다시 강제하고 한 번 더 정렬한다.
            ForceResultSymbols(_stopCellIndex, _resultSymbols);
            SnapToStopIndex();

            _isSpinning = false;
            _spinRoutine = null;

            OnReelStop?.Invoke(reelIndex);
        }

        private static float EaseOutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - (inv * inv * inv);
        }

        private static float EaseOutBack(float t)
        {
            const float s = 1.70158f;
            t -= 1f;
            return 1f + ((s + 1f) * t * t * t) + (s * t * t);
        }

        /// <summary>
        /// 프리팹 없이 동적으로 심볼 생성
        /// </summary>
        private void CreateSymbolsWithoutPrefab()
        {
            foreach (Transform child in symbolContainer)
            {
                Destroy(child.gameObject);
            }
            _symbols.Clear();

            SetupLayout();

            for (int i = 0; i < _symbolOffsets.Length; i++)
            {
                GameObject symbolObj = new GameObject($"Symbol_{i}");
                symbolObj.transform.SetParent(symbolContainer, false);

                RectTransform rt = symbolObj.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, _symbolOffsets[i] * symbolHeight);
                rt.sizeDelta = new Vector2(symbolHeight - 10f, symbolHeight - 10f);

                Image img = symbolObj.AddComponent<Image>();
                img.color = Color.white;

                Symbol symbol = symbolObj.AddComponent<Symbol>();
                _symbols.Add(symbol);
            }

            _lastAssignedData = new SymbolData[_symbols.Count];
        }

        // 현재 보이는 3개 심볼의 데이터 반환 (상단, 중앙, 하단)
        public SymbolData[] GetVisibleSymbols()
        {
            SymbolData[] visible = new SymbolData[3];

            if (_symbols.Count == 0)
                return visible;

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
