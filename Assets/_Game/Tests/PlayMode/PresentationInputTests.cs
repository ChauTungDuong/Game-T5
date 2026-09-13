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
        private InputActionAsset actions;
        private Keyboard keyboard;
        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name); objects.Add(go);
            var existing = go.GetComponent<T>();
            return existing ? existing : go.AddComponent<T>();
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

        // Missing subscriptions or button listeners would leave stale numbers/panels.
        [Test] public void Hud_ShowsLiveValuesAndButtonsDriveSingleSceneFlow()
        {
            var s = Session(); var hud = Make<HudPresenter>("HUD"); hud.Session = s;
            hud.StatsText = Make<Text>("Stats"); hud.CoreText = Make<Text>("Core text");
            hud.TimerText = Make<Text>("Timer"); hud.StateText = Make<Text>("State"); hud.ResultText = Make<Text>("Result");
            hud.StartPanel = Make<Transform>("Start").gameObject;
            hud.PausePanel = Make<Transform>("Pause").gameObject;
            hud.ResultPanel = Make<Transform>("Result panel").gameObject;
            hud.StartButton = Make<Button>("Start button"); hud.ResumeButton = Make<Button>("Resume"); hud.RetryButton = Make<Button>("Retry");
            hud.Bind();
            Assert.That(hud.StatsText.text, Does.Contain("HP 100"));
            Assert.That(hud.StartPanel.activeSelf, Is.True);
            Assert.That(hud.PausePanel.activeSelf, Is.False);
            hud.StartButton.onClick.Invoke(); s.Player.ApplyDamage(60); s.Player.AddCoins(7); s.Core.ApplyDamage(20); s.Advance(.5f);
            Assert.That(hud.StatsText.text, Does.Contain("HP 90"));
            Assert.That(hud.StatsText.text, Does.Contain("ARMOR 0"));
            Assert.That(hud.StatsText.text, Does.Contain("COINS 7"));
            Assert.That(hud.CoreText.text, Does.Contain("80"));
            Assert.That(hud.TimerText.text, Does.Contain("89.5"));
            Assert.That(hud.StartPanel.activeSelf, Is.False);
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
