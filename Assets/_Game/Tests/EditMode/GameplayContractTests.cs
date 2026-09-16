using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.Editor
{
    // Regressions: diagonal boost, HP bypassing armor, repeated contact damage,
    // timeout winning over death, paused simulation, spawn backlog, incomplete retry.
    public sealed class GameplayContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
        private GameSession Session()
        {
            var s = Make<GameSession>("Session");
            s.Player = Make<PlayerStats>("Player");
            s.Core = Make<CoreHealth>("Core");
            s.Motor = s.Player.gameObject.AddComponent<PlayerMotor>();
            s.Motor.Session = s;
            s.Spawner = Make<EnemySpawner>("Spawner");
            s.Spawner.Session = s;
            s.Spawner.Core = s.Core;
            s.Spawner.Prefab = Make<EnemyController>("Enemy prefab");
            s.Spawner.Prefab.gameObject.SetActive(false);
            s.Spawner.Gates = new Transform[4];
            var positions = new[] { new Vector2(7, 0), new Vector2(0, 3), new Vector2(-7, 0), new Vector2(0, -3) };
            for (var i = 0; i < 4; i++)
            {
                s.Spawner.Gates[i] = Make<Transform>("Gate " + i);
                s.Spawner.Gates[i].position = positions[i];
            }
            s.Initialize();
            return s;
        }
        [TearDown] public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }
        [Test] public void Movement_NormalizesDiagonalAndPreservesAnalogInput()
        {
            Assert.That(PlayerMotor.NormalizeMove(new Vector2(1, 1)).magnitude, Is.EqualTo(1).Within(.0001));
            Assert.That(PlayerMotor.NormalizeMove(Vector2.right), Is.EqualTo(Vector2.right));
            Assert.That(PlayerMotor.NormalizeMove(new Vector2(.25f, 0)), Is.EqualTo(new Vector2(.25f, 0)));
        }
        [Test] public void Stats_DamageReducesHpDirectlyWithoutConsumingEnergy()
        {
            var stats = Make<PlayerStats>("Stats");
            stats.ResetStats();
            var changes = 0;
            stats.Changed += () => changes++;
            stats.ApplyDamage(60);
            Assert.That(stats.HP, Is.EqualTo(40), "Damage must reduce HP directly.");
            Assert.That(stats.Energy, Is.EqualTo(100), "Damage must not reduce energy.");
            Assert.That(changes, Is.EqualTo(1));
            stats.ApplyDamage(-10);
            Assert.That(stats.HP, Is.EqualTo(40));
            stats.ApplyDamage(200);
            Assert.That(stats.HP, Is.Zero);
        }
        [Test] public void Stats_SkillsConsume20Energy_AndRegenerate5PerSecond()
        {
            var stats = Make<PlayerStats>("Stats");
            stats.ResetStats();
            Assert.That(stats.Energy, Is.EqualTo(100));

            // Using skill reduces energy by 20
            Assert.That(stats.TryConsumeEnergy(20), Is.True);
            Assert.That(stats.Energy, Is.EqualTo(80));

            // Regenerates 5 per second
            stats.Advance(1f);
            Assert.That(stats.Energy, Is.EqualTo(85));

            // Consume rest of energy
            stats.TryConsumeEnergy(85);
            Assert.That(stats.Energy, Is.Zero);
            Assert.That(stats.CanUseSkill(), Is.False);
            Assert.That(stats.TryConsumeEnergy(20), Is.False, "Cannot use skill without at least 20 energy.");

            // Regenerate for 4 seconds -> 20 energy
            stats.Advance(4f);
            Assert.That(stats.Energy, Is.EqualTo(20));
            Assert.That(stats.CanUseSkill(), Is.True);
            Assert.That(stats.TryConsumeEnergy(20), Is.True);
            Assert.That(stats.Energy, Is.Zero);
        }
        [Test]
        public void ArenaBackground_FitsFullScreen()
        {
            var arena = Make<ArenaBackgroundFitter>("Arena");
            var floor = Make<SpriteRenderer>("Floor");
            floor.transform.SetParent(arena.transform);
            arena.Floor = floor.transform;
            arena.Fit();

            Assert.That(floor.transform.localScale.x, Is.GreaterThanOrEqualTo(ArenaBackgroundFitter.DefaultWidth));
            Assert.That(floor.transform.localScale.y, Is.GreaterThanOrEqualTo(ArenaBackgroundFitter.DefaultHeight));
        }
        [Test] public void Enemy_ContactDamagesCoreExactlyOnceAndRetires()
        {
            var s = Session(); s.StartMatch();
            var enemy = Make<EnemyController>("Enemy");
            enemy.Initialize(s, s.Core);
            enemy.TouchCore(); enemy.TouchCore();
            Assert.That(s.Core.HP, Is.EqualTo(80));
            Assert.That(enemy.IsAlive, Is.False);
        }
        [Test] public void Session_PauseFreezesTimerAndResumeContinues()
        {
            var s = Session(); s.StartMatch(); s.Advance(2);
            Assert.That(s.Remaining, Is.EqualTo(88));
            s.TogglePause(); s.Advance(20);
            Assert.That(s.State, Is.EqualTo(MatchState.Paused));
            Assert.That(s.Remaining, Is.EqualTo(88));
            s.TogglePause(); s.Advance(1);
            Assert.That(s.Remaining, Is.EqualTo(87));
        }
        [Test] public void Session_DeathBeatsTimeoutAndAliveTimeoutWins()
        {
            var s = Session(); s.StartMatch();
            s.Core.ApplyDamage(100); s.Advance(90);
            Assert.That(s.State, Is.EqualTo(MatchState.Lost));
            s.Retry();
            s.Spawner.enabled = false;
            s.Advance(90);
            Assert.That(s.State, Is.EqualTo(MatchState.Won));
        }
        [Test] public void Spawner_CapConsumesMissedIntervalsAndRotatesGates()
        {
            var s = Session(); s.StartMatch();
            s.Spawner.Advance(4.9f);
            Assert.That(s.Spawner.Living.Count, Is.Zero);
            s.Spawner.Advance(.1f);
            Assert.That(s.Spawner.Living.Count, Is.EqualTo(1));
            Assert.That((Vector2)s.Spawner.Living[0].transform.position, Is.EqualTo(new Vector2(7, 0)));
            for (var i = 0; i < 5; i++) s.Spawner.Advance(5);
            Assert.That(s.Spawner.Living.Count, Is.EqualTo(6));
            Assert.That(s.Spawner.NextGate, Is.EqualTo(2));
            s.Spawner.Advance(10);
            Assert.That(s.Spawner.Living.Count, Is.EqualTo(6));
            s.Spawner.Living[0].ApplyDamage(60);
            s.Spawner.Advance(.1f);
            Assert.That(s.Spawner.Living.Count, Is.EqualTo(5));
            s.Spawner.Advance(4.9f);
            Assert.That(s.Spawner.Living.Count, Is.EqualTo(6));
            Assert.That(s.Spawner.NextGate, Is.EqualTo(3));
        }
        [Test] public void Retry_CentralResetRestoresStatsCoreClockPositionAndSpawns()
        {
            var s = Session(); s.StartMatch();
            s.Spawner.Advance(5); s.Player.AddCoins(12); s.Player.ApplyDamage(75);
            s.Core.ApplyDamage(100); s.Advance(.02f);
            var resets = 0; s.Reset += () => resets++;
            s.Retry();
            Assert.That(resets, Is.EqualTo(1));
            Assert.That(s.State, Is.EqualTo(MatchState.Playing));
            Assert.That(s.Player.HP, Is.EqualTo(100));
            Assert.That(s.Player.Armor, Is.EqualTo(50));
            Assert.That(s.Player.Coins, Is.Zero);
            Assert.That(s.Core.HP, Is.EqualTo(100));
            Assert.That(s.Remaining, Is.EqualTo(90));
            Assert.That((Vector2)s.Motor.transform.position, Is.EqualTo(new Vector2(-3, 0)));
            Assert.That(s.Spawner.Living.Count, Is.Zero);
            Assert.That(s.Spawner.NextGate, Is.Zero);
            s.Spawner.Advance(4.9f);
            Assert.That(s.Spawner.Living.Count, Is.Zero);
            s.Spawner.Advance(.1f);
            Assert.That(s.Spawner.Living.Count, Is.EqualTo(1));
        }

        [Test]
        public void Retry_FiveSuccessiveRetries_NeverDuplicatesOrLeavesStaleState()
        {
            var s = Session();
            var weapon = s.Player.gameObject.AddComponent<WeaponController>(); weapon.Session = s; s.Weapon = weapon;
            var defense = s.Player.gameObject.AddComponent<DefenseController>(); defense.Session = s; defense.Player = s.Player; s.Defense = defense;
            var effects = s.Player.gameObject.AddComponent<StatusEffects>(); effects.Session = s; s.Effects = effects;
            var audio = s.gameObject.AddComponent<AudioService>(); audio.Session = s; audio.Weapon = weapon; audio.Defense = defense; s.Audio = audio;
            var zone = Make<ForbiddenZone>("Zone"); zone.Session = s; zone.Audio = audio;
            s.StartMatch();

            for (var cycle = 1; cycle <= 5; cycle++)
            {
                s.Player.ApplyDamage(60f);
                s.Player.AddCoins(5 * cycle);
                s.Core.ApplyDamage(40f);
                effects.ApplySlow(0.5f, 3f);
                effects.ApplyBoost(1.5f, 4f);
                defense.TryActivateShield();
                audio.RequestAlert();
                s.Spawner.Advance(5f);

                Assert.That(s.Player.HP, Is.EqualTo(90f));
                Assert.That(s.Player.Armor, Is.Zero);
                Assert.That(s.Player.Coins, Is.GreaterThan(0));
                Assert.That(effects.IsSlowed, Is.True);
                Assert.That(defense.ShieldActive, Is.True);
                Assert.That(audio.PendingAlertJobs, Is.GreaterThanOrEqualTo(1));

                s.Core.ApplyDamage(60f);
                s.Advance(.02f);
                Assert.That(s.State, Is.EqualTo(MatchState.Lost));

                s.Retry();

                Assert.That(s.State, Is.EqualTo(MatchState.Playing));
                Assert.That(s.Player.HP, Is.EqualTo(100));
                Assert.That(s.Player.Armor, Is.EqualTo(50));
                Assert.That(s.Player.Coins, Is.Zero);
                Assert.That(s.Core.HP, Is.EqualTo(100));
                Assert.That(s.Remaining, Is.EqualTo(90));
                Assert.That((Vector2)s.Motor.transform.position, Is.EqualTo(new Vector2(-3, 0)));
                Assert.That(s.Spawner.Living.Count, Is.Zero);
                Assert.That(s.Spawner.NextGate, Is.Zero);
                Assert.That(effects.IsSlowed, Is.False);
                Assert.That(effects.IsBoosted, Is.False);
                Assert.That(effects.CurrentSpeed, Is.EqualTo(4f));
                Assert.That(defense.ShieldActive, Is.False);
                Assert.That(defense.ShieldCooldownRemaining, Is.Zero);
                Assert.That(defense.EmpCooldownRemaining, Is.Zero);
                Assert.That(audio.PendingAlertJobs, Is.Zero);
                Assert.That(zone.OccupantCount, Is.Zero);
                Assert.That(s.GetComponentsInChildren<Projectile>(true).Length, Is.Zero);
                Assert.That(s.GetComponentsInChildren<Mine>(true).Length, Is.Zero);
            }
        }
    }
}
