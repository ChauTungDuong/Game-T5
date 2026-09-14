using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuard
{
    // Owns the timing and placement of the three world interactions. InteractionObject
    // remains responsible for applying an effect; this component only manages its life.
    public sealed class InteractionCycleController : MonoBehaviour
    {
        [Serializable]
        private struct Slot
        {
            public InteractionObject Interaction;
            public float Remaining;
            public bool Visible;
        }

        public GameSession Session;
        public PlayerStats Player;
        public CoreHealth Core;
        public InteractionObject X;
        public InteractionObject Y;
        public InteractionObject Z;
        public float VisibleDuration = 5f;
        public float HiddenDuration = 5f;
        public Vector2 PositionMin = new Vector2(-6.5f, -3f);
        public Vector2 PositionMax = new Vector2(6.5f, 3f);
        public float MinInteractionDistance = 1.2f;
        public float CoreClearance = 1.5f;
        public float PlayerClearance = 1.25f;
        public float PlacementRadius = .45f;
        public int MaxPlacementAttempts = 64;
        public int Seed = 1337;
        public LayerMask BlockedLayers;

        private Slot[] slots;
        private System.Random random;
        private InteractionObject[] bound;

        private void Awake()
        {
            Bind();
        }

        public void Configure(GameSession session, PlayerStats player, CoreHealth core,
            InteractionObject x, InteractionObject y, InteractionObject z)
        {
            Session = session;
            Player = player;
            Core = core;
            X = x;
            Y = y;
            Z = z;
            Bind();
        }

        public void ResetCycle()
        {
            Bind();
            random = new System.Random(Seed);
            EnsureSlots();
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i].Remaining = VisibleDuration;
                slots[i].Visible = false;
                if (slots[i].Interaction) slots[i].Interaction.HideForCycle();
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var interaction = slots[i].Interaction;
                if (!interaction) continue;
                Vector2 position;
                if (!TryFindValidPosition(i, out position))
                {
                    slots[i].Remaining = HiddenDuration;
                    continue;
                }

                interaction.ShowForCycle(position);
                slots[i].Visible = true;
            }
        }

        public void Advance(float delta)
        {
            if (delta <= 0f) return;
            Bind();
            EnsureSlots();
            for (var i = 0; i < slots.Length; i++) AdvanceSlot(i, delta);
        }

        // Candidate generation is deliberately independent from lifecycle timing so
        // placement can be tested with a deterministic seed.
        public Vector2 GenerateCandidate()
        {
            if (random == null) random = new System.Random(Seed);
            var x = Mathf.Lerp(PositionMin.x, PositionMax.x, (float)random.NextDouble());
            var y = Mathf.Lerp(PositionMin.y, PositionMax.y, (float)random.NextDouble());
            return new Vector2(x, y);
        }

        public bool TryFindValidPosition(int slotIndex, out Vector2 position)
        {
            position = default(Vector2);
            EnsureSlots();
            if (slotIndex < 0 || slotIndex >= slots.Length || !slots[slotIndex].Interaction) return false;
            var attempts = Mathf.Max(1, MaxPlacementAttempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var candidate = GenerateCandidate();
                if (!IsValidPosition(candidate, slots[slotIndex].Interaction)) continue;
                position = candidate;
                return true;
            }
            return false;
        }

        public bool IsValidPosition(Vector2 candidate, InteractionObject ignored)
        {
            var min = PositionMin + Vector2.one * PlacementRadius;
            var max = PositionMax - Vector2.one * PlacementRadius;
            if (candidate.x < min.x || candidate.x > max.x || candidate.y < min.y || candidate.y > max.y)
                return false;

            var player = Player ? Player : (Session ? Session.Player : null);
            if (player && Vector2.Distance(candidate, player.transform.position) < PlayerClearance) return false;
            var core = Core ? Core : (Session ? Session.Core : null);
            if (core && Vector2.Distance(candidate, core.transform.position) < CoreClearance) return false;

            for (var i = 0; i < slots.Length; i++)
            {
                var other = slots[i].Interaction;
                if (!other || other == ignored || !slots[i].Visible) continue;
                if (Vector2.Distance(candidate, other.transform.position) < MinInteractionDistance) return false;
            }

            var colliders = BlockedLayers.value != 0
                ? Physics2D.OverlapCircleAll(candidate, PlacementRadius, BlockedLayers)
                : Physics2D.OverlapCircleAll(candidate, PlacementRadius);
            foreach (var collider in colliders)
            {
                if (!collider) continue;
                var interaction = collider.GetComponentInParent<InteractionObject>();
                if (interaction && interaction == ignored) continue;
                return false;
            }
            return true;
        }

        private void AdvanceSlot(int index, float delta)
        {
            var slot = slots[index];
            if (!slot.Interaction) return;
            var remainingDelta = delta;
            var transitionsRemaining = 8;
            while (remainingDelta > 0f && transitionsRemaining-- > 0)
            {
                var duration = Mathf.Max(0f, slot.Remaining);
                if (duration - remainingDelta > .00001f)
                {
                    slot.Remaining = duration - remainingDelta;
                    remainingDelta = 0f;
                    break;
                }

                remainingDelta = Mathf.Max(0f, remainingDelta - duration);
                if (slot.Visible)
                {
                    slot.Visible = false;
                    slot.Remaining = HiddenDuration;
                    slot.Interaction.HideForCycle();
                }
                else
                {
                    Vector2 position;
                    if (TryFindValidPosition(index, out position))
                    {
                        slot.Interaction.ShowForCycle(position);
                        slot.Visible = true;
                        slot.Remaining = VisibleDuration;
                    }
                    else
                    {
                        slot.Remaining = HiddenDuration;
                    }
                }
            }
            slots[index] = slot;
        }

        private void OnInteractionActivated(InteractionObject interaction)
        {
            EnsureSlots();
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Interaction != interaction || !slots[i].Visible) continue;
                slots[i].Visible = false;
                slots[i].Remaining = HiddenDuration;
                interaction.HideForCycle();
                return;
            }
        }

        private void Bind()
        {
            var current = new[] { X, Y, Z };
            if (bound != null && Same(bound, current)) return;
            if (bound != null)
                foreach (var interaction in bound)
                    if (interaction) interaction.Activated -= OnInteractionActivated;
            bound = current;
            foreach (var interaction in bound)
                if (interaction) interaction.Activated += OnInteractionActivated;
            EnsureSlots();
        }

        private void EnsureSlots()
        {
            var current = new[] { X, Y, Z };
            if (slots == null || slots.Length != current.Length)
            {
                slots = new Slot[current.Length];
                for (var i = 0; i < slots.Length; i++) slots[i].Interaction = current[i];
                return;
            }
            for (var i = 0; i < slots.Length; i++) slots[i].Interaction = current[i];
        }

        private static bool Same(IReadOnlyList<InteractionObject> a, IReadOnlyList<InteractionObject> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
