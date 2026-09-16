using System.Collections.Generic;
using UnityEngine;

namespace CoreGuard
{
    public static class Explosion
    {
        public static int ApplyAt(Vector2 center, float radius, float damage, GameSession session)
        {
            if (radius <= 0 || damage <= 0 || float.IsNaN(radius) || float.IsNaN(damage)) return 0;

            var affected = new HashSet<EnemyController>();
            foreach (var collider in Physics2D.OverlapCircleAll(center, radius))
            {
                var enemy = collider ? collider.GetComponentInParent<EnemyController>() : null;
                if (!enemy || !enemy.IsAlive || (session && enemy.Session != session)) continue;
                if (!affected.Add(enemy)) continue;
                enemy.ApplyDamage(damage);
            }

            return affected.Count;
        }
    }
}
