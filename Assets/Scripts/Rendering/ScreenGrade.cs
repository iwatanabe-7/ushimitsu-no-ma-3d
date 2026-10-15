using UnityEngine;

namespace Ushimitsu.Rendering
{
    // One cheap full-screen pass that makes the image read as "filmed" rather than
    // rendered: a slightly desaturated, split-toned grade (cold shadows, warm
    // highlights), a soft vignette, faint lens fringing toward the edges, and
    // film grain that lives in the shadows. Written as a single pass instead of
    // using the post-processing package so it stays light enough for phones.
    [RequireComponent(typeof(Camera))]
    public class ScreenGrade : MonoBehaviour
    {
        public Shader shader;

        [Range(0f, 1f)] public float vignette = 0.5f;
        [Range(0f, 0.15f)] public float grain = 0.04f;
        [Range(0f, 1.5f)] public float saturation = 0.8f;
        [Range(0.5f, 1.5f)] public float contrast = 1.1f;
        [Tooltip("Multiplier applied to the shadows (values above 1 allowed).")]
        public Color shadowTint = new Color(0.9f, 0.97f, 1.1f);
        [Tooltip("Multiplier applied to the highlights (values above 1 allowed).")]
        public Color highlightTint = new Color(1.08f, 1f, 0.88f);
        [Range(0f, 0.02f)] public float aberration = 0.004f;

        static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        static readonly int GrainId = Shader.PropertyToID("_Grain");
        static readonly int GrainSeedId = Shader.PropertyToID("_GrainSeed");
        static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        static readonly int ShadowTintId = Shader.PropertyToID("_ShadowTint");
        static readonly int HighlightTintId = Shader.PropertyToID("_HighlightTint");
        static readonly int AberrationId = Shader.PropertyToID("_Aberration");

        Material material;

        void OnEnable()
        {
            if (shader == null || !shader.isSupported)
            {
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (material != null)
            {
                Destroy(material);
                material = null;
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null)
            {
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            material.SetFloat(VignetteId, vignette);
            material.SetFloat(GrainId, grain);
            material.SetFloat(GrainSeedId, Random.value * 1000f);
            material.SetFloat(SaturationId, saturation);
            material.SetFloat(ContrastId, contrast);
            material.SetColor(ShadowTintId, shadowTint);
            material.SetColor(HighlightTintId, highlightTint);
            material.SetFloat(AberrationId, aberration);

            Graphics.Blit(source, destination, material);
        }
    }
}
