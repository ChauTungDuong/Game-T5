using System;
using UnityEngine;
namespace CoreGuard
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class CoreHealth : MonoBehaviour, IDamageable
    {
        public const float MaxHP = 100f;
        public float HP { get; private set; }
        public event Action Changed;
        private LineRenderer healthBack;
        private LineRenderer healthFill;

        private void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
            CreateHealthBar();
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

        private void CreateHealthBar()
        {
            if (!Application.isPlaying || healthFill) return;
            healthBack = CreateBar("Core health background", new Color(.025f, .045f, .07f, .95f), .14f);
            healthFill = CreateBar("Core health", new Color(.2f, .95f, .8f, 1f), .18f);
        }

        private LineRenderer CreateBar(string name, Color color, float width)
        {
            var barObject = new GameObject(name);
            barObject.transform.SetParent(transform, false);
            barObject.transform.localPosition = new Vector3(0, 1.05f, 0);
            var bar = barObject.AddComponent<LineRenderer>();
            bar.useWorldSpace = false;
            bar.positionCount = 2;
            bar.widthMultiplier = width;
            bar.startColor = color;
            bar.endColor = color;
            bar.sortingOrder = 7;
            bar.SetPosition(0, new Vector3(-1f, 0, 0));
            bar.SetPosition(1, new Vector3(1f, 0, 0));
            return bar;
        }

        private void RefreshHealthBar()
        {
            if (!healthFill) return;
            var ratio = Mathf.Clamp01(HP / MaxHP);
            healthFill.SetPosition(1, new Vector3(-1f + 2f * ratio, 0, 0));
            var color = ratio > .5f
                ? Color.Lerp(new Color(1f, .78f, .2f), new Color(.2f, .95f, .8f), (ratio - .5f) * 2f)
                : Color.Lerp(new Color(1f, .2f, .24f), new Color(1f, .78f, .2f), ratio * 2f);
            healthFill.startColor = color;
            healthFill.endColor = color;
            healthBack.enabled = HP > 0;
            healthFill.enabled = HP > 0;
        }
    }
}
