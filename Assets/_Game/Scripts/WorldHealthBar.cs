using UnityEngine;

namespace CoreGuard
{
    [DisallowMultipleComponent]
    public sealed class WorldHealthBar : MonoBehaviour
    {
        public float Width = 1.2f;
        public float BarHeight = .16f;
        public int SortingOrder = 7;
        public Color BackgroundColor = new Color(.06f, .08f, .12f, .95f);
        public Color FullColor = Color.green;
        public bool UseThresholdColors = true;

        public Sprite BadgeSprite;
        [SerializeField] private LineRenderer background;
        [SerializeField] private LineRenderer fill;
        [SerializeField] private TextMesh valueText;
        [SerializeField] private SpriteRenderer badgeRenderer;

        public LineRenderer Background => background;
        public LineRenderer Fill => fill;
        public TextMesh ValueText => valueText;
        public SpriteRenderer BadgeRenderer => badgeRenderer;
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
            // Background container frame: taller than the fill bar
            background.widthMultiplier = BarHeight;
            background.sortingOrder = SortingOrder;
            background.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            background.SetPosition(1, new Vector3(halfWidth, 0f, 0f));

            // Fill bar: fits neatly inside the background container frame
            var fillHeight = BarHeight * 0.72f;
            fill.widthMultiplier = fillHeight;
            fill.sortingOrder = SortingOrder + 1;
            fill.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            fill.SetPosition(1, new Vector3(-halfWidth + Width * FillRatio, 0f, 0f));

            background.startColor = BackgroundColor;
            background.endColor = BackgroundColor;
            var fillColor = (UseThresholdColors && FillRatio < .25f) ? Color.red :
                (UseThresholdColors && FillRatio < .5f) ? Color.yellow : FullColor;
            fill.startColor = fillColor;
            fill.endColor = fillColor;
            background.enabled = current > 0f;
            fill.enabled = current > 0f && FillRatio > 0f;
            if (badgeRenderer)
            {
                badgeRenderer.enabled = current > 0f;
                badgeRenderer.transform.localPosition = new Vector3(-halfWidth - BarHeight * 0.95f, 0f, 0f);
            }
            valueText.text = $"{Mathf.Max(0f, current):0}/{maximum:0}";
            valueText.gameObject.SetActive(current > 0f);
        }

        private void EnsureVisuals()
        {
            if (!background) background = FindChild<LineRenderer>("Health background");
            if (!background) background = CreateLine("Health background", 0);
            if (!fill) fill = FindChild<LineRenderer>("Health fill");
            if (!fill) fill = CreateLine("Health fill", 1);
            ApplyDefaultMaterial(background);
            ApplyDefaultMaterial(fill);
            if (!badgeRenderer) badgeRenderer = FindChild<SpriteRenderer>("Health badge");
            if (!badgeRenderer)
            {
                var badgeObj = new GameObject("Health badge");
                badgeObj.transform.SetParent(transform, false);
                badgeRenderer = badgeObj.AddComponent<SpriteRenderer>();
            }
            if (badgeRenderer)
            {
                badgeRenderer.sortingOrder = SortingOrder + 2;
                if (!BadgeSprite)
                {
#if UNITY_EDITOR
                    var path = (FullColor == Color.cyan || FullColor == new Color(0f, 1f, 1f, 1f))
                        ? "Assets/_Game/Art/UI/badge_heart_cyan.png"
                        : "Assets/_Game/Art/UI/badge_heart_green.png";
                    BadgeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#endif
                }
                if (BadgeSprite) badgeRenderer.sprite = BadgeSprite;
                badgeRenderer.transform.localScale = Vector3.one * (BarHeight * 2.2f);
            }
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
            ApplyDefaultMaterial(line);
            return line;
        }

        private static void ApplyDefaultMaterial(LineRenderer line)
        {
            if (!line) return;
            var shader = Shader.Find("Sprites/Default");
            if (shader && (line.sharedMaterial == null || line.sharedMaterial.name.Contains("Default-Line")))
                line.material = new Material(shader);
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
