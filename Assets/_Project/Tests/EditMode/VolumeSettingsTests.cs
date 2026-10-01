using ArenaSurvival.Audio;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class VolumeSettingsTests
    {
        [Test]
        public void ToDecibels_FullVolume_IsZeroDecibels()
        {
            Assert.AreEqual(0f, VolumeSettings.ToDecibels(1f), 0.0001f);
        }

        [Test]
        public void ToDecibels_Half_IsAboutMinusSixDecibels()
        {
            Assert.AreEqual(-6.0206f, VolumeSettings.ToDecibels(0.5f), 0.001f);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void ToDecibels_ZeroOrNegative_IsSilent(float linear)
        {
            // Log10(0) là âm vô cực, mixer không nhận được: phải kẹp về mức thấp nhất.
            Assert.AreEqual(VolumeSettings.MinDecibels, VolumeSettings.ToDecibels(linear));
        }

        [Test]
        public void ToDecibels_AboveOne_DoesNotAmplify()
        {
            Assert.AreEqual(0f, VolumeSettings.ToDecibels(1.5f), 0.0001f);
        }

        [Test]
        public void ToDecibels_IsIncreasing()
        {
            float previous = VolumeSettings.ToDecibels(0f);
            for (int i = 1; i <= 100; i++)
            {
                float current = VolumeSettings.ToDecibels(i / 100f);
                Assert.Greater(current, previous, $"tại {i}%");
                previous = current;
            }
        }

        [Test]
        public void ParameterName_MatchesMixerExposedNames()
        {
            Assert.AreEqual("MasterVolume", VolumeSettings.ParameterName(VolumeChannel.Master));
            Assert.AreEqual("MusicVolume", VolumeSettings.ParameterName(VolumeChannel.Music));
            Assert.AreEqual("SFXVolume", VolumeSettings.ParameterName(VolumeChannel.Sfx));
        }
    }
}
