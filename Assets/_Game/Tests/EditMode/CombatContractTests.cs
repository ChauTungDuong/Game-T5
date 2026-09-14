using System.Collections.Generic;
using System.Linq;
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
        public void Enemy_FiresProjectileThatUsesArmorFirstDamage()
        {
            var session = MakeSession();
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
    }
}
