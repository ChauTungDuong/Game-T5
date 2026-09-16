using System.Collections.Generic;
using UnityEngine;
namespace CoreGuard
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class EnemyController : MonoBehaviour, IDamageable
    {
        private static readonly HashSet<EnemyController> Active = new HashSet<EnemyController>();
        public static IReadOnlyCollection<EnemyController> ActiveEnemies => Active;
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
        private static readonly Color FireFlashColor = new Color(1f, .55f, .15f);
        private bool retired;
        private Rigidbody2D body;
        private SpriteRenderer bodyRenderer;
        private Color bodyColor;
        private bool bodyColorCaptured;
        private LineRenderer healthBack;
        private LineRenderer healthFill;
        private SpriteRenderer healthBadge;
        private float hitFlashRemaining;
        private float fireFlashRemaining;
        private float shootRemaining;
        private float stunRemaining;
        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);
        private void OnDestroy() => Active.Remove(this);
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            GetComponent<CircleCollider2D>().isTrigger = true;
            CacheBodyRenderer();
            CreateHealthBar();
        }
        public void Initialize(GameSession session, CoreHealth core)
        {
            Active.Add(this);
            Session = session; Core = core; Player = session ? session.Player : null;
            EnemyProjectilePrefab = session ? session.EnemyProjectilePrefab : null;
            HP = MaxHP; retired = false; shootRemaining = .6f; stunRemaining = 0;
            hitFlashRemaining = 0; fireFlashRemaining = 0;
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
            AdvanceFeedback(Time.deltaTime);
            RefreshHealthBar();
        }

        private void AdvanceFeedback(float delta)
        {
            hitFlashRemaining = Mathf.Max(0, hitFlashRemaining - Mathf.Max(0, delta));
            fireFlashRemaining = Mathf.Max(0, fireFlashRemaining - Mathf.Max(0, delta));
            if (!bodyRenderer) return;
            bodyRenderer.color = hitFlashRemaining > 0
                ? Color.white
                : fireFlashRemaining > 0
                    ? FireFlashColor
                    : bodyColor;
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
            FlashWhenFiring();
            if (Session.Audio) Session.Audio.PlaySfx(Session.Audio.BulletFire);
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
            CacheBodyRenderer();
            if (bodyRenderer) bodyRenderer.color = Color.white;
            RefreshHealthBar();
            if (HP <= 0) Retire();
        }
        private void CreateHealthBar()
        {
            if (!Application.isPlaying) return;
            healthBack = CreateBar("Enemy health background", new Color(.08f, .08f, .1f, .95f), .15f, 6);
            healthFill = CreateBar("Enemy health", new Color(.95f, .18f, .22f, 1f), .10f, 7);
            var badgeObj = new GameObject("Enemy health badge");
            badgeObj.transform.SetParent(transform, false);
            badgeObj.transform.localPosition = new Vector3(-.55f - .12f, .72f, 0);
            healthBadge = badgeObj.AddComponent<SpriteRenderer>();
            healthBadge.sortingOrder = 8;
            healthBadge.transform.localScale = Vector3.one * .28f;
#if UNITY_EDITOR
            healthBadge.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/badge_heart_red.png");
#endif
        }

        private void FlashWhenFiring()
        {
            fireFlashRemaining = .1f;
            CacheBodyRenderer();
            if (bodyRenderer) bodyRenderer.color = FireFlashColor;
        }

        private void CacheBodyRenderer()
        {
            if (!bodyRenderer) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bodyRenderer && !bodyColorCaptured)
            {
                bodyColor = bodyRenderer.color;
                bodyColorCaptured = true;
            }
        }

        private LineRenderer CreateBar(string name, Color color, float width, int order)
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
            bar.sortingOrder = order;
            var shader = Shader.Find("Sprites/Default");
            if (shader) bar.material = new Material(shader);
            bar.SetPosition(0, new Vector3(-.55f, 0, 0));
            bar.SetPosition(1, new Vector3(.55f, 0, 0));
            return bar;
        }

        private void RefreshHealthBar()
        {
            if (!healthFill || !healthBack) return;
            var ratio = Mathf.Clamp01(HP / MaxHP);
            healthFill.SetPosition(1, new Vector3(-.55f + 1.1f * ratio, 0, 0));
            healthBack.enabled = IsAlive;
            healthFill.enabled = IsAlive && ratio > 0f;
            if (healthBadge) healthBadge.enabled = IsAlive;
        }
        private void Retire()
        {
            retired = true;
            if (healthBadge) healthBadge.enabled = false;
            gameObject.SetActive(false);
            // Destruction is deferred during play, but immediately excluded from simulation.
            if (Application.isPlaying) Destroy(gameObject);
        }
    }
}
