using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.PlayMode
{
    public sealed class PhysicsGameplayTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private SimulationMode2D previousMode;
        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name); objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
        [SetUp] public void Setup()
        {
            previousMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
        }
        [TearDown] public void Cleanup()
        {
            Physics2D.simulationMode = previousMode;
            foreach (var go in objects) if (go) Object.DestroyImmediate(go);
            objects.Clear();
        }
        private GameSession Session()
        {
            var s = Make<GameSession>("Session"); s.enabled = false;
            s.Player = Make<PlayerStats>("Player");
            s.Core = Make<CoreHealth>("Core");
            s.Motor = s.Player.gameObject.AddComponent<PlayerMotor>(); s.Motor.Session = s;
            s.Motor.Turret = Make<Transform>("Turret"); s.Motor.Turret.SetParent(s.Motor.transform);
            s.Spawner = Make<EnemySpawner>("Spawner");
            s.Spawner.Session = s; s.Spawner.Core = s.Core; s.Spawner.enabled = false;
            s.Initialize(); s.StartMatch();
            return s;
        }
        // A missing MovePosition, diagonal boost or gravity would break literal 8-unit travel.
        [Test] public void Motor_TwoSecondsAxialAndDiagonalTravelBothEightUnits()
        {
            var s = Session(); var motor = s.Motor; var body = motor.GetComponent<Rigidbody2D>();
            motor.MinBounds = new Vector2(-20, -20); motor.MaxBounds = new Vector2(20, 20);
            motor.SpawnPosition = Vector2.zero; motor.ResetPosition();
            for (var i = 0; i < 100; i++) { motor.Step(Vector2.right, Vector2.right, .02f); Physics2D.Simulate(.02f); }
            Assert.That(body.position.x, Is.EqualTo(8).Within(.03));
            Assert.That(body.position.y, Is.Zero.Within(.01));
            motor.ResetPosition();
            for (var i = 0; i < 100; i++) { motor.Step(Vector2.one, Vector2.right, .02f); Physics2D.Simulate(.02f); }
            Assert.That(body.position.magnitude, Is.EqualTo(8).Within(.03));
            Assert.That(body.position.x, Is.EqualTo(body.position.y).Within(.01));
        }
        // Removing state gating, clamping or zero-aim retention must fail this scenario.
        [Test] public void Motor_ClampsBoundsPausesAndRetainsAimAtCenter()
        {
            var s = Session(); var motor = s.Motor; var body = motor.GetComponent<Rigidbody2D>();
            motor.SpawnPosition = new Vector2(7.49f, 3.69f); motor.ResetPosition();
            motor.Step(Vector2.one, body.position + Vector2.up, .02f); Physics2D.Simulate(.02f);
            Assert.That(body.position.x, Is.EqualTo(7.5f).Within(.001));
            Assert.That(body.position.y, Is.EqualTo(3.7f).Within(.001));
            Assert.That(motor.Turret.right.y, Is.EqualTo(1).Within(.001));
            motor.Step(Vector2.zero, body.position, .02f); Physics2D.Simulate(.02f);
            Assert.That(motor.Turret.right.y, Is.EqualTo(1).Within(.001));
            s.TogglePause(); var held = body.position;
            motor.Step(Vector2.left, body.position + Vector2.left, .5f); Physics2D.Simulate(.5f);
            Assert.That(body.position, Is.EqualTo(held));
            Assert.That(motor.Turret.right.y, Is.EqualTo(1).Within(.001));
        }
        // Predicting core contact in the same simulation step prevents a timeout win.
        [Test] public void Enemy_MovesAtOnePointTwoAndFinalTickContactLosesBeforeTimeout()
        {
            var s = Session();
            var enemy = Make<EnemyController>("Enemy"); enemy.Initialize(s, s.Core);
            var body = enemy.GetComponent<Rigidbody2D>(); body.position = new Vector2(3, 0);
            enemy.Step(.5f); Physics2D.Simulate(.5f);
            Assert.That(body.position.x, Is.EqualTo(2.4f).Within(.01));
            s.TogglePause(); enemy.Step(.5f); Physics2D.Simulate(.5f);
            Assert.That(body.position.x, Is.EqualTo(2.4f).Within(.01));
            s.TogglePause(); s.Advance(89.99f);
            s.Core.ApplyDamage(80);
            body.position = new Vector2(1.01f, 0);
            s.Spawner.Living.Add(enemy); s.Spawner.enabled = true;
            s.Advance(.02f);
            Assert.That(s.Core.HP, Is.Zero);
            Assert.That(s.State, Is.EqualTo(MatchState.Lost));
            Assert.That(enemy.IsAlive, Is.False);
        }

        [Test]
        public void Motor_SlowAndBoost_ModifyPhysicalDistanceAccurately()
        {
            var s = Session();
            var effects = s.Player.gameObject.AddComponent<StatusEffects>();
            effects.Session = s;
            s.Motor.Effects = effects;
            var body = s.Motor.GetComponent<Rigidbody2D>();
            s.Motor.MinBounds = new Vector2(-50, -50);
            s.Motor.MaxBounds = new Vector2(50, 50);
            s.Motor.SpawnPosition = Vector2.zero;

            // 1. Baseline: 4.0 units/sec -> 50 steps of 0.02s = 1 sec -> 4 units
            s.Motor.ResetPosition();
            for (var i = 0; i < 50; i++) { s.Motor.Step(Vector2.right, Vector2.right, .02f); Physics2D.Simulate(.02f); }
            Assert.That(body.position.x, Is.EqualTo(4f).Within(.02f));

            // 2. Slowed: 0.5x -> 2.0 units/sec -> 50 steps of 0.02s = 1 sec -> 2 units
            effects.ApplySlow(0.5f, 5f);
            s.Motor.ResetPosition();
            for (var i = 0; i < 50; i++) { s.Motor.Step(Vector2.right, Vector2.right, .02f); Physics2D.Simulate(.02f); }
            Assert.That(body.position.x, Is.EqualTo(2f).Within(.02f));

            // 3. Boosted: 1.5x -> 6.0 units/sec -> 50 steps of 0.02s = 1 sec -> 6 units
            effects.ResetRun();
            effects.ApplyBoost(1.5f, 5f);
            s.Motor.ResetPosition();
            for (var i = 0; i < 50; i++) { s.Motor.Step(Vector2.right, Vector2.right, .02f); Physics2D.Simulate(.02f); }
            Assert.That(body.position.x, Is.EqualTo(6f).Within(.02f));

            // 4. Combined Slow + Boost: 4 * 0.5 * 1.5 = 3.0 units/sec -> 50 steps of 0.02s = 1 sec -> 3 units
            effects.ApplySlow(0.5f, 5f);
            s.Motor.ResetPosition();
            for (var i = 0; i < 50; i++) { s.Motor.Step(Vector2.right, Vector2.right, .02f); Physics2D.Simulate(.02f); }
            Assert.That(body.position.x, Is.EqualTo(3f).Within(.02f));
        }
    }
}
