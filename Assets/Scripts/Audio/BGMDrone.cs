using UnityEngine;

namespace Ushimitsu.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class BGMDrone : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.09f;

        static readonly float[] Frequencies = { 55f, 82.5f, 110f };
        static readonly float[] OscGain = { 0.5f, 0.22f, 0.22f };

        double[] phase = new double[3];
        double lfoPhase;
        double sampleRate;
        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            sampleRate = AudioSettings.outputSampleRate;

            var silentClip = AudioClip.Create("BGMDroneCarrier", 1024, 1, (int)sampleRate, false);
            source.clip = silentClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        void Start()
        {
            source.Play();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            double lfoInc = 2.0 * Mathf.PI * 0.06 / sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;
                for (int o = 0; o < Frequencies.Length; o++)
                {
                    phase[o] += 2.0 * System.Math.PI * Frequencies[o] / sampleRate;
                    sample += (float)System.Math.Sin(phase[o]) * OscGain[o];
                }

                lfoPhase += lfoInc;
                float swell = 1f + 0.4f * (float)System.Math.Sin(lfoPhase);
                sample *= volume * swell * 0.33f;

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] = sample;
                }
            }
        }
    }
}
