using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.Editor
{
    public sealed class HealthBarContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            objects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        [TearDown]
        public void Cleanup()
        {
            for (var index = objects.Count - 1; index >= 0; index--)
                if (objects[index]) Object.DestroyImmediate(objects[index]);
            objects.Clear();
        }

        // Break caught: a shared bar that does not show its value, red fill, or empty state.
        [Test]
        public void WorldHealthBar_UsesRedFillAndReflectsZeroHealth()
        {
            var bar = Make<WorldHealthBar>("Health bar");
            bar.FullColor = Color.red;

            bar.SetHealth(100f, 100f);
            Assert.That(bar.ValueText.text, Is.EqualTo("100/100"));
            Assert.That(bar.FillRatio, Is.EqualTo(1f));
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.red));

            bar.SetHealth(49f, 100f);
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.red));
            bar.SetHealth(24f, 100f);
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.red));

            bar.SetHealth(0f, 100f);
            Assert.That(bar.FillRatio, Is.Zero);
            Assert.That(bar.Fill.enabled, Is.False);
        }

        // Break caught: PlayerStats changes leave the player world bar stale or fail to restore it on reset.
        [Test]
        public void PlayerStats_UpdatesAttachedHealthBarFromChangedValuesAndReset()
        {
            var stats = Make<PlayerStats>("Player");
            var bar = stats.gameObject.AddComponent<WorldHealthBar>();
            bar.FullColor = Color.red;

            stats.ResetStats();
            Assert.That(bar.ValueText.text, Is.EqualTo("100/100"));
            Assert.That(bar.FillRatio, Is.EqualTo(1f));

            stats.ApplyEnvironmentHit(76f, 0f);
            Assert.That(bar.ValueText.text, Is.EqualTo("24/100"));
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.red));

            stats.ResetStats();
            Assert.That(bar.FillRatio, Is.EqualTo(1f));
            Assert.That(bar.Fill.enabled, Is.True);
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.red));
        }

        // Break caught: CoreHealth retains its private implementation or does not update its shared bar immediately.
        [Test]
        public void CoreHealth_UpdatesSharedCyanBarImmediatelyAndRestoresItOnReset()
        {
            var core = Make<CoreHealth>("Core");
            var bar = core.gameObject.AddComponent<WorldHealthBar>();
            bar.FullColor = Color.cyan;
            core.HealthBar = bar;

            core.ResetHealth();
            Assert.That(bar.ValueText.text, Is.EqualTo("100/100"));
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.cyan));

            core.ApplyDamage(51f);
            Assert.That(bar.FillRatio, Is.EqualTo(.49f));
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.cyan));

            core.ResetHealth();
            Assert.That(bar.FillRatio, Is.EqualTo(1f));
            Assert.That(bar.Fill.startColor, Is.EqualTo(Color.cyan));
        }
    }
}
