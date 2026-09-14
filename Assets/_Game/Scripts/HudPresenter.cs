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
        private void OnDisable() => Unbind();

        public void Bind()
        {
            Unbind();
            boundSession = Session;
            Session.Changed += Refresh;
            Session.Player.Changed += Refresh;
            Session.Core.Changed += Refresh;
            if (Session.Defense) Session.Defense.Changed += Refresh;
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
            if (boundSession.Defense) boundSession.Defense.Changed -= Refresh;
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

        private void SelectBullet() { Session.Weapon?.Select(WeaponKind.Bullet); Refresh(); }
        private void SelectRocket() { Session.Weapon?.Select(WeaponKind.Rocket); Refresh(); }
        private void SelectMine() { Session.Weapon?.Select(WeaponKind.Mine); Refresh(); }
        private void ActivateShield() => Session.Defense?.TryActivateShield();
        private void ActivateEmp() => Session.Defense?.TryActivateEmp();

        private void Refresh()
        {
            StatsText.text = $"PLAYER HP {Session.Player.HP:0}/{PlayerStats.MaxHP:0}\nARMOR {Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}\nCOINS {Session.Player.Coins}";
            CoreText.text = $"CORE {Session.Core.HP:0}/{CoreHealth.MaxHP:0}";
            TimerText.text = $"TIME {Session.Remaining:00.0}";
            StateText.text = Session.State.ToString().ToUpperInvariant();
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
