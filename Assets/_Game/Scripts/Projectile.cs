using UnityEngine;

namespace CoreGuard
{
    public enum ProjectileKind
    {
        Bullet,
        Rocket,
        Laser,
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        public GameSession Session { get; private set; }
        public ProjectileKind Kind { get; private set; }
        public bool IsEnemyProjectile { get; private set; }
        public bool IsLive => this && gameObject.activeSelf && !resolved;

        public Sprite RocketSprite;
        public Sprite LaserSprite;
        public Sprite BulletSprite;

        private static Sprite cachedRocketSprite;
        private static Sprite cachedLaserSprite;

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
            var visual = transform.Find("Projectile body");
            if (!visual) return;
            var renderer = visual.GetComponent<SpriteRenderer>();
            if (!renderer) return;

            if (IsEnemyProjectile)
            {
                visual.localScale = Vector3.one * .82f;
                renderer.color = new Color(1f, .12f, .08f);
                return;
            }

            if (Kind == ProjectileKind.Rocket)
            {
                var sprite = RocketSprite;
                if (!sprite)
                {
                    if (!cachedRocketSprite)
                    {
#if UNITY_EDITOR
                        cachedRocketSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Kenney/Tanks/rocket_outline.png");
#endif
                    }
                    sprite = cachedRocketSprite;
                }
                if (sprite) renderer.sprite = sprite;
                visual.localScale = new Vector3(1.3f, 1.3f, 1f);
                renderer.color = Color.white;
            }
            else if (Kind == ProjectileKind.Laser)
            {
                var sprite = LaserSprite;
                if (!sprite)
                {
                    if (!cachedLaserSprite)
                    {
#if UNITY_EDITOR
                        cachedLaserSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Kenney/Tanks/laser_outline.png");
#endif
                    }
                    sprite = cachedLaserSprite;
                }
                if (sprite) renderer.sprite = sprite;
                visual.localScale = new Vector3(1.3f, 1.5f, 1f);
                renderer.color = Color.white;
            }
            else
            {
                if (BulletSprite) renderer.sprite = BulletSprite;
                visual.localScale = Vector3.one;
                renderer.color = new Color(.55f, .95f, 1f);
            }
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

            // Projectiles pass through interactive objects (X, Y, Z), mines, and zone triggers
            if (other.GetComponentInParent<InteractionObject>() ||
                other.GetComponentInParent<ForbiddenZone>() ||
                other.GetComponentInParent<Mine>())
            {
                return false;
            }

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
                    player.ApplyDamage(damage);
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
            Explosion.ApplyAt(transform.position, explosionRadius, damage, Session);
            Retire();
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
