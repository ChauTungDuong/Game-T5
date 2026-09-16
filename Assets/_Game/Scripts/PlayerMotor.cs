using UnityEngine;
namespace CoreGuard
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public GameSession Session;
        public Transform Turret;
        public Vector2 SpawnPosition = new Vector2(-3, 0);
        public Vector2 MinBounds = new Vector2(-12f, -4.5f);
        public Vector2 MaxBounds = new Vector2(12f, 4.5f);
        public StatusEffects Effects;
        public float BaseSpeed = 4f;
        public float CurrentSpeed => Effects ? Effects.CurrentSpeed : BaseSpeed;
        public static Vector2 NormalizeMove(Vector2 input) => Vector2.ClampMagnitude(input, 1);
        private Rigidbody2D body;
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (!Effects) Effects = GetComponent<StatusEffects>();
            UpdateBoundsFromCamera();
        }

        public void UpdateBoundsFromCamera()
        {
            var cam = Camera.main;
            if (cam && cam.orthographic)
            {
                var vertExtent = cam.orthographicSize;
                var horzExtent = vertExtent * cam.aspect;
                float padding = 0.5f;
                MinBounds = new Vector2(-horzExtent + padding, -vertExtent + padding);
                MaxBounds = new Vector2(horzExtent - padding, vertExtent - padding);
            }
        }

        public void Step(Vector2 movement, Vector2 aimWorld, float delta)
        {
            if (Session && Session.State != MatchState.Playing || delta <= 0) return;
            if (!body) Awake();
            UpdateBoundsFromCamera();
            var next = body.position + NormalizeMove(movement) * (CurrentSpeed * delta);
            next.x = Mathf.Clamp(next.x, MinBounds.x, MaxBounds.x);
            next.y = Mathf.Clamp(next.y, MinBounds.y, MaxBounds.y);
            body.MovePosition(next);
            var aim = aimWorld - body.position;
            if (Turret && aim.sqrMagnitude > .000001f)
                Turret.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
        }
        public void ResetPosition()
        {
            var body = GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.position = SpawnPosition;
            transform.position = SpawnPosition;
            Physics2D.SyncTransforms();
        }
    }
}
