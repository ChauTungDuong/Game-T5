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
        public Color FullColor = Color.red;
        public Sprite BarSprite;
        public Sprite FillSprite;
        public Sprite IconSprite;

        [SerializeField] private LineRenderer background;
        [SerializeField] private LineRenderer fill;
        [SerializeField] private TextMesh valueText;
        [SerializeField] private SpriteRenderer artBackground;
        [SerializeField] private SpriteRenderer artFill;
        [SerializeField] private SpriteRenderer artIcon;

        public LineRenderer Background => background;
        public LineRenderer Fill => fill;
        public TextMesh ValueText => valueText;
        public float FillRatio { get; private set; }
        public bool UsesSpriteArt => BarSprite && IconSprite;

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
            if (background)
            {
                background.widthMultiplier = BarHeight;
                background.sortingOrder = SortingOrder;
                background.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
                background.SetPosition(1, new Vector3(halfWidth, 0f, 0f));
                background.startColor = BackgroundColor;
                background.endColor = BackgroundColor;
                background.enabled = !UsesSpriteArt && current > 0f;
            }
            if (fill)
            {
                fill.widthMultiplier = BarHeight * 1.25f;
                fill.sortingOrder = SortingOrder + 1;
                fill.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
                fill.SetPosition(1, new Vector3(-halfWidth + Width * FillRatio, 0f, 0f));
                fill.startColor = FullColor;
                fill.endColor = FullColor;
                fill.enabled = !UsesSpriteArt && current > 0f;
            }
            valueText.text = $"{Mathf.Max(0f, current):0}/{maximum:0}";
            valueText.gameObject.SetActive(current > 0f);
            RefreshSpriteArt();
        }

        private void EnsureVisuals()
        {
            if (!background) background = FindChild<LineRenderer>("Health background");
            if (!background && !UsesSpriteArt) background = CreateLine("Health background", 0);
            if (!fill) fill = FindChild<LineRenderer>("Health fill");
            if (!fill && !UsesSpriteArt) fill = CreateLine("Health fill", 1);
            if (!valueText) valueText = FindChild<TextMesh>("Health value");
            if (!valueText)
            {
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
            EnsureSpriteVisuals();
        }

        private void EnsureSpriteVisuals()
        {
            if (!UsesSpriteArt) return;
            if (!artBackground) artBackground = FindChild<SpriteRenderer>("Health art background");
            if (!artBackground) artBackground = CreateSprite("Health art background");
            if (!artFill) artFill = FindChild<SpriteRenderer>("Health art fill");
            if (!artFill) artFill = CreateSprite("Health art fill");
            if (!artIcon) artIcon = FindChild<SpriteRenderer>("Health art icon");
            if (!artIcon) artIcon = CreateSprite("Health art icon");

            artBackground.sprite = BarSprite;
            artBackground.sortingOrder = SortingOrder;
            artFill.sprite = FillSprite ? FillSprite : BarSprite;
            artFill.sortingOrder = SortingOrder + 1;
            artIcon.sprite = IconSprite;
            artIcon.sortingOrder = SortingOrder + 2;
        }

        private SpriteRenderer CreateSprite(string name)
        {
            var spriteObject = new GameObject(name, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(transform, false);
            return spriteObject.GetComponent<SpriteRenderer>();
        }

        private void RefreshSpriteArt()
        {
            if (!UsesSpriteArt) return;
            EnsureSpriteVisuals();
            var iconSize = Mathf.Min(Width * .28f, BarHeight * 3.8f);
            var gap = Mathf.Min(.06f, Width * .04f);
            var barWidth = Mathf.Max(.1f, Width - iconSize - gap);
            var barStart = -Width * .5f + iconSize + gap;

            ConfigureSprite(artBackground, BarSprite, barWidth, BarHeight,
                new Vector3(barStart + barWidth * .5f, 0f, 0f), BackgroundColor);
            artBackground.enabled = FillRatio > 0f;
            var fillSprite = FillSprite ? FillSprite : BarSprite;
            var fillWidth = barWidth * FillRatio;
            var fallbackFill = !FillSprite || FillSprite == BarSprite;
            if (fallbackFill)
            {
                // Main.unity can be opened before the editor has generated the
                // derived solid-red fill texture. Use the existing line as a
                // precise fill overlay until that texture is available.
                artFill.enabled = false;
                if (!fill) fill = CreateLine("Health fill", 1);
                fill.widthMultiplier = BarHeight * .95f;
                fill.sortingOrder = SortingOrder + 1;
                fill.SetPosition(0, new Vector3(barStart, 0f, -.01f));
                fill.SetPosition(1, new Vector3(barStart + fillWidth, 0f, -.01f));
                fill.startColor = FullColor;
                fill.endColor = FullColor;
                fill.enabled = FillRatio > 0f;
            }
            else
            {
                if (fill) fill.enabled = false;
                ConfigureSprite(artFill, fillSprite, barWidth, BarHeight,
                    new Vector3(barStart + fillWidth * .5f, 0f, -.01f), FullColor);
                artFill.transform.localScale = new Vector3(
                    artFill.transform.localScale.x * FillRatio,
                    artFill.transform.localScale.y,
                    1f);
                artFill.enabled = FillRatio > 0f;
            }

            var iconBounds = IconSprite.bounds.size;
            var iconScale = iconBounds.y > 0f ? iconSize / iconBounds.y : 1f;
            artIcon.transform.localPosition = new Vector3(-Width * .5f + iconSize * .5f, 0f, -.02f);
            artIcon.transform.localScale = new Vector3(iconScale, iconScale, 1f);
            artIcon.color = Color.white;
            artIcon.enabled = FillRatio > 0f;
        }

        private static void ConfigureSprite(SpriteRenderer renderer, Sprite sprite, float width, float height, Vector3 position, Color color)
        {
            if (!renderer || !sprite) return;
            var bounds = sprite.bounds.size;
            renderer.transform.localPosition = position;
            renderer.transform.localScale = new Vector3(
                bounds.x > 0f ? width / bounds.x : 1f,
                bounds.y > 0f ? height / bounds.y : 1f,
                1f);
            renderer.color = color;
            renderer.enabled = true;
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
