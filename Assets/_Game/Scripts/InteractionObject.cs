using System;
using UnityEngine;

namespace CoreGuard
{
    public enum InteractionKind
    {
        X = 0,          // X đỏ: -20 HP
        Y = 1,          // Y tím: Giảm tốc chạy
        Z = 2,          // Z xanh lá: +20 HP
        X_Energy = 3,   // X cam: -20 Năng lượng
        Y_Shield = 4,   // Y lam: Phá khiên khi bật khiên
        Z_Speed = 5,    // Z vàng: Tăng tốc chạy
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
        private bool yTriggeredUntilExit;

        private void Awake()
        {
            trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = .6f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other ? (other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>()) : null;
            if (player && !(Kind == InteractionKind.Y && yTriggeredUntilExit)) ApplyTo(player);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other ? (other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>()) : null;
            if (player && !(Kind == InteractionKind.Y && yTriggeredUntilExit)) ApplyTo(player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (Kind != InteractionKind.Y) return;
            var player = other ? (other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>()) : null;
            if (player && (!Player || player == Player)) yTriggeredUntilExit = false;
        }

        public bool ApplyTo(PlayerStats target)
        {
            if (!target || IsConsumed || (Kind == InteractionKind.Y && yTriggeredUntilExit)) return false;
            Player = target;
            var applied = false;
            switch (Kind)
            {
                case InteractionKind.X:
                    target.ApplyEnvironmentHit(20f, 0f);
                    applied = true;
                    break;

                case InteractionKind.X_Energy:
                    target.ApplyEnvironmentHit(0f, 20f);
                    applied = true;
                    break;

                case InteractionKind.Y:
                    var slowEffects = target.GetComponent<StatusEffects>();
                    if (slowEffects) slowEffects.ApplySlow(.5f, 3f);
                    applied = true;
                    break;

                case InteractionKind.Y_Shield:
                    if (Session && Session.Defense) Session.Defense.BreakShield();
                    applied = true;
                    break;

                case InteractionKind.Z:
                    target.Heal(20f);
                    applied = true;
                    break;

                case InteractionKind.Z_Speed:
                    var boostEffects = target.GetComponent<StatusEffects>();
                    if (boostEffects) boostEffects.ApplyBoost(1.5f, 4f);
                    applied = true;
                    break;

                default:
                    return false;
            }

            if (applied)
            {
                InteractionFeedbackCue.Spawn(Kind, transform.position);
                if (Session && Session.Audio)
                    Session.Audio.PlaySfx(Session.Audio.GetInteractionClip(Kind));
                if (Kind == InteractionKind.Y) yTriggeredUntilExit = true;
                else Consume();
            }
            return applied;
        }

        public void ResetObject()
        {
            IsConsumed = false;
            yTriggeredUntilExit = false;
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
