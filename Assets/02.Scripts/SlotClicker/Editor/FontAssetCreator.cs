using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace SlotClicker.Editor
{
    /// <summary>
    /// NeoDunggeunmo 폰트 에셋 자동 생성 및 적용
    /// Unity Editor에서 Tools > SlotClicker > Create Font Asset 실행
    /// </summary>
    public class FontAssetCreator : EditorWindow
    {
        private const string FONT_PATH = "Assets/08.Fonts/NeoDunggeunmoPro-Regular.ttf";
        private const string OUTPUT_PATH = "Assets/08.Fonts/NeoDunggeunmoPro-Regular SDF.asset";
        private const string RESOURCES_PATH = "Assets/Resources/Fonts/NeoDunggeunmoPro-Regular SDF.asset";

        [MenuItem("Tools/SlotClicker/Create Font Asset")]
        public static void CreateFontAsset()
        {
            // 폰트 파일 로드
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FONT_PATH);
            if (sourceFont == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"폰트 파일을 찾을 수 없습니다: {FONT_PATH}", "OK");
                return;
            }

            // 기존 에셋 확인
            TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OUTPUT_PATH);
            if (existingAsset != null)
            {
                if (!EditorUtility.DisplayDialog("Confirm",
                    "폰트 에셋이 이미 존재합니다. 덮어쓰시겠습니까?", "Yes", "No"))
                {
                    return;
                }
                AssetDatabase.DeleteAsset(OUTPUT_PATH);
                AssetDatabase.DeleteAsset(RESOURCES_PATH);
            }

            // TMP Font Asset 생성 (아틀라스 포함)
            // 한글 전체를 위해 큰 아틀라스 사용 (4096x4096)
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                46,                          // 샘플링 포인트 크기 (작게 해서 더 많은 글자 수용)
                5,                           // 아틀라스 패딩
                GlyphRenderMode.SDFAA,       // SDF 안티앨리어싱 렌더 모드
                4096,                        // 아틀라스 너비
                4096                         // 아틀라스 높이
            );

            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "폰트 에셋 생성에 실패했습니다.\n\n" +
                    "Window > TextMeshPro > Font Asset Creator를\n" +
                    "사용하여 수동으로 생성해주세요.", "OK");
                return;
            }

            // 에셋 먼저 저장 (서브 에셋 추가 전에)
            AssetDatabase.CreateAsset(fontAsset, OUTPUT_PATH);

            // 기본 문자 세트 추가
            string characters = GetCharacterSet();

            // Dynamic 모드로 설정 (런타임에 필요한 글자 자동 추가)
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            // Multi Atlas Textures 활성화 (한글 전체를 위해 필요)
            fontAsset.isMultiAtlasTexturesEnabled = true;

            // 문자 추가 시도 (진행 표시)
            EditorUtility.DisplayProgressBar("Font Asset Creator", "Adding Korean characters...", 0.5f);

            bool success = fontAsset.TryAddCharacters(characters, out string missingChars);

            if (!success && !string.IsNullOrEmpty(missingChars))
            {
                Debug.LogWarning($"[FontAssetCreator] Some characters could not be added: {missingChars.Length} missing");
            }

            EditorUtility.DisplayProgressBar("Font Asset Creator", "Saving assets...", 0.8f);

            // 모든 아틀라스 텍스처 저장
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    if (fontAsset.atlasTextures[i] != null)
                    {
                        fontAsset.atlasTextures[i].name = $"{fontAsset.name} Atlas {i}";
                        if (!AssetDatabase.Contains(fontAsset.atlasTextures[i]))
                        {
                            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                        }
                    }
                }
            }

            // Material 저장
            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                if (!AssetDatabase.Contains(fontAsset.material))
                {
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();

            // Resources 폴더에도 복사 (런타임 로드용)
            EnsureResourcesFolder();
            AssetDatabase.CopyAsset(OUTPUT_PATH, RESOURCES_PATH);

            AssetDatabase.Refresh();

            int charCount = fontAsset.characterTable != null ? fontAsset.characterTable.Count : 0;
            int atlasCount = fontAsset.atlasTextures != null ? fontAsset.atlasTextures.Length : 0;

            EditorUtility.DisplayDialog("Success",
                $"폰트 에셋이 생성되었습니다:\n\n" +
                $"문자 수: {charCount}자\n" +
                $"아틀라스: {atlasCount}개\n\n" +
                $"• {OUTPUT_PATH}\n" +
                $"• {RESOURCES_PATH}\n\n" +
                "게임 실행 시 자동으로 적용됩니다.", "OK");

            // 생성된 에셋 선택
            Selection.activeObject = fontAsset;
        }

        [MenuItem("Tools/SlotClicker/Open TMP Font Asset Creator")]
        public static void OpenFontAssetCreator()
        {
            // TMP Font Asset Creator 창 열기
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Font Asset Creator");

            EditorUtility.DisplayDialog("Font Asset Creator",
                "Font Asset Creator가 열렸습니다.\n\n" +
                "1. Source Font File에 NeoDunggeunmoPro-Regular 선택\n" +
                "2. Atlas Resolution: 1024 x 1024\n" +
                "3. Character Set: Custom Characters\n" +
                "4. Custom Character List에 한글 입력\n" +
                "5. Generate Font Atlas 클릭\n" +
                "6. Save 클릭하여 저장", "OK");
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Fonts"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Fonts");
            }
        }

        private static string GetCharacterSet()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 기본 ASCII (공백~틸드)
            for (char c = ' '; c <= '~'; c++) sb.Append(c);

            // 한글 자모 (ㄱ-ㅎ, ㅏ-ㅣ)
            for (char c = '\u3131'; c <= '\u3163'; c++) sb.Append(c);

            // 한글 완성형 전체 (가-힣) - 11,172자
            for (char c = '\uAC00'; c <= '\uD7A3'; c++) sb.Append(c);

            Debug.Log($"[FontAssetCreator] Character set size: {sb.Length} characters");
            return sb.ToString();
        }

        [MenuItem("Tools/SlotClicker/Apply Font to All TMP")]
        public static void ApplyFontToAllTMP()
        {
            // 폰트 에셋 로드
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OUTPUT_PATH);
            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RESOURCES_PATH);
            }

            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("Error",
                    $"폰트 에셋을 찾을 수 없습니다.\n\n" +
                    "먼저 'Tools > SlotClicker > Create Font Asset'을 실행하거나\n" +
                    "'Tools > SlotClicker > Open TMP Font Asset Creator'를 사용하세요.", "OK");
                return;
            }

            // TMP Settings 업데이트
            UpdateTMPSettings(fontAsset);

            EditorUtility.DisplayDialog("Success",
                "기본 폰트가 설정되었습니다.\n\n" +
                "런타임에 생성되는 TMP 텍스트에도 자동 적용됩니다.", "OK");
        }

        private static void UpdateTMPSettings(TMP_FontAsset fontAsset)
        {
            // TMP Settings 에셋 로드
            string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);

            if (settings != null)
            {
                var so = new SerializedObject(settings);

                // 기본 폰트 설정
                var defaultFontProp = so.FindProperty("m_defaultFontAsset");
                if (defaultFontProp != null)
                {
                    defaultFontProp.objectReferenceValue = fontAsset;
                }

                // Fallback 폰트 설정 (LiberationSans SDF)
                var fallbackProp = so.FindProperty("m_fallbackFontAssets");
                if (fallbackProp != null)
                {
                    TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

                    if (fallbackFont != null)
                    {
                        fallbackProp.ClearArray();
                        fallbackProp.InsertArrayElementAtIndex(0);
                        fallbackProp.GetArrayElementAtIndex(0).objectReferenceValue = fallbackFont;
                        Debug.Log("[FontAssetCreator] Fallback font set: LiberationSans SDF");
                    }
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();

                Debug.Log("[FontAssetCreator] TMP Settings updated");
            }
            else
            {
                Debug.LogWarning("[FontAssetCreator] TMP Settings not found at: " + settingsPath);
            }
        }

        [MenuItem("Tools/SlotClicker/Add Missing Chars to Font")]
        public static void AddMissingCharsToFont()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OUTPUT_PATH);
            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "폰트 에셋을 먼저 생성하세요.", "OK");
                return;
            }

            // 게임에서 사용하는 특수 문자들
            string gameChars = "×÷±≠≤≥∞°%‰₩$€£¥★☆♠♣♥♦◆◇○●□■△▲▽▼←→↑↓↔⇐⇒⇑⇓";
            gameChars += "①②③④⑤⑥⑦⑧⑨⑩";
            gameChars += "ⓐⓑⓒⓓⓔⓕⓖⓗⓘⓙⓚⓛⓜⓝⓞⓟⓠⓡⓢⓣⓤⓥⓦⓧⓨⓩ";

            fontAsset.TryAddCharacters(gameChars, out string missing);

            if (!string.IsNullOrEmpty(missing))
            {
                Debug.Log($"[FontAssetCreator] Could not add: {missing}");
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Complete",
                $"특수 문자 추가 완료\n\n폰트에 없는 문자: {(string.IsNullOrEmpty(missing) ? "없음" : missing.Length + "개")}", "OK");
        }
    }
}
