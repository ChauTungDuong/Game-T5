using System.IO;
using System.Linq;
using CoreGuard;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// All roots are explicitly placed in the supplied scene. Existing named objects
// and assigned gameplay references are retained on repeated configuration.
public static class DemoSceneBuilder
{
    private const string ActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";
    private const string SpritePath = "Assets/_Game/Art/ArenaWhite.asset";
    private const string EnemyPath = "Assets/_Game/Prefabs/Enemy.prefab";
    private static readonly Color Cyan = new Color(.25f, .9f, .85f);
    private static readonly Color Ink = new Color(.055f, .085f, .12f, .97f);

    public static void Configure(Scene scene)
    {
        EnsureLayers();
        var sprite = EnsureSprite();
        var actions = EnsureActions();
        var camera = Find<Camera>(scene);
        var canvas = Find<Canvas>(scene);
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

        var world = Root(scene, "Arena");
        Visual(world.transform, "Floor", sprite, Vector2.zero, new Vector2(16, 8.4f), new Color(.075f, .115f, .15f), -10);
        Border(world.transform, "North wall", sprite, new Vector2(0, 4.1f), new Vector2(16, .2f));
        Border(world.transform, "South wall", sprite, new Vector2(0, -4.1f), new Vector2(16, .2f));
        Border(world.transform, "East wall", sprite, new Vector2(7.9f, 0), new Vector2(.2f, 8));
        Border(world.transform, "West wall", sprite, new Vector2(-7.9f, 0), new Vector2(.2f, 8));

        var coreObject = Root(scene, "Core");
        coreObject.layer = LayerMask.NameToLayer("Core");
        var core = GetOrAdd<CoreHealth>(coreObject);
        core.GetComponent<CircleCollider2D>().radius = .65f;
        core.GetComponent<CircleCollider2D>().isTrigger = true;
        Visual(coreObject.transform, "Core glow", sprite, Vector2.zero, new Vector2(1.7f, 1.7f), new Color(.08f, .3f, .32f), 0);
        Visual(coreObject.transform, "Core body", sprite, Vector2.zero, new Vector2(1.2f, 1.2f), Cyan, 1);
        Visual(coreObject.transform, "Core center", sprite, Vector2.zero, new Vector2(.55f, .55f), Ink, 2);

        var playerObject = Root(scene, "Player");
        playerObject.layer = LayerMask.NameToLayer("Player");
        var stats = GetOrAdd<PlayerStats>(playerObject);
        var newMotor = !playerObject.GetComponent<PlayerMotor>();
        var motor = GetOrAdd<PlayerMotor>(playerObject);
        var playerBody = playerObject.GetComponent<Rigidbody2D>();
        playerBody.gravityScale = 0; playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetOrAdd<CircleCollider2D>(playerObject).radius = .35f;
        if (newMotor) playerObject.transform.position = motor.SpawnPosition;
        Visual(playerObject.transform, "Player body", sprite, Vector2.zero, new Vector2(.7f, .7f), new Color(.3f, .65f, 1), 3);
        var turret = Child(playerObject.transform, "Turret");
        Visual(turret.transform, "Barrel", sprite, new Vector2(.4f, 0), new Vector2(.7f, .16f), Color.white, 4);
        if (!motor.Turret) motor.Turret = turret.transform;

        var session = GetOrAdd<GameSession>(Root(scene, "Game Session"));
        var spawner = GetOrAdd<EnemySpawner>(Root(scene, "Enemy Spawner"));
        if (!spawner.Prefab) spawner.Prefab = EnsureEnemyPrefab(scene, sprite);
        if (spawner.Gates == null || spawner.Gates.Length == 0)
        {
            var positions = new[] { new Vector2(7.4f, 0), new Vector2(0, 3.6f), new Vector2(-7.4f, 0), new Vector2(0, -3.6f) };
            spawner.Gates = new Transform[4];
            for (var i = 0; i < 4; i++)
            {
                var gate = Child(world.transform, "Gate " + (i + 1));
                gate.transform.localPosition = positions[i];
                Visual(gate.transform, "Gate marker", sprite, Vector2.zero, new Vector2(.25f, .25f), new Color(1, .47f, .35f), 1);
                spawner.Gates[i] = gate.transform;
            }
        }
        if (!session.Player) session.Player = stats;
        if (!session.Core) session.Core = core;
        if (!session.Motor) session.Motor = motor;
        if (!session.Spawner) session.Spawner = spawner;
        if (!motor.Session) motor.Session = session;
        if (!spawner.Session) spawner.Session = session;
        if (!spawner.Core) spawner.Core = core;
        var input = GetOrAdd<InputReader>(session.gameObject);
        if (!input.Actions) input.Actions = actions;
        if (!input.Session) input.Session = session;
        if (!input.WorldCamera) input.WorldCamera = camera;
        if (!session.Input) session.Input = input;
        var ui = Find<InputSystemUIInputModule>(scene);
        ui.actionsAsset = actions;
        ui.point = ActionReference("UI/Point"); ui.leftClick = ActionReference("UI/Click");
        ui.rightClick = ActionReference("UI/RightClick"); ui.middleClick = ActionReference("UI/MiddleClick");
        ui.scrollWheel = ActionReference("UI/ScrollWheel"); ui.move = ActionReference("UI/Navigate");
        ui.submit = ActionReference("UI/Submit"); ui.cancel = ActionReference("UI/Cancel");
        BuildHud(canvas.transform, session);
    }

    private static void BuildHud(Transform canvas, GameSession session)
    {
        var root = Child(canvas, "Core Guard HUD", typeof(RectTransform));
        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var hud = GetOrAdd<HudPresenter>(root); if (!hud.Session) hud.Session = session;
        Label(root.transform, "Title", "CORE / GUARD", new Vector2(-462, 323), new Vector2(300, 42), 27, Cyan);
        hud.StatsText = Label(root.transform, "Stats", "HP 100     ARMOR 50     COINS 0", new Vector2(-200, 277), new Vector2(650, 35), 21, Color.white);
        hud.CoreText = Label(root.transform, "Core health", "CORE 100", new Vector2(250, 323), new Vector2(170, 40), 23, Cyan);
        hud.TimerText = Label(root.transform, "Timer", "90.0 s", new Vector2(490, 323), new Vector2(170, 40), 28, Color.white);
        hud.StateText = Label(root.transform, "State", "READY", new Vector2(492, 278), new Vector2(170, 30), 16, Cyan);
        Label(root.transform, "Controls", "WASD / ARROWS  Move     MOUSE  Aim     ESC  Pause     R  Retry after result", new Vector2(0, -329), new Vector2(1100, 35), 18, new Color(.62f, .73f, .8f));
        hud.StartPanel = Panel(root.transform, "Start panel");
        Label(hud.StartPanel.transform, "Heading", "HOLD THE CORE", new Vector2(0, 82), new Vector2(490, 50), 32, Cyan);
        Label(hud.StartPanel.transform, "Brief", "A  /  PILOT     B  /  INTRUDERS\nKeep the core online for 90 seconds.\nIntruders arrive from four gates.", new Vector2(0, 0), new Vector2(470, 110), 20, Color.white);
        hud.StartButton = Button(hud.StartPanel.transform, "Start button", "START", new Vector2(0, -103));
        hud.PausePanel = Panel(root.transform, "Pause panel");
        Label(hud.PausePanel.transform, "Heading", "PAUSED", new Vector2(0, 55), new Vector2(490, 55), 34, Cyan);
        Label(hud.PausePanel.transform, "Hint", "Take a breath. The arena is waiting.", new Vector2(0, -5), new Vector2(490, 50), 20, Color.white);
        hud.ResumeButton = Button(hud.PausePanel.transform, "Resume button", "RESUME  /  ESC", new Vector2(0, -103));
        hud.ResultPanel = Panel(root.transform, "Result panel");
        hud.ResultText = Label(hud.ResultPanel.transform, "Heading", "CORE OFFLINE — LOST", new Vector2(0, 55), new Vector2(510, 55), 30, Cyan);
        Label(hud.ResultPanel.transform, "Hint", "Reset the arena and make another run.", new Vector2(0, -5), new Vector2(490, 50), 20, Color.white);
        hud.RetryButton = Button(hud.ResultPanel.transform, "Retry button", "RETRY  /  R", new Vector2(0, -103));
        hud.StartPanel.SetActive(true); hud.PausePanel.SetActive(false); hud.ResultPanel.SetActive(false);
    }
    private static GameObject Panel(Transform parent, string name)
    {
        var panel = Child(parent, name, typeof(RectTransform), typeof(Image));
        Layout((RectTransform)panel.transform, Vector2.zero, new Vector2(560, 320));
        panel.GetComponent<Image>().color = Ink;
        return panel;
    }
    private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var existing = parent.Find(name);
        if (existing && existing.TryGetComponent<Text>(out var retained)) return retained;
        var go = Child(parent, name, typeof(RectTransform), typeof(Text));
        Layout((RectTransform)go.transform, position, size);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
    private static Button Button(Transform parent, string name, string title, Vector2 position)
    {
        var go = Child(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
        Layout((RectTransform)go.transform, position, new Vector2(290, 52));
        go.GetComponent<Image>().color = new Color(.13f, .43f, .46f);
        Label(go.transform, "Label", title, Vector2.zero, new Vector2(280, 48), 21, Color.white);
        return go.GetComponent<Button>();
    }
    private static void Layout(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    private static void Border(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var wall = Visual(parent, name, sprite, position, size, new Color(.2f, .34f, .4f), 0);
        wall.layer = LayerMask.NameToLayer("Arena"); GetOrAdd<BoxCollider2D>(wall);
    }
    private static GameObject Visual(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
    {
        var old = parent.Find(name); if (old) return old.gameObject;
        var go = Child(parent, name, typeof(SpriteRenderer));
        go.transform.localPosition = position; go.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        return go;
    }
    private static EnemyController EnsureEnemyPrefab(Scene scene, Sprite sprite)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
        if (existing) return existing.GetComponent<EnemyController>();
        var go = new GameObject("Enemy", typeof(EnemyController)); SceneManager.MoveGameObjectToScene(go, scene);
        go.layer = LayerMask.NameToLayer("Enemy");
        var body = go.GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.bodyType = RigidbodyType2D.Kinematic; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var collider = go.GetComponent<CircleCollider2D>(); collider.radius = .35f; collider.isTrigger = true;
        Visual(go.transform, "Enemy body", sprite, Vector2.zero, new Vector2(.7f, .7f), new Color(1, .36f, .3f), 3);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, EnemyPath);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<EnemyController>();
    }
    private static Sprite EnsureSprite()
    {
        var existing = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().FirstOrDefault();
        if (existing) return existing;
        var texture = new Texture2D(1, 1); texture.name = "Arena white"; texture.SetPixel(0, 0, Color.white); texture.Apply();
        AssetDatabase.CreateAsset(texture, SpritePath);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        sprite.name = "Arena square"; AssetDatabase.AddObjectToAsset(sprite, texture); AssetDatabase.SaveAssets();
        return sprite;
    }
    private static InputActionAsset EnsureActions()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (asset.FindActionMap("Gameplay") != null) return asset;
        var editable = InputActionAsset.FromJson(asset.ToJson());
        var map = editable.AddActionMap("Gameplay");
        var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        map.AddAction("Aim", InputActionType.Value, "<Mouse>/position", expectedControlLayout: "Vector2");
        map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        map.AddAction("Weapon1", InputActionType.Button, "<Keyboard>/digit1"); map.AddAction("Weapon2", InputActionType.Button, "<Keyboard>/digit2"); map.AddAction("Weapon3", InputActionType.Button, "<Keyboard>/digit3");
        map.AddAction("Shield", InputActionType.Button, "<Keyboard>/q"); map.AddAction("EMP", InputActionType.Button, "<Keyboard>/e");
        map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape"); map.AddAction("Retry", InputActionType.Button, "<Keyboard>/r");
        File.WriteAllText(ActionsPath, editable.ToJson()); Object.DestroyImmediate(editable);
        AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
    }
    private static InputActionReference ActionReference(string path) => AssetDatabase.LoadAllAssetsAtPath(ActionsPath)
        .OfType<InputActionReference>().First(reference => reference.action.actionMap.name + "/" + reference.action.name == path);
    private static void EnsureLayers()
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        foreach (var name in new[] { "Player", "Enemy", "Core", "Arena" })
        {
            if (LayerMask.NameToLayer(name) >= 0) continue;
            for (var i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i); if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = name; tags.ApplyModifiedProperties(); break;
            }
        }
    }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).First();
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing ? existing : go.AddComponent<T>();
    }
    private static GameObject Root(Scene scene, string name)
    {
        var existing = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name); if (existing) return existing;
        var created = new GameObject(name); SceneManager.MoveGameObjectToScene(created, scene); return created;
    }
    private static GameObject Child(Transform parent, string name, params System.Type[] components)
    {
        var existing = parent.Find(name); if (existing) return existing.gameObject;
        var created = new GameObject(name, components); SceneManager.MoveGameObjectToScene(created, parent.gameObject.scene);
        created.transform.SetParent(parent, false); return created;
    }
}
