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
        public WeaponController Weapon;
        public DefenseController Defense;
        public StatusEffects Effects;
        public AudioService Audio;
        public DemoDirector Demo;
        public Projectile EnemyProjectilePrefab;
        public EnemySpawner Spawner;
        public InputReader Input;
        public InteractionCycleController InteractionCycle;
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
        public void ResetForDemo() => ResetRun(MatchState.Playing);
        public void RestoreInteractions()
        {
            if (InteractionCycle && InteractionCycle.Session == this) InteractionCycle.ResetCycle();
            Changed?.Invoke();
        }
        private void ResetRun(MatchState next)
        {
            State = next;
            ClearTransientObjects();
            if (Audio) Audio.CancelAlerts();
            Player.ResetStats(); Core.ResetHealth(); Motor.ResetPosition();
            if (Weapon) Weapon.ResetRun();
            if (Defense) Defense.ResetRun();
            if (Effects) Effects.ResetRun();
            if (InteractionCycle && InteractionCycle.Session == this) InteractionCycle.ResetCycle();
            foreach (var zone in FindObjectsByType<ForbiddenZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (zone && zone.Session == this) zone.ResetOccupancy();
            Spawner.ResetSpawns(); Remaining = 90;
            if (Demo && Demo.IsDemoMode) Spawner.AutoSpawn = false;
            Reset?.Invoke(); Changed?.Invoke();
        }
        public void Advance(float delta)
        {
            if (State != MatchState.Playing || delta <= 0) return;
            if (Player) Player.Advance(delta);
            if (Weapon) Weapon.Advance(delta);
            if (Defense) Defense.Advance(delta);
            if (Effects) Effects.Advance(delta);
            if (InteractionCycle && InteractionCycle.Session == this) InteractionCycle.Advance(delta);
            if (Audio) Audio.Advance(delta);
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

        private void ClearTransientObjects()
        {
            foreach (var projectile in GetComponentsInChildren<Projectile>(true))
                DestroyTransient(projectile.gameObject);
            foreach (var mine in GetComponentsInChildren<Mine>(true))
                DestroyTransient(mine.gameObject);
        }

        private static void DestroyTransient(GameObject target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

    }
}
