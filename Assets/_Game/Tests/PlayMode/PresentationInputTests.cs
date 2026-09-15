using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace CoreGuard.Tests.PlayMode
{
    public sealed class PresentationInputTests : InputTestFixture
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        private InputActionAsset actions;
        private Keyboard keyboard;
        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name); objects.Add(go);
            var existing = go.GetComponent<T>();
            return existing ? existing : go.AddComponent<T>();
        }
        private Sprite CreateTestSprite(string name)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = name + " texture";
            assets.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            sprite.name = name;
            assets.Add(sprite);
            return sprite;
        }
        private GameSession Session()
        {
            var s = Make<GameSession>("Session");
            s.Player = Make<PlayerStats>("Player"); s.Core = Make<CoreHealth>("Core");
            s.Motor = s.Player.gameObject.AddComponent<PlayerMotor>(); s.Motor.Session = s;
            s.Spawner = Make<EnemySpawner>("Spawner"); s.Spawner.enabled = false;
            s.Initialize(); return s;
        }
        [TearDown] public void Cleanup()
        {
            foreach (var go in objects) if (go) Object.DestroyImmediate(go);
            objects.Clear();
            for (var index = assets.Count - 1; index >= 0; index--)
                if (assets[index]) Object.DestroyImmediate(assets[index]);
            assets.Clear();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (actions) Object.DestroyImmediate(actions);
        }
        // Real queued key states must reach Move/Pause/Retry, gated by match state.
        [Test] public void Input_KeyboardMovementPauseAndResultRetryUseActions()
        {
            var s = Session(); var reader = Make<InputReader>("Input");
            keyboard = InputSystem.AddDevice<Keyboard>();
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = actions.AddActionMap("Gameplay");
            map.AddAction("Move", InputActionType.Value).AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Right", "<Keyboard>/d");
            map.AddAction("Aim", InputActionType.Value, "<Mouse>/position");
            map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            map.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            map.AddAction("Retry", InputActionType.Button, "<Keyboard>/r");
            reader.Actions = actions; reader.Session = s; reader.BindActions();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update();
            Assert.That(reader.Move, Is.EqualTo(Vector2.zero));
            s.StartMatch();
            Assert.That(reader.Move, Is.EqualTo(Vector2.right));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update(); reader.ReadCommands();
            Assert.That(s.State, Is.EqualTo(MatchState.Paused));
            Assert.That(reader.Move, Is.EqualTo(Vector2.zero));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); InputSystem.Update(); reader.ReadCommands();
            Assert.That(s.State, Is.EqualTo(MatchState.Paused));
            s.TogglePause(); s.Player.ApplyDamage(150); s.Advance(.02f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); InputSystem.Update(); reader.ReadCommands();
            Assert.That(s.State, Is.EqualTo(MatchState.Playing));
            Assert.That(s.Player.HP, Is.EqualTo(100));
        }
        // Missing subscriptions or button listeners would leave stale numbers/panels.
        [UnityTest] public IEnumerator Input_FireIsSuppressedOverUiAndOutsidePlaying()
        {
            var s = Session(); s.StartMatch();
            var reader = Make<InputReader>("Input");
            var mouse = InputSystem.AddDevice<Mouse>();
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = actions.AddActionMap("Gameplay");
            map.AddAction("Move", InputActionType.Value);
            map.AddAction("Aim", InputActionType.Value, "<Mouse>/position");
            map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            map.AddAction("Pause", InputActionType.Button);
            map.AddAction("Retry", InputActionType.Button);
            reader.Actions = actions; reader.Session = s; reader.BindActions();
            var events = Make<EventSystem>("Events");
            var module = events.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            var canvas = Make<Canvas>("Canvas"); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            var image = Make<Image>("Blocking UI"); image.transform.SetParent(canvas.transform, false);
            image.rectTransform.sizeDelta = new Vector2(200, 200);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            yield return null;
            Canvas.ForceUpdateCanvases();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(5, 5) }.WithButton(MouseButton.Left));
            InputSystem.Update(); module.Process();
            Assert.That(reader.FireHeld, Is.True, "Fire is available in the playing arena away from UI.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) }.WithButton(MouseButton.Left));
            InputSystem.Update(); module.Process();
            Assert.That(events.IsPointerOverGameObject(), Is.True, "Fixture pointer must hit the real Canvas image.");
            Assert.That(reader.FireHeld, Is.False, "Pointer over UI must suppress gameplay fire.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(5, 5) }.WithButton(MouseButton.Left));
            InputSystem.Update(); module.Process();
            s.TogglePause(); Assert.That(reader.FireHeld, Is.False);
        }

        [UnityTest] public IEnumerator DefenseFeedback_ShowsShieldRingAndEmpAreaWithAffectedCount()
        {
            var s = Session();
            var defense = s.Player.gameObject.AddComponent<DefenseController>();
            defense.Session = s; defense.Player = s.Player; s.Defense = defense;
            var presenter = s.Player.gameObject.AddComponent<CombatVfxPresenter>();
            presenter.Defense = defense; presenter.Bind();
            s.StartMatch();

            Assert.That(defense.TryActivateShield(), Is.True);
            yield return null;
            var shield = s.Player.transform.Find("Shield Indicator");
            Assert.That(shield, Is.Not.Null);
            Assert.That(shield.GetComponent<LineRenderer>(), Is.Not.Null);

            var enemy = Make<EnemyController>("EMP target");
            enemy.Initialize(s, s.Core);
            enemy.transform.position = s.Player.transform.position + Vector3.right;
            Assert.That(defense.TryActivateEmp(), Is.True);
            yield return null;
            var pulse = s.Player.transform.Find("EMP Indicator");
            Assert.That(pulse, Is.Not.Null);
            Assert.That(pulse.GetComponent<LineRenderer>(), Is.Not.Null);
            Assert.That(pulse.GetComponentInChildren<TextMesh>().text, Is.EqualTo("EMP 1"));

            s.Advance(3f);
            yield return null;
            Assert.That(s.Player.transform.Find("Shield Indicator"), Is.Null);
        }

        [UnityTest] public IEnumerator PlayerAndEnemyDeaths_SpawnExplosionFeedback()
        {
            var s = Session();
            var presenter = s.Player.gameObject.AddComponent<CombatVfxPresenter>();
            var frame = CreateTestSprite("Death explosion");
            presenter.ExplosionFrames = new[] { frame };
            s.StartMatch();

            var enemy = Make<EnemyController>("Exploding enemy");
            enemy.Initialize(s, s.Core);
            enemy.ApplyDamage(EnemyController.MaxHP);
            yield return null;
            Assert.That(GameObject.Find("Explosion VFX"), Is.Not.Null);

            yield return new WaitForSeconds(.05f);
            s.Player.ApplyDamage(150f);
            yield return null;
            Assert.That(GameObject.Find("Explosion VFX"), Is.Not.Null);
            yield return new WaitForSeconds(.1f);
        }

        // Missing subscriptions or button listeners would leave stale numbers/panels.
        [UnityTest] public IEnumerator Hud_ShowsLiveValuesAndButtonsDriveSingleSceneFlow()
        {
            var s = Session();
            s.Weapon = s.Player.gameObject.AddComponent<WeaponController>(); s.Weapon.Session = s;
            s.Defense = s.Player.gameObject.GetComponent<DefenseController>() ?? s.Player.gameObject.AddComponent<DefenseController>();
            s.Defense.Session = s; s.Defense.Player = s.Player;
            var hud = Make<HudPresenter>("HUD"); hud.Session = s;
            hud.StatsText = Make<Text>("Stats"); hud.CoreText = Make<Text>("Core text");
            hud.TimerText = Make<Text>("Timer"); hud.StateText = Make<Text>("State"); hud.ResultText = Make<Text>("Result");
            hud.WeaponText = Make<Text>("Weapon"); hud.CooldownsText = Make<Text>("Defense state");
            hud.StartPanel = Make<Transform>("Start").gameObject;
            hud.PausePanel = Make<Transform>("Pause").gameObject;
            hud.ResultPanel = Make<Transform>("Result panel").gameObject;
            hud.CountdownImage = Make<Image>("Countdown image");
            hud.ResultImage = Make<Image>("Result image");
            var countdownSprite = CreateTestSprite("Countdown");
            hud.CountdownZero = countdownSprite; hud.CountdownOne = countdownSprite;
            hud.CountdownTwo = countdownSprite; hud.CountdownThree = countdownSprite;
            hud.WinSprite = countdownSprite; hud.LoseSprite = countdownSprite;
            hud.CountdownDuration = .05f;
            hud.StartButton = Make<Button>("Start button"); hud.ResumeButton = Make<Button>("Resume"); hud.RetryButton = Make<Button>("Retry");
            hud.BulletButton = Make<Button>("Bullet"); hud.RocketButton = Make<Button>("Rocket"); hud.MineButton = Make<Button>("Mine");
            hud.ShieldButton = Make<Button>("Shield"); hud.EmpButton = Make<Button>("EMP");
            hud.Bind();
            Assert.That(hud.StatsText.text, Is.EqualTo("PLAYER HP 100/100\nARMOR 50/50\nCOINS 0"));
            Assert.That(hud.CoreText.text, Is.EqualTo("CORE 100/100"));
            Assert.That(hud.TimerText.text, Is.EqualTo("TIME 90.0"));
            Assert.That(hud.WeaponText.text, Is.EqualTo("WEAPON: BULLET"));
            Assert.That(hud.CooldownsText.text, Does.Contain("SPEED: 4.0"));
            Assert.That(hud.CooldownsText.text, Does.Contain("SHIELD: READY"));
            Assert.That(hud.CooldownsText.text, Does.Contain("EMP: READY"));
            Assert.That(hud.StartPanel.activeSelf, Is.True);
            Assert.That(hud.PausePanel.activeSelf, Is.False);
            hud.StartButton.onClick.Invoke();
            Assert.That(s.State, Is.EqualTo(MatchState.Ready));
            Assert.That(hud.CountdownImage.gameObject.activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(s.State, Is.EqualTo(MatchState.Playing));
            Assert.That(hud.CountdownImage.gameObject.activeSelf, Is.False);
            s.Weapon.Select(WeaponKind.Rocket);
            Assert.That(hud.WeaponText.text, Is.EqualTo("WEAPON: ROCKET"));
            s.Weapon.Select(WeaponKind.Bullet);
            s.Player.ApplyDamage(60); s.Player.AddCoins(7); s.Core.ApplyDamage(20); s.Advance(.5f);
            Assert.That(hud.StatsText.text, Does.Contain("PLAYER HP 90/100"));
            Assert.That(hud.StatsText.text, Does.Contain("ARMOR 0/50"));
            Assert.That(hud.StatsText.text, Does.Contain("COINS 7"));
            Assert.That(hud.CoreText.text, Does.Contain("80"));
            Assert.That(hud.TimerText.text, Does.Contain("89."));
            Assert.That(hud.StartPanel.activeSelf, Is.False);
            Assert.That(s.Defense.TryActivateShield(), Is.True);
            Assert.That(hud.CooldownsText.text, Does.Contain("SHIELD: 3/3"));
            Assert.That(hud.ShieldButton.interactable, Is.False);
            s.TogglePause(); Assert.That(hud.PausePanel.activeSelf, Is.True);
            hud.ResumeButton.onClick.Invoke(); Assert.That(hud.PausePanel.activeSelf, Is.False);
            s.Core.ApplyDamage(100); s.Advance(.02f);
            Assert.That(hud.ResultPanel.activeSelf, Is.True);
            Assert.That(hud.ResultText.text, Does.Contain("LOST"));
            hud.RetryButton.onClick.Invoke();
            Assert.That(hud.ResultPanel.activeSelf, Is.False);
            Assert.That(hud.StatsText.text, Does.Contain("HP 100"));
        }
    }
}
