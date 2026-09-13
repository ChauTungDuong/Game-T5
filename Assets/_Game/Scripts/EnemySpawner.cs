using System.Collections.Generic;
using UnityEngine;
namespace CoreGuard
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        public GameSession Session;
        public CoreHealth Core;
        public EnemyController Prefab;
        public Transform[] Gates;
        public List<EnemyController> Living { get; } = new List<EnemyController>();
        public int NextGate { get; private set; }
        private float elapsed;
        public void ResetSpawns()
        {
            foreach (var enemy in GetComponentsInChildren<EnemyController>(true))
            {
                enemy.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(enemy.gameObject);
                else DestroyImmediate(enemy.gameObject);
            }
            Living.Clear(); elapsed = 0; NextGate = 0;
        }
        public void Advance(float delta)
        {
            if (Session.State != MatchState.Playing || delta <= 0) return;
            Living.RemoveAll(enemy => !enemy || !enemy.IsAlive);
            elapsed += delta;
            while (elapsed >= 5f)
            {
                elapsed -= 5f;
                if (Living.Count >= 6) continue;
                var enemy = Instantiate(Prefab, Gates[NextGate].position, Quaternion.identity, transform);
                enemy.Initialize(Session, Core);
                enemy.gameObject.SetActive(true);
                Living.Add(enemy);
                NextGate = (NextGate + 1) % Gates.Length;
            }
        }
    }
}
