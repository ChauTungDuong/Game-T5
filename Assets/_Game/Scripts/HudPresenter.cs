using UnityEngine;
using UnityEngine.UI;
namespace CoreGuard
{
    public sealed class HudPresenter : MonoBehaviour
    {
        public GameSession Session;
        public Text StatsText, CoreText, TimerText, StateText, ResultText;
        public GameObject StartPanel, PausePanel, ResultPanel;
        public Button StartButton, ResumeButton, RetryButton;
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
            boundSession = null;
        }
        private void Refresh()
        {
            StatsText.text = $"HP {Session.Player.HP:0}     ARMOR {Session.Player.Armor:0}     COINS {Session.Player.Coins}";
            CoreText.text = $"CORE {Session.Core.HP:0}";
            TimerText.text = $"{Session.Remaining:00.0} s";
            StateText.text = Session.State.ToString().ToUpperInvariant();
            ResultText.text = Session.State == MatchState.Won ? "CORE SECURED — WON" : "CORE OFFLINE — LOST";
            StartPanel.SetActive(Session.State == MatchState.Ready);
            PausePanel.SetActive(Session.State == MatchState.Paused);
            ResultPanel.SetActive(Session.State == MatchState.Won || Session.State == MatchState.Lost);
        }
    }
}
