using System;
using UnityEngine;
namespace CoreGuard
{
    public sealed class PlayerStats : MonoBehaviour, IDamageable
    {
        public const float MaxHP = 100f;
        public const float MaxArmor = 100f;
        public const float MaxEnergy = 100f;
        public const float EnergyRegenPerSecond = 2f;
        public const float SkillEnergyCost = 20f;
        public float HP { get; private set; }
        public float Energy { get; private set; }
        public float Armor => Energy;
        public int Coins { get; private set; }
        public WorldHealthBar HealthBar;
        public event Action Changed;
        private bool deathFeedbackPlayed;
        private void Awake() => RefreshHealthBar();
        public void ResetStats()
        {
            HP = MaxHP; Energy = MaxEnergy; Coins = 0;
            deathFeedbackPlayed = false;
            NotifyChanged();
        }
        public void ApplyDamage(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || HP <= 0) return;
            // Being hit subtracts HP directly, not energy
            HP = Mathf.Max(0, HP - amount);
            NotifyChanged();
        }
        public void ApplyEnvironmentHit(float hpLoss, float armorLoss)
        {
            if ((hpLoss <= 0 || float.IsNaN(hpLoss)) && (armorLoss <= 0 || float.IsNaN(armorLoss))) return;
            HP = Mathf.Max(0, HP - Mathf.Max(0, hpLoss));
            Energy = Mathf.Max(0, Energy - Mathf.Max(0, armorLoss));
            NotifyChanged();
        }
        public void Heal(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || HP <= 0) return;
            HP = Mathf.Min(200f, HP + amount);
            NotifyChanged();
        }
        public bool CanUseSkill(float cost = SkillEnergyCost) => Energy >= cost;
        public bool TryConsumeEnergy(float amount = SkillEnergyCost)
        {
            if (Energy < amount) return false;
            Energy = Mathf.Max(0, Energy - amount);
            NotifyChanged();
            return true;
        }
        public void RegenerateEnergy(float amount)
        {
            if (amount <= 0 || Energy >= MaxEnergy) return;
            Energy = Mathf.Min(MaxEnergy, Energy + amount);
            NotifyChanged();
        }
        public void Advance(float delta)
        {
            if (delta <= 0) return;
            if (Energy < MaxEnergy)
            {
                Energy = Mathf.Min(MaxEnergy, Energy + EnergyRegenPerSecond * delta);
                NotifyChanged();
            }
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
            if (HP <= 0 && !deathFeedbackPlayed)
            {
                deathFeedbackPlayed = true;
                var presenter = GetComponent<CombatVfxPresenter>();
                if (presenter) presenter.SpawnExplosion(transform.position, .3f);
            }
            Changed?.Invoke();
        }
    }
}
