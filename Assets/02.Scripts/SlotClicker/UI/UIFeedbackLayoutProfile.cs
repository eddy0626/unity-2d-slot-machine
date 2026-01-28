using UnityEngine;

namespace SlotClicker.UI
{
    [CreateAssetMenu(menuName = "SlotClicker/UI/Feedback Layout Profile", fileName = "UIFeedbackLayoutProfile")]
    public class UIFeedbackLayoutProfile : ScriptableObject
    {
        [Header("Confirm Buttons")]
        public float ConfirmButtonHeight = 26.423f;
        public float ConfirmButtonFont = 13.452f;

        private static UIFeedbackLayoutProfile _default;

        public static UIFeedbackLayoutProfile Default
        {
            get
            {
                if (_default == null)
                {
                    _default = CreateInstance<UIFeedbackLayoutProfile>();
                    _default.name = "UIFeedbackLayoutProfile(Default)";
                    _default.hideFlags = HideFlags.HideAndDontSave;
                }
                return _default;
            }
        }
    }
}
