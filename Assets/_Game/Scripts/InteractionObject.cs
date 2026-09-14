using System;
using UnityEngine;

namespace CoreGuard
{
    public enum InteractionKind
    {
        X,
        Y,
        Z,
    }

    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class InteractionObject : MonoBehaviour
    {
        public InteractionKind Kind;
        public GameSession Session;
        public PlayerStats Player;
        public bool IsConsumed { get; private set; }
        public event Action<InteractionObject> Activated;

        private CircleCollider2D trigger;

        private void Awake()
        {
            trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .45f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other ? other.GetComponentInParent<PlayerStats>() : null;
            if (player) ApplyTo(player);
        }

        public bool ApplyTo(PlayerStats target)
        {
            if (!target || IsConsumed || (Player && target != Player)) return false;
            Player = target;
            switch (Kind)
            {
                case InteractionKind.X:
                    target.ApplyEnvironmentHit(20, 10);
                    Consume();
                    return true;

                case InteractionKind.Y:
                    var effects = target.GetComponent<StatusEffects>();
                    if (effects) effects.ApplySlow(.5f, 3f);
                    if (Session && Session.Defense) Session.Defense.BreakShield();
                    Consume();
                    return true;

                case InteractionKind.Z:
                    target.AddCoins(10);
                    var boost = target.GetComponent<StatusEffects>();
                    if (boost) boost.ApplyBoost(1.5f, 4f);
                    Consume();
                    return true;

                default:
                    return false;
            }
        }

        public void ResetObject()
        {
            IsConsumed = false;
            gameObject.SetActive(true);
        }

        public void HideForCycle()
        {
            gameObject.SetActive(false);
        }

        public void ShowForCycle(Vector2 position)
        {
            transform.position = position;
            ResetObject();
        }

        private void Consume()
        {
            IsConsumed = true;
            gameObject.SetActive(false);
            Activated?.Invoke(this);
        }
    }
}
