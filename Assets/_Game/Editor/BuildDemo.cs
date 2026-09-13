using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildDemo
{
    private const string MainScenePath = "Assets/_Game/Scenes/Main.unity";
    private const string WindowsBuildDirectory = "Builds/Windows/CoreGuard";
    private const string WindowsExecutablePath = WindowsBuildDirectory + "/CoreGuard.exe";

    private static readonly string[] GameDirectories =
    {
        "Assets/_Game/Art",
        "Assets/_Game/Audio",
        "Assets/_Game/Editor",
        "Assets/_Game/Prefabs",
        "Assets/_Game/Scenes",
        "Assets/_Game/Scripts",
        "Assets/_Game/Settings",
        "Assets/_Game/Tests",
    };

    [MenuItem("Core Guard/Configure Project")]
    public static void ConfigureProject()
    {
        foreach (var directory in GameDirectories)
        {
            Directory.CreateDirectory(directory);
        }

        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorSettings.externalVersionControl = "Visible Meta Files";
        PlayerSettings.companyName = "Core Guard";
        PlayerSettings.productName = "Core Guard";

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64))
        {
            throw new InvalidOperationException("Could not switch the active build target to Windows 64-bit.");
        }

        var scene = OpenOrCreateMainScene();
        CreateCameraIfMissing(scene);
        CreateCanvasIfMissing(scene);
        CreateEventSystemIfMissing(scene);
        DemoSceneBuilder.Configure(scene);

        if (!EditorSceneManager.SaveScene(scene, MainScenePath))
        {
            throw new InvalidOperationException($"Could not save scene at {MainScenePath}.");
        }

        var preservedBuildScenes = EditorBuildSettings.scenes
            .Where(buildScene => buildScene.path != MainScenePath)
            .ToList();
        preservedBuildScenes.Add(new EditorBuildSettingsScene(MainScenePath, true));
        EditorBuildSettings.scenes = preservedBuildScenes.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Core Guard configured: {MainScenePath} is enabled for Windows builds.");
    }

    private static Scene OpenOrCreateMainScene()
    {
        var loadedMainScene = SceneManager.GetSceneByPath(MainScenePath);
        if (loadedMainScene.IsValid() && loadedMainScene.isLoaded)
        {
            return loadedMainScene;
        }

        return File.Exists(MainScenePath)
            ? EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void CreateCameraIfMissing(Scene scene)
    {
        if (FindFirstComponentInScene<Camera>(scene) != null)
        {
            return;
        }

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.075f, 0.09f, 1f);
    }

    private static void CreateCanvasIfMissing(Scene scene)
    {
        if (FindFirstComponentInScene<Canvas>(scene) != null)
        {
            return;
        }

        var canvasObject = new GameObject(
            "Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void CreateEventSystemIfMissing(Scene scene)
    {
        if (FindFirstComponentInScene<EventSystem>(scene) != null)
        {
            return;
        }

        var eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static T FindFirstComponentInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .FirstOrDefault();
    }

    [MenuItem("Core Guard/Build Windows")]
    public static void BuildWindows()
    {
        if (EditorUtility.scriptCompilationFailed)
        {
            throw new BuildFailedException("Cannot build Core Guard while script compilation errors exist.");
        }

        var mainScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.path == MainScenePath);
        if (mainScene == null || !mainScene.enabled || !File.Exists(MainScenePath))
        {
            throw new BuildFailedException($"Required scene is missing or disabled: {MainScenePath}");
        }

        var enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && File.Exists(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        Directory.CreateDirectory(WindowsBuildDirectory);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = enabledScenes,
            locationPathName = WindowsExecutablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Windows build failed: {report.summary.result} " +
                $"({report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings).");
        }

        Debug.Log(
            $"Core Guard Windows build succeeded: {WindowsExecutablePath} " +
            $"({report.summary.totalSize} bytes).");
    }
}
