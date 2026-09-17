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
        private LineRenderer shockwaveRing;
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
            if (shockwaveRing)
            {
                var ringScale = Mathf.Lerp(0.5f, 2.2f, progress);
                shockwaveRing.transform.localScale = Vector3.one * ringScale;
                var ringColor = ColorFor(Kind);
                ringColor.a = Mathf.Clamp01(1f - progress);
                shockwaveRing.startColor = shockwaveRing.endColor = ringColor;
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

            var ringObject = new GameObject("Shockwave ring");
            ringObject.transform.SetParent(transform, false);
            shockwaveRing = ringObject.AddComponent<LineRenderer>();
            shockwaveRing.useWorldSpace = false;
            shockwaveRing.loop = true;
            shockwaveRing.positionCount = 32;
            shockwaveRing.startWidth = shockwaveRing.endWidth = .09f;
            shockwaveRing.startColor = shockwaveRing.endColor = ColorFor(kind);
            shockwaveRing.sortingOrder = 21;
            var shader = Shader.Find("Sprites/Default");
            if (shader) shockwaveRing.material = new Material(shader);
            for (var i = 0; i < shockwaveRing.positionCount; i++)
            {
                var rad = i * Mathf.PI * 2f / shockwaveRing.positionCount;
                shockwaveRing.SetPosition(i, new Vector3(Mathf.Cos(rad) * .5f, Mathf.Sin(rad) * .5f, 0));
            }

            var particleObject = new GameObject("Pulse particles", typeof(ParticleSystem));
            particleObject.transform.SetParent(transform, false);
            particles = particleObject.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = Duration;
            main.startLifetime = .35f;
            main.startSpeed = 2.2f;
            main.startSize = .2f;
            main.startColor = ColorFor(kind);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .35f;
            particles.Emit(20);
        }

        private void Update() => Advance(Time.deltaTime);

        private static string MessageFor(InteractionKind kind)
        {
            switch (kind)
            {
                case InteractionKind.X: return "-20 HP";
                case InteractionKind.X_Energy: return "-20 NĂNG LƯỢNG";
                case InteractionKind.Y: return "GIẢM TỐC CHẠY";
                case InteractionKind.Y_Shield: return "PHÁ KHIÊN";
                case InteractionKind.Z: return "+20 HP";
                case InteractionKind.Z_Speed: return "TĂNG TỐC CHẠY";
                default: return string.Empty;
            }
        }

        public static Color ColorFor(InteractionKind kind)
        {
            switch (kind)
            {
                case InteractionKind.X: return new Color(1f, .12f, .12f);       // Đỏ tươi
                case InteractionKind.X_Energy: return new Color(1f, .55f, 0f);    // Cam
                case InteractionKind.Y: return new Color(.72f, .2f, 1f);          // Tím
                case InteractionKind.Y_Shield: return new Color(.1f, .65f, 1f);   // Lam
                case InteractionKind.Z: return new Color(.15f, .95f, .3f);        // Xanh lá
                case InteractionKind.Z_Speed: return new Color(1f, .85f, .1f);    // Vàng
                default: return Color.white;
            }
        }
    }
}
