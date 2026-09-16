using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard.Tests.Editor
{
    public sealed class MobileControlsContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private GameSession MakeSession()
        {
            var session = Make<GameSession>("Session");
            session.enabled = false;
            session.Player = Make<PlayerStats>("Player");
            session.Player.gameObject.AddComponent<CircleCollider2D>();
            session.Core = Make<CoreHealth>("Core");
            session.Motor = session.Player.gameObject.AddComponent<PlayerMotor>();
            session.Motor.Session = session;
            session.Defense = session.Player.gameObject.AddComponent<DefenseController>();
            session.Defense.Session = session;
            session.Defense.Player = session.Player;
            session.Input = Make<InputReader>("Input");
            session.Input.Session = session;
            session.Spawner = Make<EnemySpawner>("Spawner");
            session.Spawner.Session = session;
            session.Spawner.Core = session.Core;
            session.Spawner.enabled = false;
            session.Initialize();
            session.StartMatch();
            return session;
        }

        private VirtualDirectionalButton MakeDpadButton(string name, Vector2 direction, MobileControlsPresenter presenter)
        {
            var btn = Make<VirtualDirectionalButton>(name);
            btn.Direction = direction;
            btn.Presenter = presenter;
            btn.BackgroundImage = btn.gameObject.AddComponent<Image>();
            btn.LabelText = btn.gameObject.AddComponent<Text>();
            return btn;
        }

        [TearDown]
        public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void Dpad_PressingIndividualDirections_UpdatesInputMoveCorrectly()
        {
            var session = MakeSession();
            var presenter = Make<MobileControlsPresenter>("Mobile controls");
            presenter.Session = session;

            var up = MakeDpadButton("Up", Vector2.up, presenter);
            var down = MakeDpadButton("Down", Vector2.down, presenter);
            var left = MakeDpadButton("Left", Vector2.left, presenter);
            var right = MakeDpadButton("Right", Vector2.right, presenter);

            presenter.UpButton = up;
            presenter.DownButton = down;
            presenter.LeftButton = left;
            presenter.RightButton = right;
            presenter.Bind();

            Assert.That(session.Input.Move, Is.EqualTo(Vector2.zero));

            // Up
            up.SetPressedState(true);
            Assert.That(session.Input.Move.x, Is.EqualTo(0f).Within(.0001f));
            Assert.That(session.Input.Move.y, Is.EqualTo(1f).Within(.0001f));

            up.SetPressedState(false);
            Assert.That(session.Input.Move, Is.EqualTo(Vector2.zero));

            // Down
            down.SetPressedState(true);
            Assert.That(session.Input.Move.x, Is.EqualTo(0f).Within(.0001f));
            Assert.That(session.Input.Move.y, Is.EqualTo(-1f).Within(.0001f));

            down.SetPressedState(false);
            Assert.That(session.Input.Move, Is.EqualTo(Vector2.zero));

            // Left
            left.SetPressedState(true);
            Assert.That(session.Input.Move.x, Is.EqualTo(-1f).Within(.0001f));
            Assert.That(session.Input.Move.y, Is.EqualTo(0f).Within(.0001f));

            left.SetPressedState(false);
            Assert.That(session.Input.Move, Is.EqualTo(Vector2.zero));

            // Right
            right.SetPressedState(true);
            Assert.That(session.Input.Move.x, Is.EqualTo(1f).Within(.0001f));
            Assert.That(session.Input.Move.y, Is.EqualTo(0f).Within(.0001f));

            right.SetPressedState(false);
            Assert.That(session.Input.Move, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Dpad_MultiTouchDiagonalDirections_NormalizesMoveVector()
        {
            var session = MakeSession();
            var presenter = Make<MobileControlsPresenter>("Mobile controls");
            presenter.Session = session;

            var up = MakeDpadButton("Up", Vector2.up, presenter);
            var down = MakeDpadButton("Down", Vector2.down, presenter);
            var left = MakeDpadButton("Left", Vector2.left, presenter);
            var right = MakeDpadButton("Right", Vector2.right, presenter);

            presenter.UpButton = up;
            presenter.DownButton = down;
            presenter.LeftButton = left;
            presenter.RightButton = right;
            presenter.Bind();

            // Up + Right diagonal
            up.SetPressedState(true);
            right.SetPressedState(true);

            var expected = new Vector2(1f, 1f).normalized;
            Assert.That(session.Input.Move.x, Is.EqualTo(expected.x).Within(.001f));
            Assert.That(session.Input.Move.y, Is.EqualTo(expected.y).Within(.001f));

            // Release Up, keep Right
            up.SetPressedState(false);
            Assert.That(session.Input.Move.x, Is.EqualTo(1f).Within(.0001f));
            Assert.That(session.Input.Move.y, Is.EqualTo(0f).Within(.0001f));

            // Down + Left diagonal
            right.SetPressedState(false);
            down.SetPressedState(true);
            left.SetPressedState(true);

            var expectedDownLeft = new Vector2(-1f, -1f).normalized;
            Assert.That(session.Input.Move.x, Is.EqualTo(expectedDownLeft.x).Within(.001f));
            Assert.That(session.Input.Move.y, Is.EqualTo(expectedDownLeft.y).Within(.001f));

            down.SetPressedState(false);
            left.SetPressedState(false);
            Assert.That(session.Input.Move, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void WeaponCycleButton_CyclesWeaponsAndUpdatesLabelText()
        {
            var session = MakeSession();
            var weapon = Make<WeaponController>("Weapons");
            weapon.Session = session;
            session.Weapon = weapon;

            var presenter = Make<MobileControlsPresenter>("Mobile controls");
            presenter.Session = session;

            var cycleBtn = Make<Button>("Weapon cycle button");
            var label = cycleBtn.gameObject.AddComponent<Text>();

            presenter.WeaponCycleButton = cycleBtn;
            presenter.WeaponCycleLabel = label;
            presenter.Bind();

            Assert.That(weapon.SelectedWeapon, Is.EqualTo(WeaponKind.Bullet));
            Assert.That(label.text, Does.Contain("BULLET"));

            // Tap 1: Bullet -> Rocket
            presenter.OnWeaponCycleClicked();
            Assert.That(weapon.SelectedWeapon, Is.EqualTo(WeaponKind.Rocket));
            Assert.That(label.text, Does.Contain("ROCKET"));

            // Tap 2: Rocket -> Laser
            presenter.OnWeaponCycleClicked();
            Assert.That(weapon.SelectedWeapon, Is.EqualTo(WeaponKind.Laser));
            Assert.That(label.text, Does.Contain("LASER"));

            // Tap 3: Laser -> Bullet
            presenter.OnWeaponCycleClicked();
            Assert.That(weapon.SelectedWeapon, Is.EqualTo(WeaponKind.Bullet));
            Assert.That(label.text, Does.Contain("BULLET"));
        }

        [Test]
        public void ShieldButton_ActivatesShieldAndUpdatesLabel()
        {
            var session = MakeSession();
            var presenter = Make<MobileControlsPresenter>("Mobile controls");
            presenter.Session = session;

            var shieldBtn = Make<Button>("Shield button");
            var label = shieldBtn.gameObject.AddComponent<Text>();
            presenter.ShieldButton = shieldBtn;
            presenter.ShieldLabel = label;
            presenter.Bind();

            Assert.That(session.Defense.ShieldActive, Is.False);
            Assert.That(label.text, Does.Contain("SHIELD"));

            presenter.OnShieldClicked();
            Assert.That(session.Defense.ShieldActive, Is.True);
            Assert.That(label.text, Does.Contain("HIT"));
        }

        [Test]
        public void EmpButton_ActivatesEmpAndUpdatesLabel()
        {
            var session = MakeSession();
            var presenter = Make<MobileControlsPresenter>("Mobile controls");
            presenter.Session = session;

            var empBtn = Make<Button>("Emp button");
            var label = empBtn.gameObject.AddComponent<Text>();
            presenter.EmpButton = empBtn;
            presenter.EmpLabel = label;
            presenter.Bind();

            Assert.That(session.Defense.EmpCooldownRemaining, Is.EqualTo(0f));
            Assert.That(label.text, Does.Contain("EMP"));

            presenter.OnEmpClicked();
            Assert.That(session.Defense.EmpCooldownRemaining, Is.GreaterThan(0f));
            Assert.That(label.text, Does.Contain("s"));
        }

        [Test]
        public void EnsureControls_CreatesDpadAndThreeCircularActionButtonsAutomatically()
        {
            var session = MakeSession();
            var canvas = Make<Canvas>("Canvas");
            var presenter = Make<MobileControlsPresenter>("Mobile controls");
            presenter.transform.SetParent(canvas.transform, false);
            presenter.Session = session;

            presenter.EnsureControls();

            // 4 Directional buttons
            Assert.That(presenter.UpButton, Is.Not.Null);
            Assert.That(presenter.DownButton, Is.Not.Null);
            Assert.That(presenter.LeftButton, Is.Not.Null);
            Assert.That(presenter.RightButton, Is.Not.Null);

            Assert.That(presenter.UpButton.Direction, Is.EqualTo(Vector2.up));
            Assert.That(presenter.DownButton.Direction, Is.EqualTo(Vector2.down));
            Assert.That(presenter.LeftButton.Direction, Is.EqualTo(Vector2.left));
            Assert.That(presenter.RightButton.Direction, Is.EqualTo(Vector2.right));

            // 3 Circular Action buttons: Q (Shield), E (EMP), R (Weapon)
            Assert.That(presenter.ShieldButton, Is.Not.Null);
            Assert.That(presenter.ShieldLabel, Is.Not.Null);
            Assert.That(presenter.EmpButton, Is.Not.Null);
            Assert.That(presenter.EmpLabel, Is.Not.Null);
            Assert.That(presenter.WeaponCycleButton, Is.Not.Null);
            Assert.That(presenter.WeaponCycleLabel, Is.Not.Null);

            Assert.That(presenter.ShieldLabel.text, Does.Contain("Q"));
            Assert.That(presenter.EmpLabel.text, Does.Contain("E"));
            Assert.That(presenter.WeaponCycleLabel.text, Does.Contain("R"));
        }

        [Test]
        public void MobileControls_HiddenWhenNotInPlayState()
        {
            var session = MakeSession();
            var canvas = Make<Canvas>("Canvas");
            var presenter = Make<MobileControlsPresenter>("Mobile controls");
            presenter.transform.SetParent(canvas.transform, false);
            presenter.Session = session;
            presenter.EnsureControls();
            presenter.Bind();

            // When Playing (MatchState.Playing)
            Assert.That(presenter.ShieldButton.gameObject.activeSelf, Is.True);
            Assert.That(presenter.EmpButton.gameObject.activeSelf, Is.True);
            Assert.That(presenter.WeaponCycleButton.gameObject.activeSelf, Is.True);
            Assert.That(presenter.transform.Find("Dpad").gameObject.activeSelf, Is.True);

            // When on Home Screen (MatchState.Ready)
            session.Retry();
            presenter.Refresh();

            Assert.That(presenter.ShieldButton.gameObject.activeSelf, Is.False);
            Assert.That(presenter.EmpButton.gameObject.activeSelf, Is.False);
            Assert.That(presenter.WeaponCycleButton.gameObject.activeSelf, Is.False);
            Assert.That(presenter.transform.Find("Dpad").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void HomeScreen_EnsuresFullScreenBackgroundAndLogo()
        {
            var session = MakeSession();
            session.Retry();
            var canvas = Make<Canvas>("Canvas");
            var hud = Make<HudPresenter>("HUD");
            hud.transform.SetParent(canvas.transform, false);
            hud.Session = session;

            var startPanel = Make<Image>("Start panel");
            startPanel.transform.SetParent(hud.transform, false);
            hud.StartPanel = startPanel.gameObject;

            var heading = Make<Text>("Heading");
            heading.transform.SetParent(startPanel.transform, false);
            var brief = Make<Text>("Brief");
            brief.transform.SetParent(startPanel.transform, false);

            var startBtn = Make<Button>("Start button");
            startBtn.transform.SetParent(startPanel.transform, false);
            hud.StartButton = startBtn;

            var dummyBg = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            var dummyLogo = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            hud.HomeBackground = dummyBg;
            hud.HomeLogo = dummyLogo;

            hud.Bind();

            var startRt = (RectTransform)startPanel.transform;
            Assert.That(startRt.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(startRt.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(startPanel.sprite, Is.EqualTo(dummyBg));

            var logo = startPanel.transform.Find("Logo");
            Assert.That(logo, Is.Not.Null);
            var logoImg = logo.GetComponent<Image>();
            Assert.That(logoImg, Is.Not.Null);
            Assert.That(logoImg.sprite, Is.EqualTo(dummyLogo));

            Assert.That(heading.gameObject.activeSelf, Is.False);
            Assert.That(brief.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SettingsPanel_DisplaysAsSmallWindowOverBackground()
        {
            var session = MakeSession();
            session.Retry();
            var canvas = Make<Canvas>("Canvas");
            var hud = Make<HudPresenter>("HUD");
            hud.transform.SetParent(canvas.transform, false);
            hud.Session = session;

            var startPanel = Make<Image>("Start panel");
            startPanel.transform.SetParent(hud.transform, false);
            hud.StartPanel = startPanel.gameObject;

            var startBtn = Make<Button>("Start button");
            startBtn.transform.SetParent(startPanel.transform, false);
            hud.StartButton = startBtn;

            var settingsBtn = Make<Button>("Settings button");
            settingsBtn.transform.SetParent(startPanel.transform, false);
            hud.SettingsButton = settingsBtn;

            var settingsPanel = Make<Image>("Settings panel");
            settingsPanel.transform.SetParent(hud.transform, false);
            hud.SettingsPanel = settingsPanel.gameObject;

            var closeBtn = Make<Button>("Close settings button");
            closeBtn.transform.SetParent(settingsPanel.transform, false);
            hud.CloseSettingsButton = closeBtn;

            hud.Bind();

            Assert.That(startPanel.gameObject.activeSelf, Is.True);
            Assert.That(settingsPanel.gameObject.activeSelf, Is.False);
            Assert.That(startBtn.gameObject.activeSelf, Is.True);
            Assert.That(settingsBtn.gameObject.activeSelf, Is.True);

            settingsBtn.onClick.Invoke();

            Assert.That(startPanel.gameObject.activeSelf, Is.True, "Background StartPanel must remain active when settings is open");
            Assert.That(settingsPanel.gameObject.activeSelf, Is.True, "SettingsPanel must be active");
            Assert.That(startBtn.gameObject.activeSelf, Is.False, "Start button should be hidden while settings is open");
            Assert.That(settingsBtn.gameObject.activeSelf, Is.False, "Settings button should be hidden while settings is open");

            var settingsRt = (RectTransform)settingsPanel.transform;
            Assert.That(settingsRt.sizeDelta, Is.EqualTo(new Vector2(420, 270)));
            Assert.That(settingsRt.anchoredPosition, Is.EqualTo(Vector2.zero));

            closeBtn.onClick.Invoke();

            Assert.That(startPanel.gameObject.activeSelf, Is.True);
            Assert.That(settingsPanel.gameObject.activeSelf, Is.False);
            Assert.That(startBtn.gameObject.activeSelf, Is.True);
            Assert.That(settingsBtn.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Gameplay_BottomActionButtonsAreHidden()
        {
            var session = MakeSession();
            var canvas = Make<Canvas>("Canvas");
            var hud = Make<HudPresenter>("HUD");
            hud.transform.SetParent(canvas.transform, false);
            hud.Session = session;

            var bulletBtn = Make<Button>("Bullet button");
            bulletBtn.transform.SetParent(hud.transform, false);
            hud.BulletButton = bulletBtn;

            var cycleBtn = Make<Button>("Weapon cycle button");
            cycleBtn.transform.SetParent(hud.transform, false);
            hud.WeaponCycleButton = cycleBtn;

            var shieldBtn = Make<Button>("Shield button");
            shieldBtn.transform.SetParent(hud.transform, false);
            hud.ShieldButton = shieldBtn;

            var empBtn = Make<Button>("EMP button");
            empBtn.transform.SetParent(hud.transform, false);
            hud.EmpButton = empBtn;

            hud.Bind();

            Assert.That(bulletBtn.gameObject.activeSelf, Is.False, "Bullet button must be hidden");
            Assert.That(cycleBtn.gameObject.activeSelf, Is.False, "Weapon cycle button must be hidden");
            Assert.That(shieldBtn.gameObject.activeSelf, Is.False, "Shield button must be hidden");
            Assert.That(empBtn.gameObject.activeSelf, Is.False, "Emp button must be hidden");
        }
    }
}
