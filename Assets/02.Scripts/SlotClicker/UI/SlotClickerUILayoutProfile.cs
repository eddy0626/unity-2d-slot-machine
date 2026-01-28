using UnityEngine;

namespace SlotClicker.UI
{
    [CreateAssetMenu(menuName = "SlotClicker/UI/Layout Profile", fileName = "SlotClickerUILayoutProfile")]
    public class SlotClickerUILayoutProfile : ScriptableObject
    {
        [Header("Button Label")]
        public float DefaultButtonLabelFont = 12.81f;

        [Header("Bet Panel")]
        public float BetPanelHeight = 104.09f;
        public float BetPanelYOffset = 52.045f;
        public float BetButtonWidth = 72.062f;
        public float BetButtonHeight = 28.825f;
        public float BetButtonSpacing = 7.687f;
        public float BetButtonY = 14.012f;
        public float BetButtonFont = 15.372f;

        [Header("Spin/Auto Buttons")]
        public float SpinButtonWidth = 124.908f;
        public float AutoButtonWidth = 76.867f;
        public float SpinAutoHeight = 38.434f;
        public float SpinAutoGap = 8.007f;
        public float SpinAutoY = 21.201f;
        public float SpinButtonFont = 20.178f;
        public float AutoButtonFont = 15.372f;

        [Header("Top Buttons")]
        public float UpgradeButtonWidth = 67.259f;
        public float UpgradeButtonHeight = 21.618f;
        public float PrestigeButtonWidth = 67.259f;
        public float PrestigeButtonHeight = 21.618f;

        [Header("Help Buttons")]
        public float HelpButtonSize = 31.228f;
        public float HelpButtonFont = 19.217f;
        public float HelpCloseSize = 24.02f;
        public float HelpCloseFont = 13.452f;

        private static SlotClickerUILayoutProfile _default;

        public static SlotClickerUILayoutProfile Default
        {
            get
            {
                if (_default == null)
                {
                    _default = CreateInstance<SlotClickerUILayoutProfile>();
                    _default.name = "SlotClickerUILayoutProfile(Default)";
                    _default.hideFlags = HideFlags.HideAndDontSave;
                }
                return _default;
            }
        }
    }
}
