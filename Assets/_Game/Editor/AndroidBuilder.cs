using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CoreGuard.Editor
{
    [InitializeOnLoad]
    public static class AndroidBuilder
    {
        private const string TriggerFile = "Temp/BuildApkTrigger.txt";
        private const string StatusFile = "Temp/BuildApkStatus.txt";
        private const string OutputApk = "Builds/CoreGuard.apk";
        private const string MainScenePath = "Assets/_Game/Scenes/Main.unity";

        private static bool _isBuilding = false;

        static AndroidBuilder()
        {
            EditorApplication.update += CheckTrigger;
        }

        private static void CheckTrigger()
        {
            if (_isBuilding) return;
            if (!File.Exists(TriggerFile)) return;

            try
            {
                File.Delete(TriggerFile);
            }
            catch
            {
                return;
            }

            BuildAndroidApk();
        }

        [MenuItem("Core Guard/Build Android APK")]
        public static void BuildAndroidApkMenu()
        {
            BuildAndroidApk();
        }

        public static void BuildCommandLine()
        {
            var result = BuildAndroidApk();
            if (result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
            else
            {
                EditorApplication.Exit(0);
            }
        }

        public static BuildResult BuildAndroidApk()
        {
            if (_isBuilding)
            {
                Debug.LogWarning("[AndroidBuilder] A build is already in progress.");
                return BuildResult.Unknown;
            }

            _isBuilding = true;
            try
            {
                File.WriteAllText(StatusFile, "BUILDING\nStarted at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch {}

            try
            {
                Debug.Log("[AndroidBuilder] Starting Android APK build preparation...");

                // 1. Ensure output directory exists
                string outputDir = Path.GetDirectoryName(OutputApk);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 2. Configure Android External Tools (Embedded SDK / NDK / JDK)
                try
                {
                    EditorPrefs.SetBool("SdkUseEmbedded", true);
                    EditorPrefs.SetBool("NdkUseEmbedded", true);
                    EditorPrefs.SetBool("JdkUseEmbedded", true);

                    var extToolsType = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
                    if (extToolsType != null)
                    {
                        var sdkProp = extToolsType.GetProperty("sdkUseEmbedded", BindingFlags.Public | BindingFlags.Static);
                        sdkProp?.SetValue(null, true);
                        var ndkProp = extToolsType.GetProperty("ndkUseEmbedded", BindingFlags.Public | BindingFlags.Static);
                        ndkProp?.SetValue(null, true);
                        var jdkProp = extToolsType.GetProperty("jdkUseEmbedded", BindingFlags.Public | BindingFlags.Static);
                        jdkProp?.SetValue(null, true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[AndroidBuilder] Note on external tools: " + ex.Message);
                }

                // 3. Configure Player Settings for Android
                PlayerSettings.companyName = "Core Guard";
                PlayerSettings.productName = "Core Guard";
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.coreguard.game");
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

                // 4. Determine scenes to include
                string[] scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                    .Select(s => s.path)
                    .ToArray();

                if (scenes.Length == 0)
                {
                    scenes = new[] { MainScenePath };
                }

                // 5. Switch active build target if not Android
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    Debug.Log("[AndroidBuilder] Switching active build target to Android...");
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }

                // 6. Build Player
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = OutputApk,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };

                Debug.Log($"[AndroidBuilder] Building APK to '{OutputApk}'...");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    long fileSizeBytes = File.Exists(OutputApk) ? new FileInfo(OutputApk).Length : (long)summary.totalSize;
                    string successMsg = $"SUCCESS\nPath: {Path.GetFullPath(OutputApk)}\nSize: {fileSizeBytes} bytes\nTime: {summary.totalTime.TotalSeconds:F1}s";
                    File.WriteAllText(StatusFile, successMsg);
                    Debug.Log($"[AndroidBuilder] Build succeeded: {OutputApk} ({fileSizeBytes / (1024 * 1024):F1} MB)");
                    return BuildResult.Succeeded;
                }
                else
                {
                    string failMsg = $"FAILED\nResult: {summary.result}\nErrors: {summary.totalErrors}\nTime: {summary.totalTime.TotalSeconds:F1}s";
                    File.WriteAllText(StatusFile, failMsg);
                    Debug.LogError($"[AndroidBuilder] Build failed with result: {summary.result}, errors: {summary.totalErrors}");
                    return summary.result;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"ERROR\nException: {ex.Message}\n{ex.StackTrace}";
                try
                {
                    File.WriteAllText(StatusFile, errorMsg);
                }
                catch {}
                Debug.LogError($"[AndroidBuilder] Exception during build: {ex}");
                return BuildResult.Failed;
            }
            finally
            {
                _isBuilding = false;
            }
        }
    }
}
