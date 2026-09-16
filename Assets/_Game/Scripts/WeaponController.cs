using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuard
{
    public enum WeaponKind
    {
        Bullet,
        Rocket,
        Laser,
        Mine,
    }

    public sealed class WeaponController : MonoBehaviour
    {
        public GameSession Session;
        public Transform Muzzle;
        public Projectile ProjectilePrefab;
        public Mine MinePrefab;
        public WeaponKind SelectedWeapon { get; private set; } = WeaponKind.Bullet;
        public float BulletDamage = 20;
        public float BulletSpeed = 14;
        public float BulletCooldown = .2f;
        public float RocketDamage = 50;
        public float RocketSpeed = 11;
        public float RocketCooldown = 1.2f;
        public float RocketRadius = 1.5f;
        public float LaserDamage = 60;
        public float LaserSpeed = 22;
        public float LaserCooldown = 2f;
        public float MineDamage = 50;
        public float MineCooldown = 2;
        public int MaxMines = 3;
        public int LivingMineCount
        {
            get
            {
                RemoveDeadMines();
                return mines.Count;
            }
        }
        public float CooldownRemaining => (int)SelectedWeapon < cooldowns.Length ? cooldowns[(int)SelectedWeapon] : 0f;

        public event Action<WeaponKind, Vector2, Vector2> AttackAccepted;
        public event Action<WeaponKind> WeaponChanged;

        private readonly List<Mine> mines = new List<Mine>();
        private readonly float[] cooldowns = new float[4];

        public bool Select(WeaponKind weapon)
        {
            if (Session && Session.State != MatchState.Playing) return false;
            SelectedWeapon = weapon;
            WeaponChanged?.Invoke(weapon);
            return true;
        }

        public WeaponKind CycleNextWeapon()
        {
            var next = SelectedWeapon switch
            {
                WeaponKind.Bullet => WeaponKind.Rocket,
                WeaponKind.Rocket => WeaponKind.Laser,
                _ => WeaponKind.Bullet
            };
            Select(next);
            return SelectedWeapon;
        }

        public void ProcessInput(bool fireHeld, bool firePressed, Vector2 aimWorld)
        {
            if (!Session || Session.State != MatchState.Playing) return;
            var wantsToFire = SelectedWeapon == WeaponKind.Bullet ? fireHeld : firePressed;
            if (wantsToFire) TryFire(aimWorld);
        }

        public bool TryFire(Vector2 aimWorld)
        {
            if (!Session || Session.State != MatchState.Playing || CooldownRemaining > 0) return false;
            RemoveDeadMines();
            if (SelectedWeapon == WeaponKind.Mine && mines.Count >= MaxMines) return false;

            var origin = Muzzle ? (Vector2)Muzzle.position : (Vector2)transform.position;
            var direction = aimWorld - origin;
            if (direction.sqrMagnitude <= .000001f)
                direction = Muzzle ? (Vector2)Muzzle.right : Vector2.right;

            switch (SelectedWeapon)
            {
                case WeaponKind.Bullet:
                    if (!ProjectilePrefab) return false;
                    var bullet = Instantiate(ProjectilePrefab, origin, Quaternion.identity, Session.transform);
                    bullet.InitializePlayerShot(Session, direction, ProjectileKind.Bullet, BulletDamage, BulletSpeed, 2, 0);
                    cooldowns[(int)WeaponKind.Bullet] = BulletCooldown;
                    AttackAccepted?.Invoke(SelectedWeapon, origin, direction);
                    return true;

                case WeaponKind.Rocket:
                    if (!ProjectilePrefab) return false;
                    var rocket = Instantiate(ProjectilePrefab, origin, Quaternion.identity, Session.transform);
                    rocket.InitializePlayerShot(Session, direction, ProjectileKind.Rocket, RocketDamage, RocketSpeed, 3, RocketRadius);
                    cooldowns[(int)WeaponKind.Rocket] = RocketCooldown;
                    AttackAccepted?.Invoke(SelectedWeapon, origin, direction);
                    return true;

                case WeaponKind.Laser:
                    if (!ProjectilePrefab) return false;
                    var laser = Instantiate(ProjectilePrefab, origin, Quaternion.identity, Session.transform);
                    laser.InitializePlayerShot(Session, direction, ProjectileKind.Laser, LaserDamage, LaserSpeed, 2.5f, 0);
                    cooldowns[(int)WeaponKind.Laser] = LaserCooldown;
                    AttackAccepted?.Invoke(SelectedWeapon, origin, direction);
                    return true;

                case WeaponKind.Mine:
                    if (!MinePrefab) return false;
                    var mine = Instantiate(MinePrefab, origin, Quaternion.identity, Session.transform);
                    mine.Initialize(Session, MineDamage, 1.8f);
                    mines.Add(mine);
                    cooldowns[(int)WeaponKind.Mine] = MineCooldown;
                    AttackAccepted?.Invoke(SelectedWeapon, origin, direction);
                    return true;

                default:
                    return false;
            }
        }

        public void Advance(float delta)
        {
            if (delta <= 0 || (Session && Session.State != MatchState.Playing)) return;
            for (var i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0, cooldowns[i] - delta);
            RemoveDeadMines();
        }

        public void ResetRun()
        {
            for (var i = 0; i < cooldowns.Length; i++) cooldowns[i] = 0;
            SelectedWeapon = WeaponKind.Bullet;
            mines.Clear();
            WeaponChanged?.Invoke(SelectedWeapon);
        }

        private void RemoveDeadMines()
        {
            mines.RemoveAll(mine => !mine || !mine.IsLive);
        }
    }
}
