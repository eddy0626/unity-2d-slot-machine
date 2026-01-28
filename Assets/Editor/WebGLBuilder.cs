using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

public class WebGLBuilder
{
    [MenuItem("Build/Build WebGL (High Quality)")]
    public static void BuildWebGL()
    {
        // 고품질 설정 적용
        ApplyHighQualitySettings();

        // 빌드 경로
        string buildPath = "Builds/WebGL";

        // 빌드 폴더 생성
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        // 씬 목록 가져오기
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };

        // 빌드 옵션
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        // 빌드 실행
        Debug.Log("[WebGLBuilder] Starting WebGL build with high quality settings...");
        var report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[WebGLBuilder] Build succeeded! Output: {buildPath}");
            Debug.Log($"[WebGLBuilder] Total size: {report.summary.totalSize / 1024 / 1024} MB");
        }
        else
        {
            Debug.LogError($"[WebGLBuilder] Build failed: {report.summary.result}");
        }
    }

    private static void ApplyHighQualitySettings()
    {
        // WebGL 해상도 설정
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;

        // WebGL 특정 설정
        PlayerSettings.WebGL.template = "APPLICATION:Default";

        // 안티앨리어싱 설정 (2x MSAA)
        QualitySettings.antiAliasing = 2;

        // 텍스처 품질 최대
        QualitySettings.globalTextureMipmapLimit = 0;

        // 이방성 필터링
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

        // VSyncCount (웹에서는 보통 1)
        QualitySettings.vSyncCount = 1;

        Debug.Log("[WebGLBuilder] High quality settings applied");
    }

    [MenuItem("Build/Apply WebGL Quality Settings")]
    public static void ApplyQualitySettingsOnly()
    {
        ApplyHighQualitySettings();
        Debug.Log("[WebGLBuilder] Quality settings applied (no build)");
    }
}
