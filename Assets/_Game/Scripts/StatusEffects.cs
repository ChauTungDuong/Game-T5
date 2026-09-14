using System;
using UnityEngine;

namespace CoreGuard
{
    public sealed class StatusEffects : MonoBehaviour
    {
        public GameSession Session;
        public float BaseSpeed = 4f;
        public bool IsSlowed => slowRemaining > 0;
        public bool IsBoosted => boostRemaining > 0;
        public float CurrentSpeed => BaseSpeed * (IsSlowed ? slowMultiplier : 1f) * (IsBoosted ? boostMultiplier : 1f);
        public float SlowRemaining => Mathf.Max(0, slowRemaining);
        public float BoostRemaining => Mathf.Max(0, boostRemaining);
        public event Action Changed;

        private float slowRemaining;
        private float boostRemaining;
        private float slowMultiplier = .5f;
        private float boostMultiplier = 1.5f;

        public void ApplySlow(float multiplier, float duration)
        {
            if (duration <= 0 || float.IsNaN(duration)) return;
            slowMultiplier = Mathf.Clamp(multiplier, .01f, 1f);
            slowRemaining = duration;
            Changed?.Invoke();
        }

        public void ApplyBoost(float multiplier, float duration)
        {
            if (duration <= 0 || float.IsNaN(duration)) return;
            boostMultiplier = Mathf.Max(1f, multiplier);
            boostRemaining = duration;
            Changed?.Invoke();
        }

        public void Advance(float delta)
        {
            if (delta <= 0 || (Session && Session.State != MatchState.Playing)) return;
            var wasChanged = slowRemaining > 0 || boostRemaining > 0;
            slowRemaining = Mathf.Max(0, slowRemaining - delta);
            boostRemaining = Mathf.Max(0, boostRemaining - delta);
            if (wasChanged) Changed?.Invoke();
        }

        public void ResetRun()
        {
            slowRemaining = 0;
            boostRemaining = 0;
            slowMultiplier = .5f;
            boostMultiplier = 1.5f;
            Changed?.Invoke();
        }
    }
}
