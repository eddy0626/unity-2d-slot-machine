using UnityEditor;
using UnityEngine;
using System.Reflection;

/// <summary>
/// 모바일 해상도 테스트를 위한 에디터 헬퍼
/// </summary>
public class MobileResolutionHelper : EditorWindow
{
    [MenuItem("Tools/Mobile Resolution/iPhone SE (640x1136)")]
    static void SetResolutioniPhoneSE() => SetGameViewResolution(640, 1136, "iPhone SE");

    [MenuItem("Tools/Mobile Resolution/iPhone 8 (750x1334)")]
    static void SetResolutioniPhone8() => SetGameViewResolution(750, 1334, "iPhone 8");

    [MenuItem("Tools/Mobile Resolution/iPhone 12 (1170x2532)")]
    static void SetResolutioniPhone12() => SetGameViewResolution(1170, 2532, "iPhone 12");

    [MenuItem("Tools/Mobile Resolution/iPhone 14 Pro (1179x2556)")]
    static void SetResolutioniPhone14Pro() => SetGameViewResolution(1179, 2556, "iPhone 14 Pro");

    [MenuItem("Tools/Mobile Resolution/Android HD (720x1280)")]
    static void SetResolutionAndroidHD() => SetGameViewResolution(720, 1280, "Android HD");

    [MenuItem("Tools/Mobile Resolution/Android FHD (1080x1920)")]
    static void SetResolutionAndroidFHD() => SetGameViewResolution(1080, 1920, "Android FHD");

    [MenuItem("Tools/Mobile Resolution/Android QHD (1440x2560)")]
    static void SetResolutionAndroidQHD() => SetGameViewResolution(1440, 2560, "Android QHD");

    [MenuItem("Tools/Mobile Resolution/Galaxy S21 (1080x2400)")]
    static void SetResolutionGalaxyS21() => SetGameViewResolution(1080, 2400, "Galaxy S21");

    [MenuItem("Tools/Mobile Resolution/Tablet Portrait (1536x2048)")]
    static void SetResolutionTabletPortrait() => SetGameViewResolution(1536, 2048, "Tablet Portrait");

    [MenuItem("Tools/Mobile Resolution/Tablet Landscape (2048x1536)")]
    static void SetResolutionTabletLandscape() => SetGameViewResolution(2048, 1536, "Tablet Landscape");

    static void SetGameViewResolution(int width, int height, string name)
    {
        // PlayerSettings 해상도 변경
        PlayerSettings.defaultScreenWidth = width;
        PlayerSettings.defaultScreenHeight = height;

        Debug.Log($"[MobileResolution] Set to {name}: {width}x{height}");
        Debug.Log($"[MobileResolution] Game View에서 'Free Aspect' 드롭다운을 클릭하고 '{width}x{height}' 또는 유사한 해상도를 선택하세요.");

        // Game View 창 포커스
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }

    [MenuItem("Tools/Mobile Resolution/Show Current Settings")]
    static void ShowCurrentSettings()
    {
        int w = PlayerSettings.defaultScreenWidth;
        int h = PlayerSettings.defaultScreenHeight;
        var orientation = PlayerSettings.defaultInterfaceOrientation;

        Debug.Log($"=== Current Resolution Settings ===");
        Debug.Log($"Default Screen: {w}x{h}");
        Debug.Log($"Orientation: {orientation}");
        Debug.Log($"Aspect Ratio: {(float)w/h:F2}");
    }
}
