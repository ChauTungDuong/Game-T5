using UnityEngine;

namespace CoreGuard
{
    public enum ProjectileKind
    {
        Bullet,
        Rocket,
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        public GameSession Session { get; private set; }
        public ProjectileKind Kind { get; private set; }
        public bool IsEnemyProjectile { get; private set; }
        public bool IsLive => this && gameObject.activeSelf && !resolved;

        private Rigidbody2D body;
        private CircleCollider2D hitCollider;
        private Vector2 direction;
        private float speed;
        private float damage;
        private float ttl;
        private float explosionRadius;
        private bool resolved;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            hitCollider = GetComponent<CircleCollider2D>();
            hitCollider.isTrigger = true;
        }

        public void InitializePlayerShot(
            GameSession session,
            Vector2 aimDirection,
            ProjectileKind kind,
            float damageAmount,
            float travelSpeed,
            float lifetime,
            float radius)
        {
            Initialize(session, aimDirection, kind, damageAmount, travelSpeed, lifetime, radius, false);
        }

        public void InitializeEnemyShot(
            GameSession session,
            Vector2 aimDirection,
            float damageAmount,
            float travelSpeed,
            float lifetime)
        {
            Initialize(session, aimDirection, ProjectileKind.Bullet, damageAmount, travelSpeed, lifetime, 0, true);
        }

        private void Initialize(
            GameSession session,
            Vector2 aimDirection,
            ProjectileKind kind,
            float damageAmount,
            float travelSpeed,
            float lifetime,
            float radius,
            bool enemyProjectile)
        {
            Session = session;
            Kind = kind;
            IsEnemyProjectile = enemyProjectile;
            direction = aimDirection.sqrMagnitude > .000001f ? aimDirection.normalized : Vector2.right;
            damage = damageAmount;
            speed = travelSpeed;
            ttl = lifetime;
            explosionRadius = radius;
            resolved = false;
            gameObject.SetActive(true);
            if (!body || !hitCollider) Awake();
            body.position = transform.position;
            // The Kenney projectile sprite is vertical, so its local up axis is
            // the travel direction (not local right).
            transform.up = direction;
            ApplyVisualStyle();
        }

        private void ApplyVisualStyle()
        {
            var bulletVisual = transform.Find("Projectile body");
            var rocketVisual = transform.Find("Rocket body");
            var rocketShot = !IsEnemyProjectile && Kind == ProjectileKind.Rocket;
            if (bulletVisual) bulletVisual.gameObject.SetActive(!rocketShot);
            if (rocketVisual) rocketVisual.gameObject.SetActive(rocketShot);
            var visual = rocketShot && rocketVisual ? rocketVisual : bulletVisual;
            if (!visual) return;
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (!renderer) return;
            visual.localScale = Vector3.one * (IsEnemyProjectile ? .82f : rocketShot ? .2f : 1.0f);
            renderer.color = IsEnemyProjectile ? new Color(1f, .12f, .08f) : Color.white;
        }

        private void FixedUpdate()
        {
            if (!Session || Session.State != MatchState.Playing || resolved) return;

            var delta = Time.fixedDeltaTime;
            ttl -= delta;
            if (ttl <= 0)
            {
                Retire();
                return;
            }

            var travel = speed * delta;
            var castRadius = hitCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), .0001f);
            foreach (var hit in Physics2D.CircleCastAll(body.position, castRadius, direction, travel))
            {
                if (hit.collider == hitCollider) continue;
                if (ResolveAgainst(hit.collider)) return;
            }

            body.MovePosition(body.position + direction * travel);
        }

        public bool ResolveAgainst(Collider2D other)
        {
            if (resolved || !other || other == hitCollider) return false;

            if (IsEnemyProjectile)
            {
                var player = other.GetComponentInParent<PlayerStats>();
                if (player && (!Session || player == Session.Player))
                {
                    if (Session && Session.Defense && Session.Defense.TryBlockProjectile())
                    {
                        Retire();
                        return true;
                    }
                    player.ApplyCombatDamage(damage);
                    Retire();
                    return true;
                }

                if (other.GetComponentInParent<EnemyController>()) return false;
                if (other.GetComponentInParent<CoreHealth>()) return false;
                Retire();
                return true;
            }

            var enemy = other.GetComponentInParent<EnemyController>();
            if (enemy && (!Session || enemy.Session == Session))
            {
                if (Kind == ProjectileKind.Rocket) Detonate();
                else
                {
                    enemy.ApplyDamage(damage);
                    Retire();
                }
                return true;
            }

            // Player weapons never damage the player or the core. Any other
            // collider is treated as arena geometry.
            if (other.GetComponentInParent<PlayerStats>() || other.GetComponentInParent<CoreHealth>()) return false;
            if (Kind == ProjectileKind.Rocket) Detonate();
            else Retire();
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other) => ResolveAgainst(other);

        private void Detonate()
        {
            if (resolved) return;
            resolved = true;
            PlayExplosionFeedback();
            Explosion.ApplyAt(transform.position, explosionRadius, damage, Session);
            Retire();
        }

        private void PlayExplosionFeedback()
        {
            if (Session && Session.Audio) Session.Audio.PlaySfx(Session.Audio.Explosion);
            var presenter = Session && Session.Player
                ? Session.Player.GetComponent<CombatVfxPresenter>()
                : null;
            if (presenter) presenter.SpawnExplosion(transform.position, IsEnemyProjectile ? .16f : .23f);
        }

        private void Retire()
        {
            if (resolved == false) resolved = true;
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }
}
