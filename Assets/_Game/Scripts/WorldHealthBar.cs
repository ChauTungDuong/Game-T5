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

        [SerializeField] private LineRenderer background;
        [SerializeField] private LineRenderer fill;
        [SerializeField] private TextMesh valueText;

        public LineRenderer Background => background;
        public LineRenderer Fill => fill;
        public TextMesh ValueText => valueText;
        public float FillRatio { get; private set; }

        private PlayerStats playerStats;
        private CoreHealth coreHealth;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void OnEnable()
        {
            playerStats = GetComponentInParent<PlayerStats>();
            if (playerStats)
            {
                playerStats.Changed += RefreshPlayerHealth;
                RefreshPlayerHealth();
                return;
            }
            coreHealth = GetComponentInParent<CoreHealth>();
            if (!coreHealth) return;
            coreHealth.Changed += RefreshCoreHealth;
            RefreshCoreHealth();
        }

        private void OnDisable()
        {
            if (playerStats) playerStats.Changed -= RefreshPlayerHealth;
            if (coreHealth) coreHealth.Changed -= RefreshCoreHealth;
            playerStats = null;
            coreHealth = null;
        }

        public void SetHealth(float current, float maximum)
        {
            EnsureVisuals();
            maximum = Mathf.Max(1f, maximum);
            FillRatio = Mathf.Clamp01(current / maximum);
            var halfWidth = Width * .5f;
            background.widthMultiplier = BarHeight;
            background.sortingOrder = SortingOrder;
            background.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            background.SetPosition(1, new Vector3(halfWidth, 0f, 0f));
            fill.widthMultiplier = BarHeight * 1.25f;
            fill.sortingOrder = SortingOrder + 1;
            fill.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            fill.SetPosition(1, new Vector3(-halfWidth + Width * FillRatio, 0f, 0f));
            background.startColor = BackgroundColor;
            background.endColor = BackgroundColor;
            var fillColor = FillRatio < .25f ? Color.red : FillRatio < .5f ? Color.yellow : FullColor;
            fill.startColor = fillColor;
            fill.endColor = fillColor;
            background.enabled = current > 0f;
            fill.enabled = current > 0f;
            valueText.text = $"{Mathf.Max(0f, current):0}/{maximum:0}";
            valueText.gameObject.SetActive(current > 0f);
        }

        private void EnsureVisuals()
        {
            if (!background) background = FindChild<LineRenderer>("Health background");
            if (!background) background = CreateLine("Health background", 0);
            if (!fill) fill = FindChild<LineRenderer>("Health fill");
            if (!fill) fill = CreateLine("Health fill", 1);
            if (!valueText) valueText = FindChild<TextMesh>("Health value");
            if (valueText) return;
            var label = new GameObject("Health value", typeof(TextMesh));
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, .18f, 0f);
            valueText = label.GetComponent<TextMesh>();
            valueText.anchor = TextAnchor.MiddleCenter;
            valueText.alignment = TextAlignment.Center;
            valueText.characterSize = .08f;
            valueText.fontSize = 32;
            valueText.color = Color.white;
            valueText.GetComponent<MeshRenderer>().sortingOrder = SortingOrder + 2;
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

        private void RefreshCoreHealth()
        {
            if (coreHealth) SetHealth(coreHealth.HP, CoreHealth.MaxHP);
        }

        private T FindChild<T>(string name) where T : Component
        {
            var child = transform.Find(name);
            return child ? child.GetComponent<T>() : null;
        }
    }
}
