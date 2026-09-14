using System;
using UnityEngine;
namespace CoreGuard
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CoreHealth : MonoBehaviour, IDamageable
    {
        public const float MaxHP = 100f;
        public float HP { get; private set; }
        public WorldHealthBar HealthBar;
        public event Action Changed;

        private void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
            RefreshHealthBar();
        }

        public void ResetHealth()
        {
            HP = MaxHP;
            RefreshHealthBar();
            Changed?.Invoke();
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || HP <= 0) return;
            HP = Mathf.Max(0, HP - amount);
            RefreshHealthBar();
            Changed?.Invoke();
        }

        public void RefreshHealthBar()
        {
            if (!HealthBar) HealthBar = GetComponentInChildren<WorldHealthBar>();
            if (HealthBar) HealthBar.SetHealth(HP, MaxHP);
        }
    }
}
