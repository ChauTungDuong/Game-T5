using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CoreGuard.Tests.Editor
{
    public sealed class InteractionCycleControllerTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private InteractionCycleController MakeController(out InteractionObject x, out InteractionObject y, out InteractionObject z)
        {
            var controller = Make<InteractionCycleController>("Interaction Cycle");
            controller.enabled = false;
            x = Make<InteractionObject>("X");
            y = Make<InteractionObject>("Y");
            z = Make<InteractionObject>("Z");
            var player = Make<PlayerStats>("Player");
            var core = Make<CoreHealth>("Core");
            player.transform.position = new Vector2(5.5f, 2.5f);
            x.Kind = InteractionKind.X;
            y.Kind = InteractionKind.Y;
            z.Kind = InteractionKind.Z;
            x.Player = player;
            y.Player = player;
            z.Player = player;
            controller.X = x;
            controller.Y = y;
            controller.Z = z;
            controller.Player = player;
            controller.Core = core;
            controller.Seed = 17;
            return controller;
        }

        [TearDown]
        public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void ResetCycle_StartsAllInteractionsVisibleAtDeterministicBoundedPositions()
        {
            var controller = MakeController(out var x, out var y, out var z);
            controller.ResetCycle();
            var first = new[] { x.transform.position, y.transform.position, z.transform.position };
            controller.ResetCycle();
            var second = new[] { x.transform.position, y.transform.position, z.transform.position };

            Assert.That(x.gameObject.activeSelf && y.gameObject.activeSelf && z.gameObject.activeSelf, Is.True);
            for (var i = 0; i < first.Length; i++)
            {
                Assert.That(first[i], Is.EqualTo(second[i]));
                Assert.That(first[i].x, Is.InRange(-6.5f, 6.5f));
                Assert.That(first[i].y, Is.InRange(-3f, 3f));
            }
            Assert.That(Vector2.Distance(x.transform.position, y.transform.position), Is.GreaterThanOrEqualTo(controller.MinInteractionDistance));
            Assert.That(Vector2.Distance(x.transform.position, z.transform.position), Is.GreaterThanOrEqualTo(controller.MinInteractionDistance));
            Assert.That(Vector2.Distance(y.transform.position, z.transform.position), Is.GreaterThanOrEqualTo(controller.MinInteractionDistance));
        }

        [Test]
        public void Advance_HidesAfterFiveSecondsAndRespawnsAfterFiveHiddenSeconds()
        {
            var controller = MakeController(out var x, out _, out _);
            controller.ResetCycle();
            var first = x.transform.position;

            controller.Advance(4.99f);
            Assert.That(x.gameObject.activeSelf, Is.True);
            controller.Advance(.01f);
            Assert.That(x.gameObject.activeSelf, Is.False);
            controller.Advance(4.99f);
            Assert.That(x.gameObject.activeSelf, Is.False);
            controller.Advance(.01f);
            Assert.That(x.gameObject.activeSelf, Is.True);
            Assert.That(x.transform.position, Is.Not.EqualTo(first));
        }

        [Test]
        public void ConsumedInteraction_HidesOnlyThatInteractionAndRespawnsAfterFiveSeconds()
        {
            var controller = MakeController(out var x, out var y, out var z);
            controller.ResetCycle();
            Assert.That(x.ApplyTo(x.Player), Is.True);

            Assert.That(x.gameObject.activeSelf, Is.False);
            Assert.That(y.gameObject.activeSelf, Is.True);
            Assert.That(z.gameObject.activeSelf, Is.True);
            controller.Advance(5f);
            Assert.That(x.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void IsValidPosition_RejectsEdgesPlayerCoreOtherInteractionsAndBlockedGeometry()
        {
            var controller = MakeController(out var x, out var y, out _);
            controller.ResetCycle();

            Assert.That(controller.IsValidPosition(new Vector2(-6.5f, 0), x), Is.False, "The full marker must remain inside camera bounds.");
            Assert.That(controller.IsValidPosition(controller.Player.transform.position, x), Is.False);
            Assert.That(controller.IsValidPosition(controller.Core.transform.position, x), Is.False);
            Assert.That(controller.IsValidPosition(y.transform.position, x), Is.False);

            var wall = new GameObject("Wall");
            objects.Add(wall);
            wall.transform.position = new Vector2(-4f, -2f);
            wall.AddComponent<CircleCollider2D>().radius = .75f;
            Physics2D.SyncTransforms();
            Assert.That(controller.IsValidPosition(wall.transform.position, x), Is.False);
        }

        [Test]
        public void TryFindValidPosition_StopsAfterBoundedAttemptsWhenNoCandidateCanFit()
        {
            var controller = MakeController(out _, out _, out _);
            controller.PositionMin = Vector2.zero;
            controller.PositionMax = Vector2.zero;
            controller.MaxPlacementAttempts = 2;

            Assert.That(controller.TryFindValidPosition(0, out _), Is.False);
        }
    }
}
