#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SlotClicker.Data;

namespace SlotClicker.Editor
{
    /// <summary>
    /// 스핀 프로파일 프리셋 생성 에디터 유틸리티
    /// </summary>
    public class SpinProfilePresetCreator
    {
        private const string PROFILE_PATH = "Assets/Resources/SpinProfiles";

        [MenuItem("SlotClicker/Create Spin Profiles/All Presets", priority = 100)]
        public static void CreateAllPresets()
        {
            EnsureFolderExists();
            CreateDefaultProfile();
            CreateFastProfile();
            CreateClassicProfile();
            CreateDramaticProfile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpinProfilePresetCreator] All preset profiles created in " + PROFILE_PATH);
        }

        [MenuItem("SlotClicker/Create Spin Profiles/Default Profile")]
        public static void CreateDefaultProfile()
        {
            EnsureFolderExists();
            var profile = ScriptableObject.CreateInstance<SlotClickerSpinProfile>();
            // 기본값 사용 (SlotClickerSpinProfile 초기값 그대로)
            SaveProfile(profile, "SpinProfile_Default");
        }

        [MenuItem("SlotClicker/Create Spin Profiles/Fast Profile")]
        public static void CreateFastProfile()
        {
            EnsureFolderExists();
            var profile = ScriptableObject.CreateInstance<SlotClickerSpinProfile>();

            // 빠른 스핀 프리셋
            profile.accelDuration = 0.2f;
            profile.accelStartSpeed = 0.1f;
            profile.maxSpeed = 0.02f;
            profile.minSteadyDuration = 0.5f;
            profile.decelerationSteps = 2;
            profile.decelerationCurve = new AnimationCurve(
                new Keyframe(0f, 0.04f),
                new Keyframe(1f, 0.08f)
            );
            profile.columnStopDelays = new float[] { 0f, 0.05f, 0.1f };
            profile.bounceIntensity = 0.08f;
            profile.bounceDuration = 0.2f;
            profile.bounceVibrato = 3;
            profile.bounceElasticity = 0.5f;

            SaveProfile(profile, "SpinProfile_Fast");
        }

        [MenuItem("SlotClicker/Create Spin Profiles/Classic Profile")]
        public static void CreateClassicProfile()
        {
            EnsureFolderExists();
            var profile = ScriptableObject.CreateInstance<SlotClickerSpinProfile>();

            // 클래식 슬롯머신 느낌
            profile.accelDuration = 0.4f;
            profile.accelStartSpeed = 0.2f;
            profile.maxSpeed = 0.04f;
            profile.minSteadyDuration = 1.2f;
            profile.decelerationSteps = 4;
            profile.decelerationCurve = new AnimationCurve(
                new Keyframe(0f, 0.06f),
                new Keyframe(0.33f, 0.09f),
                new Keyframe(0.66f, 0.13f),
                new Keyframe(1f, 0.18f)
            );
            profile.columnStopDelays = new float[] { 0f, 0.15f, 0.3f };
            profile.bounceIntensity = 0.15f;
            profile.bounceDuration = 0.35f;
            profile.bounceVibrato = 5;
            profile.bounceElasticity = 0.65f;
            profile.slideDistance = 18f;

            SaveProfile(profile, "SpinProfile_Classic");
        }

        [MenuItem("SlotClicker/Create Spin Profiles/Dramatic Profile")]
        public static void CreateDramaticProfile()
        {
            EnsureFolderExists();
            var profile = ScriptableObject.CreateInstance<SlotClickerSpinProfile>();

            // 드라마틱한 연출
            profile.accelDuration = 0.35f;
            profile.accelStartSpeed = 0.18f;
            profile.maxSpeed = 0.025f;
            profile.minSteadyDuration = 1f;
            profile.decelerationSteps = 5;
            profile.decelerationCurve = new AnimationCurve(
                new Keyframe(0f, 0.05f),
                new Keyframe(0.25f, 0.07f),
                new Keyframe(0.5f, 0.1f),
                new Keyframe(0.75f, 0.14f),
                new Keyframe(1f, 0.2f)
            );
            profile.columnStopDelays = new float[] { 0f, 0.2f, 0.4f };
            profile.bounceIntensity = 0.2f;
            profile.bounceDuration = 0.4f;
            profile.bounceVibrato = 6;
            profile.bounceElasticity = 0.7f;
            profile.enableLandingFlash = true;
            profile.flashIntensity = 1.6f;
            profile.flashDuration = 0.12f;
            profile.slideDistance = 20f;
            profile.accelPunchScale = 0.08f;

            SaveProfile(profile, "SpinProfile_Dramatic");
        }

        [MenuItem("SlotClicker/Create Spin Profiles/Mobile Optimized Profile")]
        public static void CreateMobileProfile()
        {
            EnsureFolderExists();
            var profile = ScriptableObject.CreateInstance<SlotClickerSpinProfile>();

            // 모바일 최적화 (빠르고 가벼운 효과)
            profile.accelDuration = 0.25f;
            profile.accelStartSpeed = 0.12f;
            profile.maxSpeed = 0.025f;
            profile.minSteadyDuration = 0.6f;
            profile.decelerationSteps = 2;
            profile.decelerationCurve = new AnimationCurve(
                new Keyframe(0f, 0.05f),
                new Keyframe(1f, 0.1f)
            );
            profile.columnStopDelays = new float[] { 0f, 0.06f, 0.12f };
            profile.bounceIntensity = 0.1f;
            profile.bounceDuration = 0.25f;
            profile.bounceVibrato = 3;
            profile.bounceElasticity = 0.5f;
            profile.enableLandingFlash = true;
            profile.flashIntensity = 1.3f;
            profile.flashDuration = 0.08f;
            profile.slideDistance = 12f;
            profile.accelPunchScale = 0.05f;
            profile.spinBlurAlpha = 0.9f;

            SaveProfile(profile, "SpinProfile_Mobile");
        }

        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(PROFILE_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "SpinProfiles");
            }
        }

        private static void SaveProfile(SlotClickerSpinProfile profile, string name)
        {
            string path = $"{PROFILE_PATH}/{name}.asset";

            // 기존 에셋이 있으면 덮어쓰기
            var existing = AssetDatabase.LoadAssetAtPath<SlotClickerSpinProfile>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(profile, existing);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[SpinProfilePresetCreator] Updated: {path}");
            }
            else
            {
                AssetDatabase.CreateAsset(profile, path);
                Debug.Log($"[SpinProfilePresetCreator] Created: {path}");
            }
        }
    }
}
#endif
