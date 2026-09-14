using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.Editor
{
    public sealed class InteractionContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private GameSession MakeSession(out DefenseController defense, out StatusEffects effects)
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
            session.Spawner.enabled = false;
            defense = session.Player.gameObject.AddComponent<DefenseController>();
            effects = session.Player.gameObject.AddComponent<StatusEffects>();
            session.Defense = defense;
            session.Effects = effects;
            defense.Session = session; defense.Player = session.Player;
            effects.Session = session;
            session.Initialize();
            session.StartMatch();
            return session;
        }

        private InteractionObject MakeInteraction(GameSession session, InteractionKind kind)
        {
            var interaction = Make<InteractionObject>(kind.ToString());
            interaction.Kind = kind;
            interaction.Session = session;
            interaction.Player = session.Player;
            return interaction;
        }

        [TearDown]
        public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void X_AppliesIndependentHpAndArmorLossAndConsumes()
        {
            var session = MakeSession(out _, out _);
            var interaction = MakeInteraction(session, InteractionKind.X);

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(session.Player.HP, Is.EqualTo(80));
            Assert.That(session.Player.Armor, Is.EqualTo(40));
            Assert.That(interaction.IsConsumed, Is.True);
        }

        [Test]
        public void Y_SlowsPlayerAndBreaksShieldAndConsumesAfterActivation()
        {
            var session = MakeSession(out var defense, out var effects);
            var interaction = MakeInteraction(session, InteractionKind.Y);
            defense.TryActivateShield();

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(2).Within(.001));
            Assert.That(defense.ShieldActive, Is.False);
            Assert.That(interaction.IsConsumed, Is.True);
            effects.ApplyBoost(1.5f, 4f);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(3).Within(.001));
            Assert.That(interaction.ApplyTo(session.Player), Is.False);
            Assert.That(effects.SlowRemaining, Is.EqualTo(3).Within(.001));
            effects.Advance(3f);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(6).Within(.001));
            effects.Advance(1f);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(4).Within(.001));
        }

        [Test]
        public void Z_AddsCoinsAndRefreshesNonStackingBoost()
        {
            var session = MakeSession(out _, out var effects);
            var interaction = MakeInteraction(session, InteractionKind.Z);

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(session.Player.Coins, Is.EqualTo(10));
            Assert.That(effects.CurrentSpeed, Is.EqualTo(6).Within(.001));
            Assert.That(interaction.IsConsumed, Is.True);
            effects.Advance(4);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(4).Within(.001));
        }
    }
}
