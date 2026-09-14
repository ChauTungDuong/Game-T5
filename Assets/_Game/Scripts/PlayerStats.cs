using System;
using UnityEngine;
namespace CoreGuard
{
    public sealed class PlayerStats : MonoBehaviour, IDamageable
    {
        public float HP { get; private set; }
        public float Armor { get; private set; }
        public int Coins { get; private set; }
        public event Action Changed;
        public void ResetStats()
        {
            HP = 100; Armor = 50; Coins = 0;
            Changed?.Invoke();
        }
        public void ApplyDamage(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || HP <= 0) return;
            var absorbed = Mathf.Min(Armor, amount);
            Armor -= absorbed;
            HP = Mathf.Max(0, HP - (amount - absorbed));
            Changed?.Invoke();
        }
        public void ApplyEnvironmentHit(float hpLoss, float armorLoss)
        {
            if ((hpLoss <= 0 || float.IsNaN(hpLoss)) && (armorLoss <= 0 || float.IsNaN(armorLoss))) return;
            HP = Mathf.Max(0, HP - Mathf.Max(0, hpLoss));
            Armor = Mathf.Max(0, Armor - Mathf.Max(0, armorLoss));
            Changed?.Invoke();
        }
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Changed?.Invoke();
        }
    }
}
