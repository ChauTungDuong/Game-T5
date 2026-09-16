using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuard
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ForbiddenZone : MonoBehaviour
    {
        public GameSession Session;
        public AudioService Audio;
        public float Radius = 3f;
        public int OccupantCount => occupants.Count;
        public event Action<EnemyController> EnemyEntered;

        private readonly HashSet<EnemyController> occupants = new HashSet<EnemyController>();
        private CircleCollider2D trigger;

        private void Awake()
        {
            trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = Radius;
            var ring = GetComponentInChildren<LineRenderer>();
            if (ring)
            {
                ring.widthMultiplier = .05f;
                ring.startColor = new Color(1f, .82f, .12f, .9f);
                ring.endColor = ring.startColor;
                var shader = Shader.Find("Sprites/Default");
                if (shader && (ring.sharedMaterial == null || ring.sharedMaterial.name.Contains("Default-Line")))
                    ring.material = new Material(shader);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other ? other.GetComponentInParent<EnemyController>() : null;
            if (!enemy || !enemy.IsAlive || (Session && enemy.Session != Session)) return;
            if (!occupants.Add(enemy)) return;
            EnemyEntered?.Invoke(enemy);
            if (Audio) Audio.RequestAlert();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var enemy = other ? other.GetComponentInParent<EnemyController>() : null;
            if (enemy) occupants.Remove(enemy);
        }

        private void FixedUpdate()
        {
            occupants.RemoveWhere(enemy => !enemy || !enemy.IsAlive || !enemy.gameObject.activeInHierarchy);
        }

        public void ResetOccupancy() => occupants.Clear();

        private void OnDisable() => occupants.Clear();
    }
}
