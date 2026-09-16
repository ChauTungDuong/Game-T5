using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.Editor
{
    public sealed class DemoDirectorContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private GameSession MakeSession(out DemoDirector demo)
        {
            var session = Make<GameSession>("Session");
            session.enabled = false;
            session.Player = Make<PlayerStats>("Player");
            session.Core = Make<CoreHealth>("Core");
            session.Motor = session.Player.gameObject.AddComponent<PlayerMotor>();
            session.Motor.Session = session;
            session.Spawner = Make<EnemySpawner>("Spawner");
            session.Spawner.Session = session;
            session.Spawner.Core = session.Core;
            session.Spawner.Prefab = Make<EnemyController>("Enemy prefab");
            session.Spawner.Prefab.gameObject.SetActive(false);
            session.Spawner.Gates = new Transform[4];

            demo = session.gameObject.AddComponent<DemoDirector>();
            demo.Session = session;
            session.Demo = demo;
            session.Initialize();
            session.StartMatch();
            demo.Bind();
            return session;
        }

        [TearDown]
        public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void Toggle_DisablesAutomaticSpawnsAndScenarioSpawnUsesRealSpawner()
        {
            var session = MakeSession(out var demo);
            demo.Toggle();

            Assert.That(demo.IsDemoMode, Is.True);
            Assert.That(session.Spawner.AutoSpawn, Is.False);
            demo.SpawnZoneEnemy();

            Assert.That(session.Spawner.Living.Count, Is.EqualTo(1));
            Assert.That(session.Spawner.Living[0].Session, Is.SameAs(session));
            Assert.That(Vector2.Distance(session.Spawner.Living[0].transform.position, session.Core.transform.position), Is.GreaterThan(3f));
        }

        [Test]
        public void ResetScenario_ClearsSpawnedEnemiesAndRestoresRunStateWhileKeepingDemoMode()
        {
            var session = MakeSession(out var demo);
            demo.Toggle();
            demo.SpawnEnemyCluster();
            session.Player.AddCoins(10);
            session.Advance(2);

            demo.ResetScenario();

            Assert.That(demo.IsDemoMode, Is.True);
            Assert.That(session.Spawner.AutoSpawn, Is.False);
            Assert.That(session.Spawner.Living, Is.Empty);
            Assert.That(session.Remaining, Is.EqualTo(90));
            Assert.That(session.Player.Coins, Is.Zero);
        }
    }
}
