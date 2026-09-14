using UnityEngine;

namespace CoreGuard
{
    [DisallowMultipleComponent]
    public sealed class WorldHealthBar : MonoBehaviour
    {
        public float Width = 1.2f;
        public float BarHeight = .12f;
        public int SortingOrder = 7;
        public Color BackgroundColor = new Color(.025f, .045f, .07f, .95f);
        public Color FullColor = Color.green;

        public LineRenderer Background { get; private set; }
        public LineRenderer Fill { get; private set; }
        public TextMesh ValueText { get; private set; }
        public float FillRatio { get; private set; }

        private PlayerStats playerStats;

        private void Awake()
        {
            EnsureVisuals();
            SetHealth(0f, 1f);
        }

        private void OnEnable()
        {
            playerStats = GetComponentInParent<PlayerStats>();
            if (!playerStats) return;
            playerStats.Changed += RefreshPlayerHealth;
            RefreshPlayerHealth();
        }

        private void OnDisable()
        {
            if (playerStats) playerStats.Changed -= RefreshPlayerHealth;
            playerStats = null;
        }

        public void SetHealth(float current, float maximum)
        {
            EnsureVisuals();
            maximum = Mathf.Max(1f, maximum);
            FillRatio = Mathf.Clamp01(current / maximum);
            var halfWidth = Width * .5f;
            Background.widthMultiplier = BarHeight;
            Background.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            Background.SetPosition(1, new Vector3(halfWidth, 0f, 0f));
            Fill.widthMultiplier = BarHeight * 1.25f;
            Fill.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            Fill.SetPosition(1, new Vector3(-halfWidth + Width * FillRatio, 0f, 0f));
            Background.startColor = BackgroundColor;
            Background.endColor = BackgroundColor;
            var fillColor = FillRatio < .25f ? Color.red : FillRatio < .5f ? Color.yellow : FullColor;
            Fill.startColor = fillColor;
            Fill.endColor = fillColor;
            Background.enabled = current > 0f;
            Fill.enabled = current > 0f;
            ValueText.text = $"{Mathf.Max(0f, current):0}/{maximum:0}";
            ValueText.gameObject.SetActive(current > 0f);
        }

        private void EnsureVisuals()
        {
            if (!Background)
                Background = CreateLine("Health background", 0);
            if (!Fill)
                Fill = CreateLine("Health fill", 1);
            if (ValueText) return;
            var label = new GameObject("Health value", typeof(TextMesh));
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, .18f, 0f);
            ValueText = label.GetComponent<TextMesh>();
            ValueText.anchor = TextAnchor.MiddleCenter;
            ValueText.alignment = TextAlignment.Center;
            ValueText.characterSize = .08f;
            ValueText.fontSize = 32;
            ValueText.color = Color.white;
            ValueText.GetComponent<MeshRenderer>().sortingOrder = SortingOrder + 2;
        }

        private LineRenderer CreateLine(string name, int orderOffset)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.sortingOrder = SortingOrder + orderOffset;
            return line;
        }

        private void RefreshPlayerHealth()
        {
            if (playerStats) SetHealth(playerStats.HP, PlayerStats.MaxHP);
        }
    }
}
