using UnityEngine;
namespace CoreGuard
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class EnemyController : MonoBehaviour, IDamageable
    {
        public GameSession Session;
        public CoreHealth Core;
        public float HP { get; private set; }
        public bool IsAlive => HP > 0 && !retired;
        private bool retired;
        private Rigidbody2D body;
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            GetComponent<CircleCollider2D>().isTrigger = true;
        }
        public void Initialize(GameSession session, CoreHealth core)
        {
            Session = session; Core = core; HP = 60; retired = false;
        }
        public void Step(float delta)
        {
            if (!IsAlive || Session.State != MatchState.Playing || delta <= 0) return;
            if (!body) Awake();
            var target = (Vector2)Core.transform.position;
            var contactRadius = GetComponent<CircleCollider2D>().radius * Mathf.Abs(transform.lossyScale.x)
                + Core.GetComponent<CircleCollider2D>().radius * Mathf.Abs(Core.transform.lossyScale.x);
            var travel = 1.2f * delta;
            // Swept circle arrival is resolved here, before the session evaluates timeout.
            if (Vector2.Distance(body.position, target) <= contactRadius + travel)
            {
                TouchCore();
                return;
            }
            body.MovePosition(Vector2.MoveTowards(body.position, target, travel));
        }
        public void TouchCore()
        {
            if (!IsAlive || Session.State != MatchState.Playing) return;
            Core.ApplyDamage(20);
            Retire();
        }
        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0 || float.IsNaN(amount)) return;
            HP = Mathf.Max(0, HP - amount);
            if (HP <= 0) Retire();
        }
        private void Retire()
        {
            retired = true;
            gameObject.SetActive(false);
            // Destruction is deferred during play, but immediately excluded from simulation.
            if (Application.isPlaying) Destroy(gameObject);
        }
    }
}
