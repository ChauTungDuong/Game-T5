using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CoreGuard
{
    public sealed class InputReader : MonoBehaviour
    {
        public InputActionAsset Actions;
        public GameSession Session;
        public Camera WorldCamera;
        private InputActionMap gameplay;
        private InputAction move, aim, fire, weapon1, weapon2, weapon3, shield, emp, demoToggle, pause, retry;
        public Vector2 VirtualMoveInput { get; set; }
        public Vector2 Move => Session && Session.State == MatchState.Playing
            ? PlayerMotor.NormalizeMove((move != null ? move.ReadValue<Vector2>() : Vector2.zero) + VirtualMoveInput)
            : Vector2.zero;
        public Vector2 AimWorld
        {
            get
            {
                if (!WorldCamera) return Vector2.zero;
                if (TryGetGameplayTouch(out var touchPos, out _, out _))
                {
                    return (Vector2)WorldCamera.ScreenToWorldPoint(touchPos);
                }
                return aim != null ? (Vector2)WorldCamera.ScreenToWorldPoint(aim.ReadValue<Vector2>()) : Vector2.zero;
            }
        }
        public bool FireHeld => Session && Session.State == MatchState.Playing &&
            (TryGetGameplayTouch(out _, out var touchHeld, out _)
                ? touchHeld
                : ((fire != null && fire.IsPressed()) && PointerAllowsGameplay));

        private void OnEnable() { if (Actions) BindActions(); }
        private void OnDisable() { gameplay?.Disable(); }
        private void Update() { ReadCommands(); }
        public void BindActions()
        {
            gameplay = Actions.FindActionMap("Gameplay", true);
            move = gameplay.FindAction("Move", true); aim = gameplay.FindAction("Aim", true);
            fire = gameplay.FindAction("Fire", true); pause = gameplay.FindAction("Pause", true);
            weapon1 = gameplay.FindAction("Weapon1", false); weapon2 = gameplay.FindAction("Weapon2", false);
            weapon3 = gameplay.FindAction("Weapon3", false);
            shield = gameplay.FindAction("Shield", false); emp = gameplay.FindAction("EMP", false);
            demoToggle = gameplay.FindAction("DemoMode", false);
            retry = gameplay.FindAction("Retry", true);
            gameplay.Enable();
        }
        public void ReadCommands()
        {
            if (!Session || gameplay == null) return;
            if (pause != null && pause.WasPressedThisFrame()) Session.TogglePause();
            if (demoToggle != null && demoToggle.WasPressedThisFrame() && Session.Demo) Session.Demo.Toggle();

            var rPressed = (retry != null && retry.WasPressedThisFrame())
                || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);

            if (Session.State == MatchState.Won || Session.State == MatchState.Lost)
            {
                if (rPressed) Session.Retry();
                return;
            }

            if (Session.State != MatchState.Playing) return;

            if (rPressed && Session.Weapon)
            {
                Session.Weapon.CycleNextWeapon();
            }

            if (shield != null && shield.WasPressedThisFrame() && Session.Defense) Session.Defense.TryActivateShield();
            if (emp != null && emp.WasPressedThisFrame() && Session.Defense) Session.Defense.TryActivateEmp();
            if (!Session.Weapon) return;
            if (weapon1 != null && weapon1.WasPressedThisFrame()) Session.Weapon.Select(WeaponKind.Bullet);
            if (weapon2 != null && weapon2.WasPressedThisFrame()) Session.Weapon.Select(WeaponKind.Rocket);
            if (weapon3 != null && weapon3.WasPressedThisFrame()) Session.Weapon.Select(WeaponKind.Laser);

            var hasTouch = TryGetGameplayTouch(out _, out var touchHeld, out var touchPressed);
            var isFireHeld = hasTouch ? touchHeld : ((fire != null && fire.IsPressed()) && PointerAllowsGameplay);
            var isFirePressed = hasTouch ? touchPressed : ((fire != null && fire.WasPressedThisFrame()) && PointerAllowsGameplay);

            Session.Weapon.ProcessInput(
                isFireHeld,
                isFirePressed,
                AimWorld);
        }

        private bool TryGetGameplayTouch(out Vector2 touchPosition, out bool isPressed, out bool wasPressedThisFrame)
        {
            touchPosition = Vector2.zero;
            isPressed = false;
            wasPressedThisFrame = false;

            if (Touchscreen.current == null) return false;

            var touches = Touchscreen.current.touches;
            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var held = touch.press.isPressed;
                var pressed = touch.press.wasPressedThisFrame;
                if (!held && !pressed) continue;

                var touchId = touch.touchId.ReadValue();
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
                {
                    continue;
                }

                touchPosition = touch.position.ReadValue();
                isPressed = held;
                wasPressedThisFrame = pressed;
                return true;
            }

            return false;
        }

        private static bool PointerAllowsGameplay
        {
            get
            {
                if (!EventSystem.current) return true;
                return !EventSystem.current.IsPointerOverGameObject();
            }
        }
    }
}
