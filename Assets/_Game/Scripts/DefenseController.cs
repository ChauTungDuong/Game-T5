using System;
using UnityEngine;

namespace CoreGuard
{
    public sealed class DefenseController : MonoBehaviour
    {
        public GameSession Session;
        public PlayerStats Player;
        public float ShieldDuration = 3f;
        public int ShieldMaxHits = 3;
        public float ShieldCooldown = 8f;
        public float EmpRadius = 3f;
        public float EmpDuration = 2f;
        public float EmpCooldown = 6f;
        public bool ShieldActive => shieldRemaining > 0 && shieldHitsRemaining > 0;
        public int ShieldHitsRemaining => shieldHitsRemaining;
        public float ShieldRemaining => Mathf.Max(0, shieldRemaining);
        public float ShieldCooldownRemaining => Mathf.Max(0, shieldCooldownRemaining);
        public float EmpCooldownRemaining => Mathf.Max(0, empCooldownRemaining);
        public int LastEmpAffectedCount { get; private set; }
        public event Action Changed;
        public event Action ShieldActivated;
        public event Action ShieldBlocked;
        public event Action EmpActivated;
        public event Action<int> EmpResolved;

        private float shieldRemaining;
        private int shieldHitsRemaining;
        private float shieldCooldownRemaining;
        private float empCooldownRemaining;

        private void Awake()
        {
            if (!Player) Player = GetComponent<PlayerStats>();
        }

        public bool TryActivateShield()
        {
            if (!CanUse() || ShieldActive || shieldCooldownRemaining > 0) return false;
            shieldRemaining = ShieldDuration;
            shieldHitsRemaining = ShieldMaxHits;
            shieldCooldownRemaining = ShieldCooldown;
            ShieldActivated?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public bool TryBlockProjectile()
        {
            if (!CanUse() || !ShieldActive) return false;
            shieldHitsRemaining--;
            ShieldBlocked?.Invoke();
            if (shieldHitsRemaining <= 0) BreakShield();
            else Changed?.Invoke();
            return true;
        }

        public void BreakShield()
        {
            if (shieldRemaining <= 0 && shieldHitsRemaining <= 0) return;
            shieldRemaining = 0;
            shieldHitsRemaining = 0;
            Changed?.Invoke();
        }

        public bool TryActivateEmp()
        {
            if (!CanUse() || empCooldownRemaining > 0 || !Player) return false;
            var affectedCount = 0;
            var radiusSquared = EmpRadius * EmpRadius;
            foreach (var enemy in EnemyController.ActiveEnemies)
            {
                if (!enemy || !enemy.IsAlive || enemy.Session != Session) continue;
                if (((Vector2)(enemy.transform.position - Player.transform.position)).sqrMagnitude > radiusSquared) continue;
                enemy.Stun(EmpDuration);
                affectedCount++;
            }

            LastEmpAffectedCount = affectedCount;
            empCooldownRemaining = EmpCooldown;
            EmpActivated?.Invoke();
            EmpResolved?.Invoke(LastEmpAffectedCount);
            Changed?.Invoke();
            return true;
        }

        public void Advance(float delta)
        {
            if (delta <= 0 || (Session && Session.State != MatchState.Playing)) return;
            var wasChanged = false;
            if (shieldCooldownRemaining > 0)
            {
                shieldCooldownRemaining = Mathf.Max(0, shieldCooldownRemaining - delta);
                wasChanged = true;
            }
            if (empCooldownRemaining > 0)
            {
                empCooldownRemaining = Mathf.Max(0, empCooldownRemaining - delta);
                wasChanged = true;
            }
            if (shieldRemaining > 0)
            {
                shieldRemaining = Mathf.Max(0, shieldRemaining - delta);
                wasChanged = true;
                if (shieldRemaining <= 0) shieldHitsRemaining = 0;
            }
            if (wasChanged) Changed?.Invoke();
        }

        public void ResetRun()
        {
            shieldRemaining = 0;
            shieldHitsRemaining = 0;
            shieldCooldownRemaining = 0;
            empCooldownRemaining = 0;
            LastEmpAffectedCount = 0;
            Changed?.Invoke();
        }

        private bool CanUse() => Session && Session.State == MatchState.Playing;
    }
}
