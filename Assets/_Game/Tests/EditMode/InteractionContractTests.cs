using System.Collections.Generic;
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

        [TestCase(InteractionKind.X, "-20 HP / -10 ARMOR")]
        [TestCase(InteractionKind.Y, "SLOWED / SHIELD BROKEN")]
        [TestCase(InteractionKind.Z, "+10 COINS / SPEED BOOST")]
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

            var cue = Object.FindFirstObjectByType<InteractionFeedbackCue>();
            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.transform.parent, Is.Null, "the cue must survive while the interaction is hidden");
            Assert.That(cue.Kind, Is.EqualTo(kind));
            Assert.That(cue.Message, Is.EqualTo(message));
            Assert.That(cue.GetComponentInChildren<TextMesh>().text, Is.EqualTo(message));
            Assert.That(cue.GetComponentInChildren<ParticleSystem>(), Is.Not.Null);
            Assert.That(cue.Duration, Is.GreaterThan(0f));
            Assert.That(played, Is.SameAs(audio.GetInteractionClip(kind)));
        }

        [Test]
        public void FeedbackCue_PulsesFloatsFadesAndExpires()
        {
            var cue = InteractionFeedbackCue.Spawn(InteractionKind.X, Vector3.zero);
            var label = cue.GetComponentInChildren<TextMesh>();
            var initialScale = cue.transform.localScale;
            var initialPosition = label.transform.position;
            var initialAlpha = label.color.a;

            cue.Advance(cue.Duration * .5f);

            Assert.That(cue.transform.localScale.x, Is.GreaterThan(initialScale.x));
            Assert.That(label.transform.position.y, Is.GreaterThan(initialPosition.y));
            Assert.That(label.color.a, Is.LessThan(initialAlpha));

            cue.Advance(cue.Duration * .5f);
            Assert.That(cue == null, Is.True);
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
        public void Y_SlowsPlayerAndBreaksShieldAndTriggersOnceUntilPlayerLeaves()
        {
            var session = MakeSession(out var defense, out var effects);
            var interaction = MakeInteraction(session, InteractionKind.Y);
            defense.TryActivateShield();

            Assert.That(interaction.ApplyTo(session.Player), Is.True);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(2).Within(.001));
            Assert.That(defense.ShieldActive, Is.False);
            Assert.That(interaction.IsConsumed, Is.False);
            effects.ApplyBoost(1.5f, 4f);
            Assert.That(effects.CurrentSpeed, Is.EqualTo(3).Within(.001));
            Assert.That(interaction.ApplyTo(session.Player), Is.False);
            typeof(InteractionObject).GetMethod("OnTriggerExit2D", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(interaction, new object[] { session.Player.GetComponent<CircleCollider2D>() });
            Assert.That(interaction.ApplyTo(session.Player), Is.True);
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
