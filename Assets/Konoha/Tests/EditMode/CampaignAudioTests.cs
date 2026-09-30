using System;
using Konoha.Campaign;
using NUnit.Framework;
using UnityEngine;

namespace Konoha.Tests
{
    // 0.0.9.4: every sound effect is synthesised, short and never clips.
    public sealed class CampaignAudioTests
    {
        [Test]
        public void EverySoundHasAShortCleanClip()
        {
            AudioClip[] clips = CampaignAudio.BuildClips();
            Assert.That(clips, Has.Length.EqualTo(Enum.GetValues(typeof(CampaignSound)).Length));
            try
            {
                foreach (CampaignSound sound in Enum.GetValues(typeof(CampaignSound)))
                {
                    AudioClip clip = clips[(int)sound];
                    Assert.That(clip, Is.Not.Null, sound.ToString());
                    Assert.That(clip.frequency, Is.EqualTo(CampaignAudio.SampleRate));
                    Assert.That(clip.length, Is.InRange(0.03f, 2f), sound.ToString());
                    var data = new float[clip.samples];
                    clip.GetData(data, 0);
                    float peak = 0f;
                    foreach (float value in data)
                    {
                        Assert.That(float.IsNaN(value), Is.False, sound + " has NaN");
                        peak = Mathf.Max(peak, Mathf.Abs(value));
                    }
                    Assert.That(peak, Is.InRange(0.02f, 1f), sound + " is silent or clipping");
                }
            }
            finally
            {
                foreach (AudioClip clip in clips)
                    if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void GamelanLoopIsSeamlessLengthAndNeverClips()
        {
            AudioClip clip = CampaignAudio.BuildGamelanLoop();
            try
            {
                Assert.That(clip.length, Is.EqualTo(CampaignAudio.GamelanBeat * CampaignAudio.GamelanBeats).Within(0.01f));
                var data = new float[clip.samples];
                clip.GetData(data, 0);
                float peak = 0f;
                foreach (float value in data)
                {
                    Assert.That(float.IsNaN(value), Is.False);
                    peak = Mathf.Max(peak, Mathf.Abs(value));
                }
                Assert.That(peak, Is.InRange(0.1f, 0.91f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}
