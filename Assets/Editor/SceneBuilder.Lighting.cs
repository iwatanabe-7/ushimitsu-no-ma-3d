using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ushimitsu.EditorTools
{
    // Baked lighting for SceneBuilder. Everything in the house is static, so the
    // bounce light, soft shadows and contact darkening are all precomputed into
    // lightmaps: far better looking than realtime lights alone, and cheaper to
    // render on phones and in WebGL.
    //
    // Lights are Mixed in Shadowmask mode: their direct light stays realtime (so
    // LightFlicker still works), shadows are baked into a shadowmask, and the
    // indirect bounce is baked into the lightmaps.
    public static partial class SceneBuilder
    {
        const string LightingSettingsPath = "Assets/Scenes/Main.lighting";

        [MenuItem("Ushimitsu/Bake Lighting")]
        public static void BakeLightingMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("[SceneBuilder] Open Assets/Scenes/Main.unity (or run Build Scene) before baking.");
                return;
            }
            BakeLighting(scene, scene.path);
        }

        static void MakeMixedLight(Light light, float bakedShadowRadius)
        {
            light.lightmapBakeType = LightmapBakeType.Mixed;
            light.shadows = LightShadows.Soft;
            light.shadowRadius = bakedShadowRadius;
        }

        static void BuildReflectionProbe(Transform parent, Vector3 center, Vector3 size)
        {
            GameObject go = new GameObject("ReflectionProbe");
            go.transform.SetParent(parent, false);
            go.transform.position = center;

            // Box projection makes the mirror, gilt altar and polished wood reflect
            // the room as a room, rather than as an infinitely distant backdrop.
            ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.size = size;
            probe.resolution = 128;
            probe.hdr = false;
            probe.blendDistance = 0.3f;
        }

        static void MarkStatic(GameObject root)
        {
            const StaticEditorFlags Flags = StaticEditorFlags.ContributeGI
                | StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.ReflectionProbeStatic;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, Flags);
            }
        }

        static void BakeLighting(Scene scene, string scenePath)
        {
            // Fresh settings each time so the bake is fully described by this code.
            AssetDatabase.DeleteAsset(LightingSettingsPath);
            var settings = new LightingSettings { name = "Main" };
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.Shadowmask;
            settings.autoGenerate = false;

            // ~5cm per texel: enough for soft furniture shadows and the corners of
            // the rooms while the whole house still fits in one or two 1024 maps.
            settings.lightmapResolution = 20f;
            settings.lightmapMaxSize = 1024;
            settings.lightmapPadding = 4;
            settings.directionalityMode = LightmapsMode.NonDirectional;

            settings.directSampleCount = 32;
            settings.indirectSampleCount = 256;
            settings.environmentSampleCount = 128;
            settings.maxBounces = 3;

            // Baked ambient occlusion: the dark seams where tatami meets wall, under
            // the altar, inside the tokonoma, that make a room read as solid.
            settings.ao = true;
            settings.aoMaxDistance = 0.6f;
            settings.aoExponentIndirect = 1f;
            settings.aoExponentDirect = 0f;

            AssetDatabase.CreateAsset(settings, LightingSettingsPath);
            Lightmapping.lightingSettings = settings;
            EditorSceneManager.SaveScene(scene, scenePath);

            if (Lightmapping.Bake())
            {
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log("[SceneBuilder] Lighting baked.");
                return;
            }

            // An unbaked Mixed light renders as a realtime shadow-casting light - the
            // blocky, expensive point light shadows this setup exists to avoid - so
            // fall back to shadowless lights until a bake succeeds.
            foreach (Light light in Object.FindObjectsOfType<Light>())
            {
                light.shadows = LightShadows.None;
            }
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.LogError("[SceneBuilder] Lighting bake failed or was cancelled; lights were left shadowless. " +
                "Run Ushimitsu > Bake Lighting to try again.");
        }
    }
}
