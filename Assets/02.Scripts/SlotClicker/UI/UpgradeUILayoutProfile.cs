using UnityEngine;

namespace SlotClicker.UI
{
    [CreateAssetMenu(menuName = "SlotClicker/UI/Upgrade Layout Profile", fileName = "UpgradeUILayoutProfile")]
    public class UpgradeUILayoutProfile : ScriptableObject
    {
        [Header("Tabs")]
        public float TabHeight = 24f;
        public float TabSpacing = 5f;
        public int TabFontSize = 12;

        [Header("Content")]
        public float ContentSpacing = 4f;

        [Header("Close Button")]
        public float CloseButtonHeight = 24f;
        public int CloseButtonFontSize = 13;

        [Header("Upgrade Item")]
        public float ItemHeight = 48f;
        public int ItemPadding = 5;
        public float ItemSpacing = 5f;

        [Header("Buy Button")]
        public float BuyButtonWidth = 48f;
        public int BuyButtonFontSize = 11;

        private static UpgradeUILayoutProfile _default;

        public static UpgradeUILayoutProfile Default
        {
            get
            {
                if (_default == null)
                {
                    _default = CreateInstance<UpgradeUILayoutProfile>();
                    _default.name = "UpgradeUILayoutProfile(Default)";
                    _default.hideFlags = HideFlags.HideAndDontSave;
                }
                return _default;
            }
        }
    }
}
