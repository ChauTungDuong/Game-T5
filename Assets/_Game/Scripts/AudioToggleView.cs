using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard
{
    public sealed class AudioToggleView : MonoBehaviour
    {
        public AudioService Audio;
        public Button SoundOff;
        public Button SoundOn;
        public Button MusicOn;
        public Button MusicOff;

        private AudioService boundAudio;

        private void OnEnable() { if (Audio) Bind(); }
        private void OnDisable() { Unbind(); }

        public void Bind()
        {
            Unbind();
            boundAudio = Audio;
            boundAudio.Changed += Refresh;
            if (SoundOff) SoundOff.onClick.AddListener(boundAudio.PlayUiClick);
            if (SoundOn) SoundOn.onClick.AddListener(boundAudio.PlayUiClick);
            if (MusicOn) MusicOn.onClick.AddListener(boundAudio.PlayUiClick);
            if (MusicOff) MusicOff.onClick.AddListener(boundAudio.PlayUiClick);
            if (SoundOff) SoundOff.onClick.AddListener(DisableSfx);
            if (SoundOn) SoundOn.onClick.AddListener(EnableSfx);
            if (MusicOn) MusicOn.onClick.AddListener(EnableMusic);
            if (MusicOff) MusicOff.onClick.AddListener(DisableMusic);
            Refresh();
        }

        private void DisableSfx() => boundAudio.SetSfxEnabled(false);
        private void EnableSfx() => boundAudio.SetSfxEnabled(true);
        private void EnableMusic() => boundAudio.SetMusicEnabled(true);
        private void DisableMusic() => boundAudio.SetMusicEnabled(false);

        public void Refresh()
        {
            if (!boundAudio)
            {
                if (Audio) boundAudio = Audio;
                else return;
            }
            var hud = GetComponentInParent<HudPresenter>();
            if (hud)
            {
                var isPlaying = boundAudio.Session && boundAudio.Session.State == MatchState.Playing;
                var isPaused = boundAudio.Session && boundAudio.Session.State == MatchState.Paused;
                var showSettings = hud.SettingsPanel && hud.SettingsPanel.activeSelf;
                if (!isPlaying && !isPaused && !showSettings)
                {
                    if (SoundOff) SoundOff.gameObject.SetActive(false);
                    if (SoundOn) SoundOn.gameObject.SetActive(false);
                    if (MusicOn) MusicOn.gameObject.SetActive(false);
                    if (MusicOff) MusicOff.gameObject.SetActive(false);
                    return;
                }
            }
            var paused = boundAudio.Session && boundAudio.Session.State == MatchState.Paused;
            if (SoundOff) SoundOff.gameObject.SetActive(boundAudio.SfxEnabled);
            if (SoundOn) SoundOn.gameObject.SetActive(!boundAudio.SfxEnabled);
            if (MusicOn) MusicOn.gameObject.SetActive(!boundAudio.MusicEnabled);
            if (MusicOff) MusicOff.gameObject.SetActive(boundAudio.MusicEnabled);
            if (SoundOff) SoundOff.interactable = !paused;
            if (SoundOn) SoundOn.interactable = !paused;
            if (MusicOn) MusicOn.interactable = !paused;
            if (MusicOff) MusicOff.interactable = !paused;
        }

        private void Unbind()
        {
            if (!boundAudio) return;
            boundAudio.Changed -= Refresh;
            if (SoundOff) SoundOff.onClick.RemoveListener(boundAudio.PlayUiClick);
            if (SoundOn) SoundOn.onClick.RemoveListener(boundAudio.PlayUiClick);
            if (MusicOn) MusicOn.onClick.RemoveListener(boundAudio.PlayUiClick);
            if (MusicOff) MusicOff.onClick.RemoveListener(boundAudio.PlayUiClick);
            if (SoundOff) SoundOff.onClick.RemoveListener(DisableSfx);
            if (SoundOn) SoundOn.onClick.RemoveListener(EnableSfx);
            if (MusicOn) MusicOn.onClick.RemoveListener(EnableMusic);
            if (MusicOff) MusicOff.onClick.RemoveListener(DisableMusic);
            boundAudio = null;
        }
    }
}
