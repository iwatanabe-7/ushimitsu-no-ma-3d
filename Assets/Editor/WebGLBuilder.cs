using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Ushimitsu.EditorTools
{
    public static class WebGLBuilder
    {
        [MenuItem("Ushimitsu/Build WebGL")]
        public static void Build()
        {
            // unityroom serves the Build/ folder with the correct Content-Encoding
            // itself, so it wants plain .gz files and no JS-side fallback decompressor.
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;

            string outputPath = "Builds/WebGL";

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                UnityEngine.Debug.Log("[WebGLBuilder] Build succeeded: " + outputPath +
                    " (" + report.summary.totalSize + " bytes)");
            }
            else
            {
                UnityEngine.Debug.LogError("[WebGLBuilder] Build " + report.summary.result +
                    " with " + report.summary.totalErrors + " error(s).");
            }
        }
    }
}
