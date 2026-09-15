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
        public LineRenderer WarningRing;
        public float Radius = 3f;
        public float WarningDuration = .8f;
        public int OccupantCount => occupants.Count;
        public event Action<EnemyController> EnemyEntered;

        private readonly HashSet<EnemyController> occupants = new HashSet<EnemyController>();
        private CircleCollider2D trigger;
        private float warningRemaining;
        private float warningElapsed;
        private float warningBaseWidth;
        private Color warningBaseStartColor;
        private Color warningBaseEndColor;
        private bool warningVisualInitialized;

        private void Awake()
        {
            trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = Radius;
        }

        private void Update()
        {
            if (!WarningRing || warningRemaining <= 0f) return;
            warningElapsed += Time.deltaTime;
            warningRemaining = Mathf.Max(0f, warningRemaining - Time.deltaTime);
            var pulse = .5f + .5f * Mathf.Sin(warningElapsed * Mathf.PI * 5f);
            WarningRing.startColor = WithAlpha(warningBaseStartColor, Mathf.Lerp(.12f, .98f, pulse));
            WarningRing.endColor = WithAlpha(warningBaseEndColor, Mathf.Lerp(.12f, .98f, pulse));
            WarningRing.widthMultiplier = warningBaseWidth * Mathf.Lerp(.9f, 1.8f, pulse);
            if (warningRemaining <= 0f) ResetWarningVisual();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var enemy = other ? other.GetComponentInParent<EnemyController>() : null;
            if (!enemy || !enemy.IsAlive || (Session && enemy.Session != Session)) return;
            if (!occupants.Add(enemy)) return;
            BeginWarningVisual();
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

        public void ResetOccupancy()
        {
            occupants.Clear();
            ResetWarningVisual();
        }

        private void OnDisable()
        {
            occupants.Clear();
            ResetWarningVisual();
        }

        private void BeginWarningVisual()
        {
            if (!WarningRing) return;
            if (!warningVisualInitialized)
            {
                warningBaseWidth = WarningRing.widthMultiplier;
                warningBaseStartColor = WarningRing.startColor;
                warningBaseEndColor = WarningRing.endColor;
                warningVisualInitialized = true;
            }
            warningElapsed = 0f;
            warningRemaining = Mathf.Max(.1f, WarningDuration);
        }

        private void ResetWarningVisual()
        {
            warningRemaining = 0f;
            if (!WarningRing || !warningVisualInitialized) return;
            WarningRing.widthMultiplier = warningBaseWidth;
            WarningRing.startColor = warningBaseStartColor;
            WarningRing.endColor = warningBaseEndColor;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
