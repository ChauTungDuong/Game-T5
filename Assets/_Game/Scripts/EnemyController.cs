using UnityEngine;
namespace CoreGuard
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class EnemyController : MonoBehaviour, IDamageable
    {
        public GameSession Session;
        public CoreHealth Core;
        public PlayerStats Player;
        public Projectile EnemyProjectilePrefab;
        public float AttackRange = 7f;
        public float AttackInterval = 1.5f;
        public float EnemyShotDamage = 10f;
        public float EnemyShotSpeed = 7f;
        public float HP { get; private set; }
        public const float MaxHP = 60f;
        public bool IsAlive => HP > 0 && !retired;
        public bool IsStunned => stunRemaining > 0;
        private bool retired;
        private Rigidbody2D body;
        private SpriteRenderer bodyRenderer;
        private Color bodyColor;
        private LineRenderer healthBack;
        private LineRenderer healthFill;
        private float hitFlashRemaining;
        private float shootRemaining;
        private float stunRemaining;
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            GetComponent<CircleCollider2D>().isTrigger = true;
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bodyRenderer) bodyColor = bodyRenderer.color;
            CreateHealthBar();
        }
        public void Initialize(GameSession session, CoreHealth core)
        {
            Session = session; Core = core; Player = session ? session.Player : null;
            EnemyProjectilePrefab = session ? session.EnemyProjectilePrefab : null;
            HP = MaxHP; retired = false; shootRemaining = .6f; stunRemaining = 0;
            hitFlashRemaining = 0;
            RefreshHealthBar();
        }
        public void Step(float delta)
        {
            if (!IsAlive || Session.State != MatchState.Playing || delta <= 0) return;
            if (!body) Awake();
            if (stunRemaining > 0)
            {
                stunRemaining = Mathf.Max(0, stunRemaining - delta);
                return;
            }

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

            TryShoot(delta);
            body.MovePosition(Vector2.MoveTowards(body.position, target, travel));
        }

        private void LateUpdate()
        {
            if (hitFlashRemaining > 0)
            {
                hitFlashRemaining -= Time.deltaTime;
                if (hitFlashRemaining <= 0 && bodyRenderer) bodyRenderer.color = bodyColor;
            }
            RefreshHealthBar();
        }

        public void Stun(float seconds)
        {
            if (!IsAlive || seconds <= 0 || float.IsNaN(seconds)) return;
            stunRemaining = Mathf.Max(stunRemaining, seconds);
        }

        private void TryShoot(float delta)
        {
            if (!Player || !EnemyProjectilePrefab || Player.HP <= 0) return;
            if (Vector2.Distance(body.position, Player.transform.position) > AttackRange) return;
            shootRemaining -= delta;
            if (shootRemaining > 0) return;

            var direction = (Vector2)Player.transform.position - body.position;
            if (direction.sqrMagnitude <= .000001f) return;
            var projectile = Instantiate(EnemyProjectilePrefab, body.position, Quaternion.identity, Session.transform);
            projectile.InitializeEnemyShot(Session, direction, EnemyShotDamage, EnemyShotSpeed, 4);
            shootRemaining = AttackInterval;
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
            hitFlashRemaining = .12f;
            if (bodyRenderer) bodyRenderer.color = Color.white;
            RefreshHealthBar();
            if (HP <= 0) Retire();
        }
        private void CreateHealthBar()
        {
            if (!Application.isPlaying) return;
            healthBack = CreateBar("Enemy health background", new Color(.04f, .06f, .08f, .9f), .1f);
            healthFill = CreateBar("Enemy health", new Color(.2f, .95f, .35f, 1f), .12f);
        }

        private LineRenderer CreateBar(string name, Color color, float width)
        {
            var barObject = new GameObject(name);
            barObject.transform.SetParent(transform, false);
            barObject.transform.localPosition = new Vector3(0, .72f, 0);
            var bar = barObject.AddComponent<LineRenderer>();
            bar.useWorldSpace = false;
            bar.positionCount = 2;
            bar.widthMultiplier = width;
            bar.startColor = color;
            bar.endColor = color;
            bar.sortingOrder = 6;
            bar.SetPosition(0, new Vector3(-.55f, 0, 0));
            bar.SetPosition(1, new Vector3(.55f, 0, 0));
            return bar;
        }

        private void RefreshHealthBar()
        {
            if (!healthFill) return;
            var ratio = Mathf.Clamp01(HP / MaxHP);
            healthFill.SetPosition(1, new Vector3(-.55f + 1.1f * ratio, 0, 0));
            healthBack.enabled = IsAlive;
            healthFill.enabled = IsAlive;
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
