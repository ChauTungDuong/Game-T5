using System;
using UnityEngine;
namespace CoreGuard
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CoreHealth : MonoBehaviour, IDamageable
    {
        public float HP { get; private set; }
        public event Action Changed;
        public void ResetHealth() { HP = 100; Changed?.Invoke(); }
        public void ApplyDamage(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || HP <= 0) return;
            HP = Mathf.Max(0, HP - amount);
            Changed?.Invoke();
        }
    }
}
