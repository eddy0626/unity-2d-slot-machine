using UnityEngine;

namespace SlotMachine.Data
{
    [CreateAssetMenu(fileName = "PaylineData", menuName = "SlotMachine/Payline Data")]
    public class PaylineData : ScriptableObject
    {
        [Header("Payline Settings")]
        public string paylineName;
        public Color lineColor = Color.yellow;

        [Header("Position Indices (0-8 for 3x3 grid)")]
        [Tooltip("3x3 그리드에서 페이라인 위치\n0 1 2\n3 4 5\n6 7 8")]
        public int[] positions = new int[3];

        // 편의를 위한 미리 정의된 페이라인
        public static readonly int[][] DefaultPaylines = new int[][]
        {
            new int[] { 3, 4, 5 },  // 라인1: 중앙 가로
            new int[] { 0, 1, 2 },  // 라인2: 상단 가로
            new int[] { 6, 7, 8 },  // 라인3: 하단 가로
            new int[] { 0, 4, 8 },  // 라인4: 대각선 ↘
            new int[] { 6, 4, 2 }   // 라인5: 대각선 ↗
        };

        public static readonly string[] PaylineNames = new string[]
        {
            "Center",
            "Top",
            "Bottom",
            "Diagonal Down",
            "Diagonal Up"
        };

        public static readonly Color[] PaylineColors = new Color[]
        {
            new Color(1f, 0.8f, 0f),      // Gold
            new Color(0f, 1f, 0.5f),      // Cyan-Green
            new Color(1f, 0.2f, 0.5f),    // Pink
            new Color(0.5f, 0.2f, 1f),    // Purple
            new Color(0f, 0.8f, 1f)       // Light Blue
        };
    }
}
