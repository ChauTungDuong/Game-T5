using System;
using UnityEngine;
namespace CoreGuard
{
    public sealed class PlayerStats : MonoBehaviour, IDamageable
    {
        public const float MaxHP = 100f;
        public const float MaxArmor = 50f;
        public float HP { get; private set; }
        public float Armor { get; private set; }
        public int Coins { get; private set; }
        public WorldHealthBar HealthBar;
        public event Action Changed;
        private void Awake() => RefreshHealthBar();
        public void ResetStats()
        {
            HP = MaxHP; Armor = MaxArmor; Coins = 0;
            NotifyChanged();
        }
        public void ApplyDamage(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || HP <= 0) return;
            var absorbed = Mathf.Min(Armor, amount);
            Armor -= absorbed;
            HP = Mathf.Max(0, HP - (amount - absorbed));
            NotifyChanged();
        }
        public void ApplyEnvironmentHit(float hpLoss, float armorLoss)
        {
            if ((hpLoss <= 0 || float.IsNaN(hpLoss)) && (armorLoss <= 0 || float.IsNaN(armorLoss))) return;
            HP = Mathf.Max(0, HP - Mathf.Max(0, hpLoss));
            Armor = Mathf.Max(0, Armor - Mathf.Max(0, armorLoss));
            NotifyChanged();
        }
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            NotifyChanged();
        }
        public void RefreshHealthBar()
        {
            if (!HealthBar) HealthBar = GetComponentInChildren<WorldHealthBar>();
            if (HealthBar) HealthBar.SetHealth(HP, MaxHP);
        }
        private void NotifyChanged()
        {
            RefreshHealthBar();
            Changed?.Invoke();
        }
    }
}
