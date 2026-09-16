using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreGuard
{
    public sealed class AudioService : MonoBehaviour
    {
        public GameSession Session;
        public WeaponController Weapon;
        public DefenseController Defense;
        public AudioSource MusicSource;
        public AudioSource SfxSource;
        public AudioSource AlertSource;
        public AudioClip BulletFire;
        public AudioClip RocketLaunch;
        public AudioClip LaserFire;
        public AudioClip MineDrop;
        public AudioClip AlertBeep;
        public AudioClip ShieldActivate;
        public AudioClip EmpActivate;
        public AudioClip InteractionDamage;
        public AudioClip InteractionEmp;
        public AudioClip InteractionBoost;
        public AudioClip MusicLoop;
        public bool SfxEnabled { get; private set; } = true;
        public bool MusicEnabled { get; private set; }
        public int AlertBeepsPlayed { get; private set; }
        public int PendingAlertJobs => alertJobs.Count;
        public event Action Changed;
        public event Action<AudioClip> SfxPlayed;

        private readonly Queue<AlertJob> alertJobs = new Queue<AlertJob>();
        private float timeUntilAlertEvent;
        private bool sessionWasPaused;
        private GameSession boundSession;
        private WeaponController boundWeapon;
        private DefenseController boundDefense;

        private sealed class AlertJob
        {
            public int BeepsPlayed;
        }

        private void Awake()
        {
            EnsureSources();
            EnsureClips();
        }

        private void OnEnable()
        {
            if (Session || Weapon) Bind();
        }

        private void OnDisable() => Unbind();

        public void Bind()
        {
            Unbind();
            EnsureSources();
            EnsureClips();
            boundSession = Session;
            boundWeapon = Weapon ? Weapon : (Session ? Session.Weapon : null);
            boundDefense = Defense ? Defense : (Session ? Session.Defense : null);
            if (boundSession) boundSession.Changed += HandleSessionChanged;
            if (boundWeapon) boundWeapon.AttackAccepted += HandleAttackAccepted;
            if (boundDefense)
            {
                boundDefense.ShieldActivated += HandleShieldActivated;
                boundDefense.EmpActivated += HandleEmpActivated;
            }
        }

        public void EnsureSourcesForScene() => EnsureSources();

        public void PlaySfx(AudioClip clip)
        {
            if (!SfxEnabled || !clip) return;
            EnsureSources();
            SfxSource.PlayOneShot(clip);
            SfxPlayed?.Invoke(clip);
        }

        public AudioClip GetInteractionClip(InteractionKind kind)
        {
            EnsureClips();
            switch (kind)
            {
                case InteractionKind.X: return InteractionDamage;
                case InteractionKind.Y: return InteractionEmp;
                case InteractionKind.Z: return InteractionBoost;
                default: return null;
            }
        }

        public void RequestAlert()
        {
            if (!SfxEnabled) return;
            var wasEmpty = alertJobs.Count == 0;
            alertJobs.Enqueue(new AlertJob());
            if (wasEmpty)
            {
                timeUntilAlertEvent = 0;
                ProcessAlertClock(0);
            }
            Changed?.Invoke();
        }

        public void Advance(float delta)
        {
            if (delta <= 0 || (Session && Session.State != MatchState.Playing)) return;
            ProcessAlertClock(delta);
        }

        public void SetSfxEnabled(bool enabled)
        {
            if (SfxEnabled == enabled) return;
            SfxEnabled = enabled;
            EnsureSources();
            if (!enabled)
            {
                SfxSource.Stop();
                AlertSource.Stop();
                alertJobs.Clear();
                timeUntilAlertEvent = 0;
            }
            Changed?.Invoke();
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (MusicEnabled == enabled) return;
            MusicEnabled = enabled;
            EnsureSources();
            if (MusicEnabled)
            {
                MusicSource.clip = MusicLoop;
                MusicSource.loop = true;
                MusicSource.Play();
            }
            else MusicSource.Stop();
            Changed?.Invoke();
        }

        public void CancelAlerts()
        {
            alertJobs.Clear();
            timeUntilAlertEvent = 0;
            if (AlertSource) AlertSource.Stop();
            AlertBeepsPlayed = 0;
            Changed?.Invoke();
        }

        private void ProcessAlertClock(float delta)
        {
            if (!SfxEnabled) return;
            timeUntilAlertEvent -= delta;
            while (alertJobs.Count > 0 && timeUntilAlertEvent <= 0)
            {
                var job = alertJobs.Peek();
                if (job.BeepsPlayed >= 4)
                {
                    alertJobs.Dequeue();
                    timeUntilAlertEvent = 0;
                    continue;
                }

                EnsureSources();
                AlertSource.PlayOneShot(AlertBeep);
                job.BeepsPlayed++;
                AlertBeepsPlayed++;
                timeUntilAlertEvent = job.BeepsPlayed < 4 ? .45f : Mathf.Max(.15f, AlertBeep.length);
            }
        }

        private void HandleAttackAccepted(WeaponKind kind, Vector2 origin, Vector2 direction)
        {
            switch (kind)
            {
                case WeaponKind.Bullet: PlaySfx(BulletFire); break;
                case WeaponKind.Rocket: PlaySfx(RocketLaunch); break;
                case WeaponKind.Laser: PlaySfx(LaserFire ? LaserFire : RocketLaunch); break;
                case WeaponKind.Mine: PlaySfx(MineDrop); break;
            }
        }

        private void HandleShieldActivated() => PlaySfx(ShieldActivate);
        private void HandleEmpActivated() => PlaySfx(EmpActivate);

        private void HandleSessionChanged()
        {
            if (!Session) return;
            if (Session.State == MatchState.Paused && !sessionWasPaused)
            {
                sessionWasPaused = true;
                if (SfxSource) SfxSource.Pause();
                if (AlertSource) AlertSource.Pause();
                if (MusicSource) MusicSource.Pause();
            }
            else if (Session.State == MatchState.Playing && sessionWasPaused)
            {
                sessionWasPaused = false;
                if (SfxEnabled && SfxSource) SfxSource.UnPause();
                if (SfxEnabled && AlertSource) AlertSource.UnPause();
                if (MusicEnabled && MusicSource) MusicSource.UnPause();
            }
            else if (Session.State == MatchState.Won || Session.State == MatchState.Lost)
            {
                CancelAlerts();
                if (SfxSource) SfxSource.Stop();
            }
            Changed?.Invoke();
        }

        private void EnsureSources()
        {
            MusicSource = MusicSource ? MusicSource : gameObject.AddComponent<AudioSource>();
            SfxSource = SfxSource ? SfxSource : gameObject.AddComponent<AudioSource>();
            AlertSource = AlertSource ? AlertSource : gameObject.AddComponent<AudioSource>();
            MusicSource.playOnAwake = false; MusicSource.spatialBlend = 0; MusicSource.loop = true; MusicSource.volume = .25f;
            SfxSource.playOnAwake = false; SfxSource.spatialBlend = 0; SfxSource.volume = 1f;
            AlertSource.playOnAwake = false; AlertSource.spatialBlend = 0; AlertSource.volume = 1f;
        }

        private void EnsureClips()
        {
            BulletFire = BulletFire ? BulletFire : Tone("Bullet fire", 720, .08f);
            RocketLaunch = RocketLaunch ? RocketLaunch : Tone("Rocket launch", 260, .16f);
            MineDrop = MineDrop ? MineDrop : Tone("Mine drop", 420, .12f);
            AlertBeep = AlertBeep ? AlertBeep : Tone("Alert beep", 900, .16f);
            ShieldActivate = ShieldActivate ? ShieldActivate : Tone("Shield activate", 520, .18f);
            EmpActivate = EmpActivate ? EmpActivate : Tone("EMP activate", 180, .2f);
            InteractionDamage = InteractionDamage ? InteractionDamage : Tone("Interaction damage", 120, .24f);
            InteractionEmp = InteractionEmp ? InteractionEmp : Tone("Interaction EMP", 190, .28f);
            InteractionBoost = InteractionBoost ? InteractionBoost : Tone("Interaction boost", 760, .24f);
            MusicLoop = MusicLoop ? MusicLoop : Tone("Arena loop", 110, 2f);
        }

        private static AudioClip Tone(string name, float frequency, float seconds)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            var samples = new float[sampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                var envelope = 1f - (float)i / samples.Length;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / sampleRate) * envelope * .2f;
            }
            clip.SetData(samples, 0);
            return clip;
        }

        private void Unbind()
        {
            if (boundSession) boundSession.Changed -= HandleSessionChanged;
            if (boundWeapon) boundWeapon.AttackAccepted -= HandleAttackAccepted;
            if (boundDefense)
            {
                boundDefense.ShieldActivated -= HandleShieldActivated;
                boundDefense.EmpActivated -= HandleEmpActivated;
            }
            boundSession = null;
            boundWeapon = null;
            boundDefense = null;
        }
    }
}
