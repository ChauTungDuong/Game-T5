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
        public InteractionObject X_Energy;
        public InteractionObject Y_Shield;
        public InteractionObject Z_Speed;
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

        private void OnEnable() => Bind();
        private void OnDisable() => Unbind();

        public void Configure(GameSession session, PlayerStats player, CoreHealth core,
            InteractionObject x, InteractionObject y, InteractionObject z,
            InteractionObject x2 = null, InteractionObject y2 = null, InteractionObject z2 = null)
        {
            Session = session;
            Player = player;
            Core = core;
            X = x;
            Y = y;
            Z = z;
            X_Energy = x2;
            Y_Shield = y2;
            Z_Speed = z2;
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
            if (VisibleDuration <= 0f && HiddenDuration <= 0f)
            {
                for (var i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].Interaction) continue;
                    slots[i].Visible = false;
                    slots[i].Remaining = 0f;
                    slots[i].Interaction.HideForCycle();
                }
                return;
            }

            var remainingDelta = delta;
            while (remainingDelta > 0f)
            {
                var step = remainingDelta;
                for (var i = 0; i < slots.Length; i++)
                    if (slots[i].Interaction && slots[i].Remaining > 0f)
                        step = Mathf.Min(step, slots[i].Remaining);

                for (var i = 0; i < slots.Length; i++)
                    if (slots[i].Interaction)
                        slots[i].Remaining = Mathf.Max(0f, slots[i].Remaining - step);
                remainingDelta = Mathf.Max(0f, remainingDelta - step);

                var transitioned = false;
                for (var i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].Interaction || slots[i].Remaining > .00001f) continue;
                    TransitionSlot(i);
                    transitioned = true;
                }
                if (step <= 0f && !transitioned) break;
            }
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
            EnsureSlots();
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

        private void TransitionSlot(int index)
        {
            var slot = slots[index];
            if (!slot.Interaction) return;
            if (slot.Visible)
            {
                slot.Visible = false;
                slot.Remaining = Mathf.Max(0f, HiddenDuration);
                slot.Interaction.HideForCycle();
            }
            else
            {
                Vector2 position;
                if (TryFindValidPosition(index, out position))
                {
                    slot.Interaction.ShowForCycle(position);
                    slot.Visible = true;
                    slot.Remaining = Mathf.Max(0f, VisibleDuration);
                }
                else
                {
                    slot.Remaining = Mathf.Max(0f, HiddenDuration);
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

        public void EnsureAllVariants()
        {
            var parent = X ? X.transform.parent : (Y ? Y.transform.parent : (Z ? Z.transform.parent : null));
            if (!parent) return;

            if (!X_Energy)
            {
                var existing = parent.Find("X_Energy");
                if (existing) X_Energy = existing.GetComponent<InteractionObject>();
                else if (X) X_Energy = SpawnVariant(X, "X_Energy", InteractionKind.X_Energy, new Vector2(-5f, -2.2f), new Color(1f, .55f, 0f), "-20 NL");
            }
            if (!Y_Shield)
            {
                var existing = parent.Find("Y_Shield");
                if (existing) Y_Shield = existing.GetComponent<InteractionObject>();
                else if (Y) Y_Shield = SpawnVariant(Y, "Y_Shield", InteractionKind.Y_Shield, new Vector2(7f, -2.2f), new Color(.1f, .65f, 1f), "PHÁ KHIÊN");
            }
            if (!Z_Speed)
            {
                var existing = parent.Find("Z_Speed");
                if (existing) Z_Speed = existing.GetComponent<InteractionObject>();
                else if (Z) Z_Speed = SpawnVariant(Z, "Z_Speed", InteractionKind.Z_Speed, new Vector2(7f, 2.2f), new Color(1f, .85f, .1f), "TĂNG TỐC");
            }
        }

        private InteractionObject SpawnVariant(InteractionObject template, string name, InteractionKind kind, Vector2 position, Color color, string label)
        {
            if (!template) return null;
            var go = Instantiate(template.gameObject, template.transform.parent);
            go.name = name;
            go.transform.localPosition = position;
            var obj = go.GetComponent<InteractionObject>();
            if (obj)
            {
                obj.Kind = kind;
                obj.Session = Session ? Session : template.Session;
                obj.Player = Player ? Player : template.Player;
            }
            var marker = go.transform.Find("Marker")?.GetComponent<SpriteRenderer>();
            if (marker) marker.color = color;
            var text = go.transform.Find("Label")?.GetComponent<TextMesh>();
            if (text)
            {
                text.text = label;
                text.color = color;
            }
            return obj;
        }

        private InteractionObject[] GetCurrentInteractions()
        {
            var list = new List<InteractionObject>();
            if (X) list.Add(X);
            if (Y) list.Add(Y);
            if (Z) list.Add(Z);
            if (X_Energy && !list.Contains(X_Energy)) list.Add(X_Energy);
            if (Y_Shield && !list.Contains(Y_Shield)) list.Add(Y_Shield);
            if (Z_Speed && !list.Contains(Z_Speed)) list.Add(Z_Speed);
            return list.ToArray();
        }

        private void Bind()
        {
            EnsureAllVariants();
            var current = GetCurrentInteractions();
            if (bound != null && Same(bound, current)) return;
            Unbind();
            bound = current;
            foreach (var interaction in bound)
                if (interaction) interaction.Activated += OnInteractionActivated;
            EnsureSlots();
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (bound == null) return;
            foreach (var interaction in bound)
                if (interaction) interaction.Activated -= OnInteractionActivated;
            bound = null;
        }

        private void EnsureSlots()
        {
            var current = GetCurrentInteractions();
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
