using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 제출용 Player 빌드를 같은 순서(Addressables 콘텐츠 → Player)로 만듭니다.
// Editor 폴더에 있으므로 Player 빌드에는 포함되지 않습니다.
// 배치 모드 예: Unity.exe -batchmode -quit -projectPath . -executeMethod SubmissionBuilder.BuildWindows
public static class SubmissionBuilder
{
    private const string Root = "Builds";

    [MenuItem("Tools/Platform Lab/Submission/Build Windows")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, "Windows/PlatformCoinLab.exe");
    }

    [MenuItem("Tools/Platform Lab/Submission/Build Android APK")]
    public static void BuildAndroid()
    {
        // AAB가 아닌 설치 가능한 APK를 만듭니다. 서명 키를 지정하지 않으면 Unity 디버그 키로 서명됩니다.
        EditorUserBuildSettings.buildAppBundle = false;
        Build(BuildTarget.Android, BuildTargetGroup.Android, "Android/PlatformCoinLab.apk");
    }

    [MenuItem("Tools/Platform Lab/Submission/Build Web")]
    public static void BuildWeb()
    {
        // ContentServer는 Content-Encoding 헤더를 보내지 않으므로 압축 없이 빌드합니다.
        // 프로젝트 설정은 빌드 후 원래 값으로 되돌립니다.
        WebGLCompressionFormat previous = PlayerSettings.WebGL.compressionFormat;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        try
        {
            Build(BuildTarget.WebGL, BuildTargetGroup.WebGL, "Web");
        }
        finally
        {
            PlayerSettings.WebGL.compressionFormat = previous;
        }
    }

    private static void Build(BuildTarget target, BuildTargetGroup group, string relativeOutput)
    {
        if (EditorUserBuildSettings.activeBuildTarget != target
            && !EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
        {
            Fail($"Build target switch failed: {target}");
            return;
        }

        // 1. 현재 활성 프로필 기준으로 Addressables 콘텐츠를 먼저 만듭니다.
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
        if (!string.IsNullOrEmpty(content.Error))
        {
            Fail($"Addressables build failed: {content.Error}");
            return;
        }

        // 2. Build Settings에 켜 둔 씬만 사용합니다.
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Fail("No enabled scene in Build Settings.");
            return;
        }

        string output = Path.Combine(Root, relativeOutput);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) ?? Root);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            target = target,
            targetGroup = group,
            locationPathName = output,
            options = BuildOptions.None
        });

        BuildSummary summary = report.summary;
        string line = $"[Submission] {target} {summary.result} size={summary.totalSize} bytes " +
                      $"time={summary.totalTime} errors={summary.totalErrors} -> {output}";
        File.AppendAllText(Path.Combine(Root, "build-log.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}{Environment.NewLine}");

        if (summary.result != BuildResult.Succeeded)
        {
            Fail(line);
            return;
        }

        Debug.Log(line);
    }

    private static void Fail(string message)
    {
        Debug.LogError("[Submission] " + message);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }
}
