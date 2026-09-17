using UnityEngine;

namespace CoreGuard
{
    public sealed class InteractionFeedbackCue : MonoBehaviour
    {
        public InteractionKind Kind { get; private set; }
        public string Message { get; private set; }
        public float Duration { get; private set; } = .8f;

        private TextMesh label;
        private ParticleSystem particles;
        private float elapsed;
        private Vector3 origin;

        public static InteractionFeedbackCue Spawn(InteractionKind kind, Vector3 position)
        {
            var root = new GameObject($"Interaction feedback {kind}");
            root.transform.position = position;
            var cue = root.AddComponent<InteractionFeedbackCue>();
            cue.Initialize(kind);
            return cue;
        }

        public void Advance(float delta)
        {
            if (delta <= 0f) return;
            elapsed += delta;
            var progress = Mathf.Clamp01(elapsed / Duration);
            var pulse = Mathf.Sin(progress * Mathf.PI);
            transform.localScale = Vector3.one * Mathf.Lerp(.7f, 1.15f, pulse);
            if (label)
            {
                label.transform.position = origin + Vector3.up * Mathf.Lerp(.55f, 1.25f, progress);
                var color = label.color;
                color.a = 1f - progress;
                label.color = color;
            }
            if (elapsed >= Duration)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
            }
        }

        private void Initialize(InteractionKind kind)
        {
            Kind = kind;
            Message = MessageFor(kind);
            origin = transform.position;

            var textObject = new GameObject("Floating text", typeof(TextMesh));
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = Vector3.up * .55f;
            label = textObject.GetComponent<TextMesh>();
            label.text = Message;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 42;
            label.characterSize = .065f;
            label.color = ColorFor(kind);
            label.GetComponent<MeshRenderer>().sortingOrder = 20;

            var particleObject = new GameObject("Pulse particles", typeof(ParticleSystem));
            particleObject.transform.SetParent(transform, false);
            particles = particleObject.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = Duration;
            main.startLifetime = .35f;
            main.startSpeed = kind == InteractionKind.X ? 2.2f : 1.5f;
            main.startSize = .16f;
            main.startColor = ColorFor(kind);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .35f;
            particles.Emit(kind == InteractionKind.X ? 18 : 12);
        }

        private void Update() => Advance(Time.deltaTime);

        private static string MessageFor(InteractionKind kind)
        {
            switch (kind)
            {
                case InteractionKind.X: return "-20 HP / -10 ARMOR";
                case InteractionKind.Y: return "SLOWED / SHIELD BROKEN";
                case InteractionKind.Z: return "+20 HP / SPEED BOOST";
                default: return string.Empty;
            }
        }

        private static Color ColorFor(InteractionKind kind)
        {
            switch (kind)
            {
                case InteractionKind.X: return new Color(1f, .25f, .12f);
                case InteractionKind.Y: return new Color(.45f, .85f, 1f);
                case InteractionKind.Z: return new Color(.25f, 1f, .55f);
                default: return Color.white;
            }
        }
    }
}
