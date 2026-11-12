using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Temporary build job for this cleanup. No menu entries; remove after both APKs have been verified.
[InitializeOnLoad]
public static class AndroidSizeBuildJob
{
    private const string Request = "Temp/AndroidSizeBuildJob.request.json";
    private const string Output = "Builds/Android";

    static AndroidSizeBuildJob() { EditorApplication.update += Wait; }

    private static void Wait()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        EditorApplication.update -= Wait;
        File.Move(Request, "Temp/AndroidSizeBuildJob.running.json");
        Run();
    }

    private static void Run()
    {
        Directory.CreateDirectory(Output);
        AndroidArchitecture original = PlayerSettings.Android.targetArchitectures;
        bool originalBundle = EditorUserBuildSettings.buildAppBundle;
        bool originalExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        bool originalSplit = PlayerSettings.Android.buildApkPerCpuArchitecture;
        bool originalDevelopment = EditorUserBuildSettings.development;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            Build(AndroidArchitecture.ARM64, "FishJamFever-ARM64.apk");
            Build(AndroidArchitecture.X86_64, "FishJamFever-x86_64.apk");
            File.WriteAllText(Output + "/build-complete.txt", "Both release APK builds succeeded.\n");
        }
        catch (Exception error)
        {
            File.WriteAllText(Output + "/build-error.txt", error.ToString());
            Debug.LogException(error);
        }
        finally
        {
            PlayerSettings.Android.targetArchitectures = original;
            PlayerSettings.Android.buildApkPerCpuArchitecture = originalSplit;
            EditorUserBuildSettings.buildAppBundle = originalBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = originalExport;
            EditorUserBuildSettings.development = originalDevelopment;
            AssetDatabase.SaveAssets();
            File.WriteAllText(Output + "/build-job-finished.txt", DateTime.UtcNow.ToString("O"));
        }
    }

    private static void Build(AndroidArchitecture architecture, string filename)
    {
        PlayerSettings.Android.targetArchitectures = architecture;
        string path = Output + "/" + filename;
        File.WriteAllText(Output + "/build-progress.txt", "Building " + architecture + " at " + DateTime.UtcNow.ToString("O"));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            locationPathName = path,
            options = BuildOptions.CompressWithLz4HC | BuildOptions.DetailedBuildReport
        });
        var text = new StringBuilder();
        text.AppendLine("Architecture: " + architecture);
        text.AppendLine("Result: " + report.summary.result);
        text.AppendLine("Bytes: " + report.summary.totalSize);
        text.AppendLine("Time: " + report.summary.totalTime);
        foreach (var item in report.packedAssets.SelectMany(pack => pack.contents)
            .OrderByDescending(item => item.packedSize).Take(60))
            text.AppendLine(item.packedSize + "\t" + item.sourceAssetPath);
        File.WriteAllText(Output + "/" + filename + ".report.txt", text.ToString());
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Android build failed for " + architecture + ": " + report.SummarizeErrors());
        Debug.Log("Optimized APK ready: " + path + " (" + new FileInfo(path).Length + " bytes)");
    }
}
