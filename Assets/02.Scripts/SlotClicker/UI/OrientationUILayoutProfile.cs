using UnityEngine;

namespace SlotClicker.UI
{
    [CreateAssetMenu(menuName = "SlotClicker/UI/Orientation Layout Profile", fileName = "OrientationUILayoutProfile")]
    public class OrientationUILayoutProfile : ScriptableObject
    {
        [Header("Panel")]
        public float PanelWidth = 192.167f;
        public float PanelHeight = 240.209f;
        public float PanelOutline = 0.801f;

        [Header("Typography")]
        public float TitleFont = 12.811f;
        public float LabelFont = 8.007f;

        [Header("Spacing")]
        public float StartY = 96.083f;
        public float StepLarge = 28.825f;
        public float StepMedium = 24.02f;
        public float StepSmall = 19.217f;
        public float StepTiny = 14.412f;
        public float ButtonColumnX = 48.042f;

        [Header("Label")]
        public float LabelWidth = 168.146f;
        public float LabelHeight = 19.217f;

        [Header("Button")]
        public float ButtonWidth = 76.867f;
        public float ButtonHeight = 19.217f;
        public float ButtonFont = 8.647f;

        [Header("Toggle")]
        public float ToggleWidth = 144.125f;
        public float ToggleHeight = 19.217f;
        public float ToggleBoxOffsetX = 9.608f;
        public float ToggleBoxSize = 14.412f;
        public float ToggleCheckInset = 2.402f;
        public float ToggleLabelOffsetX = 28.825f;
        public float ToggleLabelFont = 10.569f;

        [Header("Slider")]
        public float SliderWidth = 144.125f;
        public float SliderHeight = 9.608f;
        public float SliderInset = 2.402f;
        public float SliderHandleInset = 4.804f;
        public float SliderHandleWidth = 9.608f;

        private static OrientationUILayoutProfile _default;

        public static OrientationUILayoutProfile Default
        {
            get
            {
                if (_default == null)
                {
                    _default = CreateInstance<OrientationUILayoutProfile>();
                    _default.name = "OrientationUILayoutProfile(Default)";
                    _default.hideFlags = HideFlags.HideAndDontSave;
                }
                return _default;
            }
        }
    }
}
