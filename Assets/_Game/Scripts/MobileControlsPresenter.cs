using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard
{
    public sealed class MobileControlsPresenter : MonoBehaviour
    {
        public GameSession Session;
        public VirtualDirectionalButton UpButton;
        public VirtualDirectionalButton DownButton;
        public VirtualDirectionalButton LeftButton;
        public VirtualDirectionalButton RightButton;

        public Button ShieldButton;
        public Text ShieldLabel;

        public Button EmpButton;
        public Text EmpLabel;

        public Button WeaponCycleButton;
        public Text WeaponCycleLabel;

        private void Awake()
        {
            EnsureControls();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            if (Session && Session.State == MatchState.Playing)
            {
                Refresh();
            }
        }

        public void EnsureControls()
        {
            if (!Session) Session = GetComponentInParent<GameSession>() ?? FindFirstObjectByType<GameSession>();

            var rt = transform as RectTransform;
            if (rt)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Find or create Dpad
            var dpadT = transform.Find("Dpad");
            if (!dpadT)
            {
                var dpadGo = new GameObject("Dpad", typeof(RectTransform));
                dpadGo.transform.SetParent(transform, false);
                dpadT = dpadGo.transform;
            }
            var dpadRt = (RectTransform)dpadT;
            dpadRt.anchorMin = Vector2.zero;
            dpadRt.anchorMax = Vector2.zero;
            dpadRt.pivot = Vector2.zero;
            dpadRt.anchoredPosition = new Vector2(24, 24);
            dpadRt.sizeDelta = new Vector2(170, 170);

            if (!UpButton) UpButton = EnsureDpadButton(dpadT, "Dpad Up", "▲", new Vector2(0, 52), new Vector2(50, 48), Vector2.up);
            if (!DownButton) DownButton = EnsureDpadButton(dpadT, "Dpad Down", "▼", new Vector2(0, -52), new Vector2(50, 48), Vector2.down);
            if (!LeftButton) LeftButton = EnsureDpadButton(dpadT, "Dpad Left", "◄", new Vector2(-52, 0), new Vector2(48, 50), Vector2.left);
            if (!RightButton) RightButton = EnsureDpadButton(dpadT, "Dpad Right", "►", new Vector2(52, 0), new Vector2(48, 50), Vector2.right);

            // Clean up obsolete rectangular button if present
            var oldWeapon = transform.Find("Mobile Weapon Switch");
            if (oldWeapon)
            {
                if (Application.isPlaying) Destroy(oldWeapon.gameObject);
                else DestroyImmediate(oldWeapon.gameObject);
            }

            // Find circular button sprite if available
            var circleSprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s && s.name == "button_circle");

            // 3 Round Action Buttons at Bottom-Right: Q (Shield), E (EMP), R (Weapon)
            if (!ShieldButton)
            {
                ShieldButton = EnsureCircularButton("Mobile Shield Button", "Q\nSHIELD", new Vector2(-135, 120), new Vector2(62, 62), new Color(.12f, .48f, .68f, .55f), circleSprite, out ShieldLabel);
            }
            if (!EmpButton)
            {
                EmpButton = EnsureCircularButton("Mobile Emp Button", "E\nEMP", new Vector2(-55, 120), new Vector2(62, 62), new Color(.48f, .20f, .65f, .55f), circleSprite, out EmpLabel);
            }
            if (!WeaponCycleButton)
            {
                WeaponCycleButton = EnsureCircularButton("Mobile Weapon Button", "R\n[BULLET]", new Vector2(-95, 48), new Vector2(68, 68), new Color(.13f, .45f, .48f, .55f), circleSprite, out WeaponCycleLabel);
            }
        }

        private VirtualDirectionalButton EnsureDpadButton(Transform parent, string name, string arrow, Vector2 position, Vector2 size, Vector2 direction)
        {
            var t = parent.Find(name);
            GameObject go;
            if (!t)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VirtualDirectionalButton));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
            }

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(.5f, .5f);
            rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = new Color(.12f, .38f, .44f, .4f);

            var btn = go.GetComponent<VirtualDirectionalButton>() ?? go.AddComponent<VirtualDirectionalButton>();
            btn.Direction = direction;
            btn.Presenter = this;
            btn.NormalAlpha = .4f;
            btn.PressedAlpha = .85f;
            btn.BackgroundImage = img;

            var labelT = go.transform.Find("Label");
            Text label;
            if (!labelT)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                var lRt = (RectTransform)labelGo.transform;
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.offsetMin = Vector2.zero;
                lRt.offsetMax = Vector2.zero;
                label = labelGo.GetComponent<Text>();
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.font = font;
                label.fontSize = 22;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
            }
            else
            {
                label = labelT.GetComponent<Text>();
            }
            label.text = arrow;
            btn.LabelText = label;

            return btn;
        }

        private Button EnsureCircularButton(string name, string defaultText, Vector2 position, Vector2 size, Color color, Sprite sprite, out Text label)
        {
            var t = transform.Find(name);
            GameObject go;
            if (!t)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
            }
            else
            {
                go = t.gameObject;
            }

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            if (sprite) img.sprite = sprite;
            img.color = color;
            img.type = Image.Type.Simple;

            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();

            var labelT = go.transform.Find("Label");
            if (!labelT)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(go.transform, false);
                var lRt = (RectTransform)labelGo.transform;
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.offsetMin = Vector2.zero;
                lRt.offsetMax = Vector2.zero;
                label = labelGo.GetComponent<Text>();
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.font = font;
                label.fontSize = 11;
                label.alignment = TextAnchor.MiddleCenter;
                label.lineSpacing = 0.9f;
                label.color = Color.white;
            }
            else
            {
                label = labelT.GetComponent<Text>();
            }
            label.text = defaultText;

            return btn;
        }

        public void Bind()
        {
            Unbind();
            EnsureControls();

            if (UpButton) UpButton.Presenter = this;
            if (DownButton) DownButton.Presenter = this;
            if (LeftButton) LeftButton.Presenter = this;
            if (RightButton) RightButton.Presenter = this;

            if (ShieldButton) ShieldButton.onClick.AddListener(OnShieldClicked);
            if (EmpButton) EmpButton.onClick.AddListener(OnEmpClicked);
            if (WeaponCycleButton) WeaponCycleButton.onClick.AddListener(OnWeaponCycleClicked);

            if (Session)
            {
                Session.Changed += Refresh;
                if (Session.Player) Session.Player.Changed += Refresh;
                if (Session.Weapon)
                {
                    Session.Weapon.WeaponChanged += HandleWeaponChanged;
                    Session.Weapon.WeaponSelected += HandleWeaponSelected;
                }
                if (Session.Defense)
                {
                    Session.Defense.Changed += Refresh;
                }
            }

            Refresh();
        }

        private void Unbind()
        {
            if (ShieldButton) ShieldButton.onClick.RemoveListener(OnShieldClicked);
            if (EmpButton) EmpButton.onClick.RemoveListener(OnEmpClicked);
            if (WeaponCycleButton) WeaponCycleButton.onClick.RemoveListener(OnWeaponCycleClicked);

            if (Session)
            {
                Session.Changed -= Refresh;
                if (Session.Player) Session.Player.Changed -= Refresh;
                if (Session.Weapon)
                {
                    Session.Weapon.WeaponChanged -= HandleWeaponChanged;
                    Session.Weapon.WeaponSelected -= HandleWeaponSelected;
                }
                if (Session.Defense)
                {
                    Session.Defense.Changed -= Refresh;
                }
            }
        }

        public void NotifyDirectionChanged()
        {
            var combined = Vector2.zero;
            if (UpButton && UpButton.IsPressed) combined.y += 1f;
            if (DownButton && DownButton.IsPressed) combined.y -= 1f;
            if (LeftButton && LeftButton.IsPressed) combined.x -= 1f;
            if (RightButton && RightButton.IsPressed) combined.x += 1f;

            if (Session && Session.Input)
            {
                Session.Input.VirtualMoveInput = combined;
            }
        }

        public void OnShieldClicked()
        {
            if (!Session || Session.State != MatchState.Playing || !Session.Defense) return;
            Session.Defense.TryActivateShield();
            if (Session.Audio) Session.Audio.PlayUiClick();
            Refresh();
        }

        public void OnEmpClicked()
        {
            if (!Session || Session.State != MatchState.Playing || !Session.Defense) return;
            Session.Defense.TryActivateEmp();
            if (Session.Audio) Session.Audio.PlayUiClick();
            Refresh();
        }

        public void OnWeaponCycleClicked()
        {
            if (!Session || Session.State != MatchState.Playing || !Session.Weapon) return;
            Session.Weapon.CycleNextWeapon();
            if (Session.Audio) Session.Audio.PlayUiClick();
            RefreshWeaponLabel();
        }

        private void HandleWeaponChanged(WeaponKind _) => RefreshWeaponLabel();
        private void HandleWeaponSelected(WeaponKind _) => RefreshWeaponLabel();

        public void Refresh()
        {
            var playing = Session && Session.State == MatchState.Playing;

            var dpadT = transform.Find("Dpad");
            if (dpadT) dpadT.gameObject.SetActive(playing);
            if (ShieldButton) ShieldButton.gameObject.SetActive(playing);
            if (EmpButton) EmpButton.gameObject.SetActive(playing);
            if (WeaponCycleButton) WeaponCycleButton.gameObject.SetActive(playing);

            if (!playing) return;

            WeaponCycleButton.interactable = true;
            if (ShieldButton)
            {
                var canUseShield = Session.Defense && !Session.Defense.ShieldActive
                    && (!Session.Player || Session.Player.CanUseSkill());
                ShieldButton.interactable = canUseShield;
            }
            if (EmpButton)
            {
                var canUseEmp = Session.Defense
                    && (!Session.Player || Session.Player.CanUseSkill());
                EmpButton.interactable = canUseEmp;
            }

            RefreshWeaponLabel();
            RefreshDefenseLabels();
        }

        public void RefreshWeaponLabel()
        {
            if (!WeaponCycleLabel) return;
            var kind = Session && Session.Weapon ? Session.Weapon.SelectedWeapon : WeaponKind.Bullet;
            WeaponCycleLabel.text = $"R\n[{kind.ToString().ToUpper()}]";
        }

        public void RefreshDefenseLabels()
        {
            if (ShieldLabel)
            {
                if (!Session || !Session.Defense)
                {
                    ShieldLabel.text = "Q\nSHIELD";
                }
                else if (Session.Defense.ShieldActive)
                {
                    ShieldLabel.text = $"Q\n{Session.Defense.ShieldHitsRemaining} HIT";
                }
                else
                {
                    ShieldLabel.text = "Q\nSHIELD";
                }
            }

            if (EmpLabel)
            {
                EmpLabel.text = "E\nEMP";
            }
        }
    }
}
