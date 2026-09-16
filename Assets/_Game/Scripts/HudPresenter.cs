using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard
{
    public sealed class HudPresenter : MonoBehaviour
    {
        public GameSession Session;
        public Text StatsText, CoreText, TimerText, StateText, ResultText;
        public Text WeaponText, CooldownsText, FeedbackText;
        public Image PlayerHpFill, PlayerArmorFill, CoreHpFill;
        public Text PlayerHpLabel, PlayerArmorLabel;
        public bool CompactHud = true;
        public Image ResultImage, CountdownImage;
        public Image LoadingProgress;
        public Sprite WinSprite, LoseSprite, CountdownZero, CountdownOne, CountdownTwo, CountdownThree;
        public float CountdownDuration = 3f;
        public float LoadingDuration = .75f;
        public GameObject StartPanel, PausePanel, ResultPanel;
        public GameObject SettingsPanel, LoadingPanel;
        public Button StartButton, ResumeButton, RetryButton;
        public Button WeaponCycleButton;
        public Button SettingsButton, CloseSettingsButton;
        public Button BulletButton, RocketButton, MineButton, ShieldButton, EmpButton;
        private GameSession boundSession;
        private float feedbackRemaining;
        private float countdownRemaining;
        private float loadingRemaining;
        private bool countdownActive;
        private bool loadingActive;
        private bool settingsOpen;

        private void OnEnable() { if (Session) Bind(); }
        private void OnDisable() => Unbind();

        public void Bind()
        {
            Unbind();
            boundSession = Session;
            Session.Changed += Refresh;
            Session.Player.Changed += Refresh;
            Session.Core.Changed += Refresh;
            if (Session.Defense) Session.Defense.Changed += Refresh;
            if (Session.Effects) Session.Effects.Changed += Refresh;
            if (Session.Weapon)
            {
                Session.Weapon.WeaponChanged += OnWeaponChanged;
                Session.Weapon.WeaponSelected += HandleWeaponSelected;
                Session.Weapon.ActionRejected += ShowActionRejected;
            }
            Session.Reset += HandleSessionReset;
            if (StartButton) StartButton.onClick.AddListener(BeginStartCountdown);
            if (StartButton) StartButton.onClick.AddListener(PlayUiClick);
            if (SettingsButton) SettingsButton.onClick.AddListener(OpenSettings);
            if (SettingsButton) SettingsButton.onClick.AddListener(PlayUiClick);
            if (CloseSettingsButton) CloseSettingsButton.onClick.AddListener(CloseSettings);
            if (CloseSettingsButton) CloseSettingsButton.onClick.AddListener(PlayUiClick);
            if (ResumeButton) ResumeButton.onClick.AddListener(Session.TogglePause);
            if (ResumeButton) ResumeButton.onClick.AddListener(PlayUiClick);
            if (RetryButton) RetryButton.onClick.AddListener(Session.Retry);
            if (RetryButton) RetryButton.onClick.AddListener(PlayUiClick);
            if (WeaponCycleButton) WeaponCycleButton.onClick.AddListener(CycleWeapon);
            if (WeaponCycleButton) WeaponCycleButton.onClick.AddListener(PlayUiClick);
            if (BulletButton) BulletButton.onClick.AddListener(SelectBullet);
            if (BulletButton) BulletButton.onClick.AddListener(PlayUiClick);
            if (RocketButton) RocketButton.onClick.AddListener(SelectRocket);
            if (RocketButton) RocketButton.onClick.AddListener(PlayUiClick);
            if (MineButton) MineButton.onClick.AddListener(SelectMine);
            if (MineButton) MineButton.onClick.AddListener(PlayUiClick);
            if (ShieldButton) ShieldButton.onClick.AddListener(ActivateShield);
            if (ShieldButton) ShieldButton.onClick.AddListener(PlayUiClick);
            if (EmpButton) EmpButton.onClick.AddListener(ActivateEmp);
            if (EmpButton) EmpButton.onClick.AddListener(PlayUiClick);
            ApplyCompactPresentation();
            EnsureMobileControls();
            Refresh();
        }

        private void OnWeaponChanged(WeaponKind _) => Refresh();

        private void Unbind()
        {
            if (!boundSession) return;
            boundSession.Changed -= Refresh;
            if (boundSession.Player) boundSession.Player.Changed -= Refresh;
            if (boundSession.Core) boundSession.Core.Changed -= Refresh;
            if (boundSession.Defense) boundSession.Defense.Changed -= Refresh;
            if (boundSession.Effects) boundSession.Effects.Changed -= Refresh;
            if (boundSession.Weapon)
            {
                boundSession.Weapon.WeaponChanged -= OnWeaponChanged;
                boundSession.Weapon.WeaponSelected -= HandleWeaponSelected;
                boundSession.Weapon.ActionRejected -= ShowActionRejected;
            }
            boundSession.Reset -= HandleSessionReset;
            if (StartButton) StartButton.onClick.RemoveListener(BeginStartCountdown);
            if (StartButton) StartButton.onClick.RemoveListener(PlayUiClick);
            if (SettingsButton) SettingsButton.onClick.RemoveListener(OpenSettings);
            if (SettingsButton) SettingsButton.onClick.RemoveListener(PlayUiClick);
            if (CloseSettingsButton) CloseSettingsButton.onClick.RemoveListener(CloseSettings);
            if (CloseSettingsButton) CloseSettingsButton.onClick.RemoveListener(PlayUiClick);
            if (ResumeButton) ResumeButton.onClick.RemoveListener(boundSession.TogglePause);
            if (ResumeButton) ResumeButton.onClick.RemoveListener(PlayUiClick);
            if (RetryButton) RetryButton.onClick.RemoveListener(boundSession.Retry);
            if (RetryButton) RetryButton.onClick.RemoveListener(PlayUiClick);
            if (WeaponCycleButton) WeaponCycleButton.onClick.RemoveListener(CycleWeapon);
            if (WeaponCycleButton) WeaponCycleButton.onClick.RemoveListener(PlayUiClick);
            if (BulletButton) BulletButton.onClick.RemoveListener(SelectBullet);
            if (BulletButton) BulletButton.onClick.RemoveListener(PlayUiClick);
            if (RocketButton) RocketButton.onClick.RemoveListener(SelectRocket);
            if (RocketButton) RocketButton.onClick.RemoveListener(PlayUiClick);
            if (MineButton) MineButton.onClick.RemoveListener(SelectMine);
            if (MineButton) MineButton.onClick.RemoveListener(PlayUiClick);
            if (ShieldButton) ShieldButton.onClick.RemoveListener(ActivateShield);
            if (ShieldButton) ShieldButton.onClick.RemoveListener(PlayUiClick);
            if (EmpButton) EmpButton.onClick.RemoveListener(ActivateEmp);
            if (EmpButton) EmpButton.onClick.RemoveListener(PlayUiClick);
            boundSession = null;
        }

        private void CycleWeapon() { Session.Weapon?.CycleNextWeapon(); Refresh(); }
        private void SelectBullet() { Session.Weapon?.Select(WeaponKind.Bullet); Refresh(); }
        private void SelectRocket() { Session.Weapon?.Select(WeaponKind.Rocket); Refresh(); }
        private void SelectMine() { Session.Weapon?.Select(WeaponKind.Laser); Refresh(); }
        private void ActivateShield() => Session.Defense?.TryActivateShield();
        private void ActivateEmp() => Session.Defense?.TryActivateEmp();
        private void HandleWeaponSelected(WeaponKind _) => Refresh();
        private void PlayUiClick() => Session?.Audio?.PlayUiClick();

        private void BeginStartCountdown()
        {
            if (!Session || Session.State != MatchState.Ready || countdownActive || loadingActive || settingsOpen) return;
            if (!LoadingPanel)
            {
                BeginCountdown();
                return;
            }
            loadingActive = true;
            loadingRemaining = Mathf.Max(.1f, LoadingDuration);
            RefreshLoading();
            Refresh();
        }

        private void BeginCountdown()
        {
            countdownActive = true;
            countdownRemaining = Mathf.Max(.1f, CountdownDuration);
            RefreshCountdown();
            Refresh();
        }

        private void OpenSettings()
        {
            if (!Session || Session.State != MatchState.Ready || loadingActive || countdownActive || !SettingsPanel) return;
            settingsOpen = true;
            SettingsPanel.transform.SetAsLastSibling();
            Refresh();
        }

        private void CloseSettings()
        {
            settingsOpen = false;
            Refresh();
        }

        private void ShowActionRejected(string message)
        {
            if (!FeedbackText) return;
            FeedbackText.text = message;
            FeedbackText.gameObject.SetActive(true);
            feedbackRemaining = 1.25f;
        }

        private void ClearFeedback()
        {
            feedbackRemaining = 0f;
            if (!FeedbackText) return;
            FeedbackText.text = string.Empty;
            FeedbackText.gameObject.SetActive(false);
        }

        private void ApplyCompactPresentation()
        {
            if (!CompactHud) return;
            HideVerboseLabel("Objective");
            HideVerboseLabel("Controls");
            HideVerboseLabel("Actions");
            SetTextRect(StatsText, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22), new Vector2(390, 30), TextAnchor.UpperLeft);
            SetTextRect(CoreText, Vector2.one, Vector2.one, new Vector2(-24, -22), new Vector2(190, 28), TextAnchor.UpperRight);
            SetTextRect(TimerText, Vector2.one, Vector2.one, new Vector2(-24, -52), new Vector2(120, 26), TextAnchor.UpperRight);
            SetTextRect(WeaponText, Vector2.zero, Vector2.zero, new Vector2(24, 72), new Vector2(160, 26), TextAnchor.LowerLeft);
            SetTextRect(CooldownsText, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 180), new Vector2(300, 26), TextAnchor.LowerRight);
        }

        private void HideVerboseLabel(string childName)
        {
            var child = transform.Find(childName);
            if (child) child.gameObject.SetActive(false);
        }

        private static void SetTextRect(Text text, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, TextAnchor alignment)
        {
            if (!text) return;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.alignment = alignment;
        }

        private void EnsureMobileControls()
        {
            var mobileRoot = transform.Find("Mobile controls");
            if (!mobileRoot)
            {
                var go = new GameObject("Mobile controls", typeof(RectTransform), typeof(MobileControlsPresenter));
                go.transform.SetParent(transform, false);
                mobileRoot = go.transform;
            }
            var presenter = mobileRoot.GetComponent<MobileControlsPresenter>();
            if (presenter)
            {
                presenter.Session = Session;
                presenter.Bind();
            }
        }

        private void HandleSessionReset()
        {
            ClearFeedback();
            settingsOpen = false;
            loadingActive = false;
            loadingRemaining = 0f;
            countdownActive = false;
            countdownRemaining = 0f;
            if (CountdownImage) CountdownImage.gameObject.SetActive(false);
            if (LoadingPanel) LoadingPanel.SetActive(false);
        }

        private void Update()
        {
            if (loadingActive)
            {
                loadingRemaining = Mathf.Max(0f, loadingRemaining - Time.unscaledDeltaTime);
                RefreshLoading();
                if (loadingRemaining <= 0f)
                {
                    loadingActive = false;
                    if (LoadingPanel) LoadingPanel.SetActive(false);
                    BeginCountdown();
                }
            }
            else if (countdownActive)
            {
                countdownRemaining = Mathf.Max(0f, countdownRemaining - Time.unscaledDeltaTime);
                if (countdownRemaining <= 0f)
                {
                    countdownActive = false;
                    if (CountdownImage) CountdownImage.gameObject.SetActive(false);
                    Session.StartMatch();
                }
                else RefreshCountdown();
            }
            if (!FeedbackText || feedbackRemaining <= 0f) return;
            feedbackRemaining = Mathf.Max(0f, feedbackRemaining - Time.unscaledDeltaTime);
            if (feedbackRemaining <= 0f) ClearFeedback();
        }

        private void RefreshCountdown()
        {
            if (!CountdownImage) return;
            var sprite = countdownRemaining <= .18f ? CountdownZero :
                countdownRemaining > 2f ? CountdownThree :
                countdownRemaining > 1f ? CountdownTwo : CountdownOne;
            CountdownImage.sprite = sprite;
            CountdownImage.enabled = sprite != null;
            CountdownImage.gameObject.SetActive(sprite != null);
        }

        private void RefreshLoading()
        {
            var duration = Mathf.Max(.1f, LoadingDuration);
            var progress = Mathf.Clamp01(1f - loadingRemaining / duration);
            if (LoadingProgress)
            {
                LoadingProgress.type = Image.Type.Filled;
                LoadingProgress.fillMethod = Image.FillMethod.Horizontal;
                LoadingProgress.fillOrigin = 0;
                LoadingProgress.fillAmount = progress;
            }
            var loadingText = LoadingPanel ? LoadingPanel.transform.Find("Loading label")?.GetComponent<Text>() : null;
            if (loadingText) loadingText.text = $"LOADING {Mathf.RoundToInt(progress * 100f):0}%";
            if (LoadingPanel) LoadingPanel.SetActive(loadingActive);
        }

        private void Refresh()
        {
            if (Session && Session.Player)
            {
                var hpRatio = Mathf.Clamp01(Session.Player.HP / Mathf.Max(1f, PlayerStats.MaxHP));
                var armorRatio = Mathf.Clamp01(Session.Player.Armor / Mathf.Max(1f, PlayerStats.MaxArmor));
                if (PlayerHpFill) PlayerHpFill.fillAmount = hpRatio;
                if (PlayerArmorFill) PlayerArmorFill.fillAmount = armorRatio;
                if (PlayerHpLabel) PlayerHpLabel.text = $"{Session.Player.HP:0}/{PlayerStats.MaxHP:0}";
                if (PlayerArmorLabel) PlayerArmorLabel.text = $"{Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}";
            }
            if (Session && Session.Core)
            {
                var coreRatio = Mathf.Clamp01(Session.Core.HP / Mathf.Max(1f, CoreHealth.MaxHP));
                if (CoreHpFill) CoreHpFill.fillAmount = coreRatio;
            }

            if (StatsText && Session && Session.Player)
                StatsText.text = CompactHud
                    ? $"HP {Session.Player.HP:0}/{PlayerStats.MaxHP:0}  •  ARM {Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}  •  C {Session.Player.Coins}"
                    : $"PLAYER HP {Session.Player.HP:0}/{PlayerStats.MaxHP:0}\nARMOR {Session.Player.Armor:0}/{PlayerStats.MaxArmor:0}\nCOINS {Session.Player.Coins}";
            if (CoreText && Session && Session.Core)
                CoreText.text = $"CORE {Session.Core.HP:0}/{CoreHealth.MaxHP:0}";
            if (TimerText && Session)
                TimerText.text = CompactHud ? $"{Session.Remaining:00.0}s" : $"TIME {Session.Remaining:00.0}";
            if (StateText && Session)
                StateText.text = CompactHud ? string.Empty : Session.State.ToString().ToUpperInvariant();
            if (StateText) StateText.gameObject.SetActive(!CompactHud);
            if (ResultText && Session)
                ResultText.text = Session.State == MatchState.Won ? "CORE SECURED — YOU WIN" : "CORE OFFLINE — LOST — TRY AGAIN";
            if (WeaponText)
                WeaponText.text = CompactHud
                    ? (Session.Weapon ? Session.Weapon.SelectedWeapon.ToString().ToUpperInvariant() : "—")
                    : (Session.Weapon ? $"WEAPON: {Session.Weapon.SelectedWeapon.ToString().ToUpperInvariant()}" : "WEAPON: NONE");
            if (ResultImage)
            {
                var hasResult = Session.State == MatchState.Won || Session.State == MatchState.Lost;
                ResultImage.sprite = Session.State == MatchState.Won ? WinSprite : LoseSprite;
                ResultImage.enabled = hasResult && ResultImage.sprite != null;
            }
            if (CooldownsText)
            {
                var shield = !Session.Defense || Session.Defense.ShieldCooldownRemaining <= 0f
                    ? "READY"
                    : Session.Defense.ShieldActive
                        ? $"{Session.Defense.ShieldHitsRemaining}/{Session.Defense.ShieldMaxHits}"
                        : $"{Session.Defense.ShieldCooldownRemaining:0.0}s";
                var emp = !Session.Defense || Session.Defense.EmpCooldownRemaining <= 0f
                    ? "READY"
                    : $"{Session.Defense.EmpCooldownRemaining:0.0}s  AFFECTED: {Session.Defense.LastEmpAffectedCount}";
                var speed = Session.Effects ? Session.Effects.CurrentSpeed :
                    Session.Motor ? Session.Motor.CurrentSpeed : 0f;
                CooldownsText.text = CompactHud
                    ? $"SH {shield}  •  EMP {emp}"
                    : $"SPEED: {speed:0.0}\nSHIELD: {shield}\nEMP: {emp}";
            }
            if (StartPanel) StartPanel.SetActive(Session.State == MatchState.Ready && !countdownActive && !loadingActive && !settingsOpen);
            if (PausePanel) PausePanel.SetActive(Session.State == MatchState.Paused);
            if (ResultPanel) ResultPanel.SetActive(Session.State == MatchState.Won || Session.State == MatchState.Lost);
            if (SettingsPanel) SettingsPanel.SetActive(settingsOpen && Session.State == MatchState.Ready && !loadingActive && !countdownActive);
            if (LoadingPanel) LoadingPanel.SetActive(loadingActive);
            if (SettingsButton) SettingsButton.interactable = Session.State == MatchState.Ready && !loadingActive && !countdownActive;
            if (CloseSettingsButton) CloseSettingsButton.interactable = settingsOpen;
            if (StartButton) StartButton.interactable = Session.State == MatchState.Ready && !loadingActive && !countdownActive && !settingsOpen;
            var gameplayActionsEnabled = Session.State == MatchState.Playing;
            if (WeaponCycleButton)
            {
                WeaponCycleButton.interactable = gameplayActionsEnabled;
                var weaponLabel = WeaponCycleButton.GetComponentInChildren<Text>();
                if (weaponLabel && Session.Weapon)
                    weaponLabel.text = $"R  {Session.Weapon.SelectedWeapon.ToString().ToUpperInvariant()}";
            }
            if (BulletButton) BulletButton.interactable = gameplayActionsEnabled;
            if (RocketButton) RocketButton.interactable = gameplayActionsEnabled;
            if (MineButton) MineButton.interactable = gameplayActionsEnabled;
            if (ShieldButton) ShieldButton.interactable = gameplayActionsEnabled && Session.Defense
                && !Session.Defense.ShieldActive && Session.Defense.ShieldCooldownRemaining <= 0f;
            if (EmpButton) EmpButton.interactable = gameplayActionsEnabled && Session.Defense
                && Session.Defense.EmpCooldownRemaining <= 0f;
        }
    }
}
