using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.Editor
{
    public sealed class DefenseContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private GameSession MakeSession(out DefenseController defense)
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
            defense = Make<DefenseController>("Defense");
            defense.Session = session;
            defense.Player = session.Player;
            session.Defense = defense;
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

        private Projectile MakeEnemyShot(GameSession session, Vector2 position)
        {
            var shot = Make<Projectile>("Enemy shot");
            shot.transform.position = position;
            shot.InitializeEnemyShot(session, Vector2.right, 10, 5, 4);
            return shot;
        }

        [TearDown]
        public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void Shield_BlocksThreeProjectilesThenFourthUsesDamageRoute()
        {
            var session = MakeSession(out var defense);
            var playerCollider = session.Player.GetComponent<CircleCollider2D>();
            Assert.That(defense.TryActivateShield(), Is.True);

            for (var i = 0; i < 3; i++)
            {
                var shot = MakeEnemyShot(session, session.Player.transform.position);
                shot.ResolveAgainst(playerCollider);
            }

            Assert.That(defense.ShieldActive, Is.False);
            Assert.That(defense.ShieldRemaining, Is.Zero);
            Assert.That(session.Player.HP, Is.EqualTo(100));
            Assert.That(session.Player.Armor, Is.EqualTo(50));

            var fourth = MakeEnemyShot(session, session.Player.transform.position);
            fourth.ResolveAgainst(playerCollider);
            Assert.That(session.Player.HP, Is.EqualTo(100));
            Assert.That(session.Player.Armor, Is.EqualTo(40));
        }

        [Test]
        public void Emp_StunsOnlyEnemiesInsideRadius()
        {
            var session = MakeSession(out var defense);
            var origin = (Vector2)session.Player.transform.position;
            var near = MakeEnemy(session, "Near", origin + new Vector2(2, 0));
            var far = MakeEnemy(session, "Far", origin + new Vector2(4, 0));
            var foreignSession = Make<GameSession>("Foreign session");
            foreignSession.enabled = false;
            var foreign = MakeEnemy(foreignSession, "Foreign near", origin + new Vector2(1, 0));
            var reportedCount = -1;
            defense.EmpResolved += count => reportedCount = count;

            Assert.That(near.Session, Is.SameAs(session));
            Assert.That(near.IsAlive, Is.True);
            Assert.That(Vector2.Distance(near.transform.position, session.Player.transform.position), Is.LessThanOrEqualTo(defense.EmpRadius));
            Assert.That(EnemyController.ActiveEnemies, Does.Contain(near));
            Assert.That(defense.TryActivateEmp(), Is.True);
            Assert.That(defense.LastEmpAffectedCount, Is.EqualTo(1));
            Assert.That(reportedCount, Is.EqualTo(1));
            Assert.That(near.IsStunned, Is.True);
            Assert.That(far.IsStunned, Is.False);
            Assert.That(foreign.IsStunned, Is.False);
            Assert.That(defense.TryActivateEmp(), Is.False, "EMP cooldown must reject an immediate second pulse.");
        }

        [Test]
        public void Defense_CooldownsAndShieldExpireOnlyDuringPlaying()
        {
            var session = MakeSession(out var defense);
            Assert.That(defense.TryActivateShield(), Is.True);
            session.TogglePause();
            defense.Advance(10);
            Assert.That(defense.ShieldActive, Is.True);
            Assert.That(defense.ShieldRemaining, Is.EqualTo(3).Within(.001));
            session.TogglePause();
            session.Advance(3);
            Assert.That(defense.ShieldActive, Is.False);
            Assert.That(defense.ShieldCooldownRemaining, Is.EqualTo(5).Within(.001));
        }

        [Test]
        public void Defense_PauseFreezesEmpCooldownAndResetClearsAllState()
        {
            var session = MakeSession(out var defense);
            MakeEnemy(session, "Near", new Vector2(1, 0));
            Assert.That(defense.TryActivateShield(), Is.True);
            Assert.That(defense.TryActivateEmp(), Is.True);

            session.TogglePause();
            defense.Advance(20f);
            Assert.That(defense.EmpCooldownRemaining, Is.EqualTo(6f).Within(.001f));
            Assert.That(defense.ShieldRemaining, Is.EqualTo(3f).Within(.001f));

            session.ResetForDemo();
            Assert.That(defense.ShieldActive, Is.False);
            Assert.That(defense.ShieldCooldownRemaining, Is.Zero);
            Assert.That(defense.EmpCooldownRemaining, Is.Zero);
            Assert.That(defense.LastEmpAffectedCount, Is.Zero);
            Assert.That(defense.TryActivateShield(), Is.True);
            Assert.That(defense.TryActivateEmp(), Is.True);
        }
    }
}
