using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CoreGuard.Tests.PlayMode
{
    public sealed class AudioContractTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            objects.Add(go);
            return go.AddComponent<T>();
        }

        private Button MakeButton(string name)
        {
            return Make<Button>(name);
        }

        [TearDown]
        public void Cleanup()
        {
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void Alert_EmitsExactlyFourBeepsAtTheDefinedCadence()
        {
            var audio = Make<AudioService>("Audio");
            audio.RequestAlert();
            Assert.That(audio.AlertBeepsPlayed, Is.EqualTo(1));
            Assert.That(audio.PendingAlertJobs, Is.EqualTo(1));

            audio.Advance(.45f);
            audio.Advance(.45f);
            audio.Advance(.45f);
            Assert.That(audio.AlertBeepsPlayed, Is.EqualTo(4));
            audio.Advance(.2f);
            Assert.That(audio.PendingAlertJobs, Is.Zero);
        }

        [Test]
        public void SfxMute_CancelsQueuedAlertsAndDoesNotReplayWhenEnabled()
        {
            var audio = Make<AudioService>("Audio");
            audio.RequestAlert();
            audio.SetSfxEnabled(false);
            Assert.That(audio.PendingAlertJobs, Is.Zero);
            var playedBeforeReenable = audio.AlertBeepsPlayed;
            audio.SetSfxEnabled(true);
            audio.Advance(2);
            Assert.That(audio.AlertBeepsPlayed, Is.EqualTo(playedBeforeReenable));
        }

        [Test]
        public void ToggleView_AlternatesTheRequiredButtonPairsIndependently()
        {
            var audio = Make<AudioService>("Audio");
            var view = Make<AudioToggleView>("Audio toggles");
            view.Audio = audio;
            view.SoundOff = MakeButton("SoundOff");
            view.SoundOn = MakeButton("SoundOn");
            view.MusicOn = MakeButton("MusicOn");
            view.MusicOff = MakeButton("MusicOff");
            view.Bind();

            Assert.That(view.SoundOff.gameObject.activeSelf, Is.True);
            Assert.That(view.SoundOn.gameObject.activeSelf, Is.False);
            Assert.That(view.MusicOn.gameObject.activeSelf, Is.True);
            Assert.That(view.MusicOff.gameObject.activeSelf, Is.False);

            view.SoundOff.onClick.Invoke();
            Assert.That(audio.SfxEnabled, Is.False);
            Assert.That(view.SoundOff.gameObject.activeSelf, Is.False);
            Assert.That(view.SoundOn.gameObject.activeSelf, Is.True);
            Assert.That(view.MusicOn.gameObject.activeSelf, Is.True);

            view.MusicOn.onClick.Invoke();
            Assert.That(audio.MusicEnabled, Is.True);
            Assert.That(view.MusicOn.gameObject.activeSelf, Is.False);
            Assert.That(view.MusicOff.gameObject.activeSelf, Is.True);
        }
    }
}
