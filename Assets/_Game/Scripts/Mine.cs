using UnityEngine;

namespace CoreGuard
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Mine : MonoBehaviour
    {
        public GameSession Session { get; private set; }
        public bool IsArmed { get; private set; }
        public bool IsLive => this && gameObject.activeSelf;

        private CircleCollider2D trigger;
        private float armRemaining;
        private float remaining;
        private float damage;
        private float radius;
        private bool detonated;

        private void Awake()
        {
            var body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .28f;
        }

        public void Initialize(GameSession session, float damageAmount = 50, float explosionRadius = 1.8f)
        {
            Session = session;
            IsArmed = false;
            armRemaining = .5f;
            remaining = 12f;
            damage = damageAmount;
            radius = explosionRadius;
            detonated = false;
            gameObject.SetActive(true);
            if (!trigger) Awake();
        }

        private void FixedUpdate()
        {
            Advance(Time.fixedDeltaTime);
        }

        public void Advance(float delta)
        {
            if (!Session || Session.State != MatchState.Playing || detonated || delta <= 0) return;
            remaining -= delta;
            if (remaining <= 0)
            {
                Retire();
                return;
            }

            if (!IsArmed)
            {
                armRemaining -= delta;
                if (armRemaining <= 0) IsArmed = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other ? other.GetComponentInParent<EnemyController>() : null;
            if (enemy) TryDetonate(enemy);
        }

        public bool TryDetonate(EnemyController triggeringEnemy)
        {
            if (!triggeringEnemy || !IsArmed || !triggeringEnemy.IsAlive || detonated) return false;
            if (Session && triggeringEnemy.Session != Session) return false;
            detonated = true;
            if (Session && Session.Audio) Session.Audio.PlaySfx(Session.Audio.Explosion);
            var presenter = Session && Session.Player
                ? Session.Player.GetComponent<CombatVfxPresenter>()
                : null;
            if (presenter) presenter.SpawnExplosion(transform.position, .23f);
            Explosion.ApplyAt(transform.position, radius, damage, Session);
            Retire();
            return true;
        }

        private void Retire()
        {
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }
}
