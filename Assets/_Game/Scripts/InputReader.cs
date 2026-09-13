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
        private InputAction move, aim, fire, pause, retry;
        public Vector2 Move => Session && Session.State == MatchState.Playing && move != null
            ? PlayerMotor.NormalizeMove(move.ReadValue<Vector2>()) : Vector2.zero;
        public Vector2 AimWorld => WorldCamera && aim != null
            ? (Vector2)WorldCamera.ScreenToWorldPoint(aim.ReadValue<Vector2>()) : Vector2.zero;
        public bool FireHeld => Session && Session.State == MatchState.Playing && fire != null && fire.IsPressed()
            && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject());
        private void OnEnable() { if (Actions) BindActions(); }
        private void OnDisable() { gameplay?.Disable(); }
        private void Update() { ReadCommands(); }
        public void BindActions()
        {
            gameplay = Actions.FindActionMap("Gameplay", true);
            move = gameplay.FindAction("Move", true); aim = gameplay.FindAction("Aim", true);
            fire = gameplay.FindAction("Fire", true); pause = gameplay.FindAction("Pause", true);
            retry = gameplay.FindAction("Retry", true);
            gameplay.Enable();
        }
        public void ReadCommands()
        {
            if (!Session || gameplay == null) return;
            if (pause.WasPressedThisFrame()) Session.TogglePause();
            if (retry.WasPressedThisFrame()) Session.Retry();
        }
    }
}
