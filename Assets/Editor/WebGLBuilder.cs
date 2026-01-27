using UnityEditor;
using UnityEngine;
using System.IO;

public class WebGLBuilder
{
    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
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
        Debug.Log("[WebGLBuilder] Starting WebGL build...");
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
}
