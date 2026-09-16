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

            // Find or create Mobile Weapon Switch button
            if (!WeaponCycleButton)
            {
                var weaponT = transform.Find("Mobile Weapon Switch");
                GameObject weaponGo;
                if (!weaponT)
                {
                    weaponGo = new GameObject("Mobile Weapon Switch", typeof(RectTransform), typeof(Image), typeof(Button));
                    weaponGo.transform.SetParent(transform, false);
                }
                else
                {
                    weaponGo = weaponT.gameObject;
                }
                var weaponRt = (RectTransform)weaponGo.transform;
                weaponRt.anchorMin = new Vector2(1, 0);
                weaponRt.anchorMax = new Vector2(1, 0);
                weaponRt.pivot = new Vector2(1, 0);
                weaponRt.anchoredPosition = new Vector2(-24, 115);
                weaponRt.sizeDelta = new Vector2(115, 52);

                var img = weaponGo.GetComponent<Image>();
                img.color = new Color(.13f, .45f, .48f, .55f);

                WeaponCycleButton = weaponGo.GetComponent<Button>();

                var labelT = weaponGo.transform.Find("Label");
                if (!labelT)
                {
                    var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    labelGo.transform.SetParent(weaponGo.transform, false);
                    var lRt = (RectTransform)labelGo.transform;
                    lRt.anchorMin = Vector2.zero;
                    lRt.anchorMax = Vector2.one;
                    lRt.offsetMin = Vector2.zero;
                    lRt.offsetMax = Vector2.zero;
                    WeaponCycleLabel = labelGo.GetComponent<Text>();
                    var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    WeaponCycleLabel.font = font;
                    WeaponCycleLabel.fontSize = 12;
                    WeaponCycleLabel.alignment = TextAnchor.MiddleCenter;
                    WeaponCycleLabel.color = Color.white;
                }
                else
                {
                    WeaponCycleLabel = labelT.GetComponent<Text>();
                }
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

            var img = go.GetComponent<Image>();
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

        public void Bind()
        {
            Unbind();
            EnsureControls();

            if (UpButton) UpButton.Presenter = this;
            if (DownButton) DownButton.Presenter = this;
            if (LeftButton) LeftButton.Presenter = this;
            if (RightButton) RightButton.Presenter = this;

            if (WeaponCycleButton)
            {
                WeaponCycleButton.onClick.AddListener(OnWeaponCycleClicked);
            }

            if (Session)
            {
                Session.Changed += Refresh;
                if (Session.Weapon)
                {
                    Session.Weapon.WeaponChanged += HandleWeaponChanged;
                    Session.Weapon.WeaponSelected += HandleWeaponSelected;
                }
            }

            Refresh();
        }

        private void Unbind()
        {
            if (WeaponCycleButton)
            {
                WeaponCycleButton.onClick.RemoveListener(OnWeaponCycleClicked);
            }

            if (Session)
            {
                Session.Changed -= Refresh;
                if (Session.Weapon)
                {
                    Session.Weapon.WeaponChanged -= HandleWeaponChanged;
                    Session.Weapon.WeaponSelected -= HandleWeaponSelected;
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
            RefreshWeaponLabel();
            var playing = Session && Session.State == MatchState.Playing;
            if (WeaponCycleButton) WeaponCycleButton.interactable = playing;
        }

        public void RefreshWeaponLabel()
        {
            if (!WeaponCycleLabel) return;
            var kind = Session && Session.Weapon ? Session.Weapon.SelectedWeapon : WeaponKind.Bullet;
            WeaponCycleLabel.text = $"WEAPON\n[{kind.ToString().ToUpper()}]";
        }
    }
}
