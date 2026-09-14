using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildDemo
{
    private const string ConfigureMenuPath = "Core Guard/Configure Project";
    private const string MainScenePath = "Assets/_Game/Scenes/Main.unity";

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

    [MenuItem(ConfigureMenuPath)]
    public static void ConfigureProject()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Core Guard: Stop Play Mode before running Configure Project.");
            return;
        }

        foreach (var directory in GameDirectories)
        {
            Directory.CreateDirectory(directory);
        }

        EditorSettings.serializationMode = SerializationMode.ForceText;
        VersionControlSettings.mode = "Visible Meta Files";
        PlayerSettings.companyName = "Core Guard";
        PlayerSettings.productName = "Core Guard";

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
        Debug.Log($"Core Guard configured for Unity Editor demo: {MainScenePath}");
    }

    [MenuItem(ConfigureMenuPath, true)]
    private static bool ValidateConfigureProject()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
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
        var existingCamera = FindFirstComponentInScene<Camera>(scene);
        if (existingCamera != null)
        {
            ConfigureCamera(existingCamera);
            return;
        }

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        ConfigureCamera(cameraObject.GetComponent<Camera>());
    }

    private static void ConfigureCamera(Camera camera)
    {
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.018f, .035f, .065f, 1f);
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

}
