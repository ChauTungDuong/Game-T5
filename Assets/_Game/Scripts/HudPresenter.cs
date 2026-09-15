using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard
{
    public sealed class HudPresenter : MonoBehaviour
    {
        public GameSession Session;
        public Text StatsText, CoreText, TimerText, StateText, ResultText;
        public Text WeaponText, CooldownsText, FeedbackText;
        public Image ResultImage, CountdownImage;
        public Sprite WinSprite, LoseSprite, CountdownZero, CountdownOne, CountdownTwo, CountdownThree;
        public float CountdownDuration = 3f;
        public GameObject StartPanel, PausePanel, ResultPanel;
        public Button StartButton, ResumeButton, RetryButton;
        public Button BulletButton, RocketButton, MineButton, ShieldButton, EmpButton;
        private GameSession boundSession;
        private float feedbackRemaining;
        private float countdownRemaining;
        private bool countdownActive;

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
            if (Session.Effects) Session.Effects.Changed += Refresh;
            if (Session.Weapon)
            {
                Session.Weapon.WeaponSelected += HandleWeaponSelected;
                Session.Weapon.ActionRejected += ShowActionRejected;
            }
            Session.Reset += HandleSessionReset;
            StartButton.onClick.AddListener(BeginStartCountdown);
            if (StartButton) StartButton.onClick.AddListener(PlayUiClick);
            ResumeButton.onClick.AddListener(Session.TogglePause);
            if (ResumeButton) ResumeButton.onClick.AddListener(PlayUiClick);
            RetryButton.onClick.AddListener(Session.Retry);
            if (RetryButton) RetryButton.onClick.AddListener(PlayUiClick);
            if (BulletButton) BulletButton.onClick.AddListener(SelectBullet);
            if (BulletButton) BulletButton.onClick.AddListener(PlayUiClick);
            if (RocketButton) RocketButton.onClick.AddListener(SelectRocket);
            if (RocketButton) RocketButton.onClick.AddListener(PlayUiClick);
            if (MineButton) MineButton.onClick.AddListener(SelectMine);
            if (MineButton) MineButton.onClick.AddListener(PlayUiClick);
            if (ShieldButton) ShieldButton.onClick.AddListener(ActivateShield);
            if (ShieldButton) ShieldButton.onClick.AddListener(PlayUiClick);
            if (EmpButton) EmpButton.onClick.AddListener(ActivateEmp);
            if (EmpButton) EmpButton.onClick.AddListener(PlayUiClick);
            Refresh();
        }

        private void Unbind()
        {
            if (!boundSession) return;
            boundSession.Changed -= Refresh;
            if (boundSession.Player) boundSession.Player.Changed -= Refresh;
            if (boundSession.Core) boundSession.Core.Changed -= Refresh;
            if (boundSession.Defense) boundSession.Defense.Changed -= Refresh;
            if (boundSession.Effects) boundSession.Effects.Changed -= Refresh;
            if (boundSession.Weapon)
            {
                boundSession.Weapon.WeaponSelected -= HandleWeaponSelected;
                boundSession.Weapon.ActionRejected -= ShowActionRejected;
            }
            boundSession.Reset -= HandleSessionReset;
            if (StartButton) StartButton.onClick.RemoveListener(BeginStartCountdown);
            if (StartButton) StartButton.onClick.RemoveListener(PlayUiClick);
            if (ResumeButton) ResumeButton.onClick.RemoveListener(boundSession.TogglePause);
            if (ResumeButton) ResumeButton.onClick.RemoveListener(PlayUiClick);
            if (RetryButton) RetryButton.onClick.RemoveListener(boundSession.Retry);
            if (RetryButton) RetryButton.onClick.RemoveListener(PlayUiClick);
            if (BulletButton) BulletButton.onClick.RemoveListener(SelectBullet);
            if (BulletButton) BulletButton.onClick.RemoveListener(PlayUiClick);
            if (RocketButton) RocketButton.onClick.RemoveListener(SelectRocket);
            if (RocketButton) RocketButton.onClick.RemoveListener(PlayUiClick);
            if (MineButton) MineButton.onClick.RemoveListener(SelectMine);
            if (MineButton) MineButton.onClick.RemoveListener(PlayUiClick);
            if (ShieldButton) ShieldButton.onClick.RemoveListener(ActivateShield);
            if (ShieldButton) ShieldButton.onClick.RemoveListener(PlayUiClick);
            if (EmpButton) EmpButton.onClick.RemoveListener(ActivateEmp);
            if (EmpButton) EmpButton.onClick.RemoveListener(PlayUiClick);
            boundSession = null;
        }

        private void SelectBullet() { Session.Weapon?.Select(WeaponKind.Bullet); Refresh(); }
        private void SelectRocket() { Session.Weapon?.Select(WeaponKind.Rocket); Refresh(); }
        private void SelectMine() { Session.Weapon?.Select(WeaponKind.Mine); Refresh(); }
        private void ActivateShield() => Session.Defense?.TryActivateShield();
        private void ActivateEmp() => Session.Defense?.TryActivateEmp();
        private void HandleWeaponSelected(WeaponKind _) => Refresh();
        private void PlayUiClick() => Session?.Audio?.PlayUiClick();

        private void BeginStartCountdown()
        {
            if (!Session || Session.State != MatchState.Ready || countdownActive) return;
            countdownActive = true;
            countdownRemaining = Mathf.Max(.1f, CountdownDuration);
            RefreshCountdown();
            Refresh();
        }

        private void ShowActionRejected(string message)
        {
            if (!FeedbackText) return;
            FeedbackText.text = message;
            FeedbackText.gameObject.SetActive(true);
            feedbackRemaining = 1.25f;
        }

        private void ClearFeedback()
        {
            feedbackRemaining = 0f;
            if (!FeedbackText) return;
            FeedbackText.text = string.Empty;
            FeedbackText.gameObject.SetActive(false);
        }

        private void HandleSessionReset()
        {
            ClearFeedback();
            countdownActive = false;
            countdownRemaining = 0f;
            if (CountdownImage) CountdownImage.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (countdownActive)
            {
                countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.unscaledDeltaTime);
                if (countdownRemaining <= 0f)
                {
                    countdownActive = false;
                    if (CountdownImage) CountdownImage.gameObject.SetActive(false);
                    Session.StartMatch();
                }
                else RefreshCountdown();
            }
            if (!FeedbackText || feedbackRemaining <= 0f) return;
            feedbackRemaining = Mathf.Max(0f, feedbackRemaining - Time.unscaledDeltaTime);
            if (feedbackRemaining <= 0f) ClearFeedback();
        }

        private void RefreshCountdown()
        {
            if (!CountdownImage) return;
            var sprite = countdownRemaining <= .18f ? CountdownZero :
                countdownRemaining > 2f ? CountdownThree :
                countdownRemaining > 1f ? CountdownTwo : CountdownOne;
            CountdownImage.sprite = sprite;
            CountdownImage.enabled = sprite != null;
            CountdownImage.gameObject.SetActive(sprite != null);
        }

        private void Refresh()
        {
            StatsText.text = $"PLAYER HP {Session.Player.HP:0}/{PlayerStats.MaxHP:0}\nARMOR {Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}\nCOINS {Session.Player.Coins}";
            CoreText.text = $"CORE {Session.Core.HP:0}/{CoreHealth.MaxHP:0}";
            TimerText.text = $"TIME {Session.Remaining:00.0}";
            StateText.text = Session.State.ToString().ToUpperInvariant();
            ResultText.text = Session.State == MatchState.Won ? "CORE SECURED — YOU WIN" : "CORE OFFLINE — LOST — TRY AGAIN";
            if (WeaponText)
                WeaponText.text = Session.Weapon ? $"WEAPON: {Session.Weapon.SelectedWeapon.ToString().ToUpperInvariant()}" : "WEAPON: NONE";
            if (ResultImage)
            {
                var hasResult = Session.State == MatchState.Won || Session.State == MatchState.Lost;
                ResultImage.sprite = Session.State == MatchState.Won ? WinSprite : LoseSprite;
                ResultImage.enabled = hasResult && ResultImage.sprite != null;
            }
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
                var speed = Session.Effects ? Session.Effects.CurrentSpeed :
                    Session.Motor ? Session.Motor.CurrentSpeed : 0f;
                CooldownsText.text = $"SPEED: {speed:0.0}\nSHIELD: {shield}\nEMP: {emp}";
            }
            StartPanel.SetActive(Session.State == MatchState.Ready && !countdownActive);
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
