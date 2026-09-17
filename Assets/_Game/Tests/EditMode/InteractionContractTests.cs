using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            session.Player.gameObject.AddComponent<CircleCollider2D>();
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
            foreach (var cue in Object.FindObjectsByType<InteractionFeedbackCue>(FindObjectsSortMode.None))
                if (cue) Object.DestroyImmediate(cue.gameObject);
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [TestCase(InteractionKind.X, "-20 HP")]
        [TestCase(InteractionKind.X_Energy, "-20 NĂNG LƯỢNG")]
        [TestCase(InteractionKind.Y, "GIẢM TỐC CHẠY")]
        [TestCase(InteractionKind.Y_Shield, "PHÁ KHIÊN")]
        [TestCase(InteractionKind.Z, "+20 HP")]
        [TestCase(InteractionKind.Z_Speed, "TĂNG TỐC CHẠY")]
        public void Activation_EmitsTypedAudioAndDetachedAnimatedWorldCue(InteractionKind kind, string message)
        {
            var session = MakeSession(out _, out _);
            var audio = Make<AudioService>("Audio");
            session.Audio = audio;
            audio.Session = session;
            audio.Bind();
            var interaction = MakeInteraction(session, kind);
            interaction.transform.position = new Vector3(2f, -1f, 0f);
            AudioClip played = null;
            audio.SfxPlayed += clip => played = clip;

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(played, Is.EqualTo(audio.GetInteractionClip(kind)));

            var cue = Object.FindFirstObjectByType<InteractionFeedbackCue>();
            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.Kind, Is.EqualTo(kind));
            Assert.That(cue.Message, Is.EqualTo(message));
            Assert.That(cue.transform.position, Is.EqualTo(interaction.transform.position));

            var label = cue.GetComponentInChildren<TextMesh>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo(message));
            Assert.That(label.color, Is.EqualTo(InteractionFeedbackCue.ColorFor(kind)));

            var initialPosition = label.transform.position;
            var initialScale = cue.transform.localScale;
            var initialAlpha = label.color.a;
            cue.Advance(cue.Duration * .5f);

            Assert.That(cue.transform.localScale.x, Is.GreaterThan(initialScale.x));
            Assert.That(label.transform.position.y, Is.GreaterThan(initialPosition.y));
            Assert.That(label.color.a, Is.LessThan(initialAlpha));

            cue.Advance(cue.Duration * .5f);
            Assert.That(cue == null, Is.True);
        }

        [Test]
        public void X_ReducesHealthOnlyAndConsumes()
        {
            var session = MakeSession(out _, out _);
            var interaction = MakeInteraction(session, InteractionKind.X);

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(session.Player.HP, Is.EqualTo(80));
            Assert.That(session.Player.Armor, Is.EqualTo(100));
            Assert.That(interaction.IsConsumed, Is.True);
        }

        [Test]
        public void X_Energy_ReducesEnergyOnlyAndConsumes()
        {
            var session = MakeSession(out _, out _);
            var interaction = MakeInteraction(session, InteractionKind.X_Energy);

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(session.Player.HP, Is.EqualTo(100));
            Assert.That(session.Player.Armor, Is.EqualTo(80));
            Assert.That(interaction.IsConsumed, Is.True);
        }

        [Test]
        public void Y_SlowsPlayerOnly()
        {
            var session = MakeSession(out var defense, out var effects);
            var interaction = MakeInteraction(session, InteractionKind.Y);
            defense.TryActivateShield();

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(2).Within(.001));
            Assert.That(defense.ShieldActive, Is.True);
            Assert.That(interaction.IsConsumed, Is.False);
        }

        [Test]
        public void Y_Shield_BreaksShieldOnlyAndConsumes()
        {
            var session = MakeSession(out var defense, out var effects);
            var interaction = MakeInteraction(session, InteractionKind.Y_Shield);
            defense.TryActivateShield();

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(defense.ShieldActive, Is.False);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(4).Within(.001));
            Assert.That(interaction.IsConsumed, Is.True);
        }

        [Test]
        public void Z_HealsHealthOnly()
        {
            var session = MakeSession(out _, out var effects);
            session.Player.ApplyDamage(40f);
            Assert.That(session.Player.HP, Is.EqualTo(60f));

            var interaction1 = MakeInteraction(session, InteractionKind.Z);
            Assert.That(interaction1.ApplyTo(session.Player), Is.True);
            Assert.That(session.Player.HP, Is.EqualTo(80f));
            Assert.That(effects.CurrentSpeed, Is.EqualTo(4).Within(.001));

            var interaction2 = MakeInteraction(session, InteractionKind.Z);
            Assert.That(interaction2.ApplyTo(session.Player), Is.True);
            Assert.That(session.Player.HP, Is.EqualTo(100f));
            Assert.That(effects.CurrentSpeed, Is.EqualTo(4).Within(.001));
        }

        [Test]
        public void Z_Speed_BoostsSpeedOnlyAndConsumes()
        {
            var session = MakeSession(out _, out var effects);
            var interaction = MakeInteraction(session, InteractionKind.Z_Speed);

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(6).Within(.001));
            Assert.That(session.Player.HP, Is.EqualTo(100f));
            Assert.That(interaction.IsConsumed, Is.True);
            effects.Advance(4);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(4).Within(.001));
        }

        [Test]
        public void Z_IncreasesCurrentHealthFromFull()
        {
            var session = MakeSession(out _, out _);
            Assert.That(session.Player.HP, Is.EqualTo(100f));

            var interaction = MakeInteraction(session, InteractionKind.Z);
            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(session.Player.HP, Is.EqualTo(120f));
        }
    }
}
