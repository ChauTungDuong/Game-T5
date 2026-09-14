using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CoreGuard.Tests.PlayMode
{
    public sealed class HealthBarPresentationTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            objects.Add(gameObject);
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        private GameSession Session()
        {
            var session = Make<GameSession>("Session");
            session.Player = Make<PlayerStats>("Player");
            session.Core = Make<CoreHealth>("Core");
            session.Motor = session.Player.gameObject.AddComponent<PlayerMotor>();
            session.Motor.Session = session;
            session.Spawner = Make<EnemySpawner>("Spawner");
            session.Spawner.enabled = false;
            session.Initialize();
            return session;
        }

        [TearDown]
        public void Cleanup()
        {
            for (var index = objects.Count - 1; index >= 0; index--)
                if (objects[index]) Object.DestroyImmediate(objects[index]);
            objects.Clear();
        }

        // Break caught: a Core bar added after CoreHealth initialization starts empty and misses future health changes.
        [UnityTest]
        public IEnumerator CoreHealthBar_UsesExistingCoreHealthWhenCreatedAfterCoreInitialization()
        {
            var core = Make<CoreHealth>("Core");
            core.ResetHealth();
            var bar = core.gameObject.AddComponent<WorldHealthBar>();
            bar.FullColor = Color.cyan;
            core.HealthBar = bar;

            yield return null;

            Assert.That(bar.FillRatio, Is.EqualTo(1f));
            core.ApplyDamage(51f);
            Assert.That(bar.FillRatio, Is.EqualTo(.49f));
            core.ResetHealth();
            Assert.That(bar.FillRatio, Is.EqualTo(1f));
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.cyan));
        }

        // Break caught: live player health, armor, and coin values stop using the compact current/max HUD format.
        [Test]
        public void Hud_CompactStatsShowsInitialAndDamagedCurrentMaxValues()
        {
            var session = Session();
            var hud = Make<HudPresenter>("HUD");
            hud.Session = session;
            hud.StatsText = Make<Text>("Stats");
            hud.CoreText = Make<Text>("Core");
            hud.TimerText = Make<Text>("Timer");
            hud.StateText = Make<Text>("State");
            hud.ResultText = Make<Text>("Result");
            hud.StartPanel = Make<Transform>("Start").gameObject;
            hud.PausePanel = Make<Transform>("Pause").gameObject;
            hud.ResultPanel = Make<Transform>("Result panel").gameObject;
            hud.StartButton = Make<Button>("Start button");
            hud.ResumeButton = Make<Button>("Resume button");
            hud.RetryButton = Make<Button>("Retry button");

            hud.Bind();
            Assert.That(hud.StatsText.text, Is.EqualTo("PLAYER HP 100/100\nARMOR 50/50\nCOINS 0"));

            session.Player.ApplyDamage(60f);
            session.Player.AddCoins(7);
            Assert.That(hud.StatsText.text, Is.EqualTo("PLAYER HP 90/100\nARMOR 0/50\nCOINS 7"));
        }
    }
}
