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
        public Vector2 Move => Session && Session.State == MatchState.Playing && move != null
            ? PlayerMotor.NormalizeMove(move.ReadValue<Vector2>()) : Vector2.zero;
        public Vector2 AimWorld => WorldCamera && aim != null
            ? (Vector2)WorldCamera.ScreenToWorldPoint(aim.ReadValue<Vector2>()) : Vector2.zero;
        public bool FireHeld => Session && Session.State == MatchState.Playing && fire != null && fire.IsPressed()
            && PointerAllowsGameplay;
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
            if (pause.WasPressedThisFrame()) Session.TogglePause();
            if (retry.WasPressedThisFrame()) Session.Retry();
            if (demoToggle != null && demoToggle.WasPressedThisFrame() && Session.Demo) Session.Demo.Toggle();
            if (Session.State != MatchState.Playing) return;
            if (shield != null && shield.WasPressedThisFrame() && Session.Defense) Session.Defense.TryActivateShield();
            if (emp != null && emp.WasPressedThisFrame() && Session.Defense) Session.Defense.TryActivateEmp();
            if (!Session.Weapon) return;
            if (weapon1 != null && weapon1.WasPressedThisFrame()) Session.Weapon.Select(WeaponKind.Bullet);
            if (weapon2 != null && weapon2.WasPressedThisFrame()) Session.Weapon.Select(WeaponKind.Rocket);
            if (weapon3 != null && weapon3.WasPressedThisFrame()) Session.Weapon.Select(WeaponKind.Mine);
            Session.Weapon.ProcessInput(
                fire != null && fire.IsPressed() && PointerAllowsGameplay,
                fire != null && fire.WasPressedThisFrame() && PointerAllowsGameplay,
                AimWorld);
        }

        private static bool PointerAllowsGameplay =>
            !(EventSystem.current && EventSystem.current.IsPointerOverGameObject());
    }
}
