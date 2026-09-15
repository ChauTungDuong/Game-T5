using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.Editor
{
    public sealed class CombatContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private GameSession MakeSession()
        {
            var session = Make<GameSession>("Session");
            session.enabled = false;
            session.Player = Make<PlayerStats>("Player");
            session.Player.gameObject.AddComponent<CircleCollider2D>();
            session.Core = Make<CoreHealth>("Core");
            session.Motor = session.Player.gameObject.AddComponent<PlayerMotor>();
            session.Motor.Session = session;
            session.Spawner = Make<EnemySpawner>("Spawner");
            session.Spawner.Session = session;
            session.Spawner.Core = session.Core;
            session.Spawner.enabled = false;
            session.Initialize();
            session.StartMatch();
            return session;
        }

        private EnemyController MakeEnemy(GameSession session, string name, Vector2 position)
        {
            var enemy = Make<EnemyController>(name);
            enemy.Initialize(session, session.Core);
            enemy.transform.position = position;
            enemy.GetComponent<Rigidbody2D>().position = position;
            Physics2D.SyncTransforms();
            return enemy;
        }

        private WeaponController MakeWeapon(GameSession session, out Projectile projectilePrefab, out Mine minePrefab)
        {
            var weapon = Make<WeaponController>("Weapons");
            weapon.Session = session;
            weapon.Muzzle = weapon.transform;
            projectilePrefab = Make<Projectile>("Projectile prefab");
            projectilePrefab.gameObject.SetActive(false);
            minePrefab = Make<Mine>("Mine prefab");
            minePrefab.gameObject.SetActive(false);
            weapon.ProjectilePrefab = projectilePrefab;
            weapon.MinePrefab = minePrefab;
            return weapon;
        }

        [TearDown]
        public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void Bullet_DamagesOneEnemyAndProjectileResolvesOnlyOnce()
        {
            var session = MakeSession();
            var enemy = MakeEnemy(session, "Enemy", new Vector2(1, 0));
            var weapon = MakeWeapon(session, out _, out _);

            Assert.That(weapon.TryFire(Vector2.right), Is.True);
            var bullet = session.GetComponentsInChildren<Projectile>(true).Single(projectile => projectile.IsLive);
            bullet.ResolveAgainst(enemy.GetComponent<CircleCollider2D>());
            bullet.ResolveAgainst(enemy.GetComponent<CircleCollider2D>());

            Assert.That(enemy.HP, Is.EqualTo(30));
            Assert.That(bullet.IsLive, Is.False);
            Assert.That(weapon.TryFire(Vector2.right), Is.False, "Bullet cooldown must reject an immediate second shot.");
            weapon.Advance(.2f);
            Assert.That(weapon.TryFire(Vector2.right), Is.True);
            var secondBullet = session.GetComponentsInChildren<Projectile>(true).Single(projectile => projectile.IsLive);
            secondBullet.ResolveAgainst(enemy.GetComponent<CircleCollider2D>());
            Assert.That(enemy.HP, Is.Zero, "Two standard bullets should defeat a 60 HP enemy.");
        }

        [Test]
        public void Projectile_IgnoresItsOwnColliderInsteadOfRetiringAtSpawn()
        {
            var session = MakeSession();
            var weapon = MakeWeapon(session, out _, out _);

            Assert.That(weapon.TryFire(Vector2.right), Is.True);
            var bullet = session.GetComponentsInChildren<Projectile>(true).Single(projectile => projectile.IsLive);

            Assert.That(bullet.ResolveAgainst(bullet.GetComponent<CircleCollider2D>()), Is.False);
            Assert.That(bullet.IsLive, Is.True);
        }

        [Test]
        public void Rocket_DamagesEachEnemyInRadiusOnce()
        {
            var session = MakeSession();
            var first = MakeEnemy(session, "Enemy A", new Vector2(1, 0));
            var second = MakeEnemy(session, "Enemy B", new Vector2(1, .5f));
            var weapon = MakeWeapon(session, out _, out _);
            weapon.Select(WeaponKind.Rocket);

            Assert.That(weapon.TryFire(Vector2.right), Is.True);
            var rocket = session.GetComponentsInChildren<Projectile>(true).Single(projectile => projectile.IsLive);
            rocket.ResolveAgainst(first.GetComponent<CircleCollider2D>());

            Assert.That(first.HP, Is.EqualTo(25));
            Assert.That(second.HP, Is.EqualTo(25));
            Assert.That(rocket.IsLive, Is.False);
        }

        [Test]
        public void Mine_DoesNotTriggerBeforeArmingAndDamagesEnemyAfterward()
        {
            var session = MakeSession();
            var enemy = MakeEnemy(session, "Enemy", new Vector2(.5f, 0));
            var weapon = MakeWeapon(session, out _, out _);
            weapon.Select(WeaponKind.Mine);

            Assert.That(weapon.TryFire(Vector2.zero), Is.True);
            var mine = session.GetComponentsInChildren<Mine>(true).Single();
            Assert.That(mine.TryDetonate(enemy), Is.False);
            mine.Advance(.5f);
            Assert.That(mine.IsArmed, Is.True);
            Assert.That(mine.TryDetonate(enemy), Is.True);
            Assert.That(enemy.HP, Is.EqualTo(10));
        }

        [Test]
        public void Mine_LimitReportsFeedbackWithoutCreatingAnotherMineOrCooldown()
        {
            var session = MakeSession();
            var weapon = MakeWeapon(session, out _, out _);
            weapon.Select(WeaponKind.Mine);
            weapon.MaxMines = 1;
            string rejection = null;
            weapon.ActionRejected += message => rejection = message;

            Assert.That(weapon.TryFire(Vector2.zero), Is.True);
            weapon.Advance(weapon.MineCooldown);

            Assert.That(weapon.TryFire(Vector2.zero), Is.False);
            Assert.That(rejection, Is.EqualTo("MINE LIMIT 1/1"));
            Assert.That(weapon.LivingMineCount, Is.EqualTo(1));
            Assert.That(weapon.CooldownRemaining, Is.EqualTo(0).Within(.001));
        }

        [Test]
        public void Enemy_FiresProjectileThatUsesArmorFirstDamage()
        {
            var session = MakeSession();
            session.Player.transform.position = Vector2.zero;
            session.Player.GetComponent<Rigidbody2D>().position = Vector2.zero;
            Physics2D.SyncTransforms();
            var playerCollider = session.Player.GetComponent<CircleCollider2D>();
            var enemyProjectilePrefab = Make<Projectile>("Enemy projectile prefab");
            enemyProjectilePrefab.gameObject.SetActive(false);
            session.EnemyProjectilePrefab = enemyProjectilePrefab;
            var enemy = MakeEnemy(session, "Enemy", new Vector2(5, 0));
            enemy.Step(2.01f);

            var shot = session.GetComponentsInChildren<Projectile>(true).Single(projectile => projectile.IsLive);
            shot.ResolveAgainst(playerCollider);

            Assert.That(session.Player.HP, Is.EqualTo(100));
            Assert.That(session.Player.Armor, Is.EqualTo(40));
        }

        // Break caught: a successful enemy shot has no distinct firing feedback.
        [Test]
        public void Enemy_FiringFlashesOrangeInsteadOfUsingTheHitFlash()
        {
            var session = MakeSession();
            session.Player.transform.position = Vector2.zero;
            session.Player.GetComponent<Rigidbody2D>().position = Vector2.zero;
            var enemyProjectilePrefab = Make<Projectile>("Enemy projectile prefab");
            enemyProjectilePrefab.gameObject.SetActive(false);
            session.EnemyProjectilePrefab = enemyProjectilePrefab;
            var enemy = MakeEnemy(session, "Enemy", new Vector2(5, 0));
            var renderer = enemy.gameObject.AddComponent<SpriteRenderer>();

            enemy.Step(.61f);

            Assert.That(renderer.color, Is.EqualTo(new Color(1f, .55f, .15f)));
        }

        [Test]
        public void Enemy_FlashRestoresTheHighestPriorityActiveColorThenBaseColor()
        {
            var session = MakeSession();
            session.Player.transform.position = Vector2.zero;
            session.Player.GetComponent<Rigidbody2D>().position = Vector2.zero;
            var enemyProjectilePrefab = Make<Projectile>("Enemy projectile prefab");
            enemyProjectilePrefab.gameObject.SetActive(false);
            session.EnemyProjectilePrefab = enemyProjectilePrefab;
            var enemy = MakeEnemy(session, "Enemy", new Vector2(5, 0));
            var visual = new GameObject("Authored enemy visual");
            objects.Add(visual);
            visual.transform.SetParent(enemy.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.color = Color.blue;

            enemy.ApplyDamage(1);
            enemy.Step(.61f);
            Assert.That(renderer.color, Is.EqualTo(new Color(1f, .55f, .15f)));

            AdvanceFeedback(enemy, .105f);
            Assert.That(renderer.color, Is.EqualTo(Color.white), "The still-active hit flash must reappear after the fire flash expires.");

            AdvanceFeedback(enemy, .02f);
            Assert.That(renderer.color, Is.EqualTo(Color.blue), "The authored base color must return after both flashes expire.");
        }

        // Break caught: changing the player's position after the shot makes an enemy projectile home.
        [Test]
        public void EnemyShot_KeepsItsCapturedDirectionAfterPlayerMoves()
        {
            var session = MakeSession();
            session.Player.transform.position = Vector2.zero;
            session.Player.GetComponent<Rigidbody2D>().position = Vector2.zero;
            var enemyProjectilePrefab = Make<Projectile>("Enemy projectile prefab");
            enemyProjectilePrefab.gameObject.SetActive(false);
            session.EnemyProjectilePrefab = enemyProjectilePrefab;
            var enemy = MakeEnemy(session, "Enemy", new Vector2(-5, 0));

            enemy.Step(.61f);
            var shot = session.GetComponentsInChildren<Projectile>(true).Single(projectile => projectile.IsLive);
            session.Player.transform.position = new Vector2(0, 4);
            session.Player.GetComponent<Rigidbody2D>().position = new Vector2(0, 4);

            Assert.That(shot.transform.up.x, Is.EqualTo(1f).Within(.0001f));
            Assert.That(shot.transform.up.y, Is.EqualTo(0f).Within(.0001f));
        }

        [Test]
        public void EnemyProjectile_IgnoresEnemiesWithoutRetiring()
        {
            var session = MakeSession();
            var enemy = MakeEnemy(session, "Enemy", new Vector2(1, 0));
            var shot = Make<Projectile>("Enemy shot");
            shot.InitializeEnemyShot(session, Vector2.right, 10, 7, 4);

            Assert.That(shot.ResolveAgainst(enemy.GetComponent<CircleCollider2D>()), Is.False);
            Assert.That(shot.IsLive, Is.True);
        }

        [Test]
        public void EnemyProjectile_DamagesHpAfterArmorIsDepleted()
        {
            var session = MakeSession();
            session.Player.ApplyEnvironmentHit(0, PlayerStats.MaxArmor);
            var shot = Make<Projectile>("Enemy shot");
            shot.InitializeEnemyShot(session, Vector2.right, 10, 7, 4);

            Assert.That(shot.ResolveAgainst(session.Player.GetComponent<CircleCollider2D>()), Is.True);
            Assert.That(session.Player.Armor, Is.Zero);
            Assert.That(session.Player.HP, Is.EqualTo(90));
        }

        // Break caught: the audio service has no observable firing-SFX contract.
        [Test]
        public void AudioService_ExposesSfxPlayedContract()
        {
            Assert.That(typeof(AudioService).GetEvent("SfxPlayed"), Is.Not.Null);
        }

        // Break caught: enemy shots instantiate successfully but do not request the project SFX service.
        [Test]
        public void Enemy_FiringRequestsTheConfiguredAudioServiceSfx()
        {
            var session = MakeSession();
            session.Player.transform.position = Vector2.zero;
            session.Player.GetComponent<Rigidbody2D>().position = Vector2.zero;
            var audio = Make<AudioService>("Audio");
            audio.Session = session;
            session.Audio = audio;
            var firingClip = AudioClip.Create("Enemy firing", 32, 1, 8000, false);
            audio.BulletFire = firingClip;
            var enemyProjectilePrefab = Make<Projectile>("Enemy projectile prefab");
            enemyProjectilePrefab.gameObject.SetActive(false);
            session.EnemyProjectilePrefab = enemyProjectilePrefab;
            var enemy = MakeEnemy(session, "Enemy", new Vector2(5, 0));
            AudioClip played = null;
            audio.SfxPlayed += clip => played = clip;

            try
            {
                enemy.Step(.61f);
                Assert.That(played, Is.SameAs(firingClip));
            }
            finally
            {
                Object.DestroyImmediate(firingClip);
            }
        }


        private static void AdvanceFeedback(EnemyController enemy, float delta)
        {
            var method = typeof(EnemyController).GetMethod("AdvanceFeedback", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "EnemyController needs a deterministic feedback lifecycle seam.");
            method.Invoke(enemy, new object[] { delta });
        }
    }
}
