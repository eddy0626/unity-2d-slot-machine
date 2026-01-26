using UnityEngine;

namespace SlotMachine.Data
{
    public enum SymbolType
    {
        Normal,
        Wild,
        Scatter,
        Bonus
    }

    [CreateAssetMenu(fileName = "SymbolData", menuName = "SlotMachine/Symbol Data")]
    public class SymbolData : ScriptableObject
    {
        [Header("Basic Info")]
        public string symbolName;
        public int symbolId;
        public Sprite sprite;
        public SymbolType symbolType = SymbolType.Normal;

        [Header("Payout Settings")]
        [Tooltip("배율 (3개 일치 시)")]
        public int payoutMultiplier = 1;

        [Header("Rarity")]
        [Range(1, 100)]
        [Tooltip("출현 가중치 (높을수록 자주 등장)")]
        public int weight = 10;

        [Header("Visual")]
        public Color glowColor = Color.yellow;
    }
}
