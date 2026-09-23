using UnityEngine;

namespace Ushimitsu.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class StingPlayer : MonoBehaviour
    {
        [Range(0f, 1f)] public float peakVolume = 0.18f;
        public float duration = 0.9f;

        static readonly float[] Frequencies = { 220f, 233f, 415f };

        AudioSource source;
        AudioClip clip;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            clip = BuildStingClip();
        }

        public void PlaySting()
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip, 1f);
            }
        }

        AudioClip BuildStingClip()
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            var samples = new float[sampleCount];

            double attack = 0.02 * sampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope;
                if (i < attack)
                {
                    envelope = i / (float)attack;
                }
                else
                {
                    envelope = Mathf.Exp(-4.2f * (t - (float)(attack / sampleRate)));
                }

                float sum = 0f;
                foreach (float freq in Frequencies)
                {
                    double phase = (i * freq / sampleRate) % 1.0;
                    float saw = (float)(2.0 * (phase - System.Math.Floor(phase + 0.5)));
                    sum += saw;
                }

                samples[i] = (sum / Frequencies.Length) * envelope * peakVolume;
            }

            var audioClip = AudioClip.Create("GhostSting", sampleCount, 1, sampleRate, false);
            audioClip.SetData(samples, 0);
            return audioClip;
        }
    }
}
