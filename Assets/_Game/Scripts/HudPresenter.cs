using UnityEngine;
using UnityEngine.UI;
namespace CoreGuard
{
    public sealed class HudPresenter : MonoBehaviour
    {
        public GameSession Session;
        public Text StatsText, CoreText, TimerText, StateText, ResultText;
        public Text WeaponText, CooldownsText;
        public GameObject StartPanel, PausePanel, ResultPanel;
        public Button StartButton, ResumeButton, RetryButton;
        public Button BulletButton, RocketButton, MineButton, ShieldButton, EmpButton;
        private GameSession boundSession;
        private void OnEnable() { if (Session) Bind(); }
        private void OnDisable() { Unbind(); }
        public void Bind()
        {
            Unbind();
            boundSession = Session;
            Session.Changed += Refresh;
            Session.Player.Changed += Refresh;
            Session.Core.Changed += Refresh;
            StartButton.onClick.AddListener(Session.StartMatch);
            ResumeButton.onClick.AddListener(Session.TogglePause);
            RetryButton.onClick.AddListener(Session.Retry);
            if (BulletButton) BulletButton.onClick.AddListener(SelectBullet);
            if (RocketButton) RocketButton.onClick.AddListener(SelectRocket);
            if (MineButton) MineButton.onClick.AddListener(SelectMine);
            if (ShieldButton) ShieldButton.onClick.AddListener(ActivateShield);
            if (EmpButton) EmpButton.onClick.AddListener(ActivateEmp);
            Refresh();
        }
        private void Unbind()
        {
            if (!boundSession) return;
            boundSession.Changed -= Refresh;
            if (boundSession.Player) boundSession.Player.Changed -= Refresh;
            if (boundSession.Core) boundSession.Core.Changed -= Refresh;
            if (StartButton) StartButton.onClick.RemoveListener(boundSession.StartMatch);
            if (ResumeButton) ResumeButton.onClick.RemoveListener(boundSession.TogglePause);
            if (RetryButton) RetryButton.onClick.RemoveListener(boundSession.Retry);
            if (BulletButton) BulletButton.onClick.RemoveListener(SelectBullet);
            if (RocketButton) RocketButton.onClick.RemoveListener(SelectRocket);
            if (MineButton) MineButton.onClick.RemoveListener(SelectMine);
            if (ShieldButton) ShieldButton.onClick.RemoveListener(ActivateShield);
            if (EmpButton) EmpButton.onClick.RemoveListener(ActivateEmp);
            boundSession = null;
        }
        private void SelectBullet() => Session.Weapon?.Select(WeaponKind.Bullet);
        private void SelectRocket() => Session.Weapon?.Select(WeaponKind.Rocket);
        private void SelectMine() => Session.Weapon?.Select(WeaponKind.Mine);
        private void ActivateShield() => Session.Defense?.TryActivateShield();
        private void ActivateEmp() => Session.Defense?.TryActivateEmp();
        private void Refresh()
        {
            StatsText.text = $"PLAYER HP {Session.Player.HP:0}/{PlayerStats.MaxHP:0}   ARMOR {Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}   COINS {Session.Player.Coins}";
            CoreText.text = $"CORE {Session.Core.HP:0}";
            TimerText.text = $"{Session.Remaining:00.0} s";
            StateText.text = Session.State.ToString().ToUpperInvariant();
            ResultText.text = Session.State == MatchState.Won ? "CORE SECURED — YOU WIN" : "CORE OFFLINE — TRY AGAIN";
            if (WeaponText)
                WeaponText.text = Session.Weapon ? $"SELECTED  {Session.Weapon.SelectedWeapon.ToString().ToUpperInvariant()}" : "SELECT WEAPON";
            if (CooldownsText)
            {
                var weaponCooldown = Session.Weapon ? Session.Weapon.CooldownRemaining : 0;
                var shieldCooldown = Session.Defense ? Session.Defense.ShieldCooldownRemaining : 0;
                var empCooldown = Session.Defense ? Session.Defense.EmpCooldownRemaining : 0;
                CooldownsText.text = $"COOLDOWN   Weapon {weaponCooldown:0.0}s   Shield {shieldCooldown:0.0}s   EMP {empCooldown:0.0}s";
            }
            StartPanel.SetActive(Session.State == MatchState.Ready);
            PausePanel.SetActive(Session.State == MatchState.Paused);
            ResultPanel.SetActive(Session.State == MatchState.Won || Session.State == MatchState.Lost);
            var gameplayActionsEnabled = Session.State == MatchState.Playing;
            if (BulletButton) BulletButton.interactable = gameplayActionsEnabled;
            if (RocketButton) RocketButton.interactable = gameplayActionsEnabled;
            if (MineButton) MineButton.interactable = gameplayActionsEnabled;
            if (ShieldButton) ShieldButton.interactable = gameplayActionsEnabled;
            if (EmpButton) EmpButton.interactable = gameplayActionsEnabled;
        }
    }
}
