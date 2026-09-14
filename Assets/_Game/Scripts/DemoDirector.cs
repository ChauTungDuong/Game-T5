using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard
{
    public sealed class DemoDirector : MonoBehaviour
    {
        public GameSession Session;
        public GameObject Panel;
        public Button ResetButton;
        public Button ZoneEnemyButton;
        public Button ShooterButton;
        public Button ClusterButton;
        public Button RestoreInteractionsButton;
        public bool IsDemoMode { get; private set; }

        private GameSession boundSession;

        private void Awake()
        {
            if (!Session) Session = GetComponent<GameSession>();
        }

        private void OnEnable()
        {
            if (Session) Bind();
        }

        private void OnDisable() => Unbind();

        public void Bind()
        {
            Unbind();
            boundSession = Session;
            if (ResetButton) ResetButton.onClick.AddListener(ResetScenario);
            if (ZoneEnemyButton) ZoneEnemyButton.onClick.AddListener(SpawnZoneEnemy);
            if (ShooterButton) ShooterButton.onClick.AddListener(SpawnShooter);
            if (ClusterButton) ClusterButton.onClick.AddListener(SpawnEnemyCluster);
            if (RestoreInteractionsButton) RestoreInteractionsButton.onClick.AddListener(RestoreInteractions);
            SetPanel(false);
        }

        public void Toggle()
        {
            if (!Session || Session.State != MatchState.Playing) return;
            IsDemoMode = !IsDemoMode;
            if (Session.Spawner) Session.Spawner.AutoSpawn = !IsDemoMode;
            SetPanel(IsDemoMode);
        }

        public void ResetScenario()
        {
            if (!IsDemoMode || !Session) return;
            Session.ResetForDemo();
            if (Session.Spawner) Session.Spawner.AutoSpawn = false;
        }

        public void SpawnZoneEnemy()
        {
            if (!IsDemoMode || !Session || !Session.Spawner) return;
            Session.Spawner.SpawnAt(new Vector2(4.2f, 0));
        }

        public void SpawnShooter()
        {
            if (!IsDemoMode || !Session || !Session.Spawner) return;
            Session.Spawner.SpawnAt(new Vector2(2.5f, 1.2f));
        }

        public void SpawnEnemyCluster()
        {
            if (!IsDemoMode || !Session || !Session.Spawner) return;
            Session.Spawner.SpawnAt(new Vector2(4.1f, -.75f));
            Session.Spawner.SpawnAt(new Vector2(4.1f, 0));
            Session.Spawner.SpawnAt(new Vector2(4.1f, .75f));
        }

        public void RestoreInteractions()
        {
            if (!IsDemoMode || !Session) return;
            Session.RestoreInteractions();
        }

        private void SetPanel(bool visible)
        {
            if (Panel) Panel.SetActive(visible);
        }

        private void Unbind()
        {
            if (!boundSession) return;
            if (ResetButton) ResetButton.onClick.RemoveListener(ResetScenario);
            if (ZoneEnemyButton) ZoneEnemyButton.onClick.RemoveListener(SpawnZoneEnemy);
            if (ShooterButton) ShooterButton.onClick.RemoveListener(SpawnShooter);
            if (ClusterButton) ClusterButton.onClick.RemoveListener(SpawnEnemyCluster);
            if (RestoreInteractionsButton) RestoreInteractionsButton.onClick.RemoveListener(RestoreInteractions);
            boundSession = null;
        }
    }
}
