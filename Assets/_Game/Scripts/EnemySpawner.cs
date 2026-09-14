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
        public bool AutoSpawn = true;
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
            Living.Clear(); elapsed = 0; NextGate = 0; AutoSpawn = true;
        }
        public void Advance(float delta)
        {
            if (Session.State != MatchState.Playing || delta <= 0) return;
            Living.RemoveAll(enemy => !enemy || !enemy.IsAlive);
            if (!AutoSpawn) return;
            elapsed += delta;
            while (elapsed >= 5f)
            {
                elapsed -= 5f;
                if (Living.Count >= 6) continue;
                if (SpawnAt(Gates[NextGate].position))
                    NextGate = (NextGate + 1) % Gates.Length;
            }
        }

        public EnemyController SpawnAt(Vector2 position)
        {
            Living.RemoveAll(enemy => !enemy || !enemy.IsAlive);
            if (!Session || Session.State != MatchState.Playing || !Prefab || Living.Count >= 6) return null;
            var enemy = Instantiate(Prefab, position, Quaternion.identity, transform);
            enemy.Initialize(Session, Core);
            enemy.gameObject.SetActive(true);
            Living.Add(enemy);
            return enemy;
        }
    }
}
