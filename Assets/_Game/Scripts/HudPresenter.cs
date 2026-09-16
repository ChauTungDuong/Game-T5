using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard
{
    public sealed class HudPresenter : MonoBehaviour
    {
        public GameSession Session;
        public Text StatsText, CoreText, TimerText, StateText, ResultText;
        public Text WeaponText, CooldownsText;
        public Image PlayerHpFill, PlayerArmorFill, CoreHpFill;
        public Text PlayerHpLabel, PlayerArmorLabel;
        public GameObject StartPanel, PausePanel, ResultPanel;
        public Button StartButton, ResumeButton, RetryButton;
        public Button WeaponCycleButton;
        public Button BulletButton, RocketButton, MineButton, ShieldButton, EmpButton;
        private GameSession boundSession;

        private void OnEnable() { if (Session) Bind(); }
        private void OnDisable() => Unbind();

        public void Bind()
        {
            Unbind();
            boundSession = Session;
            Session.Changed += Refresh;
            Session.Player.Changed += Refresh;
            Session.Core.Changed += Refresh;
            if (Session.Defense) Session.Defense.Changed += Refresh;
            if (Session.Weapon) Session.Weapon.WeaponChanged += OnWeaponChanged;
            StartButton.onClick.AddListener(Session.StartMatch);
            ResumeButton.onClick.AddListener(Session.TogglePause);
            RetryButton.onClick.AddListener(Session.Retry);
            if (WeaponCycleButton) WeaponCycleButton.onClick.AddListener(CycleWeapon);
            if (BulletButton) BulletButton.onClick.AddListener(SelectBullet);
            if (RocketButton) RocketButton.onClick.AddListener(SelectRocket);
            if (MineButton) MineButton.onClick.AddListener(SelectMine);
            if (ShieldButton) ShieldButton.onClick.AddListener(ActivateShield);
            if (EmpButton) EmpButton.onClick.AddListener(ActivateEmp);
            Refresh();
        }

        private void OnWeaponChanged(WeaponKind _) => Refresh();

        private void Unbind()
        {
            if (!boundSession) return;
            boundSession.Changed -= Refresh;
            if (boundSession.Player) boundSession.Player.Changed -= Refresh;
            if (boundSession.Core) boundSession.Core.Changed -= Refresh;
            if (boundSession.Defense) boundSession.Defense.Changed -= Refresh;
            if (boundSession.Weapon) boundSession.Weapon.WeaponChanged -= OnWeaponChanged;
            if (StartButton) StartButton.onClick.RemoveListener(boundSession.StartMatch);
            if (ResumeButton) ResumeButton.onClick.RemoveListener(boundSession.TogglePause);
            if (RetryButton) RetryButton.onClick.RemoveListener(boundSession.Retry);
            if (WeaponCycleButton) WeaponCycleButton.onClick.RemoveListener(CycleWeapon);
            if (BulletButton) BulletButton.onClick.RemoveListener(SelectBullet);
            if (RocketButton) RocketButton.onClick.RemoveListener(SelectRocket);
            if (MineButton) MineButton.onClick.RemoveListener(SelectMine);
            if (ShieldButton) ShieldButton.onClick.RemoveListener(ActivateShield);
            if (EmpButton) EmpButton.onClick.RemoveListener(ActivateEmp);
            boundSession = null;
        }

        private void CycleWeapon() { Session.Weapon?.CycleNextWeapon(); Refresh(); }
        private void SelectBullet() { Session.Weapon?.Select(WeaponKind.Bullet); Refresh(); }
        private void SelectRocket() { Session.Weapon?.Select(WeaponKind.Rocket); Refresh(); }
        private void SelectMine() { Session.Weapon?.Select(WeaponKind.Laser); Refresh(); }
        private void ActivateShield() => Session.Defense?.TryActivateShield();
        private void ActivateEmp() => Session.Defense?.TryActivateEmp();

        private void Refresh()
        {
            if (Session && Session.Player)
            {
                var hpRatio = Mathf.Clamp01(Session.Player.HP / Mathf.Max(1f, PlayerStats.MaxHP));
                var armorRatio = Mathf.Clamp01(Session.Player.Armor / Mathf.Max(1f, PlayerStats.MaxArmor));
                if (PlayerHpFill) PlayerHpFill.fillAmount = hpRatio;
                if (PlayerArmorFill) PlayerArmorFill.fillAmount = armorRatio;
                if (PlayerHpLabel) PlayerHpLabel.text = $"{Session.Player.HP:0}/{PlayerStats.MaxHP:0}";
                if (PlayerArmorLabel) PlayerArmorLabel.text = $"{Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}";
            }
            if (Session && Session.Core)
            {
                var coreRatio = Mathf.Clamp01(Session.Core.HP / Mathf.Max(1f, CoreHealth.MaxHP));
                if (CoreHpFill) CoreHpFill.fillAmount = coreRatio;
            }

            if (StatsText && Session && Session.Player)
                StatsText.text = $"PLAYER HP {Session.Player.HP:0}/{PlayerStats.MaxHP:0}\nARMOR {Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}\nCOINS {Session.Player.Coins}";
            if (CoreText && Session && Session.Core)
                CoreText.text = $"CORE {Session.Core.HP:0}/{CoreHealth.MaxHP:0}";
            if (TimerText && Session)
                TimerText.text = $"TIME {Session.Remaining:00.0}";
            if (StateText && Session)
                StateText.text = Session.State.ToString().ToUpperInvariant();
            if (ResultText && Session)
                ResultText.text = Session.State == MatchState.Won ? "CORE SECURED — YOU WIN" : "CORE OFFLINE — LOST — TRY AGAIN";
            if (WeaponText)
                WeaponText.text = Session.Weapon ? $"WEAPON: {Session.Weapon.SelectedWeapon.ToString().ToUpperInvariant()}" : "WEAPON: NONE";
            if (CooldownsText)
            {
                var shield = !Session.Defense || Session.Defense.ShieldCooldownRemaining <= 0f
                    ? "READY"
                    : Session.Defense.ShieldActive
                        ? $"{Session.Defense.ShieldHitsRemaining}/{Session.Defense.ShieldMaxHits}"
                        : $"{Session.Defense.ShieldCooldownRemaining:0.0}s";
                var emp = !Session.Defense || Session.Defense.EmpCooldownRemaining <= 0f
                    ? "READY"
                    : $"{Session.Defense.EmpCooldownRemaining:0.0}s  AFFECTED: {Session.Defense.LastEmpAffectedCount}";
                CooldownsText.text = $"SHIELD: {shield}\nEMP: {emp}";
            }
            StartPanel.SetActive(Session.State == MatchState.Ready);
            PausePanel.SetActive(Session.State == MatchState.Paused);
            ResultPanel.SetActive(Session.State == MatchState.Won || Session.State == MatchState.Lost);
            var gameplayActionsEnabled = Session.State == MatchState.Playing;
            if (WeaponCycleButton)
            {
                WeaponCycleButton.interactable = gameplayActionsEnabled;
                var weaponLabel = WeaponCycleButton.GetComponentInChildren<Text>();
                if (weaponLabel && Session.Weapon)
                    weaponLabel.text = $"R  {Session.Weapon.SelectedWeapon.ToString().ToUpperInvariant()}";
            }
            if (BulletButton) BulletButton.interactable = gameplayActionsEnabled;
            if (RocketButton) RocketButton.interactable = gameplayActionsEnabled;
            if (MineButton) MineButton.interactable = gameplayActionsEnabled;
            if (ShieldButton) ShieldButton.interactable = gameplayActionsEnabled && Session.Defense
                && !Session.Defense.ShieldActive && Session.Defense.ShieldCooldownRemaining <= 0f;
            if (EmpButton) EmpButton.interactable = gameplayActionsEnabled && Session.Defense
                && Session.Defense.EmpCooldownRemaining <= 0f;
        }
    }
}
