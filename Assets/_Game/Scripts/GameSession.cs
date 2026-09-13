using System;
using UnityEngine;
namespace CoreGuard
{
    public enum MatchState { Ready, Playing, Paused, Won, Lost }
    public sealed class GameSession : MonoBehaviour
    {
        public PlayerStats Player;
        public CoreHealth Core;
        public PlayerMotor Motor;
        public EnemySpawner Spawner;
        public InputReader Input;
        public MatchState State { get; private set; }
        public float Remaining { get; private set; }
        public event Action Reset;
        public event Action Changed;
        private void Awake()
        {
            if (Player && Core && Motor && Spawner) Initialize();
        }
        private void FixedUpdate()
        {
            if (Input && Motor) Motor.Step(Input.Move, Input.AimWorld, Time.fixedDeltaTime);
            Advance(Time.fixedDeltaTime);
        }
        public void Initialize() { ResetRun(MatchState.Ready); }
        public void StartMatch()
        {
            if (State == MatchState.Ready) ResetRun(MatchState.Playing);
        }
        public void TogglePause()
        {
            if (State == MatchState.Playing) State = MatchState.Paused;
            else if (State == MatchState.Paused) State = MatchState.Playing;
            else return;
            Changed?.Invoke();
        }
        public void Retry()
        {
            if (State == MatchState.Won || State == MatchState.Lost) ResetRun(MatchState.Playing);
        }
        private void ResetRun(MatchState next)
        {
            State = next;
            Player.ResetStats(); Core.ResetHealth(); Motor.ResetPosition();
            Spawner.ResetSpawns(); Remaining = 90;
            Reset?.Invoke(); Changed?.Invoke();
        }
        public void Advance(float delta)
        {
            if (State != MatchState.Playing || delta <= 0) return;
            if (Spawner.enabled)
            {
                Spawner.Advance(delta);
                foreach (var enemy in Spawner.Living)
                    if (enemy && enemy.IsAlive) enemy.Step(delta);
            }
            Remaining = Mathf.Max(0, Remaining - delta);
            // All contact damage in this tick precedes terminal state evaluation.
            if (Player.HP <= 0 || Core.HP <= 0) State = MatchState.Lost;
            else if (Remaining <= 0) State = MatchState.Won;
            Changed?.Invoke();
        }
    }
}
